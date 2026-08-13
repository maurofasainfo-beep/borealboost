# CURRENT_TASK.md
# BorealBoost — Fase 4
# Optimization Engine + Safety + Snapshot + Rollback

> Fases 0, 1, 2 e 3: APROVADAS.
>
> Esta é a primeira fase que introduz infraestrutura capaz de modificar o Windows.
>
> Segurança, atomicidade, validação, recuperação e rollback têm prioridade sobre quantidade de funcionalidades.
>
> NÃO implementar o catálogo amplo de otimizações da Fase 5.

---

# 1. STATUS

Concluído:

✅ FASE 0 — Discovery e Arquitetura
✅ FASE 1 — Foundation
✅ FASE 2 — System Scanner
✅ FASE 3 — Analysis + Recommendation Engine

Fase atual:

🚧 FASE 4 — OPTIMIZATION ENGINE + SAFETY + SNAPSHOT + ROLLBACK

Próximas:

FASE 5 — Optimization Catalog: Safe + Medium + Advanced/Aggressive
FASE 6 — Drivers + Benchmark + Results + Reporting
FASE 7 — Installer + Hardening + Production Readiness

---

# 2. OBJETIVO

Construir a infraestrutura segura que permitirá ao BorealBoost executar otimizações reais futuramente.

Fluxo alvo:

Recommendation
↓
OptimizationDefinition
↓
Compatibility Check
↓
Detection
↓
Execution Plan
↓
Preflight
↓
Safety Validation
↓
Snapshot
↓
Restore Point Policy
↓
Technician Confirmation
↓
Privileged Agent
↓
Typed Operation
↓
Verification
↓
Journal / Commit
↓
Rollback disponível

Nesta fase, qualidade do motor é mais importante que quantidade de tweaks.

---

# 3. PRINCÍPIO FUNDAMENTAL

Nenhuma modificação no Windows pode acontecer fora do pipeline aprovado.

PROIBIDO:

UI
→ Registry diretamente

UI
→ ServiceController diretamente

UI
→ Power diretamente

UI
→ shell diretamente

Recommendation
→ executar diretamente

AnalysisRule
→ executar diretamente

Todo write futuro deve passar por:

ExecutionPlan
→ validação
→ safety
→ snapshot
→ Agent
→ operação tipada
→ verification
→ journal

---

# 4. LEITURA OBRIGATÓRIA

Antes de modificar qualquer código, leia integralmente:

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
- ANALYSIS_ENGINE.md
- PHASE1_REVALIDATION.md
- PHASE2_REVALIDATION.md
- PHASE3_AUDIT.md
- PHASE3_REVALIDATION.md

Depois inspecione integralmente o código existente relacionado a:

- Core
- Optimization
- Restore
- System
- Infrastructure
- Agent
- App
- testes

Não reimplementar mecanismos já existentes sem necessidade.

---

# 5. FASE 4 NÃO É O CATÁLOGO DE TWEAKS

Não transformar esta fase em uma coleção de otimizações.

NÃO adicionar dezenas de:

- Registry tweaks;
- service tweaks;
- debloat;
- AppX removal;
- network tweaks;
- gaming tweaks;
- telemetry tweaks;
- security tweaks;
- GPU tweaks.

Fase 4 constrói o MOTOR.

Fase 5 alimentará esse motor com catálogo real.

---

# 6. OPERAÇÕES REAIS NESTA FASE

Para provar o pipeline end-to-end, implementar somente um conjunto mínimo de operações controladas.

Prioridade:

1. operações simuladas/in-memory;
2. operações read-only reais;
3. no máximo operações reais reversíveis extremamente controladas necessárias para validar infraestrutura.

Não utilizar configuração crítica do Windows como laboratório.

Se for necessário validar write real:

- usar alvo de teste explicitamente pertencente ao BorealBoost;
- preferencialmente chave Registry própria de teste;
- snapshot obrigatório;
- verify obrigatório;
- rollback obrigatório;
- cleanup obrigatório.

Exemplo aceitável:

HKCU\Software\BorealBoost\IntegrationTest

Não usar nesta fase para teste:

