namespace Content.Shared._Dumont.ZLevel;

public abstract class SharedZLevelSystem : EntitySystem
{
    public static readonly string[] Directions = { "up", "down" };

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
