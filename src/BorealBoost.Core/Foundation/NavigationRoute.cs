namespace BorealBoost.Core.Foundation;

public sealed record NavigationRoute(
    string Key,
    string DisplayName,
    string Description,
    bool IsImplemented)
{
    public static IReadOnlyList<NavigationRoute> FoundationRoutes { get; } =
    [
        new("Dashboard", "Dashboard", "Status inicial da aplicacao.", true),
        new("Analysis", "Analise", "Placeholder da Fase 2/3.", false),
        new("Optimization", "Otimizacao", "Placeholder da Fase 4+.", false),
        new("Drivers", "Drivers", "Placeholder da Fase 7.", false),
        new("Custom", "Personalizado", "Placeholder de selecao futura.", false),
        new("Tools", "Ferramentas", "Placeholder de atalhos futuros.", false),
        new("Results", "Resultados", "Placeholder de relatorios futuros.", false),
        new("Restore", "Restauracao", "Placeholder de safety futuro.", false),
        new("Logs", "Logs", "Placeholder de observabilidade.", false),
        new("Settings", "Configuracoes", "Placeholder de preferencias.", false)
    ];
}