- Defender;
- Firewall;
- VBS;
- Memory Integrity;
- serviços críticos;
- boot configuration;
- network stack;
- drivers;
- AppX;
- Windows Update;
- políticas críticas.

---

# 7. MODELO DE OPTIMIZATION DEFINITION

Criar/fortalecer OptimizationDefinition.

Deve possuir conforme aplicável:

- OptimizationId
- Version
- Title
- Description
- Category
- RiskLevel
- EvidenceLevel
- SupportedWindows
- CompatibilityRequirements
- RequiredCapabilities
- Conflicts
- Dependencies
- RequiresElevation
- RequiresRestart
- SupportsUndo
- SnapshotRequirements
- OperationSpecs
- VerificationSpecs
- RollbackSpecs
- FailurePolicy
- TimeoutPolicy
- SourceMetadata

IDs devem ser estáveis.

---

# 8. OPERATION SPEC

OperationSpec deve ser declarativo e tipado.

Cada operação deve declarar:

- OperationId
- OperationType
- target
- desired state
- detection strategy
- apply strategy
- verification strategy
- rollback strategy
- timeout
- retry policy
- idempotency
- reversibility
- reboot boundary
- failure policy
- required snapshot data

Não permitir:

Command = "qualquer string"

ou equivalente.

---

# 9. TIPOS DE OPERAÇÃO

Preparar arquitetura para tipos futuros como:

RegistryOperation
ServiceOperation
PowerOperation
WindowsFeatureOperation
DnsOperation
FileOperation
PackageOperation

Mas NÃO é obrigatório implementar todos operacionalmente nesta fase.

Prefira poucos tipos corretamente modelados.

---

# 10. ZERO EXECUÇÃO ARBITRÁRIA

Esta regra continua absoluta.

O Agent NÃO pode receber:

- command line arbitrária;
- PowerShell arbitrário;
- script;
- executable path arbitrário;
- shell command;
- cmd;
- argumentos genéricos para processo externo.

PROIBIDO criar:

ExecuteCommand(string)
ExecutePowerShell(string)
ExecuteProcess(string)
RunShell(string)
Run(string command)

ou abstrações semanticamente equivalentes.

---

# 11. AGENT ALLOWLIST

O Agent deve executar somente operações tipadas e conhecidas.

Fluxo:

OperationType
↓
Typed Handler
↓
Strict Input Validation
↓
Known Windows API / controlled adapter

Tipos desconhecidos:

REJECT.

Versões desconhecidas:

REJECT.

Campos extras perigosos:

REJECT quando apropriado.

---

# 12. TRUST BOUNDARY

Tratar App → Agent como fronteira de confiança.

Mesmo que App gere o ExecutionPlan, o Agent não deve confiar cegamente nele.

Agent deve revalidar pelo menos:

- protocolo;
- sessão;
- request;
- OperationType;
- OptimizationId;
- OperationId;
- parâmetros;
- limites;
- allowlist;
- versão;
- replay;
- estado esperado quando necessário.

Nunca assumir:

"veio da UI, então é seguro".

---

# 13. EXECUTION PLAN

Implementar ExecutionPlanner.

Entrada conceitual:

SystemSnapshot
AnalysisResult
RecommendationPlan
OptimizationDefinitions
TechnicianSelection

Saída:

ExecutionPlan

ExecutionPlan deve ser imutável após aprovação/início da execução ou possuir mecanismo equivalente seguro.

---

# 14. EXECUTION PLAN CONTENT

Incluir:

- PlanId
- SessionId
- ScanId
- AnalysisId
- CatalogVersion
- CreatedAtUtc
- target OS/build
- selected optimizations
- ordered operations
- dependencies
- conflicts
- risk summary
- elevation requirements
- snapshot requirements
- reboot boundaries
- estimated step count
- warnings
- blockers

Não estimar ganho de FPS.

---

# 15. PLAN VALIDATION

Antes de execução validar:

- IDs únicos;
- OptimizationIds existentes;
- OperationIds existentes;
- dependencies presentes;
- conflicts ausentes;
- compatibilidade;
- capabilities;
- OS/build;
- risk policy;
- snapshot support;
- rollback support;
- handler disponível;
- versão suportada.

