using Microsoft.Win32;

namespace LaunchAtLogin;

class CurrentUserRegistry : IRegistry
{
    public object? GetValue(string keyPath, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        return key?.GetValue(valueName);
    }

    public void SetValue(string keyPath, string valueName, object value, RegistryValueKind kind)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        key.SetValue(valueName, value, kind);
    }

    public void DeleteValueIfPresent(string keyPath, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }
}
