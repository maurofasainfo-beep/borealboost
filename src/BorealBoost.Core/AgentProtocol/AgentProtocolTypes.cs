using BorealBoost.Core.Identity;
using BorealBoost.Core.Optimization;
using System.Text.Json;

namespace BorealBoost.Core.AgentProtocol;

public enum MessageType
{
    HandshakeRequest,
    HandshakeResponse,
    AgentStatusRequest,
    AgentStatusResponse,
    ValidateOperationRequest,
    ValidateOperationResponse,
    CaptureSnapshotRequest,
    CaptureSnapshotResponse,
    ExecuteOperationRequest,
    ExecuteOperationResponse,
    VerifyOperationRequest,
    VerifyOperationResponse,
    RollbackOperationRequest,
    RollbackOperationResponse,
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
    ValidateOperationRequest,
    ValidateOperationResponse,
    CaptureSnapshotRequest,
    CaptureSnapshotResponse,
    ExecuteOperationRequest,
    ExecuteOperationResponse,
    VerifyOperationRequest,
    VerifyOperationResponse,
    RollbackOperationRequest,
    RollbackOperationResponse,
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

public sealed record AgentStatusResponsePayload(string AgentVersion, bool AcceptsPrivilegedOperations, bool IsElevated)
    : AgentPayload(PayloadType.AgentStatusResponse);

public sealed record ValidateOperationRequestPayload(
    string PlanSchemaVersion,
    string CatalogVersion,
    OptimizationId OptimizationId,
    OperationSpec Operation)
    : AgentPayload(PayloadType.ValidateOperationRequest);

public sealed record ValidateOperationResponsePayload(
    bool Accepted,
    IReadOnlyList<OptimizationIssue> Issues,
    IReadOnlyList<OperationType> SupportedOperationTypes)
    : AgentPayload(PayloadType.ValidateOperationResponse);

public sealed record CaptureSnapshotRequestPayload(
    string PlanSchemaVersion,
    string CatalogVersion,
    OptimizationId OptimizationId,
    OperationSpec Operation)
    : AgentPayload(PayloadType.CaptureSnapshotRequest);

public sealed record CaptureSnapshotResponsePayload(
    bool Captured,
    OperationSnapshotItem? SnapshotItem,
    IReadOnlyList<OptimizationIssue> Issues)
    : AgentPayload(PayloadType.CaptureSnapshotResponse);

public sealed record ExecuteOperationRequestPayload(
    string PlanSchemaVersion,
    string CatalogVersion,
    OptimizationId OptimizationId,
    OperationSpec Operation,
    OperationSnapshotItem SnapshotItem)
    : AgentPayload(PayloadType.ExecuteOperationRequest);

public sealed record ExecuteOperationResponsePayload(
    OperationExecutionResult? Result,
    IReadOnlyList<OptimizationIssue> Issues)
    : AgentPayload(PayloadType.ExecuteOperationResponse);

public sealed record VerifyOperationRequestPayload(
    string PlanSchemaVersion,
    string CatalogVersion,
    OptimizationId OptimizationId,
    OperationSpec Operation)
    : AgentPayload(PayloadType.VerifyOperationRequest);

public sealed record VerifyOperationResponsePayload(
    OperationVerificationResult? Result,
    IReadOnlyList<OptimizationIssue> Issues)
    : AgentPayload(PayloadType.VerifyOperationResponse);

public sealed record RollbackOperationRequestPayload(
    string PlanSchemaVersion,
    string CatalogVersion,
    OptimizationId OptimizationId,
    OperationSpec Operation,
    OperationSnapshotItem SnapshotItem)
    : AgentPayload(PayloadType.RollbackOperationRequest);

public sealed record RollbackOperationResponsePayload(
    OperationRollbackResult? Result,
    IReadOnlyList<OptimizationIssue> Issues)
    : AgentPayload(PayloadType.RollbackOperationResponse);

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