Plano inválido:

NÃO EXECUTA.

---

# 16. TOCTOU / STALE PLAN

O estado da máquina pode mudar entre:

Scan
→ Analysis
→ Execution.

Antes de modificar o sistema, revalidar fatos críticos.

Exemplo:

serviço mudou;
valor Registry mudou;
Windows rebootou;
build mudou;
estado esperado não corresponde mais.

Não executar cegamente plano obsoleto.

Definir conceito:

Fresh
Stale
NeedsRevalidation
Blocked

ou equivalente.

---

# 17. DRY RUN

Implementar Dry Run real.

Dry Run deve:

- montar plano;
- validar;
- detectar estado atual;
- calcular snapshots necessários;
- informar operações que seriam executadas;
- informar reboot;
- informar riscos;
- informar blockers.

Dry Run NÃO pode modificar Windows.

---

# 18. PREFLIGHT

Antes de Apply:

validar conforme aplicável:

- OS/build;
- sessão válida;
- privilégio;
- plano válido;
- estado atual;
- pending reboot;
- disk space necessário;
- dependencies;
- conflicts;
- snapshot availability;
- rollback capability;
- Agent connection;
- handler support.

Falha crítica de preflight:

não executar.

---

# 19. OPTIMIZATION SESSION

Criar modelo persistível de OptimizationSession.

Deve registrar:

- SessionId
- PlanId
- ScanId
- AnalysisId
- started/completed timestamps
- state
- selected optimizations
- operation journal
- snapshots
- verification results
- rollback state
- reboot required
- failure information
- app version
- engine version
- schema version

Não incluir dados pessoais desnecessários.

---

# 20. SESSION STATE MACHINE

Definir state machine explícita.

Exemplo conceitual:

Created
Planned
PreflightPassed
Snapshotting
Ready
Executing
Verifying
Completed
CompletedWithWarnings
Failed
RollbackPending
RollingBack
RolledBack
RollbackFailed
Cancelled
Interrupted
RecoveryRequired

Transições inválidas devem ser rejeitadas.

---

# 21. JOURNAL / WRITE-AHEAD SAFETY

Antes de uma mutação relevante, persistir informação suficiente para recuperar estado.

Não depender de:

"aplico primeiro e salvo depois".

Fluxo conceitual:

capture before state
↓
persist snapshot/journal
↓
flush durable state
↓
apply
↓
verify
↓
persist result

O objetivo é sobreviver a:

- crash do App;
- crash do Agent;
- queda de energia;
- encerramento inesperado.

---

# 22. PERSISTÊNCIA ATÔMICA

Arquivos de sessão/snapshot não devem ser gravados de forma vulnerável a corrupção simples.

Usar estratégia como:

write temp
↓
flush
↓
atomic replace/rename

quando tecnicamente apropriado.

Não deixar JSON parcialmente gravado ser tratado como sessão válida.

---

# 23. SCHEMA VERSION

Persistências devem possuir:

SchemaVersion.

Leitura deve:

- validar schema;
- rejeitar versão incompatível;
- não interpretar silenciosamente estrutura desconhecida.

---

# 24. INTEGRITY

Avaliar mecanismo de integridade para artefatos críticos locais.

No mínimo detectar corrupção acidental.

Se implementar hash:

- definir exatamente o que é protegido;
- evitar falsa sensação de autenticidade;
- distinguir integrity de authenticity.

Não inventar assinatura criptográfica sem infraestrutura adequada.

---

# 25. SNAPSHOT

Snapshot deve capturar o estado ANTES da operação.

Cada SnapshotItem deve registrar conforme aplicável:

- SnapshotItemId
- OperationId
- ResourceType
- ResourceIdentity
- existed before
- previous value/state
- value type
- capture method
- captured at UTC
- restoration strategy
- limitations
- verification metadata

---

# 26. SNAPSHOT NÃO É SYSTEM SNAPSHOT

Separar claramente:

SystemSnapshot
= fatos de análise da máquina

OperationSnapshot
= estado reversível de um recurso antes da alteração

Não reutilizar os conceitos de forma ambígua.

---

# 27. SNAPSHOT OBRIGATÓRIO

