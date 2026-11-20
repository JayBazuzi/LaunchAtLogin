# LaunchAtLogin.Velopack

Keeps a [Velopack](https://velopack.io) app in Windows' startup apps across installs, updates and uninstalls, and respects the user turning it off in Task Manager.

```cs
VelopackApp.Build()
    .KeepInStartupApps(new StartupApp("MyApp"))
    .Run();
```

For more details see the [README](https://github.com/JayBazuzi/LaunchAtLogin/blob/main/README.md).
