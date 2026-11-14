# End-to-end test

The unit tests use a fake registry, so they can't prove that Windows actually launches the app at login.
This test does, by having you log out and back in twice.

It checks two things:

1. A startup app that's turned on launches at login.
1. After `TurnOff()`, the way Task Manager turns an app off, it doesn't launch at login.

It isn't part of `build-and-test.cmd`, because it needs a person to log out.

## Running it

It uses your real startup apps, under the name `LaunchAtLogin.EndToEnd`.
It removes itself when it finishes, passes or fails.

1. Run `dotnet run --project LaunchAtLogin.EndToEnd -- manual`.
   It turns itself on as a startup app.
1. Log out and back in.
   It should open by itself and say `Stage 1 PASS`.
   It then turns itself off.
1. Log out and back in again.
   It should *not* open by itself.
1. Run `dotnet run --project LaunchAtLogin.EndToEnd -- manual` again.
   It should say `PASS`.

If it doesn't open at step 2, run the command yourself to see the failure.

## How it knows what happened

- **Launched at login, or by you:** the startup entry runs the exe with no arguments.
  That's why you always pass `manual`.
- **Which stage it's in:** a progress file in `%TEMP%` holds the stage and when it started.
- **Whether you've logged in since:** it checks that Explorer started after the stage did.

## If something goes wrong

Run it with `manual` before logging out to see the current stage, the startup command, and whether it's on.
Type `reset` there to remove it from your startup apps and start over.

If Explorer crashes and restarts during a stage, the test may think you've logged in.
Reset and start again.