Se operação declara que precisa snapshot e captura falhar:

NÃO EXECUTAR.

Não permitir:

snapshot failed
→ continue anyway

para operação reversível que depende dele.

---

# 28. RESTORE POINT

Implementar RestorePointService de forma controlada se tecnicamente viável e coerente com a arquitetura aprovada.

Restore Point é camada adicional.

NÃO substitui OperationSnapshot.

Fluxo:

OperationSnapshot
+
Session Journal
+
Restore Point quando aplicável.

---

# 29. RESTORE POINT POLICY

Modelar resultados como:

Created
RecentRestorePointAvailable
Unavailable
Disabled
Failed
NotRequired
Unknown

ou equivalente.

Não tratar criação de restore point como garantida.

Se falhar:

seguir FailurePolicy aprovada.

Não continuar silenciosamente.

---

# 30. RESTORE POINT E PROGRESSO

Não inventar percentual de criação de restore point se a API não fornecer progresso real.

Mostrar etapas reais:

Preparando...
Solicitando ponto de restauração...
Validando...
Concluído/Falhou.

---

# 31. ROLLBACK

Implementar RollbackEngine.

Rollback deve usar:

snapshot real

e não:

"default que provavelmente era o original".

---

# 32. UNDO INDIVIDUAL

Operação reversível deve poder declarar:

SupportsUndo
UndoStrategy
RequiredSnapshotItems
VerificationAfterUndo

Se estado original não puder ser restaurado exatamente:

declarar limitação.

---

# 33. SESSION ROLLBACK

Rollback de sessão deve ocorrer preferencialmente em ordem inversa das operações aplicadas.

Exemplo:

Apply:
A → B → C

Rollback:
C → B → A

Respeitar dependências quando necessário.

---

# 34. EXTERNAL STATE CHANGE

Antes de rollback:

comparar estado atual com:

- estado aplicado;
- estado original.

Classificar:

AppliedStateStillPresent
AlreadyOriginal
ChangedExternally
Unknown

Se recurso mudou externamente depois do BorealBoost:

não sobrescrever cegamente.

Exigir política/decisão segura.

---

# 35. VERIFICATION

Apply retornar sucesso NÃO significa otimização concluída.

Depois de cada operação:

Verify.

Estados conceituais:

Verified
FailedVerification
ManualVerificationRequired
NotApplicable

FailedVerification não pode virar Success.

---

# 36. IDEMPOTÊNCIA

Operações devem declarar comportamento idempotente.

Exemplo:

desired state já existe
→ AlreadySatisfied

e não:

aplicar novamente sem necessidade.

Isso é importante para:

- retry;
- recovery;
- execução repetida;
- rollback.

---

# 37. RETRY

Retry somente quando tecnicamente seguro.

Não usar retry genérico para toda mutação.

Cada OperationSpec deve declarar:

RetryAllowed
MaxAttempts
RetryableFailures

ou equivalente.

Não repetir operação não idempotente cegamente.

---

# 38. TIMEOUT

Cada operação deve possuir timeout coerente.

Timeout não significa automaticamente:

"operação não aconteceu".

Se resultado ficar incerto:

marcar:

OutcomeUnknown

ou equivalente

e bloquear continuidade quando necessário.

---

# 39. CANCELLATION

Cancelamento durante execução exige política.

Não interromper mutação no meio de forma insegura.

Definir safe cancellation points:

- antes de operação;
- depois de operação + verification;
- entre operações.

Se operação já começou:

permitir que alcance estado consistente quando necessário.

---

# 40. FAILURE POLICY

Modelar políticas como:

StopPlan
ContinueIndependent
RollbackCurrent
RollbackSession
ManualIntervention

ou equivalente.

Não continuar depois de falha crítica sem justificativa.

---

# 41. REBOOT BOUNDARY

Preparar arquitetura para operações que exigem reinicialização.

Estados:

NoReboot
RebootRecommended
RebootRequired

Não reiniciar automaticamente nesta fase.

Persistir estado suficiente para futura continuação pós-reboot.

---

# 42. CRASH RECOVERY

Ao iniciar BorealBoost:

detectar sessões não finalizadas.

