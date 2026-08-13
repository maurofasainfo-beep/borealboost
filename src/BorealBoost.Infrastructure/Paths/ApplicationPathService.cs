namespace BorealBoost.Infrastructure.Paths;

using BorealBoost.Core.Identity;

public sealed class ApplicationPathService : IApplicationPathService
{
    private const string ProductDirectoryName = "BorealBoost";

    public BorealBoostPaths GetPaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        var userRoot = Path.Combine(localAppData, ProductDirectoryName);
        var machineRoot = Path.Combine(commonAppData, ProductDirectoryName);

        return new BorealBoostPaths(
            UserDataRoot: userRoot,
            MachineDataRoot: machineRoot,
            LogsDirectory: Path.Combine(userRoot, "Logs"),
            ConfigurationDirectory: Path.Combine(machineRoot, "Config"),
            SessionsDirectory: Path.Combine(machineRoot, "Sessions"),
            SnapshotsDirectory: Path.Combine(machineRoot, "Snapshots"),
            ReportsDirectory: Path.Combine(machineRoot, "Reports"));
    }

    public void EnsureUserWritableDirectories()
    {
        var paths = GetPaths();

        Directory.CreateDirectory(paths.UserDataRoot);
        Directory.CreateDirectory(paths.LogsDirectory);
    }

    public string GetSessionDirectory(SessionId sessionId)
    {
        if (sessionId.Value == Guid.Empty)
        {
            throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));
        }

        var paths = GetPaths();
        return CombineContainedPath(paths.SessionsDirectory, sessionId.ToString());
    }

    private static string CombineContainedPath(string root, string childName)
    {
        var fullRoot = Path.GetFullPath(root);
        var combined = Path.GetFullPath(Path.Combine(fullRoot, childName));
        var rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved path escaped BorealBoost data root.");
        }

        return combined;
    }
}
