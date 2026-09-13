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

    public DistributionChannel Current => current;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        current = await DetectAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<DistributionChannel> DetectAsync(CancellationToken cancellationToken)
    {
        if (appTransactionEnvironmentProvider is null)
        {
            return DistributionChannel.Unknown;
        }

        string? environment;
        try
        {
            environment = await appTransactionEnvironmentProvider.GetEnvironmentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return DistributionChannel.Unknown;
        }

        return environment switch
        {
            "Xcode" => DistributionChannel.DebugOrLocal,
            "Sandbox" => DistributionChannel.TestFlight,
            "Production" => DistributionChannel.AppStore,
            _ => DistributionChannel.Unknown,
        };
    }
}
