using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Common;
using BorealBoost.Core.Foundation;
using BorealBoost.Core.Identity;
using BorealBoost.Core.Optimization;
using BorealBoost.Infrastructure.AgentIpc;
using BorealBoost.Optimization.Catalog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Agent;

public sealed class AgentIpcSession
{
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(30);

    private readonly AgentBootstrapOptions _options;
    private readonly IApplicationInfoProvider _applicationInfoProvider;
    private readonly IOptimizationCatalog _catalog;
    private readonly IOperationHandlerRegistry _operationHandlerRegistry;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<AgentIpcSession> _logger;
    private readonly AgentOperationSecurityValidator _operationSecurityValidator = new();
    private readonly CanonicalOperationSpecValidator _canonicalOperationValidator;

    public AgentIpcSession(
        AgentBootstrapOptions options,
        IApplicationInfoProvider applicationInfoProvider,
        IOptimizationCatalog catalog,
        IOperationHandlerRegistry operationHandlerRegistry,
        IHostApplicationLifetime applicationLifetime,
        ILogger<AgentIpcSession> logger)
    {
        _options = options;
        _applicationInfoProvider = applicationInfoProvider;
        _catalog = catalog;
        _operationHandlerRegistry = operationHandlerRegistry;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _canonicalOperationValidator = new CanonicalOperationSpecValidator(catalog);
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

                var isElevated = IsCurrentProcessElevated();
                var response = CreateResponse(
                    message.Envelope,
                    MessageType.AgentStatusResponse,
                    PayloadType.AgentStatusResponse,
                    new AgentStatusResponsePayload(appInfo.Version.ToString(), AcceptsPrivilegedOperations: isElevated, IsElevated: isElevated));
                await AgentPipeProtocol.WriteMessageAsync(stream, response, cancellationToken).ConfigureAwait(false);
                return false;
            }

            case MessageType.ValidateOperationRequest:
                return await HandleValidateOperationAsync(stream, message, cancellationToken).ConfigureAwait(false);

            case MessageType.CaptureSnapshotRequest:
                return await HandleCaptureSnapshotAsync(stream, message, cancellationToken).ConfigureAwait(false);

            case MessageType.ExecuteOperationRequest:
                return await HandleExecuteOperationAsync(stream, message, cancellationToken).ConfigureAwait(false);

            case MessageType.VerifyOperationRequest:
                return await HandleVerifyOperationAsync(stream, message, cancellationToken).ConfigureAwait(false);

            case MessageType.RollbackOperationRequest:
                return await HandleRollbackOperationAsync(stream, message, cancellationToken).ConfigureAwait(false);

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

