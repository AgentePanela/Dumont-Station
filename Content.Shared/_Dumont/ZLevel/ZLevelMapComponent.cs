using Robust.Shared.GameStates;

namespace Content.Shared._Dumont.ZLevel;

/// <summary>
/// Marks a map entity as part of a vertical stack of z-levels, linking it to the maps directly above and below.
/// Client-side uses this to know which map to render underneath the current one.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, UnsavedComponent]
public sealed partial class ZLevelMapComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? MapAbove;

    [DataField, AutoNetworkedField]
    public EntityUid? MapBelow;

    /// <summary>
    /// The origin (depth 0) map of the stack. Every level points back to it so you can reach
    /// the "main" map without walking the whole chain.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BaseMap;

    /// <summary>
    /// 0 is the origin level
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Depth;

    /// <summary>
    /// Ambient light of the origin level. Lower levels get darker relative to this.
    /// Used for planet maps.
    /// </summary>
    [DataField]
    public Color BaseAmbientLight = Color.Black;
}
