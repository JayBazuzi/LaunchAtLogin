# LaunchAtLogin: implementation plan

This document is for the agent that builds the first version of this repo.
Read all of it before starting.
The skills in `.claude/skills/` also apply, and win if anything here disagrees with them.

## What the library is for

A Windows desktop app that offers "launch at login" usually writes a value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
Windows also keeps a second, separate record: the on/off switch shown in Task Manager's **Startup apps** tab and in **Settings > Apps > Startup**.
That switch lives in `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run`.
When the user turns an app off there, Windows leaves the `Run` value alone and writes a binary marker under `StartupApproved\Run` instead.

Apps that only look at the `Run` value get two things wrong:

1. They show "on" in their own settings while Task Manager shows "off".
1. When their installer or updater rewrites the `Run` value, some of them also clear the switch, which turns the app back on against the user's choice.

This library handles both records together, so an app's own setting and Task Manager always agree, and the user's choice survives installs and updates.

### Why a new library

No existing NuGet package does this.
These were checked, by reading their source or unzipping the package, in November 2026:

| Package | What it does | Why it doesn't fit |
|---|---|---|
| WindowsAutostartApi, AutostartManager, BootMeUp, RunOnStartup, StartupHelper | Write the `Run` value or a Startup folder shortcut | Never read or write `StartupApproved` |
| BlackSharp.Platform.Startup | Uses Task Scheduler | Task Manager's switch doesn't apply to scheduled tasks |
| Shiny.Core (`AppStartupInstaller`, 6.6 beta only) | Reads the switch correctly | Can't turn an app off the way Task Manager does, and pulls in dependency injection, logging and other packages |
| DevWinUI (`StartupHelper`) | Writes the switch | Its "off" marker is all zeros, which Windows reads as **on** |
| ManagedShell | Reads the switch | Only to launch other apps, as a shell replacement |

The thing no other package does is **turn an app off exactly the way Task Manager does**, so the app still appears in Task Manager's list, marked Disabled.
Say so in the README.

## Where the code comes from

The code already exists and is tested in the FZScout app, at `C:\Code\ResourceHunterBonusScout`:

- `FZScout/LaunchAtLogin.cs`: the class, 72 lines.
- `FZScout.Tests/LaunchAtLoginTests.cs`: 11 tests against throwaway registry keys.
- `FZScout/Program.cs` and `FZScout/TrayApplication.cs`: how the app uses it.

Start from those files.
Keep the behavior identical unless this document says otherwise, since FZScout's users already have registry values written by that code.

## Repo layout

Copy the layout and conventions of the sibling repo `C:\Code\NotificationTrayIconVisibility`, which is the same kind of small, single-class Windows package.
Read its files before creating these:

```text
.editorconfig
.gitattributes
.gitignore
.github/dependabot.yml
.github/workflows/build-and-test.yml   (build, test, publish a ci.N prerelease on push to main)
.github/workflows/release.yml          (manual: pack, push to NuGet, create GitHub release)
.github/workflows/tidy-code.yml
Directory.Build.props
Directory.Packages.props
LICENSE
LaunchAtLogin.slnx
LaunchAtLogin/LaunchAtLogin.csproj
LaunchAtLogin/PACKAGE.md
LaunchAtLogin/StartupApp.cs
LaunchAtLogin.Tests/LaunchAtLogin.Tests.csproj
LaunchAtLogin.Tests/StartupAppTests.cs
LaunchAtLogin.Tests/RegistryTableTests.cs
LaunchAtLogin.Tests/UsageExamples.cs
LaunchAtLogin.Tests/registry-table.verified.include.md
README.md
build-and-test.cmd
dotnet-tools.json
global.json
mdsnippets.json
tidy-code.cmd
```

Notes on these files:

- In the workflows, replace every `NotificationTrayIconVisibility` with `LaunchAtLogin`.
  They use the same NuGet trusted-publishing secret (`NUGET_USER`), which the repo owner sets up on GitHub.
- `global.json`: follow the dotnet-projects skill.
  SDK 11.0.100 is installed, and NotificationTrayIconVisibility pins it.
- `LICENSE`: the new-repo-setup skill says to ask which license to use.
  Ask the repo owner; NotificationTrayIconVisibility uses MIT.
