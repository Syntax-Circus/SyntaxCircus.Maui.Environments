namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// <see cref="ISecureTokenStorage"/> decorator that prefixes every key with the currently selected
/// environment's key, so secrets stored through it (e.g. an installation identity/credential)
/// never leak between environments. Register <paramref name="inner"/> as the app's normal,
/// un-namespaced <see cref="ISecureTokenStorage"/>.
/// </summary>
public sealed class EnvironmentPrefixingSecureTokenStorage(IAppEnvironmentSelector selector, ISecureTokenStorage inner) : IEnvironmentScopedSecureTokenStorage
{
    public Task StoreAsync(string key, string value) => inner.StoreAsync(Prefix(key), value);

    public Task<string?> RetrieveAsync(string key) => inner.RetrieveAsync(Prefix(key));

    public Task RemoveAsync(string key) => inner.RemoveAsync(Prefix(key));

    private string Prefix(string key) => $"{selector.CurrentKey}_{key}";
}
