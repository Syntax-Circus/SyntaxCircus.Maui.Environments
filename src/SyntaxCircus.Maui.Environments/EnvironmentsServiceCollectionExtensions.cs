namespace SyntaxCircus.Maui.Environments;

public static class EnvironmentsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAppEnvironmentCatalog"/> (built from <paramref name="environments"/>),
    /// <see cref="IAppEnvironmentSelector"/>, <see cref="IAppEnvironmentSwitchCoordinator"/>,
    /// <see cref="IEnvironmentScopedPreferences"/>, <see cref="IEnvironmentScopedSecureTokenStorage"/>,
    /// and the platform-appropriate <see cref="IDistributionChannelService"/>, all as singletons.
    /// Also registers the raw <see cref="IPreferences"/>/<see cref="ISecureTokenStorage"/> if not
    /// already registered (idempotent — safe alongside <c>SyntaxCircus.Maui.TokenStorage</c>'s own
    /// <c>AddSecureTokenStorage()</c>/<c>AddInstallationIdentityStore()</c>).
    /// </summary>
    public static IServiceCollection AddAppEnvironments(this IServiceCollection services, IEnumerable<AppEnvironmentDescriptor> environments)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environments);

        services.TryAddSingleton(Preferences.Default);
        services.TryAddSingleton(SecureStorage.Default);
        services.TryAddSingleton<ISecureTokenStorage, SecureTokenStorage>();

        services.AddSingleton<IAppEnvironmentCatalog>(new AppEnvironmentCatalog(environments));
        services.AddSingleton<IAppEnvironmentSelector, AppEnvironmentSelector>();
        services.AddSingleton<IAppEnvironmentSwitchCoordinator, AppEnvironmentSwitchCoordinator>();
        services.AddSingleton<IEnvironmentScopedPreferences, EnvironmentPrefixingPreferences>();
        services.AddSingleton<IEnvironmentScopedSecureTokenStorage, EnvironmentPrefixingSecureTokenStorage>();

#if ANDROID
        services.AddSingleton<IDistributionChannelService, AndroidDistributionChannelService>();
#elif IOS
        services.AddSingleton<IDistributionChannelService, IosDistributionChannelService>();
#else
        services.AddSingleton<IDistributionChannelService, UnknownDistributionChannelService>();
#endif

        return services;
    }
}
