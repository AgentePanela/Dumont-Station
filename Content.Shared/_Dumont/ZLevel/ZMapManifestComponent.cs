namespace Content.Shared._Dumont.ZLevel;

/// <summary>
/// Placed on the base map entity of a z-map. Lists which z-levels (by depth) exist so the loader
/// knows which sibling files to open.
/// </summary>
[RegisterComponent]
public sealed partial class ZMapManifestComponent : Component
{
    /// <summary>
    /// Depths of the floors that exist, e.g. 1, 2, -1. 0 is the base itself and isn't listed.
    /// </summary>
    [DataField]
    public List<int> Depths = new();

    [ViewVariables]
    public bool Built;
}
