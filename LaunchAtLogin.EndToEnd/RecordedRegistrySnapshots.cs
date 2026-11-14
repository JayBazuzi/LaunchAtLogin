using System.Runtime.CompilerServices;
using System.Text.Json;

namespace LaunchAtLogin.EndToEnd;

/// <summary>
/// The registry snapshots the end-to-end test recorded on a real Windows machine, kept in the repo
/// so unit tests can check the fake registry against what Windows really stores.
/// </summary>
class RecordedRegistrySnapshots
{
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    readonly SortedDictionary<SnapshotName, RegistrySnapshot> _snapshotsByName;

    RecordedRegistrySnapshots(SortedDictionary<SnapshotName, RegistrySnapshot> snapshotsByName) => this._snapshotsByName = snapshotsByName;

    public static string FilePath { get; } = Path.Combine(DirectoryOfThisSourceFile(), "registry-snapshots.json");

    public static RecordedRegistrySnapshots Load()
    {
        if (!File.Exists(FilePath))
        {
            return new([]);
        }

        var snapshotsByName = JsonSerializer.Deserialize<SortedDictionary<SnapshotName, RegistrySnapshot>>(File.ReadAllText(FilePath), JsonOptions) ?? [];
        return new(snapshotsByName);
    }

    public RegistrySnapshot? Find(SnapshotName name) => this._snapshotsByName.GetValueOrDefault(name);

    /// <summary>Throws <see cref="SnapshotMismatchException"/> unless the recording has this snapshot, exactly.</summary>
    public void Verify(SnapshotName name, RegistrySnapshot actual)
    {
        var expected = this.Find(name);
        if (expected != actual)
        {
            throw new SnapshotMismatchException(name, expected, actual);
        }
    }

    public void RecordAndSave(SnapshotName name, RegistrySnapshot actual)
    {
        this._snapshotsByName[name] = actual;
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this._snapshotsByName, JsonOptions) + Environment.NewLine);
    }

    static string DirectoryOfThisSourceFile([CallerFilePath] string sourceFilePath = "") => Path.GetDirectoryName(sourceFilePath)!;
}
