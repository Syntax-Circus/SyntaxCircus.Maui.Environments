namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Persists and reads which <see cref="AppEnvironmentDescriptor.Key"/> the app is currently
/// running against. This is the one deliberately un-namespaced piece of local state in the whole
/// package — everything else (installation identity, session, app-domain caches) should be stored
/// under a key derived from <see cref="CurrentKey"/> instead, via
/// <see cref="EnvironmentPrefixingPreferences"/>/<see cref="EnvironmentPrefixingSecureTokenStorage"/>.
/// </summary>
public interface IAppEnvironmentSelector
{
    /// <summary>
    /// The currently selected environment's key. Falls back to the catalog's
    /// <see cref="IAppEnvironmentCatalog.Default"/> if nothing has been selected yet, or if a
    /// previously-selected key no longer exists in the catalog (e.g. an app version removed it).
    /// </summary>
    string CurrentKey { get; }

    /// <summary>
    /// When <see cref="CurrentKey"/> was last explicitly set via <see cref="SelectAsync"/>, or
    /// <see langword="null"/> if it has never been explicitly selected (fresh install, or the
    /// selection was made without going through this store).
    /// </summary>
    DateTimeOffset? SelectedAt { get; }

    /// <summary>
    /// <see langword="true"/> when the current selection is not the catalog default and
    /// <paramref name="ttl"/> has elapsed since it was selected (or it was never explicitly
    /// selected at all). Always <see langword="false"/> while already on the default environment.
    /// </summary>
    bool HasExpired(TimeSpan ttl);

    /// <summary>Persists <paramref name="key"/> as the current selection, with a fresh <see cref="SelectedAt"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> is not a key in the catalog.</exception>
    Task SelectAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverts to the catalog default (via <see cref="SelectAsync"/>) if <see cref="HasExpired"/>
    /// is true for <paramref name="ttl"/>, then returns the resulting <see cref="CurrentKey"/>.
    /// Call this once during app startup, before building anything environment-scoped.
    /// </summary>
    Task<string> EnsureFreshAsync(TimeSpan ttl, CancellationToken cancellationToken = default);
}
