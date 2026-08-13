# CURRENT_TASK.md
# BorealBoost — Fase 3: Analysis + Recommendation Engine

> Fase 2 — System Scanner: APROVADA.
>
> Esta fase transforma os fatos coletados pelo Scanner em análises, oportunidades e recomendações técnicas.
>
> O BorealBoost ainda NÃO deve modificar o Windows nesta fase.

---

# 1. STATUS

Concluído:

✅ FASE 0 — Discovery e Arquitetura  
✅ FASE 1 — Foundation  
✅ FASE 2 — System Scanner

Fase atual:

🚧 FASE 3 — ANALYSIS + RECOMMENDATION ENGINE

---

# 2. OBJETIVO

Implementar:

SystemSnapshot
↓
Analysis Engine
↓
Compatibility Evaluation
↓
Opportunity Detection
↓
Recommendation Engine
↓
Recommendation Plan
↓
UI

O objetivo é responder:

"O que pode ser melhorado nesta máquina?"

sem ainda executar:

"Faça essa alteração."

---

# 3. REGRA FUNDAMENTAL

Manter separação rigorosa:

Scanner
= fatos

Analysis
= interpretação

Recommendation
= sugestão

Optimization
= execução

Rollback
= recuperação

Nesta fase:

Scanner → existente
Analysis → implementar
Recommendation → implementar
Optimization → NÃO executar
Rollback → NÃO executar

---

# 4. LEITURA OBRIGATÓRIA

Antes de implementar, leia integralmente:

- BOREALBOOST_MASTER_SPEC.md
- CODEX_BOOTSTRAP.md
- CURRENT_TASK.md
- REQUIREMENTS.md
- ARCHITECTURE.md
- ARCHITECTURE_DECISION_RECORD.md
- DOMAIN_MODEL.md
- OPTIMIZATION_ENGINE.md
- ROLLBACK_ENGINE.md
- SECURITY.md
- COMPATIBILITY_MATRIX.md
- IMPLEMENTATION_ROADMAP.md
- UX_SPECIFICATION.md
- SYSTEM_SCANNER.md
- BOREAL_SCORE.md
- PHASE2_AUDIT.md
- PHASE2_REVALIDATION.md

Depois analise o código atual integralmente.

Não contradiga decisões aprovadas silenciosamente.

---

# 5. ROADMAP OTIMIZADO

A partir desta fase, utilizar roadmap consolidado:

FASE 3
Analysis + Recommendation Engine

FASE 4
Optimization Engine + Safety + Snapshot + Rollback

FASE 5
Optimization Catalog — Safe + Medium + Advanced/Aggressive

FASE 6
Drivers + Benchmark + Results + Reporting

FASE 7
Installer + Hardening + Production Readiness

Este roadmap substitui a divisão excessivamente granular anterior.

Atualize IMPLEMENTATION_ROADMAP.md de forma coerente.

Não antecipe essas fases.

---

# 6. NÃO ALTERAR O WINDOWS

Esta fase deve permanecer essencialmente read-only.

NÃO:

- escrever Registry;
- alterar Services;
- alterar Startup;
- modificar Power Plan;
- alterar DNS;
- modificar Network;
- remover AppX;
- alterar Windows Features;
- modificar Defender;
- modificar Firewall;
- alterar VBS;
- alterar Memory Integrity;
- instalar drivers;
- atualizar drivers;
- remover drivers;
- executar Windows Update;
- executar DISM/SFC operacional;
- executar tweaks;
- executar scripts;
- aplicar recomendações.

Uma Recommendation é somente uma descrição estruturada de uma possível ação futura.

---

# 7. PRINCÍPIO DE EVIDÊNCIA

O BorealBoost não deve recomendar algo apenas porque:

- aparece em vídeos de otimização;
- é popular;
- existe no WinUtil;
- alguém chamou de "FPS tweak";
- existe um Registry tweak conhecido.

Toda Recommendation deve possuir justificativa técnica estruturada.

---

# 8. EVIDENCE LEVEL

Definir classificação consistente.

Sugestão conceitual:

Strong
Moderate
Experimental
Unknown

ou nomenclatura equivalente já aprovada.

Strong:
evidência técnica/documentação confiável e benefício contextual claro.

Moderate:
benefício plausível e tecnicamente justificável, mas dependente do cenário.

Experimental:
resultado altamente dependente do hardware/workload ou evidência limitada.

Unknown:
não deve ser recomendado automaticamente.

Não inventar Strong para dar aparência de qualidade.

---

