using BorealBoost.Core.Scanner;

namespace BorealBoost.Tests.Unit;

public sealed class ScannerNormalizationTests
{
    [Fact]
    public void Windows_10_19045_x64_is_legacy_supported()
    {
        var result = WindowsCompatibilityClassifier.Classify("Microsoft Windows 10 Pro", 19045, "x64");

        Assert.Equal(WindowsCompatibilityStatus.LegacySupported, result.Status);
    }

    [Fact]
    public void Windows_11_26200_x64_is_supported()
    {
        var result = WindowsCompatibilityClassifier.Classify("Microsoft Windows 11 Pro", 26200, "x64");

        Assert.Equal(WindowsCompatibilityStatus.Supported, result.Status);
    }

    [Fact]
    public void Unknown_os_data_remains_unknown()
    {
        var result = WindowsCompatibilityClassifier.Classify(null, null, "x64");

        Assert.Equal(WindowsCompatibilityStatus.Unknown, result.Status);
    }

    [Theory]
    [InlineData(0, DeviceHealthStatus.Ok)]
    [InlineData(22, DeviceHealthStatus.Disabled)]
    [InlineData(28, DeviceHealthStatus.MissingDriver)]
    [InlineData(31, DeviceHealthStatus.Problem)]
    public void Device_problem_codes_are_normalized(uint problemCode, DeviceHealthStatus expected)
    {
        var result = DeviceHealthClassifier.Classify(problemCode, "Error");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Storage_media_prefers_nvme_bus_over_media_code()
    {
        var result = StorageMediaClassifier.FromMicrosoftStorageCodes(0, 17);

        Assert.Equal(StorageMediaKind.Nvme, result);
    }

    [Fact]
    public void Wmi_adapter_ram_is_not_reported_as_known_vram()
    {
        var result = GpuMemoryReliabilityClassifier.FromWmiAdapterRam(4_293_918_720, GpuFormFactor.Unknown);

        Assert.Null(result.Bytes);
        Assert.Equal(VramDetectionStatus.Unknown, result.Status);
    }

    [Fact]
    public void Zero_adapter_ram_value_remains_unknown()
    {
        var result = GpuMemoryReliabilityClassifier.FromWmiAdapterRam(0, GpuFormFactor.Unknown);

        Assert.Null(result.Bytes);
        Assert.Equal(VramDetectionStatus.Unknown, result.Status);
    }

    [Fact]
    public void Null_adapter_ram_value_remains_unknown()
    {
        var result = GpuMemoryReliabilityClassifier.FromWmiAdapterRam(null, GpuFormFactor.Unknown);

        Assert.Null(result.Bytes);
        Assert.Equal(VramDetectionStatus.Unknown, result.Status);
    }

    [Fact]
    public void Integrated_gpu_keeps_vram_unknown_without_dedicated_source()
    {
        var formFactor = GpuFormFactorClassifier.Classify("Intel(R) UHD Graphics 770", HardwareVendor.Intel);
        var vram = GpuMemoryReliabilityClassifier.FromWmiAdapterRam(1_073_741_824, formFactor);

        Assert.Equal(GpuFormFactor.Integrated, formFactor);
        Assert.Null(vram.Bytes);
        Assert.Equal(VramDetectionStatus.Unknown, vram.Status);
    }

    [Fact]
    public void Microsoft_basic_display_adapter_is_virtual_for_scanner_purposes()
    {
        var formFactor = GpuFormFactorClassifier.Classify("Microsoft Basic Display Adapter", HardwareVendor.Microsoft);

        Assert.Equal(GpuFormFactor.Virtual, formFactor);
    }

    [Theory]
    [InlineData("Intel(R) Core", HardwareVendor.Intel)]
    [InlineData("Advanced Micro Devices", HardwareVendor.Amd)]
    [InlineData("NVIDIA", HardwareVendor.Nvidia)]
    [InlineData("", HardwareVendor.Unknown)]
    public void Hardware_vendor_classifier_handles_known_and_unknown_values(string text, HardwareVendor expected)
    {
        var result = HardwareVendorClassifier.ClassifyCpuOrDeviceVendor(text);

        Assert.Equal(expected, result);
    }
}
