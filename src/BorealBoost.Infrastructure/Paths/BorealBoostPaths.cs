namespace BorealBoost.Infrastructure.Paths;

public sealed record BorealBoostPaths(
    string UserDataRoot,
    string MachineDataRoot,
    string LogsDirectory,
    string ConfigurationDirectory,
    string SessionsDirectory,
    string SnapshotsDirectory,
    string ReportsDirectory);
