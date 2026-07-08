using Content.Shared._Dumont.ZLevel;
using Robust.Client.Graphics;

namespace Content.Client._Dumont.ZLevel;

public sealed class ZLevelSystem : SharedZLevelSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new ZLevelOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<ZLevelOverlay>();
    }
}
