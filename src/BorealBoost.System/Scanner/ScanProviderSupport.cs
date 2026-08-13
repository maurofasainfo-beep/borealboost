using System.Diagnostics;
using BorealBoost.Core.Scanner;

namespace BorealBoost.System.Scanner;

internal static class ScanProviderSupport
{
    public static async Task<ProviderScanResult> RunAsync(
        string providerName,
        DataSourceKind source,
        Func<CancellationToken, Task<SystemSnapshotPatch>> collect,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var patch = await collect(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            return new ProviderScanResult(ProviderResult.Succeeded(providerName, source, stopwatch.Elapsed), patch);
        }
        catch (NotSupportedException exception)
        {
            stopwatch.Stop();
            return ProviderScanResult.Empty(ProviderResult.NotSupported(providerName, source, stopwatch.Elapsed, "scanner.provider.not_supported", exception.Message));
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            return ProviderScanResult.Empty(ProviderResult.TimedOut(providerName, stopwatch.Elapsed));
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or global::System.Management.ManagementException)
        {
            stopwatch.Stop();
            return ProviderScanResult.Empty(ProviderResult.Failed(providerName, source, stopwatch.Elapsed, "scanner.provider.failed", exception.Message));
        }
    }

    public static ProviderScanResult Partial(string providerName, DataSourceKind source, TimeSpan duration, SystemSnapshotPatch patch, string code, string message)
    {
        return new ProviderScanResult(
            ProviderResult.Partial(providerName, source, duration, [new ScanIssue(code, message, providerName)]),
            patch);
    }
}
