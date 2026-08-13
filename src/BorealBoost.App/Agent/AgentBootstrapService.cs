using System.Diagnostics;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Identity;
using BorealBoost.Infrastructure.AgentIpc;
using Microsoft.Extensions.Logging;

namespace BorealBoost.App.Agent;

public sealed class AgentBootstrapService : IAgentBootstrapService
{
    private static readonly TimeSpan AgentStartupTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PipeOperationTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<AgentBootstrapService> _logger;

    public AgentBootstrapService(ILogger<AgentBootstrapService> logger)
    {
        _logger = logger;
    }

    public async Task<AgentBootstrapResult> ProbeAsync(CancellationToken cancellationToken)
    {
        var sessionId = SessionId.New();
        var correlationId = CorrelationId.New();
        var bootstrapNonce = AgentNonce.GenerateBootstrapNonce();
        var pipeName = AgentPipeName.CreateFullName(sessionId, AgentNonce.GeneratePipeToken());
        var agentPath = ResolveAgentPath();

        if (agentPath is null)
        {
            return AgentBootstrapResult.Failed("agent.path.not_found", "BorealBoost.Agent executable was not found.");
        }

        using var process = StartAgent(agentPath, pipeName, sessionId, bootstrapNonce);
        if (process is null)
        {
            return AgentBootstrapResult.Failed("agent.start.failed", "BorealBoost.Agent could not be started.");
        }

        try
        {
            await using var client = await ConnectAsync(pipeName, cancellationToken).ConfigureAwait(false);
            var handshake = AgentPipeProtocol.CreateMessage(
                sessionId,
                correlationId,
                sequenceNumber: 1,
                bootstrapNonce,
                MessageType.HandshakeRequest,
                PayloadType.HandshakeRequest,
                new HandshakeRequestPayload(sessionId, bootstrapNonce, DateTimeOffset.UtcNow),
                DateTimeOffset.UtcNow);

            var writeHandshake = await client.WriteMessageAsync(handshake, cancellationToken).ConfigureAwait(false);
            if (writeHandshake.IsFailure)
            {
                return AgentBootstrapResult.Failed(writeHandshake.ErrorCode ?? "agent.handshake.write_failed", writeHandshake.ErrorMessage ?? "Agent handshake write failed.");
            }

            var handshakeResponse = await client.ReadMessageAsync(cancellationToken).ConfigureAwait(false);
            if (handshakeResponse.IsFailure || handshakeResponse.Value is null ||
                handshakeResponse.Value.Envelope.MessageType != MessageType.HandshakeResponse)
            {
                return AgentBootstrapResult.Failed(handshakeResponse.ErrorCode ?? "agent.handshake.failed", handshakeResponse.ErrorMessage ?? "Agent handshake failed.");
            }

            var statusRequest = AgentPipeProtocol.CreateMessage(
                sessionId,
                correlationId,
                sequenceNumber: 3,
                bootstrapNonce,
                MessageType.AgentStatusRequest,
                PayloadType.AgentStatusRequest,
                new AgentStatusRequestPayload(),
                DateTimeOffset.UtcNow);

            var writeStatus = await client.WriteMessageAsync(statusRequest, cancellationToken).ConfigureAwait(false);
            if (writeStatus.IsFailure)
            {
                return AgentBootstrapResult.Failed(writeStatus.ErrorCode ?? "agent.status.write_failed", writeStatus.ErrorMessage ?? "Agent status write failed.");
            }

            var statusResponse = await client.ReadMessageAsync(cancellationToken).ConfigureAwait(false);
            if (statusResponse.IsFailure || statusResponse.Value is null ||
                statusResponse.Value.Envelope.MessageType != MessageType.AgentStatusResponse)
            {
                return AgentBootstrapResult.Failed(statusResponse.ErrorCode ?? "agent.status.failed", statusResponse.ErrorMessage ?? "Agent status request failed.");
            }

            var payload = AgentPipeProtocol.DeserializePayload<AgentStatusResponsePayload>(statusResponse.Value);
            if (payload.IsFailure || payload.Value is null)
            {
                return AgentBootstrapResult.Failed(payload.ErrorCode ?? "agent.status.payload_invalid", payload.ErrorMessage ?? "Agent status payload invalid.");
            }

            var shutdownRequest = AgentPipeProtocol.CreateMessage(
                sessionId,
                correlationId,
                sequenceNumber: 5,
                bootstrapNonce,
                MessageType.ShutdownRequest,
                PayloadType.ShutdownRequest,
                new ShutdownRequestPayload("foundation-probe-complete"),
                DateTimeOffset.UtcNow);

            await client.WriteMessageAsync(shutdownRequest, cancellationToken).ConfigureAwait(false);
            await client.ReadMessageAsync(cancellationToken).ConfigureAwait(false);

            await WaitForAgentExitAsync(process, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Agent bootstrap probe completed. SessionId={SessionId}; AgentVersion={AgentVersion}; AcceptsPrivilegedOperations={AcceptsPrivilegedOperations}",
                sessionId,
                payload.Value.AgentVersion,
                payload.Value.AcceptsPrivilegedOperations);
            return AgentBootstrapResult.Completed(payload.Value.AgentVersion);
        }
        catch (Exception exception) when (exception is IOException or TimeoutException or OperationCanceledException)
        {
            _logger.LogWarning(exception, "Agent bootstrap probe failed. SessionId={SessionId}", sessionId);
            return AgentBootstrapResult.Failed("agent.probe.failed", exception.Message);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private static async Task<NamedPipeAgentClient> ConnectAsync(string pipeName, CancellationToken cancellationToken)
    {
        var connect = await NamedPipeAgentClient.ConnectAsync(pipeName, PipeOperationTimeout, cancellationToken).ConfigureAwait(false);
        if (connect.IsFailure || connect.Value is null)
        {
            throw new IOException(connect.ErrorMessage ?? "Agent pipe connection failed.");
        }

        return connect.Value;
    }

    private static Process? StartAgent(string agentPath, string pipeName, SessionId sessionId, string bootstrapNonce)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = agentPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("--pipeName");
        startInfo.ArgumentList.Add(pipeName);
        startInfo.ArgumentList.Add("--sessionId");
        startInfo.ArgumentList.Add(sessionId.ToString());
        startInfo.ArgumentList.Add("--bootstrapNonce");
        startInfo.ArgumentList.Add(bootstrapNonce);
        startInfo.ArgumentList.Add("--protocolVersion");
        startInfo.ArgumentList.Add(ProtocolVersion.Current.ToString());

        return Process.Start(startInfo);
    }

    private static string? ResolveAgentPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var sameDirectory = Path.Combine(baseDirectory, "BorealBoost.Agent.exe");
        if (File.Exists(sameDirectory))
        {
            return sameDirectory;
        }

        var repositoryOutput = Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "..",
            "..",
            "..",
            "BorealBoost.Agent",
            "bin",
            "Debug",
            "net10.0-windows10.0.19041.0",
            "BorealBoost.Agent.exe"));

        return File.Exists(repositoryOutput) ? repositoryOutput : null;
    }

    private static async Task WaitForAgentExitAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken).WaitAsync(AgentStartupTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }
}
