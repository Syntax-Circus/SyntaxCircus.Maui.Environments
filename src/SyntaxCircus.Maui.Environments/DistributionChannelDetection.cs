namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// The retry/timeout/failure-classification logic behind <c>IosDistributionChannelService</c>,
/// pulled out of that iOS-only-compiled class so it can be unit-tested against a fake
/// <see cref="IAppTransactionEnvironmentProvider"/> without the iOS workload. One retry on
/// <see cref="DistributionChannelDetectionReason.Timeout"/>/<see cref="DistributionChannelDetectionReason.NativeError"/>
/// covers the known case of <c>AppTransaction.shared</c> being slow/unavailable in the moments right
/// after a fresh TestFlight/App Store install.
/// </summary>
public static class DistributionChannelDetection
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public static async Task<(DistributionChannel Channel, DistributionChannelDetectionReason Reason)> DetectAsync(
        IAppTransactionEnvironmentProvider? provider,
        CancellationToken cancellationToken)
    {
        if (provider is null)
        {
            return (DistributionChannel.Unknown, DistributionChannelDetectionReason.NoProviderRegistered);
        }

        var result = await DetectOnceAsync(provider, cancellationToken).ConfigureAwait(false);
        if (result.Reason is DistributionChannelDetectionReason.Timeout or DistributionChannelDetectionReason.NativeError)
        {
            await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
            result = await DetectOnceAsync(provider, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private static async Task<(DistributionChannel Channel, DistributionChannelDetectionReason Reason)> DetectOnceAsync(
        IAppTransactionEnvironmentProvider provider,
        CancellationToken cancellationToken)
    {
        string? environment;
        try
        {
            environment = await provider.GetEnvironmentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return (DistributionChannel.Unknown, DistributionChannelDetectionReason.Timeout);
        }
        catch (Exception)
        {
            return (DistributionChannel.Unknown, DistributionChannelDetectionReason.NativeError);
        }

        return environment switch
        {
            "Xcode" => (DistributionChannel.DebugOrLocal, DistributionChannelDetectionReason.Detected),
            "Sandbox" => (DistributionChannel.TestFlight, DistributionChannelDetectionReason.Detected),
            "Production" => (DistributionChannel.AppStore, DistributionChannelDetectionReason.Detected),
            _ => (DistributionChannel.Unknown, DistributionChannelDetectionReason.NoSignal),
        };
    }
}
