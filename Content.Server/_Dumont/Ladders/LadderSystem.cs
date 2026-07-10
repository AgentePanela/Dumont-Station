using Content.Server._Dumont.ZLevel;
using Content.Shared._Dumont.Ladders;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Server._Dumont.Ladders;

public sealed class LadderSystem : SharedLadderSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ZLevelSystem _zLevel = default!;

    private const float MinWalkSpeed = 0.1f; // below this we can't tell what direction you're walking

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StairsComponent, StepTriggeredOffEvent>(OnStairsStepTriggered);
        SubscribeLocalEvent<LadderComponent, LadderClimbDoAfterEvent>(OnLadderClimb);
    }

    private void OnStairsStepTriggered(Entity<StairsComponent> ent, ref StepTriggeredOffEvent args)
    {
        var walkDir = _transform.GetWorldRotation(ent).GetCardinalDir().GetOpposite();

        // make that only if u are walking in the stairs direction u get teleported
        if (!TryComp<PhysicsComponent>(args.Tripper, out var physics)
            || physics.LinearVelocity.Length() < MinWalkSpeed
            || physics.LinearVelocity.ToWorldAngle().GetCardinalDir() != walkDir)
            return;

        if (!_zLevel.TryMoveZ(args.Tripper, ent.Comp.Up, out var error, keepPull: true))
            _popup.PopupEntity(error, args.Tripper, args.Tripper);
    }

    private void OnLadderClimb(Entity<LadderComponent> ent, ref LadderClimbDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_zLevel.TryMoveZ(args.User, ent.Comp.Up, out var error, keepPull: true))
            _popup.PopupEntity(error, args.User, args.User);

        args.Handled = true;
    }
}
