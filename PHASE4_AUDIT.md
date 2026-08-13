# Executive Summary

Auditoria tecnica da Fase 4 - Optimization Engine + Safety + Snapshot + Rollback.

Resultado: a implementacao compila, os 159 testes passam, o fluxo controlado real em `HKCU\Software\BorealBoost\IntegrationTest\Phase4ControlledValue` executou com snapshot, verify e rollback, e nao foi encontrada execucao arbitraria no Agent.

Entretanto, a fase nao deve ser aprovada no estado atual. O handler controlado declara reversibilidade completa, mas normaliza `REG_EXPAND_SZ` como `String` e pode declarar rollback bem-sucedido sem restaurar exatamente o tipo original. Isso viola a garantia central da Fase 4: rollback deve usar estado original capturado e sucesso de rollback deve significar estado original verificado.

Tambem foram encontrados riscos altos antes da Fase 5: ausencia de lock cross-process para sessoes de otimizacao, Agent sem binding real entre operacao e catalogo confiavel, `PlanHash` nao validado, recovery cego a artefatos corrompidos e Agent iniciado sem elevacao/UAC apesar do contrato arquitetural de Agent elevado.

# Verdict

REJECTED

# Architecture

Grafo real de dependencias de projetos:

- `BorealBoost.Core -> (none)`
- `BorealBoost.Analysis -> BorealBoost.Core`
- `BorealBoost.Optimization -> BorealBoost.Core`
- `BorealBoost.Restore -> BorealBoost.Core`
- `BorealBoost.Infrastructure -> BorealBoost.Core`
- `BorealBoost.System -> BorealBoost.Core`
- `BorealBoost.Agent -> BorealBoost.Core, BorealBoost.Infrastructure, BorealBoost.Optimization, BorealBoost.System`
- `BorealBoost.App -> BorealBoost.Analysis, BorealBoost.Core, BorealBoost.Infrastructure, BorealBoost.Optimization, BorealBoost.Restore, BorealBoost.System`
- `BorealBoost.Drivers -> BorealBoost.Core`
- `BorealBoost.Benchmark -> BorealBoost.Core`
- `BorealBoost.Reporting -> BorealBoost.Core`

Core permanece independente. Optimization/Restore dependem apenas de Core. System encapsula o adapter Windows controlado. App referencia System por causa do Scanner, mas a operacao modificadora registrada na UI e `AgentOperationHandler`, que chama IPC do Agent.

Nao encontrei dependencia circular.

Separacao `SystemSnapshot` vs `OperationSnapshot`: existe no dominio. `SystemSnapshot` vem do Scanner/Analysis; `OperationSnapshot` e capturado antes da mutacao e usado para rollback.

# Phase Boundary

Nao foi encontrado catalogo real de performance, gaming, debloat, telemetry, services, power, DNS, GPU, AppX ou Windows Update.

A unica operacao real encontrada e `BorealIntegrationRegistryValue`, restrita arquiteturalmente a `HKCU\Software\BorealBoost\IntegrationTest\Phase4ControlledValue`.

Nao foi encontrada funcionalidade destrutiva real de otimizacao.

# Arbitrary Execution Validation

Busca global executada:

`ExecuteCommand|ExecutePowerShell|ExecuteProcess|RunCommand|RunShell|Process.Start|cmd.exe|powershell.exe|pwsh.exe|ShellExecute|CreateProcess|WinExec`

Ocorrencias classificadas:

- `src/BorealBoost.App/Agent/AgentBootstrapService.cs:330-346`: `Process.Start` usado para iniciar `BorealBoost.Agent` a partir de caminho interno resolvido. Nao recebe executable path da UI/payload.
- `tests/BorealBoost.Tests.System/AgentIpcSystemTests.cs:350-366`: `Process.Start` em teste de IPC do Agent.
- `cmd.exe` aparece apenas em testes negativos de injecao/payload.
- `UseShellExecute=false` aparece no bootstrap do Agent e testes.

Nao encontrei shell execution, PowerShell execution, executor generico, reflection/dynamic invocation perigosa ou OperationType generico capaz de virar shell.

# Agent Allowlist

OperationType aceito operacionalmente pelo Agent:

