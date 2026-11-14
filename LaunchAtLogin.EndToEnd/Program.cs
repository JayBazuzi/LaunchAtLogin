using System.Diagnostics;
using System.Globalization;
using LaunchAtLogin;
using LaunchAtLogin.EndToEnd;

// The Run key launches this exe with no arguments; people run it with "manual".
var wasLaunchedAtLogin = args.Length == 0;

const string AppName = "LaunchAtLogin.EndToEnd";
var exePath = Environment.ProcessPath ?? throw new InvalidOperationException("Can't tell this process's exe path.");
var registry = new CurrentUserRegistry();
var startupApp = new StartupApp(AppName);
var recordedSnapshots = RecordedRegistrySnapshots.Load();
var progressFile = Path.Combine(Path.GetTempPath(), $"{AppName}.progress.txt");
var progress = ReadProgress();
var shouldAcceptSnapshots = ShouldAcceptSnapshots();

try
{
    if (progress is null)
    {
        StartExpectingLaunchWhileOn();
    }
    else if (!ExplorerStartedAfter(progress.SinceUtc))
    {
        StillWaitingForLogin(progress.Stage, wasLaunchedAtLogin);
    }
    else if (progress.Stage == Stage.ExpectingLaunchWhileOn)
    {
        FinishExpectingLaunchWhileOn(wasLaunchedAtLogin);
    }
    else
    {
        FinishExpectingNoLaunchWhileOff(wasLaunchedAtLogin);
    }
}
catch (EndToEndFailureException failure)
{
    Fail(failure.Message);
}

Console.WriteLine("Press Enter to close.");
Console.ReadLine();

void StartExpectingLaunchWhileOn()
{
    startupApp.TurnOn();
    CheckSnapshot(SnapshotName.AfterTurnOn);
    ChangeThenCheckSnapshot(SnapshotName.AfterSettingsTurnedOff, () => FlipSwitchInSettings("off"));
    ChangeThenCheckSnapshot(SnapshotName.AfterSettingsTurnedOn, () => FlipSwitchInSettings("on"));
    Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true });
    ChangeThenCheckSnapshot(SnapshotName.AfterTaskManagerTurnedOff, () => AskPerson($"In Task Manager's Startup apps, right-click {AppName} and choose Disable."));
    ChangeThenCheckSnapshot(SnapshotName.AfterTaskManagerTurnedOn, () => AskPerson($"In Task Manager's Startup apps, right-click {AppName} and choose Enable."));

    WriteProgress(Stage.ExpectingLaunchWhileOn);
    Console.WriteLine($"""
        Stage 1 of 2: turned on '{startupApp.Name}' as a startup app: {startupApp.Command}

        Now log out and back in.
        This program should open by itself and say PASS.
        If it doesn't, run: dotnet run --project {AppName} -- manual
        """);
}

void FinishExpectingLaunchWhileOn(bool launchedAtLogin)
{
    if (!launchedAtLogin)
    {
        Fail("Stage 1: it didn't launch at login while turned on.");
        return;
    }

    startupApp.TurnOff();
    CheckSnapshot(SnapshotName.AfterTurnOff);
    WriteProgress(Stage.ExpectingNoLaunchWhileOff);
    Console.WriteLine($"""
        Stage 1 PASS: launched at login while turned on.

        Stage 2 of 2: turned it off, the way Task Manager does.
        Now log out and back in again.
        This program should NOT open by itself. After logging in, run:
          dotnet run --project {AppName} -- manual
        """);
}

void FinishExpectingNoLaunchWhileOff(bool launchedAtLogin)
{
    if (launchedAtLogin)
    {
        Fail("Stage 2: it launched at login while turned off.");
        return;
    }

    Cleanup();
    Console.WriteLine("PASS: launched at login while on, and didn't while off. Removed from startup apps.");
}

void StillWaitingForLogin(Stage stage, bool launchedAtLogin)
{
    Console.WriteLine($"""
        Still waiting at {stage}: you haven't logged out and back in since it started.
          Launched at login: {launchedAtLogin}
          Command:           {startupApp.Command ?? "(missing)"}
          IsOn:              {startupApp.IsOn}

        Type 'reset' to remove it from startup apps and start over, or press Enter to keep waiting.
        """);
    if (Console.ReadLine()?.Trim() == "reset")
    {
        Cleanup();
        Console.WriteLine("Reset.");
    }
}

void FlipSwitchInSettings(string onOrOff)
{
    Console.WriteLine($"Turning it {onOrOff} in Settings...");
    try
    {
        SettingsStartupAppsPage.FlipSwitch(AppName);
    }
    catch (Exception couldNotDriveSettings) when (couldNotDriveSettings is TimeoutException or InvalidOperationException or System.Runtime.InteropServices.COMException)
    {
        Console.WriteLine(couldNotDriveSettings.Message);
        AskPerson($"In Settings > Apps > Startup, turn {AppName} {onOrOff}.");
    }
}

void AskPerson(string instruction)
{
    Console.WriteLine($"{instruction} Then press Enter.");
    Console.ReadLine();
}

void ChangeThenCheckSnapshot(SnapshotName name, Action change)
{
    var before = TakeSnapshot();
    change();
    WaitForRegistryToChangeFrom(before, name);
    CheckSnapshot(name);
}

void WaitForRegistryToChangeFrom(RegistrySnapshot before, SnapshotName expectedChange)
{
    var stopwatch = Stopwatch.StartNew();
    while (TakeSnapshot() == before)
    {
        if (stopwatch.Elapsed > TimeSpan.FromSeconds(10))
        {
            throw new EndToEndFailureException($"Expected the registry to change for {expectedChange}, but it stayed {before}.");
        }

        Thread.Sleep(TimeSpan.FromMilliseconds(250));
    }
}

void CheckSnapshot(SnapshotName name)
{
    var actual = TakeSnapshot();
    if (shouldAcceptSnapshots)
    {
        recordedSnapshots.RecordAndSave(name, actual);
        Console.WriteLine($"Recorded {name}: {actual}");
    }
    else
    {
        recordedSnapshots.Verify(name, actual);
        Console.WriteLine($"Matches recording {name}: {actual}");
    }
}

RegistrySnapshot TakeSnapshot() => RegistrySnapshot.Take(registry, AppName, exePath);

void Fail(string reason)
{
    Cleanup();
    Console.WriteLine($"FAIL: {reason}{Environment.NewLine}Removed from startup apps.");
}

void Cleanup()
{
    startupApp.RemoveFromStartupApps();
    File.Delete(progressFile);
}

bool ShouldAcceptSnapshots()
{
    if (progress is null)
    {
        return args.Contains("--accept");
    }

    return progress.ShouldAcceptSnapshots;
}

void WriteProgress(Stage stage) => File.WriteAllLines(progressFile, [stage.ToString(), DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), shouldAcceptSnapshots.ToString()]);

Progress? ReadProgress()
{
    if (!File.Exists(progressFile))
    {
        return null;
    }

    var lines = File.ReadAllLines(progressFile);
    return new Progress(
        Enum.Parse<Stage>(lines[0]),
        DateTime.Parse(lines[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        bool.Parse(lines[2]));
}

static bool ExplorerStartedAfter(DateTime utcTime) => Process.GetProcessesByName("explorer").Any(explorer => explorer.StartTime.ToUniversalTime() > utcTime);

enum Stage
{
    ExpectingLaunchWhileOn,
    ExpectingNoLaunchWhileOff,
}

record Progress(Stage Stage, DateTime SinceUtc, bool ShouldAcceptSnapshots);
