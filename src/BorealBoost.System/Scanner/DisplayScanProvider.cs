using System.Runtime.InteropServices;
using BorealBoost.Core.Scanner;

namespace BorealBoost.System.Scanner;

public sealed class DisplayScanProvider : ISystemScanProvider
{
    private const int EnumCurrentSettings = -1;
    private const int DisplayDeviceActive = 0x1;
    private const int DisplayDevicePrimaryDevice = 0x4;

    public string Name => "Displays";

    public int Weight => 8;

    public TimeSpan Timeout => TimeSpan.FromSeconds(5);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.WindowsApi, CollectCoreAsync, cancellationToken);
    }

    private static Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var displays = new List<DisplaySnapshot>();
        uint index = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var device = DisplayDevice.Create();
            if (!EnumDisplayDevices(null, index, ref device, 0))
            {
                break;
            }

            if ((device.StateFlags & DisplayDeviceActive) == 0)
            {
                index++;
                continue;
            }

            var mode = DevMode.Create();
            var hasMode = EnumDisplaySettings(device.DeviceName, EnumCurrentSettings, ref mode);
            displays.Add(new DisplaySnapshot(
                device.DeviceName,
                string.IsNullOrWhiteSpace(device.DeviceString) ? null : device.DeviceString,
                hasMode ? mode.PelsWidth : null,
                hasMode ? mode.PelsHeight : null,
                hasMode && mode.DisplayFrequency > 0 ? mode.DisplayFrequency : null,
                hasMode && mode.LogPixels > 0 ? (int)mode.LogPixels : null,
                (device.StateFlags & DisplayDevicePrimaryDevice) != 0,
                DataSourceKind.WindowsApi));

            index++;
        }

        return Task.FromResult(new SystemSnapshotPatch(Displays: displays));
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DevMode lpDevMode);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int Cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public int StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;

        public static DisplayDevice Create()
        {
            return new DisplayDevice
            {
                Cb = Marshal.SizeOf<DisplayDevice>(),
                DeviceName = string.Empty,
                DeviceString = string.Empty,
                DeviceId = string.Empty,
                DeviceKey = string.Empty
            };
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        public ushort SpecVersion;
        public ushort DriverVersion;
        public ushort Size;
        public ushort DriverExtra;
        public uint Fields;
        public int PositionX;
        public int PositionY;
        public uint DisplayOrientation;
        public uint DisplayFixedOutput;
        public short Color;
        public short Duplex;
        public short YResolution;
        public short TTOption;
        public short Collate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FormName;

        public ushort LogPixels;
        public uint BitsPerPel;
        public int PelsWidth;
        public int PelsHeight;
        public uint DisplayFlags;
        public int DisplayFrequency;
        public uint ICMMethod;
        public uint ICMIntent;
        public uint MediaType;
        public uint DitherType;
        public uint Reserved1;
        public uint Reserved2;
        public uint PanningWidth;
        public uint PanningHeight;

        public static DevMode Create()
        {
            return new DevMode
            {
                DeviceName = string.Empty,
                FormName = string.Empty,
                Size = (ushort)Marshal.SizeOf<DevMode>()
            };
        }
    }
}
