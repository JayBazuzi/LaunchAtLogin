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

    readonly TimeProvider _timeProvider;
    readonly IRegistry _registry;

    public StartupApp(string name)
        : this(name, TimeProvider.System, new CurrentUserRegistry())
    {
    }

    internal StartupApp(string name, TimeProvider timeProvider, IRegistry registry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        this._timeProvider = timeProvider;
        this._registry = registry;
    }

    public string Name { get; }

    public string? Command
    {
        get
        {
            return this._registry.GetValue(RunKeyPath, Name) as string;
        }
    }

    public bool IsOn => Command is not null && !IsTurnedOffInStartupApproved();

    /// <summary>
    /// Adds the app to the startup apps, or updates its command, without changing whether the user turned it off.
    /// Call from an installer or updater.
    /// </summary>
    /// <param name="exePath">The exe to launch. Defaults to this process's exe.</param>
    public void AddToStartupApps(string? exePath = null)
    {
        exePath ??= Environment.ProcessPath ?? throw new InvalidOperationException("Can't tell this process's exe path. Pass exePath.");
        this._registry.SetValue(RunKeyPath, Name, $"\"{exePath}\"", RegistryValueKind.String);
    }

    /// <summary>Removes the app from the startup apps, and forgets whether it was turned off. Call from an uninstaller.</summary>
    public void RemoveFromStartupApps()
    {
        this._registry.DeleteValueIfPresent(RunKeyPath, Name);
        ForgetWhetherTurnedOff();
    }

    /// <summary>Adds the app to the startup apps if needed, and turns it on. Call when the user turns it on in the app's own settings.</summary>
    /// <param name="exePath">The exe to launch. Defaults to this process's exe.</param>
    public void TurnOn(string? exePath = null)
    {
        AddToStartupApps(exePath);
        ForgetWhetherTurnedOff();
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
        this._registry.SetValue(StartupApprovedKeyPath, Name, turnedOffMarker, RegistryValueKind.Binary);
    }

    bool IsTurnedOffInStartupApproved()
    {
        return this._registry.GetValue(StartupApprovedKeyPath, Name) is byte[] { Length: > 0 } marker
            && (marker[0] & TurnedOffBitInFirstMarkerByte) != 0;
    }

    void ForgetWhetherTurnedOff()
    {
        this._registry.DeleteValueIfPresent(StartupApprovedKeyPath, Name);
    }
}
