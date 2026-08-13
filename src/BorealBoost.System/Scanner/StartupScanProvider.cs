using Microsoft.Win32;
using BorealBoost.Core.Scanner;

namespace BorealBoost.System.Scanner;

public sealed class StartupScanProvider : ISystemScanProvider
{
    private static readonly string[] StartupRegistryPaths =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
    ];

    public string Name => "Startup";

    public int Weight => 6;

    public TimeSpan Timeout => TimeSpan.FromSeconds(5);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var startupItems = new List<StartupItemSnapshot>();
            var warnings = new List<ScanIssue>();
            foreach (var path in StartupRegistryPaths)
            {
                ReadStartupNames(RegistryHive.CurrentUser, RegistryView.Registry64, path, "HKCU64", startupItems, warnings);
                ReadStartupNames(RegistryHive.LocalMachine, RegistryView.Registry64, path, "HKLM64", startupItems, warnings);
                ReadStartupNames(RegistryHive.CurrentUser, RegistryView.Registry32, path, "HKCU32", startupItems, warnings);
                ReadStartupNames(RegistryHive.LocalMachine, RegistryView.Registry32, path, "HKLM32", startupItems, warnings);
            }

            ReadStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "UserStartupFolder", startupItems, warnings);
            ReadStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "CommonStartupFolder", startupItems, warnings);

            var duration = DateTimeOffset.UtcNow - startedAt;
            var patch = new SystemSnapshotPatch(StartupItems: startupItems);
            return Task.FromResult(warnings.Count == 0
                ? new ProviderScanResult(ProviderResult.Succeeded(Name, DataSourceKind.RegistryReadOnly, duration), patch)
                : new ProviderScanResult(ProviderResult.Partial(Name, DataSourceKind.RegistryReadOnly, duration, warnings), patch));
        }
        catch (OperationCanceledException)
        {
            var duration = DateTimeOffset.UtcNow - startedAt;
            return Task.FromResult(ProviderScanResult.Empty(ProviderResult.Canceled(Name, duration)));
        }
    }

    private void ReadStartupNames(
        RegistryHive hive,
        RegistryView view,
        string subKeyPath,
        string locationPrefix,
        ICollection<StartupItemSnapshot> destination,
        ICollection<ScanIssue> warnings)
    {
        try
        {
            using var key = RegistryKey.OpenBaseKey(hive, view).OpenSubKey(subKeyPath, writable: false);
            if (key is null)
            {
                return;
            }

            foreach (var name in key.GetValueNames())
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                destination.Add(new StartupItemSnapshot(name, $"{locationPrefix}\\{subKeyPath}", DataSourceKind.RegistryReadOnly));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or global::System.Security.SecurityException)
        {
            warnings.Add(new ScanIssue("scanner.startup.registry_unavailable", $"{locationPrefix}\\{subKeyPath} could not be read: {exception.GetType().Name}", Name));
        }
    }

    private void ReadStartupFolder(
        string folderPath,
        string locationPrefix,
        ICollection<StartupItemSnapshot> destination,
        ICollection<ScanIssue> warnings)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        try
        {
            foreach (var filePath in Directory.EnumerateFiles(folderPath))
            {
                var name = Path.GetFileNameWithoutExtension(filePath);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                destination.Add(new StartupItemSnapshot(name, locationPrefix, DataSourceKind.FileSystemReadOnly));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or global::System.Security.SecurityException)
        {
            warnings.Add(new ScanIssue("scanner.startup.folder_unavailable", $"{locationPrefix} could not be read: {exception.GetType().Name}", Name));
        }
    }
}
