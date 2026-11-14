using LaunchAtLogin.EndToEnd;
using Microsoft.Extensions.Time.Testing;

namespace LaunchAtLogin.Tests;

/// <summary>Checks the fake registry against what the end-to-end test recorded Windows really storing.</summary>
public class RecordedRegistrySnapshotsTests
{
    const string AN_APP_NAME = "TestApp";
    const string AN_APP_PATH = @"C:\TestApp.exe";

    readonly RecordedRegistrySnapshots _recordedSnapshots = RecordedRegistrySnapshots.Load();
    readonly FakeRegistry _registry = new();
    readonly StartupApp _startupApp;

    public RecordedRegistrySnapshotsTests()
    {
        var aTimeWithNonzeroTimestampBytes = new FakeTimeProvider(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
        this._startupApp = new StartupApp(AN_APP_NAME, aTimeWithNonzeroTimestampBytes, this._registry);
    }

    [Fact]
    public void TurnOnWritesWhatWasRecorded()
    {
        this._startupApp.TurnOn(AN_APP_PATH);

        AssertMatchesRecording(SnapshotName.AfterTurnOn);
    }

    [Fact]
    public void TurnOffWritesWhatWasRecorded()
    {
        this._startupApp.TurnOn(AN_APP_PATH);

        this._startupApp.TurnOff();

        AssertMatchesRecording(SnapshotName.AfterTurnOff);
    }

    [Theory]
    [InlineData(SnapshotName.AfterTurnOn, true)]
    [InlineData(SnapshotName.AfterSettingsTurnedOff, false)]
    [InlineData(SnapshotName.AfterSettingsTurnedOn, true)]
    [InlineData(SnapshotName.AfterTaskManagerTurnedOff, false)]
    [InlineData(SnapshotName.AfterTaskManagerTurnedOn, true)]
    [InlineData(SnapshotName.AfterTurnOff, false)]
    public void ReadsRecordedValuesCorrectly(SnapshotName name, bool expectedIsOn)
    {
        RecordedOrSkip(name).WriteTo(this._registry, AN_APP_NAME, AN_APP_PATH);

        Assert.Equal(expectedIsOn, this._startupApp.IsOn);
    }

    void AssertMatchesRecording(SnapshotName name)
    {
        var expected = RecordedOrSkip(name);
        var actual = RegistrySnapshot.Take(this._registry, AN_APP_NAME, AN_APP_PATH);
        Assert.Equal(expected, actual);
    }

    RegistrySnapshot RecordedOrSkip(SnapshotName name)
    {
        var recorded = this._recordedSnapshots.Find(name);
        Assert.True(recorded is not null, $"{name} isn't recorded. Run the end-to-end test with --accept.");
        return recorded;
    }
}
