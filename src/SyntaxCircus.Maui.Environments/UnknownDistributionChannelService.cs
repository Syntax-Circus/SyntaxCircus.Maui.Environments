namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Fallback <see cref="IDistributionChannelService"/> for targets with no platform-specific
/// detection (e.g. the <c>net10.0</c> TFM used for unit testing). Always reports
/// <see cref="DistributionChannel.Unknown"/>.
/// </summary>
public sealed class UnknownDistributionChannelService : IDistributionChannelService
{
    public DistributionChannel Current => DistributionChannel.Unknown;

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
