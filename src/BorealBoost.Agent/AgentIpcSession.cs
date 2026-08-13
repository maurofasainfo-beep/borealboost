using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Common;
using BorealBoost.Core.Foundation;
using BorealBoost.Core.Identity;
using BorealBoost.Infrastructure.AgentIpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Agent;

public sealed class AgentIpcSession
{
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    private readonly AgentBootstrapOptions _options;
    private readonly IApplicationInfoProvider _applicationInfoProvider;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<AgentIpcSession> _logger;

    public AgentIpcSession(
        AgentBootstrapOptions options,
        IApplicationInfoProvider applicationInfoProvider,
        IHostApplicationLifetime applicationLifetime,
        ILogger<AgentIpcSession> logger)
    {
        _options = options;
        _applicationInfoProvider = applicationInfoProvider;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_options.LocalPipeName is null ||
            _options.SessionId is null ||
            _options.BootstrapNonce is null ||
            _options.ProtocolVersion is null)
        {
            throw new InvalidOperationException("Agent IPC session requires validated bootstrap options.");
        }

        var localPipeName = _options.LocalPipeName;
        var sessionId = _options.SessionId.Value;
        var bootstrapNonce = _options.BootstrapNonce;
        var protocolVersion = _options.ProtocolVersion.Value;

        await using var server = CreateServerStream(_options.LocalPipeName);

        _logger.LogInformation(
            "Agent IPC session waiting. SessionId={SessionId}; Protocol={ProtocolVersion}",
            sessionId,
            protocolVersion);

        try
        {
            using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectTimeout.CancelAfter(ConnectTimeout);
            await server.WaitForConnectionAsync(connectTimeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Agent IPC connection timed out. SessionId={SessionId}", sessionId);
            _applicationLifetime.StopApplication();
            return;
        }

        var sessionValidator = new AgentProtocolSessionValidator(
            sessionId,
            bootstrapNonce,
            DateTimeOffset.UtcNow.Add(IdleTimeout));

        var handshakeAccepted = false;
        while (!cancellationToken.IsCancellationRequested && server.IsConnected)
        {
            using var idleTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            idleTimeout.CancelAfter(IdleTimeout);

            var readResult = await AgentPipeProtocol.ReadMessageAsync(server, idleTimeout.Token).ConfigureAwait(false);
            if (readResult.IsFailure || readResult.Value is null)
            {
                _logger.LogWarning(
                    "Agent IPC read failed. SessionId={SessionId}; ErrorCode={ErrorCode}",
                    _options.SessionId,
                    readResult.ErrorCode);
                break;
            }

            var validation = sessionValidator.ValidateNext(readResult.Value.Envelope, DateTimeOffset.UtcNow);
            if (validation.IsFailure)
            {
                await WriteErrorAsync(server, readResult.Value.Envelope, validation.ErrorCode ?? "protocol.invalid", validation.ErrorMessage ?? "Invalid protocol message", cancellationToken)
                    .ConfigureAwait(false);
                break;
            }

            if (!handshakeAccepted && readResult.Value.Envelope.MessageType != MessageType.HandshakeRequest)
            {
                await WriteErrorAsync(server, readResult.Value.Envelope, "protocol.handshake.required", "Handshake is required before other requests.", cancellationToken)
                    .ConfigureAwait(false);
                break;
            }

            var shouldStop = await HandleMessageAsync(server, readResult.Value, cancellationToken).ConfigureAwait(false);
            handshakeAccepted = handshakeAccepted || readResult.Value.Envelope.MessageType == MessageType.HandshakeRequest;
            if (shouldStop)
            {
                break;
            }
        }

        _logger.LogInformation("Agent IPC session ended. SessionId={SessionId}", sessionId);
        _applicationLifetime.StopApplication();
    }