- `LaunchAtLogin.csproj`: copy NotificationTrayIconVisibility's csproj, with:
  - `<TargetFramework>net10.0-windows</TargetFramework>`
  - `<PackageId>LaunchAtLogin</PackageId>`, which is free on nuget.org as of November 2026
  - `<VersionPrefix>0.1.0</VersionPrefix>`
  - `<Description>Launches a Windows app at login, and respects the user turning it off in Task Manager's Startup apps.</Description>`
  - `<PackageTags>startup;autostart;launch-at-login;run-at-login;task-manager;registry;windows</PackageTags>`
  - `<InternalsVisibleTo Include="LaunchAtLogin.Tests" />`
- The test project does **not** need `UseWPF`, unlike NotificationTrayIconVisibility's.
- The library has **no package dependencies**.
  `Microsoft.Win32.Registry` is part of `net10.0-windows`.

## Public API

Namespace `LaunchAtLogin`, one class, `StartupApp`.
The class isn't called `LaunchAtLogin` because a class with the same name as its namespace forces callers to write `LaunchAtLogin.LaunchAtLogin`.

Keep FZScout's method names, so FZScout's move to the package is a rename of the type and nothing else.

```csharp
namespace LaunchAtLogin;

/// <summary>
/// One app's entry in Windows' startup apps for the current user: the command in the Run key,
/// and the on/off switch that Task Manager and Settings keep in the StartupApproved key.
/// </summary>
public class StartupApp
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string StartupApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    /// <param name="name">The registry value name under both keys. Usually the app's name.</param>
    public StartupApp(string name);

    /// <summary>For tests: throwaway keys under HKCU, and a fixed clock for TurnOff's timestamp.</summary>
    internal StartupApp(string name, string runKeyPath, string startupApprovedKeyPath, TimeProvider timeProvider);

    public string Name { get; }

    /// <summary>The command Windows runs at login, or null if the app isn't in the startup apps.</summary>
    public string? Command { get; }

    /// <summary>Whether Windows will launch the app at login: it's in the startup apps, and not turned off.</summary>
    public bool IsOn { get; }

    /// <summary>
    /// Adds the app to the startup apps, or updates its command, without changing whether the user turned it off.
    /// Call from an installer or updater.
    /// </summary>
    public void AddToStartupApps(string? exePath = null);

    /// <summary>Removes the app from the startup apps, and forgets whether it was turned off. Call from an uninstaller.</summary>
    public void RemoveFromStartupApps();

    /// <summary>Adds the app to the startup apps if needed, and turns it on. Call when the user turns it on in the app's own settings.</summary>
    public void TurnOn(string? exePath = null);

    /// <summary>
    /// Turns the app off the same way Task Manager does: it stays in Task Manager's list, marked Disabled.
    /// Call when the user turns it off in the app's own settings.
    /// </summary>
    public void TurnOff();
}
```

Changes from FZScout's class:

1. **The value name is a constructor parameter** instead of the constant `"FZScout"`.
   Throw `ArgumentException` if it's null, empty, or whitespace.
1. **`exePath` is optional** and defaults to `Environment.ProcessPath`, the same way `TrayIconReachability.CanUserReachTrayIcon` works in NotificationTrayIconVisibility.
   If both are null, throw `InvalidOperationException` saying to pass `exePath`.
1. **The key paths are public constants**, as in NotificationTrayIconVisibility, so the README table can be generated from them.
1. **`TurnOff` takes its timestamp from a `TimeProvider`**, `TimeProvider.System` by default, so a test can check the exact bytes.
   FZScout uses `DateTime.UtcNow`.
1. **The test constructor is `internal`** and the test project gets `InternalsVisibleTo`.
   FZScout's constructor has the key paths as optional public parameters, which isn't an API to publish.

Keep the class not `sealed`, per the csharp skill.

## Behavior

All of it is in the current user's registry (HKCU), so nothing needs administrator rights.

### `Command`

Reads the string value named `Name` under the Run key.
Returns null if the key or value is missing, or if the value isn't a string.

### `IsOn`

True when both of these are true:

1. `Command` is not null.
1. The `StartupApproved` value is missing, or is a non-empty `byte[]` whose first byte has its low bit clear (`(marker[0] & 0x01) == 0`).

A missing `StartupApproved` value means "on": Windows only writes one when the switch is first used.
Values observed from Windows include `0x02` and `0x06` for on, and `0x03` and `0x09` for off; only the low bit matters.
A `StartupApproved` value that isn't a `byte[]`, or is an empty one, counts as on, the same as FZScout.

