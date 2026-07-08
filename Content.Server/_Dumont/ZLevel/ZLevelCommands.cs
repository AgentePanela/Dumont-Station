using Content.Server.Administration;
using Content.Shared._Dumont.ZLevel;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server._Dumont.ZLevel;

[AdminCommand(AdminFlags.Fun)]
public sealed class ZLevelAddCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public string Command => "zlevel_add";
    public string Description => Loc.GetString("zlevel-add-command-description");
    public string Help => Loc.GetString("zlevel-add-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { AttachedEntity: { } attached })
        {
            shell.WriteError(Loc.GetString("zlevel-command-no-entity"));
            return;
        }

        if (args.Length is < 1 or > 2 || !SharedZLevelSystem.TryParseDirection(args[0], out var up))
        {
            shell.WriteLine(Help);
            return;
        }

        var xform = _entities.GetComponent<TransformComponent>(attached);
        if (xform.MapUid is not { } mapUid)
        {
            shell.WriteError(Loc.GetString("zlevel-command-no-map"));
            return;
        }

        ResPath? gridPath = args.Length == 2 ? new ResPath(args[1]) : null;
        var worldPos = _entities.System<SharedTransformSystem>().GetWorldPosition(attached);

        if (!_entities.System<ZLevelSystem>().TryAddLevel(mapUid, up, gridPath, xform.GridUid, worldPos, out var newMap, out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine(Loc.GetString("zlevel-add-command-success", ("map", newMap.ToString())));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(SharedZLevelSystem.Directions, "up|down"),
            2 => CompletionResult.FromHintOptions(CompletionHelper.ContentFilePath(args[1], _res),
                Loc.GetString("zlevel-add-command-arg-path")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class ZLevelMoveCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "zlevel_move";
    public string Description => Loc.GetString("zlevel-move-command-description");
    public string Help => Loc.GetString("zlevel-move-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { AttachedEntity: { } attached })
        {
            shell.WriteError(Loc.GetString("zlevel-command-no-entity"));
            return;
        }

        if (args.Length != 1 || !SharedZLevelSystem.TryParseDirection(args[0], out var up))
        {
            shell.WriteLine(Help);
            return;
        }

        if (!_entities.System<ZLevelSystem>().TryMoveZ(attached, up, out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine(Loc.GetString("zlevel-move-command-success", ("direction", args[0].ToLowerInvariant())));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(SharedZLevelSystem.Directions, "up|down")
            : CompletionResult.Empty;
    }
}
