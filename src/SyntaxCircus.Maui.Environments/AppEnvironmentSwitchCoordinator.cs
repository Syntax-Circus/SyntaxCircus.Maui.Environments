namespace SyntaxCircus.Maui.Environments;

/// <inheritdoc cref="IAppEnvironmentSwitchCoordinator" />
public sealed class AppEnvironmentSwitchCoordinator(IAppEnvironmentCatalog catalog, IAppEnvironmentSelector selector)
    : IAppEnvironmentSwitchCoordinator, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task SwitchAsync(string targetKey, Func<string, CancellationToken, Task> onSwitching, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetKey);
        ArgumentNullException.ThrowIfNull(onSwitching);
        if (!catalog.Environments.Any(e => string.Equals(e.Key, targetKey, StringComparison.Ordinal)))
        {
            throw new ArgumentException($"'{targetKey}' is not a known environment key.", nameof(targetKey));
        }

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (string.Equals(selector.CurrentKey, targetKey, StringComparison.Ordinal))
            {
                return;
            }

            await onSwitching(targetKey, cancellationToken).ConfigureAwait(false);
            await selector.SelectAsync(targetKey, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose() => gate.Dispose();
}
