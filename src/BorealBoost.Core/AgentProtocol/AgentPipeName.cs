using BorealBoost.Core.Common;
using BorealBoost.Core.Identity;

namespace BorealBoost.Core.AgentProtocol;

public sealed record AgentPipeIdentity(SessionId SessionId, string PipeToken, string FullName, string LocalName);

public static class AgentPipeName
{
    public const int MaxFullNameLength = 256;
    public const string FullPrefix = @"\\.\pipe\";
    public const string LocalPrefix = "BorealBoost.Agent.";

    public static string CreateFullName(SessionId sessionId, string pipeToken)
    {
        if (sessionId.Value == Guid.Empty)
        {
            throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));
        }

        if (!AgentNonce.IsValidPipeToken(pipeToken))
        {
            throw new ArgumentException("Pipe token is invalid.", nameof(pipeToken));
        }

        return FullPrefix + LocalPrefix + sessionId + "." + pipeToken;
    }

    public static Result<AgentPipeIdentity> ParseFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > MaxFullNameLength)
        {
            return Result<AgentPipeIdentity>.Failure("agent.pipe.invalid", "Agent pipe name is invalid.");
        }

        if (!fullName.StartsWith(FullPrefix + LocalPrefix, StringComparison.Ordinal))
        {
            return Result<AgentPipeIdentity>.Failure("agent.pipe.prefix_invalid", "Agent pipe name prefix is invalid.");
        }

        var localName = fullName[FullPrefix.Length..];
        var suffix = localName[LocalPrefix.Length..];
        var parts = suffix.Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !SessionId.TryParse(parts[0], out var sessionId) ||
            !AgentNonce.IsValidPipeToken(parts[1]))
        {
            return Result<AgentPipeIdentity>.Failure("agent.pipe.format_invalid", "Agent pipe name format is invalid.");
        }

        return Result<AgentPipeIdentity>.Success(new AgentPipeIdentity(sessionId, parts[1], fullName, localName));
    }
}
