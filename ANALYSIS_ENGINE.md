# BorealBoost - Analysis Engine

Data: 2026-08-13
Status: Fase 3 implementada como engine read-only de analise e recomendacoes.
Revalidacao: correcoes finais da Fase 3 aplicadas antes da Fase 4.

## Objetivo

O Analysis Engine transforma um `SystemSnapshot` ja coletado pelo Scanner em findings, oportunidades, avisos, bloqueios e recomendacoes estruturadas.

Separacao obrigatoria:

- Scanner = fatos detectados.
- Analysis = interpretacao dos fatos.
- Recommendation = sugestao estruturada de acao futura.
- Optimization = execucao futura, fora da Fase 3.
- Rollback = recuperacao futura, fora da Fase 3.

Nesta fase o engine nao altera Windows, nao executa comandos, nao consulta WMI/Registry/Network/Process diretamente e nao inicia Driver Engine, Benchmark, Rollback ou Boreal Score operacional.

## Arquitetura

Contratos em `BorealBoost.Core.Analysis`:

- `IAnalysisEngine`;
- `IAnalysisRule`;
- `IAnalysisResultStore`;
- `AnalysisResult`;
- `AnalysisRuleEvaluation`;
- `AnalysisFinding`;
- `Recommendation`;
- `RecommendationPlan`;
- `PresetPreview`;
- enums de categoria, status, risco, evidencia, impacto e compatibilidade.

Implementacao em `BorealBoost.Analysis.RecommendationEngine`:

- `AnalysisEngine`;
- `InMemoryAnalysisResultStore`;
- regras modulares code-first em `RecommendationEngine/Rules`.

O App consome os contratos por DI e apresenta os resultados na pagina `Analise`.

## Lifecycle

1. Scanner produz `SystemSnapshot`.
2. App guarda o snapshot em `ISystemSnapshotStore`.
3. AnalysisPage solicita analise do snapshot existente.
4. `AnalysisSessionService` singleton controla uma unica analise ativa por vez.
5. `AnalysisEngine` executa regras ordenadas por `RuleId`.
6. Falha de uma regra vira warning tecnico isolado e nao gera recomendacao falsa.
7. `RecommendationModelValidator` valida invariantes de recomendacoes e plano.
8. `RecommendationId` duplicado ou metadata incoerente bloqueia a analise com falha observavel; nao ha deduplicacao silenciosa.
9. `RecommendationPlan` monta preview dos presets Basico, Medio, Avancado e Custom somente apos validacao.
10. `AnalysisResult` e guardado em memoria por `IAnalysisResultStore`.

## Versionamento

`AnalysisResult` registra:

- `AnalysisId`;
- `ScanId`;
- `StartedAtUtc`;
- `CompletedAtUtc`;
- `Duration`;
- `EngineVersion`;
- `RuleCatalogVersion`.

Versoes iniciais:

- `EngineVersion = 3.0.0`;
- `RuleCatalogVersion = 3.0.0-code-first`;
- `RecommendationPlan.PlanVersion = 3.0.0-preview`.

Dado o mesmo `SystemSnapshot` e o mesmo `RuleCatalogVersion`, RuleIds, estados, RecommendationIds, risco, evidencia, compatibilidade, presets, conflitos e requisitos devem ser deterministicos. `AnalysisId`, timestamps e duracao podem variar.

## Validacao de Invariantes

O engine executa validacao central antes de publicar resultado:

- `RecommendationId` e `RuleId` obrigatorios, estaveis e no formato BorealBoost;
- `RecommendationId` unico;
- categoria, risco, evidencia, impacto, reversibilidade e compatibilidade validos;
- recomendacoes Advanced/Aggressive exigem confirmacao futura e nao entram em Basico/Medio;
- evidencia Experimental nao entra em Basico/Medio;
- compatibilidade Unknown/Incompatible nao entra em presets;
- `FutureOptimizationId`, quando existir, deve ser referencia estavel e nao executavel;
- `ConflictsWith` e `Requires` nao podem apontar para si mesmos nem para IDs inexistentes.

Falha de validacao retorna `analysis.validation.failed`, registra erro tecnico e impede plano ambiguo.

## Status de Regra

- `Healthy`: fato conhecido e sem oportunidade/aviso nesta regra.
- `Opportunity`: oportunidade tecnicamente defensavel com recomendacao estruturada.
- `Warning`: aviso ou guardrail, sem necessariamente indicar melhoria.
- `Blocked`: contexto bloqueia planejamento automatico futuro.
- `Unknown`: dados insuficientes; nunca vira oportunidade automatica.
- `NotApplicable`: regra nao se aplica ao ambiente.

## Risk Level

