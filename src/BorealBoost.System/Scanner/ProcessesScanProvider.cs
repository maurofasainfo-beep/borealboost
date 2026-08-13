using System.Diagnostics;
using BorealBoost.Core.Scanner;

namespace BorealBoost.System.Scanner;

public sealed class ProcessesScanProvider : ISystemScanProvider
{
    public string Name => "Processes";

    public int Weight => 6;

    public TimeSpan Timeout => TimeSpan.FromSeconds(5);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.DotNetRuntime, CollectCoreAsync, cancellationToken);
    }

    private static Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var processes = Process.GetProcesses()
            .Select(process =>
            {
                using (process)
                {
                    return new ProcessSnapshot(
                        SafeProcessId(process),
                        SafeProcessName(process),
                        SafeWorkingSet(process),
                        DataSourceKind.DotNetRuntime);
                }
            })
            .Where(process => process.ProcessId > 0 && !string.IsNullOrWhiteSpace(process.ProcessName))
            .ToArray();

        return Task.FromResult(new SystemSnapshotPatch(Processes: processes));
    }

    private static int SafeProcessId(Process process)
    {
        try
        {
            return process.Id;
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }

    private static string SafeProcessName(Process process)
    {
        try
        {
            return process.ProcessName;
        }
        catch (InvalidOperationException)
        {
            return "Unknown";
        }
    }

    private static long? SafeWorkingSet(Process process)
    {
        try
        {
            return process.WorkingSet64;
        }
        catch (Exception exception) when (exception is InvalidOperationException or global::System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
