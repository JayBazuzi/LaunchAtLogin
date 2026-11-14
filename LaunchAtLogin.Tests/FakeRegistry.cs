using Microsoft.Win32;

namespace LaunchAtLogin.Tests;

class FakeRegistry : IRegistry
{
    readonly Dictionary<(string KeyPath, string ValueName), object> _valuesByKeyPathAndName = [];

    public object? GetValue(string keyPath, string valueName) => this._valuesByKeyPathAndName.GetValueOrDefault((keyPath, valueName));

    public void SetValue(string keyPath, string valueName, object value, RegistryValueKind kind) => this._valuesByKeyPathAndName[(keyPath, valueName)] = value;

    public void DeleteValueIfPresent(string keyPath, string valueName) => this._valuesByKeyPathAndName.Remove((keyPath, valueName));
}
