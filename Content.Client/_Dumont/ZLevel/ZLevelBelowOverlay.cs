using Content.Client.Parallax;
using Content.Shared._Dumont.ZLevel;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Dumont.ZLevel;

/// <summary>
/// Manages the zlevel rendering, rendering putting a texture of the level between the parallax and the current level.
/// </summary>
public sealed class ZLevelOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly LightManager _light = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    private readonly ShaderInstance _unshaded;
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private static readonly Color BelowTint = new(0.55f, 0.55f, 0.6f);
    public static IClydeViewport? ActiveViewport;

    private IClydeViewport? _viewport;
    private readonly FixedEye _eye = new()
    {
        DrawFov = false, // we are peering down from above, not standing there
    };

    private MapId _belowMapId;

    public ZLevelOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
        _unshaded = _proto.Index(UnshadedShader).Instance();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport == _viewport)
            return false;

        _belowMapId = MapId.Nullspace;

        if (args.MapUid.IsValid()
            && _entManager.TryGetComponent<ZLevelMapComponent>(args.MapUid, out var zLevel)
            && zLevel.MapBelow is { } below
            && _entManager.TryGetComponent<MapComponent>(below, out var map))
        {
            _eye.DrawLight = _light.Enabled; // pls work
            _belowMapId = map.MapId;
            return true;
        }

        _eye.Position = MapCoordinates.Nullspace; // No level below
        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_belowMapId == MapId.Nullspace || args.Viewport.Eye is not { } currentEye)
            return;

        if (_viewport == null || _viewport.Size != args.Viewport.Size)
        {
            _viewport?.Dispose();
            _viewport = _clyde.CreateViewport(args.Viewport.Size, name: "z-level-below");
            _viewport.Eye = _eye;
            _viewport.ClearColor = Color.Transparent;
            _viewport.AutomaticRender = true;
            ActiveViewport = _viewport;
        }

        if (_eye.Position.MapId == _belowMapId)
        {
            var center = _eye.Position.Position + _eye.Offset;
            var aabb = Box2.CenteredAround(center,
                _viewport.Size / _viewport.RenderScale / EyeManager.PixelsPerMeter * _eye.Zoom);
            var bounds = new Box2Rotated(aabb, -_eye.Rotation, aabb.Center);

            args.WorldHandle.UseShader(_unshaded);
            args.WorldHandle.DrawTextureRect(_viewport.RenderTarget.Texture, bounds, BelowTint);
            args.WorldHandle.UseShader(null);
        }

        // mirror the player eye onto the lower map for the next frame render
        _eye.Position = new MapCoordinates(currentEye.Position.Position, _belowMapId);
        _eye.Zoom = currentEye.Zoom;
        _eye.Rotation = currentEye.Rotation;
        _eye.Offset = currentEye.Offset;
        _viewport.RenderScale = args.Viewport.RenderScale;
    }

    protected override void DisposeBehavior()
    {
        ActiveViewport = null;
        _viewport?.Dispose();
        _viewport = null;
        base.DisposeBehavior();
    }
}
