namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Marker interface distinguishing environment-namespaced <see cref="ISecureTokenStorage"/> access
/// from the raw, un-namespaced registration a package like <c>SyntaxCircus.Maui.TokenStorage</c>
/// registers by default. Feed this into stores whose data must not leak between environments —
/// e.g. an <c>InstallationIdentityStore</c> — instead of the plain <see cref="ISecureTokenStorage"/>.
/// </summary>
public interface IEnvironmentScopedSecureTokenStorage : ISecureTokenStorage;
