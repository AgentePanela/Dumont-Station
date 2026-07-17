using System.Linq;
using Content.Server.Administration;
using Content.Shared._Dumont.ZLevel;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._Dumont.ZLevel;

[AdminCommand(AdminFlags.Mapping)]
public sealed class ZLevelAddCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

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

        if (args.Length != 1 || !SharedZLevelSystem.TryParseDirection(args[0], out var up))
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

        if (!_entities.System<ZLevelSystem>().TryAddLevel(mapUid, up, out var newMap, out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine(Loc.GetString("zlevel-add-command-success", ("map", newMap.ToString())));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(SharedZLevelSystem.Directions, "up|down")
            : CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Mapping)]
public sealed class ZLevelAddGridCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public string Command => "zlevel_addgrid";
    public string Description => Loc.GetString("zlevel-addgrid-command-description");
    public string Help => Loc.GetString("zlevel-addgrid-command-help");

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
        if (xform.GridUid is not { } gridUid)
        {
            shell.WriteError(Loc.GetString("zlevel-command-no-grid"));
            return;
        }

        ResPath? gridPath = args.Length == 2 ? new ResPath(args[1]) : null;

        if (!_entities.System<ZLevelSystem>().TryAddGrid(gridUid, up, gridPath, out _, out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine(Loc.GetString("zlevel-addgrid-command-success", ("direction", args[0].ToLowerInvariant())));
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

[AdminCommand(AdminFlags.Mapping)]
public sealed class ZLevelSaveMapCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public string Command => "savezmap";
    public string Description => Loc.GetString("zlevel-savemap-command-description");
    public string Help => Loc.GetString("zlevel-savemap-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2 || !int.TryParse(args[0], out var id))
        {
            shell.WriteLine(Help);
            return;
        }

        if (!_entities.System<SharedMapSystem>().TryGetMap(new MapId(id), out var mapUid))
        {
            shell.WriteError(Loc.GetString("zlevel-error-invalid-mapid", ("id", args[0])));
            return;
        }

        if (!_entities.System<ZMapManifestSystem>().TrySaveZMap(mapUid.Value, new ResPath(args[1]), out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine(Loc.GetString("zlevel-savemap-command-success", ("path", args[1])));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.MapIds(_entities),
                Loc.GetString("zlevel-savemap-command-arg-mapid")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.UserFilePath(args[1], _res.UserData),
                Loc.GetString("zlevel-add-command-arg-path")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Mapping)]
public sealed class ZLevelLoadMapCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    public string Command => "loadzmap";
    public string Description => Loc.GetString("zlevel-loadmap-command-description");
    public string Help => Loc.GetString("zlevel-loadmap-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2 || !int.TryParse(args[0], out var id))
        {
            shell.WriteLine(Help);
            return;
        }

        if (!_entities.System<ZMapManifestSystem>().TryLoadZMap(new ResPath(args[1]), new MapId(id), out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine(Loc.GetString("zlevel-loadmap-command-success", ("path", args[1])));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("zlevel-savemap-command-arg-mapid")),
            2 => CompletionResult.FromHintOptions(
                CompletionHelper.UserFilePath(args[1], _res.UserData).Concat(CompletionHelper.ContentFilePath(args[1], _res)),
                Loc.GetString("zlevel-add-command-arg-path")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Mapping)]
public sealed class ZLevelInitMapCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "mapzinit";
    public string Description => Loc.GetString("zlevel-initmap-command-description");
    public string Help => Loc.GetString("zlevel-initmap-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        EntityUid stackMap;
        if (args.Length == 1)
        {
            if (!int.TryParse(args[0], out var id)
                || !_entities.System<SharedMapSystem>().TryGetMap(new MapId(id), out var mapUid))
            {
                shell.WriteError(Loc.GetString("zlevel-error-invalid-mapid", ("id", args[0])));
                return;
            }

            stackMap = mapUid.Value;
        }
        else
        {
            // no arg: init the stack you're standing in
            if (shell.Player is not { AttachedEntity: { } attached })
            {
                shell.WriteError(Loc.GetString("zlevel-command-no-entity"));
                return;
            }

            if (_entities.GetComponent<TransformComponent>(attached).MapUid is not { } cur)
            {
                shell.WriteError(Loc.GetString("zlevel-command-no-map"));
                return;
            }

            stackMap = cur;
        }

        if (!_entities.System<ZMapManifestSystem>().TryInitStack(stackMap, out var error))
        {
            shell.WriteError(error);
            return;
        }

        shell.WriteLine(Loc.GetString("zlevel-initmap-command-success"));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(CompletionHelper.MapIds(_entities),
                Loc.GetString("zlevel-savemap-command-arg-mapid"))
            : CompletionResult.Empty;
    }
}
