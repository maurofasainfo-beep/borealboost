using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Common;
using BorealBoost.Core.Identity;

namespace BorealBoost.Agent;

public static class AgentBootstrapOptionsParser
{
    private const int MaxOptionLength = 32;
    private const int MaxValueLength = 256;

    private static readonly HashSet<string> AllowedOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "--pipeName",
        "--sessionId",
        "--bootstrapNonce",
        "--protocolVersion"
    };

    public static Result<AgentBootstrapOptions> Parse(IReadOnlyList<string> args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Count; index += 2)
        {
            var option = args[index];
            if (string.IsNullOrWhiteSpace(option) || option.Length > MaxOptionLength)
            {
                return Result<AgentBootstrapOptions>.Failure(
                    "agent.bootstrap.option_invalid",
                    "Agent bootstrap option is invalid.");
            }

            if (!AllowedOptions.Contains(option))
            {
                return Result<AgentBootstrapOptions>.Failure(
                    "agent.bootstrap.option_not_allowed",
                    $"Agent bootstrap option '{option}' is not allowed.");
            }

            if (index + 1 >= args.Count)
            {
                return Result<AgentBootstrapOptions>.Failure(
                    "agent.bootstrap.value_missing",
                    $"Agent bootstrap option '{option}' requires a value.");
            }

            if (values.ContainsKey(option))
            {
                return Result<AgentBootstrapOptions>.Failure(
                    "agent.bootstrap.option_duplicate",
                    $"Agent bootstrap option '{option}' was provided more than once.");
            }

            var value = args[index + 1];
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaxValueLength)
            {
                return Result<AgentBootstrapOptions>.Failure(
                    "agent.bootstrap.value_invalid",
                    $"Agent bootstrap option '{option}' has an invalid value.");
            }

            values[option] = value;
        }

        if (values.Count == 0)
        {
            return Result<AgentBootstrapOptions>.Success(new AgentBootstrapOptions(null, null, null, null, null));
        }

        if (!values.TryGetValue("--pipeName", out var pipeName) ||
            !values.TryGetValue("--sessionId", out var sessionIdRaw) ||
            !values.TryGetValue("--bootstrapNonce", out var nonce) ||
            !values.TryGetValue("--protocolVersion", out var protocolVersionRaw))
        {
            return Result<AgentBootstrapOptions>.Failure(
                "agent.bootstrap.incomplete",
                "Agent bootstrap requires pipeName, sessionId, bootstrapNonce and protocolVersion.");
        }

        var pipeResult = AgentPipeName.ParseFullName(pipeName);
        if (pipeResult.IsFailure || pipeResult.Value is null)
        {
            return Result<AgentBootstrapOptions>.Failure(
                "agent.bootstrap.pipe_invalid",
                "Agent pipe name is invalid.");
        }

        if (!SessionId.TryParse(sessionIdRaw, out var sessionId))
        {
            return Result<AgentBootstrapOptions>.Failure(
                "agent.bootstrap.session_invalid",
                "Agent sessionId is invalid.");
        }

        if (pipeResult.Value.SessionId != sessionId)
        {
            return Result<AgentBootstrapOptions>.Failure(
                "agent.bootstrap.pipe_session_mismatch",
                "Agent pipe name does not match sessionId.");
        }

        if (!AgentNonce.IsValidBootstrapNonce(nonce))
        {
            return Result<AgentBootstrapOptions>.Failure(
                "agent.bootstrap.nonce_invalid",
                "Agent bootstrapNonce is invalid.");
        }

        if (!ProtocolVersion.TryParse(protocolVersionRaw, out var protocolVersion) ||
            !protocolVersion.IsCompatibleWith(ProtocolVersion.Current))
        {
            return Result<AgentBootstrapOptions>.Failure(
                "agent.bootstrap.protocol_invalid",
                "Agent protocolVersion is invalid or unsupported.");
        }

        return Result<AgentBootstrapOptions>.Success(new AgentBootstrapOptions(
            pipeName,
            pipeResult.Value.LocalName,
            sessionId,
            nonce,
            protocolVersion));
    }
}
