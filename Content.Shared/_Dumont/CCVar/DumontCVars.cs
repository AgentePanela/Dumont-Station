using Robust.Shared.Configuration;

namespace Content.Shared._Dumont.CCVar;

[CVarDefs]
public sealed partial class DumontCVars
{
    public static readonly CVarDef<bool> CurseEnabled =
        CVarDef.Create("dumont.curse.enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Comma separated user ids that roll a curse when spawning on the station.
    /// </summary>
    public static readonly CVarDef<string> CurseUserIds =
        CVarDef.Create("dumont.curse.user_ids", "5f430e69-8cb9-4c1f-893d-50a5f9ae271e", CVar.SERVERONLY);

    public static readonly CVarDef<float> CurseDoubleChance =
        CVarDef.Create("dumont.curse.double_chance", 0.35f, CVar.SERVERONLY);
}