Não mostrar sessão Interrupted como Completed.

Classificar e oferecer futuramente:

Inspect
Resume quando seguro
Rollback
ManualRecovery

Nesta fase implementar foundation segura de detecção/recovery.

---

# 43. AGENT LIFETIME

Agent elevado deve permanecer limitado à sessão necessária.

Não criar serviço Windows permanente nesta fase.

Não deixar Agent elevado indefinidamente.

Manter:

- handshake;
- session authentication;
- replay protection;
- timeout;
- shutdown controlado.

---

# 44. AGENT OPERATION PROTOCOL

Expandir protocolo atual somente com mensagens tipadas.

Exemplo conceitual:

ValidatePlanRequest
CaptureSnapshotRequest
ExecuteOperationRequest
VerifyOperationRequest
RollbackOperationRequest
SessionStatusRequest

Não necessariamente usar exatamente esses nomes.

Não enviar objetos arbitrários sem validação.

---

# 45. DEFENSE IN DEPTH

Mesmo que ExecutionPlan tenha sido validado no App:

Agent deve validar novamente operação privilegiada.

O handler deve validar novamente parâmetros críticos.

Camadas:

App validation
↓
Plan validation
↓
IPC validation
↓
Agent validation
↓
Operation handler validation

---

# 46. REGISTRY OPERATION — PROVA CONTROLADA

Se implementar RegistryOperation nesta fase:

usar APIs .NET/Windows apropriadas.

Suportar de forma tipada:

Read
Set
DeleteValue

mas write/delete real somente em alvo explicitamente seguro de integração nesta fase.

Snapshot deve preservar:

- key;
- value name;
- existed;
- previous type;
- previous value.

Rollback deve restaurar exatamente:

existência + tipo + valor.

---

# 47. REGISTRY VIEW

Tratar corretamente quando aplicável:

Registry32
Registry64

Não depender implicitamente da arquitetura do processo.

---

# 48. SERVICE OPERATION

Pode modelar ServiceOperation nesta fase.

Não alterar serviços reais críticos como prova.

Se não houver ambiente de teste seguro:

implementar adapter/contrato + testes controlados.

Não usar produção do Windows como fixture.

---

# 49. POWER OPERATION

Pode modelar PowerOperation.

NÃO criar ainda plano agressivo de performance do BorealBoost.

Isso pertence ao catálogo da Fase 5.

---

# 50. COMMAND-BASED OPERATIONS

Algumas funções futuras podem exigir ferramentas oficiais do Windows.

Não criar executor genérico.

Se futuramente necessário:

cada ferramenta deve possuir adapter específico e argumentos tipados/allowlisted.

Exemplo conceitual futuro:

PowerCfgAdapter

não:

CommandRunner("powercfg " + userInput)

---

# 51. CATALOG FOUNDATION

Preparar foundation do Optimization Catalog.

Catálogo deve ser:

- versionado;
- schema validated;
- deterministicamente carregado;
- IDs únicos;
- sem operações desconhecidas;
- compatibilidade validada.

Não preencher catálogo amplo ainda.

---

# 52. BUILT-IN VS UPDATED CATALOG

Preservar arquitetura já aprovada para distinguir:

BuiltInCatalog
UpdatedCatalog

Updated catalog futuramente exigirá:

- assinatura;
- publisher confiável;
- version;
- anti-downgrade;
- schema.

Nesta fase não implementar atualização remota se ainda não houver infraestrutura de assinatura.

---

# 53. NÃO BAIXAR SCRIPTS

Nunca implementar:

download script
→ execute

Nem:

irm URL | iex

Nem equivalente.

WinUtil é referência funcional, não runtime dependency.

---

# 54. UI — OTIMIZAÇÃO

Evoluir página Otimização para suportar foundation real.

Fluxo:

Recommendations
↓
Preset/Custom selection
↓
Review Plan
↓
Dry Run
↓
Safety Summary
↓
Confirmation
↓
Execution

Nesta fase, execução real deve permanecer restrita ao conjunto de prova controlado aprovado.

---

# 55. REVIEW PLAN

Antes de executar, UI deve mostrar:

