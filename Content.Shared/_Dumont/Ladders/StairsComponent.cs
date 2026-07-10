using Robust.Shared.GameStates;

namespace Content.Shared._Dumont.Ladders;

/// <summary>
/// Stepping on this moves you one z-level up or down.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StairsComponent : Component
{
    [DataField]
    public bool Up = true;
}