- `BorealIntegrationRegistryValue`
- Handler: `BorealIntegrationRegistryOperationHandler`
- Escopo: `RegistryHiveKind.CurrentUser`, key `Software\BorealBoost\IntegrationTest`, value `Phase4ControlledValue`
- Value kinds modelados: `String`, `DWord`
- Timeout maximo: 30s
- Retry maximo: 3
- Rollback: `SnapshotRestore`
- Snapshot obrigatorio para reversibilidade full

Tipo desconhecido e rejeitado. Handler inexistente e rejeitado. Target fora da allowlist e rejeitado.

Finding relevante: o Agent valida sintaxe de `OptimizationId` e `OperationId`, mas nao valida que eles pertencem ao catalogo built-in confiavel nem que o OperationSpec recebido e exatamente o OperationSpec catalogado.

# Trust Boundary

Camadas existentes:

- App monta plano e usa handler IPC.
- PlanValidator valida operacoes e catalogo no processo App.
- IPC valida protocolo, sessionId, nonce, timestamp, requestId, sequenceNumber e tamanho.
- Agent revalida OperationType, parametros e allowlist.
- Handler revalida parametros antes de capturar/aplicar/verificar/rollback.

Lacuna: a fronteira App-Agent ainda confia demais no payload para identidade catalogada da operacao e para snapshot de rollback. O Agent nao possui catalogo proprio para validar `OptimizationId -> OperationId -> OperationSpec` nem autentica a origem/proveniencia do snapshot.

# OptimizationDefinition

`BuiltInOptimizationCatalog` contem somente `BB.OPT.INTEGRATION.REGISTRY_PROOF`. A definicao declara risco Safe, evidencia Strong, Windows Supported/LegacySupported, build minimo 19045, undo suportado, restore point NotRequired e uma operacao tipada.

Nao ha tweak real de performance escondido.

# OperationSpec

O modelo contem OperationId, OperationType, target, desired state, timeout, retry, idempotency, reversibility, reboot boundary, failure policy, verification strategy, rollback strategy e snapshot requirements.

Nao existe campo `Command`, `Script`, `ExecutablePath` ou equivalente.

Risco encontrado: o modelo `RegistryValueDataKind` suporta apenas `String` e `DWord`, mas o handler captura `REG_EXPAND_SZ` como `String`, destruindo a distincao de tipo necessaria para rollback exato.

# ExecutionPlan

ExecutionPlan inclui PlanId, SessionId, ScanId, AnalysisId, SchemaVersion, EngineVersion, CatalogVersion, target OS/build/architecture, selected optimizations, ordered operations, dependencies, conflicts, risk summary, elevation/restart, restore point policy, snapshot requirements, reboot boundaries, estimated steps, warnings, blockers, PlanHash e IsApproved.

Plano invalido nao executa via Preflight/PlanValidator no caminho normal.

# Plan Validation

PlanValidator valida schema, catalog version, snapshot stale por ScanId/build/architecture, operacoes vazias, duplicate OperationId, definicoes ausentes, compatibilidade, dependencias, conflitos, handler e AgentOperationSecurityValidator.

Lacuna: `PlanHash` existe, mas nao e recalculado nem comparado. `IsApproved` e setado pelo proprio `OptimizationSessionService` imediatamente antes de executar, sem representar uma aprovacao imutavel/duravel.

# Stale Plan / TOCTOU

Existe validacao de stale plan para:

- ScanId diferente;
- arquitetura diferente;
- build diferente;
- CatalogVersion diferente.

Nao ha revalidacao generica de preconditions por recurso alem do handler controlado. Para a chave controlada, o snapshot captura estado antes da mutacao e o rollback compara estado atual contra desired/original.

Risco: planos manipulados apos criacao podem preservar ScanId/build e ainda passar porque PlanHash nao e validado.

# Dry Run

Dry Run cria plano, valida e chama `VerifyAsync` para detectar se o estado desejado ja esta presente. No App, esse handler e IPC via Agent. No handler System, `VerifyAsync` usa leitura de Registry com `OpenSubKey(writable: false)`.

Nao encontrei write durante Dry Run. A validacao real Scanner -> Analysis -> Optimization Dry Run passou.

# Preflight

Preflight valida plano e cada handler. Bloqueia quando ha issues ou validation status diferente de Valid.

Lacunas:

- nao valida elevacao real do Agent;
- nao valida pending reboot;
- nao valida assinatura/hash do Agent;
- nao valida hash do plano;
- nao valida lock cross-process.

# Session State Machine

Estados reais:

