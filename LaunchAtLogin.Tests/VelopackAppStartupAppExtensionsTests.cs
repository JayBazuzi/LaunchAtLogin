using System.Reflection;
using Velopack;
using static LaunchAtLogin.StartupApp;

namespace LaunchAtLogin.Tests;

public class VelopackAppStartupAppExtensionsTests
{
    const string AN_APP_NAME = "TestApp";

    static readonly SemanticVersion A_VERSION = SemanticVersion.Parse("1.2.3");

    static readonly string THIS_EXE_PATH = Environment.ProcessPath ?? throw new InvalidOperationException("Can't tell the test process's exe path.");

    readonly FakeRegistry _registry = new();
    readonly StartupApp _startupApp;
    readonly VelopackApp _velopackApp;

    public VelopackAppStartupAppExtensionsTests()
    {
        this._startupApp = new StartupApp(AN_APP_NAME, TimeProvider.System, this._registry);
        this._velopackApp = VelopackApp.Build().KeepInStartupApps(this._startupApp);
    }

    [Fact]
    public void AfterInstallAddsThisExeTurnedOn()
    {
        RunHookStoredInField("_install");

        Assert.Equal(InDoubleQuotes(THIS_EXE_PATH), this._startupApp.Command);
        Assert.True(this._startupApp.IsOn);
    }

    [Fact]
    public void AfterUpdateUpdatesTheCommand()
    {
        this._startupApp.AddToStartupApps(@"C:\OldVersion\TestApp.exe");

        RunHookStoredInField("_update");

        Assert.Equal(InDoubleQuotes(THIS_EXE_PATH), this._startupApp.Command);
    }

    [Fact]
    public void AfterUpdateKeepsItOff()
    {
        this._startupApp.AddToStartupApps(@"C:\OldVersion\TestApp.exe");
        this._startupApp.TurnOff();

        RunHookStoredInField("_update");

        Assert.False(this._startupApp.IsOn);
    }

    [Fact]
    public void BeforeUninstallRemovesIt()
    {
        this._startupApp.AddToStartupApps(@"C:\TestApp.exe");
        this._startupApp.TurnOff();

        RunHookStoredInField("_uninstall");

        Assert.Null(this._startupApp.Command);
        Assert.Null(this._registry.GetValue(StartupApprovedKeyPath, AN_APP_NAME));
    }

    [Fact]
    public void KeepsHooksAlreadyRegistered()
    {
        var wasAppsOwnHookRun = false;
        var velopackApp = VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => wasAppsOwnHookRun = true)
            .KeepInStartupApps(this._startupApp);

        RunHookStoredInField(velopackApp, "_install");

        Assert.True(wasAppsOwnHookRun);
        Assert.NotNull(this._startupApp.Command);
    }

    /// <remarks>
    /// <see cref="VelopackApp.Run"/> exits the process after running a hook, so read the hook Velopack stored instead.
    /// </remarks>
    static void RunHookStoredInField(VelopackApp velopackApp, string fieldName)
    {
        var field = typeof(VelopackApp).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Velopack no longer stores a hook in {nameof(VelopackApp)}.{fieldName}; update this test.");
        var hook = (VelopackHook?)field.GetValue(velopackApp)
            ?? throw new InvalidOperationException($"No hook registered in {nameof(VelopackApp)}.{fieldName}.");
        hook(A_VERSION);
    }

    void RunHookStoredInField(string fieldName) => RunHookStoredInField(this._velopackApp, fieldName);
}
