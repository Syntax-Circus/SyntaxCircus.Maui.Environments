namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Marker interface distinguishing environment-namespaced <see cref="IPreferences"/> access from
/// the raw, un-namespaced <see cref="IPreferences"/> registration. Depend on this type (not
/// <see cref="IPreferences"/>) in any store whose data must not leak between environments — e.g.
/// cached app-domain state. <see cref="IAppEnvironmentSelector"/> itself must keep depending on
/// plain <see cref="IPreferences"/>, since its own keys are the one thing that is never
/// environment-scoped.
/// </summary>
public interface IEnvironmentScopedPreferences : IPreferences;
