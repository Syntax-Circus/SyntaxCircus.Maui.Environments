namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// The fixed, compile-time-supplied set of environments an app can switch between. An app
/// implements this (or uses <see cref="AppEnvironmentCatalog"/> directly) to describe its own
/// environments — this package has no built-in notion of "Production" or "UAT" specifically.
/// </summary>
public interface IAppEnvironmentCatalog
{
    /// <summary>All environments the app supports, in no particular order.</summary>
    IReadOnlyList<AppEnvironmentDescriptor> Environments { get; }

    /// <summary>The single descriptor with <see cref="AppEnvironmentDescriptor.IsDefault"/> set.</summary>
    AppEnvironmentDescriptor DefaultEnvironment { get; }
}
