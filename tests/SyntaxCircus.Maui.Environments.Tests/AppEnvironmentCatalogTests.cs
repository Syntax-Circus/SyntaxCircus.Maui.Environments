namespace SyntaxCircus.Maui.Environments.Tests;

public class AppEnvironmentCatalogTests
{
    private static readonly AppEnvironmentDescriptor Production = new("production", "Production", IsDefault: true);
    private static readonly AppEnvironmentDescriptor Uat = new("uat", "UAT");

    [Fact]
    public void Constructor_ValidDescriptors_ExposesEnvironmentsAndDefault()
    {
        var catalog = new AppEnvironmentCatalog([Production, Uat]);

        catalog.Environments.ShouldBe([Production, Uat]);
        catalog.Default.ShouldBe(Production);
    }

    [Fact]
    public void Constructor_NullEnvironments_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new AppEnvironmentCatalog(null!));
    }

    [Fact]
    public void Constructor_EmptyEnvironments_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new AppEnvironmentCatalog([]));
    }

    [Fact]
    public void Constructor_DuplicateKeys_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new AppEnvironmentCatalog([Production, Production with { DisplayName = "Prod 2" }]));
    }

    [Fact]
    public void Constructor_NoDefault_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new AppEnvironmentCatalog([Uat]));
    }

    [Fact]
    public void Constructor_MultipleDefaults_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new AppEnvironmentCatalog([Production, Uat with { IsDefault = true }]));
    }
}
