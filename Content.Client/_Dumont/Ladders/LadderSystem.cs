using Content.Shared._Dumont.Ladders;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._Dumont.Ladders;

public sealed class LadderSystem : SharedLadderSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId ArrowPrototype = "ZLevelDirectionArrow";

    private const float ProximityRange = 3f;
    private const float CheckInterval = 0.1f;

    private float _timer;

    // one arrow per structure, spawned when you get close and removed when you leave - not
    // respawned every check, so it doesn't flicker or double up
    private readonly Dictionary<EntityUid, EntityUid> _stairsArrows = [];
    private readonly Dictionary<EntityUid, EntityUid> _ladderArrows = [];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _timer += frameTime;
        if (_timer < CheckInterval)
            return;
        _timer -= CheckInterval;

        if (_player.LocalEntity is null)
            return;

        var playerCoords = Transform(_player.LocalEntity.Value).Coordinates;

        var stairsQuery = EntityQueryEnumerator<StairsComponent, TransformComponent>();
        while (stairsQuery.MoveNext(out var uid, out var stairs, out var xform))
            UpdateArrow(uid, xform.Coordinates, stairs.Up, playerCoords, _stairsArrows);

        var ladderQuery = EntityQueryEnumerator<LadderComponent, TransformComponent>();
        while (ladderQuery.MoveNext(out var uid, out var ladder, out var xform))
            UpdateArrow(uid, xform.Coordinates, ladder.Up, playerCoords, _ladderArrows);

        PruneOrphans(_stairsArrows);
        PruneOrphans(_ladderArrows);
    }

    private void UpdateArrow(EntityUid structUid, EntityCoordinates structCoords, bool up,
        EntityCoordinates playerCoords, Dictionary<EntityUid, EntityUid> arrows)
    {
        var inRange = _transform.InRange(structCoords, playerCoords, ProximityRange);
        var hasArrow = arrows.TryGetValue(structUid, out var arrow);

        if (inRange && !hasArrow)
        {
            arrow = Spawn(ArrowPrototype, structCoords);
            _transform.SetLocalRotation(arrow, up ? Angle.FromDegrees(180) : Angle.Zero);
            arrows[structUid] = arrow;
        }
        else if (!inRange && hasArrow)
        {
            Del(arrow);
            arrows.Remove(structUid);
        }
    }

    private void PruneOrphans(Dictionary<EntityUid, EntityUid> arrows)
    {
        List<EntityUid>? toRemove = null;

        foreach (var structUid in arrows.Keys)
        {
            if (!Deleted(structUid))     // catches structures that got deleted while we were tracking them
                continue;

            toRemove ??= [];
            toRemove.Add(structUid);
        }

        if (toRemove == null)
            return;

        foreach (var structUid in toRemove)
        {
            Del(arrows[structUid]);
            arrows.Remove(structUid);
        }
    }
}
