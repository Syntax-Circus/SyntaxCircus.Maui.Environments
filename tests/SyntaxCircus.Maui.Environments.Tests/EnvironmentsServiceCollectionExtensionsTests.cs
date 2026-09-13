namespace SyntaxCircus.Maui.Environments.Tests;

public class EnvironmentsServiceCollectionExtensionsTests
{
    private static readonly AppEnvironmentDescriptor[] Environments =
    [
        new("production", "Production", IsDefault: true),
        new("uat", "UAT"),
    ];

    [Fact]
    public void AddAppEnvironments_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() => services.AddAppEnvironments(Environments));
    }

    [Fact]
    public void AddAppEnvironments_NullEnvironments_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() => services.AddAppEnvironments(null!));
    }

    [Fact]
    public void AddAppEnvironments_InvalidCatalog_ThrowsImmediately()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentException>(() => services.AddAppEnvironments([new AppEnvironmentDescriptor("only", "Only")]));
    }

    [Fact]
    public void AddAppEnvironments_RegistersCatalogSelectorCoordinatorAndScopedStores()
    {
        var services = new ServiceCollection();

        services.AddAppEnvironments(Environments);
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IAppEnvironmentCatalog>().Environments.Count.ShouldBe(2);
        provider.GetRequiredService<IAppEnvironmentSelector>().ShouldBeOfType<AppEnvironmentSelector>();
        provider.GetRequiredService<IAppEnvironmentSwitchCoordinator>().ShouldBeOfType<AppEnvironmentSwitchCoordinator>();
        provider.GetRequiredService<IEnvironmentScopedPreferences>().ShouldBeOfType<EnvironmentPrefixingPreferences>();
        provider.GetRequiredService<IEnvironmentScopedSecureTokenStorage>().ShouldBeOfType<EnvironmentPrefixingSecureTokenStorage>();
        provider.GetRequiredService<IDistributionChannelService>().ShouldBeOfType<UnknownDistributionChannelService>();
    }
}
