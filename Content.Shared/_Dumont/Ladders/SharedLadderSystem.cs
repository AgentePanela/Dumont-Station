using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Verbs;

namespace Content.Shared._Dumont.Ladders;

public abstract class SharedLadderSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StairsComponent, StepTriggerAttemptEvent>(OnStairsStepAttempt);
        SubscribeLocalEvent<LadderComponent, CanDropTargetEvent>(OnLadderCanDrop);
        SubscribeLocalEvent<LadderComponent, DragDropTargetEvent>(OnLadderDragDrop);
        SubscribeLocalEvent<LadderComponent, GetVerbsEvent<AlternativeVerb>>(OnLadderGetVerbs);
    }

    private void OnStairsStepAttempt(Entity<StairsComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnLadderCanDrop(Entity<LadderComponent> ent, ref CanDropTargetEvent args)
    {
        args.CanDrop = args.User == args.Dragged;
        args.Handled = true;
    }

    private void OnLadderDragDrop(Entity<LadderComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled || args.User != args.Dragged)
            return;

        args.Handled = TryStartClimb(ent, args.User);
    }

    private void OnLadderGetVerbs(Entity<LadderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryStartClimb(ent, user),
            Text = Loc.GetString(ent.Comp.Up ? "ladder-verb-climb-up" : "ladder-verb-climb-down"),
        });
    }

    private bool TryStartClimb(Entity<LadderComponent> ent, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, ent.Comp.ClimbDelay,
            new LadderClimbDoAfterEvent(), ent, target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DuplicateCondition = DuplicateConditions.SameTarget, // anti spam
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }
}
