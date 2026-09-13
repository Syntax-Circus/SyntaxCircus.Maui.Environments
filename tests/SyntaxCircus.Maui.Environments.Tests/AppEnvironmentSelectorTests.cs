namespace SyntaxCircus.Maui.Environments.Tests;

public class AppEnvironmentSelectorTests
{
    private static readonly AppEnvironmentDescriptor Production = new("production", "Production", IsDefault: true);
    private static readonly AppEnvironmentDescriptor Uat = new("uat", "UAT");

    private static (AppEnvironmentSelector Selector, IPreferences Preferences) CreateSelector()
    {
        var catalog = new AppEnvironmentCatalog([Production, Uat]);
        var preferences = Substitute.For<IPreferences>();
        var selector = new AppEnvironmentSelector(catalog, preferences);
        return (selector, preferences);
    }

    [Fact]
    public void CurrentKey_NothingStored_ReturnsCatalogDefault()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment", string.Empty, null).Returns(string.Empty);

        selector.CurrentKey.ShouldBe("production");
    }

    [Fact]
    public void CurrentKey_StoredUnknownKey_FallsBackToDefault()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment", string.Empty, null).Returns("staging");

        selector.CurrentKey.ShouldBe("production");
    }

    [Fact]
    public void CurrentKey_StoredKnownKey_ReturnsIt()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment", string.Empty, null).Returns("uat");

        selector.CurrentKey.ShouldBe("uat");
    }

    [Fact]
    public void SelectedAt_NothingStored_ReturnsNull()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment_selected_at", string.Empty, null).Returns(string.Empty);

        selector.SelectedAt.ShouldBeNull();
    }

    [Fact]
    public void SelectedAt_StoredValue_ParsesUnixSeconds()
    {
        var (selector, preferences) = CreateSelector();
        var expected = DateTimeOffset.FromUnixTimeSeconds(2_000_000_000);
        preferences.Get("syntaxcircus_app_environment_selected_at", string.Empty, null).Returns("2000000000");

        selector.SelectedAt.ShouldBe(expected);
    }

    [Fact]
    public async Task SelectAsync_UnknownKey_ThrowsArgumentException()
    {
        var (selector, _) = CreateSelector();

        await Should.ThrowAsync<ArgumentException>(() => selector.SelectAsync("staging"));
    }

    [Fact]
    public async Task SelectAsync_WhitespaceKey_ThrowsArgumentException()
    {
        var (selector, _) = CreateSelector();

        await Should.ThrowAsync<ArgumentException>(() => selector.SelectAsync("   "));
    }

    [Fact]
    public async Task SelectAsync_KnownKey_PersistsKeyAndTimestamp()
    {
        var (selector, preferences) = CreateSelector();

        await selector.SelectAsync("uat", TestContext.Current.CancellationToken);

        preferences.Received(1).Set("syntaxcircus_app_environment", "uat", null);
        preferences.Received(1).Set("syntaxcircus_app_environment_selected_at", Arg.Any<string>(), null);
    }

    [Fact]
    public void HasExpired_CurrentlyOnDefault_AlwaysFalse()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment", string.Empty, null).Returns("production");
        preferences.Get("syntaxcircus_app_environment_selected_at", string.Empty, null).Returns(string.Empty);

        selector.HasExpired(TimeSpan.FromDays(7)).ShouldBeFalse();
    }

    [Fact]
    public void HasExpired_NonDefaultNeverExplicitlySelected_ReturnsTrue()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment", string.Empty, null).Returns("uat");
        preferences.Get("syntaxcircus_app_environment_selected_at", string.Empty, null).Returns(string.Empty);

        selector.HasExpired(TimeSpan.FromDays(7)).ShouldBeTrue();
    }

    [Fact]
    public void HasExpired_NonDefaultWithinTtl_ReturnsFalse()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment", string.Empty, null).Returns("uat");
        var selectedAt = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        preferences.Get("syntaxcircus_app_environment_selected_at", string.Empty, null).Returns(selectedAt);

        selector.HasExpired(TimeSpan.FromDays(7)).ShouldBeFalse();
    }

    [Fact]
    public void HasExpired_NonDefaultBeyondTtl_ReturnsTrue()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment", string.Empty, null).Returns("uat");
        var selectedAt = DateTimeOffset.UtcNow.AddDays(-8).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        preferences.Get("syntaxcircus_app_environment_selected_at", string.Empty, null).Returns(selectedAt);

        selector.HasExpired(TimeSpan.FromDays(7)).ShouldBeTrue();
    }

    [Fact]
    public async Task EnsureFreshAsync_NotExpired_DoesNotPersistAndReturnsCurrentKey()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment", string.Empty, null).Returns("uat");
        var selectedAt = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        preferences.Get("syntaxcircus_app_environment_selected_at", string.Empty, null).Returns(selectedAt);

        var result = await selector.EnsureFreshAsync(TimeSpan.FromDays(7), TestContext.Current.CancellationToken);

        result.ShouldBe("uat");
        preferences.DidNotReceive().Set("syntaxcircus_app_environment", Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task EnsureFreshAsync_Expired_RevertsToDefaultAndPersists()
    {
        var (selector, preferences) = CreateSelector();
        preferences.Get("syntaxcircus_app_environment", string.Empty, null).Returns("uat");
        preferences.Get("syntaxcircus_app_environment_selected_at", string.Empty, null).Returns(string.Empty);

        var result = await selector.EnsureFreshAsync(TimeSpan.FromDays(7), TestContext.Current.CancellationToken);

        result.ShouldBe("production");
        preferences.Received(1).Set("syntaxcircus_app_environment", "production", null);
    }
}
