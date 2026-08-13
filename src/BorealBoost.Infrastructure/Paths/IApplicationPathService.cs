namespace BorealBoost.Infrastructure.Paths;

using BorealBoost.Core.Identity;

public interface IApplicationPathService
{
    BorealBoostPaths GetPaths();

    string GetSessionDirectory(SessionId sessionId);

    void EnsureUserWritableDirectories();
}
