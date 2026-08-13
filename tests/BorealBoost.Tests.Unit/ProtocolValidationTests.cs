using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Identity;

namespace BorealBoost.Tests.Unit;

public sealed class ProtocolValidationTests
{
    [Fact]
    public void Current_protocol_version_parses_and_is_compatible()
    {
        var parsed = ProtocolVersion.TryParse("1.0.0", out var version);

        Assert.True(parsed);
        Assert.True(version.IsCompatibleWith(ProtocolVersion.Current));
    }

    [Fact]
    public void Validator_rejects_unsupported_major_version()
    {
        var validator = new AgentProtocolValidator();
        var envelope = CreateValidEnvelope() with
        {
            ProtocolVersion = new ProtocolVersion(2, 0, 0)
        };

        var result = validator.ValidateEnvelope(envelope, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.version.unsupported", result.ErrorCode);
    }

    [Fact]
    public void Validator_rejects_payload_mismatch()
    {
        var validator = new AgentProtocolValidator();
        var envelope = CreateValidEnvelope() with
        {
            MessageType = MessageType.HandshakeRequest,
            PayloadType = PayloadType.AgentStatusRequest
        };

        var result = validator.ValidateEnvelope(envelope, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.payload.mismatch", result.ErrorCode);
    }

    [Fact]
    public void Validator_rejects_payload_over_size_limit()
    {
        var validator = new AgentProtocolValidator();
        var envelope = CreateValidEnvelope() with
        {
            PayloadSizeBytes = AgentProtocolValidator.MaxMessageBytes + 1
        };

        var result = validator.ValidateEnvelope(envelope, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.payload.too_large", result.ErrorCode);
    }

    [Fact]
    public void Validator_rejects_unknown_message_type()
    {
        var validator = new AgentProtocolValidator();
        var envelope = CreateValidEnvelope() with
        {
            MessageType = (MessageType)999
        };

        var result = validator.ValidateEnvelope(envelope, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.type.unknown", result.ErrorCode);
    }

    [Fact]
    public void Session_validator_rejects_wrong_session()
    {
        var nonce = AgentNonce.GenerateBootstrapNonce();
        var validator = new AgentProtocolSessionValidator(SessionId.New(), nonce, DateTimeOffset.UtcNow.AddMinutes(1));
        var envelope = CreateValidEnvelope() with
        {
            Nonce = nonce
        };

        var result = validator.ValidateNext(envelope, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.session.mismatch", result.ErrorCode);
    }

    [Fact]
    public void Session_validator_rejects_wrong_nonce()
    {
        var sessionId = SessionId.New();
        var validator = new AgentProtocolSessionValidator(sessionId, AgentNonce.GenerateBootstrapNonce(), DateTimeOffset.UtcNow.AddMinutes(1));
        var envelope = CreateValidEnvelope() with
        {
            SessionId = sessionId,
            Nonce = AgentNonce.GenerateBootstrapNonce()
        };

        var result = validator.ValidateNext(envelope, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.nonce.mismatch", result.ErrorCode);
    }

    [Fact]
    public void Session_validator_rejects_duplicate_request()
    {
        var sessionId = SessionId.New();
        var nonce = AgentNonce.GenerateBootstrapNonce();
        var validator = new AgentProtocolSessionValidator(sessionId, nonce, DateTimeOffset.UtcNow.AddMinutes(1));
        var envelope = CreateValidEnvelope() with
        {
            SessionId = sessionId,
            Nonce = nonce
        };

        Assert.True(validator.ValidateNext(envelope, DateTimeOffset.UtcNow).IsSuccess);
        var result = validator.ValidateNext(envelope, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.request.replay", result.ErrorCode);
    }

    [Fact]
    public void Session_validator_rejects_expired_request()
    {
        var sessionId = SessionId.New();
        var nonce = AgentNonce.GenerateBootstrapNonce();
        var validator = new AgentProtocolSessionValidator(sessionId, nonce, DateTimeOffset.UtcNow.AddSeconds(-1));
        var envelope = CreateValidEnvelope() with
        {
            SessionId = sessionId,
            Nonce = nonce
        };

        var result = validator.ValidateNext(envelope, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.session.expired", result.ErrorCode);
    }

    private static AgentMessageEnvelope CreateValidEnvelope()
    {
        var now = DateTimeOffset.UtcNow;
        return new AgentMessageEnvelope(
            ProtocolVersion.Current,
            MessageType.HandshakeRequest,
            SessionId.New(),
            CorrelationId.New(),
            RequestId.New(),
            1,
            now,
            AgentNonce.GenerateBootstrapNonce(),
            PayloadType.HandshakeRequest,
            256);
    }
}
