namespace SyntaxCircus.Maui.Environments.Tests;

public class DistributionChannelDetectionTests
{
    [Fact]
    public async Task DetectAsync_NoProvider_ReturnsUnknownWithNoProviderRegistered()
    {
        var result = await DistributionChannelDetection.DetectAsync(null, TestContext.Current.CancellationToken);

        result.Channel.ShouldBe(DistributionChannel.Unknown);
        result.Reason.ShouldBe(DistributionChannelDetectionReason.NoProviderRegistered);
    }

    [Theory]
    [InlineData("Sandbox", DistributionChannel.TestFlight)]
    [InlineData("Production", DistributionChannel.AppStore)]
    [InlineData("Xcode", DistributionChannel.DebugOrLocal)]
    public async Task DetectAsync_ProviderReturnsKnownValue_MapsToChannelAndDetected(string raw, DistributionChannel expected)
    {
        var provider = Substitute.For<IAppTransactionEnvironmentProvider>();
        provider.GetEnvironmentAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(raw));

        var result = await DistributionChannelDetection.DetectAsync(provider, TestContext.Current.CancellationToken);

        result.Channel.ShouldBe(expected);
        result.Reason.ShouldBe(DistributionChannelDetectionReason.Detected);
    }

    [Fact]
    public async Task DetectAsync_ProviderReturnsNull_ReturnsUnknownWithNoSignal()
    {
        var provider = Substitute.For<IAppTransactionEnvironmentProvider>();
        provider.GetEnvironmentAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(null));

        var result = await DistributionChannelDetection.DetectAsync(provider, TestContext.Current.CancellationToken);

        result.Channel.ShouldBe(DistributionChannel.Unknown);
        result.Reason.ShouldBe(DistributionChannelDetectionReason.NoSignal);
    }

    [Fact]
    public async Task DetectAsync_TimesOutThenSucceeds_RetriesOnceAndReturnsSuccess()
    {
        var provider = Substitute.For<IAppTransactionEnvironmentProvider>();
        provider.GetEnvironmentAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<string?>(new TimeoutException()),
                Task.FromResult<string?>("Sandbox"));

        var result = await DistributionChannelDetection.DetectAsync(provider, TestContext.Current.CancellationToken);

        result.Channel.ShouldBe(DistributionChannel.TestFlight);
        result.Reason.ShouldBe(DistributionChannelDetectionReason.Detected);
        await provider.Received(2).GetEnvironmentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DetectAsync_TimesOutTwice_ReturnsUnknownWithTimeoutAfterOneRetry()
    {
        var provider = Substitute.For<IAppTransactionEnvironmentProvider>();
        provider.GetEnvironmentAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<string?>(new TimeoutException()));

        var result = await DistributionChannelDetection.DetectAsync(provider, TestContext.Current.CancellationToken);

        result.Channel.ShouldBe(DistributionChannel.Unknown);
        result.Reason.ShouldBe(DistributionChannelDetectionReason.Timeout);
        await provider.Received(2).GetEnvironmentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DetectAsync_ThrowsGenericException_ReturnsUnknownWithNativeErrorAfterOneRetry()
    {
        var provider = Substitute.For<IAppTransactionEnvironmentProvider>();
        provider.GetEnvironmentAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<string?>(new InvalidOperationException("boom")));

        var result = await DistributionChannelDetection.DetectAsync(provider, TestContext.Current.CancellationToken);

        result.Channel.ShouldBe(DistributionChannel.Unknown);
        result.Reason.ShouldBe(DistributionChannelDetectionReason.NativeError);
        await provider.Received(2).GetEnvironmentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DetectAsync_ProviderThrowsOperationCanceled_PropagatesWithoutRetrying()
    {
        var provider = Substitute.For<IAppTransactionEnvironmentProvider>();
        provider.GetEnvironmentAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<string?>(new OperationCanceledException()));

        await Should.ThrowAsync<OperationCanceledException>(
            () => DistributionChannelDetection.DetectAsync(provider, TestContext.Current.CancellationToken));

        await provider.Received(1).GetEnvironmentAsync(Arg.Any<CancellationToken>());
    }
}
