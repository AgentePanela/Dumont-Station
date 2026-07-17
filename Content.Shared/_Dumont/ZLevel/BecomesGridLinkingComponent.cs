namespace Content.Shared._Dumont.ZLevel;

/// <summary>
/// Identifies which grids link with which across z-levels during map loading
/// </summary>
[RegisterComponent]
public sealed partial class BecomesGridLinkingComponent : Component
{
    [DataField(required: true)]
    public string Id = default!;
}
