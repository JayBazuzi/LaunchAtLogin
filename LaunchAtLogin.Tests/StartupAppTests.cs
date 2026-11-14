using Microsoft.Win32;

namespace LaunchAtLogin.Tests;

public class StartupAppTests : IDisposable
{
    const string AN_APP_NAME = "TestApp";
    const string AN_APP_PATH = @"C:\TestApp.exe";

    readonly string _testKeyPath = $@"Software\LaunchAtLogin.Tests.{Guid.NewGuid()}";
    readonly StartupApp _startupApp;

    public StartupAppTests()
    {
        this._startupApp = new StartupApp(AN_APP_NAME, $@"{this._testKeyPath}\Run", $@"{this._testKeyPath}\StartupApproved", TimeProvider.System);
    }

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(this._testKeyPath, throwOnMissingSubKey: false);

    [Fact]
    public void AddToStartupAppsRunsTheExeQuoted()
    {
        this._startupApp.AddToStartupApps(AN_APP_PATH);

        Assert.Equal($"\"{AN_APP_PATH}\"", this._startupApp.Command);
    }

    [Fact]
    public void RemoveFromStartupAppsRemovesIt()
    {
        this._startupApp.AddToStartupApps(AN_APP_PATH);

        this._startupApp.RemoveFromStartupApps();

        Assert.Null(this._startupApp.Command);
    }

    [Fact]
    public void RemoveFromStartupAppsWhenNeverAddedDoesNothing()
    {
        this._startupApp.RemoveFromStartupApps();

        Assert.Null(this._startupApp.Command);
    }

    [Fact]
    public void AddingToStartupAppsTurnsItOn()
    {
        this._startupApp.AddToStartupApps(AN_APP_PATH);

        Assert.True(this._startupApp.IsOn);
    }

    [Fact]
    public void DefaultsToOff()
    {
        Assert.False(this._startupApp.IsOn);
    }

    [Fact]
    public void TurnOff_KeepsItInStartupApps()
    {
        this._startupApp.AddToStartupApps(AN_APP_PATH);

        this._startupApp.TurnOff();

        Assert.False(this._startupApp.IsOn);
        Assert.NotNull(this._startupApp.Command);
    }

    [Fact]
    public void AddingToStartupAppsAgainKeepsItOff()
    {
        this._startupApp.AddToStartupApps(AN_APP_PATH);
        this._startupApp.TurnOff();

        this._startupApp.AddToStartupApps(AN_APP_PATH);

        Assert.False(this._startupApp.IsOn);
    }

    [Fact]
    public void TurnOnAfterTurnOff()
    {
        this._startupApp.AddToStartupApps(AN_APP_PATH);
        this._startupApp.TurnOff();

        this._startupApp.TurnOn(AN_APP_PATH);

        Assert.True(this._startupApp.IsOn);
    }

    [Fact]
    public void TurnOnWhenNeverAdded()
    {
        this._startupApp.TurnOn(AN_APP_PATH);

        Assert.True(this._startupApp.IsOn);
    }

    [Fact]
    public void ReaddingAfterRemovingWhileOffTurnsItOn()
    {
        this._startupApp.AddToStartupApps(AN_APP_PATH);
        this._startupApp.TurnOff();
        this._startupApp.RemoveFromStartupApps();

        this._startupApp.AddToStartupApps(AN_APP_PATH);

        Assert.True(this._startupApp.IsOn);
    }

    [Fact]
    public void TaskManagerTurnedOnIsOn()
    {
        this._startupApp.AddToStartupApps(AN_APP_PATH);
        using (var key = Registry.CurrentUser.CreateSubKey($@"{this._testKeyPath}\StartupApproved"))
        {
            key.SetValue(AN_APP_NAME, new byte[] { 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
        }

        Assert.True(this._startupApp.IsOn);
    }
}
