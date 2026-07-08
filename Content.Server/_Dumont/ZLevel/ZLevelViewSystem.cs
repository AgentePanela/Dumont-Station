using System.Numerics;
using Content.Shared._Dumont.ZLevel;
using Content.Shared._Gabystation.CCVar;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Dumont.ZLevel;

// todo: revisar esse codigo
// Esse codigo foi feito ultilizando IA e seu objetivo é enviar via PVS apenas o que os clientes precisam

/// <summary>
/// Gives each player normal PVS range on the maps below them, using invisible proxy entities
/// that shadow the player's position. Without this the client never receives what's down there
/// and the overlay has nothing to render.
/// </summary>
public sealed class ZLevelViewSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private const float MoveTolerance = 2f; // how far the player drifts before the proxy follows
    private int _maxViewDepth = 3;

    private readonly Dictionary<ICommonSession, List<EntityUid>> _proxies = new();

    public override void Initialize()
    {
        base.Initialize();
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
        Subs.CVar(_cfg, GabyCVars.ZLevelMaxViewDepth, (v) => _maxViewDepth = v, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            ClearProxies(args.Session);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is not { } attached || Deleted(attached))
            {
                ClearProxies(session);
                continue;
            }

            UpdateProxies(session, attached);
        }
    }

    private void UpdateProxies(ICommonSession session, EntityUid attached)
    {
        var pos = _transform.GetWorldPosition(attached);
        var mapUid = Transform(attached).MapUid;
        var depth = 0;

        while (depth < _maxViewDepth
               && mapUid != null
               && TryComp<ZLevelMapComponent>(mapUid.Value, out var zLevel)
               && zLevel.MapBelow is { } below
               && !Deleted(below)
               && TryComp<MapComponent>(below, out var map))
        {
            var proxies = _proxies.GetOrNew(session);

            if (depth >= proxies.Count)
                proxies.Add(EntityUid.Invalid);

            var proxy = proxies[depth];
            var coords = new MapCoordinates(pos, map.MapId);

            if (!Exists(proxy))
            {
                proxy = Spawn(null, coords);
                _viewSubscriber.AddViewSubscriber(proxy, session);
                proxies[depth] = proxy;
            }
            else if (Transform(proxy).MapID != map.MapId
                     || (pos - _transform.GetWorldPosition(proxy)).LengthSquared() > MoveTolerance * MoveTolerance)
            {
                _transform.SetMapCoordinates(proxy, coords);
            }

            depth++;
            mapUid = below;
        }

        // drop proxies for levels that no longer exist below us
        if (_proxies.TryGetValue(session, out var list))
        {
            for (var i = list.Count - 1; i >= depth; i--)
            {
                Del(list[i]);
                list.RemoveAt(i);
            }

            if (list.Count == 0)
                _proxies.Remove(session);
        }
    }

    private void ClearProxies(ICommonSession session)
    {
        if (!_proxies.Remove(session, out var list))
            return;

        foreach (var proxy in list)
        {
            Del(proxy);
        }
    }
}
