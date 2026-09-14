namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Best-effort iOS distribution detection via StoreKit 2's <c>AppTransaction.environment</c>,
/// through an app-supplied <see cref="IAppTransactionEnvironmentProvider"/> (see that interface's
/// remarks — the underlying Swift API needs a native shim dotnet/macios doesn't provide out of the
/// box). With no provider registered, or if it returns <see langword="null"/> or throws,
/// <see cref="Current"/> stays <see cref="DistributionChannel.Unknown"/> rather than guessing.
/// </summary>
public sealed class IosDistributionChannelService(IAppTransactionEnvironmentProvider? appTransactionEnvironmentProvider = null) : IDistributionChannelService
{
    private DistributionChannel current = DistributionChannel.Unknown;
    private DistributionChannelDetectionReason lastReason = DistributionChannelDetectionReason.NotYetChecked;
    private DateTimeOffset? lastCheckedAt;

    public DistributionChannel Current => current;

    public DateTimeOffset? LastCheckedAt => lastCheckedAt;

    public DistributionChannelDetectionReason LastDetectionReason => lastReason;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        (current, lastReason) = await DistributionChannelDetection.DetectAsync(appTransactionEnvironmentProvider, cancellationToken).ConfigureAwait(false);
        lastCheckedAt = DateTimeOffset.UtcNow;
    }
}