`Created`, `Planned`, `PreflightPassed`, `Snapshotting`, `Ready`, `Executing`, `Verifying`, `Completed`, `CompletedWithWarnings`, `Failed`, `RollbackPending`, `RollingBack`, `RolledBack`, `RollbackFailed`, `Cancelled`, `Interrupted`, `RecoveryRequired`, `RebootPending`, `ManualActionRequired`.

Problemas:

- `Executing -> Completed` e `Verifying -> Completed` sao transicoes permitidas no state machine, embora o contrato exija Completed apenas apos verification e commit duravel.
- `OptimizationSessionService.RollbackAsync` pode trocar qualquer estado para `RollingBack` usando `session with { State = RollingBack }`, contornando o state machine quando o estado nao e `RollbackPending`.

# Journal Durability

No caminho principal, a ordem observada e correta:

1. cria sessao e persiste;
2. preflight e persiste;
3. entra em Snapshotting e persiste;
4. captura snapshot;
5. persiste snapshot + journal `SnapshotCaptured`;
6. persiste `ApplyStarted`;
7. aplica;
8. persiste `ApplyCompleted`;
9. persiste `VerificationPending`;
10. verifica;
11. persiste `Verified`;
12. persiste Completed.

Essa ordem atende o requisito de snapshot/journal antes de mutacao no caminho normal.

Lacuna: se cancelamento/timeout ocorrer durante IPC depois de o Agent ter aplicado, o catch geral pode marcar a sessao como `Cancelled` com `CompletedAtUtc`, sem forcar `RecoveryRequired`/`OutcomeUnknown` baseado no ultimo journal duravel.

# Atomic Persistence

`FileOptimizationSessionStore` grava envelope em arquivo `.tmp`, usa `FileOptions.WriteThrough`, `FlushAsync`, e depois `File.Move(tempPath, finalPath, overwrite: true)`.

JSON truncado/parcial nao e aceito em `LoadAsync`. Hash divergente e rejeitado.

Lacunas:

- `.tmp` residual nao e limpo/classificado;
- `ListAsync` ignora silenciosamente arquivos de sessao que falham ao carregar, entao Recovery nao enxerga corrupcao.

# Schema / Integrity

SchemaVersion atual: `4.0.0`.

`LoadAsync` rejeita schema incompativel. O hash SHA-256 cobre a serializacao JSON da `OptimizationSession` dentro do envelope e detecta corrupcao acidental.

A documentacao diz corretamente que hash nao e autenticidade criptografica. O codigo nao implementa assinatura/HMAC para sessao local.

# Snapshot

OperationSnapshotItem captura SnapshotItemId, OperationId, ResourceType, ResourceIdentity, ExistedBefore, RegistryTarget, PreviousValueKind, PreviousStringValue, PreviousDWordValue, CaptureMethod, CapturedAtUtc, RestorationStrategy, Limitations e VerificationMetadata.

Snapshot obrigatorio falhando impede Apply no caminho normal.

Problema blocker: captura de `REG_EXPAND_SZ` e verificacao de valores unsupported podem produzir estado normalizado que nao representa exatamente o recurso original.

# Controlled Registry Handler

Allowlist do target e estrita para hive/key/value. `HKLM`, `SAM`, `SECURITY`, `SYSTEM` ou paths externos nao passam pelo validator.

O handler usa `CreateSubKey`, `SetValue` e `DeleteValue` apenas para o recurso controlado. Testes fazem backup/restauracao do mesmo valor.

Problema critico: `ConvertValue` mapeia `RegistryValueKind.String or RegistryValueKind.ExpandString` para `RegistryValueDataKind.String`. `RestoreSnapshot` grava `RegistryValueKind.String`, entao um valor originalmente `REG_EXPAND_SZ` vira `REG_SZ` e pode ser considerado restaurado.

Outro risco: `ReadCurrentState` retorna `Exists=false` quando encontra tipo nao suportado, o que pode mascarar mudanca externa para tipo unsupported.

# Apply / Verification

Apply valida operacao e snapshot, verifica se ja esta no estado desejado, grava o estado desejado e retorna `Applied` ou `AlreadySatisfied`.

Verify le o estado real via Registry e compara com desired state. Sucesso de Apply sozinho nao marca sessao Completed; o engine chama Verify antes de concluir.

# Idempotency

A operacao controlada e declarada idempotente. Se o estado desejado ja existe, o handler retorna `AlreadySatisfied` e `ChangedState=false`.

