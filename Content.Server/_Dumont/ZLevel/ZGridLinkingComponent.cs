namespace Content.Server._Dumont.ZLevel;

/// <summary>
/// Keeps grids on neighbouring z-levels glued together -  when one moves, the others follows.
/// </summary>
[RegisterComponent]
public sealed partial class ZGridLinkingComponent : Component
{
    [ViewVariables]
    public EntityUid? GridAbove;

    [ViewVariables]
    public EntityUid? GridBelow;
}