# 9. RISK LEVEL

Cada Recommendation deve possuir risco.

No mínimo:

Safe
Medium
Advanced
Aggressive

ou estrutura equivalente compatível com os presets desejados.

Safe:
baixo risco operacional.

Medium:
alteração mais relevante, mas normalmente reversível.

Advanced:
pode afetar recursos/comportamento e exige maior cuidado.

Aggressive:
pode comprometer compatibilidade, segurança, estabilidade, funcionalidades ou manutenção.

Aggressive nunca deve significar:

"melhor".

Significa:

"maior impacto e maior risco".

---

# 10. PRESETS

Preparar modelo para os presets desejados:

Básico
Médio
Avançado

E permitir futuramente:

Personalizado

Não aplicar presets nesta fase.

Apenas definir como recomendações poderão ser agrupadas.

---

# 11. BASIC PRESET

Conceitualmente deverá aceitar somente recomendações:

- altamente compatíveis;
- reversíveis;
- baixo risco;
- sem redução relevante de segurança;
- sem dependência frágil de hardware.

---

# 12. MEDIUM PRESET

Poderá incluir:

Safe + Medium

desde que compatíveis com a máquina.

Não incluir automaticamente recomendações Aggressive.

---

# 13. ADVANCED PRESET

Poderá apresentar:

Safe
Medium
Advanced
Aggressive

Mas recomendações de risco elevado devem possuir:

- aviso;
- motivo;
- impacto;
- compatibilidade;
- rollback futuro;
- consentimento explícito futuro.

Nesta fase apenas modelar.

---

# 14. ANALYSIS ENGINE

Implementar motor desacoplado da UI.

Conceitualmente:

IAnalysisEngine

recebe:

SystemSnapshot

retorna:

AnalysisResult

AnalysisResult deve representar:

- oportunidades;
- observações;
- incompatibilidades;
- warnings;
- recommendations;
- metadata.

Não acessar UI diretamente.

---

# 15. RULE ENGINE

Evitar um método gigante:

Analyze(SystemSnapshot snapshot)
{
    if (...)
    if (...)
    if (...)
    ...
}

Criar regras independentes.

Exemplo conceitual:

IAnalysisRule

com:

RuleId
Category
Evaluate(snapshot)
Result

Cada regra deve ser testável isoladamente.

---

# 16. IDENTIDADE DAS REGRAS

Cada regra deve possuir identificador estável.

Exemplo:

BB.POWER.001
BB.STARTUP.001
BB.DRIVER.001

Definir convenção consistente.

IDs não devem depender do texto exibido na UI.

---

# 17. CATEGORIAS

Preparar categorias como:

System
Performance
Gaming
Power
Graphics
Memory
Storage
Startup
Services
Network
Drivers
Security
Privacy
Windows
Maintenance

Não é obrigatório preencher todas nesta fase.

---

# 18. ANALYSIS RESULT

Uma regra pode retornar conceitualmente:

NotApplicable
Healthy
Opportunity
Warning
Blocked
Unknown

Não transformar Unknown em Opportunity.

---

# 19. RECOMMENDATION MODEL

Criar modelo robusto.

Cada Recommendation deve possuir, conforme aplicável:

- RecommendationId
- RuleId
- Title
- ShortDescription
- TechnicalReason
- Category
- RiskLevel
- EvidenceLevel
- Compatibility
- DetectedState
- DesiredState
- ExpectedImpact
- SideEffects
- RebootRequired
- Reversible
- PresetEligibility
- UserConfirmationRequired
- FutureOptimizationId
- Source/Evidence metadata

Não preencher com textos falsos.

---

# 20. EXPECTED IMPACT

Não prometer números inventados.

PROIBIDO:

"+30 FPS"
"-20 ms"
"+40% performance"

sem benchmark/evidência real específica.

Utilizar classificações qualitativas quando necessário:

Low
Moderate
PotentiallyHigh
WorkloadDependent
Unknown

ou modelo equivalente.

---

# 21. FPS

O objetivo comercial inclui gaming/FPS.

Mas Recommendation Engine deve ser tecnicamente honesto.

Distinguir:

- FPS médio;
- 1% low;
- frame-time;
- stutter;
- input latency;
- background contention;
- boot time;
- responsiveness;
- power consumption.

Uma otimização pode melhorar:

background contention

sem necessariamente aumentar:

average FPS.

Não chamar tudo de "FPS boost".

---

# 22. COMPATIBILITY ENGINE

Criar avaliação reutilizável.

Uma Recommendation deve poder declarar:

