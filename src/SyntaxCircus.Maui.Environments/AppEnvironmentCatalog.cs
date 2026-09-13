namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Default <see cref="IAppEnvironmentCatalog"/> implementation: validates its descriptors once at
/// construction time (unique keys, exactly one default) so every other component in the package
/// can trust the catalog's shape without re-checking it.
/// </summary>
public sealed class AppEnvironmentCatalog : IAppEnvironmentCatalog
{
    public IReadOnlyList<AppEnvironmentDescriptor> Environments { get; }

    public AppEnvironmentDescriptor DefaultEnvironment { get; }

    public AppEnvironmentCatalog(IEnumerable<AppEnvironmentDescriptor> environments)
    {
        ArgumentNullException.ThrowIfNull(environments);

        var list = environments.ToArray();
        if (list.Length == 0)
        {
            throw new ArgumentException("At least one environment must be supplied.", nameof(environments));
        }

        if (list.Select(e => e.Key).Distinct(StringComparer.Ordinal).Count() != list.Length)
        {
            throw new ArgumentException("Environment keys must be unique.", nameof(environments));
        }

        var defaults = list.Where(e => e.IsDefault).ToArray();
        if (defaults.Length != 1)
        {
            throw new ArgumentException("Exactly one environment must have IsDefault = true.", nameof(environments));
        }

        Environments = list;
        DefaultEnvironment = defaults[0];
    }
}