# Timeout / Cancellation / Retry

Timeout e modelado por operacao. `ExecuteWithRetryAsync` transforma timeout em `OutcomeUnknown` quando o cancellation veio do timeout interno e nao do token externo.

Retry so ocorre se `RetryAllowed`, ate `MaxAttempts`, e se a categoria estiver em `RetryableFailures`.

Risco: cancelamento externo durante execucao pode marcar sessao como Cancelled mesmo se o ultimo journal for `ApplyStarted`/`VerificationPending`. Isso nao e suficientemente conservador para mutacoes futuras.

# Failure Policy

A definicao controlada usa `AttemptRollback`. Falha de apply/verify dispara rollback dos itens com `ApplyCompleted` ou `Verified` em ordem inversa.

Dependencias futuras ainda nao possuem solver avancado; para Fase 4 isso e aceitavel, pois ha uma unica operacao.

# Rollback

Rollback de sessao usa snapshot e ordem inversa.

Rollback do handler compara:

- estado atual;
- estado desejado aplicado;
- estado original capturado.

Se o estado atual nao for o desejado, o handler evita sobrescrever e retorna falha segura.

Problema critico: por normalizar tipos de valor, o rollback pode verificar contra um modelo ja degradado, nao contra o tipo real original.

# External State Changes

O handler tenta detectar mudanca externa: se o estado atual nao e original nem desired, nao sobrescreve.

Lacuna: tipo Registry unsupported e lido como absent; isso pode esconder uma mudanca externa para `QWord`, `Binary`, `MultiString` ou outro tipo.

# Crash Recovery

RecoveryService detecta sessoes persistidas incompletas quando `CompletedAtUtc` e null ou estado nao final.

Nao reaplica nada automaticamente.

Lacunas:

- artefatos corrompidos nao viram RecoveryCandidate porque `ListAsync` ignora load failure;
- recovery nao interpreta journal para diferenciar `ApplyStarted` sem `ApplyCompleted`, `ApplyCompleted` sem `Verified`, ou `RollbackStarted` sem `RollbackVerified` com a granularidade descrita na arquitetura.

# Concurrency

Dentro de um processo, `OptimizationSessionService` usa `static SemaphoreSlim` e bloqueia duas sessoes simultaneas.

Nao ha mutex nomeado, file lock duravel ou lock cross-process. Duas instancias separadas do BorealBoost podem ter semaforos independentes e iniciar sessoes simultaneas.

Cross-process concurrency status: NAO VALIDADO por execucao destrutiva; por inspecao estatica, NAO ESTA IMPLEMENTADO.

# Agent Security

SessionId, nonce, RequestId, SequenceNumber, timestamp, protocol version e payload size continuam validados.

Named pipe usa nome nao previsivel com sessionId + pipe token. ACL permite usuario atual, Administrators e LocalSystem.

Lacunas:

- identidade do processo cliente nao e verificada via impersonation/PID;
- Agent nao valida assinatura/hash do binario App;
- Agent nao carrega catalogo proprio;
- Agent e iniciado com `UseShellExecute=false`, sem `runas`, entao nao ha elevacao/UAC validada no fluxo implementado.

# Payload Validation

`AgentPipeProtocol` usa `JsonUnmappedMemberHandling.Disallow` e `JsonStringEnumConverter`. Payload com campo extra `command` e rejeitado em teste.

Envelope valida tamanho maximo de 1 MiB, timestamp, nonce, session, requestId, sequence e mismatch MessageType/PayloadType.

Lacunas:

- `OperationSnapshotItem` vindo do cliente e validado apenas por OperationId/key/value;
- nao ha validacao profunda de `SnapshotItemId`, hive, view, resource type, previous value kind, restoration strategy, limitations ou provenance.

# Restore Point Foundation

`RestorePointRequirement.NotRequired` retorna `NotRequired` para a prova controlada. `BestEffort` e `Required` retornam `Unavailable`; `Required` bloqueia no session service.

Nao ha codigo que marque restore point como `Created` sem cria-lo.

Restore point real nao foi validado nesta fase.

# Reboot Boundary

RebootBoundary esta modelado. A operacao controlada usa `None`. Nao ha reboot automatico.

# UI Optimization

OptimizationPage possui botoes separados para Dry Run e confirmar prova controlada. Selecionar/preset nao aplica nada automaticamente.

