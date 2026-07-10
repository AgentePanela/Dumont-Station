using Content.Client.Parallax;
using Content.Shared._Dumont.ZLevel;
using Content.Shared._Gabystation.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _xform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    private readonly ShaderInstance _unshaded;
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private static readonly Color BelowTint = new(0.55f, 0.55f, 0.6f);
    public static readonly HashSet<IClydeViewport> ActiveViewports = new();

    private int _maxDepth; // how deep down we bother rendering, comes from the zlevel.max_depth cvar
    private const long MaxCullScanTiles = 4096; // max culling size

    // one off-screen render per visible depth
    private sealed class Layer
    {
        public readonly FixedEye Eye = new()
        {
            DrawFov = false, // we are peering down from above, not standing there
        };

        public IClydeViewport? Viewport;
        public MapId MapId = MapId.Nullspace;
    }

    private readonly List<Layer> _layers = new();
    private int _visibleLevels;
    private List<Entity<MapGridComponent>> _grids = new();

    public ZLevelOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
        _unshaded = _proto.Index(UnshadedShader).Instance();
        _map = _entManager.System<SharedMapSystem>();
        _xform = _entManager.System<SharedTransformSystem>();

        _cfg.OnValueChanged(GabyCVars.ZLevelMaxViewDepth, SetMaxDepth, true);
    }

    private void SetMaxDepth(int depth)
    {
        _maxDepth = Math.Max(0, depth);

        while (_layers.Count < _maxDepth)
            _layers.Add(new Layer());

        while (_layers.Count > _maxDepth)
        {
            DisposeLayer(_layers[^1]);
            _layers.RemoveAt(_layers.Count - 1);
        }
    }

    private static void DisposeLayer(Layer layer)
    {
        if (layer.Viewport == null)
            return;

        ActiveViewports.Remove(layer.Viewport);
        layer.Viewport.Dispose();
        layer.Viewport = null;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (ActiveViewports.Contains(args.Viewport))
            return false;

        _visibleLevels = 0;

        // bigger, so openings start rendering right before they scroll into view
        var aabb = args.WorldAABB.Enlarged(1f);
        var mapUid = args.MapUid;
        var covered = mapUid.IsValid() && ViewFullyCovered(args.MapId, aabb);

        while (_visibleLevels < _maxDepth
               && !covered
               && _entManager.TryGetComponent<ZLevelMapComponent>(mapUid, out var zLevel)
               && zLevel.MapBelow is { } below
               && _entManager.TryGetComponent<MapComponent>(below, out var map))
        {
            var layer = _layers[_visibleLevels];
            layer.MapId = map.MapId;
            layer.Eye.DrawLight = _light.Enabled; // pls work
            _visibleLevels++;

            // if the floor is covered
            covered = ViewFullyCovered(map.MapId, aabb);
            mapUid = below;
        }

        // dont render eyes that dont have a visible floor
        for (var i = _visibleLevels; i < _layers.Count; i++)
        {
            _layers[i].MapId = MapId.Nullspace;
            _layers[i].Eye.Position = MapCoordinates.Nullspace;
        }

        return _visibleLevels > 0;
    }

    private bool ViewFullyCovered(MapId mapId, Box2 aabb)
    {
        _grids.Clear();
        _mapManager.FindGridsIntersecting(mapId, aabb, ref _grids);

        foreach (var grid in _grids)
        {
            if (GridCoversBox(grid, _xform.GetInvWorldMatrix(grid.Owner).TransformBox(aabb)))
                return true;
        }

        return false;
    }

    private bool GridCoversBox(Entity<MapGridComponent> grid, Box2 localBox)
    {
        var tileSize = grid.Comp.TileSize;
        var min = new Vector2i((int) Math.Floor(localBox.Left / tileSize), (int) Math.Floor(localBox.Bottom / tileSize));
        var max = new Vector2i((int) Math.Floor(localBox.Right / tileSize), (int) Math.Floor(localBox.Top / tileSize));

        // zoomed way out the scan costs more than the render it would save
        if ((long) (max.X - min.X + 1) * (max.Y - min.Y + 1) > MaxCullScanTiles)
            return false;

        for (var x = min.X; x <= max.X; x++)
        {
            for (var y = min.Y; y <= max.Y; y++)
            {
                if (!_map.TryGetTileRef(grid.Owner, grid.Comp, new Vector2i(x, y), out var tile) || tile.Tile.IsEmpty)
                    return false;
            }
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_visibleLevels == 0 || args.Viewport.Eye is not { } currentEye)
            return;

        // deepest first, so the shallower floors composite over it
        // (clamped in case the cvar shrank the layer list since BeforeDraw)
        for (var i = Math.Min(_visibleLevels, _layers.Count) - 1; i >= 0; i--)
        {
            var layer = _layers[i];

            if (layer.Viewport == null || layer.Viewport.Size != args.Viewport.Size)
            {
                if (layer.Viewport != null)
                {
                    ActiveViewports.Remove(layer.Viewport);
                    layer.Viewport.Dispose();
                }

                layer.Viewport = _clyde.CreateViewport(args.Viewport.Size, name: $"z-level-below-{i + 1}");
                layer.Viewport.Eye = layer.Eye;
                layer.Viewport.ClearColor = Color.Transparent;
                layer.Viewport.AutomaticRender = true;
                ActiveViewports.Add(layer.Viewport);
            }

            if (layer.Eye.Position.MapId == layer.MapId)
            {
                var center = layer.Eye.Position.Position + layer.Eye.Offset;
                var aabb = Box2.CenteredAround(center,
                    layer.Viewport.Size / layer.Viewport.RenderScale / EyeManager.PixelsPerMeter * layer.Eye.Zoom);
                var bounds = new Box2Rotated(aabb, -layer.Eye.Rotation, aabb.Center);

                args.WorldHandle.UseShader(_unshaded);
                args.WorldHandle.DrawTextureRect(layer.Viewport.RenderTarget.Texture, bounds, TintForDepth(i));
                args.WorldHandle.UseShader(null);
            }

            // mirror the player eye onto this map for the next frame render
            layer.Eye.Position = new MapCoordinates(currentEye.Position.Position, layer.MapId);
            layer.Eye.Zoom = currentEye.Zoom;
            layer.Eye.Rotation = currentEye.Rotation;
            layer.Eye.Offset = currentEye.Offset;
            layer.Viewport.RenderScale = args.Viewport.RenderScale;
        }
    }

    private static Color TintForDepth(int i)
    {
        return new Color(
            MathF.Pow(BelowTint.R, i + 1),
            MathF.Pow(BelowTint.G, i + 1),
            MathF.Pow(BelowTint.B, i + 1));
    }

    protected override void DisposeBehavior()
    {
        _cfg.UnsubValueChanged(GabyCVars.ZLevelMaxViewDepth, SetMaxDepth);

        foreach (var layer in _layers)
        {
            DisposeLayer(layer);
        }

        base.DisposeBehavior();
    }
}