### `AddToStartupApps(exePath)`

1. Creates the Run key if needed, and sets the string value `Name` to the exe path in double quotes: `"C:\path\to\App.exe"`.
1. Leaves `StartupApproved` alone.
   This is the point: an update that calls this again must not turn the app back on.

### `RemoveFromStartupApps()`

1. Deletes the Run value, if there is one.
1. Deletes the `StartupApproved` value, if there is one.
   Otherwise a later reinstall would find a stale "off" and start turned off.

Neither missing key nor missing value is an error.

### `TurnOn(exePath)`

1. Same as `AddToStartupApps(exePath)`.
1. Deletes the `StartupApproved` value, which Windows reads as on.

Don't write an "on" marker (`0x02 …`) instead: deleting is what FZScout does, and Windows treats the two the same.

### `TurnOff()`

Creates the `StartupApproved` key if needed, and sets the value `Name` to a 12-byte `REG_BINARY`:

| Bytes | Content |
|---|---|
| 0 | `0x03` |
| 1–3 | `0x00` |
| 4–11 | the current time as a UTC FILETIME (`DateTime.ToFileTimeUtc()`), little-endian |

That's the marker Task Manager writes; the timestamp records when the app was turned off.

`TurnOff` doesn't touch the Run value, and works even if the app isn't in the startup apps yet.
That way a user who turns it off before the installer adds it still gets "off".

### Errors

Let registry exceptions (`UnauthorizedAccessException`, `SecurityException`, `IOException`) propagate.
Don't catch and hide them.

### Thread safety

None, and the XML doc comment doesn't need to say so.
Each call opens and closes its own registry keys, so it holds no state between calls.

## Out of scope for 0.1

Say these in the README's "Limitations" section, one line each:

- **Packaged (MSIX) apps.** Windows redirects their HKCU writes into a private copy, so a Run value they write does nothing. They should use the `windows.startupTask` manifest extension and the `StartupTask` API instead.
- **All users (HKLM).** It needs administrator rights, and Windows keeps a separate switch for it.
- **32-bit apps on 64-bit Windows (`Run32`) and Startup folder shortcuts.** They use different `StartupApproved` subkeys.
- **Command-line arguments.** `AddToStartupApps` and `TurnOn` write only the quoted exe path.
  If a later version adds arguments, it needs Windows' command-line quoting rules (those of `CommandLineToArgvW`).
- **Opening the Startup apps settings page.** An app can do that itself with `Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true })`; the README can show that.

## Tests

xUnit v3 and Verify, per the dotnet-projects skill.
Port FZScout's tests, keeping its approach: each test uses its own throwaway key `HKCU\Software\LaunchAtLogin.Tests.{Guid}` and deletes it in `Dispose`.
Pass `@"{testKey}\Run"` and `@"{testKey}\StartupApproved"` to the internal constructor, and a `FakeTimeProvider` (from `Microsoft.Extensions.TimeProvider.Testing`) where the timestamp matters.
Never touch the real Run or StartupApproved keys from a test.

### Ported from FZScout (rename `FZScout` to a test name like `"TestApp"`)

1. `AddToStartupAppsRunsTheExeQuoted`
1. `RemoveFromStartupAppsRemovesIt`
1. `RemoveFromStartupAppsWhenNeverAddedDoesNothing`
1. `AddingToStartupAppsTurnsItOn`
1. `NeverAddedIsOff`
1. `TurnOffKeepsItInStartupApps`
1. `AddingToStartupAppsAgainKeepsItOff`
1. `TurnOnAfterTurnOff`
1. `TurnOnWhenNeverAdded`
1. `ReaddingAfterRemovingWhileOffTurnsItOn`
1. `TaskManagerTurnedOnIsOn` (marker starting `0x02`)

### New

1. `TurnOffWritesTheSameMarkerAsTaskManager`: with a `FakeTimeProvider` at a fixed time, the value is `REG_BINARY`, exactly 12 bytes, `03 00 00 00` followed by that time's FILETIME.
   Check the bytes with Verify, so the snapshot shows them in hex.
