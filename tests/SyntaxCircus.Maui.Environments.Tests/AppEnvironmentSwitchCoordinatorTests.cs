namespace SyntaxCircus.Maui.Environments.Tests;

public class AppEnvironmentSwitchCoordinatorTests
{
    private static readonly AppEnvironmentDescriptor Production = new("production", "Production", IsDefault: true);
    private static readonly AppEnvironmentDescriptor Uat = new("uat", "UAT");

    private static (AppEnvironmentSwitchCoordinator Coordinator, IAppEnvironmentSelector Selector) CreateCoordinator(string currentKey = "production")
    {
        var catalog = new AppEnvironmentCatalog([Production, Uat]);
        var selector = Substitute.For<IAppEnvironmentSelector>();
        selector.CurrentKey.Returns(currentKey);
        var coordinator = new AppEnvironmentSwitchCoordinator(catalog, selector);
        return (coordinator, selector);
    }

    [Fact]
    public async Task SwitchAsync_UnknownTargetKey_ThrowsArgumentException()
    {
        var (coordinator, _) = CreateCoordinator();

        await Should.ThrowAsync<ArgumentException>(() => coordinator.SwitchAsync("staging", (_, _) => Task.CompletedTask));
    }

    [Fact]
    public async Task SwitchAsync_NullCallback_ThrowsArgumentNullException()
    {
        var (coordinator, _) = CreateCoordinator();

        await Should.ThrowAsync<ArgumentNullException>(() => coordinator.SwitchAsync("uat", null!));
    }

    [Fact]
    public async Task SwitchAsync_AlreadyOnTarget_DoesNotInvokeCallbackOrPersist()
    {
        var (coordinator, selector) = CreateCoordinator(currentKey: "uat");
        var invoked = false;

        await coordinator.SwitchAsync("uat", (_, _) => { invoked = true; return Task.CompletedTask; }, TestContext.Current.CancellationToken);

        invoked.ShouldBeFalse();
        await selector.DidNotReceive().SelectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SwitchAsync_DifferentTarget_InvokesCallbackWithTargetKey()
    {
        var (coordinator, _) = CreateCoordinator(currentKey: "production");
        string? received = null;

        await coordinator.SwitchAsync("uat", (key, _) => { received = key; return Task.CompletedTask; }, TestContext.Current.CancellationToken);

        received.ShouldBe("uat");
    }

    [Fact]
    public async Task SwitchAsync_CallbackSucceeds_PersistsSelection()
    {
        var (coordinator, selector) = CreateCoordinator(currentKey: "production");

        await coordinator.SwitchAsync("uat", (_, _) => Task.CompletedTask, TestContext.Current.CancellationToken);

        await selector.Received(1).SelectAsync("uat", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SwitchAsync_CallbackThrows_DoesNotPersistSelectionAndPropagates()
    {
        var (coordinator, selector) = CreateCoordinator(currentKey: "production");

        await Should.ThrowAsync<InvalidOperationException>(() =>
            coordinator.SwitchAsync("uat", (_, _) => throw new InvalidOperationException("boom")));

        await selector.DidNotReceive().SelectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SwitchAsync_ConcurrentCalls_AreSerialized()
    {
        var (coordinator, selector) = CreateCoordinator(currentKey: "production");
        var gate = new SemaphoreSlim(0, 2);
        var concurrentCount = 0;
        var maxConcurrent = 0;

        async Task OnSwitching(string key, CancellationToken ct)
        {
            concurrentCount++;
            maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
            await gate.WaitAsync(ct);
            concurrentCount--;
        }

        var first = coordinator.SwitchAsync("uat", OnSwitching, TestContext.Current.CancellationToken);
        var second = coordinator.SwitchAsync("uat", OnSwitching, TestContext.Current.CancellationToken);

        gate.Release(2);
        await Task.WhenAll(first, second);

        maxConcurrent.ShouldBe(1);
    }
}