    private async Task<bool> HandleMessageAsync(
        Stream stream,
        AgentProtocolMessage message,
        CancellationToken cancellationToken)
    {
        var appInfo = _applicationInfoProvider.GetApplicationInfo();

        switch (message.Envelope.MessageType)
        {
            case MessageType.HandshakeRequest:
            {
                var payload = AgentPipeProtocol.DeserializePayload<HandshakeRequestPayload>(message);
                if (payload.IsFailure || payload.Value is null ||
                    payload.Value.SessionId != _options.SessionId ||
                    !string.Equals(payload.Value.BootstrapNonce, _options.BootstrapNonce, StringComparison.Ordinal))
                {
                    await WriteErrorAsync(stream, message.Envelope, "protocol.handshake.invalid", "Handshake payload is invalid.", cancellationToken)
                        .ConfigureAwait(false);
                    return true;
                }

                var response = CreateResponse(
                    message.Envelope,
                    MessageType.HandshakeResponse,
                    PayloadType.HandshakeResponse,
                    new HandshakeResponsePayload(appInfo.Version.ToString(), ProtocolVersion.Current));
                await AgentPipeProtocol.WriteMessageAsync(stream, response, cancellationToken).ConfigureAwait(false);
                return false;
            }

            case MessageType.AgentStatusRequest:
            {
                var payload = AgentPipeProtocol.DeserializePayload<AgentStatusRequestPayload>(message);
                if (payload.IsFailure)
                {
                    await WriteErrorAsync(stream, message.Envelope, payload.ErrorCode ?? "protocol.payload.invalid", payload.ErrorMessage ?? "Invalid payload.", cancellationToken)
                        .ConfigureAwait(false);
                    return true;
                }

                var response = CreateResponse(
                    message.Envelope,
                    MessageType.AgentStatusResponse,
                    PayloadType.AgentStatusResponse,
                    new AgentStatusResponsePayload(appInfo.Version.ToString(), AcceptsPrivilegedOperations: false));
                await AgentPipeProtocol.WriteMessageAsync(stream, response, cancellationToken).ConfigureAwait(false);
                return false;
            }

            case MessageType.ShutdownRequest:
            {
                var response = CreateResponse(
                    message.Envelope,
                    MessageType.ShutdownResponse,
                    PayloadType.ShutdownResponse,
                    new ShutdownResponsePayload(Accepted: true));
                await AgentPipeProtocol.WriteMessageAsync(stream, response, cancellationToken).ConfigureAwait(false);
                return true;
            }

            default:
                await WriteErrorAsync(stream, message.Envelope, "protocol.message.not_allowed", "Message type is not allowed in Foundation.", cancellationToken)
                    .ConfigureAwait(false);
                return true;
        }
    }

    private static AgentProtocolMessage CreateResponse(
        AgentMessageEnvelope request,
        MessageType responseType,
        PayloadType payloadType,
        object payload)
    {
        return AgentPipeProtocol.CreateMessage(
            request.SessionId,
            request.CorrelationId,
            request.SequenceNumber + 1,
            request.Nonce,
            responseType,
            payloadType,
            payload,
            DateTimeOffset.UtcNow);
    }

    private static Task<Result> WriteErrorAsync(
        Stream stream,
        AgentMessageEnvelope request,
        string code,
        string message,
        CancellationToken cancellationToken)
    {
        var response = CreateResponse(
            request,
            MessageType.Error,
            PayloadType.Error,
            new ErrorPayload(new AgentProtocolError(code, message)));
        return AgentPipeProtocol.WriteMessageAsync(stream, response, cancellationToken);
    }

    private static NamedPipeServerStream CreateServerStream(string localPipeName)
    {
        var security = new PipeSecurity();
        var currentUser = WindowsIdentity.GetCurrent().User;

        if (currentUser is not null)
        {
            security.AddAccessRule(new PipeAccessRule(
                currentUser,
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
        }

        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            localPipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            inBufferSize: 0,
            outBufferSize: 0,
            pipeSecurity: security);
    }
}
