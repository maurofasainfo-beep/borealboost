namespace BorealBoost.Core.Optimization;

public enum OperationDataClassification
{
    PublicTechnical,
    InternalTechnical,
    Sensitive,
    DoNotLog,
    DoNotReport
}

public sealed record OperationPrivacyRule(
    string FieldPath,
    OperationDataClassification Classification,
    string Reason);

public static class OptimizationPrivacyPolicy
{
    public static IReadOnlyList<OperationPrivacyRule> Rules { get; } =
    [
        new("ExecutionPlan.PlanId", OperationDataClassification.PublicTechnical, "Stable technical session identifier."),
        new("ExecutionPlan.SelectedOptimizationIds", OperationDataClassification.PublicTechnical, "Catalog IDs selected by technician."),
        new("OperationSpec.RegistryValue.Target", OperationDataClassification.InternalTechnical, "May reveal local configuration location."),
        new("OperationSpec.RegistryValue.DesiredState", OperationDataClassification.DoNotLog, "Registry values may contain sensitive data."),
        new("OperationSnapshotItem.PreviousStringValue", OperationDataClassification.DoNotLog, "Previous value may contain secrets."),
        new("OperationSnapshotItem.PreviousDWordValue", OperationDataClassification.InternalTechnical, "Numeric values are still not customer-report content by default."),
        new("OperationJournalEntry.SafeMessage", OperationDataClassification.PublicTechnical, "Sanitized operational outcome.")
    ];

    public static OperationSnapshotItem RedactSnapshotItem(OperationSnapshotItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item with
        {
            PreviousStringValue = item.PreviousStringValue is null ? null : "<redacted>",
            PreviousDWordValue = item.PreviousDWordValue
        };
    }
}
