namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// <see cref="IPreferences"/> decorator that prefixes every key with the currently selected
/// environment's key, so app-domain <c>Preferences</c> reads/writes never cross between
/// environments. Register <paramref name="inner"/> as the app's normal, un-namespaced
/// <see cref="IPreferences"/> (e.g. <c>Preferences.Default</c>) — this type wraps it rather than
/// replacing it, since <see cref="IAppEnvironmentSelector"/> itself must keep using the raw one.
/// </summary>
/// <remarks>
/// <see cref="Clear"/> intentionally throws: <see cref="IPreferences"/> has no way to enumerate or
/// selectively clear only prefixed keys, so a correct per-environment <c>Clear()</c> isn't
/// possible through this interface — delegating it to <paramref name="inner"/> would wipe every
/// environment's data at once, which is worse than not supporting it. Remove specific keys
/// individually instead.
/// </remarks>
public sealed class EnvironmentPrefixingPreferences(IAppEnvironmentSelector selector, IPreferences inner) : IEnvironmentScopedPreferences
{
    public bool ContainsKey(string key, string? sharedName) => inner.ContainsKey(Prefix(key), sharedName);

    public void Remove(string key, string? sharedName) => inner.Remove(Prefix(key), sharedName);

    public void Clear(string? sharedName) =>
        throw new NotSupportedException($"{nameof(EnvironmentPrefixingPreferences)} cannot selectively clear only the current environment's keys — remove specific keys instead.");

    public void Set<T>(string key, T value, string? sharedName) => inner.Set(Prefix(key), value, sharedName);

    public T Get<T>(string key, T defaultValue, string? sharedName) => inner.Get(Prefix(key), defaultValue, sharedName);

    private string Prefix(string key) => $"{selector.CurrentKey}_{key}";
}
