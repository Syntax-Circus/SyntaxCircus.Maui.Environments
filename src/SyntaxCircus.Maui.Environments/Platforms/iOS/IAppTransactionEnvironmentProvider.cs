namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Reads StoreKit 2's <c>AppTransaction.shared.environment</c>. Apple's <c>AppTransaction</c> API
/// is Swift-only — as of current dotnet/macios tooling it is not bound for direct C# calls the way
/// Objective-C-compatible StoreKit APIs are, so surfacing it requires a small native Swift shim
/// (a static library exposing a C-callable function that returns the raw environment string, or an
/// equivalent) that the consuming app supplies and registers. See the package README's
/// "iOS StoreKit AppTransaction detection" section for a worked example.
/// </summary>
/// <remarks>
/// This interface is intentionally separate from <see cref="IDistributionChannelService"/> so that
/// <see cref="IosDistributionChannelService"/> degrades safely to
/// <see cref="DistributionChannel.Unknown"/> when no implementation is registered, rather than the
/// whole package depending on a native shim existing.
/// </remarks>
public interface IAppTransactionEnvironmentProvider
{
    /// <summary>
    /// Returns StoreKit's raw <c>AppStoreEnvironment</c> value — <c>"Production"</c>,
    /// <c>"Sandbox"</c>, or <c>"Xcode"</c> — or <see langword="null"/> if unavailable, unverified,
    /// or running below iOS 16 (where <c>AppTransaction</c> doesn't exist). Must not throw for any
    /// of those expected cases; return <see langword="null"/> instead.
    /// </summary>
    Task<string?> GetEnvironmentAsync(CancellationToken cancellationToken = default);
}
