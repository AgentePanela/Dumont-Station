using System.Linq;
using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Dumont.ZLevel;
using Content.Shared.Station.Components;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server._Dumont.ZLevel;

/// <summary>
/// Persists and reconstructs whole z-stacks: the base map carries a <see cref="ZMapManifestComponent"/>
/// listing the floor depths, and each floor lives in a map file.
/// </summary>
public sealed class ZMapManifestSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ZGridLinkingSystem _gridLinking = default!;
    [Dependency] private readonly ZLevelSystem _zLevel = default!;

    private Vector2 _pendingOffset;
    private Angle _pendingRotation;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PreGameMapLoad>(OnPreGameMapLoad);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad, after: [typeof(StationSystem)]);
        SubscribeLocalEvent<ZMapManifestComponent, MapInitEvent>(OnBaseMapInit);
    }

    private void OnPreGameMapLoad(PreGameMapLoad ev)
    {
        _pendingOffset = ev.Offset;
        _pendingRotation = ev.Rotation;
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        var baseMap = _map.GetMap(ev.Map);
        if (HasComp<ZMapManifestComponent>(baseMap))
            LoadZStack(baseMap, ev.GameMap.MapPath, initialized: false, _pendingOffset, _pendingRotation);
    }

    private void OnBaseMapInit(Entity<ZMapManifestComponent> ent, ref MapInitEvent args)
    {
        List<(int depth, EntityUid map)> floors = [];
        GatherSide(ent, true, floors);
        GatherSide(ent, false, floors);
        foreach (var f in floors)
            InitMap(f.map);
    }

    /// <summary>
    /// Rebuilds a whole stack from a base map's manifest, loading each floor map file
    /// from the same directory as <paramref name="basePath"/>.
    /// </summary>
    public void LoadZStack(EntityUid baseMap, ResPath basePath, bool initialized,
        Vector2 offset = default, Angle rotation = default)
    {
        if (!TryComp<ZMapManifestComponent>(baseMap, out var manifest) || manifest.Built)
            return;
        manifest.Built = true;

        var dir = basePath.Directory;
        var stem = basePath.FilenameWithoutExtension;

        LoadSide(baseMap, dir, stem, manifest.Depths.Where(d => d > 0).OrderBy(d => d), true, initialized, offset, rotation);
        LoadSide(baseMap, dir, stem, manifest.Depths.Where(d => d < 0).OrderByDescending(d => d), false, initialized, offset, rotation);
    }

    private void LoadSide(EntityUid baseMap, ResPath dir, string stem,
        IEnumerable<int> depths, bool up, bool initialized, Vector2 offset, Angle rotation)
    {
        var source = baseMap;
        foreach (var depth in depths)
        {
            var path = dir / $"{stem}_z{depth}.yml";
            if (!_zLevel.TryLoadLevel(source, up, path, initialized, offset, rotation, out var levelMap, out var error))
            {
                Log.Error($"z-level: failed loading {path}: {error}");
                return; // deeper floors hang off this one, so stop the side
            }

            source = levelMap;
        }
    }

    /// <summary>
    /// Saves the base map and every floor
    /// in the same directory, stamping the manifest, link ids and station markers.
    /// </summary>
    public bool TrySaveZMap(EntityUid stackMap, ResPath path, out string error)
    {
        error = string.Empty;

        // any map of the stack works, everything gets saved from the base
        if (!TryComp<ZLevelMapComponent>(stackMap, out var z))
        {
            error = Loc.GetString("zlevel-error-no-levels");
            return false;
        }

        var baseMap = z.BaseMap ?? stackMap;
        var dir = path.Directory;
        var stem = path.FilenameWithoutExtension;

        List<(int depth, EntityUid map)> floors = [];
        GatherSide(baseMap, true, floors);
        GatherSide(baseMap, false, floors);

        var manifest = EnsureComp<ZMapManifestComponent>(baseMap);
        manifest.Depths = floors.Select(f => f.depth).ToList();

        StampLinkIds(baseMap, floors);
        StampStationLevels(baseMap, floors);

        // don't bake a dead station uid into the files; the loader re-adds membership
        List<(EntityUid grid, EntityUid station)> restore = [];
        StripStationMembers(baseMap, restore);
        foreach (var f in floors)
            StripStationMembers(f.map, restore);

        var ok = _loader.TrySaveMap(baseMap, dir / $"{stem}.yml");
        foreach (var f in floors)
            ok &= _loader.TrySaveMap(f.map, dir / $"{stem}_z{f.depth}.yml");

        foreach (var (grid, station) in restore)
            _station.AddGridToStation(station, grid, name: Name(station));

        if (!ok)
            error = Loc.GetString("zlevel-save-failed");
        return ok;
    }

    /// <summary>
    /// Loads a saved z-map (base + its floors) uninitialized, for editing in the map editor.
    /// Optionally forces the base map's id so you know which id to pass to mapzinit afterwards.
    /// </summary>
    public bool TryLoadZMap(ResPath path, MapId? forcedId, out string error)
    {
        error = string.Empty;

        var dir = path.Directory;
        var stem = path.FilenameWithoutExtension;
        var basePath = dir / $"{stem}.yml";
        var opts = new DeserializationOptions { InitializeMaps = false };

        bool loaded;
        Entity<MapComponent>? map;
        if (forcedId is { } id)
            loaded = _loader.TryLoadMapWithId(id, basePath, out map, out _, opts);
        else
            loaded = _loader.TryLoadMap(basePath, out map, out _, opts);

        if (!loaded || map is not { } baseMap)
        {
            error = Loc.GetString("zlevel-error-grid-load", ("path", basePath.ToString()));
            return false;
        }

        if (!HasComp<ZMapManifestComponent>(baseMap.Owner))
        {
            error = Loc.GetString("zlevel-load-no-manifest", ("path", basePath.ToString()));
            return false;
        }

        LoadZStack(baseMap.Owner, basePath, initialized: false);
        return true;
    }

    /// <summary>
    /// Map-initializes every map in the stack. Handy after loadzmap, which loads uninitialized
    /// for editing.
    /// </summary>
    public bool TryInitStack(EntityUid stackMap, out string error)
    {
        error = string.Empty;

        // any map of the stack works, everything gets inited from the base
        if (!TryComp<ZLevelMapComponent>(stackMap, out var z))
        {
            error = Loc.GetString("zlevel-error-no-levels");
            return false;
        }

        var baseMap = z.BaseMap ?? stackMap;
        List<(int depth, EntityUid map)> floors = [];
        GatherSide(baseMap, true, floors);
        GatherSide(baseMap, false, floors);

        InitMap(baseMap);
        foreach (var f in floors)
            InitMap(f.map);

        return true;
    }

    private void InitMap(EntityUid map)
    {
        if (!_map.IsInitialized(map))
            _map.InitializeMap(map);
    }

    // walks one direction of the stack collecting each floor's map
    private void GatherSide(EntityUid baseMap, bool up, List<(int depth, EntityUid map)> floors)
    {
        var map = baseMap;
        while (TryComp<ZLevelMapComponent>(map, out var z))
        {
            var next = up ? z.MapAbove : z.MapBelow;
            if (next == null || Deleted(next.Value))
                break;

            floors.Add((Comp<ZLevelMapComponent>(next.Value).Depth, next.Value));
            map = next.Value;
        }
    }

    private void StampLinkIds(EntityUid baseMap, List<(int depth, EntityUid map)> floors)
    {
        var stackMaps = new HashSet<EntityUid> { baseMap };
        foreach (var f in floors)
            stackMaps.Add(f.map);

        _gridLinking.StampLinkIds(stackMaps);
    }

    // station member grids get the station-level marker so they rejoin the station on load
    private void StampStationLevels(EntityUid baseMap, List<(int depth, EntityUid map)> floors)
    {
        if (GetStationGrid(baseMap) is not { } baseGrid)
            return;

        foreach (var (depth, map) in floors)
        {
            var query = EntityQueryEnumerator<StationMemberComponent, MapGridComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out _, out _, out var xform))
            {
                if (xform.MapUid == map)
                    _zLevel.StampStationLevel(uid, baseGrid, depth);
            }
        }
    }

    private EntityUid? GetStationGrid(EntityUid map)
    {
        var query = EntityQueryEnumerator<BecomesStationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid == map)
                return uid;
        }

        return null;
    }

    private void StripStationMembers(EntityUid map, List<(EntityUid grid, EntityUid station)> restore)
    {
        List<EntityUid> toStrip = [];
        var query = EntityQueryEnumerator<StationMemberComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var member, out var xform))
        {
            if (xform.MapUid != map)
                continue;

            restore.Add((uid, member.Station));
            toStrip.Add(uid);
        }

        foreach (var uid in toStrip)
            RemComp<StationMemberComponent>(uid);
    }
}
