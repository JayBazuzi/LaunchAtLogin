using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace LaunchAtLogin.EndToEnd;

/// <summary>Drives the switch for one app on Settings' Startup apps page, using UI Automation.</summary>
static class SettingsStartupAppsPage
{
    static readonly TimeSpan FindTimeout = TimeSpan.FromSeconds(20);

    static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>Flips the app's switch. Throws if the page or the switch can't be found in time.</summary>
    public static void FlipSwitch(string appName)
    {
        Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true });
        using var automation = new UIA3Automation();
        var settingsWindow = WaitFor(() => automation.GetDesktop().FindFirstChild(condition => condition.ByName("Settings")), "the Settings window");
        try
        {
            var appSwitch = WaitFor(() => FindSwitch(settingsWindow, appName), $"the switch for '{appName}' in Settings");
            appSwitch.Patterns.Toggle.Pattern.Toggle();
        }
        finally
        {
            settingsWindow.AsWindow().Close();
        }
    }

    static AutomationElement? FindSwitch(AutomationElement settingsWindow, string appName) => settingsWindow.FindAllDescendants()
            .FirstOrDefault(element => element.Properties.Name.ValueOrDefault?.Contains(appName, StringComparison.OrdinalIgnoreCase) == true
                && element.Patterns.Toggle.IsSupported);

    static AutomationElement WaitFor(Func<AutomationElement?> find, string description)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < FindTimeout)
        {
            var element = find();
            if (element is not null)
            {
                return element;
            }

            Thread.Sleep(PollInterval);
        }

        throw new TimeoutException($"Couldn't find {description} within {FindTimeout.TotalSeconds} seconds.");
    }
}
