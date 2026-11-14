namespace LaunchAtLogin.Tests;

public class StartupAppTests
{
    const string AN_APP_NAME = "TestApp";
    const string AN_APP_PATH = @"C:\TestApp.exe";

    readonly FakeRegistry _registry = new();
    readonly StartupApp _startupApp;

    public StartupAppTests()
    {
        this._startupApp = new StartupApp(AN_APP_NAME, TimeProvider.System, this._registry);
    }

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
    public void ReadingAfterRemovingWhileOffTurnsItOn()
    {
        this._startupApp.AddToStartupApps(AN_APP_PATH);
        this._startupApp.TurnOff();
        this._startupApp.RemoveFromStartupApps();

        this._startupApp.AddToStartupApps(AN_APP_PATH);

        Assert.True(this._startupApp.IsOn);
    }
}
