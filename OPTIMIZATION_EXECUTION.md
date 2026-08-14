# BorealBoost - Optimization Execution

Data: 2026-08-13
Status: implementado e revalidado na Fase 4; expandido na Fase 5 para Catalog V1 allowlisted.

## Escopo

A Fase 4 implementa o motor transacional seguro. A Fase 5 adiciona o primeiro Catalog V1 real usando somente `OperationType.RegistryValue` allowlisted. O motor continua sem alterar Services, Power, DNS, Drivers, Windows Update, Defender, Firewall, BCD, timer behavior ou pagefile.

## Pipeline Implementado

1. `ExecutionPlanner` cria `ExecutionPlan` a partir de `SystemSnapshot`, `AnalysisResult`, `RecommendationPlan` e selecao tecnica.
2. `ExecutionPlanValidator` valida schema, catalogo, OS/build, dependencias, conflitos, handlers e allowlist.
3. `DryRunService` detecta se a operacao ja esta satisfeita e informa blockers sem mutacao.
4. `PreflightService` revalida o plano imediatamente antes do apply, exigindo plano aprovado e hash canonico valido.
5. `OptimizationSessionService` adquire lock cross-process e persiste a sessao em `Planned`.
6. Cada operacao captura `OperationSnapshot`.
7. Snapshot e journal sao persistidos antes de tocar no recurso.
8. Apply roda por handler tipado allowlisted.
9. Verify e obrigatorio.
10. Commit duravel so marca `Completed` apos verification.
11. Falha com policy `AttemptRollback` reverte operacoes aplicadas em ordem inversa.
12. Recovery detecta sessoes incompletas na proxima inicializacao.

## Operacao Real Controlada

Recurso:

`HKCU\Software\BorealBoost\IntegrationTest\Phase4ControlledValue`

OperationType:

`BorealIntegrationRegistryValue`

Garantias:

- hive deve ser `CurrentUser`;
- key/value devem bater exatamente com a allowlist;
- snapshot e obrigatorio;
- rollback restaura existencia, tipo e valor original;
- rollback distingue chave existente de chave criada pelo BorealBoost; se a chave nao existia antes e ficou vazia apos remover o valor, a chave criada e removida;
- tipos suportados no snapshot/rollback: `String`, `ExpandString`, `DWord`, `QWord`, `MultiString` e `Binary`;
- tipos nao suportados sao rejeitados antes do apply quando uma operacao tentaria grava-los ou antes do rollback quando aparecem em snapshot adulterado;
- mudanca externa apos apply bloqueia sobrescrita cega;
- nao exige elevation;
- nao exige reboot;
- nao representa otimizacao de performance.

## Catalog V1 Runtime

O Catalog V1 possui 12 OptimizationDefinitions reais, `schemaVersion = 5.1.0`, `catalogVersion = 5.1.0-built-in-v1` e exclui a prova de integracao dos presets. Todas as operacoes reais usam:

- `OperationType.RegistryValue`;
- desired state DWORD fixo no catalogo;
- detection por leitura exata do valor alvo no Dry Run;
- snapshot obrigatorio antes de write;
- apply por handler tipado;
- verify por leitura real apos apply;
- rollback por `SnapshotRestore`;
- `RebootBoundary.None`.

O App registra dois handlers por DI:

- `BorealIntegrationRegistryValue` para `BB.OPT.INTEGRATION.REGISTRY_PROOF`;
- `RegistryValue` para os targets fixos de `TrustedRegistryOperationTargets.CatalogV1`.

Operacoes HKCU podem rodar sem elevacao. Operacoes HKLM declaram `RequiresElevation=true`; o App deve iniciar o Agent com UAC e o Agent bloqueia apply se o token nao estiver elevado.

O Catalog V1 nao possui operacao irreversible, SecurityTradeoff ou Aggressive/Experimental.

## Agent

O Agent aceita somente mensagens tipadas:

- `ValidateOperationRequest`;
- `CaptureSnapshotRequest`;
- `ExecuteOperationRequest`;
- `VerifyOperationRequest`;
- `RollbackOperationRequest`.

Payloads sao desserializados com rejeicao de campos desconhecidos. O Agent revalida `CatalogVersion`, `OptimizationId`, `OperationId`, `OperationType`, schema, target, desired state, timeout, retry, snapshot e rollback contra o catalogo built-in confiavel antes de encaminhar ao handler.

`OperationSpec` recebido pelo Agent deve ser equivalente a definicao canonica do catalogo. Um payload com mesmo `OptimizationId`/`OperationId` e target ou desired state adulterado e rejeitado.

O status do Agent informa se o token do processo esta elevado. Operacoes que declararem `RequiresElevation=true` sao bloqueadas pelo Agent quando o token nao estiver elevado. O App inicia o Agent elevado somente para operacoes canonicas que declaram elevacao; operacoes HKCU do Catalog V1 mantem o contexto do usuario para nao escrever no HKCU de outro token. Operacoes HKLM do Catalog V1 estao marcadas como `UNVALIDATED_FOR_RELEASE` para apply real ate validacao em VM/ambiente seguro.

## Persistencia

`FileOptimizationSessionStore` grava sessoes como JSON versionado com envelope:

- `schemaVersion`;
- `integrityHash`;
- `session`.

O hash SHA-256 protege o payload de sessao contra corrupcao acidental. Isso nao e autenticidade criptografica nem substitui assinatura de catalogo.

`PlanHash` e calculado sobre a representacao canonica do plano aprovado e revalidado antes de preflight/apply. Alterar operacoes, ordem, target, desired state, catalog version, selected optimizations ou metadados transacionais invalida o plano.

Cada `OperationSnapshotItem` possui hash de integridade local cobrindo recurso, tipo, valor original, estrategia de restauracao e metadata de captura. Snapshot adulterado e rejeitado antes de apply/rollback.

Recovery enumera tambem artefatos invalidos. JSON truncado, schema incompativel, hash divergente e `.tmp` residual viram candidatos `ManualRecovery`, nao desaparecem silenciosamente.

## Timeout e Cancelamento

Cancelamento externo e aceito somente em pontos seguros: antes de mutacao, entre operacoes e apos verify duravel.

Depois de `ApplyStarted`, o motor nao deixa um token externo cancelar no meio de uma mutacao critica. Apply/verify/rollback terminam usando token interno controlado ou sem cancelamento externo para persistir estado final confiavel.

Timeout iniciado depois de `ApplyStarted` nao significa "falhou sem alterar". Quando o estado final nao puder ser provado, a operacao retorna `OutcomeUnknown`, a sessao entra em `RecoveryRequired` e nao pode ser marcada como `Completed`.

Concorrencia:

- `CrossProcessOptimizationSessionLock` usa lock file exclusivo por usuario em `%LocalAppData%\BorealBoost\Locks`;
- duas instancias do BorealBoost nao conseguem iniciar sessao mutavel simultanea;
- crash libera o lock pelo fechamento do handle pelo Windows.

## Limites

- Catalogo updated assinado ainda nao existe.
- Restore point real nao e criado.
- Fase 6 nao foi iniciada.
- Nenhum executor generico de processo, cmd ou PowerShell existe.
