namespace Content.Shared._Dumont.ZLevel;

/// <summary>
/// Marks the station main grid of a z-level floor. Mirrors <c>BecomesStationComponent</c>:
/// </summary>
[RegisterComponent]
public sealed partial class BecomesStationLevelComponent : Component
{
    /// <summary>
    /// Same id string as the base grid BecomStationComp
    /// </summary>
    [DataField(required: true)]
    public string Id = default!;

    [DataField]
    public int Depth;
}