O botao de execucao exige Dry Run valido.

Risco baixo: handlers de evento sao `async void`; erros principais sao tratados no ViewModel, mas a pagina nao encapsula excecoes de lifecycle.

# UI Restore

RestorePage lista sessoes e recovery candidates. Nao executa rollback destrutivo automaticamente.

Finding: a UI marca sessoes Completed/CompletedWithWarnings como "Rollback disponivel se snapshot existir" sem validar artifact integrity/snapshot/rollback support. Como arquivos corrompidos sao omitidos pelo store, a UI pode subnotificar recovery.

# Privacy

Snapshots persistem valores anteriores em claro porque esses valores sao necessarios para rollback. A policy `OptimizationPrivacyPolicy` classifica `PreviousStringValue` como `DoNotLog`.

Nao encontrei log indiscriminado do snapshot inteiro.

Risco residual: nao ha criptografia/ACL dedicada auditada para o journal; a protecao atual e integridade acidental, nao confidencialidade.

# Logging

Ha logging estruturado para:

- Agent bootstrap/probe/session;
- completion/failure geral de OptimizationSession;
- falhas de UI.

Lacuna: os eventos por operacao exigidos pela Fase 4 (`SnapshotCaptured`, `ApplyStarted`, `ApplyCompleted`, `VerificationPending`, `Verified`, `RollbackStarted`, `RollbackVerified`) existem no journal, mas nao sao emitidos como logs estruturados com todos os campos SessionId/PlanId/OperationId/OptimizationId/Action/Outcome/Duration.

# Tests

Total executado: 159 testes.

Resultado:

- Unit: 121 passed
- Integration: 16 passed
- System: 22 passed
- Total: 159 passed, 0 failed

Cobertura positiva relevante:

- OperationDefinition/OperationSpec transactional contract;
- operation type desconhecido;
- registry target fora da allowlist;
- command/executable target injection;
- missing snapshot for full reversibility;
- stale plan;
- dry run;
- invalid state transition completed -> executing;
- corrupted JSON load;
- integrity hash mismatch;
- recovery de sessao incompleta;
- concorrencia intra-process;
- rollback em ordem inversa em falha;
- Agent IPC handshake/status/operation/rollback;
- payload extra `command`;
- scanner -> analysis -> optimization dry run -> controlled rollback.

Gaps:

- `REG_EXPAND_SZ`, `QWord`, `MultiString`, `Binary` no handler controlado;
- external state change para tipo unsupported;
- snapshot adulterado;
- PlanHash adulterado;
- catalog/OperationId mismatch aceito pelo Agent;
- concorrencia cross-process;
- cancelamento externo depois de `ApplyStarted`;
- timeout com mutacao incerta via Agent;
- rollback parcial com B sucesso e A falha;
- recovery granular por ultima entrada de journal;
- restore UI com artifact corrupt.

# Controlled Runtime Validation

Teste real controlado executado novamente:

- Recurso: `HKCU\Software\BorealBoost\IntegrationTest`
- Valor: `Phase4ControlledValue`
- Estado inicial observado: Absent
- Teste: `Agent_executes_controlled_registry_operation_with_snapshot_verify_and_rollback`
- Resultado: passed
- Estado final observado: Absent

Fluxo real Scanner -> Analysis -> Optimization:

- Teste: `Real_scanner_analysis_flows_into_optimization_dry_run_and_controlled_rollback`
- Resultado: passed
- `Phase4PlanOperations=1`
- `Phase4DryRunBlockers=0`
- `Phase4SessionState=Completed`
- `Phase4RollbackState=RolledBack`
- `Phase4JournalEntries=9`

UI runtime:

- `BorealBoost.App.exe` iniciou;
- janela principal encontrada com titulo `BorealBoost`;
- processo permaneceu ativo por 8 segundos;
- encerrado manualmente pelo teste para nao deixar processo aberto.

# Build Validation

Comandos executados:

- `dotnet --info`: SDK 10.0.400 em Windows 10.0.26200 x64.
- `dotnet restore .\BorealBoost.sln`: sucesso.
- `dotnet build .\BorealBoost.sln --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\BorealBoost.sln --no-build`: sucesso, 159 passed, 0 failed.

# Dependency Validation

`dotnet list .\BorealBoost.sln package --vulnerable`:

- nenhuma vulnerabilidade reportada nas fontes atuais.

`dotnet list .\BorealBoost.sln package --outdated`:

- projetos de produto: sem updates reportados;
- projetos de teste: updates disponiveis para `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`.

Nenhuma dependencia foi atualizada nesta auditoria.

# Findings Table

| ID | Severity | File | Evidence | Impact | Recommended correction |
|---|---|---|---|---|---|
| PH4-BLOCKER-001 | BLOCKER | `src/BorealBoost.System/Operations/BorealIntegrationRegistryOperationHandler.cs` | Lines 359, 320, 267, 212 | Rollback pode declarar sucesso sem restaurar tipo original de Registry. | Preservar `RegistryValueKind` real ou rejeitar tipos nao suportados antes de apply; nunca tratar unsupported como absent; adicionar testes de tipos. |
| PH4-HIGH-001 | HIGH | `src/BorealBoost.Optimization/Execution/OptimizationSessionService.cs` | Lines 13, 65, 222 | Duas instancias do App podem executar sessoes simultaneas. | Implementar mutex/lock cross-process com stale recovery. |
| PH4-HIGH-002 | HIGH | `src/BorealBoost.Agent/AgentIpcSession.cs`, `src/BorealBoost.Core/Optimization/AgentOperationSecurityValidator.cs` | Agent lines 350-374; validator line 18 | Agent nao vincula OptimizationId/OperationId ao catalogo confiavel. | Agent deve carregar catalogo built-in/confiavel e validar binding exato de operacao. |
| PH4-HIGH-003 | HIGH | `src/BorealBoost.Core/Optimization/OptimizationModels.cs`, `src/BorealBoost.Optimization/Planning/ExecutionPlanValidator.cs`, `src/BorealBoost.Optimization/Execution/OptimizationSessionService.cs` | PlanHash/IsApproved model lines 145-146; approval line 72; validator sem PlanHash | Plano pode ser adulterado/forjado sem detectar hash invalido. | Recalcular hash canonico no validator e exigir approval imutavel/duravel. |
| PH4-HIGH-004 | HIGH | `src/BorealBoost.Infrastructure/Persistence/FileOptimizationSessionStore.cs` | Lines 139-142 | Artefatos corrompidos sao ignorados por recovery/UI. | Listar tambem load failures como artifacts invalidos/recovery candidates. |
| PH4-HIGH-005 | HIGH | `src/BorealBoost.App/Agent/AgentBootstrapService.cs` | Lines 330-346 | Agent nao e elevado/validado apesar do contrato arquitetural de Agent elevado. | Implementar bootstrap UAC quando operacao exigir elevacao e validar token elevado no Agent. |
| PH4-MEDIUM-001 | MEDIUM | `src/BorealBoost.Agent/AgentIpcSession.cs` | Lines 380-389 | Snapshot recebido do cliente e validado superficialmente. | Validar hive/view/resource type/value kind/restoration strategy/provenance/hash do snapshot. |
| PH4-MEDIUM-002 | MEDIUM | `src/BorealBoost.Optimization/Execution/OptimizationSessionService.cs` | Lines 186-197 | Cancelamento pode mascarar outcome incerto como Cancelled final. | Usar safe cancellation points e RecoveryRequired/OutcomeUnknown quando apply/verify ja iniciou. |
| PH4-MEDIUM-003 | MEDIUM | `src/BorealBoost.Optimization/Execution/OptimizationSessionStateMachine.cs`, `src/BorealBoost.Optimization/Execution/OptimizationSessionService.cs` | State machine lines 15-16; rollback line 243 | Transicoes permissivas e bypass do state machine. | Remover transicoes diretas perigosas e exigir transicoes explicitas tambem no rollback manual. |
| PH4-MEDIUM-004 | MEDIUM | `src/BorealBoost.App/ViewModels/RestoreViewModel.cs` | Line 50 | UI pode sugerir rollback disponivel sem validar snapshot/artifact. | Calcular disponibilidade real por session validator/recovery artifact status. |
| PH4-MEDIUM-005 | MEDIUM | `src/BorealBoost.Optimization/Execution/OptimizationSessionService.cs` | Logs apenas lines 178, 204 | Logs estruturados por etapa/op ficam ausentes; apenas journal registra. | Emitir logs estruturados sanitizados para snapshot/apply/verify/rollback. |
| PH4-MEDIUM-006 | MEDIUM | `tests/` | Test inventory | Gaps em casos negativos obrigatorios para Fase 5. | Adicionar testes de tipos Registry, snapshot tamper, cross-process lock, cancellation/timeout incerto e rollback parcial. |
| PH4-MEDIUM-007 | MEDIUM | `src/BorealBoost.Restore/RollbackEngine.cs` | Lines 56-72 | RollbackEngine retorna `Success` com estado `RollbackFailed` e nao persiste journal; esta registrado em DI. | Alinhar contrato de Result/estado, persistencia e uso real ou remover do caminho operacional. |
| PH4-LOW-001 | LOW | `src/BorealBoost.Agent/BorealBoost.Agent.csproj`, `src/BorealBoost.Restore/BorealBoost.Restore.csproj` | Agent line 6; Restore line 4 | Metadados descrevem estado antigo, contradizendo Fase 4. | Atualizar descriptions. |
| PH4-LOW-002 | LOW | `src/BorealBoost.Infrastructure/Persistence/FileOptimizationSessionStore.cs` | Temp file flow lines 46-62 | `.tmp` residual pode ficar apos falha. | Limpar/classificar temp files na inicializacao/recovery. |
| PH4-LOW-003 | LOW | `src/BorealBoost.App/Pages/OptimizationPage.xaml.cs`, `src/BorealBoost.App/Pages/RestorePage.xaml.cs` | async void event handlers | Excecoes de lifecycle podem ficar menos previsiveis. | Encapsular handlers com helper async seguro/logging. |

