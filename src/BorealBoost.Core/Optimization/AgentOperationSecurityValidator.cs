using BorealBoost.Core.Common;

namespace BorealBoost.Core.Optimization;

public sealed class AgentOperationSecurityValidator
{
    public const int MaxTargetLength = 256;
    public const int MaxValueNameLength = 64;
    public const int MaxStringValueLength = 256;
    public const int MaxMultiStringItems = 32;
    public const int MaxBinaryValueBytes = 1024;
    public const string IntegrationTestKeyPath = @"Software\BorealBoost\IntegrationTest";
    public const string IntegrationTestValueName = "Phase4ControlledValue";
    public static readonly TimeSpan MaxOperationTimeout = TimeSpan.FromSeconds(30);

    public Result Validate(OperationSpec operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!OperationId.TryCreate(operation.OperationId.Value, out _))
        {
            return Result.Failure("agent.operation.operation_id_invalid", "OperationId is invalid.");
        }

        if (!Enum.IsDefined(operation.OperationType))
        {
            return Result.Failure("agent.operation.type_unknown", "OperationType is unknown.");
        }

        if (operation.OperationType is not (OperationType.BorealIntegrationRegistryValue or OperationType.RegistryValue))
        {
            return Result.Failure("agent.operation.type_not_allowed", "OperationType is not allowlisted for the trusted BorealBoost catalog.");
        }

        if (operation.RegistryValue is null)
        {
            return Result.Failure("agent.operation.payload_invalid", "RegistryValue payload is required for this operation type.");
        }

        var target = operation.RegistryValue.Target;
        if (target.Hive is not (RegistryHiveKind.CurrentUser or RegistryHiveKind.LocalMachine))
        {
            return Result.Failure("agent.operation.hive_not_allowed", "Registry hive is not allowlisted for trusted operations.");
        }

        if (!Enum.IsDefined(target.View))
        {
            return Result.Failure("agent.operation.registry_view_invalid", "Registry view is invalid.");
        }

        if (!IsAllowedText(target.KeyPath, MaxTargetLength))
        {
            return Result.Failure("agent.operation.target_not_allowed", "Registry target is outside the BorealBoost allowlist.");
        }

        if (!IsAllowedText(target.ValueName, MaxValueNameLength))
        {
            return Result.Failure("agent.operation.value_name_not_allowed", "Registry value name is outside the BorealBoost allowlist.");
        }

        if (operation.OperationType == OperationType.BorealIntegrationRegistryValue)
        {
            if (target.Hive != RegistryHiveKind.CurrentUser ||
                !string.Equals(target.KeyPath, IntegrationTestKeyPath, StringComparison.Ordinal) ||
                !string.Equals(target.ValueName, IntegrationTestValueName, StringComparison.Ordinal))
            {
                return Result.Failure("agent.operation.target_not_allowed", "Registry target is outside the BorealBoost integration-test allowlist.");
            }
        }
        else if (!TrustedRegistryOperationTargets.IsTrustedCatalogOperation(operation))
        {
            return Result.Failure("agent.operation.target_not_allowed", "Registry target or desired state is not present in the trusted catalog allowlist.");
        }

        var desired = operation.RegistryValue.DesiredState;
        if (!Enum.IsDefined(desired.ValueKind))
        {
            return Result.Failure("agent.operation.value_kind_invalid", "Registry desired value kind is invalid.");
        }

        if (desired.ValueKind == RegistryValueDataKind.Unsupported)
        {
            return Result.Failure("agent.operation.value_kind_unsupported", "Registry desired value kind is not supported.");
        }

        if (!desired.Exists)
        {
            if (desired.StringValue is not null ||
                desired.DWordValue is not null ||
                desired.QWordValue is not null ||
                desired.MultiStringValue is not null ||
                desired.BinaryValue is not null)
            {
                return Result.Failure("agent.operation.desired_absent_has_value", "Absent desired state cannot carry value data.");
            }

            return ValidatePolicies(operation);
        }

        if (desired is { ValueKind: RegistryValueDataKind.String or RegistryValueDataKind.ExpandString } &&
            (desired.StringValue is null || desired.StringValue.Length > MaxStringValueLength))
        {
            return Result.Failure("agent.operation.string_value_invalid", "Registry string value is invalid.");
        }

        if (desired is { ValueKind: RegistryValueDataKind.DWord } && desired.DWordValue is null)
        {
            return Result.Failure("agent.operation.dword_value_invalid", "Registry DWORD value is required.");
        }

        if (desired is { ValueKind: RegistryValueDataKind.QWord } && desired.QWordValue is null)
        {
            return Result.Failure("agent.operation.qword_value_invalid", "Registry QWORD value is required.");
        }

        if (desired.ValueKind == RegistryValueDataKind.MultiString)
        {
            if (desired.MultiStringValue is null ||
                desired.MultiStringValue.Count > MaxMultiStringItems ||
                desired.MultiStringValue.Any(value => value is null || value.Length > MaxStringValueLength))
            {
                return Result.Failure("agent.operation.multi_string_value_invalid", "Registry multi-string value is invalid.");
            }
        }

        if (desired.ValueKind == RegistryValueDataKind.Binary)
        {
            if (desired.BinaryValue is null || desired.BinaryValue.Length > MaxBinaryValueBytes)
            {
                return Result.Failure("agent.operation.binary_value_invalid", "Registry binary value is invalid.");
            }
        }

        return ValidatePolicies(operation);
    }

    private static Result ValidatePolicies(OperationSpec operation)
    {
        if (operation.TimeoutPolicy.Timeout <= TimeSpan.Zero ||
            operation.TimeoutPolicy.Timeout > MaxOperationTimeout)
        {
            return Result.Failure("agent.operation.timeout_invalid", "Operation timeout is outside the allowlisted range.");
        }

        if (operation.RetryPolicy.MaxAttempts is < 1 or > 3)
        {
            return Result.Failure("agent.operation.retry_invalid", "Retry policy is outside the allowlisted range.");
        }

        if (operation.Reversibility == OperationReversibility.Full &&
            operation.SnapshotRequirements.All(requirement => requirement.Requirement != SnapshotRequirementKind.Required))
        {
            return Result.Failure("agent.operation.snapshot_required", "Fully reversible operations require a mandatory snapshot.");
        }

        if (operation.RollbackStrategy.Kind != OperationRollbackKind.SnapshotRestore)
        {
            return Result.Failure("agent.operation.rollback_invalid", "Trusted registry operations must rollback from snapshot.");
        }

        return Result.Success();
    }

    private static bool IsAllowedText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
        {
            return false;
        }

        if (value.Contains("..", StringComparison.Ordinal) ||
            value.Contains('/', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var character in value)
        {
            var valid = character is '\\' or '.' or '_' or '-' ||
                        character is >= 'A' and <= 'Z' ||
                        character is >= 'a' and <= 'z' ||
                        character is >= '0' and <= '9';
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }
}