Compatible
Incompatible
Conditional
Unknown

E razões.

Exemplo:

Recommendation:
High Performance Power Policy

Compatibility:
Conditional

Reason:
Notebook detected.

Isso será usado futuramente pelo Optimization Engine.

---

# 23. HARDWARE AWARENESS

As recomendações devem considerar fatos reais.

Exemplos:

Laptop
≠
Desktop

Integrated GPU
≠
Dedicated GPU

NVMe
≠
HDD

Windows 10
≠
Windows 11

VM
≠
PhysicalMachine

AMD
≠
Intel

NVIDIA
≠
AMD
≠
Intel

Nunca aplicar regra universal quando o contexto importa.

---

# 24. LAPTOP

Evitar recomendações agressivas de energia automaticamente em notebook.

Considerar:

- bateria;
- AC;
- temperatura não disponível;
- autonomia;
- OEM power management.

Futuramente o usuário poderá optar por performance máxima.

Mas Analysis deve informar trade-off.

---

# 25. VM

Máquina virtual deve bloquear ou limitar recomendações incompatíveis.

Não recomendar ajustes específicos de hardware físico quando executado em VM sem evidência adequada.

---

# 26. STARTUP ANALYSIS

Pode analisar fatos já coletados sobre Startup.

Identificar oportunidades apenas quando houver evidência suficiente.

Não classificar automaticamente todo startup como ruim.

Não recomendar desabilitar:

- antivírus;
- drivers;
- software crítico;
- componentes desconhecidos;

sem conhecimento suficiente.

Unknown publisher/item deve ser tratado com cautela.

---

# 27. PROCESS ANALYSIS

Processes podem ajudar a detectar:

- alta quantidade de processos;
- background activity;

mas NÃO recomendar encerrar processo específico apenas por existir.

Evitar heurística:

"muitos processos = PC ruim".

Não criar kill process nesta fase.

---

# 28. SERVICES ANALYSIS

Pode estruturar arquitetura para futuras recomendações de Services.

Porém:

não criar lista gigantesca de "serviços inúteis" sem validação técnica.

Service tweak futuro deve conhecer:

- dependências;
- Windows edition;
- hardware;
- features;
- rollback;
- side effects.

Nesta fase, mantenha recomendações conservadoras.

---

# 29. POWER ANALYSIS

Pode analisar:

- desktop/notebook;
- AC/battery;
- current power context;
- CPU/hardware.

Pode detectar oportunidade futura de perfil de performance.

Não criar plano de energia nesta fase.

---

# 30. GPU ANALYSIS

Pode detectar:

- vendor;
- quantidade;
- driver state;
- Basic Display Adapter;
- device problem;
- multi-GPU.

Não recomendar configurações específicas do painel NVIDIA/AMD que o Scanner não consegue comprovar.

---

# 31. DRIVER ANALYSIS

Pode recomendar conceitualmente:

"Investigar dispositivo sem driver"

quando Scanner possui evidência objetiva.

Não dizer:

"Driver X está desatualizado"

sem fonte de versão mais recente.

Driver Engine real pertence à Fase 6.

---

# 32. STORAGE ANALYSIS

Pode identificar fatos relevantes como:

- pouco espaço livre;
- HDD vs SSD quando confiável;
- system drive próximo da capacidade.

Definir thresholds justificáveis e centralizados.

Não executar limpeza.

---

# 33. MEMORY ANALYSIS

Pode analisar:

- RAM instalada;
- RAM visível;
- módulos;
- quantidade.

Evitar recomendações simplistas como:

"16 GB é ruim".

Contexto importa.

Não recomendar XMP/EXPO sem evidência suficiente.

---

# 34. SECURITY

Não recomendar redução de segurança apenas para aumentar performance.

Itens como:

- Defender;
- Firewall;
- Secure Boot;
- VBS;
- Memory Integrity;

devem possuir tratamento especial.

Uma possível alteração futura que reduza segurança deve ser classificada claramente como risco elevado e nunca entrar silenciosamente em Basic/Medium.

Nesta fase não aplicar nenhuma.

---

# 35. UNKNOWN

Regra crítica:

Unknown
≠
False

Unknown
≠
Opportunity

Unknown
≠
Compatible

Se informação necessária não estiver disponível:

Recommendation pode ser:

Blocked
Conditional
Unknown
NotApplicable

conforme caso.

---

# 36. CONFLITOS

Preparar arquitetura para recomendações conflitantes.

Exemplo conceitual:

Recommendation A
requer configuração X.

