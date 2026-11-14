using System.Diagnostics;
using System.Globalization;
using LaunchAtLogin;

// The Run key launches this exe with no arguments; people run it with "manual".
var wasLaunchedAtLogin = args.Length == 0;

var startupApp = new StartupApp("LaunchAtLogin.E2E");
var progressFile = Path.Combine(Path.GetTempPath(), "LaunchAtLogin.E2E.progress.txt");
var progress = ReadProgress();

if (progress is null)
{
    StartExpectingLaunchWhileOn();
}
else if (!ExplorerStartedAfter(progress.Value.SinceUtc))
{
    StillWaitingForLogin(progress.Value.Stage, wasLaunchedAtLogin);
}
else if (progress.Value.Stage == Stage.ExpectingLaunchWhileOn)
{
    FinishExpectingLaunchWhileOn(wasLaunchedAtLogin);
}
else
{
    FinishExpectingNoLaunchWhileOff(wasLaunchedAtLogin);
}

Console.WriteLine("Press Enter to close.");
Console.ReadLine();

void StartExpectingLaunchWhileOn()
{
    startupApp.TurnOn();
    WriteProgress(Stage.ExpectingLaunchWhileOn);
    Console.WriteLine($"""
        Stage 1 of 2: turned on '{startupApp.Name}' as a startup app: {startupApp.Command}

        Now log out and back in.
        This program should open by itself and say PASS.
        If it doesn't, run: dotnet run --project LaunchAtLogin.E2E -- manual
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
    WriteProgress(Stage.ExpectingNoLaunchWhileOff);
    Console.WriteLine("""
        Stage 1 PASS: launched at login while turned on.

        Stage 2 of 2: turned it off, the way Task Manager does.
        Now log out and back in again.
        This program should NOT open by itself. After logging in, run:
          dotnet run --project LaunchAtLogin.E2E -- manual
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

void Fail(string reason)
{
    Cleanup();
    Console.WriteLine($"FAIL: {reason} Removed from startup apps.");
}

void Cleanup()
{
    startupApp.RemoveFromStartupApps();
    File.Delete(progressFile);
}

void WriteProgress(Stage stage) =>
    File.WriteAllLines(progressFile, [stage.ToString(), DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)]);

(Stage Stage, DateTime SinceUtc)? ReadProgress()
{
    if (!File.Exists(progressFile))
    {
        return null;
    }

    var lines = File.ReadAllLines(progressFile);
    return (Enum.Parse<Stage>(lines[0]), DateTime.Parse(lines[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
}

static bool ExplorerStartedAfter(DateTime utcTime) =>
    Process.GetProcessesByName("explorer").Any(explorer => explorer.StartTime.ToUniversalTime() > utcTime);

enum Stage
{
    ExpectingLaunchWhileOn,
    ExpectingNoLaunchWhileOff,
}