- otimizações selecionadas;
- quantidade de operações;
- RiskLevel;
- warnings;
- incompatibilidades;
- reboot;
- rollback availability;
- restore point policy.

Itens blocked:

não selecionáveis/executáveis.

---

# 56. CONFIRMAÇÃO

Nenhuma operação modificadora deve iniciar simplesmente ao selecionar preset.

Fluxo mínimo:

selecionar
→ revisar
→ confirmar
→ executar

Advanced/Aggressive futuramente exigirão confirmação mais forte.

---

# 57. PROGRESSO

Mostrar progresso derivado das operações reais.

Exemplo:

Preparando
Capturando estado
Criando proteção
Aplicando operação 1/3
Verificando
Finalizando

Não usar timer fictício.

---

# 58. RESULTADOS

Cada operação deve produzir resultado estruturado.

Incluir conforme aplicável:

- OperationId
- status
- started/completed UTC
- duration
- changed state?
- verification
- requires restart
- error category
- safe technical details

Não exibir stack trace bruto ao cliente.

---

# 59. UI — RESTAURAÇÃO

Evoluir página Restaurar para foundation.

Mostrar sessões que realmente possuem estado reversível.

Estados:

Available
Partial
RolledBack
RollbackFailed
RecoveryRequired

Não criar botão que promete rollback quando snapshot não permite.

---

# 60. OBSERVABILIDADE

Logs estruturados devem incluir:

- SessionId
- PlanId
- OperationId
- OptimizationId
- action
- duration
- outcome
- verification
- rollback outcome

Não registrar secrets.

Cuidado com valores Registry potencialmente sensíveis.

Aplicar redaction.

---

# 61. AUDIT TRAIL

Distinguir:

Application Log

de:

Optimization Journal

Journal deve responder:

- o que pretendíamos fazer?
- qual era o estado anterior?
- o que foi executado?
- qual foi o resultado?
- foi verificado?
- houve rollback?

---

# 62. TESTES UNITÁRIOS

Cobrir pelo menos:

- OptimizationDefinition validation
- OperationSpec validation
- ExecutionPlan validation
- unique IDs
- dependencies
- conflicts
- compatibility
- stale plan
- state machine
- invalid state transitions
- snapshot models
- rollback ordering
- idempotency
- failure policy
- retry policy
- reboot boundary
- schema version
- corrupted session handling
- Unknown operation type rejection

---

# 63. TESTES DE SEGURANÇA DO AGENT

Adicionar testes negativos para:

- OperationType desconhecido;
- payload inválido;
- OptimizationId inválido;
- OperationId inválido;
- path/target fora de allowlist;
- oversized payload;
- replay;
- session mismatch;
- protocol mismatch;
- tentativa de command injection;
- tentativa de executable injection;
- parâmetros extras perigosos.

Agent deve rejeitar.

---

# 64. TESTES DE SNAPSHOT

Para operação reversível de prova:

Before
↓
Capture
↓
Persist
↓
Apply
↓
Verify
↓
Undo
↓
Verify original

Testar também:

- recurso inexistente antes;
- valor existente;
- tipo diferente;
- snapshot corrompido;
- snapshot ausente;
- falha de persistência.

---

# 65. TESTES DE ROLLBACK

Obrigatório testar:

Apply
Verify
Undo
Verify Original State

Também:

A apply
B apply
C fail
↓
policy rollback
↓
B undo
A undo

quando política assim determinar.

---

# 66. TESTE DE CRASH/INTERRUPÇÃO

Simular de forma segura:

- sessão persistida em Executing;
- operação journaled;
- processo termina antes de commit.

Ao reabrir:

sessão não pode aparecer Completed.

Deve ser:

Interrupted
RecoveryRequired

ou estado equivalente.

---

# 67. TESTE DE CORRUPÇÃO

Simular:

- JSON truncado;
- schema incompatível;
- hash/integrity inválida quando aplicável;
- snapshot incompleto.

Nunca executar rollback automaticamente com artefato não confiável.

---

# 68. TESTE DE CONCORRÊNCIA

Não permitir:

duas OptimizationSessions modificando o mesmo computador simultaneamente.

Implementar exclusão apropriada.

Testar:

