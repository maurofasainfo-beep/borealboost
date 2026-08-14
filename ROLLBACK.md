# BorealBoost - Rollback Implementation

Data: 2026-08-13
Status: foundation implementada e revalidada na Fase 4; Catalog V1 coberto por snapshot rollback na Fase 5.

## Conceito

Rollback usa `OperationSnapshot`, nao valores default. `SystemSnapshot` continua sendo inventario read-only da maquina; `OperationSnapshot` e o estado reversivel do recurso antes da mutacao.

## Estados

Estados implementados em `OptimizationSessionState`:

- Created;
- Planned;
- PreflightPassed;
- Snapshotting;
- Ready;
- Executing;
- Verifying;
- Completed;
- CompletedWithWarnings;
- Failed;
- RollbackPending;
- RollingBack;
- RolledBack;
- RollbackFailed;
- Cancelled;
- Interrupted;
- RecoveryRequired;
- RebootPending;
- ManualActionRequired.

`Completed` exige verify e commit duravel. Sessao interrompida ou sem `CompletedAtUtc` nunca aparece como concluida.

## Rollback de Sessao

Para operacoes aplicadas:

1. carregar sessao persistida;
2. validar snapshot;
3. executar rollback em ordem inversa;
4. verificar estado original;
5. persistir resultado.

Se snapshot estiver ausente/corrompido, com hash divergente, pertencente a outra sessao/plano ou semanticamente incoerente, rollback automatico e bloqueado.

Cada item de snapshot da Fase 4 preserva:

- existencia original;
- `RegistryValueKind`;
- valor original bruto;
- `RegistryView`;
- target canonico;
- estrategia de restauracao;
- hash de integridade local.

Tipos Registry preservados no alvo controlado:

- `String`;
- `ExpandString`;
- `DWord`;
- `QWord`;
- `MultiString`;
- `Binary`.

`REG_EXPAND_SZ` e lido com `DoNotExpandEnvironmentNames`, portanto variaveis nao sao expandidas antes de persistir o estado original. Rollback restaura `REG_EXPAND_SZ` como `REG_EXPAND_SZ`.

## Mudanca Externa

O handler controlado compara estado atual com estado desejado e estado original. Se o recurso foi alterado externamente depois do BorealBoost, rollback nao sobrescreve cegamente e retorna falha segura.

## Catalog V1

As 12 OptimizationDefinitions reais do Catalog V1 declaram:

- `SupportsUndo=true`;
- `OperationReversibility.Full`;
- `OperationRollbackKind.SnapshotRestore`;
- snapshot obrigatorio e bloqueante;
- verification exata depois do apply;
- `RebootBoundary.None`.

O mesmo handler Registry preserva o estado anterior para as operacoes `RegistryValue` allowlisted. A validacao end-to-end real da Fase 5 foi executada em uma operacao HKCU segura do catalogo (`BB.OPT.VISUAL.TRANSPARENCY.DISABLE`) com:

Before -> Snapshot -> Apply -> Verify -> Rollback -> Final State.

Na revalidacao da Fase 5, o snapshot tambem registra se a chave Registry existia antes do apply. Se a chave nao existia, BorealBoost criou somente o valor aprovado e a chave continua vazia apos remover o valor, rollback remove a chave criada. Se a chave contem dados de terceiros, rollback nao remove a chave.

Rollback coverage deve ser reportado por nivel: capacidade do handler, validacao unitaria por otimizacao, validacao de integracao, validacao em VM ou validacao em hardware. As 12 definicoes nao devem ser anunciadas como individualmente comprovadas end-to-end quando apenas o handler compartilhado foi exercitado.

Operacoes HKLM do Catalog V1 permanecem cobertas por contrato, dry run, allowlist canonica e testes negativos de tamper; apply real deve ser validado em VM ou ambiente tecnico elevado antes de uso comercial amplo.

## Restore Point

Restore point real esta modelado por `RestorePointService`, mas nao e criado automaticamente na Fase 4. Para a prova controlada HKCU, a policy e `NotRequired`.

## Recovery

`RecoveryService` lista sessoes persistidas e retorna candidates quando:

- `CompletedAtUtc` esta ausente;
- estado final nao e `Completed`, `CompletedWithWarnings`, `RolledBack` ou `Cancelled`;
- artefato local e invalido: JSON truncado, schema desconhecido, hash divergente, snapshot inconsistente ou `.tmp` residual.

A acao sugerida pode ser `Inspect`, `Verify`, `Rollback` ou `ManualRecovery`. Nenhuma recuperacao destrutiva roda automaticamente na Fase 4.
