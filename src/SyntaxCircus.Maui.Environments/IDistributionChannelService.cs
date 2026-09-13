namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Best-effort detection of how this app install reached the device. Android has no supported,
/// reliable way to distinguish Play testing tracks (Internal/Closed/Open) from Production at
/// runtime — do not build product requirements or tests around Android track differentiation.
/// Never use <see cref="Current"/> to gate anything security-sensitive; it is a UX/defaulting
/// signal only.
/// </summary>
public interface IDistributionChannelService
{
    /// <summary>
    /// The detected channel, or <see cref="DistributionChannel.Unknown"/> if
    /// <see cref="InitializeAsync"/> hasn't run yet or detection was inconclusive.
    /// </summary>
    DistributionChannel Current { get; }

    /// <summary>
    /// Performs detection and caches the result in <see cref="Current"/>. Call once during app
    /// startup, before any default-environment decision depends on <see cref="Current"/>. Safe to
    /// call more than once (re-detects each time); never throws — failures resolve to
    /// <see cref="DistributionChannel.Unknown"/>.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
