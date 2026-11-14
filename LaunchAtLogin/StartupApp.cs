using Microsoft.Win32;

namespace LaunchAtLogin;

/// <summary>
/// One app's entry in Windows' startup apps for the current user: the command in the Run key,
/// and the on/off switch that Task Manager and Settings keep in the StartupApproved key.
/// </summary>
public class StartupApp
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public const string StartupApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    const byte TurnedOffBitInFirstMarkerByte = 0x01;

    const byte TaskManagerTurnedOffFirstMarkerByte = 0x03;

    const int TaskManagerMarkerLength = 12;

    const int TaskManagerMarkerTimestampOffset = 4;

    readonly string _runKeyPath;
    readonly string _startupApprovedKeyPath;
    readonly TimeProvider _timeProvider;

    public StartupApp(string name)
        : this(name, RunKeyPath, StartupApprovedKeyPath, TimeProvider.System)
    {
    }

    internal StartupApp(string name, string runKeyPath, string startupApprovedKeyPath, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        this.Name = name;
        this._runKeyPath = runKeyPath;
        this._startupApprovedKeyPath = startupApprovedKeyPath;
        this._timeProvider = timeProvider;
    }

    public string Name { get; }

    public string? Command
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(this._runKeyPath);
            return key?.GetValue(this.Name) as string;
        }
    }

    public bool IsOn => this.Command is not null && !this.IsTurnedOffInStartupApproved();

    /// <summary>
    /// Adds the app to the startup apps, or updates its command, without changing whether the user turned it off.
    /// Call from an installer or updater.
    /// </summary>
    /// <param name="exePath">The exe to launch. Defaults to this process's exe.</param>
    public void AddToStartupApps(string? exePath = null)
    {
        exePath ??= Environment.ProcessPath ?? throw new InvalidOperationException("Can't tell this process's exe path. Pass exePath.");
        using var key = Registry.CurrentUser.CreateSubKey(this._runKeyPath);
        key.SetValue(this.Name, $"\"{exePath}\"");
    }

    /// <summary>Removes the app from the startup apps, and forgets whether it was turned off. Call from an uninstaller.</summary>
    public void RemoveFromStartupApps()
    {
        using (var runKey = Registry.CurrentUser.OpenSubKey(this._runKeyPath, writable: true))
        {
            runKey?.DeleteValue(this.Name, throwOnMissingValue: false);
        }

        this.ForgetWhetherTurnedOff();
    }

    /// <summary>Adds the app to the startup apps if needed, and turns it on. Call when the user turns it on in the app's own settings.</summary>
    /// <param name="exePath">The exe to launch. Defaults to this process's exe.</param>
    public void TurnOn(string? exePath = null)
    {
        this.AddToStartupApps(exePath);
        this.ForgetWhetherTurnedOff();
    }

    /// <summary>
    /// Turns the app off the same way Task Manager does: it stays in Task Manager's list, marked Disabled.
    /// Call when the user turns it off in the app's own settings.
    /// </summary>
    public void TurnOff()
    {
        var turnedOffMarker = new byte[TaskManagerMarkerLength];
        turnedOffMarker[0] = TaskManagerTurnedOffFirstMarkerByte;
        var turnedOffAtFileTimeUtc = this._timeProvider.GetUtcNow().UtcDateTime.ToFileTimeUtc();
        BitConverter.GetBytes(turnedOffAtFileTimeUtc).CopyTo(turnedOffMarker, TaskManagerMarkerTimestampOffset);
        using var key = Registry.CurrentUser.CreateSubKey(this._startupApprovedKeyPath);
        key.SetValue(this.Name, turnedOffMarker, RegistryValueKind.Binary);
    }



    bool IsTurnedOffInStartupApproved()
    {
        using var key = Registry.CurrentUser.OpenSubKey(this._startupApprovedKeyPath);
        return key?.GetValue(this.Name) is byte[] { Length: > 0 } marker
            && (marker[0] & TurnedOffBitInFirstMarkerByte) != 0;
    }

    void ForgetWhetherTurnedOff()
    {
        using var key = Registry.CurrentUser.OpenSubKey(this._startupApprovedKeyPath, writable: true);
        key?.DeleteValue(this.Name, throwOnMissingValue: false);
    }
}