    private async Task<bool> HandleValidateOperationAsync(
        Stream stream,
        AgentProtocolMessage message,
        CancellationToken cancellationToken)
    {
        var payload = AgentPipeProtocol.DeserializePayload<ValidateOperationRequestPayload>(message);
        if (payload.IsFailure || payload.Value is null)
        {
            await WriteErrorAsync(stream, message.Envelope, payload.ErrorCode ?? "protocol.payload.invalid", payload.ErrorMessage ?? "Invalid payload.", cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        var issues = ValidateOperation(payload.Value.PlanSchemaVersion, payload.Value.CatalogVersion, payload.Value.OptimizationId, payload.Value.Operation);
        var response = CreateResponse(
            message.Envelope,
            MessageType.ValidateOperationResponse,
            PayloadType.ValidateOperationResponse,
            new ValidateOperationResponsePayload(issues.Count == 0, issues, _operationHandlerRegistry.SupportedOperationTypes));
        await AgentPipeProtocol.WriteMessageAsync(stream, response, cancellationToken).ConfigureAwait(false);
        return false;
    }

    private async Task<bool> HandleCaptureSnapshotAsync(
        Stream stream,
        AgentProtocolMessage message,
        CancellationToken cancellationToken)
    {
        var payload = AgentPipeProtocol.DeserializePayload<CaptureSnapshotRequestPayload>(message);
        if (payload.IsFailure || payload.Value is null)
        {
            await WriteErrorAsync(stream, message.Envelope, payload.ErrorCode ?? "protocol.payload.invalid", payload.ErrorMessage ?? "Invalid payload.", cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        var issues = ValidateOperation(payload.Value.PlanSchemaVersion, payload.Value.CatalogVersion, payload.Value.OptimizationId, payload.Value.Operation);
        if (issues.Count > 0)
        {
            await WriteOperationResponseAsync(
                stream,
                message.Envelope,
                MessageType.CaptureSnapshotResponse,
                PayloadType.CaptureSnapshotResponse,
                new CaptureSnapshotResponsePayload(false, null, issues),
                cancellationToken).ConfigureAwait(false);
            return false;
        }

        var handler = GetHandler(payload.Value.Operation);
        var snapshot = await handler.CaptureSnapshotAsync(payload.Value.Operation, cancellationToken).ConfigureAwait(false);
        var responsePayload = snapshot.IsSuccess && snapshot.Value is not null
            ? new CaptureSnapshotResponsePayload(true, snapshot.Value, [])
            : new CaptureSnapshotResponsePayload(false, null, [Issue(snapshot.ErrorCode ?? "operation.snapshot.failed", snapshot.ErrorMessage ?? "Snapshot capture failed.", payload.Value.Operation.OperationId.ToString(), OperationErrorCategory.SnapshotFailed)]);
        await WriteOperationResponseAsync(stream, message.Envelope, MessageType.CaptureSnapshotResponse, PayloadType.CaptureSnapshotResponse, responsePayload, cancellationToken).ConfigureAwait(false);
        return false;
    }

    private async Task<bool> HandleExecuteOperationAsync(
        Stream stream,
        AgentProtocolMessage message,
        CancellationToken cancellationToken)
    {
        var payload = AgentPipeProtocol.DeserializePayload<ExecuteOperationRequestPayload>(message);
        if (payload.IsFailure || payload.Value is null)
        {
            await WriteErrorAsync(stream, message.Envelope, payload.ErrorCode ?? "protocol.payload.invalid", payload.ErrorMessage ?? "Invalid payload.", cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        var issues = ValidateOperation(payload.Value.PlanSchemaVersion, payload.Value.CatalogVersion, payload.Value.OptimizationId, payload.Value.Operation);
        issues.AddRange(ValidateSnapshot(payload.Value.Operation, payload.Value.SnapshotItem));
        if (issues.Count > 0)
        {
            await WriteOperationResponseAsync(stream, message.Envelope, MessageType.ExecuteOperationResponse, PayloadType.ExecuteOperationResponse, new ExecuteOperationResponsePayload(null, issues), cancellationToken).ConfigureAwait(false);
            return false;
        }

        var result = await GetHandler(payload.Value.Operation).ApplyAsync(payload.Value.Operation, payload.Value.SnapshotItem, cancellationToken).ConfigureAwait(false);
        var responsePayload = result.IsSuccess && result.Value is not null
            ? new ExecuteOperationResponsePayload(result.Value, [])
            : new ExecuteOperationResponsePayload(null, [Issue(result.ErrorCode ?? "operation.apply.failed", result.ErrorMessage ?? "Apply failed.", payload.Value.Operation.OperationId.ToString(), OperationErrorCategory.ApplyFailed)]);
        await WriteOperationResponseAsync(stream, message.Envelope, MessageType.ExecuteOperationResponse, PayloadType.ExecuteOperationResponse, responsePayload, cancellationToken).ConfigureAwait(false);
        return false;
    }

    private async Task<bool> HandleVerifyOperationAsync(
        Stream stream,
        AgentProtocolMessage message,
        CancellationToken cancellationToken)
    {
        var payload = AgentPipeProtocol.DeserializePayload<VerifyOperationRequestPayload>(message);
        if (payload.IsFailure || payload.Value is null)
        {
            await WriteErrorAsync(stream, message.Envelope, payload.ErrorCode ?? "protocol.payload.invalid", payload.ErrorMessage ?? "Invalid payload.", cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        var issues = ValidateOperation(payload.Value.PlanSchemaVersion, payload.Value.CatalogVersion, payload.Value.OptimizationId, payload.Value.Operation);
        if (issues.Count > 0)
        {
            await WriteOperationResponseAsync(stream, message.Envelope, MessageType.VerifyOperationResponse, PayloadType.VerifyOperationResponse, new VerifyOperationResponsePayload(null, issues), cancellationToken).ConfigureAwait(false);
            return false;
        }

        var result = await GetHandler(payload.Value.Operation).VerifyAsync(payload.Value.Operation, cancellationToken).ConfigureAwait(false);
        var responsePayload = result.IsSuccess && result.Value is not null
            ? new VerifyOperationResponsePayload(result.Value, [])
            : new VerifyOperationResponsePayload(null, [Issue(result.ErrorCode ?? "operation.verify.failed", result.ErrorMessage ?? "Verify failed.", payload.Value.Operation.OperationId.ToString(), OperationErrorCategory.VerificationFailed)]);
        await WriteOperationResponseAsync(stream, message.Envelope, MessageType.VerifyOperationResponse, PayloadType.VerifyOperationResponse, responsePayload, cancellationToken).ConfigureAwait(false);
        return false;
    }

    private async Task<bool> HandleRollbackOperationAsync(
        Stream stream,
        AgentProtocolMessage message,
        CancellationToken cancellationToken)
    {
        var payload = AgentPipeProtocol.DeserializePayload<RollbackOperationRequestPayload>(message);
        if (payload.IsFailure || payload.Value is null)
        {
            await WriteErrorAsync(stream, message.Envelope, payload.ErrorCode ?? "protocol.payload.invalid", payload.ErrorMessage ?? "Invalid payload.", cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        var issues = ValidateOperation(payload.Value.PlanSchemaVersion, payload.Value.CatalogVersion, payload.Value.OptimizationId, payload.Value.Operation);
        issues.AddRange(ValidateSnapshot(payload.Value.Operation, payload.Value.SnapshotItem));
        if (issues.Count > 0)
        {
            await WriteOperationResponseAsync(stream, message.Envelope, MessageType.RollbackOperationResponse, PayloadType.RollbackOperationResponse, new RollbackOperationResponsePayload(null, issues), cancellationToken).ConfigureAwait(false);
            return false;
        }

        var result = await GetHandler(payload.Value.Operation).RollbackAsync(payload.Value.Operation, payload.Value.SnapshotItem, cancellationToken).ConfigureAwait(false);
        var responsePayload = result.IsSuccess && result.Value is not null
            ? new RollbackOperationResponsePayload(result.Value, [])
            : new RollbackOperationResponsePayload(null, [Issue(result.ErrorCode ?? "operation.rollback.failed", result.ErrorMessage ?? "Rollback failed.", payload.Value.Operation.OperationId.ToString(), OperationErrorCategory.RollbackFailed)]);
        await WriteOperationResponseAsync(stream, message.Envelope, MessageType.RollbackOperationResponse, PayloadType.RollbackOperationResponse, responsePayload, cancellationToken).ConfigureAwait(false);
        return false;
    }

    private List<OptimizationIssue> ValidateOperation(
        string planSchemaVersion,
        string catalogVersion,
        OptimizationId optimizationId,
        OperationSpec operation)
    {
        var issues = new List<OptimizationIssue>();
        if (planSchemaVersion != "4.0.0")
        {
            issues.Add(Issue("agent.plan.schema_unsupported", "ExecutionPlan schema version is unsupported.", "ExecutionPlan"));
        }

        if (!OptimizationId.TryCreate(optimizationId.Value, out _))
        {
            issues.Add(Issue("agent.optimization_id_invalid", "OptimizationId is invalid.", optimizationId.ToString()));
        }

        issues.AddRange(_canonicalOperationValidator.Validate(catalogVersion, optimizationId, operation));

        var definition = _catalog.Find(optimizationId);
        if (definition?.RequiresElevation == true && !IsCurrentProcessElevated())
        {
            issues.Add(Issue("agent.elevation.required", "Trusted operation requires an elevated Agent token.", optimizationId.ToString()));
        }

        if (!_operationHandlerRegistry.TryGetHandler(operation.OperationType, out _))
        {
            issues.Add(Issue("agent.operation.handler_missing", "OperationType has no allowlisted handler.", operation.OperationId.ToString()));
        }

        var security = _operationSecurityValidator.Validate(operation);
        if (security.IsFailure)
        {
            issues.Add(Issue(security.ErrorCode ?? "agent.operation.rejected", security.ErrorMessage ?? "Operation rejected by Agent.", operation.OperationId.ToString()));
        }

        return issues;
    }

    private static List<OptimizationIssue> ValidateSnapshot(OperationSpec operation, OperationSnapshotItem snapshot)
    {
        var issues = new List<OptimizationIssue>();
        if (snapshot.OperationId != operation.OperationId ||
            snapshot.RegistryTarget is null ||
            operation.RegistryValue is null ||
            !OperationSnapshotHasher.IsValid(snapshot) ||
            snapshot.ResourceType != OperationResourceType.RegistryValue ||
            snapshot.RegistryTarget.Hive != operation.RegistryValue.Target.Hive ||
            snapshot.RegistryTarget.View != operation.RegistryValue.Target.View ||
            !string.Equals(snapshot.RegistryTarget.KeyPath, operation.RegistryValue.Target.KeyPath, StringComparison.Ordinal) ||
            !string.Equals(snapshot.RegistryTarget.ValueName, operation.RegistryValue.Target.ValueName, StringComparison.Ordinal) ||
            !string.Equals(snapshot.ResourceIdentity, $"{operation.RegistryValue.Target.Hive}\\{operation.RegistryValue.Target.KeyPath}\\{operation.RegistryValue.Target.ValueName}", StringComparison.Ordinal) ||
            snapshot.RestorationStrategy != operation.RollbackStrategy ||
            !SnapshotValueShapeIsValid(snapshot))
        {
            issues.Add(Issue("agent.snapshot.mismatch", "OperationSnapshot does not match OperationSpec.", operation.OperationId.ToString(), OperationErrorCategory.SnapshotFailed));
        }

        return issues;
    }

    private static bool SnapshotValueShapeIsValid(OperationSnapshotItem snapshot)
    {
        if (!snapshot.ExistedBefore)
        {
            return snapshot.PreviousValueKind is null &&
                   snapshot.PreviousStringValue is null &&
                   snapshot.PreviousDWordValue is null &&
                   snapshot.PreviousQWordValue is null &&
                   snapshot.PreviousMultiStringValue is null &&
                   snapshot.PreviousBinaryValue is null;
        }

        return snapshot.PreviousValueKind switch
        {
            RegistryValueDataKind.String or RegistryValueDataKind.ExpandString =>
                snapshot.PreviousStringValue is not null &&
                snapshot.PreviousDWordValue is null &&
                snapshot.PreviousQWordValue is null &&
                snapshot.PreviousMultiStringValue is null &&
                snapshot.PreviousBinaryValue is null,
            RegistryValueDataKind.DWord =>
                snapshot.PreviousStringValue is null &&
                snapshot.PreviousDWordValue is not null &&
                snapshot.PreviousQWordValue is null &&
                snapshot.PreviousMultiStringValue is null &&
                snapshot.PreviousBinaryValue is null,
            RegistryValueDataKind.QWord =>
                snapshot.PreviousStringValue is null &&
                snapshot.PreviousDWordValue is null &&
                snapshot.PreviousQWordValue is not null &&
                snapshot.PreviousMultiStringValue is null &&
                snapshot.PreviousBinaryValue is null,
            RegistryValueDataKind.MultiString =>
                snapshot.PreviousStringValue is null &&
                snapshot.PreviousDWordValue is null &&
                snapshot.PreviousQWordValue is null &&
                snapshot.PreviousMultiStringValue is not null &&
                snapshot.PreviousBinaryValue is null,
            RegistryValueDataKind.Binary =>
                snapshot.PreviousStringValue is null &&
                snapshot.PreviousDWordValue is null &&
                snapshot.PreviousQWordValue is null &&
                snapshot.PreviousMultiStringValue is null &&
                snapshot.PreviousBinaryValue is not null,
            _ => false
        };
    }

    private IOperationHandler GetHandler(OperationSpec operation)
    {
        if (_operationHandlerRegistry.TryGetHandler(operation.OperationType, out var handler))
        {
            return handler;
        }

        throw new InvalidOperationException("Operation handler missing after validation.");
    }

    private static Task<Result> WriteOperationResponseAsync(
        Stream stream,
        AgentMessageEnvelope request,
        MessageType responseType,
        PayloadType payloadType,
        object payload,
        CancellationToken cancellationToken)
    {
        var response = CreateResponse(request, responseType, payloadType, payload);
        return AgentPipeProtocol.WriteMessageAsync(stream, response, cancellationToken);
    }

    private static OptimizationIssue Issue(
        string code,
        string message,
        string scope,
        OperationErrorCategory category = OperationErrorCategory.ProtocolRejected)
    {
        return new OptimizationIssue(code, message, scope, category);
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

    private static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
