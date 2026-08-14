using System.Diagnostics;
using System.Security;
using Microsoft.Win32;
using BorealBoost.Core.Common;
using BorealBoost.Core.Optimization;

namespace BorealBoost.System.Operations;

public sealed class BorealIntegrationRegistryOperationHandler : IOperationHandler
{
    private readonly AgentOperationSecurityValidator _validator = new();
    private readonly OperationType _operationType;

    public BorealIntegrationRegistryOperationHandler()
        : this(OperationType.BorealIntegrationRegistryValue)
    {
    }

    public BorealIntegrationRegistryOperationHandler(OperationType operationType)
    {
        if (operationType is not (OperationType.BorealIntegrationRegistryValue or OperationType.RegistryValue))
        {
            throw new ArgumentOutOfRangeException(nameof(operationType), operationType, "Only trusted registry operation types are supported.");
        }

        _operationType = operationType;
    }

    public OperationType OperationType => _operationType;

    public Result Validate(OperationSpec operation)
    {
        return _validator.Validate(operation);
    }

    public Task<Result<OperationSnapshotItem>> CaptureSnapshotAsync(OperationSpec operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validation = Validate(operation);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result<OperationSnapshotItem>.Failure(validation.ErrorCode ?? "operation.invalid", validation.ErrorMessage ?? "Operation is invalid."));
        }

        var target = operation.RegistryValue!.Target;
        try
        {
            using var baseKey = OpenBaseKey(target);
            using var key = baseKey.OpenSubKey(target.KeyPath, writable: false);
            var keyExistedBefore = key is not null;
            if (key is null || !key.GetValueNames().Contains(target.ValueName, StringComparer.Ordinal))
            {
                return Task.FromResult(Result<OperationSnapshotItem>.Success(OperationSnapshotHasher.Stamp(new OperationSnapshotItem(
                    Guid.NewGuid(),
                    operation.OperationId,
                    OperationResourceType.RegistryValue,
                    ResourceIdentity(target),
                    ExistedBefore: false,
                    target,
                    null,
                    null,
                    null,
                    "Microsoft.Win32.Registry read-only",
                    DateTimeOffset.UtcNow,
                    operation.RollbackStrategy,
                    [],
                    "value_absent_before_apply",
                    RegistryKeyExistedBefore: keyExistedBefore))));
            }

            var kind = key.GetValueKind(target.ValueName);
            var value = ReadRawRegistryValue(key, target.ValueName, kind);
            var converted = ConvertValue(kind, value);
            if (converted is null)
            {
                return Task.FromResult(Result<OperationSnapshotItem>.Failure(
                    "operation.snapshot.value_kind_unsupported",
                    "Existing registry value kind cannot be restored exactly by the Phase 4 controlled handler."));
            }

            return Task.FromResult(Result<OperationSnapshotItem>.Success(OperationSnapshotHasher.Stamp(new OperationSnapshotItem(
                Guid.NewGuid(),
                operation.OperationId,
                OperationResourceType.RegistryValue,
                ResourceIdentity(target),
                ExistedBefore: true,
                target,
                converted.Kind,
                converted.StringValue,
                converted.DWordValue,
                "Microsoft.Win32.Registry read-only",
                DateTimeOffset.UtcNow,
                operation.RollbackStrategy,
                [],
                "value_captured_before_apply",
                converted.QWordValue,
                converted.MultiStringValue,
                converted.BinaryValue,
                RegistryKeyExistedBefore: keyExistedBefore))));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return Task.FromResult(Result<OperationSnapshotItem>.Failure("operation.snapshot.failed", exception.Message));
        }
    }

    public Task<Result<OperationExecutionResult>> ApplyAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var started = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var validation = ValidateOperationAndSnapshot(operation, snapshot);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result<OperationExecutionResult>.Failure(validation.ErrorCode ?? "operation.invalid", validation.ErrorMessage ?? "Operation is invalid."));
        }

        try
        {
            var verifyBefore = VerifyDesiredState(operation);
            if (verifyBefore)
            {
                stopwatch.Stop();
                return Task.FromResult(Result<OperationExecutionResult>.Success(new OperationExecutionResult(
                    operation.OperationId,
                    OperationExecutionStatus.AlreadySatisfied,
                    started,
                    DateTimeOffset.UtcNow,
                    stopwatch.Elapsed,
                    ChangedState: false,
                    RequiresRestart: false,
                    OperationErrorCategory.None,
                    "Controlled registry value was already in the desired state.")));
            }

            WriteDesiredState(operation);
            stopwatch.Stop();
            return Task.FromResult(Result<OperationExecutionResult>.Success(new OperationExecutionResult(
                operation.OperationId,
                OperationExecutionStatus.Applied,
                started,
                DateTimeOffset.UtcNow,
                stopwatch.Elapsed,
                ChangedState: true,
                RequiresRestart: false,
                OperationErrorCategory.None,
                "Controlled registry value was applied.")));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            stopwatch.Stop();
            return Task.FromResult(Result<OperationExecutionResult>.Success(new OperationExecutionResult(
                operation.OperationId,
                OperationExecutionStatus.Failed,
                started,
                DateTimeOffset.UtcNow,
                stopwatch.Elapsed,
                ChangedState: false,
                RequiresRestart: false,
                OperationErrorCategory.ApplyFailed,
                "Controlled registry value could not be applied.")));
        }
    }

    public Task<Result<OperationVerificationResult>> VerifyAsync(OperationSpec operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validation = Validate(operation);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result<OperationVerificationResult>.Failure(validation.ErrorCode ?? "operation.invalid", validation.ErrorMessage ?? "Operation is invalid."));
        }

        try
        {
            var verified = VerifyDesiredState(operation);
            return Task.FromResult(Result<OperationVerificationResult>.Success(new OperationVerificationResult(
                operation.OperationId,
                verified ? OperationExecutionStatus.Verified : OperationExecutionStatus.FailedVerification,
                DateTimeOffset.UtcNow,
                verified,
                verified ? "Controlled registry value verified." : "Controlled registry value is not in the desired state.")));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return Task.FromResult(Result<OperationVerificationResult>.Failure("operation.verify.failed", exception.Message));
        }
    }

    public Task<Result<OperationRollbackResult>> RollbackAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var started = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var validation = ValidateOperationAndSnapshot(operation, snapshot);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result<OperationRollbackResult>.Failure(validation.ErrorCode ?? "operation.invalid", validation.ErrorMessage ?? "Operation is invalid."));
        }

        try
        {
            var current = ReadCurrentState(operation.RegistryValue!.Target);
            var original = StateFromSnapshot(snapshot);
            if (StatesEqual(current, original))
            {
                stopwatch.Stop();
                return Task.FromResult(Result<OperationRollbackResult>.Success(new OperationRollbackResult(
                    operation.OperationId,
                    OperationExecutionStatus.RolledBack,
                    started,
                    DateTimeOffset.UtcNow,
                    stopwatch.Elapsed,
                    RestoredOriginalState: true,
                    OperationErrorCategory.None,
                    "Controlled registry value was already back to the original state.")));
            }

            if (!VerifyDesiredState(operation))
            {
                stopwatch.Stop();
                return Task.FromResult(Result<OperationRollbackResult>.Success(new OperationRollbackResult(
                    operation.OperationId,
                    OperationExecutionStatus.RollbackFailed,
                    started,
                    DateTimeOffset.UtcNow,
                    stopwatch.Elapsed,
                    RestoredOriginalState: false,
                    OperationErrorCategory.OutcomeUnknown,
                    "Controlled registry value changed externally; rollback did not overwrite it.")));
            }

            RestoreSnapshot(snapshot);
            var restored = StatesEqual(ReadCurrentState(operation.RegistryValue.Target), original);
            stopwatch.Stop();
            return Task.FromResult(Result<OperationRollbackResult>.Success(new OperationRollbackResult(
                operation.OperationId,
                restored ? OperationExecutionStatus.RolledBack : OperationExecutionStatus.RollbackFailed,
                started,
                DateTimeOffset.UtcNow,
                stopwatch.Elapsed,
                restored,
                restored ? OperationErrorCategory.None : OperationErrorCategory.RollbackFailed,
                restored ? "Controlled registry value was restored to the original state." : "Controlled registry value rollback verification failed.")));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            stopwatch.Stop();
            return Task.FromResult(Result<OperationRollbackResult>.Success(new OperationRollbackResult(
                operation.OperationId,
                OperationExecutionStatus.RollbackFailed,
                started,
                DateTimeOffset.UtcNow,
                stopwatch.Elapsed,
                RestoredOriginalState: false,
                OperationErrorCategory.RollbackFailed,
                "Controlled registry value could not be rolled back.")));
        }
    }

    private static RegistryKey OpenBaseKey(RegistryOperationTarget target)
    {
        var registryView = target.View switch
        {
            RegistryViewKind.Registry32 => RegistryView.Registry32,
            RegistryViewKind.Registry64 => RegistryView.Registry64,
            _ => RegistryView.Default
        };
        var hive = target.Hive switch
        {
            RegistryHiveKind.CurrentUser => RegistryHive.CurrentUser,
            RegistryHiveKind.LocalMachine => RegistryHive.LocalMachine,
            _ => throw new IOException("Registry hive is not supported by the trusted registry handler.")
        };
        return RegistryKey.OpenBaseKey(hive, registryView);
    }

    private static bool VerifyDesiredState(OperationSpec operation)
    {
        var current = ReadCurrentState(operation.RegistryValue!.Target);
        return StatesEqual(current, operation.RegistryValue.DesiredState);
    }

    private static RegistryValueState ReadCurrentState(RegistryOperationTarget target)
    {
        using var baseKey = OpenBaseKey(target);
        using var key = baseKey.OpenSubKey(target.KeyPath, writable: false);
        if (key is null || !key.GetValueNames().Contains(target.ValueName, StringComparer.Ordinal))
        {
            return new RegistryValueState(false, RegistryValueDataKind.String, null, null);
        }

        var kind = key.GetValueKind(target.ValueName);
        var converted = ConvertValue(kind, ReadRawRegistryValue(key, target.ValueName, kind));
        return converted is null
            ? new RegistryValueState(true, RegistryValueDataKind.Unsupported, null, null)
            : new RegistryValueState(
                true,
                converted.Kind,
                converted.StringValue,
                converted.DWordValue,
                converted.QWordValue,
                converted.MultiStringValue,
                converted.BinaryValue);
    }

    private static void WriteDesiredState(OperationSpec operation)
    {
        var registry = operation.RegistryValue!;
        using var baseKey = OpenBaseKey(registry.Target);
        if (!registry.DesiredState.Exists)
        {
            using var existing = baseKey.OpenSubKey(registry.Target.KeyPath, writable: true);
            existing?.DeleteValue(registry.Target.ValueName, throwOnMissingValue: false);
            return;
        }

        using var key = baseKey.CreateSubKey(registry.Target.KeyPath, writable: true);
        if (key is null)
        {
            throw new IOException("Registry key could not be opened for the controlled integration operation.");
        }

        switch (registry.DesiredState.ValueKind)
        {
            case RegistryValueDataKind.String:
                key.SetValue(registry.Target.ValueName, registry.DesiredState.StringValue ?? string.Empty, RegistryValueKind.String);
                return;
            case RegistryValueDataKind.ExpandString:
                key.SetValue(registry.Target.ValueName, registry.DesiredState.StringValue ?? string.Empty, RegistryValueKind.ExpandString);
                return;
            case RegistryValueDataKind.DWord:
                key.SetValue(registry.Target.ValueName, registry.DesiredState.DWordValue ?? 0, RegistryValueKind.DWord);
                return;
            case RegistryValueDataKind.QWord:
                key.SetValue(registry.Target.ValueName, registry.DesiredState.QWordValue ?? 0L, RegistryValueKind.QWord);
                return;
            case RegistryValueDataKind.MultiString:
                key.SetValue(registry.Target.ValueName, registry.DesiredState.MultiStringValue?.ToArray() ?? [], RegistryValueKind.MultiString);
                return;
            case RegistryValueDataKind.Binary:
                key.SetValue(registry.Target.ValueName, registry.DesiredState.BinaryValue ?? [], RegistryValueKind.Binary);
                return;
            default:
                throw new IOException("Registry value kind is not supported for the controlled integration operation.");
        }
    }

    private static void RestoreSnapshot(OperationSnapshotItem snapshot)
    {
        if (snapshot.RegistryTarget is null)
        {
            throw new InvalidOperationException("Registry rollback snapshot target is missing.");
        }

        using var baseKey = OpenBaseKey(snapshot.RegistryTarget);
        if (!snapshot.ExistedBefore)
        {
            var removeCreatedKey = false;
            using (var existing = baseKey.OpenSubKey(snapshot.RegistryTarget.KeyPath, writable: true))
            {
                existing?.DeleteValue(snapshot.RegistryTarget.ValueName, throwOnMissingValue: false);
                removeCreatedKey = snapshot.RegistryKeyExistedBefore == false && RegistryKeyIsEmpty(existing);
            }

            if (removeCreatedKey)
            {
                baseKey.DeleteSubKey(snapshot.RegistryTarget.KeyPath, throwOnMissingSubKey: false);
            }

            return;
        }

        using var key = baseKey.CreateSubKey(snapshot.RegistryTarget.KeyPath, writable: true);
        if (key is null)
        {
            throw new IOException("Registry key could not be opened for rollback.");
        }

        switch (snapshot.PreviousValueKind)
        {
            case RegistryValueDataKind.String:
                key.SetValue(snapshot.RegistryTarget.ValueName, snapshot.PreviousStringValue ?? string.Empty, RegistryValueKind.String);
                return;
            case RegistryValueDataKind.ExpandString:
                key.SetValue(snapshot.RegistryTarget.ValueName, snapshot.PreviousStringValue ?? string.Empty, RegistryValueKind.ExpandString);
                return;
            case RegistryValueDataKind.DWord:
                key.SetValue(snapshot.RegistryTarget.ValueName, snapshot.PreviousDWordValue ?? 0, RegistryValueKind.DWord);
                return;
            case RegistryValueDataKind.QWord:
                key.SetValue(snapshot.RegistryTarget.ValueName, snapshot.PreviousQWordValue ?? 0L, RegistryValueKind.QWord);
                return;
            case RegistryValueDataKind.MultiString:
                key.SetValue(snapshot.RegistryTarget.ValueName, snapshot.PreviousMultiStringValue?.ToArray() ?? [], RegistryValueKind.MultiString);
                return;
            case RegistryValueDataKind.Binary:
                key.SetValue(snapshot.RegistryTarget.ValueName, snapshot.PreviousBinaryValue ?? [], RegistryValueKind.Binary);
                return;
            default:
                throw new IOException("Registry snapshot value kind is not supported for rollback.");
        }
    }

    private Result ValidateOperationAndSnapshot(OperationSpec operation, OperationSnapshotItem snapshot)
    {
        var validation = Validate(operation);
        if (validation.IsFailure)
        {
            return validation;
        }

        if (snapshot.OperationId != operation.OperationId ||
            !OperationSnapshotHasher.IsValid(snapshot) ||
            snapshot.ResourceType != OperationResourceType.RegistryValue ||
            snapshot.RegistryTarget is null ||
            snapshot.RegistryTarget.Hive != operation.RegistryValue!.Target.Hive ||
            snapshot.RegistryTarget.View != operation.RegistryValue.Target.View ||
            !string.Equals(snapshot.RegistryTarget.KeyPath, operation.RegistryValue!.Target.KeyPath, StringComparison.Ordinal) ||
            !string.Equals(snapshot.RegistryTarget.ValueName, operation.RegistryValue.Target.ValueName, StringComparison.Ordinal) ||
            !string.Equals(snapshot.ResourceIdentity, ResourceIdentity(operation.RegistryValue.Target), StringComparison.Ordinal) ||
            snapshot.RestorationStrategy != operation.RollbackStrategy)
        {
            return Result.Failure("operation.snapshot.mismatch", "OperationSnapshot does not match OperationSpec.");
        }

        var snapshotStateValidation = ValidateSnapshotValueState(snapshot);
        if (snapshotStateValidation.IsFailure)
        {
            return snapshotStateValidation;
        }

        return Result.Success();
    }

    private static bool RegistryKeyIsEmpty(RegistryKey? key)
    {
        return key is not null &&
               key.GetValueNames().Length == 0 &&
               key.GetSubKeyNames().Length == 0;
    }

    private static RegistryValueState StateFromSnapshot(OperationSnapshotItem snapshot)
    {
        return new RegistryValueState(
            snapshot.ExistedBefore,
            snapshot.PreviousValueKind ?? RegistryValueDataKind.String,
            snapshot.PreviousStringValue,
            snapshot.PreviousDWordValue,
            snapshot.PreviousQWordValue,
            snapshot.PreviousMultiStringValue,
            snapshot.PreviousBinaryValue);
    }

    private static CapturedRegistryValue? ConvertValue(RegistryValueKind kind, object? value)
    {
        return kind switch
        {
            RegistryValueKind.String => new CapturedRegistryValue(RegistryValueDataKind.String, value?.ToString() ?? string.Empty, null, null, null, null),
            RegistryValueKind.ExpandString => new CapturedRegistryValue(RegistryValueDataKind.ExpandString, value?.ToString() ?? string.Empty, null, null, null, null),
            RegistryValueKind.DWord => new CapturedRegistryValue(RegistryValueDataKind.DWord, null, Convert.ToInt32(value, global::System.Globalization.CultureInfo.InvariantCulture), null, null, null),
            RegistryValueKind.QWord => new CapturedRegistryValue(RegistryValueDataKind.QWord, null, null, Convert.ToInt64(value, global::System.Globalization.CultureInfo.InvariantCulture), null, null),
            RegistryValueKind.MultiString => value is string[] multiString
                ? new CapturedRegistryValue(RegistryValueDataKind.MultiString, null, null, null, multiString, null)
                : null,
            RegistryValueKind.Binary => value is byte[] binary
                ? new CapturedRegistryValue(RegistryValueDataKind.Binary, null, null, null, null, binary)
                : null,
            _ => null
        };
    }

    private static object? ReadRawRegistryValue(RegistryKey key, string valueName, RegistryValueKind kind)
    {
        return kind == RegistryValueKind.ExpandString
            ? key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames)
            : key.GetValue(valueName);
    }

    private static bool StatesEqual(RegistryValueState current, RegistryValueState expected)
    {
        if (current.Exists != expected.Exists)
        {
            return false;
        }

        if (!current.Exists)
        {
            return true;
        }

        if (current.ValueKind != expected.ValueKind)
        {
            return false;
        }

        return current.ValueKind switch
        {
            RegistryValueDataKind.String => string.Equals(current.StringValue, expected.StringValue, StringComparison.Ordinal),
            RegistryValueDataKind.ExpandString => string.Equals(current.StringValue, expected.StringValue, StringComparison.Ordinal),
            RegistryValueDataKind.DWord => current.DWordValue == expected.DWordValue,
            RegistryValueDataKind.QWord => current.QWordValue == expected.QWordValue,
            RegistryValueDataKind.MultiString => SequenceEqual(current.MultiStringValue, expected.MultiStringValue),
            RegistryValueDataKind.Binary => BinaryEqual(current.BinaryValue, expected.BinaryValue),
            _ => false
        };
    }

    private static Result ValidateSnapshotValueState(OperationSnapshotItem snapshot)
    {
        if (!snapshot.ExistedBefore)
        {
            return snapshot.PreviousValueKind is null &&
                   snapshot.PreviousStringValue is null &&
                   snapshot.PreviousDWordValue is null &&
                   snapshot.PreviousQWordValue is null &&
                   snapshot.PreviousMultiStringValue is null &&
                   snapshot.PreviousBinaryValue is null
                ? Result.Success()
                : Result.Failure("operation.snapshot.absent_has_value", "Absent rollback snapshot cannot contain previous value data.");
        }

        if (snapshot.PreviousValueKind is null or RegistryValueDataKind.Unsupported)
        {
            return Result.Failure("operation.snapshot.value_kind_invalid", "Rollback snapshot value kind is missing or unsupported.");
        }

        var valid = snapshot.PreviousValueKind switch
        {
            RegistryValueDataKind.String or RegistryValueDataKind.ExpandString => snapshot.PreviousStringValue is not null &&
                snapshot.PreviousDWordValue is null &&
                snapshot.PreviousQWordValue is null &&
                snapshot.PreviousMultiStringValue is null &&
                snapshot.PreviousBinaryValue is null,
            RegistryValueDataKind.DWord => snapshot.PreviousDWordValue is not null &&
                snapshot.PreviousStringValue is null &&
                snapshot.PreviousQWordValue is null &&
                snapshot.PreviousMultiStringValue is null &&
                snapshot.PreviousBinaryValue is null,
            RegistryValueDataKind.QWord => snapshot.PreviousQWordValue is not null &&
                snapshot.PreviousStringValue is null &&
                snapshot.PreviousDWordValue is null &&
                snapshot.PreviousMultiStringValue is null &&
                snapshot.PreviousBinaryValue is null,
            RegistryValueDataKind.MultiString => snapshot.PreviousMultiStringValue is not null &&
                snapshot.PreviousStringValue is null &&
                snapshot.PreviousDWordValue is null &&
                snapshot.PreviousQWordValue is null &&
                snapshot.PreviousBinaryValue is null,
            RegistryValueDataKind.Binary => snapshot.PreviousBinaryValue is not null &&
                snapshot.PreviousStringValue is null &&
                snapshot.PreviousDWordValue is null &&
                snapshot.PreviousQWordValue is null &&
                snapshot.PreviousMultiStringValue is null,
            _ => false
        };

        return valid
            ? Result.Success()
            : Result.Failure("operation.snapshot.value_shape_invalid", "Rollback snapshot value data does not match its RegistryValueKind.");
    }

    private static bool SequenceEqual(IReadOnlyList<string>? current, IReadOnlyList<string>? expected)
    {
        if (current is null || expected is null)
        {
            return current is null && expected is null;
        }

        return current.SequenceEqual(expected, StringComparer.Ordinal);
    }

    private static bool BinaryEqual(byte[]? current, byte[]? expected)
    {
        if (current is null || expected is null)
        {
            return current is null && expected is null;
        }

        return current.AsSpan().SequenceEqual(expected);
    }

    private static string ResourceIdentity(RegistryOperationTarget target)
    {
        return $"{target.Hive}\\{target.KeyPath}\\{target.ValueName}";
    }

    private sealed record CapturedRegistryValue(
        RegistryValueDataKind Kind,
        string? StringValue,
        int? DWordValue,
        long? QWordValue,
        IReadOnlyList<string>? MultiStringValue,
        byte[]? BinaryValue);
}
