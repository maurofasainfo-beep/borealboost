using BorealBoost.Core.Common;
using BorealBoost.Core.Identity;

namespace BorealBoost.Core.AgentProtocol;

public sealed class AgentProtocolSessionValidator
{
    private readonly AgentProtocolValidator _envelopeValidator = new();
    private readonly HashSet<RequestId> _processedRequests = [];
    private readonly SessionId _sessionId;
    private readonly string _sessionNonce;
    private readonly DateTimeOffset _expiresUtc;
    private long _lastSequenceNumber;

    public AgentProtocolSessionValidator(SessionId sessionId, string sessionNonce, DateTimeOffset expiresUtc)
    {
        if (sessionId.Value == Guid.Empty)
        {
            throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));
        }

        if (!AgentNonce.IsValidBootstrapNonce(sessionNonce))
        {
            throw new ArgumentException("Session nonce is invalid.", nameof(sessionNonce));
        }

        _sessionId = sessionId;
        _sessionNonce = sessionNonce;
        _expiresUtc = expiresUtc;
    }

    public Result ValidateNext(AgentMessageEnvelope envelope, DateTimeOffset nowUtc)
    {
        var envelopeResult = _envelopeValidator.ValidateEnvelope(envelope, nowUtc);
        if (envelopeResult.IsFailure)
        {
            return envelopeResult;
        }

        if (nowUtc > _expiresUtc)
        {
            return Result.Failure("protocol.session.expired", "Agent session has expired.");
        }

        if (envelope.SessionId != _sessionId)
        {
            return Result.Failure("protocol.session.mismatch", "Agent message session does not match bootstrap session.");
        }

        if (!string.Equals(envelope.Nonce, _sessionNonce, StringComparison.Ordinal))
        {
            return Result.Failure("protocol.nonce.mismatch", "Agent message nonce does not match bootstrap nonce.");
        }

        if (_processedRequests.Contains(envelope.RequestId))
        {
            return Result.Failure("protocol.request.replay", "Agent requestId was already processed in this session.");
        }

        if (envelope.SequenceNumber <= _lastSequenceNumber)
        {
            return Result.Failure("protocol.sequence.replay", "Agent sequenceNumber must be strictly increasing.");
        }

        _processedRequests.Add(envelope.RequestId);
        _lastSequenceNumber = envelope.SequenceNumber;
        return Result.Success();
    }
}
