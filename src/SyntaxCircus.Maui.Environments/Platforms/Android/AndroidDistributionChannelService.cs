using Android.Content.PM;
using AndroidApplication = Android.App.Application;

namespace SyntaxCircus.Maui.Environments;

/// <summary>
/// Best-effort Android distribution detection via installer-package provenance. This is
/// explicitly <b>not</b> able to distinguish Google Play testing tracks (Internal/Closed/Open)
/// from Production — Android exposes no supported, reliable runtime API for that. It can only
/// tell "installed via Google Play" from "installed some other way," and even that degrades to
/// <see cref="DistributionChannel.Unknown"/> wherever the platform doesn't report it. Detection is
/// synchronous under the hood; <see cref="InitializeAsync"/> just forces it and completes.
/// </summary>
public sealed class AndroidDistributionChannelService : IDistributionChannelService
{
    private const string GooglePlayInstallerPackageName = "com.android.vending";

    private DistributionChannel current = DistributionChannel.Unknown;
    private DateTimeOffset? lastCheckedAt;

    public DistributionChannel Current => current;

    public DateTimeOffset? LastCheckedAt => lastCheckedAt;

    public DistributionChannelDetectionReason LastDetectionReason => lastCheckedAt is null
        ? DistributionChannelDetectionReason.NotYetChecked
        : current == DistributionChannel.Unknown
            ? DistributionChannelDetectionReason.NoSignal
            : DistributionChannelDetectionReason.Detected;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        current = Detect();
        lastCheckedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    private static DistributionChannel Detect()
    {
        try
        {
            var context = AndroidApplication.Context;
            var packageManager = context.PackageManager;
            var packageName = context.PackageName;
            if (packageManager is null || packageName is null)
            {
                return DistributionChannel.Unknown;
            }

            string? installerPackageName;
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                installerPackageName = packageManager.GetInstallSourceInfo(packageName)?.InstallingPackageName;
            }
            else
            {
#pragma warning disable CA1422 // deprecated below API 30 — this branch only runs below API 30
                installerPackageName = packageManager.GetInstallerPackageName(packageName);
#pragma warning restore CA1422
            }

            return installerPackageName switch
            {
                GooglePlayInstallerPackageName => DistributionChannel.GooglePlay,
                null or "" => DistributionChannel.Sideloaded,
                _ => DistributionChannel.Unknown,
            };
        }
        catch (Exception ex) when (ex is Java.Lang.Exception or InvalidOperationException)
        {
            return DistributionChannel.Unknown;
        }
    }
}
