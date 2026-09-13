namespace SyntaxCircus.Maui.Environments.Tests;

public class EnvironmentPrefixingSecureTokenStorageTests
{
    private static (EnvironmentPrefixingSecureTokenStorage Prefixed, ISecureTokenStorage Inner) Create(string currentKey = "uat")
    {
        var selector = Substitute.For<IAppEnvironmentSelector>();
        selector.CurrentKey.Returns(currentKey);
        var inner = Substitute.For<ISecureTokenStorage>();
        return (new EnvironmentPrefixingSecureTokenStorage(selector, inner), inner);
    }

    [Fact]
    public async Task StoreAsync_PrefixesKeyWithCurrentEnvironment()
    {
        var (prefixed, inner) = Create();

        await prefixed.StoreAsync("credential", "secret");

        await inner.Received(1).StoreAsync("uat_credential", "secret");
    }

    [Fact]
    public async Task RetrieveAsync_PrefixesKeyWithCurrentEnvironment()
    {
        var (prefixed, inner) = Create();
        inner.RetrieveAsync("uat_credential").Returns("secret");

        var result = await prefixed.RetrieveAsync("credential");

        result.ShouldBe("secret");
    }

    [Fact]
    public async Task RemoveAsync_PrefixesKeyWithCurrentEnvironment()
    {
        var (prefixed, inner) = Create();

        await prefixed.RemoveAsync("credential");

        await inner.Received(1).RemoveAsync("uat_credential");
    }

    [Fact]
    public async Task DifferentEnvironments_DoNotCollideOnTheSameKey()
    {
        var innerA = Substitute.For<ISecureTokenStorage>();
        var selectorA = Substitute.For<IAppEnvironmentSelector>();
        selectorA.CurrentKey.Returns("production");
        var prefixedA = new EnvironmentPrefixingSecureTokenStorage(selectorA, innerA);

        await prefixedA.StoreAsync("credential", "prod-secret");

        await innerA.Received(1).StoreAsync("production_credential", "prod-secret");
        await innerA.DidNotReceive().StoreAsync("uat_credential", Arg.Any<string>());
    }
}
