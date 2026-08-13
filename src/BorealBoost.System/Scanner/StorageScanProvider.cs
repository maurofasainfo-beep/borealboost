using BorealBoost.Core.Scanner;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class StorageScanProvider : ISystemScanProvider
{
    private readonly WmiQueryService _wmi;

    public StorageScanProvider(WmiQueryService wmi)
    {
        _wmi = wmi;
    }

    public string Name => "Storage";

    public int Weight => 10;

    public TimeSpan Timeout => TimeSpan.FromSeconds(10);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Composite, CollectCoreAsync, cancellationToken);
    }

    private async Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        var disks = await CollectDisksAsync(cancellationToken).ConfigureAwait(false);
        var volumes = DriveInfo.GetDrives()
            .Where(drive => drive.IsReady)
            .Select(drive => new StorageVolumeSnapshot(
                drive.Name,
                SafeDriveLabel(drive),
                drive.DriveType.ToString(),
                SafeTotalSize(drive),
                SafeFreeSpace(drive),
                IsSystemDrive(drive.Name),
                DataSourceKind.DriveInfo))
            .ToArray();

        return new SystemSnapshotPatch(Storage: new StorageSnapshot(disks, volumes, DataSourceKind.Composite));
    }

    private async Task<IReadOnlyList<StorageDiskSnapshot>> CollectDisksAsync(CancellationToken cancellationToken)
    {
        var storageRows = await QueryStoragePhysicalDisksAsync(cancellationToken).ConfigureAwait(false);
        if (storageRows.Count > 0)
        {
            return storageRows.Select(row =>
            {
                var mediaKind = StorageMediaClassifier.FromMicrosoftStorageCodes(row.UInt32("MediaType"), row.UInt32("BusType"));
                return new StorageDiskSnapshot(
                    row.String("FriendlyName"),
                    row.String("Manufacturer"),
                    row.UInt64("Size"),
                    mediaKind,
                    FormatBusType(row.UInt32("BusType")),
                    FormatHealthStatus(row.UInt32("HealthStatus")),
                    DataSourceKind.Wmi);
            }).ToArray();
        }

        var diskRows = await _wmi.QueryAsync(
            "SELECT Model,Manufacturer,Size,MediaType,InterfaceType,Status FROM Win32_DiskDrive",
            Timeout,
            cancellationToken).ConfigureAwait(false);

        return diskRows.Select(row => new StorageDiskSnapshot(
            row.String("Model"),
            row.String("Manufacturer"),
            row.UInt64("Size"),
            StorageMediaKind.Unknown,
            row.String("InterfaceType"),
            row.String("Status"),
            DataSourceKind.Wmi)).ToArray();
    }

    private async Task<IReadOnlyList<WmiRow>> QueryStoragePhysicalDisksAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _wmi.QueryAsync(
                @"root\Microsoft\Windows\Storage",
                "SELECT FriendlyName,Manufacturer,Size,MediaType,BusType,HealthStatus FROM MSFT_PhysicalDisk",
                TimeSpan.FromSeconds(6),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or global::System.Management.ManagementException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static string? SafeDriveLabel(DriveInfo drive)
    {
        try
        {
            return string.IsNullOrWhiteSpace(drive.VolumeLabel) ? null : drive.VolumeLabel;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static long? SafeTotalSize(DriveInfo drive)
    {
        try
        {
            return drive.TotalSize;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static long? SafeFreeSpace(DriveInfo drive)
    {
        try
        {
            return drive.AvailableFreeSpace;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool IsSystemDrive(string driveName)
    {
        var systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
        return string.Equals(Path.GetPathRoot(driveName), systemRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string? FormatBusType(uint? busType)
    {
        return busType switch
        {
            3 => "ATA",
            7 => "USB",
            11 => "SATA",
            16 => "SAS",
            17 => "NVMe",
            _ => busType?.ToString(global::System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static string? FormatHealthStatus(uint? healthStatus)
    {
        return healthStatus switch
        {
            0 => "Healthy",
            1 => "Warning",
            2 => "Unhealthy",
            _ => healthStatus?.ToString(global::System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
