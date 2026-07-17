using System.Numerics;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Dumont.ZLevel;
using Content.Shared.Maps;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Parallax;
using Content.Shared.Station.Components;
using Robust.Server.GameStates;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Dumont.ZLevel;

/// <summary>
/// Manages stacks of z-level maps: creating/linking levels and moving entities between them.
/// </summary>
public sealed class ZLevelSystem : SharedZLevelSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ZGridLinkingSystem _gridLinking = default!;

    private static readonly ProtoId<ContentTileDefinition> LatticeTile = "Lattice";
    private const float DarknessPerDepth = 0.35f; //darness in sub z levels

    /// <summary>
    /// Creates an empty map and links it as the z-level right above or below.
    /// </summary>
    public bool TryAddLevel(EntityUid sourceMap, bool up, out EntityUid newMap, out string error,
        bool initialized = false)
    {
        newMap = EntityUid.Invalid;
        error = string.Empty;

        var source = EnsureOrigin(sourceMap);

        var existing = up ? source.MapAbove : source.MapBelow;
        if (existing != null && !Deleted(existing.Value))
        {
            error = Loc.GetString("zlevel-error-level-exists");
            return false;
        }

        newMap = _map.CreateMap(out _, runMapInit: initialized);
        LinkLevelMaps(sourceMap, source, newMap, up);
        return true;
    }

    /// <summary>
    /// Puts a floor grid on the level above/below the map <paramref name="anchorGrid"/> is on, creating
    /// the level if needed
    /// </summary>
    public bool TryAddGrid(EntityUid anchorGrid, bool up, ResPath? gridPath, out EntityUid newGrid,
        out string error, bool initialized = false)
    {
        newGrid = EntityUid.Invalid;
        error = string.Empty;

        if (Transform(anchorGrid).MapUid is not { } sourceMap)
        {
            error = Loc.GetString("zlevel-command-no-map");
            return false;
        }

        // reuse the adjacent level if it's already there, otherwise make it
        EntityUid levelMap;
        if (TryComp<ZLevelMapComponent>(sourceMap, out var srcZ)
            && (up ? srcZ.MapAbove : srcZ.MapBelow) is { } existing && !Deleted(existing))
            levelMap = existing;
        else if (!TryAddLevel(sourceMap, up, out levelMap, out error, initialized))
            return false;

        if (!TryComp<MapComponent>(levelMap, out var levelMapComp))
        {
            error = Loc.GetString("zlevel-error-no-level-in-direction");
            return false;
        }

        var anchorPos = _transform.GetWorldPosition(anchorGrid);
        if (gridPath != null)
        {
            if (!_loader.TryLoadGrid(levelMapComp.MapId, gridPath.Value, out var loadedGrid))
            {
                error = Loc.GetString("zlevel-error-grid-load", ("path", gridPath.Value.ToString()));
                return false;
            }

            newGrid = loadedGrid.Value.Owner;
            _gridLinking.AlignAndLink(newGrid, anchorGrid, up);
        }
        else
            newGrid = CreateLatticeZGrid(levelMapComp.MapId, up, anchorGrid, anchorPos);

        // both sides carry the same link id so this pair finds each other again on load
        EnsureComp<BecomesGridLinkingComponent>(newGrid).Id = _gridLinking.EnsureLinkId(anchorGrid);

        var depth = Comp<ZLevelMapComponent>(levelMap).Depth;
        StampStationLevel(newGrid, anchorGrid, depth);
        AddToAnchorStation(newGrid, anchorGrid);
        return true;
    }

    public bool TryLoadLevel(EntityUid sourceMap, bool up, ResPath mapPath, bool initialized,
        Vector2 offset, Angle rotation, out EntityUid newMap, out string error)
    {
        newMap = EntityUid.Invalid;
        error = string.Empty;

        var source = EnsureOrigin(sourceMap);
        var existing = up ? source.MapAbove : source.MapBelow;
        if (existing != null && !Deleted(existing.Value))
        {
            error = Loc.GetString("zlevel-error-level-exists");
            return false;
        }

        var opts = new DeserializationOptions { InitializeMaps = initialized };
        if (!_loader.TryLoadMap(mapPath, out var map, out var grids, opts, offset, rotation))
        {
            error = Loc.GetString("zlevel-error-grid-load", ("path", mapPath.ToString()));
            return false;
        }

        newMap = map.Value.Owner;
        LinkLevelMaps(sourceMap, source, newMap, up);

        foreach (var grid in grids)
        {
            if (!TryComp<BecomesGridLinkingComponent>(grid.Owner, out var link))
                continue;

            var isStationLevel = HasComp<BecomesStationLevelComponent>(grid.Owner);
            var match = _gridLinking.FindLinkedGrid(sourceMap, link.Id);

            // already positioned by the shared transform above, so just glue it to its counterpart
            // for future movement — don't re-align (that could snap it onto the wrong grid)
            if (match != null)
                _gridLinking.Link(up ? match.Value : grid.Owner, up ? grid.Owner : match.Value);

            if (isStationLevel)
                AddToAnchorStation(grid.Owner, match ?? FindStationMemberGrid(sourceMap));
        }

        return true;
    }

    private EntityUid? FindStationMemberGrid(EntityUid map)
    {
        var query = EntityQueryEnumerator<StationMemberComponent, MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var xform))
        {
            if (xform.MapUid == map)
                return uid;
        }

        return null;
    }

    /// <summary>
    /// Moves an entity one z-level up or down, keeping its world position.
    /// </summary>
    public bool TryMoveZ(EntityUid entity, bool up, out string error, bool keepPull = false)
    {
        error = string.Empty;
        var xform = Transform(entity);

        if (xform.MapUid is not { } currentMap || !TryComp<ZLevelMapComponent>(currentMap, out var zLevel))
        {
            error = Loc.GetString("zlevel-error-no-levels");
            return false;
        }

        var target = up ? zLevel.MapAbove : zLevel.MapBelow;
        if (target == null || Deleted(target.Value) || !TryComp<MapComponent>(target.Value, out var targetMap))
        {
            error = Loc.GetString("zlevel-error-no-level-in-direction");
            return false;
        }

        // try stop the pulling
        if (TryComp<PullableComponent>(entity, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(entity, pullable, ignoreGrab: true);

        EntityUid? pulled = null;
        var pulledWorldPos = Vector2.Zero;
        if (keepPull && TryComp<PullerComponent>(entity, out var puller)
            && puller.Pulling is { } pulling
            && TryComp<PullableComponent>(pulling, out var pulledPullable))
        {
            pulled = pulling;
            pulledWorldPos = _transform.GetWorldPosition(pulling); // grab this before we move the puller
            _pulling.TryStopPull(pulling, pulledPullable, ignoreGrab: true);
        }

        var worldPos = _transform.GetWorldPosition(entity); // try to fix rotation
        var worldRot = _transform.GetWorldRotation(entity);
        _transform.SetMapCoordinates(entity, new MapCoordinates(worldPos, targetMap.MapId));
        _transform.SetWorldRotation(entity, worldRot);
        if (pulled != null)
        {
            _transform.SetMapCoordinates(pulled.Value, new MapCoordinates(pulledWorldPos, targetMap.MapId));
            _pulling.TryStartPull(entity, pulled.Value);
        }

        return true;
    }

    /// <summary>
    /// Copies the anchor's station id onto a new floor grid so it rejoins the right station on load.
    /// Reads BecomesStationComponent.Id, hence this system is in its access list.
    /// </summary>
    public void StampStationLevel(EntityUid newGrid, EntityUid anchorGrid, int depth)
    {
        string? id = null;
        if (TryComp<BecomesStationComponent>(anchorGrid, out var becomes))
            id = becomes.Id;
        else if (TryComp<BecomesStationLevelComponent>(anchorGrid, out var anchorLevel))
            id = anchorLevel.Id;

        if (id == null)
            return;

        var level = EnsureComp<BecomesStationLevelComponent>(newGrid);
        level.Id = id;
        level.Depth = depth;
    }

    // marks a map as a stack origin the first time we touch it
    private ZLevelMapComponent EnsureOrigin(EntityUid map)
    {
        if (!EnsureComp<ZLevelMapComponent>(map, out var comp))
        {
            comp.BaseMap = map;
            if (TryComp<MapLightComponent>(map, out var light))
                comp.BaseAmbientLight = light.AmbientLightColor;
        }

        return comp;
    }

    // wires the map-level links, parallax, ambient light and PVS between an existing source and a new level
    private void LinkLevelMaps(EntityUid sourceMap, ZLevelMapComponent source, EntityUid newMap, bool up)
    {
        var newComp = EnsureComp<ZLevelMapComponent>(newMap);
        newComp.Depth = source.Depth + (up ? 1 : -1);
        newComp.BaseAmbientLight = source.BaseAmbientLight;
        newComp.BaseMap = source.BaseMap;
        if (up)
        {
            newComp.MapBelow = sourceMap;
            source.MapAbove = newMap;
        }
        else
        {
            newComp.MapAbove = sourceMap;
            source.MapBelow = newMap;
        }

        Dirty(sourceMap, source);
        Dirty(newMap, newComp);

        if (TryComp<ParallaxComponent>(sourceMap, out var sourceParallax))
        {
            var parallax = EnsureComp<ParallaxComponent>(newMap);
            parallax.Parallax = sourceParallax.Parallax;
            Dirty(newMap, parallax);
        }

        if (TryComp<MapComponent>(newMap, out var mapComp))
            _map.SetAmbientLight(mapComp.MapId, ComputeAmbient(newComp.BaseAmbientLight, newComp.Depth));

        // send just the map entities (not their contents!) so clients can resolve the links;
        // what's actually on the lower levels arrives via the ZLevelViewSystem proxies
        _pvs.AddForceSend(sourceMap);
        _pvs.AddForceSend(newMap);

        _metaData.SetEntityName(newMap, $"Z[{newComp.Depth}] {Name(sourceMap)}");
    }

    private void AddToAnchorStation(EntityUid newGrid, EntityUid? anchorGrid)
    {
        if (anchorGrid != null && _station.GetOwningStation(anchorGrid.Value) is { } station)
            _station.AddGridToStation(station, newGrid, name: Name(station));
    }

    private EntityUid CreateLatticeZGrid(MapId newMapId, bool up, EntityUid? anchorGrid, Vector2 anchorPos)
    {
        var grid = _mapManager.CreateGridEntity(newMapId);
        var center = Vector2i.Zero;

        if (anchorGrid != null && TryComp<MapGridComponent>(anchorGrid.Value, out var anchorComp))
        {
            _gridLinking.AlignAndLink(grid, anchorGrid.Value, up);

            var anchorMapId = Transform(anchorGrid.Value).MapID;
            center = _map.TileIndicesFor(anchorGrid.Value, anchorComp, new MapCoordinates(anchorPos, anchorMapId));
        }
        else
            _transform.SetMapCoordinates(grid, new MapCoordinates(anchorPos, newMapId));

        var lattice = new Tile(_tileDefs[LatticeTile].TileId);

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                _map.SetTile(grid.Owner, grid.Comp, center + new Vector2i(x, y), lattice);
            }
        }

        return grid.Owner;
    }

    private Color ComputeAmbient(Color baseColor, int depth)
    {
        if (depth >= 0)
            return baseColor;

        return Color.InterpolateBetween(baseColor, Color.Black, Math.Min(1f, -depth * DarknessPerDepth));
    }
}
