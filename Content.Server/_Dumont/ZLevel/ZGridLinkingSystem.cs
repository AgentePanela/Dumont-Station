using Content.Server.Station.Components;
using Content.Shared._Dumont.ZLevel;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Dumont.ZLevel;

/// <summary>
/// Glues grids across z-levels: keeps linked grids moving together, matches pairs by their saved
/// link id (<see cref="BecomesGridLinkingComponent"/>) and stamps those ids when saving.
/// </summary>
public sealed class ZGridLinkingSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ZGridLinkingComponent, MoveEvent>(OnLinkedGridMove);
    }

    /// <summary>
    /// Links two grids vertically so they move together from then on.
    /// </summary>
    public void Link(EntityUid below, EntityUid above)
    {
        var belowLink = EnsureComp<ZGridLinkingComponent>(below);
        var aboveLink = EnsureComp<ZGridLinkingComponent>(above);
        belowLink.GridAbove = above;
        aboveLink.GridBelow = below;
    }

    /// <summary>
    /// Aligns a grid onto its counterpart's world transform and links the two.
    /// </summary>
    public void AlignAndLink(EntityUid grid, EntityUid anchorGrid, bool up)
    {
        _transform.SetMapCoordinates(grid,
            new MapCoordinates(_transform.GetWorldPosition(anchorGrid), Transform(grid).MapID));
        _transform.SetWorldRotation(grid, _transform.GetWorldRotation(anchorGrid));

        if (up)
            Link(anchorGrid, grid);
        else
            Link(grid, anchorGrid);
    }

    /// <summary>
    /// Makes sure a grid has a saved link id, deriving it from the station id when possible.
    /// </summary>
    public string EnsureLinkId(EntityUid grid)
    {
        if (TryComp<BecomesGridLinkingComponent>(grid, out var existing))
            return existing.Id;

        var link = AddComp<BecomesGridLinkingComponent>(grid);
        if (TryComp<BecomesStationComponent>(grid, out var becomes))
            link.Id = becomes.Id;
        else if (TryComp<BecomesStationLevelComponent>(grid, out var level))
            link.Id = level.Id;
        else
            link.Id = $"zlink-{grid.Id}";

        return link.Id;
    }

    /// <summary>
    /// Finds the grid on <paramref name="map"/> carrying the given link id, if any.
    /// </summary>
    public EntityUid? FindLinkedGrid(EntityUid map, string id)
    {
        var query = EntityQueryEnumerator<BecomesGridLinkingComponent, MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var link, out _, out var xform))
        {
            if (xform.MapUid == map && link.Id == id)
                return uid;
        }

        return null;
    }

    /// <summary>
    /// Turns the runtime grid links into saved ids
    /// </summary>
    public void StampLinkIds(HashSet<EntityUid> stackMaps)
    {
        var query = EntityQueryEnumerator<ZGridLinkingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var link, out var xform))
        {
            if (xform.MapUid is not { } map || !stackMaps.Contains(map))
                continue;
            if (HasComp<BecomesGridLinkingComponent>(uid))
                continue; // already covered by another chain walk

            var id = EnsureLinkId(uid);
            StampChain(link.GridAbove, id, up: true);
            StampChain(link.GridBelow, id, up: false);
        }
    }

    private void StampChain(EntityUid? grid, string id, bool up)
    {
        while (grid is { } g && !Deleted(g))
        {
            EnsureComp<BecomesGridLinkingComponent>(g).Id = id;
            grid = TryComp<ZGridLinkingComponent>(g, out var link)
                ? (up ? link.GridAbove : link.GridBelow)
                : null;
        }
    }

    private void OnLinkedGridMove(EntityUid uid, ZGridLinkingComponent comp, ref MoveEvent args)
    {
        SyncLinkedGrid(uid, comp.GridAbove);
        SyncLinkedGrid(uid, comp.GridBelow);
    }

    private void SyncLinkedGrid(EntityUid source, EntityUid? target)
    {
        if (target == null || Deleted(target.Value))
            return;

        var pos = _transform.GetWorldPosition(source);
        var rot = _transform.GetWorldRotation(source);

        if ((pos - _transform.GetWorldPosition(target.Value)).LengthSquared() < 1e-8f
            && Math.Abs(Angle.ShortestDistance(rot, _transform.GetWorldRotation(target.Value)).Theta) < 1e-5)
            return;

        _transform.SetWorldPosition(target.Value, pos);
        _transform.SetWorldRotation(target.Value, rot);
    }
}
