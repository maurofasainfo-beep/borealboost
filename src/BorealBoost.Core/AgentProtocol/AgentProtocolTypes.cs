using BorealBoost.Core.Identity;
using System.Text.Json;

namespace BorealBoost.Core.AgentProtocol;

public enum MessageType
{
    HandshakeRequest,
    HandshakeResponse,
    AgentStatusRequest,
    AgentStatusResponse,
    ShutdownRequest,
    ShutdownResponse,
    Error
}

public enum PayloadType
{
    None,
    HandshakeRequest,
    HandshakeResponse,
    AgentStatusRequest,
    AgentStatusResponse,
    ShutdownRequest,
    ShutdownResponse,
    Error
}

public sealed record AgentProtocolError(string Code, string Message);

public abstract record AgentPayload(PayloadType PayloadType);

public sealed record HandshakeRequestPayload(SessionId SessionId, string BootstrapNonce, DateTimeOffset TimestampUtc)
    : AgentPayload(PayloadType.HandshakeRequest);

public sealed record HandshakeResponsePayload(string AgentVersion, ProtocolVersion NegotiatedProtocolVersion)
    : AgentPayload(PayloadType.HandshakeResponse);

public sealed record AgentStatusRequestPayload()
    : AgentPayload(PayloadType.AgentStatusRequest);

public sealed record AgentStatusResponsePayload(string AgentVersion, bool AcceptsPrivilegedOperations)
    : AgentPayload(PayloadType.AgentStatusResponse);

public sealed record ShutdownRequestPayload(string Reason)
    : AgentPayload(PayloadType.ShutdownRequest);

public sealed record ShutdownResponsePayload(bool Accepted)
    : AgentPayload(PayloadType.ShutdownResponse);

public sealed record ErrorPayload(AgentProtocolError Error)
    : AgentPayload(PayloadType.Error);

public sealed record AgentMessageEnvelope(
    ProtocolVersion ProtocolVersion,
    MessageType MessageType,
    SessionId SessionId,
    CorrelationId CorrelationId,
    RequestId RequestId,
    long SequenceNumber,
    DateTimeOffset TimestampUtc,
    string Nonce,
    PayloadType PayloadType,
    int PayloadSizeBytes);

public sealed record AgentProtocolMessage(AgentMessageEnvelope Envelope, JsonElement Payload);
