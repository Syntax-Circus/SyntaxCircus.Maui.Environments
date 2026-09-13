# SyntaxCircus.Maui.Environments

[![Build](https://github.com/Syntax-Circus/SyntaxCircus.Maui.Environments/actions/workflows/build.yml/badge.svg)](https://github.com/Syntax-Circus/SyntaxCircus.Maui.Environments/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/SyntaxCircus.Maui.Environments.svg)](https://www.nuget.org/packages/SyntaxCircus.Maui.Environments)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

Runtime environment switching for MAUI apps that ship **one compiled binary** across multiple backends — e.g. a single signed build promoted from TestFlight/Play testing tracks through to production, with a UAT backend reachable as a deliberately-hidden, local, opt-in switch rather than a separate build. Ships a persisted environment selector with TTL-based default reversion, a switch coordinator that serializes app-supplied teardown/rebuild logic, environment-namespacing decorators for `IPreferences`/`ISecureTokenStorage`, and best-effort app-distribution-channel detection.

> **No support guaranteed.** Published as-is and maintained on a best-effort basis. Issues and PRs are welcome, but there's no SLA — fork it or vendor what you need if that's not enough.

## Targets

`net10.0-android` and `net10.0-ios`. Depends on `Microsoft.Maui.Controls` (for `IPreferences`) and `SyntaxCircus.Maui.TokenStorage` (for `ISecureTokenStorage`).

## What this package does — and deliberately doesn't do

It owns exactly three things:
1. **Which environment is currently selected**, with a friendly TTL-based reversion to a default (see [`IAppEnvironmentSelector`](#iappenvironmentselector--appenvironmentselector)).
2. **The sequencing of a switch** — validate, no-op if unchanged, serialize concurrent attempts, invoke your teardown/rebuild logic, persist only on success (see [`IAppEnvironmentSwitchCoordinator`](#iappenvironmentswitchcoordinator--appenvironmentswitchcoordinator)).
3. **Namespacing local storage** by the current environment, so app-domain caches and secrets never leak between environments (see [storage decorators](#storage-decorators)).

It has **no opinion** about your environments' names or count (2, 3, or more — you supply the catalog), how you load per-environment configuration (embedded appsettings resources, remote config, whatever you already do), how you build your `HttpClient`s or DI container, or what your UI looks like. Those all stay app-side.

It is also **not a security boundary**. A hidden switch UI, a TTL, and namespaced local storage are convenience/hygiene mechanisms — the actual boundary has to be that your non-default backend independently authenticates and authorizes every request server-side (so a revoked/blocked credential loses access immediately, regardless of what the client thinks its environment is).

## Setup

```csharp
// MauiProgram.cs
var environments = new[]
{
    new AppEnvironmentDescriptor("production", "Production", IsDefault: true),
    new AppEnvironmentDescriptor("uat", "UAT"),
};
builder.Services.AddAppEnvironments(environments);
```

`AddAppEnvironments(environments)` registers `IAppEnvironmentCatalog`, `IAppEnvironmentSelector`, `IAppEnvironmentSwitchCoordinator`, `IEnvironmentScopedPreferences`, `IEnvironmentScopedSecureTokenStorage`, and the platform-appropriate `IDistributionChannelService`, all as singletons. It also registers the raw `IPreferences`/`ISecureTokenStorage` if not already registered — idempotent alongside `SyntaxCircus.Maui.TokenStorage`'s own `AddSecureTokenStorage()`/`AddInstallationIdentityStore()`.

### Reading the current environment before building your DI container

Because most of an app's DI graph (API base URLs, RevenueCat keys, etc.) depends on *which* environment is active, resolve that once, early, before registering anything environment-dependent:

```csharp
// MauiProgram.cs — before building the rest of the container
var preferences = Preferences.Default;
var catalog = new AppEnvironmentCatalog(environments);
var selector = new AppEnvironmentSelector(catalog, preferences);
var currentKey = await selector.EnsureFreshAsync(TimeSpan.FromDays(7)); // reverts to default if the TTL lapsed

// now use currentKey to pick which appsettings resource to load, which HttpClient
// base addresses to wire up, etc., before calling builder.Services.AddAppEnvironments(...)
```

`EnsureFreshAsync` is the TTL policy in one call: if the current selection isn't the catalog default and it's been longer than the given `TimeSpan` since it was selected (or it was never explicitly selected — e.g. auto-defaulted without going through `SelectAsync`), it reverts to the default and persists that. This does **not** delete any data — see [Storage decorators](#storage-decorators) for why switching back later restores everything untouched.

## Switching environments

```csharp
public async Task OnSwitchToUatTapped()
{
    await switchCoordinator.SwitchAsync("uat", async (targetKey, ct) =>
    {
        // 1. cancel in-flight requests / dispose the current environment's session, API clients,
        //    caches — anything holding state tied to the environment you're leaving
        // 2. rebuild configuration + DI for `targetKey` (re-run the part of your bootstrap that
        //    reads appsettings/config and constructs environment-scoped services)
        // 3. swap the visible UI — e.g. replace Application.Current's root page/Shell with a
        //    freshly built one, so no page still holds references built against the old scope
    });
}
```

`SwitchAsync`:
- No-ops if `targetKey` is already the current selection.
- Serializes concurrent calls with an internal lock — a second call waits for the first to finish.
- Throws `ArgumentException` if `targetKey` isn't in the catalog.
- Only persists the new selection (via `IAppEnvironmentSelector.SelectAsync`) **after** your callback completes without throwing — if it throws, the selection is left unchanged.

This package intentionally doesn't touch `Application.Current`, `IServiceProvider`, or `HttpClient` — MAUI has no cross-platform "restart the app" API, and self-terminating an app is against iOS App Store guidelines, so the right sequence is inherently app-specific (usually: tear down, rebuild a fresh DI scope, swap the root page — never an OS-level restart).

## Storage decorators

`IPreferences`/`ISecureTokenStorage` reads and writes go to the exact same underlying storage regardless of which environment is selected, so anything an app stores through the raw types (an installation identity, a cached press history, a pending-purchase flag) would otherwise leak between environments after a switch. These two decorators prefix every key with the current environment automatically:

```csharp
// constructor-inject IEnvironmentScopedPreferences instead of IPreferences
public sealed class OwnPressStore(IEnvironmentScopedPreferences preferences)
{
    private const string Key = "recent-own-presses";
    // preferences.Get/.Set("recent-own-presses", ...) now actually reads/writes
    // "production_recent-own-presses" or "uat_recent-own-presses" depending on
    // whichever environment is currently selected — no key-string changes needed.
}
```

```csharp
// constructor-inject IEnvironmentScopedSecureTokenStorage instead of ISecureTokenStorage
// (e.g. into SyntaxCircus.Maui.TokenStorage's InstallationIdentityStore, instead of its
// default AddInstallationIdentityStore() wiring, which always binds the raw one)
var installationIdentityStore = new InstallationIdentityStore(environmentScopedSecureTokenStorage);
```

Because switching never deletes the *other* environment's namespaced data (only the active-selection pointer changes), a device that switches back to an environment it used before — even months later — finds its prior identity, session, and cached data exactly as it left them. The only things that actually remove that data are an explicit app-level deletion flow, or the underlying storage being cleared (app data cleared, app reinstalled).

`EnvironmentPrefixingPreferences.Clear()` throws `NotSupportedException` — `IPreferences` has no way to enumerate or selectively clear only the current environment's keys, so a correct per-environment `Clear()` isn't achievable through this interface. Remove specific keys individually instead.

`IAppEnvironmentSelector` itself must keep depending on the **raw**, un-namespaced `IPreferences` — its own keys (`syntaxcircus_app_environment`, `syntaxcircus_app_environment_selected_at`) are the one piece of local state that determines the environment in the first place, so they can never be environment-scoped themselves.

## Distribution channel detection

```csharp
await distributionChannelService.InitializeAsync(); // once, during startup
if (distributionChannelService.Current == DistributionChannel.TestFlight)
{
    // e.g. default a fresh install to UAT — but still go through SelectAsync so the TTL clock
    // starts correctly (see EnsureFreshAsync above)
    await selector.SelectAsync("uat");
}
```

Android has **no supported, reliable way** to distinguish Google Play testing tracks (Internal/Closed/Open) from Production at runtime — `AndroidDistributionChannelService` can only report `GooglePlay` (any track) vs. `Sideloaded` vs. `Unknown`. Never build product requirements or tests around Android track differentiation, and never use `Current` for anything security-sensitive; it's a UX/defaulting signal only.

### iOS StoreKit `AppTransaction` detection

Apple's StoreKit 2 `AppTransaction` API (needed to detect TestFlight vs. App Store vs. Xcode-run) is **Swift-only** — as of current dotnet/macios tooling it isn't bound for direct C# calls the way Objective-C-compatible StoreKit APIs are. `IosDistributionChannelService` depends on an `IAppTransactionEnvironmentProvider` seam instead of calling StoreKit directly:

```csharp
public interface IAppTransactionEnvironmentProvider
{
    // Returns "Production", "Sandbox", "Xcode" (StoreKit's raw AppStoreEnvironment values), or
    // null if unavailable/unverified/below iOS 16. Must not throw for those expected cases.
    Task<string?> GetEnvironmentAsync(CancellationToken cancellationToken = default);
}
```

Implement this with a small native Swift shim (a static library exposing a C-callable function that `await`s `AppTransaction.shared`, verifies it, and returns `.environment.rawValue`) and register it:

```csharp
#if IOS
builder.Services.AddSingleton<IAppTransactionEnvironmentProvider, YourAppTransactionShim>();
#endif
```

With no provider registered — or if it throws or returns `null` — `IosDistributionChannelService.Current` stays `DistributionChannel.Unknown` rather than guessing. **This native shim is not included in this package** and hasn't been verified against a real Xcode/iOS build; confirm the exact Swift-interop approach against your installed Xcode/dotnet-for-iOS version before relying on the TestFlight auto-default behavior.

## Testing

Every public type here takes its dependencies through constructor injection — substitute `IAppEnvironmentCatalog`, `IPreferences`, `ISecureTokenStorage`, or `IAppEnvironmentSelector` with a test double to exercise `AppEnvironmentSelector`, `AppEnvironmentSwitchCoordinator`, or the storage decorators without touching real platform storage. The package's own test suite (`SyntaxCircus.Maui.Environments.Tests`, targeting plain `net10.0`) does exactly this — see it for worked examples with NSubstitute/Shouldly.

`AndroidDistributionChannelService`/`IosDistributionChannelService` are platform-specific (compiled only under their respective TFM via the MAUI SDK's default `Platforms/{Platform}/` globbing) and depend on live platform APIs (`Android.App.Application.Context`, a native StoreKit shim) — they aren't unit-testable from a shared test project and have no automated coverage here; verify them on-device/in-simulator.

## API reference

Every public type in the package, for quick scanning by humans and AI agents alike.

### `AppEnvironmentDescriptor`

`record AppEnvironmentDescriptor(string Key, string DisplayName, bool IsDefault = false)` — one environment. `Key` is the stable, persisted identifier (keep it short/lowercase — it's written to local storage); `DisplayName` is for UI; exactly one descriptor per catalog must set `IsDefault = true`.

### `IAppEnvironmentCatalog` / `AppEnvironmentCatalog`

| Member | Description |
|---|---|
| `IReadOnlyList<AppEnvironmentDescriptor> Environments` | All environments. |
| `AppEnvironmentDescriptor Default` | The one with `IsDefault = true`. |

`AppEnvironmentCatalog(IEnumerable<AppEnvironmentDescriptor> environments)` validates at construction: throws `ArgumentException` if the list is empty, keys aren't unique, or there isn't exactly one default.

### `IAppEnvironmentSelector` / `AppEnvironmentSelector`

`AppEnvironmentSelector(IAppEnvironmentCatalog catalog, IPreferences preferences)` — takes the **raw** `IPreferences`, never the environment-scoped decorator.

| Member | Description |
|---|---|
| `string CurrentKey` | Selected key, falling back to the catalog default if unset or unknown. |
| `DateTimeOffset? SelectedAt` | When `CurrentKey` was last explicitly set via `SelectAsync`, or `null`. |
| `bool HasExpired(TimeSpan ttl)` | `true` if not on the default and `ttl` has elapsed since `SelectedAt` (or it was never set). Always `false` while on the default. |
| `Task SelectAsync(string key, CancellationToken ct = default)` | Persists `key` with a fresh `SelectedAt`. Throws `ArgumentException` for an unknown key. |
| `Task<string> EnsureFreshAsync(TimeSpan ttl, CancellationToken ct = default)` | Reverts to the default (via `SelectAsync`) if `HasExpired(ttl)`, then returns the resulting key. Call once at startup. |

### `IAppEnvironmentSwitchCoordinator` / `AppEnvironmentSwitchCoordinator`

`AppEnvironmentSwitchCoordinator(IAppEnvironmentCatalog catalog, IAppEnvironmentSelector selector)`

| Member | Description |
|---|---|
| `Task SwitchAsync(string targetKey, Func<string, CancellationToken, Task> onSwitching, CancellationToken ct = default)` | See [Switching environments](#switching-environments). Throws `ArgumentException` for an unknown `targetKey`, `ArgumentNullException` for a null callback. |

### Storage decorators

- `IEnvironmentScopedPreferences : IPreferences` / `EnvironmentPrefixingPreferences(IAppEnvironmentSelector selector, IPreferences inner)` — prefixes every key with `"{selector.CurrentKey}_"`. `Clear()` throws `NotSupportedException` (see [above](#storage-decorators)).
- `IEnvironmentScopedSecureTokenStorage : ISecureTokenStorage` / `EnvironmentPrefixingSecureTokenStorage(IAppEnvironmentSelector selector, ISecureTokenStorage inner)` — same prefixing over `StoreAsync`/`RetrieveAsync`/`RemoveAsync`.

### Distribution channel detection

`enum DistributionChannel { DebugOrLocal, TestFlight, AppStore, GooglePlay, Sideloaded, Unknown }`

| Member | Description |
|---|---|
| `IDistributionChannelService.Current` | Detected channel, `Unknown` until `InitializeAsync` runs or if detection was inconclusive. |
| `IDistributionChannelService.InitializeAsync(CancellationToken ct = default)` | Performs detection, caches it in `Current`. Never throws. |

Implementations: `UnknownDistributionChannelService` (always `Unknown`; used on the `net10.0` TFM), `AndroidDistributionChannelService` (installer-provenance best effort), `IosDistributionChannelService` (StoreKit `AppTransaction` best effort via `IAppTransactionEnvironmentProvider`, see [above](#ios-storekit-apptransaction-detection)).

### `EnvironmentsServiceCollectionExtensions`

| Member | Description |
|---|---|
| `IServiceCollection AddAppEnvironments(IEnumerable<AppEnvironmentDescriptor> environments)` | Registers everything described in [Setup](#setup). |

## Contributing

Issues and pull requests are welcome:
- Keep changes focused, with a clear description of the behavior change.
- Match the existing code style (see `.editorconfig`).
- Call out any breaking changes to the public API in your PR description.

## License

MIT — see [LICENSE.txt](LICENSE.txt).
