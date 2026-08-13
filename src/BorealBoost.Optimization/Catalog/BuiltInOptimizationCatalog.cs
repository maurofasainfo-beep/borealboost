using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Optimization.Catalog;

public sealed class BuiltInOptimizationCatalog : IOptimizationCatalog
{
    public const string CurrentSchemaVersion = "4.0.0";
    public const string CurrentCatalogVersion = "4.0.0-built-in-foundation";
    public static readonly OptimizationId IntegrationProofOptimizationId = new("BB.OPT.INTEGRATION.REGISTRY_PROOF");
    public static readonly OperationId IntegrationProofOperationId = new("BB.OP.INTEGRATION.REGISTRY_PROOF.SET_VALUE");
    public const string IntegrationProofValue = "BorealBoost-Controlled-Phase4";

    private readonly IReadOnlyList<OptimizationDefinition> _definitions;

    public BuiltInOptimizationCatalog()
    {
        _definitions = [CreateIntegrationProof()];
    }

    public string SchemaVersion => CurrentSchemaVersion;

    public string CatalogVersion => CurrentCatalogVersion;

    public IReadOnlyList<OptimizationDefinition> GetDefinitions() => _definitions;

    public OptimizationDefinition? Find(OptimizationId optimizationId)
    {
        return _definitions.FirstOrDefault(definition => definition.OptimizationId == optimizationId);
    }

    private static OptimizationDefinition CreateIntegrationProof()
    {
        var snapshotRequirement = new SnapshotRequirement(
            OperationResourceType.RegistryValue,
            SnapshotRequirementKind.Required,
            "HKCU registry value read before controlled mutation",
            BlockIfUnavailable: true,
            "InternalTechnical");

        var operation = new OperationSpec(
            IntegrationProofOperationId,
            OperationType.BorealIntegrationRegistryValue,
            new RegistryValueOperationParameters(
                new RegistryOperationTarget(
                    RegistryHiveKind.CurrentUser,
                    AgentOperationSecurityValidator.IntegrationTestKeyPath,
                    AgentOperationSecurityValidator.IntegrationTestValueName,
                    RegistryViewKind.Default),
                new RegistryValueState(
                    Exists: true,
                    RegistryValueDataKind.String,
                    IntegrationProofValue,
                    null)),
            new OperationTimeoutPolicy(TimeSpan.FromSeconds(5)),
            new OperationRetryPolicy(
                RetryAllowed: true,
                MaxAttempts: 2,
                Backoff: TimeSpan.FromMilliseconds(50),
                [OperationErrorCategory.ApplyFailed]),
            OperationIdempotency.Idempotent,
            OperationReversibility.Full,
            RebootBoundary.None,
            OperationFailurePolicy.AttemptRollback,
            new OperationVerificationStrategy(OperationVerificationKind.ExactState, "Read back the controlled registry value and compare exact state."),
            new OperationRollbackStrategy(OperationRollbackKind.SnapshotRestore, "Restore the pre-operation value or absence captured in OperationSnapshot."),
            [snapshotRequirement]);

        return new OptimizationDefinition(
            IntegrationProofOptimizationId,
            "4.0.0",
            "Controlled engine proof",
            "Validates the Phase 4 transaction pipeline using only BorealBoost's HKCU integration-test registry value.",
            OptimizationCategory.IntegrationTest,
            OptimizationRiskLevel.Safe,
            OptimizationEvidenceLevel.Strong,
            new SupportedWindowsRequirement(
                [WindowsCompatibilityStatus.Supported, WindowsCompatibilityStatus.LegacySupported],
                MinimumBuild: 19045,
                MaximumBuild: null,
                "X64"),
            [],
            [],
            [],
            [],
            RequiresElevation: false,
            RequiresRestart: false,
            SupportsUndo: true,
            RestorePointRequirement.NotRequired,
            [snapshotRequirement],
            [operation],
            [new VerificationSpec(operation.OperationId, operation.VerificationStrategy)],
            [new RollbackSpec(operation.OperationId, operation.RollbackStrategy)],
            OperationFailurePolicy.AttemptRollback,
            new TimeoutPolicy(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)),
            new SourceMetadata("BuiltIn", CurrentCatalogVersion, "OPTIMIZATION_ENGINE.md", null));
    }
}
