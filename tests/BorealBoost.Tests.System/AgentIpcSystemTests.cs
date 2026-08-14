using System.Diagnostics;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Identity;
using BorealBoost.Core.Optimization;
using BorealBoost.Infrastructure.AgentIpc;
using BorealBoost.Optimization.Catalog;
using BorealBoost.Optimization.Execution;
using BorealBoost.System.Operations;
using Microsoft.Win32;

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
            Assert.Equal(IsCurrentProcessElevated(), payload.Value!.IsElevated);
            Assert.Equal(payload.Value.IsElevated, payload.Value.AcceptsPrivilegedOperations);

            await ShutdownAsync(session.Client, session.SessionId, session.CorrelationId, session.Nonce, 5);
            await session.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, session.Process.ExitCode);
        }
    }

    [Fact]
    public async Task Agent_validates_typed_operation_and_rejects_unknown_operation()
    {
        var session = await StartAgentSessionAsync();
        await using (session.Client)
        using (session.Process)
        {
            await HandshakeAsync(session);
            var operation = BuiltInOperation();
            var request = AgentPipeProtocol.CreateMessage(
                session.SessionId,
                session.CorrelationId,
                3,
                session.Nonce,
                MessageType.ValidateOperationRequest,
                PayloadType.ValidateOperationRequest,
                new ValidateOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation),
                DateTimeOffset.UtcNow);

            Assert.True((await session.Client.WriteMessageAsync(request, CancellationToken.None)).IsSuccess);
            var response = await session.Client.ReadMessageAsync(CancellationToken.None);
            Assert.True(response.IsSuccess);
            Assert.Equal(MessageType.ValidateOperationResponse, response.Value!.Envelope.MessageType);
            var payload = AgentPipeProtocol.DeserializePayload<ValidateOperationResponsePayload>(response.Value);
            Assert.True(payload.IsSuccess);
            Assert.True(payload.Value!.Accepted);

            var unknownOperation = operation with { OperationType = (OperationType)999 };
            var rejectedRequest = AgentPipeProtocol.CreateMessage(
                session.SessionId,
                session.CorrelationId,
                5,
                session.Nonce,
                MessageType.ValidateOperationRequest,
                PayloadType.ValidateOperationRequest,
                new ValidateOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, BuiltInOptimizationCatalog.IntegrationProofOptimizationId, unknownOperation),
                DateTimeOffset.UtcNow);

            Assert.True((await session.Client.WriteMessageAsync(rejectedRequest, CancellationToken.None)).IsSuccess);
            var rejectedResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
            Assert.True(rejectedResponse.IsSuccess);
            var rejectedPayload = AgentPipeProtocol.DeserializePayload<ValidateOperationResponsePayload>(rejectedResponse.Value!);
            Assert.True(rejectedPayload.IsSuccess);
            Assert.False(rejectedPayload.Value!.Accepted);
            Assert.Contains(rejectedPayload.Value.Issues, issue => issue.Code == "agent.operation.type_unknown");

            var invalidOptimizationRequest = AgentPipeProtocol.CreateMessage(
                session.SessionId,
                session.CorrelationId,
                7,
                session.Nonce,
                MessageType.ValidateOperationRequest,
                PayloadType.ValidateOperationRequest,
                new ValidateOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, new OptimizationId("BAD"), operation),
                DateTimeOffset.UtcNow);

            Assert.True((await session.Client.WriteMessageAsync(invalidOptimizationRequest, CancellationToken.None)).IsSuccess);
            var invalidOptimizationResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
            Assert.True(invalidOptimizationResponse.IsSuccess);
            var invalidOptimizationPayload = AgentPipeProtocol.DeserializePayload<ValidateOperationResponsePayload>(invalidOptimizationResponse.Value!);
            Assert.True(invalidOptimizationPayload.IsSuccess);
            Assert.False(invalidOptimizationPayload.Value!.Accepted);
            Assert.Contains(invalidOptimizationPayload.Value.Issues, issue => issue.Code == "agent.optimization_id_invalid");

            await ShutdownAsync(session.Client, session.SessionId, session.CorrelationId, session.Nonce, 9);
        }
    }

    [Fact]
    public async Task Agent_executes_controlled_registry_operation_with_snapshot_verify_and_rollback()
    {
        using var registryScope = ControlledRegistryTestScope.Acquire();
        var backup = RegistryBackup.Capture();
        try
        {
            var session = await StartAgentSessionAsync();
            await using (session.Client)
            using (session.Process)
            {
                await HandshakeAsync(session);
                var operation = BuiltInOperation();

                var capture = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    3,
                    session.Nonce,
                    MessageType.CaptureSnapshotRequest,
                    PayloadType.CaptureSnapshotRequest,
                    new CaptureSnapshotRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation),
                    DateTimeOffset.UtcNow);
                Assert.True((await session.Client.WriteMessageAsync(capture, CancellationToken.None)).IsSuccess);
                var captureResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var capturePayload = AgentPipeProtocol.DeserializePayload<CaptureSnapshotResponsePayload>(captureResponse.Value!);
                Assert.True(capturePayload.IsSuccess);
                Assert.True(capturePayload.Value!.Captured);
                Assert.NotNull(capturePayload.Value.SnapshotItem);

                var execute = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    5,
                    session.Nonce,
                    MessageType.ExecuteOperationRequest,
                    PayloadType.ExecuteOperationRequest,
                    new ExecuteOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation, capturePayload.Value.SnapshotItem!),
                    DateTimeOffset.UtcNow);
                Assert.True((await session.Client.WriteMessageAsync(execute, CancellationToken.None)).IsSuccess);
                var executeResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var executePayload = AgentPipeProtocol.DeserializePayload<ExecuteOperationResponsePayload>(executeResponse.Value!);
                Assert.True(executePayload.IsSuccess);
                Assert.NotNull(executePayload.Value!.Result);
                Assert.True(executePayload.Value.Result!.Status is OperationExecutionStatus.Applied or OperationExecutionStatus.AlreadySatisfied);

                var verify = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    7,
                    session.Nonce,
                    MessageType.VerifyOperationRequest,
                    PayloadType.VerifyOperationRequest,
                    new VerifyOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation),
                    DateTimeOffset.UtcNow);
                Assert.True((await session.Client.WriteMessageAsync(verify, CancellationToken.None)).IsSuccess);
                var verifyResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var verifyPayload = AgentPipeProtocol.DeserializePayload<VerifyOperationResponsePayload>(verifyResponse.Value!);
                Assert.True(verifyPayload.IsSuccess);
                Assert.True(verifyPayload.Value!.Result!.Verified);

                var rollback = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    9,
                    session.Nonce,
                    MessageType.RollbackOperationRequest,
                    PayloadType.RollbackOperationRequest,
                    new RollbackOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation, capturePayload.Value.SnapshotItem!),
                    DateTimeOffset.UtcNow);
                Assert.True((await session.Client.WriteMessageAsync(rollback, CancellationToken.None)).IsSuccess);
                var rollbackResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var rollbackPayload = AgentPipeProtocol.DeserializePayload<RollbackOperationResponsePayload>(rollbackResponse.Value!);
                Assert.True(rollbackPayload.IsSuccess);
                Assert.True(rollbackPayload.Value!.Result!.RestoredOriginalState);
                backup.AssertRestored();

                await ShutdownAsync(session.Client, session.SessionId, session.CorrelationId, session.Nonce, 11);
            }
        }
        finally
        {
            backup.Restore();
        }
    }

    [Theory]
    [MemberData(nameof(SupportedRegistryValueCases))]
    public async Task Controlled_registry_handler_restores_exact_supported_registry_value_kind(
        RegistryValueKind kind,
        object value,
        RegistryValueDataKind expectedKind)
    {
        using var registryScope = ControlledRegistryTestScope.Acquire();
        var backup = RegistryBackup.Capture();
        try
        {
            SetControlledValue(value, kind);
            var handler = new BorealIntegrationRegistryOperationHandler();
            var operation = BuiltInOperation();

            var capture = await handler.CaptureSnapshotAsync(operation, CancellationToken.None);
            Assert.True(capture.IsSuccess, capture.ErrorMessage);
            Assert.True(capture.Value!.ExistedBefore);
            Assert.Equal(expectedKind, capture.Value.PreviousValueKind);

            var apply = await handler.ApplyAsync(operation, capture.Value, CancellationToken.None);
            Assert.True(apply.IsSuccess, apply.ErrorMessage);
            Assert.True(apply.Value!.Status is OperationExecutionStatus.Applied or OperationExecutionStatus.AlreadySatisfied);
            AssertControlledValue(RegistryValueKind.String, BuiltInOptimizationCatalog.IntegrationProofValue);

            var verify = await handler.VerifyAsync(operation, CancellationToken.None);
            Assert.True(verify.Value!.Verified);

            var rollback = await handler.RollbackAsync(operation, capture.Value, CancellationToken.None);
            Assert.True(rollback.IsSuccess, rollback.ErrorMessage);
            Assert.True(rollback.Value!.RestoredOriginalState);
            AssertControlledValue(kind, value);
        }
        finally
        {
            backup.Restore();
        }
    }

    [Fact]
    public async Task Controlled_registry_handler_restores_original_absence()
    {
        using var registryScope = ControlledRegistryTestScope.Acquire();
        var backup = RegistryBackup.Capture();
        try
        {
            DeleteControlledValue();
            var handler = new BorealIntegrationRegistryOperationHandler();
            var operation = BuiltInOperation();

            var capture = await handler.CaptureSnapshotAsync(operation, CancellationToken.None);
            Assert.True(capture.IsSuccess, capture.ErrorMessage);
            Assert.False(capture.Value!.ExistedBefore);

            var apply = await handler.ApplyAsync(operation, capture.Value, CancellationToken.None);
            Assert.True(apply.IsSuccess, apply.ErrorMessage);
            AssertControlledValue(RegistryValueKind.String, BuiltInOptimizationCatalog.IntegrationProofValue);

            var rollback = await handler.RollbackAsync(operation, capture.Value, CancellationToken.None);
            Assert.True(rollback.Value!.RestoredOriginalState);
            AssertControlledValueAbsent();
        }
        finally
        {
            backup.Restore();
        }
    }

    [Fact]
    public async Task Controlled_registry_handler_restores_original_key_absence()
    {
        using var registryScope = ControlledRegistryTestScope.Acquire();
        var operation = BuiltInOperation();
        var target = operation.RegistryValue!.Target;
        var backup = RegistryValueBackup.Capture(target);
        try
        {
            AssertControlledKeyContainsOnlyTestValue();
            DeleteControlledKey();
            var handler = new BorealIntegrationRegistryOperationHandler();

            var capture = await handler.CaptureSnapshotAsync(operation, CancellationToken.None);
            Assert.True(capture.IsSuccess, capture.ErrorMessage);
            Assert.False(capture.Value!.RegistryKeyExistedBefore);
            Assert.False(capture.Value.ExistedBefore);

            var apply = await handler.ApplyAsync(operation, capture.Value, CancellationToken.None);
            Assert.True(apply.IsSuccess, apply.ErrorMessage);
            AssertControlledValue(RegistryValueKind.String, BuiltInOptimizationCatalog.IntegrationProofValue);

            var rollback = await handler.RollbackAsync(operation, capture.Value, CancellationToken.None);
            Assert.True(rollback.IsSuccess, rollback.ErrorMessage);
            Assert.True(rollback.Value!.RestoredOriginalState);
            AssertControlledKeyAbsent();
        }
        finally
        {
            backup.Restore();
        }
    }

    [Fact]
    public async Task Controlled_registry_handler_rejects_unsupported_desired_kind_before_apply()
    {
        using var registryScope = ControlledRegistryTestScope.Acquire();
        var backup = RegistryBackup.Capture();
        try
        {
            DeleteControlledValue();
            var operation = BuiltInOperation() with
            {
                RegistryValue = BuiltInOperation().RegistryValue! with
                {
                    DesiredState = new RegistryValueState(true, RegistryValueDataKind.Unsupported, null, null)
                }
            };
            var handler = new BorealIntegrationRegistryOperationHandler();
            var snapshot = new OperationSnapshotItem(
                Guid.NewGuid(),
                operation.OperationId,
                OperationResourceType.RegistryValue,
                $"{operation.RegistryValue!.Target.Hive}\\{operation.RegistryValue.Target.KeyPath}\\{operation.RegistryValue.Target.ValueName}",
                false,
                operation.RegistryValue.Target,
                null,
                null,
                null,
                "test",
                DateTimeOffset.UtcNow,
                operation.RollbackStrategy,
                [],
                "test");

            var apply = await handler.ApplyAsync(operation, snapshot, CancellationToken.None);

            Assert.True(apply.IsFailure);
            Assert.Equal("agent.operation.value_kind_unsupported", apply.ErrorCode);
            AssertControlledValueAbsent();
        }
        finally
        {
            backup.Restore();
        }
    }

    [Fact]
    public async Task Controlled_registry_rollback_rejects_external_state_change()
    {
        using var registryScope = ControlledRegistryTestScope.Acquire();
        var backup = RegistryBackup.Capture();
        try
        {
            SetControlledValue("original", RegistryValueKind.String);
            var handler = new BorealIntegrationRegistryOperationHandler();
            var operation = BuiltInOperation();
            var capture = await handler.CaptureSnapshotAsync(operation, CancellationToken.None);
            Assert.True(capture.IsSuccess, capture.ErrorMessage);
            Assert.True((await handler.ApplyAsync(operation, capture.Value!, CancellationToken.None)).IsSuccess);

            SetControlledValue(1234L, RegistryValueKind.QWord);
            var rollback = await handler.RollbackAsync(operation, capture.Value!, CancellationToken.None);

            Assert.True(rollback.IsSuccess, rollback.ErrorMessage);
            Assert.False(rollback.Value!.RestoredOriginalState);
            Assert.Equal(OperationErrorCategory.OutcomeUnknown, rollback.Value.ErrorCategory);
            AssertControlledValue(RegistryValueKind.QWord, 1234L);
        }
        finally
        {
            backup.Restore();
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

    [Fact]
    public async Task Agent_rejects_canonical_operation_tampering()
    {
        var session = await StartAgentSessionAsync();
        await using (session.Client)
        using (session.Process)
        {
            await HandshakeAsync(session);
            var operation = BuiltInOperation();
            var targetTamper = operation with
            {
                RegistryValue = operation.RegistryValue! with
                {
                    Target = operation.RegistryValue.Target with { KeyPath = @"Software\BorealBoost\IntegrationTest\Other" }
                }
            };
            await AssertValidateRejectedAsync(session, targetTamper, BuiltInOptimizationCatalog.CurrentCatalogVersion, "agent.catalog.operation_mismatch", 3);

            var desiredTamper = operation with
            {
                RegistryValue = operation.RegistryValue! with
                {
                    DesiredState = operation.RegistryValue.DesiredState with { StringValue = "tampered" }
                }
            };
            await AssertValidateRejectedAsync(session, desiredTamper, BuiltInOptimizationCatalog.CurrentCatalogVersion, "agent.catalog.operation_mismatch", 5);
            await AssertValidateRejectedAsync(session, operation, "0.0.1-downgrade", "agent.catalog.version_mismatch", 7);

            var unknownOperation = operation with { OperationId = new OperationId("BB.OP.INTEGRATION.REGISTRY_PROOF.UNKNOWN") };
            await AssertValidateRejectedAsync(session, unknownOperation, BuiltInOptimizationCatalog.CurrentCatalogVersion, "agent.catalog.operation_unknown", 9);

            await ShutdownAsync(session.Client, session.SessionId, session.CorrelationId, session.Nonce, 11);
        }
    }

    [Fact]
    public async Task Agent_rejects_catalog_v1_registry_operation_tampering()
    {
        var definition = new BuiltInOptimizationCatalog().Find(new OptimizationId("BB.OPT.VISUAL.TRANSPARENCY.DISABLE"))!;
        var operation = definition.OperationSpecs.Single();
        var session = await StartAgentSessionAsync();
        await using (session.Client)
        using (session.Process)
        {
            await HandshakeAsync(session);

            var accepted = AgentPipeProtocol.CreateMessage(
                session.SessionId,
                session.CorrelationId,
                3,
                session.Nonce,
                MessageType.ValidateOperationRequest,
                PayloadType.ValidateOperationRequest,
                new ValidateOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, definition.OptimizationId, operation),
                DateTimeOffset.UtcNow);
            Assert.True((await session.Client.WriteMessageAsync(accepted, CancellationToken.None)).IsSuccess);
            var acceptedResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
            var acceptedPayload = AgentPipeProtocol.DeserializePayload<ValidateOperationResponsePayload>(acceptedResponse.Value!);
            Assert.True(acceptedPayload.IsSuccess);
            Assert.True(acceptedPayload.Value!.Accepted, string.Join("; ", acceptedPayload.Value.Issues.Select(issue => issue.Code)));

            var targetTamper = operation with
            {
                RegistryValue = operation.RegistryValue! with
                {
                    Target = operation.RegistryValue.Target with { ValueName = "UnexpectedValueName" }
                }
            };
            await AssertValidateRejectedAsync(session, targetTamper, BuiltInOptimizationCatalog.CurrentCatalogVersion, "agent.catalog.operation_mismatch", 5, definition.OptimizationId);

            var desiredTamper = operation with
            {
                RegistryValue = operation.RegistryValue! with
                {
                    DesiredState = operation.RegistryValue.DesiredState with { DWordValue = 1 }
                }
            };
            await AssertValidateRejectedAsync(session, desiredTamper, BuiltInOptimizationCatalog.CurrentCatalogVersion, "agent.catalog.operation_mismatch", 7, definition.OptimizationId);
            await AssertValidateRejectedAsync(session, operation with { OperationType = OperationType.BorealIntegrationRegistryValue }, BuiltInOptimizationCatalog.CurrentCatalogVersion, "agent.catalog.operation_mismatch", 9, definition.OptimizationId);
            await AssertValidateRejectedAsync(session, operation, "0.0.1-downgrade", "agent.catalog.version_mismatch", 11, definition.OptimizationId);

            await ShutdownAsync(session.Client, session.SessionId, session.CorrelationId, session.Nonce, 13);
        }
    }

    [Fact]
    public async Task Agent_executes_catalog_v1_hkcu_registry_operation_with_snapshot_verify_and_rollback()
    {
        var definition = new BuiltInOptimizationCatalog().Find(new OptimizationId("BB.OPT.VISUAL.TRANSPARENCY.DISABLE"))!;
        var operation = definition.OperationSpecs.Single();
        var target = operation.RegistryValue!.Target;
        var backup = RegistryValueBackup.Capture(target);
        try
        {
            var session = await StartAgentSessionAsync();
            await using (session.Client)
            using (session.Process)
            {
                await HandshakeAsync(session);

                var capture = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    3,
                    session.Nonce,
                    MessageType.CaptureSnapshotRequest,
                    PayloadType.CaptureSnapshotRequest,
                    new CaptureSnapshotRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, definition.OptimizationId, operation),
                    DateTimeOffset.UtcNow);
                Assert.True((await session.Client.WriteMessageAsync(capture, CancellationToken.None)).IsSuccess);
                var captureResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var capturePayload = AgentPipeProtocol.DeserializePayload<CaptureSnapshotResponsePayload>(captureResponse.Value!);
                Assert.True(capturePayload.IsSuccess);
                Assert.True(capturePayload.Value!.Captured, string.Join("; ", capturePayload.Value.Issues.Select(issue => issue.Code)));
                Assert.NotNull(capturePayload.Value.SnapshotItem);

                var execute = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    5,
                    session.Nonce,
                    MessageType.ExecuteOperationRequest,
                    PayloadType.ExecuteOperationRequest,
                    new ExecuteOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, definition.OptimizationId, operation, capturePayload.Value.SnapshotItem!),
                    DateTimeOffset.UtcNow);
                Assert.True((await session.Client.WriteMessageAsync(execute, CancellationToken.None)).IsSuccess);
                var executeResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var executePayload = AgentPipeProtocol.DeserializePayload<ExecuteOperationResponsePayload>(executeResponse.Value!);
                Assert.True(executePayload.IsSuccess);
                Assert.NotNull(executePayload.Value!.Result);
                Assert.True(executePayload.Value.Result!.Status is OperationExecutionStatus.Applied or OperationExecutionStatus.AlreadySatisfied);

                var verify = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    7,
                    session.Nonce,
                    MessageType.VerifyOperationRequest,
                    PayloadType.VerifyOperationRequest,
                    new VerifyOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, definition.OptimizationId, operation),
                    DateTimeOffset.UtcNow);
                Assert.True((await session.Client.WriteMessageAsync(verify, CancellationToken.None)).IsSuccess);
                var verifyResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var verifyPayload = AgentPipeProtocol.DeserializePayload<VerifyOperationResponsePayload>(verifyResponse.Value!);
                Assert.True(verifyPayload.IsSuccess);
                Assert.True(verifyPayload.Value!.Result!.Verified);

                var rollback = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    9,
                    session.Nonce,
                    MessageType.RollbackOperationRequest,
                    PayloadType.RollbackOperationRequest,
                    new RollbackOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, definition.OptimizationId, operation, capturePayload.Value.SnapshotItem!),
                    DateTimeOffset.UtcNow);
                Assert.True((await session.Client.WriteMessageAsync(rollback, CancellationToken.None)).IsSuccess);
                var rollbackResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var rollbackPayload = AgentPipeProtocol.DeserializePayload<RollbackOperationResponsePayload>(rollbackResponse.Value!);
                Assert.True(rollbackPayload.IsSuccess);
                Assert.True(rollbackPayload.Value!.Result!.RestoredOriginalState, rollbackPayload.Value.Result.SafeMessage);
                backup.AssertRestored();

                await ShutdownAsync(session.Client, session.SessionId, session.CorrelationId, session.Nonce, 11);
            }
        }
        finally
        {
            backup.Restore();
        }
    }

    [Fact]
    public async Task Agent_rejects_snapshot_tampering_before_apply()
    {
        using var registryScope = ControlledRegistryTestScope.Acquire();
        var backup = RegistryBackup.Capture();
        try
        {
            var session = await StartAgentSessionAsync();
            await using (session.Client)
            using (session.Process)
            {
                await HandshakeAsync(session);
                var operation = BuiltInOperation();

                var capture = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    3,
                    session.Nonce,
                    MessageType.CaptureSnapshotRequest,
                    PayloadType.CaptureSnapshotRequest,
                    new CaptureSnapshotRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation),
                    DateTimeOffset.UtcNow);
                Assert.True((await session.Client.WriteMessageAsync(capture, CancellationToken.None)).IsSuccess);
                var captureResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var capturePayload = AgentPipeProtocol.DeserializePayload<CaptureSnapshotResponsePayload>(captureResponse.Value!);
                Assert.True(capturePayload.Value!.Captured);

                var tampered = capturePayload.Value.SnapshotItem! with
                {
                    ResourceIdentity = @"CurrentUser\Software\BorealBoost\IntegrationTest\Tampered"
                };
                var execute = AgentPipeProtocol.CreateMessage(
                    session.SessionId,
                    session.CorrelationId,
                    5,
                    session.Nonce,
                    MessageType.ExecuteOperationRequest,
                    PayloadType.ExecuteOperationRequest,
                    new ExecuteOperationRequestPayload("4.0.0", BuiltInOptimizationCatalog.CurrentCatalogVersion, BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation, tampered),
                    DateTimeOffset.UtcNow);

                Assert.True((await session.Client.WriteMessageAsync(execute, CancellationToken.None)).IsSuccess);
                var executeResponse = await session.Client.ReadMessageAsync(CancellationToken.None);
                var executePayload = AgentPipeProtocol.DeserializePayload<ExecuteOperationResponsePayload>(executeResponse.Value!);
                Assert.True(executePayload.IsSuccess);
                Assert.Null(executePayload.Value!.Result);
                Assert.Contains(executePayload.Value.Issues, issue => issue.Code == "agent.snapshot.mismatch");
                backup.AssertRestored();

                await ShutdownAsync(session.Client, session.SessionId, session.CorrelationId, session.Nonce, 7);
            }
        }
        finally
        {
            backup.Restore();
        }
    }

    [Fact]
    public async Task Cross_process_optimization_lock_rejects_second_process_holder()
    {
        var lockPath = Path.Combine(Path.GetTempPath(), "BorealBoostCrossProcessLock", Guid.NewGuid().ToString("N"), "optimization.lock");
        using var holder = StartPowerShellFileLockHolder(lockPath);
        try
        {
            var ready = await holder.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("locked", ready);

            var lockAttempt = await new CrossProcessOptimizationSessionLock(lockPath).TryAcquireAsync(CancellationToken.None);
            Assert.True(lockAttempt.IsFailure);
            Assert.Equal("optimization.session.already_running", lockAttempt.ErrorCode);

            await holder.StandardInput.WriteLineAsync("release");
            await holder.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

            var afterRelease = await new CrossProcessOptimizationSessionLock(lockPath).TryAcquireAsync(CancellationToken.None);
            Assert.True(afterRelease.IsSuccess, afterRelease.ErrorMessage);
            await afterRelease.Value!.DisposeAsync();
        }
        finally
        {
            if (!holder.HasExited)
            {
                holder.Kill(entireProcessTree: true);
            }
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

    private static async Task HandshakeAsync(AgentSession session)
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
    }

    private static OperationSpec BuiltInOperation()
    {
        return new BuiltInOptimizationCatalog()
            .Find(BuiltInOptimizationCatalog.IntegrationProofOptimizationId)!
            .OperationSpecs
            .Single();
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

    public static IEnumerable<object[]> SupportedRegistryValueCases()
    {
        yield return [RegistryValueKind.String, "plain", RegistryValueDataKind.String];
        yield return [RegistryValueKind.String, string.Empty, RegistryValueDataKind.String];
        yield return [RegistryValueKind.ExpandString, @"%TEMP%\BorealBoost", RegistryValueDataKind.ExpandString];
        yield return [RegistryValueKind.DWord, 42, RegistryValueDataKind.DWord];
        yield return [RegistryValueKind.QWord, 42L, RegistryValueDataKind.QWord];
        yield return [RegistryValueKind.MultiString, new[] { "alpha", "beta", string.Empty }, RegistryValueDataKind.MultiString];
        yield return [RegistryValueKind.Binary, new byte[] { 0, 1, 2, 255 }, RegistryValueDataKind.Binary];
    }

    private static async Task AssertValidateRejectedAsync(
        AgentSession session,
        OperationSpec operation,
        string catalogVersion,
        string expectedIssueCode,
        long sequence,
        OptimizationId? optimizationId = null)
    {
        var request = AgentPipeProtocol.CreateMessage(
            session.SessionId,
            session.CorrelationId,
            sequence,
            session.Nonce,
            MessageType.ValidateOperationRequest,
            PayloadType.ValidateOperationRequest,
            new ValidateOperationRequestPayload("4.0.0", catalogVersion, optimizationId ?? BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation),
            DateTimeOffset.UtcNow);

        Assert.True((await session.Client.WriteMessageAsync(request, CancellationToken.None)).IsSuccess);
        var response = await session.Client.ReadMessageAsync(CancellationToken.None);
        Assert.True(response.IsSuccess);
        var payload = AgentPipeProtocol.DeserializePayload<ValidateOperationResponsePayload>(response.Value!);
        Assert.True(payload.IsSuccess);
        Assert.False(payload.Value!.Accepted);
        Assert.Contains(payload.Value.Issues, issue => issue.Code == expectedIssueCode);
    }

    private static void SetControlledValue(object value, RegistryValueKind kind)
    {
        using var key = Registry.CurrentUser.CreateSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: true);
        key?.SetValue(AgentOperationSecurityValidator.IntegrationTestValueName, value, kind);
    }

    private static void DeleteControlledValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: true);
        key?.DeleteValue(AgentOperationSecurityValidator.IntegrationTestValueName, throwOnMissingValue: false);
    }

    private static void DeleteControlledKey()
    {
        Registry.CurrentUser.DeleteSubKeyTree(AgentOperationSecurityValidator.IntegrationTestKeyPath, throwOnMissingSubKey: false);
    }

    private static void AssertControlledKeyAbsent()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: false);
        Assert.Null(key);
    }

    private static void AssertControlledKeyContainsOnlyTestValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: false);
        if (key is null)
        {
            return;
        }

        Assert.Empty(key.GetSubKeyNames());
        Assert.All(
            key.GetValueNames(),
            valueName => Assert.Equal(AgentOperationSecurityValidator.IntegrationTestValueName, valueName));
    }

    private static void AssertControlledValueAbsent()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: false);
        Assert.True(key is null || !key.GetValueNames().Contains(AgentOperationSecurityValidator.IntegrationTestValueName, StringComparer.Ordinal));
    }

    private static void AssertControlledValue(RegistryValueKind expectedKind, object expectedValue)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: false);
        Assert.NotNull(key);
        Assert.Equal(expectedKind, key!.GetValueKind(AgentOperationSecurityValidator.IntegrationTestValueName));
        AssertRegistryValuesEqual(expectedValue, ReadRawValue(key, AgentOperationSecurityValidator.IntegrationTestValueName, expectedKind));
    }

    private static object? ReadRawValue(RegistryKey key, string valueName, RegistryValueKind kind)
    {
        return kind == RegistryValueKind.ExpandString
            ? key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames)
            : key.GetValue(valueName);
    }

    private static void AssertRegistryValuesEqual(object? expected, object? actual)
    {
        switch (expected)
        {
            case byte[] expectedBytes:
                Assert.IsType<byte[]>(actual);
                Assert.Equal(expectedBytes, (byte[])actual);
                break;
            case string[] expectedStrings:
                Assert.IsType<string[]>(actual);
                Assert.Equal(expectedStrings, (string[])actual);
                break;
            default:
                Assert.Equal(expected, actual);
                break;
        }
    }

    private static Process StartPowerShellFileLockHolder(string lockPath)
    {
        var script = """
        $path = [Console]::In.ReadLine()
        $directory = [System.IO.Path]::GetDirectoryName($path)
        [System.IO.Directory]::CreateDirectory($directory) | Out-Null
        $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        [Console]::Out.WriteLine('locked')
        [Console]::Out.Flush()
        [Console]::In.ReadLine() | Out-Null
        $stream.Dispose()
        """;

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(script);

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("PowerShell lock helper did not start.");
        process.StandardInput.WriteLine(lockPath);
        return process;
    }

    private static bool IsCurrentProcessElevated()
    {
        using var identity = global::System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new global::System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(global::System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private sealed record AgentSession(
        Process Process,
        NamedPipeAgentClient Client,
        SessionId SessionId,
        CorrelationId CorrelationId,
        string Nonce);

    private sealed class RegistryBackup
    {
        private readonly bool _existed;
        private readonly RegistryValueKind? _kind;
        private readonly object? _value;

        private RegistryBackup(bool existed, RegistryValueKind? kind, object? value)
        {
            _existed = existed;
            _kind = kind;
            _value = value;
        }

        public static RegistryBackup Capture()
        {
            using var key = Registry.CurrentUser.OpenSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: false);
            if (key is null || !key.GetValueNames().Contains(AgentOperationSecurityValidator.IntegrationTestValueName, StringComparer.Ordinal))
            {
                return new RegistryBackup(false, null, null);
            }

            return new RegistryBackup(
                true,
                key.GetValueKind(AgentOperationSecurityValidator.IntegrationTestValueName),
                ReadRawValue(
                    key,
                    AgentOperationSecurityValidator.IntegrationTestValueName,
                    key.GetValueKind(AgentOperationSecurityValidator.IntegrationTestValueName)));
        }

        public void AssertRestored()
        {
            using var key = Registry.CurrentUser.OpenSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: false);
            if (!_existed)
            {
                Assert.True(key is null || !key.GetValueNames().Contains(AgentOperationSecurityValidator.IntegrationTestValueName, StringComparer.Ordinal));
                return;
            }

            Assert.NotNull(key);
            Assert.Equal(_kind, key!.GetValueKind(AgentOperationSecurityValidator.IntegrationTestValueName));
            AssertRegistryValuesEqual(_value, ReadRawValue(key, AgentOperationSecurityValidator.IntegrationTestValueName, _kind!.Value));
        }

        public void Restore()
        {
            using var key = Registry.CurrentUser.CreateSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: true);
            if (!_existed)
            {
                key?.DeleteValue(AgentOperationSecurityValidator.IntegrationTestValueName, throwOnMissingValue: false);
                return;
            }

            if (key is not null && _kind is not null)
            {
                key.SetValue(AgentOperationSecurityValidator.IntegrationTestValueName, _value ?? string.Empty, _kind.Value);
            }
        }
    }

    private sealed class RegistryValueBackup
    {
        private readonly RegistryOperationTarget _target;
        private readonly bool _keyExisted;
        private readonly bool _valueExisted;
        private readonly RegistryValueKind? _kind;
        private readonly object? _value;

        private RegistryValueBackup(
            RegistryOperationTarget target,
            bool keyExisted,
            bool valueExisted,
            RegistryValueKind? kind,
            object? value)
        {
            _target = target;
            _keyExisted = keyExisted;
            _valueExisted = valueExisted;
            _kind = kind;
            _value = value;
        }

        public static RegistryValueBackup Capture(RegistryOperationTarget target)
        {
            Assert.Equal(RegistryHiveKind.CurrentUser, target.Hive);
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, ToRegistryView(target.View));
            using var key = baseKey.OpenSubKey(target.KeyPath, writable: false);
            if (key is null)
            {
                return new RegistryValueBackup(target, keyExisted: false, valueExisted: false, null, null);
            }

            if (!key.GetValueNames().Contains(target.ValueName, StringComparer.Ordinal))
            {
                return new RegistryValueBackup(target, keyExisted: true, valueExisted: false, null, null);
            }

            var kind = key.GetValueKind(target.ValueName);
            return new RegistryValueBackup(target, keyExisted: true, valueExisted: true, kind, ReadRawValue(key, target.ValueName, kind));
        }

        public void AssertRestored()
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, ToRegistryView(_target.View));
            using var key = baseKey.OpenSubKey(_target.KeyPath, writable: false);
            if (!_keyExisted)
            {
                Assert.Null(key);
                return;
            }

            Assert.NotNull(key);
            if (!_valueExisted)
            {
                Assert.DoesNotContain(_target.ValueName, key!.GetValueNames(), StringComparer.Ordinal);
                return;
            }

            Assert.Equal(_kind, key!.GetValueKind(_target.ValueName));
            AssertRegistryValuesEqual(_value, ReadRawValue(key, _target.ValueName, _kind!.Value));
        }

        public void Restore()
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, ToRegistryView(_target.View));
            if (!_keyExisted)
            {
                baseKey.DeleteSubKeyTree(_target.KeyPath, throwOnMissingSubKey: false);
                return;
            }

            using var key = baseKey.CreateSubKey(_target.KeyPath, writable: true);
            if (key is null)
            {
                return;
            }

            if (!_valueExisted)
            {
                key.DeleteValue(_target.ValueName, throwOnMissingValue: false);
                return;
            }

            key.SetValue(_target.ValueName, _value ?? string.Empty, _kind!.Value);
        }

        private static RegistryView ToRegistryView(RegistryViewKind view)
        {
            return view switch
            {
                RegistryViewKind.Registry32 => RegistryView.Registry32,
                RegistryViewKind.Registry64 => RegistryView.Registry64,
                _ => RegistryView.Default
            };
        }
    }
}
