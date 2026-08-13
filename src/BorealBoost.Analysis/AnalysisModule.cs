using BorealBoost.Analysis.RecommendationEngine;

namespace BorealBoost.Analysis;

public static class AnalysisModule
{
    public const string Name = "Analysis";
    public const string Phase = "Fase 3";
    public const bool ScannerIsOperational = true;
    public const bool RecommendationEngineIsOperational = true;
    public const string EngineVersion = AnalysisEngine.EngineVersion;
    public const string RuleCatalogVersion = AnalysisEngine.RuleCatalogVersion;
}
