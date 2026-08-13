using BorealBoost.Core.Common;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Core.Analysis;

public readonly record struct AnalysisId(Guid Value)
{
    public static AnalysisId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public enum AnalysisCategory
{
    System,
    Performance,
    Gaming,
    Power,
    Graphics,
    Memory,
    Storage,
    Startup,
    Services,
    Network,
    Drivers,
    Security,
    Privacy,
    Windows,
    Maintenance
}

public enum AnalysisRuleStatus
{
    NotApplicable,
    Healthy,
    Opportunity,
    Warning,
    Blocked,
    Unknown
}

public enum RecommendationRiskLevel
{
    Safe,
    Medium,
    Advanced,
    Aggressive
}

public enum RecommendationEvidenceLevel
{
    Strong,
    Moderate,
    Experimental,
    Unknown
}

public enum RecommendationCompatibilityStatus
{
    Compatible,
    Incompatible,
    Conditional,
    Unknown
}

public enum ExpectedImpactLevel
{
    Low,
    Moderate,
    PotentiallyHigh,
    WorkloadDependent,
    Unknown
}

public enum RecommendationReversibility
{
    Full,
    Partial,
    None,
    Unknown
}

[Flags]
public enum RecommendationPresetEligibility
{
    None = 0,
    Basic = 1,
    Medium = 2,
    Advanced = 4,
    Custom = 8
}

public enum RecommendationPreset
{
    Basic,
    Medium,
    Advanced,
    Custom
}

public enum AnalysisSessionState
{
    Idle,
    Running,
    Cancelling,
    Completed,
    Failed,
    Cancelled
}

public interface IAnalysisRule
{
    AnalysisRuleMetadata Metadata { get; }

    AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot);
}

public interface IAnalysisEngine
{
    Task<Result<AnalysisResult>> AnalyzeAsync(SystemSnapshot snapshot, CancellationToken cancellationToken);
}

public interface IAnalysisResultStore
{
    AnalysisResult? Current { get; }

    void Set(AnalysisResult result);

    void Clear();
}

public interface IAnalysisSessionService
{
    AnalysisSessionState State { get; }

    AnalysisResult? Current { get; }

    Task<Result<AnalysisResult>> AnalyzeCurrentSnapshotAsync(CancellationToken cancellationToken);

    void Cancel();
}
