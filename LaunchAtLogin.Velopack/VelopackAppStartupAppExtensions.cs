using System.Runtime.Versioning;
using Velopack;

namespace LaunchAtLogin;

[SupportedOSPlatform("windows")]
public static class VelopackAppStartupAppExtensions
{
    /// <summary>
    /// Adds the app to the startup apps when Velopack installs it, turned on;
    /// updates its command after each update, without undoing the user turning it off;
    /// and removes it when Velopack uninstalls it.
    /// Adds to any hooks already registered, rather than replacing them.
    /// </summary>
    public static VelopackApp KeepInStartupApps(this VelopackApp velopackApp, StartupApp startupApp)
    {
        ArgumentNullException.ThrowIfNull(velopackApp);
        ArgumentNullException.ThrowIfNull(startupApp);
        return velopackApp
            .OnAfterInstallFastCallback(_ => startupApp.AddToStartupApps())
            .OnAfterUpdateFastCallback(_ => startupApp.AddToStartupApps())
            .OnBeforeUninstallFastCallback(_ => startupApp.RemoveFromStartupApps());
    }
}
