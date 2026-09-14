namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Why <see cref="IDistributionChannelService.Current"/> resolved the way it did on the last
/// <see cref="IDistributionChannelService.InitializeAsync"/> call — a diagnostic signal only, so
/// an app can surface *why* detection came back <see cref="DistributionChannel.Unknown"/> instead
/// of that being indistinguishable from "detection isn't wired up at all."
/// </summary>
public enum DistributionChannelDetectionReason
{
    /// <summary><see cref="IDistributionChannelService.InitializeAsync"/> hasn't run yet.</summary>
    NotYetChecked,

    /// <summary>This platform/service has no detection mechanism at all (e.g. the fallback service).</summary>
    NotSupported,

    /// <summary><see cref="DistributionChannel"/> was resolved successfully, including "detected as Sideloaded/GooglePlay".</summary>
    Detected,

    /// <summary>No <see cref="IAppTransactionEnvironmentProvider"/> (or platform equivalent) was registered.</summary>
    NoProviderRegistered,

    /// <summary>The underlying platform call didn't complete within the allotted time.</summary>
    Timeout,

    /// <summary>The underlying platform call threw.</summary>
    NativeError,

    /// <summary>The underlying platform call completed but returned no usable signal (e.g. unverified, or below the minimum OS version).</summary>
    NoSignal,
}
