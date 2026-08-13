namespace BorealBoost.Core.Foundation;

public sealed record OperatingSystemSummary(
    string Description,
    string Architecture,
    string MachineName);

public interface IBasicSystemInfoProvider
{
    OperatingSystemSummary GetOperatingSystemSummary();
}
