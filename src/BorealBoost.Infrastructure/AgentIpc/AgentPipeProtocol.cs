using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Common;
using BorealBoost.Core.Identity;

namespace BorealBoost.Infrastructure.AgentIpc;

public static class AgentPipeProtocol
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AgentProtocolMessage CreateMessage(
        SessionId sessionId,
        CorrelationId correlationId,
        long sequenceNumber,
        string nonce,
        MessageType messageType,
        PayloadType payloadType,
        object payload,
        DateTimeOffset timestampUtc)
    {
        var payloadElement = JsonSerializer.SerializeToElement(payload, SerializerOptions);
        var payloadSizeBytes = JsonSerializer.SerializeToUtf8Bytes(payloadElement, SerializerOptions).Length;

        var envelope = new AgentMessageEnvelope(
            ProtocolVersion.Current,
            messageType,
            sessionId,
            correlationId,
            RequestId.New(),
            sequenceNumber,
            timestampUtc,
            nonce,
            payloadType,
            payloadSizeBytes);

        return new AgentProtocolMessage(envelope, payloadElement);
    }

    public static async Task<Result> WriteMessageAsync(
        Stream stream,
        AgentProtocolMessage message,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        try
        {
            payload = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        }
        catch (JsonException exception)
        {
            return Result.Failure("protocol.serialize.failed", exception.Message);
        }

        if (payload.Length is <= 0 or > AgentProtocolValidator.MaxMessageBytes)
        {
            return Result.Failure("protocol.message.too_large", "Agent protocol message exceeds the size limit.");
        }

        var lengthPrefix = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, payload.Length);

        try
        {
            await stream.WriteAsync(lengthPrefix, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or OperationCanceledException)
        {
            return Result.Failure("protocol.write.failed", exception.Message);
        }
    }

    public static async Task<Result<AgentProtocolMessage>> ReadMessageAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var lengthPrefix = new byte[sizeof(int)];
        try
        {
            await stream.ReadExactlyAsync(lengthPrefix, cancellationToken).ConfigureAwait(false);
            var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(lengthPrefix);
            if (payloadLength is <= 0 or > AgentProtocolValidator.MaxMessageBytes)
            {
                return Result<AgentProtocolMessage>.Failure(
                    "protocol.message.size_invalid",
                    "Agent protocol message size is invalid.");
            }

            var payload = new byte[payloadLength];
            await stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);

            var message = JsonSerializer.Deserialize<AgentProtocolMessage>(payload, SerializerOptions);
            return message is null
                ? Result<AgentProtocolMessage>.Failure("protocol.message.empty", "Agent protocol message is empty.")
                : Result<AgentProtocolMessage>.Success(message);
        }
        catch (JsonException exception)
        {
            return Result<AgentProtocolMessage>.Failure("protocol.message.malformed", exception.Message);
        }
        catch (Exception exception) when (exception is EndOfStreamException or IOException or ObjectDisposedException or OperationCanceledException)
        {
            return Result<AgentProtocolMessage>.Failure("protocol.read.failed", exception.Message);
        }
    }

    public static Result<TPayload> DeserializePayload<TPayload>(AgentProtocolMessage message)
    {
        try
        {
            var payload = message.Payload.Deserialize<TPayload>(SerializerOptions);
            return payload is null
                ? Result<TPayload>.Failure("protocol.payload.empty", "Agent protocol payload is empty.")
                : Result<TPayload>.Success(payload);
        }
        catch (JsonException exception)
        {
            return Result<TPayload>.Failure("protocol.payload.malformed", exception.Message);
        }
    }
}
