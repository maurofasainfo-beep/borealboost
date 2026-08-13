using System.Diagnostics;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Identity;
using BorealBoost.Infrastructure.AgentIpc;

namespace BorealBoost.Tests.System;

public sealed class AgentIpcSystemTests
{
    [Fact]
    public async Task Agent_accepts_foundation_handshake_status_and_shutdown()
    {
        var session = await StartAgentSessionAsync();
        await using (session.Client)
        using (session.Process)
        {
            var handshake = AgentPipeProtocol.CreateMessage(
                session.SessionId,
                session.CorrelationId,
                1,
                session.Nonce,
                MessageType.HandshakeRequest,
                PayloadType.HandshakeRequest,
                new HandshakeRequestPayload(session.SessionId, session.Nonce, DateTimeOffset.UtcNow),
                DateTimeOffset.UtcNow);

            Assert.True((await session.Client.WriteMessageAsync(handshake, CancellationToken.None)).IsSuccess);
            var handshakeResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
            Assert.True(handshakeResponse.IsSuccess);
            Assert.Equal(MessageType.HandshakeResponse, handshakeResponse.Value!.Envelope.MessageType);

            var status = AgentPipeProtocol.CreateMessage(
                session.SessionId,
                session.CorrelationId,
                3,
                session.Nonce,
                MessageType.AgentStatusRequest,
                PayloadType.AgentStatusRequest,
                new AgentStatusRequestPayload(),
                DateTimeOffset.UtcNow);

            Assert.True((await session.Client.WriteMessageAsync(status, CancellationToken.None)).IsSuccess);
            var statusResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
            Assert.True(statusResponse.IsSuccess);
            Assert.Equal(MessageType.AgentStatusResponse, statusResponse.Value!.Envelope.MessageType);
            var payload = AgentPipeProtocol.DeserializePayload<AgentStatusResponsePayload>(statusResponse.Value);
            Assert.True(payload.IsSuccess);
            Assert.False(payload.Value!.AcceptsPrivilegedOperations);

            await ShutdownAsync(session.Client, session.SessionId, session.CorrelationId, session.Nonce, 5);
            await session.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, session.Process.ExitCode);
        }
    }

    [Fact]
    public async Task Agent_rejects_wrong_nonce()
    {
        var session = await StartAgentSessionAsync();
        await using (session.Client)
        using (session.Process)
        {
            var handshake = AgentPipeProtocol.CreateMessage(
                session.SessionId,
                session.CorrelationId,
                1,
                AgentNonce.GenerateBootstrapNonce(),
                MessageType.HandshakeRequest,
                PayloadType.HandshakeRequest,
                new HandshakeRequestPayload(session.SessionId, session.Nonce, DateTimeOffset.UtcNow),
                DateTimeOffset.UtcNow);

            Assert.True((await session.Client.WriteMessageAsync(handshake, CancellationToken.None)).IsSuccess);
            var response = await session.Client.ReadMessageAsync(CancellationToken.None);

            Assert.True(response.IsSuccess);
            Assert.Equal(MessageType.Error, response.Value!.Envelope.MessageType);
        }
    }

    [Fact]
    public async Task Agent_rejects_wrong_session()
    {
        var session = await StartAgentSessionAsync();
        await using (session.Client)
        using (session.Process)
        {
            var wrongSession = SessionId.New();
            var handshake = AgentPipeProtocol.CreateMessage(
                wrongSession,
                session.CorrelationId,
                1,
                session.Nonce,
                MessageType.HandshakeRequest,
                PayloadType.HandshakeRequest,
                new HandshakeRequestPayload(wrongSession, session.Nonce, DateTimeOffset.UtcNow),
                DateTimeOffset.UtcNow);

            Assert.True((await session.Client.WriteMessageAsync(handshake, CancellationToken.None)).IsSuccess);
            var response = await session.Client.ReadMessageAsync(CancellationToken.None);

            Assert.True(response.IsSuccess);
            Assert.Equal(MessageType.Error, response.Value!.Envelope.MessageType);
        }
    }

    [Fact]
    public async Task Agent_rejects_invalid_handshake_payload()
    {
        var session = await StartAgentSessionAsync();
        await using (session.Client)
        using (session.Process)
        {
            var handshake = AgentPipeProtocol.CreateMessage(
                session.SessionId,
                session.CorrelationId,
                1,
                session.Nonce,
                MessageType.HandshakeRequest,
                PayloadType.HandshakeRequest,
                new AgentStatusRequestPayload(),
                DateTimeOffset.UtcNow);

            Assert.True((await session.Client.WriteMessageAsync(handshake, CancellationToken.None)).IsSuccess);
            var response = await session.Client.ReadMessageAsync(CancellationToken.None);

            Assert.True(response.IsSuccess);
            Assert.Equal(MessageType.Error, response.Value!.Envelope.MessageType);
        }
    }

    [Fact]
    public async Task Agent_rejects_status_before_handshake()
    {
        var session = await StartAgentSessionAsync();
        await using (session.Client)
        using (session.Process)
        {
            var status = AgentPipeProtocol.CreateMessage(
                session.SessionId,
                session.CorrelationId,
                1,
                session.Nonce,
                MessageType.AgentStatusRequest,
                PayloadType.AgentStatusRequest,
                new AgentStatusRequestPayload(),
                DateTimeOffset.UtcNow);

            Assert.True((await session.Client.WriteMessageAsync(status, CancellationToken.None)).IsSuccess);
            var response = await session.Client.ReadMessageAsync(CancellationToken.None);

            Assert.True(response.IsSuccess);
            Assert.Equal(MessageType.Error, response.Value!.Envelope.MessageType);
        }
    }

    private static async Task<AgentSession> StartAgentSessionAsync()
    {
        var sessionId = SessionId.New();
        var nonce = AgentNonce.GenerateBootstrapNonce();
        var pipeName = AgentPipeName.CreateFullName(sessionId, AgentNonce.GeneratePipeToken());
        var agentPath = FindAgentExecutable();
        var process = StartAgent(agentPath, pipeName, sessionId, nonce);
        var clientResult = await NamedPipeAgentClient.ConnectAsync(pipeName, TimeSpan.FromSeconds(10), CancellationToken.None);
        Assert.True(clientResult.IsSuccess, clientResult.ErrorMessage);
        return new AgentSession(process, clientResult.Value!, sessionId, CorrelationId.New(), nonce);
    }

    private static Process StartAgent(string agentPath, string pipeName, SessionId sessionId, string nonce)
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
        startInfo.ArgumentList.Add(nonce);
        startInfo.ArgumentList.Add("--protocolVersion");
        startInfo.ArgumentList.Add(ProtocolVersion.Current.ToString());

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Agent process did not start.");
    }

    private static string FindAgentExecutable()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "src", "BorealBoost.Agent", "bin", "Debug", "net10.0-windows10.0.19041.0", "BorealBoost.Agent.exe");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Build the solution before running Agent IPC system tests.", path);
        }

        return path;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BorealBoost.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    private static async Task ShutdownAsync(NamedPipeAgentClient client, SessionId sessionId, CorrelationId correlationId, string nonce, long sequence)
    {
        var shutdown = AgentPipeProtocol.CreateMessage(
            sessionId,
            correlationId,
            sequence,
            nonce,
            MessageType.ShutdownRequest,
            PayloadType.ShutdownRequest,
            new ShutdownRequestPayload("test-complete"),
            DateTimeOffset.UtcNow);
        Assert.True((await client.WriteMessageAsync(shutdown, CancellationToken.None)).IsSuccess);
        var response = await client.ReadMessageAsync(CancellationToken.None);
        Assert.True(response.IsSuccess);
    }

    private sealed record AgentSession(
        Process Process,
        NamedPipeAgentClient Client,
        SessionId SessionId,
        CorrelationId CorrelationId,
        string Nonce);
}
