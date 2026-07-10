using System.Numerics;
using Content.Server.Station.Systems;
using Content.Shared._Dumont.ZLevel;
using Content.Shared.Maps;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Parallax;
using Robust.Server.GameStates;
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
    /// Creates a new map and links it as the z-level above or below <paramref name="sourceMap"/>.
    /// Loads a grid onto it when <paramref name="gridPath"/> is given; otherwise spawns a small
    /// lattice platform aligned and linked/>.
    /// </summary>
    public bool TryAddLevel(EntityUid sourceMap, bool up, ResPath? gridPath, EntityUid? anchorGrid, Vector2 anchorPos,
        out EntityUid newMap, out string error)
    {
        newMap = EntityUid.Invalid;
        error = string.Empty;

        if (!EnsureComp<ZLevelMapComponent>(sourceMap, out var source))
        {
            // just added it, so this map is the origin of a new stack
            source.BaseMap = sourceMap;
            if (TryComp<MapLightComponent>(sourceMap, out var light))
                source.BaseAmbientLight = light.AmbientLightColor;
        }

        var existing = up ? source.MapAbove : source.MapBelow;
        if (existing != null && !Deleted(existing.Value))
        {
            error = Loc.GetString("zlevel-error-level-exists");
            return false;
        }

        var newMapUid = _map.CreateMap(out var newMapId, runMapInit: true);
        EntityUid? newGrid = null;
        if (gridPath != null)
        {
            if (!_loader.TryLoadGrid(newMapId, gridPath.Value, out var loadedGrid))
            {
                Del(newMapUid);
                error = Loc.GetString("zlevel-error-grid-load", ("path", gridPath.Value.ToString()));
                return false;
            }

            newGrid = loadedGrid.Value.Owner;
            if (anchorGrid != null)
                AlignAndLinkGrid(newGrid.Value, anchorGrid.Value, up);
        }
        else
            newGrid = CreateLatticeZGrid(newMapId, up, anchorGrid, anchorPos);

        var newComp = AddComp<ZLevelMapComponent>(newMapUid);
        newComp.Depth = source.Depth + (up ? 1 : -1);
        newComp.BaseAmbientLight = source.BaseAmbientLight;
        newComp.BaseMap = source.BaseMap;
        if (up)
        {
            newComp.MapBelow = sourceMap;
            source.MapAbove = newMapUid;
        }
        else
        {
            newComp.MapAbove = sourceMap;
            source.MapBelow = newMapUid;
        }

        Dirty(sourceMap, source);
        Dirty(newMapUid, newComp);

        if (TryComp<ParallaxComponent>(sourceMap, out var sourceParallax))
        {
            var parallax = EnsureComp<ParallaxComponent>(newMapUid);
            parallax.Parallax = sourceParallax.Parallax;
            Dirty(newMapUid, parallax);
        }

        _map.SetAmbientLight(newMapId, ComputeAmbient(newComp.BaseAmbientLight, newComp.Depth));

        // send just the map entities (not their contents!) so clients can resolve the links;
        // what's actually on the lower levels arrives via the ZLevelViewSystem proxies
        _pvs.AddForceSend(sourceMap);
        _pvs.AddForceSend(newMapUid);

        _metaData.SetEntityName(newMapUid, $"Z[{newComp.Depth}] {Name(sourceMap)}");

        // add the new grids to the station
        if (newGrid != null && anchorGrid != null
            && _station.GetOwningStation(anchorGrid.Value) is { } station)
        {
            _station.AddGridToStation(station, newGrid.Value, name: Name(station)); // get the same name of the station
        }

        newMap = newMapUid;
        return true;
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


    private EntityUid CreateLatticeZGrid(MapId newMapId, bool up, EntityUid? anchorGrid, Vector2 anchorPos)
    {
        var grid = _mapManager.CreateGridEntity(newMapId);
        var center = Vector2i.Zero;

        if (anchorGrid != null && TryComp<MapGridComponent>(anchorGrid.Value, out var anchorComp))
        {
            AlignAndLinkGrid(grid, anchorGrid.Value, up);

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

    // Linked grids copy each other's world transform when moving, so the new grid has to be
    // aligned with its counterpart before linking — otherwise the first sync would teleport
    // one onto the other.
    private void AlignAndLinkGrid(EntityUid grid, EntityUid anchorGrid, bool up)
    {
        _transform.SetMapCoordinates(grid,
            new MapCoordinates(_transform.GetWorldPosition(anchorGrid), Transform(grid).MapID));
        _transform.SetWorldRotation(grid, _transform.GetWorldRotation(anchorGrid));

        if (up)
            _gridLinking.Link(anchorGrid, grid);
        else
            _gridLinking.Link(grid, anchorGrid);
    }

    private Color ComputeAmbient(Color baseColor, int depth)
    {
        if (depth >= 0)
            return baseColor;

        return Color.InterpolateBetween(baseColor, Color.Black, Math.Min(1f, -depth * DarknessPerDepth));
    }
}