- dois Starts;
- duas UIs;
- stale lock;
- crash;
- recovery.

Não depender apenas de bool em ViewModel.

---

# 69. INTEGRATION TEST REAL

Se usar Registry de teste:

executar somente em:

HKCU\Software\BorealBoost\IntegrationTest

ou namespace igualmente seguro e explicitamente próprio.

Fluxo obrigatório:

garantir estado inicial
→ snapshot
→ set valor de teste
→ verify
→ rollback
→ verify original
→ cleanup

Teste deve deixar máquina como encontrou.

---

# 70. NÃO USAR HKLM REAL PARA PROVA

Não usar HKLM crítico ou políticas reais apenas para provar elevação.

A elevação/Agent pode ser validada com operação controlada sem mexer em configuração sensível.

---

# 71. RESTORE POINT TEST

Restore Point real pode ter impacto no sistema.

Não criar repetidamente durante testes unitários.

Separar:

unit test
integration test
manual/system validation

Se executar restore point real:

fazer conscientemente;
registrar resultado;
não criar loops de restore points.

---

# 72. WINDOWS 10 / WINDOWS 11

OperationDefinition deve suportar restrições por:

- OS;
- build;
- architecture;
- capability.

Não assumir que operação válida no Windows 11 é válida no Windows 10.

---

# 73. PRIVACIDADE

Snapshots operacionais podem conter valores sensíveis.

Definir classificação e redaction.

Não logar indiscriminadamente:

OldValue
NewValue

Alguns recursos devem registrar apenas:

changed=true

ou representação sanitizada.

---

# 74. PERFORMANCE

Não otimizar microperformance do engine.

Priorizar:

correctness
durability
rollback
security.

Medir apenas:

- plan creation;
- persistence;
- controlled operation execution;
- rollback.

---

# 75. ERROR MODEL

Criar categorias estruturadas como:

AccessDenied
NotFound
Unsupported
ValidationFailed
SnapshotFailed
ApplyFailed
VerificationFailed
RollbackFailed
Timeout
OutcomeUnknown
ProtocolRejected
RecoveryRequired

ou modelo equivalente.

Não depender apenas de string de exception.

---

# 76. USER-FACING ERRORS

UI:

"Não foi possível concluir esta operação."

Detalhes técnicos podem ser expansíveis.

Log:

erro técnico completo sanitizado.

Não exibir stack trace cru.

---

# 77. FASE 5 NÃO DEVE SER INICIADA

NÃO implementar agora:

- catálogo amplo Safe;
- catálogo Medium;
- catálogo Advanced;
- catálogo Aggressive;
- Ultimate Performance real;
- desativação de serviços;
- debloat;
- DNS optimization;
- gaming tweaks;
- telemetry catalog;
- AppX removal catalog;
- GPU vendor tweaks;
- network latency catalog.

Tudo isso pertence à Fase 5.

---

# 78. WINUTIL

Pode consultar documentação/código do WinUtil apenas como referência funcional quando necessário.

Não copiar cegamente.

Não introduzir:

irm | iex

Não transformar BorealBoost em wrapper do WinUtil.

---

# 79. DOCUMENTAÇÃO

Atualizar conforme necessário:

- ARCHITECTURE.md
- ARCHITECTURE_DECISION_RECORD.md
- DOMAIN_MODEL.md
- OPTIMIZATION_ENGINE.md
- ROLLBACK_ENGINE.md
- SECURITY.md
- IMPLEMENTATION_ROADMAP.md
- UX_SPECIFICATION.md

Criar:

- OPTIMIZATION_EXECUTION.md
- ROLLBACK.md

se ainda não existirem e forem úteis para representar a implementação real.

---

# 80. BUILD

Ao concluir executar:

dotnet --info

dotnet restore .\BorealBoost.sln

dotnet build .\BorealBoost.sln --no-restore

dotnet test .\BorealBoost.sln --no-build

Esperado:

0 errors.

Investigar warnings novos.

---

# 81. DEPENDÊNCIAS

Executar:

dotnet list .\BorealBoost.sln package --vulnerable

dotnet list .\BorealBoost.sln package --outdated

Não atualizar pacote sem necessidade.

---

