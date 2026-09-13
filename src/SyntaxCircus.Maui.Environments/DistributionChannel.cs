namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// How this install of the app reached the device. This is a best-effort UX/policy signal only —
/// never a security boundary and never a proxy for which backend/environment the app should use.
/// See <see cref="IDistributionChannelService"/> for the platform-specific detection caveats.
/// </summary>
public enum DistributionChannel
{
    /// <summary>Run directly from a debugger/local deploy — never a distributed build.</summary>
    DebugOrLocal,

    /// <summary>iOS TestFlight (detected via StoreKit's sandbox/xcode transaction environment).</summary>
    TestFlight,

    /// <summary>iOS App Store (detected via StoreKit's production transaction environment).</summary>
    AppStore,

    /// <summary>Installed via the Google Play Store, any track. Play does not expose which track.</summary>
    GooglePlay,

    /// <summary>Installed via an APK outside Google Play (e.g. a direct download).</summary>
    Sideloaded,

    /// <summary>Detection was unavailable, unsupported on this OS version, or inconclusive.</summary>
    Unknown,
}
