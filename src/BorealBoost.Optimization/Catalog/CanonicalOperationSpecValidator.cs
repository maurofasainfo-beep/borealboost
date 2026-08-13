using BorealBoost.Core.Optimization;

namespace BorealBoost.Optimization.Catalog;

public sealed class CanonicalOperationSpecValidator
{
    private readonly IOptimizationCatalog _catalog;

    public CanonicalOperationSpecValidator(IOptimizationCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<OptimizationIssue> Validate(
        string catalogVersion,
        OptimizationId optimizationId,
        OperationSpec operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var issues = new List<OptimizationIssue>();
        if (!string.Equals(catalogVersion, _catalog.CatalogVersion, StringComparison.Ordinal))
        {
            issues.Add(Issue("agent.catalog.version_mismatch", "CatalogVersion does not match the trusted built-in catalog.", optimizationId.ToString()));
            return issues;
        }

        var definition = _catalog.Find(optimizationId);
        if (definition is null)
        {
            issues.Add(Issue("agent.catalog.optimization_unknown", "OptimizationId is not present in the trusted built-in catalog.", optimizationId.ToString()));
            return issues;
        }

        var canonical = definition.OperationSpecs.SingleOrDefault(candidate => candidate.OperationId == operation.OperationId);
        if (canonical is null)
        {
            issues.Add(Issue("agent.catalog.operation_unknown", "OperationId is not present in the trusted optimization definition.", operation.OperationId.ToString()));
            return issues;
        }

        if (!Equivalent(canonical, operation))
        {
            issues.Add(Issue("agent.catalog.operation_mismatch", "OperationSpec does not match the trusted canonical catalog definition.", operation.OperationId.ToString()));
        }

        return issues;
    }

    public static bool Equivalent(OperationSpec expected, OperationSpec actual)
    {
        return expected.OperationId == actual.OperationId &&
               expected.OperationType == actual.OperationType &&
               RegistryEquivalent(expected.RegistryValue, actual.RegistryValue) &&
               expected.TimeoutPolicy == actual.TimeoutPolicy &&
               RetryEquivalent(expected.RetryPolicy, actual.RetryPolicy) &&
               expected.Idempotency == actual.Idempotency &&
               expected.Reversibility == actual.Reversibility &&
               expected.RebootBoundary == actual.RebootBoundary &&
               expected.FailurePolicy == actual.FailurePolicy &&
               expected.VerificationStrategy == actual.VerificationStrategy &&
               expected.RollbackStrategy == actual.RollbackStrategy &&
               SnapshotRequirementsEquivalent(expected.SnapshotRequirements, actual.SnapshotRequirements);
    }

    private static bool RegistryEquivalent(RegistryValueOperationParameters? expected, RegistryValueOperationParameters? actual)
    {
        if (expected is null || actual is null)
        {
            return expected is null && actual is null;
        }

        return expected.Target == actual.Target &&
               RegistryStateEquivalent(expected.DesiredState, actual.DesiredState);
    }

    private static bool RegistryStateEquivalent(RegistryValueState expected, RegistryValueState actual)
    {
        return expected.Exists == actual.Exists &&
               expected.ValueKind == actual.ValueKind &&
               string.Equals(expected.StringValue, actual.StringValue, StringComparison.Ordinal) &&
               expected.DWordValue == actual.DWordValue &&
               expected.QWordValue == actual.QWordValue &&
               SequenceEqual(expected.MultiStringValue, actual.MultiStringValue) &&
               BinaryEqual(expected.BinaryValue, actual.BinaryValue);
    }

    private static bool RetryEquivalent(OperationRetryPolicy expected, OperationRetryPolicy actual)
    {
        return expected.RetryAllowed == actual.RetryAllowed &&
               expected.MaxAttempts == actual.MaxAttempts &&
               expected.Backoff == actual.Backoff &&
               expected.RetryableFailures.SequenceEqual(actual.RetryableFailures);
    }

    private static bool SnapshotRequirementsEquivalent(
        IReadOnlyList<SnapshotRequirement> expected,
        IReadOnlyList<SnapshotRequirement> actual)
    {
        return expected.Count == actual.Count &&
               expected.Zip(actual).All(pair => pair.First == pair.Second);
    }

    private static bool SequenceEqual(IReadOnlyList<string>? expected, IReadOnlyList<string>? actual)
    {
        if (expected is null || actual is null)
        {
            return expected is null && actual is null;
        }

        return expected.SequenceEqual(actual, StringComparer.Ordinal);
    }

    private static bool BinaryEqual(byte[]? expected, byte[]? actual)
    {
        if (expected is null || actual is null)
        {
            return expected is null && actual is null;
        }

        return expected.AsSpan().SequenceEqual(actual);
    }

    private static OptimizationIssue Issue(string code, string message, string scope)
    {
        return new OptimizationIssue(code, message, scope, OperationErrorCategory.ValidationFailed);
    }
}
