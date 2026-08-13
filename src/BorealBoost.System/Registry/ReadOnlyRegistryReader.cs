using Microsoft.Win32;

namespace BorealBoost.System.Registry;

public sealed class ReadOnlyRegistryReader
{
    public string? ReadLocalMachineString(string subKeyPath, string valueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subKeyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueName);

        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(subKeyPath, writable: false);
            return key?.GetValue(valueName) is { } value && !string.IsNullOrWhiteSpace(value.ToString())
                ? value.ToString()
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or global::System.Security.SecurityException)
        {
            return null;
        }
    }

    public int? ReadLocalMachineInt32(string subKeyPath, string valueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subKeyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueName);

        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(subKeyPath, writable: false);
            var value = key?.GetValue(valueName);

            return value switch
            {
                int number => number,
                string text when int.TryParse(text, out var parsed) => parsed,
                _ => null
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or global::System.Security.SecurityException)
        {
            return null;
        }
    }
}
