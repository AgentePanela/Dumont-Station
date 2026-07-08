namespace Content.Server._Dumont.ZLevel;

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
