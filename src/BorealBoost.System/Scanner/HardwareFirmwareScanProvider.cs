using System.Runtime.InteropServices;
using BorealBoost.Core.Scanner;
using BorealBoost.System.Registry;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class HardwareFirmwareScanProvider : ISystemScanProvider
{
    private const string SecureBootKey = @"SYSTEM\CurrentControlSet\Control\SecureBoot\State";

    private readonly WmiQueryService _wmi;
    private readonly ReadOnlyRegistryReader _registry;

    public HardwareFirmwareScanProvider(WmiQueryService wmi, ReadOnlyRegistryReader registry)
    {
        _wmi = wmi;
        _registry = registry;
    }

    public string Name => "Hardware";

    public int Weight => 10;

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Composite, CollectCoreAsync, cancellationToken);
    }

    private async Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        var computerRows = await _wmi.QueryAsync(
            "SELECT Manufacturer,Model,PCSystemTypeEx,HypervisorPresent FROM Win32_ComputerSystem",
            Timeout,
            cancellationToken).ConfigureAwait(false);
        var boardRows = await _wmi.QueryAsync(
            "SELECT Manufacturer,Product,Version FROM Win32_BaseBoard",
            Timeout,
            cancellationToken).ConfigureAwait(false);
        var biosRows = await _wmi.QueryAsync(
            "SELECT Manufacturer,SMBIOSBIOSVersion,Version,ReleaseDate FROM Win32_BIOS",
            Timeout,
            cancellationToken).ConfigureAwait(false);
        var enclosureRows = await _wmi.QueryAsync(
            "SELECT ChassisTypes FROM Win32_SystemEnclosure",
            Timeout,
            cancellationToken).ConfigureAwait(false);

        var computer = computerRows.FirstOrDefault();
        var board = boardRows.FirstOrDefault();
        var bios = biosRows.FirstOrDefault();
        var chassisTypes = enclosureRows.FirstOrDefault()?.StringArray("ChassisTypes")
            .Select(value => ushort.TryParse(value, out var parsed) ? parsed : (ushort?)null)
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .ToArray() ?? [];
        var manufacturer = computer?.String("Manufacturer");
        var model = computer?.String("Model");
        var hypervisorPresent = computer?.Bool("HypervisorPresent");
        var virtualPlatform = DetectVirtualizationPlatform(manufacturer, model);
        var formFactor = virtualPlatform is not null
            ? MachineFormFactor.VirtualMachine
            : ClassifyFormFactor(computer?.UInt16("PCSystemTypeEx"), chassisTypes);

        var firmware = new FirmwareSnapshot(
            bios?.String("Manufacturer"),
            bios?.String("SMBIOSBIOSVersion") ?? bios?.String("Version"),
            bios?.CimDateTime("ReleaseDate"),
            GetFirmwareType(),
            ReadSecureBootState(),
            DataSourceKind.Composite);

        return new SystemSnapshotPatch(
            Hardware: new HardwareSnapshot(manufacturer, model, formFactor, formFactor == MachineFormFactor.VirtualMachine, virtualPlatform, DataSourceKind.Wmi),
            Motherboard: new MotherboardSnapshot(board?.String("Manufacturer"), board?.String("Product"), board?.String("Version"), DataSourceKind.Wmi),
            Firmware: firmware,
            Capabilities: hypervisorPresent.HasValue
                ? [new SystemCapabilitySnapshot("HypervisorPresent", DetectionStatus.Known, hypervisorPresent, hypervisorPresent.Value.ToString(), DataSourceKind.Wmi)]
                : [new SystemCapabilitySnapshot("HypervisorPresent", DetectionStatus.Unknown, null, null, DataSourceKind.Wmi)]);
    }

    private bool? ReadSecureBootState()
    {
        var value = _registry.ReadLocalMachineInt32(SecureBootKey, "UEFISecureBootEnabled");
        return value switch
        {
            0 => false,
            1 => true,
            _ => null
        };
    }

    private static string? DetectVirtualizationPlatform(string? manufacturer, string? model)
    {
        var joined = $"{manufacturer} {model}";
        if (joined.Contains("Microsoft Corporation Virtual Machine", StringComparison.OrdinalIgnoreCase) ||
            joined.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase))
        {
            return "Hyper-V";
        }

        if (joined.Contains("VMware", StringComparison.OrdinalIgnoreCase))
        {
            return "VMware";
        }

        if (joined.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase))
        {
            return "VirtualBox";
        }

        if (joined.Contains("QEMU", StringComparison.OrdinalIgnoreCase))
        {
            return "QEMU";
        }

        return null;
    }

    private static MachineFormFactor ClassifyFormFactor(ushort? pcSystemTypeEx, IReadOnlyList<ushort> chassisTypes)
    {
        if (chassisTypes.Any(type => type is 30))
        {
            return MachineFormFactor.Tablet;
        }

        if (chassisTypes.Any(type => type is 31 or 32))
        {
            return MachineFormFactor.Convertible;
        }

        if (chassisTypes.Any(type => type is 8 or 9 or 10 or 14))
        {
            return MachineFormFactor.Laptop;
        }

        if (chassisTypes.Any(type => type is 3 or 4 or 5 or 6 or 7 or 15 or 16 or 35))
        {
            return MachineFormFactor.Desktop;
        }

        return pcSystemTypeEx switch
        {
            1 or 3 => MachineFormFactor.Desktop,
            2 => MachineFormFactor.Laptop,
            _ => MachineFormFactor.Unknown
        };
    }

    private static string? GetFirmwareType()
    {
        return GetFirmwareType(out var firmwareType)
            ? firmwareType switch
            {
                FirmwareTypeBios => "Legacy",
                FirmwareTypeUefi => "UEFI",
                _ => "Unknown"
            }
            : null;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetFirmwareType(out uint firmwareType);

    private const uint FirmwareTypeBios = 1;
    private const uint FirmwareTypeUefi = 2;
}
