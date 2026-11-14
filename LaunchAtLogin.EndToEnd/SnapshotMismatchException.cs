namespace LaunchAtLogin.EndToEnd;

class SnapshotMismatchException(SnapshotName name, RegistrySnapshot? expected, RegistrySnapshot actual)
    : EndToEndFailureException($"""
        Registry snapshot {name} doesn't match {RecordedRegistrySnapshots.FilePath}.
          Expected: {expected?.ToString() ?? "(not recorded)"}
          Actual:   {actual}
        If Windows' behavior really changed, rerun with: manual --accept
        """);