# 82. BUSCA DE SEGURANÇA

Fazer busca global por:

ExecuteCommand
ExecutePowerShell
ExecuteProcess
Process.Start
cmd.exe
powershell.exe
pwsh.exe
Registry.SetValue
CreateSubKey
DeleteSubKey
ServiceController.Start
ServiceController.Stop
powercfg
netsh
DISM
SFC
PnPUtil
winget
AppX

Classificar TODA ocorrência nova.

Qualquer execução genérica deve ser tratada como finding crítico.

---

# 83. RUNTIME VALIDATION

Executar BorealBoost.App quando ambiente permitir.

Validar fluxo:

Scanner
→ Analysis
→ Recommendations
→ Optimization Review
→ Dry Run

Depois validar somente a operação de integração controlada aprovada, se implementada:

Snapshot
→ Apply Test Resource
→ Verify
→ Rollback
→ Verify Original

Não aplicar otimizações reais de performance.

---

# 84. CRITÉRIOS DE ACEITAÇÃO

Fase 4 somente poderá ser considerada concluída quando:

- OptimizationDefinition existir;
- OperationSpec tipado existir;
- ExecutionPlanner existir;
- PlanValidator existir;
- Dry Run existir;
- Preflight existir;
- OptimizationSession existir;
- state machine existir;
- persistência segura existir;
- journal existir;
- snapshot operacional existir;
- rollback existir;
- verification existir;
- idempotency for modelada;
- timeout for modelado;
- cancellation segura existir;
- failure policy existir;
- reboot boundary existir;
- recovery de sessão interrompida existir;
- concorrência de sessão for bloqueada;
- Agent aceitar somente operações tipadas;
- Agent revalidar operações;
- execução arbitrária continuar impossível;
- operação controlada de prova passar Apply/Verify/Undo;
- UI Review existir;
- UI Restore foundation existir;
- build passar;
- testes passarem;
- nenhuma otimização real do catálogo da Fase 5 tiver sido antecipada.

---

# 85. ENTREGA FINAL

Ao concluir, apresentar:

## Summary

## Architecture

## OptimizationDefinition

## OperationSpec

## ExecutionPlan

## Agent Security Model

## Preflight

## Dry Run

## Optimization Session State Machine

## Persistence / Journal

## Snapshot

## Restore Point

## Verification

## Rollback

## Crash Recovery

## Concurrency

## UI

## Controlled Real Operation

Informar exatamente:

- recurso usado;
- estado antes;
- alteração controlada;
- verification;
- rollback;
- estado final.

Não incluir dados sensíveis.

## Tests

Informar:

- testes adicionados;
- total;
- pass/fail.

## Build

- restore;
- build;
- test;
- warnings/errors.

## Dependencies

- vulnerable;
- outdated.

## Safety

Responder explicitamente:

1. UI consegue executar Registry/Service/Power diretamente?
2. Agent aceita command line arbitrária?
3. Agent aceita executable path arbitrário?
4. Operação desconhecida é rejeitada?
5. Snapshot é persistido antes da mutação?
6. Apply exige verification?
7. Rollback usa estado original capturado?
8. Sessão interrompida pode aparecer como Completed?
9. Duas sessões de otimização podem executar simultaneamente?
10. Alguma otimização real de performance foi adicionada?
11. Alguma configuração crítica de segurança foi alterada?
12. Fase 5 foi iniciada?

Esperado:

1. NÃO
2. NÃO
3. NÃO
4. SIM
5. SIM
6. SIM
7. SIM
8. NÃO
9. NÃO
10. NÃO
11. NÃO
12. NÃO

## Remaining Risks

## Pending VM Validation

## Git Diff Review

---

# 86. REGRA FINAL

Esta fase não existe para fazer o computador ficar mais rápido.

Ela existe para garantir que, quando o BorealBoost começar a fazer o computador ficar mais rápido na Fase 5, ele consiga fazê-lo de maneira:

- controlada;
- tipada;
- verificável;
- auditável;
- reversível;
- recuperável;
- segura.

Uma operação que não pode ser validada, protegida e recuperada não deve ser executada.

Não faça commit automaticamente.

Não inicie a Fase 5.