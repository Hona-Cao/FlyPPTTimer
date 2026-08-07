namespace FlyPPTTimer.Forms;

/// <summary>User-initiated overlay move, raised by either the legacy or WPF timer overlay.</summary>
public sealed record OverlayMovedEventArgs(Point Location, Screen Screen);

/// <summary>User-initiated overlay size expansion request.</summary>
public sealed record OverlaySizeExpansionEventArgs(int RequiredWidth, int RequiredHeight);