Recommendation B
requer configuração Y incompatível.

Criar metadados como:

ConflictsWith
Requires
Supersedes

quando necessário.

Não implementar solver excessivamente complexo se ainda não houver casos reais.

---

# 37. DEPENDÊNCIAS

Preparar:

RequiresRecommendation
RequiresCapability
RequiresState

quando houver necessidade real.

Não criar dependências fictícias.

---

# 38. DEDUPLICAÇÃO

Duas regras não devem gerar recomendações semanticamente duplicadas.

Criar estratégia por RecommendationId/RuleId.

---

# 39. DETERMINISMO

Dado o mesmo:

SystemSnapshot
RuleCatalogVersion

o resultado deve ser essencialmente determinístico.

Não usar aleatoriedade.

Não usar IA generativa para decidir tweaks.

---

# 40. RULE CATALOG

Criar catálogo estruturado das regras.

Pode ser inicialmente code-first se isso for mais seguro para esta fase.

Não criar DSL complexa prematuramente.

Separar:

rule metadata

de:

evaluation logic

quando apropriado.

---

# 41. VERSIONAMENTO

AnalysisResult deve registrar:

- AnalysisId
- ScanId
- StartedAtUtc
- CompletedAtUtc
- EngineVersion
- RuleCatalogVersion

Isso será importante para relatórios e comparação futura.

---

# 42. SOURCES / EVIDENCE

Preparar modelo para associar recomendação à justificativa.

Fontes podem futuramente incluir:

- Microsoft documentation;
- vendor documentation;
- WinUtil como referência funcional;
- evidência interna validada.

Não precisa realizar pesquisa web em runtime.

Não incluir URLs inventadas.

---

# 43. WINUTIL

WinUtil continua sendo referência funcional.

Não copiar:

- código;
- UI;
- marca;
- catálogo cegamente.

Quando uma ideia vier do WinUtil:

reavaliar dentro da arquitetura BorealBoost.

---

# 44. REGRAS INICIAIS

Implementar um conjunto pequeno, porém real e bem testado.

Priorizar regras baseadas em fatos já confiáveis.

Exemplos aceitáveis para avaliação:

- dispositivo com problema;
- dispositivo sem driver;
- Microsoft Basic Display Adapter;
- espaço livre criticamente baixo;
- Windows incompatível/legado;
- scan parcial;
- máquina virtual;
- contexto de energia;
- startup excessivo somente se houver critério defensável;
- capabilities relevantes.

Não criar dezenas de regras ruins para aumentar quantidade.

Qualidade > quantidade.

---

# 45. NÃO CRIAR TWEAK CATALOG AINDA

Recommendation catalog
≠
Optimization catalog.

Uma Recommendation pode apontar para:

FutureOptimizationId

mas a operação ainda não deve existir.

---

# 46. UI — ANÁLISE

Evoluir a experiência após o Scanner.

Fluxo:

Scan
↓
Analyze
↓
Results

ou análise automática após snapshot válido, se coerente com UX_SPECIFICATION.md.

A análise deve ser rápida e não bloquear UI.

---

# 47. ANALYSIS PROGRESS

Se houver etapas perceptíveis:

mostrar progresso real.

Não criar animação artificial longa para parecer que o programa está "pensando".

Se análise levar 50 ms:

mostrar resultado.

---

# 48. SUMMARY

Apresentar resumo como:

Sistema analisado
X oportunidades encontradas
Y avisos
Z itens não aplicáveis

Somente com dados reais.

Não chamar todos de problemas.

---

# 49. RECOMMENDATION CARDS

Cada card deve poder mostrar:

- título;
- descrição;
- categoria;
- risco;
- impacto esperado;
- compatibilidade;
- motivo;
- status.

Design:

BorealBoost dark SaaS premium.

Usar design system existente.

---

# 50. FILTROS

Permitir filtrar quando fizer sentido:

- Todos
- Básico
- Médio
- Avançado
- Categoria
- Risco

Não criar UX excessivamente complexa.

---

# 51. DETALHES

Permitir visualizar:

Por que isso foi recomendado?

Mostrar explicação compreensível.

Separar:

Explicação para cliente

de:

Detalhes técnicos

quando apropriado.

---

# 52. SELEÇÃO

Pode permitir selecionar/desmarcar recomendações para preparar futura otimização.

Porém botão:

"Aplicar"

não deve executar nada nesta fase.

Se existir visualmente:

deve estar desabilitado ou explicitamente marcado como indisponível nesta fase de desenvolvimento.

---