- `Safe`: baixo risco, normalmente observacional ou reversivel futuramente.
- `Medium`: exige revisao tecnica antes de apply futuro.
- `Advanced`: pode afetar compatibilidade, seguranca ou comportamento se virar otimizacao.
- `Aggressive`: reservado para riscos altos futuros; nenhuma regra inicial gera Aggressive.

Advanced/Aggressive nunca significam "melhor"; significam maior cautela.

## Evidence Level

- `Strong`: fato objetivo do snapshot e interpretacao direta.
- `Moderate`: fato objetivo com impacto dependente do contexto.
- `Experimental`: evidencia limitada; nao entra em Basic/Medium automaticamente.
- `Unknown`: sem recomendacao automatica.

## Compatibility

Cada recomendacao declara:

- `Compatible`;
- `Conditional`;
- `Incompatible`;
- `Unknown`;
- razoes.

Compatibilidade desconhecida nao autoriza apply futuro. A Fase 3 apenas prepara metadados para fases posteriores.

## Presets

Presets sao apenas preview:

- Basico: recomendacoes Safe e altamente compativeis/observacionais.
- Medio: Safe + Medium quando condicionais forem aceitaveis para revisao futura.
- Avancado: inclui Advanced com aviso e confirmacao futura.
- Custom: permite revisao individual futura.

Nenhum preset executa alteracao nesta fase.

## Regras Iniciais

| RuleId | Categoria | Condicao principal | Status possiveis | Risk | Evidence |
| --- | --- | --- | --- | --- | --- |
| `BB.SYSTEM.001` | System | `Metadata.PartialScan=true` | Healthy, Warning | Safe | Strong |
| `BB.WINDOWS.001` | Windows | compatibilidade funcional Windows | Healthy, Warning, Blocked, Unknown | Safe/Advanced | Strong/Unknown |
| `BB.DRIVER.001` | Drivers | dispositivo com `MissingDriver` | Healthy, Opportunity, Unknown | Medium | Strong |
| `BB.DRIVER.002` | Drivers | dispositivo `Problem` ou `Disabled` | Healthy, Opportunity, Unknown | Medium | Strong |
| `BB.GRAPHICS.001` | Graphics | Microsoft Basic Display Adapter em maquina fisica; GPU virtual/generica em VM vira NotApplicable | Healthy, Opportunity, Unknown, NotApplicable | Medium | Strong/Moderate/Unknown |
| `BB.STORAGE.001` | Storage | volume do sistema com < 10% livre ou < 20 GiB livre | Healthy, Opportunity, Unknown | Safe | Strong |
| `BB.SYSTEM.002` | System | maquina virtual detectada | Healthy, Blocked, Unknown | Safe | Strong/Moderate |
| `BB.POWER.001` | Power | notebook/tablet/convertible/bateria | Healthy, Warning, Unknown | Advanced | Moderate |
| `BB.STARTUP.001` | Startup | 30 ou mais itens de inicializacao como inventario elevado, sem inferir degradacao | Healthy, Warning, Unknown | Safe | Experimental |
| `BB.SECURITY.001` | Security | Secure Boot known disabled/active/not supported | Healthy, Warning, NotApplicable, Unknown | Advanced | Strong/Unknown |
| `BB.MEMORY.001` | Memory | diferenca instalada/visivel > 512 MiB | Healthy, Warning, Unknown | Safe | Moderate |

## Privacidade

`AnalysisResult` nao duplica inventarios sensiveis completos. Evidencias usam contagens, status e campos tecnicos minimos. Device Instance IDs, Hardware IDs, Compatible IDs, INF, processos, services e detalhes de rede permanecem protegidos pela politica de redaction da Fase 2.

## Uso pela Fase 5

A Fase 5 consome `SystemSnapshot` e `AnalysisResult` para calcular Preset Preview do Catalog V1. Analysis continua read-only: ele nao executa OperationSpec nem consulta Windows diretamente durante regras.

O PresetEngine da Fase 5 exige que `AnalysisResult.ScanId` corresponda ao `SystemSnapshot.Metadata.ScanId`. Resultado stale bloqueia selecao automatica.

## Limitacoes

- `FutureOptimizationId` continua apenas referencia; as regras de Analysis nao criam nem executam operacoes.
- Nao ha driver download/install/update.
- Nao ha benchmark ou FPS medido.
- Nao ha Boreal Score operacional.
- Windows 10 22H2 build 19045 segue pendente de validacao real/VM.
- Regras atuais sao conservadoras e intencionalmente poucas.
- A regra de startup e observacional/experimental ate existir classificacao por item e evidencia de impacto.
- GPU virtual/generica em VM nao gera recomendacao de driver grafico fisico.
