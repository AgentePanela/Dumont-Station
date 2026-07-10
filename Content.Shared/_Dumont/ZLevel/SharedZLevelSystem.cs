namespace Content.Shared._Dumont.ZLevel;

public abstract class SharedZLevelSystem : EntitySystem
{
    public static readonly string[] Directions = { "up", "down" };

    /// <summary>
    /// Whether two maps are the same map or belong to the same vertical z-stack.
    /// </summary>
    public bool SameStack(EntityUid? mapA, EntityUid? mapB)
    {
        if (mapA == mapB)
            return true;

        return mapA != null && mapB != null
            && TryComp<ZLevelMapComponent>(mapA.Value, out var za)
            && TryComp<ZLevelMapComponent>(mapB.Value, out var zb)
            && za.BaseMap != null && za.BaseMap == zb.BaseMap;
    }

    public static bool TryParseDirection(string arg, out bool up)
    {
        up = false;
        switch (arg.ToLowerInvariant())
        {
            case "up":
                up = true;
                return true;
            case "down":
                return true;
            default:
                return false;
        }
    }
}
