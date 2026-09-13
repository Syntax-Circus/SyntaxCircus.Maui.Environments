namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Describes one environment an app can run against (e.g. Production, UAT). <see cref="Key"/> is
/// the stable identifier persisted by <see cref="IAppEnvironmentSelector"/> and used to look the
/// descriptor back up in an <see cref="IAppEnvironmentCatalog"/> — keep it a short, stable,
/// lowercase token (e.g. <c>"production"</c>) since it's what gets written to local storage.
/// </summary>
/// <param name="Key">Stable identifier, unique within a catalog. Never shown to end users.</param>
/// <param name="DisplayName">Human-readable name for UI (e.g. a settings/environment-switch page).</param>
/// <param name="IsDefault">
/// Whether this is the environment a fresh install starts on, and the one
/// <see cref="IAppEnvironmentSelector.EnsureFreshAsync"/> reverts to once the TTL lapses. Exactly
/// one descriptor in a catalog must set this to <see langword="true"/>.
/// </param>
public sealed record AppEnvironmentDescriptor(string Key, string DisplayName, bool IsDefault = false);