1. `TaskManagerTurnedOffIsOff`: markers starting `0x03` and `0x09` both count as off.
1. `TaskManagerTurnedOnWithOtherFlagsIsOn`: a marker starting `0x06` counts as on.
1. `TurnOffBeforeAddingStaysOffOnceAdded`
1. `TurnOnRemovesTheMarker`: after `TurnOff` then `TurnOn`, the `StartupApproved` value is gone.
1. `RemoveFromStartupAppsForgetsTurnedOff`: after `TurnOff` then `RemoveFromStartupApps`, the `StartupApproved` value is gone.
1. `AddToStartupAppsUpdatesTheCommand`: adding with a second path replaces the first.
1. `QuotesAPathWithSpaces`: `C:\Program Files\App\App.exe` is written as `"C:\Program Files\App\App.exe"`.
1. `ExePathDefaultsToThisProcess`: with no `exePath`, `Command` is `Environment.ProcessPath` in quotes.
1. `OtherAppsAreUntouched`: values for a different name under both keys are unchanged by every method.
1. `NameIsRequired`: null, `""` and `"  "` throw `ArgumentException`.
1. `PublicKeyPathsAreTheRealOnes`: the two constants equal the paths at the top of this document, so a typo can't slip into a release.

### Docs tests

Like NotificationTrayIconVisibility:

- `RegistryTableTests` writes `registry-table.verified.include.md` with Verify: a Markdown table of what each method reads and writes, built from the public constants.
  The README includes it with `include:`.
- `UsageExamples.cs` is compiled but never run, and holds the README's code snippets between `begin-snippet` / `end-snippet` markers.

## README

Follow the markdown-files skill: one sentence per line, `1.` for every numbered item, and every code example comes from `UsageExamples.cs` through mdsnippets.
Model it on NotificationTrayIconVisibility's README.
Sections:

1. **Title and badges** (build status, NuGet), then one sentence on what it does.
1. **Why**: the two-records problem, in three or four sentences.
1. **Example**: three snippets.
   - In the installer or updater hooks, for example Velopack's `OnAfterInstallFastCallback` and `OnBeforeUninstallFastCallback`: `AddToStartupApps()` and `RemoveFromStartupApps()`.
   - A settings checkbox: initialize it from `IsOn`, and call `TurnOn()` or `TurnOff()` when it changes.
   - A button that opens Windows' Startup apps page.
1. **How it works**: the generated registry table, and the 12-byte marker.
1. **Limitations**: the out-of-scope list above.

`PACKAGE.md` is the short version shown on nuget.org: the one-line summary, the two-records problem in two sentences, and a link to the GitHub README.

Don't name the repo owner anywhere, per the no-personal-reference skill.

## Then: move FZScout to the package

Do this only after a `ci.N` prerelease of LaunchAtLogin is on nuget.org, in `C:\Code\ResourceHunterBonusScout`, as its own commit or commits, following that repo's `AGENTS.md`:

1. Add `<PackageVersion Include="LaunchAtLogin" Version="0.1.0-ci.N" />` to `Directory.Packages.props`, and a `PackageReference` to `FZScout/FZScout.csproj`.
1. In `FZScout/Program.cs` and `FZScout/TrayApplication.cs`, replace `new LaunchAtLogin()` with `new StartupApp("FZScout")`, and add `using LaunchAtLogin;`.
   The name must stay `"FZScout"`, so values already written for existing users keep working.
1. Delete `FZScout/LaunchAtLogin.cs` and `FZScout.Tests/LaunchAtLoginTests.cs`.
1. Run `./build-and-test.cmd`.

## Definition of done

1. `build-and-test.cmd` passes with no warnings.
1. `tidy-code.cmd` leaves no changes.
1. Every public member has an XML doc comment that says something the name doesn't.
1. The README's snippets and table are generated, not hand-written.
1. Pushing to `main` publishes a `0.1.0-ci.N` prerelease (this needs the NuGet secret, which the repo owner sets).
1. A manual check on a real Windows machine:
   1. Run a small console app that calls `TurnOn()`.
      Task Manager's Startup apps tab lists it as Enabled.
   1. Call `TurnOff()`.
      Task Manager lists it as Disabled.
   1. Turn it on in Task Manager, then read `IsOn`.
      It's true.
   1. Turn it off in Task Manager, then call `AddToStartupApps()`.
      It stays Disabled, and `IsOn` is false.
   1. Call `RemoveFromStartupApps()`.
      It's gone from Task Manager.
