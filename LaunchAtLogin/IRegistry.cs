using Microsoft.Win32;

namespace LaunchAtLogin;

/// <summary>Reads, writes, and deletes values under keys in a registry. Production uses the current user's hive; tests can substitute a fake.</summary>
interface IRegistry
{
    object? GetValue(string keyPath, string valueName);

    void SetValue(string keyPath, string valueName, object value, RegistryValueKind kind);

    void DeleteValueIfPresent(string keyPath, string valueName);
}
