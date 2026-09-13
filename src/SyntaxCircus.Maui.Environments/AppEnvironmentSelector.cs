namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// <see cref="IAppEnvironmentSelector"/> backed by <see cref="IPreferences"/>. Takes the raw,
/// un-namespaced <see cref="IPreferences"/> deliberately — this store's own keys are the one thing
/// that must never be environment-prefixed, since they're what determines the environment in the
/// first place.
/// </summary>
public sealed class AppEnvironmentSelector(IAppEnvironmentCatalog catalog, IPreferences preferences) : IAppEnvironmentSelector
{
    private const string KeyPreferenceKey = "syntaxcircus_app_environment";
    private const string SelectedAtPreferenceKey = "syntaxcircus_app_environment_selected_at";

    public string CurrentKey
    {
        get
        {
            var stored = preferences.Get(KeyPreferenceKey, string.Empty);
            return !string.IsNullOrEmpty(stored) && catalog.Environments.Any(e => string.Equals(e.Key, stored, StringComparison.Ordinal))
                ? stored
                : catalog.Default.Key;
        }
    }

    public DateTimeOffset? SelectedAt
    {
        get
        {
            var raw = preferences.Get(SelectedAtPreferenceKey, string.Empty);
            return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : null;
        }
    }

    public bool HasExpired(TimeSpan ttl)
    {
        if (string.Equals(CurrentKey, catalog.Default.Key, StringComparison.Ordinal))
        {
            return false;
        }

        var selectedAt = SelectedAt;
        return selectedAt is null || DateTimeOffset.UtcNow > selectedAt.Value.Add(ttl);
    }

    public Task SelectAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!catalog.Environments.Any(e => string.Equals(e.Key, key, StringComparison.Ordinal)))
        {
            throw new ArgumentException($"'{key}' is not a known environment key.", nameof(key));
        }

        preferences.Set(KeyPreferenceKey, key);
        preferences.Set(SelectedAtPreferenceKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        return Task.CompletedTask;
    }

    public async Task<string> EnsureFreshAsync(TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (HasExpired(ttl))
        {
            await SelectAsync(catalog.Default.Key, cancellationToken).ConfigureAwait(false);
            return catalog.Default.Key;
        }

        return CurrentKey;
    }
}