# Blockers

PH4-BLOCKER-001: rollback do handler controlado nao preserva exatamente tipo Registry em todos os casos que aceita/normaliza e pode reportar sucesso indevido.

# High Priority

- PH4-HIGH-001: implementar lock cross-process antes de qualquer catalogo real.
- PH4-HIGH-002: Agent deve validar catalogo/OptimizationId/OperationId, nao apenas OperationType/target.
- PH4-HIGH-003: PlanHash/approval precisam ser efetivos.
- PH4-HIGH-004: recovery deve enxergar artefatos corrompidos.
- PH4-HIGH-005: bootstrap elevado e validacao de token do Agent precisam ser implementados antes de operacoes privilegiadas.

# Medium Priority

- PH4-MEDIUM-001: fortalecer validacao de snapshot no Agent.
- PH4-MEDIUM-002: corrigir semantica de cancelamento durante execucao.
- PH4-MEDIUM-003: endurecer state machine e rollback transitions.
- PH4-MEDIUM-004: UI Restore deve refletir disponibilidade real.
- PH4-MEDIUM-005: logs por etapa/op.
- PH4-MEDIUM-006: ampliar testes negativos.
- PH4-MEDIUM-007: alinhar RollbackEngine standalone ao contrato operacional.

# Low Priority

- PH4-LOW-001: descriptions de csproj desatualizadas.
- PH4-LOW-002: temp files residuais.
- PH4-LOW-003: async void lifecycle handlers.

# Unvalidated Items

- Concorrencia cross-process por duas instancias separadas do EXE: nao validado por execucao; por codigo, nao ha lock cross-process.
- Power loss real/crash fisico durante apply/rollback: nao validado fisicamente.
- Agent elevado via UAC: nao validado; codigo atual nao solicita elevacao.
- Restore point real: nao validado e corretamente diferido.
- VM matrix: nao validada nesta auditoria.
- Assinatura/hash de binarios App/Agent: nao validada.

# Required Corrections Before Phase 5

1. Corrigir PH4-BLOCKER-001 antes de qualquer nova operacao real.
2. Implementar lock cross-process com recovery/stale lock.
3. Fazer o Agent validar catalogo confiavel, OptimizationId, OperationId e OperationSpec exato.
4. Validar PlanHash/approval antes de Execute.
5. Fazer recovery listar e expor artifacts corrompidos/incompativeis como pendencia segura.
6. Implementar ou bloquear explicitamente fluxo de Agent elevado com validacao de token antes de operacoes privilegiadas.
7. Adicionar testes negativos para tipos Registry, snapshot tamper, external state unsupported, cross-process concurrency, cancellation/timeout incerto e rollback parcial.

# Final Recommendation

Nao iniciar a Fase 5.

A Fase 4 tem uma base arquitetural promissora e validacao real controlada, mas deve ser REJECTED ate corrigir o blocker de rollback exato e os highs que afetam seguranca transacional antes de ampliar o catalogo de otimizacoes.
