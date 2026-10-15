using Robust.Shared.Prototypes;

namespace Content.Shared._Dumont.Curse;

[Prototype("curse")]
public sealed partial class CursePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Added to the mob.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();
}
