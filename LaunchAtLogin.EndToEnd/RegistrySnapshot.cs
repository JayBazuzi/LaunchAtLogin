using System.Globalization;
using Microsoft.Win32;

namespace LaunchAtLogin.EndToEnd;

/// <summary>
/// One app's values in the Run and StartupApproved keys, with the parts that change from run to run masked:
/// the exe path becomes <see cref="ExePathPlaceholder"/>, and nonzero timestamp bytes become <see cref="MaskedByte"/>.
/// </summary>
/// <param name="Run">The Run value, or null if missing.</param>
/// <param name="StartupApproved">The StartupApproved value as space-separated hex bytes, or null if missing.</param>
record RegistrySnapshot(string? Run, string? StartupApproved)
{
    public const string ExePathPlaceholder = "<exe>";

    public const string MaskedByte = "##";

    const int TimestampOffset = 4;

    const int TimestampLength = 8;

    public static RegistrySnapshot Take(IRegistry registry, string appName, string exePath)
    {
        var run = registry.GetValue(StartupApp.RunKeyPath, appName) as string;
        var startupApproved = registry.GetValue(StartupApp.StartupApprovedKeyPath, appName) as byte[];
        return new RegistrySnapshot(run?.Replace(exePath, ExePathPlaceholder, StringComparison.OrdinalIgnoreCase), MaskTimestamp(startupApproved));
    }

    /// <summary>Writes these values for the app, filling in the exe path and putting an arbitrary time in masked timestamp bytes.</summary>
    public void WriteTo(IRegistry registry, string appName, string exePath)
    {
        WriteOrDelete(registry, StartupApp.RunKeyPath, appName, this.Run?.Replace(ExePathPlaceholder, exePath, StringComparison.Ordinal), RegistryValueKind.String);
        WriteOrDelete(registry, StartupApp.StartupApprovedKeyPath, appName, UnmaskTimestamp(this.StartupApproved), RegistryValueKind.Binary);
    }

    static void WriteOrDelete(IRegistry registry, string keyPath, string valueName, object? value, RegistryValueKind kind)
    {
        if (value is null)
        {
            registry.DeleteValueIfPresent(keyPath, valueName);
        }
        else
        {
            registry.SetValue(keyPath, valueName, value, kind);
        }
    }

    static string? MaskTimestamp(byte[]? bytes)
    {
        if (bytes is null)
        {
            return null;
        }

        var hasTimestamp = bytes.Length == TimestampOffset + TimestampLength
            && bytes.Skip(TimestampOffset).Any(timestampByte => timestampByte != 0);
        var hexBytes = bytes.Select((value, index) => HexOrMasked(value, index, hasTimestamp));
        return string.Join(' ', hexBytes);
    }

    static string HexOrMasked(byte value, int index, bool hasTimestamp)
    {
        if (hasTimestamp && index >= TimestampOffset)
        {
            return MaskedByte;
        }

        return value.ToString("X2", CultureInfo.InvariantCulture);
    }

    static byte[]? UnmaskTimestamp(string? hexBytes)
    {
        if (hexBytes is null)
        {
            return null;
        }

        var anyNonzeroTimestampByte = "01";
        return [.. hexBytes.Split(' ').Select(hex => hex == MaskedByte ? anyNonzeroTimestampByte : hex).Select(hex => byte.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture))];
    }
}
