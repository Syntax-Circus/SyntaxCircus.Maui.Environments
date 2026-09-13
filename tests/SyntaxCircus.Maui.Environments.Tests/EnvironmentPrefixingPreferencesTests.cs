namespace SyntaxCircus.Maui.Environments.Tests;

public class EnvironmentPrefixingPreferencesTests
{
    private static (EnvironmentPrefixingPreferences Prefixed, IPreferences Inner) Create(string currentKey = "uat")
    {
        var selector = Substitute.For<IAppEnvironmentSelector>();
        selector.CurrentKey.Returns(currentKey);
        var inner = Substitute.For<IPreferences>();
        return (new EnvironmentPrefixingPreferences(selector, inner), inner);
    }

    [Fact]
    public void ContainsKey_PrefixesKeyWithCurrentEnvironment()
    {
        var (prefixed, inner) = Create();
        inner.ContainsKey("uat_setting", null).Returns(true);

        prefixed.ContainsKey("setting", null).ShouldBeTrue();
    }

    [Fact]
    public void Remove_PrefixesKeyWithCurrentEnvironment()
    {
        var (prefixed, inner) = Create();

        prefixed.Remove("setting", null);

        inner.Received(1).Remove("uat_setting", null);
    }

    [Fact]
    public void Clear_ThrowsNotSupportedException()
    {
        var (prefixed, _) = Create();

        Should.Throw<NotSupportedException>(() => prefixed.Clear(null));
    }

    [Fact]
    public void Set_PrefixesKeyWithCurrentEnvironment()
    {
        var (prefixed, inner) = Create();

        prefixed.Set("setting", "value", null);

        inner.Received(1).Set("uat_setting", "value", null);
    }

    [Fact]
    public void Get_PrefixesKeyWithCurrentEnvironment()
    {
        var (prefixed, inner) = Create();
        inner.Get("uat_setting", "fallback", null).Returns("stored");

        prefixed.Get("setting", "fallback", null).ShouldBe("stored");
    }

    [Fact]
    public void DifferentEnvironments_DoNotCollideOnTheSameKey()
    {
        var innerA = Substitute.For<IPreferences>();
        var selectorA = Substitute.For<IAppEnvironmentSelector>();
        selectorA.CurrentKey.Returns("production");
        var prefixedA = new EnvironmentPrefixingPreferences(selectorA, innerA);

        prefixedA.Set("setting", "prod-value", null);

        innerA.Received(1).Set("production_setting", "prod-value", null);
        innerA.DidNotReceive().Set("uat_setting", Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public void SharedNameIsPassedThroughUnchanged()
    {
        var (prefixed, inner) = Create();

        prefixed.Set("setting", "value", "container");

        inner.Received(1).Set("uat_setting", "value", "container");
    }
}
