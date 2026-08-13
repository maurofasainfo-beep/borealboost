using System.Runtime.InteropServices;

namespace BorealBoost.Core.Scanner;

public static class WindowsCompatibilityClassifier
{
    public static (WindowsCompatibilityStatus Status, string Reason) Classify(string? osName, int? build, string architecture)
    {
        if (build is null || string.IsNullOrWhiteSpace(osName))
        {
            return (WindowsCompatibilityStatus.Unknown, "Windows version could not be fully identified.");
        }

        if (!IsX64(architecture))
        {
            return (WindowsCompatibilityStatus.Unsupported, "BorealBoost V1 targets x64 Windows only.");
        }

        if (osName.Contains("Windows 10", StringComparison.OrdinalIgnoreCase))
        {
            return build >= 19045
                ? (WindowsCompatibilityStatus.LegacySupported, "Windows 10 22H2 x64/build 19045 remains a BorealBoost legacy functional target.")
                : (WindowsCompatibilityStatus.Unsupported, "Windows 10 builds older than 19045 are outside the V1 target.");
        }

        if (osName.Contains("Windows 11", StringComparison.OrdinalIgnoreCase))
        {
            if (build >= 28000)
            {
                return (WindowsCompatibilityStatus.Unknown, "Windows 11 26H1+ requires explicit validation before being classified as supported.");
            }

            return build >= 22631
                ? (WindowsCompatibilityStatus.Supported, "Windows 11 x64 build is within the BorealBoost V1 functional target.")
                : (WindowsCompatibilityStatus.Unsupported, "Windows 11 builds older than 23H2 are outside the V1 target.");
        }

        return (WindowsCompatibilityStatus.Unknown, "The operating system family is unknown.");
    }

    private static bool IsX64(string architecture)
    {
        return architecture.Equals("X64", StringComparison.OrdinalIgnoreCase) ||
               architecture.Equals("AMD64", StringComparison.OrdinalIgnoreCase) ||
               RuntimeInformation.OSArchitecture == Architecture.X64;
    }
}

public static class HardwareVendorClassifier
{
    public static HardwareVendor ClassifyCpuOrDeviceVendor(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return HardwareVendor.Unknown;
        }

        if (text.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.Intel;
        }

        if (text.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.Amd;
        }

        if (text.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.Nvidia;
        }

        if (text.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.Microsoft;
        }

        if (text.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.HyperV;
        }

        if (text.Contains("VMware", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.Vmware;
        }

        if (text.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.VirtualBox;
        }

        return HardwareVendor.Other;
    }
}

public static class DeviceHealthClassifier
{
    public static DeviceHealthStatus Classify(uint? configManagerErrorCode, string? status)
    {
        if (configManagerErrorCode is 0)
        {
            return DeviceHealthStatus.Ok;
        }

        if (configManagerErrorCode is 22)
        {
            return DeviceHealthStatus.Disabled;
        }

        if (configManagerErrorCode is 28)
        {
            return DeviceHealthStatus.MissingDriver;
        }

        if (configManagerErrorCode is not null)
        {
            return DeviceHealthStatus.Problem;
        }

        if (status?.Equals("OK", StringComparison.OrdinalIgnoreCase) == true)
        {
            return DeviceHealthStatus.Ok;
        }

        return DeviceHealthStatus.Unknown;
    }
}

public static class StorageMediaClassifier
{
    public static StorageMediaKind FromMicrosoftStorageCodes(uint? mediaType, uint? busType)
    {
        if (busType is 17)
        {
            return StorageMediaKind.Nvme;
        }

        return mediaType switch
        {
            3 => StorageMediaKind.Hdd,
            4 => StorageMediaKind.Ssd,
            5 => StorageMediaKind.Ssd,
            _ => StorageMediaKind.Unknown
        };
    }
}

public static class GpuFormFactorClassifier
{
    public static GpuFormFactor Classify(string? name, HardwareVendor vendor)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return GpuFormFactor.Unknown;
        }

        if (vendor is HardwareVendor.HyperV or HardwareVendor.Vmware or HardwareVendor.VirtualBox or HardwareVendor.Microsoft ||
            name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Remote Display", StringComparison.OrdinalIgnoreCase))
        {
            return GpuFormFactor.Virtual;
        }

        if (vendor == HardwareVendor.Intel &&
            (name.Contains("UHD Graphics", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("HD Graphics", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("Iris", StringComparison.OrdinalIgnoreCase)))
        {
            return GpuFormFactor.Integrated;
        }

        return GpuFormFactor.Unknown;
    }
}

public static class GpuMemoryReliabilityClassifier
{
    public static (ulong? Bytes, VramDetectionStatus Status) FromWmiAdapterRam(
        ulong? adapterRamBytes,
        GpuFormFactor formFactor)
    {
        if (adapterRamBytes is null or 0)
        {
            return (null, VramDetectionStatus.Unknown);
        }

        if (formFactor is GpuFormFactor.Virtual or GpuFormFactor.Integrated)
        {
            return (null, VramDetectionStatus.Unknown);
        }

        // Win32_VideoController.AdapterRAM is frequently truncated to 32 bits or reports shared memory.
        return (null, VramDetectionStatus.Unknown);
    }
}
