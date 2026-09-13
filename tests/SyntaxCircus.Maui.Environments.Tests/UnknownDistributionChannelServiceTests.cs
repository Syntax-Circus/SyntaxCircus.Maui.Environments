namespace SyntaxCircus.Maui.Environments.Tests;

public class UnknownDistributionChannelServiceTests
{
    [Fact]
    public void Current_AlwaysUnknown()
    {
        var service = new UnknownDistributionChannelService();

        service.Current.ShouldBe(DistributionChannel.Unknown);
    }

    [Fact]
    public async Task InitializeAsync_CompletesWithoutChangingCurrent()
    {
        var service = new UnknownDistributionChannelService();

        await service.InitializeAsync(TestContext.Current.CancellationToken);

        service.Current.ShouldBe(DistributionChannel.Unknown);
    }
}
