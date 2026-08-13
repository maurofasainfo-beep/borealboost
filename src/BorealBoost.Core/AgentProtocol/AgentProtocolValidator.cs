using BorealBoost.Core.Common;

namespace BorealBoost.Core.AgentProtocol;

public sealed class AgentProtocolValidator
{
    public const int MaxMessageBytes = 1024 * 1024;
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(2);

    public Result ValidateEnvelope(AgentMessageEnvelope envelope, DateTimeOffset nowUtc)
    {
        if (!envelope.ProtocolVersion.IsCompatibleWith(ProtocolVersion.Current))
        {
            return Result.Failure("protocol.version.unsupported", "Unsupported Agent protocol version.");
        }

        if (!Enum.IsDefined(envelope.MessageType) || !Enum.IsDefined(envelope.PayloadType))
        {
            return Result.Failure("protocol.type.unknown", "Unknown Agent protocol message or payload type.");
        }

        if (envelope.SessionId.Value == Guid.Empty)
        {
            return Result.Failure("protocol.session.invalid", "SessionId is required.");
        }

        if (envelope.CorrelationId.Value == Guid.Empty)
        {
            return Result.Failure("protocol.correlation.invalid", "CorrelationId is required.");
        }

        if (envelope.RequestId.Value == Guid.Empty)
        {
            return Result.Failure("protocol.request.invalid", "RequestId is required.");
        }

        if (envelope.SequenceNumber <= 0)
        {
            return Result.Failure("protocol.sequence.invalid", "SequenceNumber must be greater than zero.");
        }

        if (!AgentNonce.IsValidBootstrapNonce(envelope.Nonce))
        {
            return Result.Failure("protocol.nonce.invalid", "Nonce is invalid.");
        }

        if (envelope.PayloadSizeBytes is < 0 or > MaxMessageBytes)
        {
            return Result.Failure("protocol.payload.too_large", "Payload exceeds the Agent protocol size limit.");
        }

        if ((nowUtc - envelope.TimestampUtc).Duration() > MaxClockSkew)
        {
            return Result.Failure("protocol.timestamp.out_of_window", "Timestamp is outside the accepted Agent protocol window.");
        }

        if (!MessageMatchesPayload(envelope.MessageType, envelope.PayloadType))
        {
            return Result.Failure("protocol.payload.mismatch", "MessageType and PayloadType do not match.");
        }

        return Result.Success();
    }

    private static bool MessageMatchesPayload(MessageType messageType, PayloadType payloadType)
    {
        return messageType switch
        {
            MessageType.HandshakeRequest => payloadType == PayloadType.HandshakeRequest,
            MessageType.HandshakeResponse => payloadType == PayloadType.HandshakeResponse,
            MessageType.AgentStatusRequest => payloadType == PayloadType.AgentStatusRequest,
            MessageType.AgentStatusResponse => payloadType == PayloadType.AgentStatusResponse,
            MessageType.ValidateOperationRequest => payloadType == PayloadType.ValidateOperationRequest,
            MessageType.ValidateOperationResponse => payloadType == PayloadType.ValidateOperationResponse,
            MessageType.CaptureSnapshotRequest => payloadType == PayloadType.CaptureSnapshotRequest,
            MessageType.CaptureSnapshotResponse => payloadType == PayloadType.CaptureSnapshotResponse,
            MessageType.ExecuteOperationRequest => payloadType == PayloadType.ExecuteOperationRequest,
            MessageType.ExecuteOperationResponse => payloadType == PayloadType.ExecuteOperationResponse,
            MessageType.VerifyOperationRequest => payloadType == PayloadType.VerifyOperationRequest,
            MessageType.VerifyOperationResponse => payloadType == PayloadType.VerifyOperationResponse,
            MessageType.RollbackOperationRequest => payloadType == PayloadType.RollbackOperationRequest,
            MessageType.RollbackOperationResponse => payloadType == PayloadType.RollbackOperationResponse,
            MessageType.ShutdownRequest => payloadType == PayloadType.ShutdownRequest,
            MessageType.ShutdownResponse => payloadType == PayloadType.ShutdownResponse,
            MessageType.Error => payloadType == PayloadType.Error,
            _ => false
        };
    }
}