# 53. PRESET PREVIEW

Implementar preview seguro:

Básico
Médio
Avançado

Selecionar preset pode apenas filtrar/marcar recomendações elegíveis.

Não executar.

---

# 54. ADVANCED WARNING

Ao visualizar itens Advanced/Aggressive:

mostrar aviso visual adequado.

Exemplo conceitual:

"Esta recomendação pode afetar compatibilidade, segurança ou comportamento do Windows."

Sem alarmismo.

---

# 55. BOREAL SCORE

NÃO implementar Boreal Score operacional ainda.

Pode haver integração futura prevista no modelo.

Não calcular score com base simplesmente na quantidade de recomendações.

---

# 56. TESTES UNITÁRIOS

Cobrir:

- AnalysisRule;
- rule evaluation;
- compatibility;
- risk;
- evidence;
- Unknown;
- NotApplicable;
- Blocked;
- deduplication;
- conflicts;
- preset eligibility;
- deterministic result;
- metadata/versioning.

Cada regra real adicionada deve possuir testes positivos e negativos.

---

# 57. SNAPSHOT FIXTURES

Criar builders/fixtures para cenários.

Exemplos:

DesktopGaming
Laptop
VirtualMachine
Windows10Legacy
Windows11
IntegratedGpu
DedicatedGpu
MissingDriver
LowDiskSpace
PartialScan

Evitar fixtures gigantes frágeis.

---

# 58. TESTES DE MATRIZ

Testar combinações relevantes.

Exemplo:

Laptop + Battery
Desktop + Dedicated GPU
VM + Unknown GPU
Windows10 + LegacySupported
DeviceProblem
PartialScan

Confirmar que regras não vazam para contextos incompatíveis.

---

# 59. TESTES DE SEGURANÇA

Garantir que Analysis Engine não introduziu:

- Process.Start;
- PowerShell;
- cmd;
- Registry write;
- Service mutation;
- driver install;
- network mutation.

Analysis deve ser pure/read-only sempre que possível.

---

# 60. GOLDEN TESTS

Quando útil, criar snapshots de entrada conhecidos e validar conjunto esperado de RecommendationIds.

Evitar golden files enormes e frágeis.

---

# 61. PERFORMANCE

Analysis Engine deve ser leve.

Medir com snapshots representativos.

Não otimizar prematuramente.

Mas não criar arquitetura que faça consultas ao Windows novamente para cada regra.

Regra importante:

Analysis deve trabalhar principalmente sobre SystemSnapshot.

Não repetir Scanner.

---

# 62. SEM CONSULTAS ESCONDIDAS

IAnalysisRule não deve sair consultando:

Registry
WMI
Network
Process
etc.

por conta própria.

Se informação é necessária:

ela deve idealmente existir no snapshot/capabilities.

Isso mantém:

determinismo
testabilidade
segurança.

---

# 63. LOGGING

Registrar:

- AnalysisId;
- ScanId;
- engine version;
- rule catalog version;
- duração;
- quantidade de regras;
- resultados agregados;
- falhas.

Não despejar dados sensíveis.

---

# 64. FAILURE ISOLATION

Falha inesperada em uma regra não deve necessariamente destruir toda análise.

Definir comportamento seguro.

Regra defeituosa deve:

- ser registrada;
- gerar warning técnico;
- não produzir Recommendation falsa.

---

# 65. PRIVACIDADE

AnalysisResult não deve duplicar indiscriminadamente dados sensíveis do SystemSnapshot.

Recommendation deve carregar apenas contexto necessário.

Aplicar política de redaction existente quando necessário.

---

# 66. DOCUMENTAÇÃO

Criar:

ANALYSIS_ENGINE.md

Documentar:

- arquitetura;
- regras;
- lifecycle;
- compatibility;
- risk;
- evidence;
- presets;
- determinismo;
- versionamento;
- failure isolation.

Atualizar conforme necessário:

- ARCHITECTURE.md
- ARCHITECTURE_DECISION_RECORD.md
- DOMAIN_MODEL.md
- REQUIREMENTS.md
- UX_SPECIFICATION.md
- IMPLEMENTATION_ROADMAP.md
- SECURITY.md

---

# 67. NÃO IMPLEMENTAR OPTIMIZATION ENGINE OPERACIONAL

Mesmo que exista:

FutureOptimizationId

não implementar:

Apply
Undo
RegistryOperation
ServiceOperation
PowerOperation

nesta fase.

---

# 68. NÃO IMPLEMENTAR ROLLBACK

Não:

- criar restore point;
- salvar registry snapshots operacionais;
- alterar power plans;
- executar undo.

Isso pertence à Fase 4 consolidada.

---

# 69. NÃO IMPLEMENTAR DRIVERS

Recommendation:

"Dispositivo sem driver detectado"

é permitido.

Ação:

"Baixar e instalar driver"

não.

---

# 70. NÃO IMPLEMENTAR BENCHMARK

Não validar recomendação executando benchmark ainda.

Benchmark pertence à Fase 6.

---

# 71. BUILD

Ao concluir executar:

dotnet --info

dotnet restore .\BorealBoost.sln

dotnet build .\BorealBoost.sln --no-restore

dotnet test .\BorealBoost.sln --no-build

Esperado:

0 errors.

Investigar warnings novos.

---

# 72. EXECUÇÃO REAL

Executar BorealBoost.App quando possível.

Fluxo real:

Scanner
→ Snapshot
→ Analysis
→ Recommendations

Validar na máquina atual.

Não aplicar nada.

---

# 73. VALIDAÇÃO REAL

No relatório, informar sem dados sensíveis:

- quantidade de regras avaliadas;
- Healthy;
- Opportunities;
- Warnings;
- Blocked;
- Unknown;
- Recommendations geradas;
- distribuição por RiskLevel;
- duração da análise.

Não presumir que mais Recommendations significa análise melhor.

---

# 74. AUDITORIA DE SEGURANÇA

Antes de concluir fazer busca por:

Registry.SetValue
CreateSubKey
ServiceController.Start
ServiceController.Stop
Process.Start
powershell
cmd.exe
powercfg
netsh
DISM
SFC
PnPUtil
Windows Update
AppX

Classificar qualquer ocorrência nova.

Agent bootstrap existente não conta como funcionalidade da Fase 3, mas deve permanecer isolado.

---

# 75. GIT DIFF

Revisar:

git diff

Confirmar que nenhuma operação modificadora foi adicionada.

Não fazer commit automaticamente.

---

# 76. CRITÉRIOS DE ACEITAÇÃO

Fase 3 somente poderá ser considerada concluída quando:

- Analysis Engine existir;
- Rule Engine modular existir;
- Recommendation model existir;
- Compatibility evaluation existir;
- RiskLevel existir;
- EvidenceLevel existir;
- Preset eligibility existir;
- Unknown for tratado corretamente;
- regras forem determinísticas;
- regras trabalharem sobre snapshot;
- conjunto inicial de regras reais existir;
- regras tiverem testes;
- UI mostrar análise real;
- recomendações tiverem justificativa;
- Advanced/Aggressive tiverem aviso;
- nenhum tweak for executado;
- nenhuma otimização for aplicada;
- build passar;
- testes passarem;
- fluxo Scanner → Analysis funcionar.

---

# 77. ENTREGA

Ao finalizar apresentar:

## Summary

## Architecture

## Rules Implemented

Para cada regra:

- RuleId
- categoria
- condição
- resultado possível
- Risk
- Evidence

## Recommendation Model

## Compatibility Model

## Presets

## UI

## Real Machine Analysis

Informar:

- rules evaluated;
- opportunities;
- warnings;
- blocked;
- unknown;
- recommendations;
- risk distribution;
- duration.

## Tests

- novos testes;
- total;
- pass/fail.

## Build

- restore;
- build;
- test.

## Safety

Responder:

1. Analysis escreve Registry?
2. Altera Services?
3. Altera Power?
4. Altera Network/DNS?
5. Instala drivers?
6. Executa comandos?
7. Executa Optimization?
8. Executa Rollback?
9. Alguma funcionalidade destrutiva foi adicionada?
10. Fase 4 foi iniciada?

Esperado:

1. NÃO
2. NÃO
3. NÃO
4. NÃO
5. NÃO
6. NÃO
7. NÃO
8. NÃO
9. NÃO
10. NÃO

## Limitations

## Pending Validation

## Git Diff Review

---

# 78. REGRA FINAL

Esta fase deve tornar o BorealBoost inteligente o suficiente para explicar:

"Encontrei estas oportunidades porque a sua máquina apresenta estes fatos."

Mas ainda incapaz de dizer ao Windows:

"Execute estas alterações."

Não confundir Recommendation com Optimization.

Não buscar quantidade artificial de recomendações.

É melhor possuir 10 regras tecnicamente confiáveis do que 100 "FPS tweaks" sem evidência.

Não faça commit automaticamente.

Não inicie a Fase 4.