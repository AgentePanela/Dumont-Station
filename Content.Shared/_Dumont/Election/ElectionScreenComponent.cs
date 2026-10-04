using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Dumont.Election;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElectionScreenComponent : Component
{
    [DataField, AutoNetworkedField]
    public ElectionScreenMode Mode = ElectionScreenMode.Scrolling;
}

/// <summary>
/// spawns an election screen on top of whatever has this when the map loads
/// </summary>
[RegisterComponent]
public sealed partial class ElectionScreenSpawnerComponent : Component
{
    [DataField]
    public EntProtoId Prototype = "ElectionScreen";
}

[Serializable, NetSerializable]
public enum ElectionScreenMode : byte
{
    Scrolling,
    Top3,
}
