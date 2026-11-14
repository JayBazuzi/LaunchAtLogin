# LaunchAtLogin

[![Build and test](https://github.com/JayBazuzi/LaunchAtLogin/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/JayBazuzi/LaunchAtLogin/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/LaunchAtLogin.svg)](https://www.nuget.org/packages/LaunchAtLogin)

A library that makes your app launch automatically when the user logs in to Windows.
It respects the user turning it off in Task Manager's Startup apps.

## How to use

Create one for your app:

<!-- snippet: create-startup-app -->
<a id='snippet-create-startup-app'></a>
```cs
var startupApp = new StartupApp(YOUR_APP_NAME);
```
<sup><a href='/LaunchAtLogin.EndToEnd/Program.cs#L10-L12' title='Snippet source file'>snippet source</a> | <a href='#snippet-create-startup-app' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Add it to the user's startup apps, turned on:

<!-- snippet: turn-on -->
<a id='snippet-turn-on'></a>
```cs
startupApp.TurnOn();
```
<sup><a href='/LaunchAtLogin.EndToEnd/Program.cs#L67-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-turn-on' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

In your installer or updater, add it or update its command, without undoing the user turning it off:

<!-- snippet: add-to-startup-apps -->
<a id='snippet-add-to-startup-apps'></a>
```cs
startupApp.AddToStartupApps(/*optional path to exe*/);
```
<sup><a href='/LaunchAtLogin.EndToEnd/Program.cs#L113-L115' title='Snippet source file'>snippet source</a> | <a href='#snippet-add-to-startup-apps' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

In your settings UI, check whether it's on:

<!-- snippet: is-on -->
<a id='snippet-is-on'></a>
```cs
var isOn = startupApp.IsOn;
//...
```
<sup><a href='/LaunchAtLogin.EndToEnd/Program.cs#L116-L119' title='Snippet source file'>snippet source</a> | <a href='#snippet-is-on' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Turn it off, the way Task Manager does:

<!-- snippet: turn-off -->
<a id='snippet-turn-off'></a>
```cs
startupApp.TurnOff();
```
<sup><a href='/LaunchAtLogin.EndToEnd/Program.cs#L95-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-turn-off' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Remove it entirely, for example in your uninstaller:

<!-- snippet: remove-from-startup-apps -->
<a id='snippet-remove-from-startup-apps'></a>
```cs
startupApp.RemoveFromStartupApps();
```
<sup><a href='/LaunchAtLogin.EndToEnd/Program.cs#L224-L226' title='Snippet source file'>snippet source</a> | <a href='#snippet-remove-from-startup-apps' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Why a new library

No existing NuGet package does this.
These were checked, by reading their source or unzipping the package, in November 2026:

| Package                                                                      | What it does                                       | Why it doesn't fit                                                                                             |
|------------------------------------------------------------------------------|----------------------------------------------------|----------------------------------------------------------------------------------------------------------------|
| WindowsAutostartApi, AutostartManager, BootMeUp, RunOnStartup, StartupHelper | Write the `Run` value or a Startup folder shortcut | Never read or write `StartupApproved`                                                                          |
| BlackSharp.Platform.Startup                                                  | Uses Task Scheduler                                | Task Manager's switch doesn't apply to scheduled tasks                                                         |
| Shiny.Core (`AppStartupInstaller`, 6.6 beta only)                            | Reads the switch correctly                         | Can't turn an app off the way Task Manager does, and pulls in dependency injection, logging and other packages |
| DevWinUI (`StartupHelper`)                                                   | Writes the switch                                  | Its "off" marker is all zeros, which Windows reads as **on**                                                   |
| ManagedShell                                                                 | Reads the switch                                   | Only to launch other apps, as a shell replacement                                                              |

A Windows desktop app that offers "launch at login" usually writes a value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
Windows also keeps a second, separate record: the on/off switch shown in Task Manager's **Startup apps** tab and in **Settings > Apps > Startup**.
That switch lives in `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run`.
When the user turns an app off there, Windows leaves the `Run` value alone and writes a binary marker under `StartupApproved\Run` instead.

Apps that only look at the `Run` value get two things wrong:

1. They show "on" in their own settings while Task Manager shows "off".
1. When their installer or updater rewrites the `Run` value, some of them also clear the switch, which turns the app back on against the user's choice.

This library handles both records together, so an app's own setting and Task Manager always agree, and the user's choice survives installs and updates.

The thing no other package does is **turn an app off exactly the way Task Manager does**, so the app still appears in Task Manager's list, marked Disabled.

## Development

1. Install the [.NET SDK](https://dotnet.microsoft.com/download).
1. Run `build-and-test.cmd`.

To check that launching at login really works, run the [end-to-end test](LaunchAtLogin.EndToEnd/README.md).
It needs you to log out and back in.

## Out of Scope for now

- **Packaged (MSIX) apps.** Windows redirects their HKCU writes into a private copy, so a Run value they write does nothing. They should use the `windows.startupTask` manifest extension and the `StartupTask` API instead.
- **All users (HKLM).** It needs administrator rights, and Windows keeps a separate switch for it.
- **32-bit apps on 64-bit Windows (`Run32`) and Startup folder shortcuts.** They use different `StartupApproved` subkeys.
- **Command-line arguments.** `AddToStartupApps` and `TurnOn` write only the quoted exe path.
  If a later version adds arguments, it needs Windows' command-line quoting rules (those of `CommandLineToArgvW`).
- **Opening the Startup apps settings page.** An app can do that itself with `Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true })`; the README can show that.

