namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Orchestrates switching the active environment. This package has no notion of dependency
/// injection containers, HTTP clients, or Shell/Window types — the actual teardown-and-rebuild
/// work is supplied by the app as the <c>onSwitching</c> callback; this type only serializes
/// concurrent switch attempts, no-ops when already on the target, and persists the new selection
/// only after the callback succeeds.
/// </summary>
public interface IAppEnvironmentSwitchCoordinator
{
    /// <summary>
    /// Switches to <paramref name="targetKey"/>. No-ops if already selected. Concurrent calls are
    /// serialized — a second call waits for the first to finish rather than running alongside it.
    /// </summary>
    /// <param name="onSwitching">
    /// App-supplied callback invoked with <paramref name="targetKey"/> before the new selection is
    /// persisted. Should cancel in-flight work tied to the current environment, dispose
    /// session/API/cache state, rebuild configuration and DI for the target environment, and swap
    /// the visible UI (e.g. the Shell/root page) — see the package README for the full sequence.
    /// If it throws, the selection is left unchanged (the switch is treated as failed).
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="targetKey"/> is not a key in the catalog.</exception>
    Task SwitchAsync(string targetKey, Func<string, CancellationToken, Task> onSwitching, CancellationToken cancellationToken = default);
}
