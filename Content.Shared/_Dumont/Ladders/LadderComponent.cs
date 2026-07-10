using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Dumont.Ladders;

/// <summary>
/// Drag yourself onto this to climb one z-level up or down.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LadderComponent : Component
{
    [DataField]
    public bool Up = true;

    [DataField]
    public TimeSpan ClimbDelay = TimeSpan.FromSeconds(3.5);
}

[Serializable, NetSerializable]
public sealed partial class LadderClimbDoAfterEvent : SimpleDoAfterEvent
{
}
