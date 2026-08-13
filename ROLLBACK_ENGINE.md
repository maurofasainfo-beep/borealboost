# BorealBoost - Rollback Engine

Data: 2026-08-12
Status: arquitetura conceitual.

## Objetivo

Permitir reversao segura de alteracoes aplicadas pelo BorealBoost por item ou sessao, usando snapshot detalhado e restore point como camada adicional.

Restore point sozinho nao basta.

## Camadas de seguranca

1. Preflight.
2. Snapshot por operacao.
3. Restore point.
4. Transaction journal.
5. Verify apos apply.
6. Undo por item.
7. Rollback por sessao.
8. Verify apos rollback.

## Snapshot

Capturar antes de cada operacao:

- registry key/value/type/existencia;
- service startup type/status;
- power plan ativo e plano criado;
- DNS por interface;
- Windows feature state;
- AppX/provisioned package state;
- policy values;
- arquivos temporarios alvo quando aplicavel;
- driver package metadata quando Driver Engine operar.

SnapshotItem deve registrar:

- metodo de captura;
- valor anterior;
- se o valor existia;
- como restaurar;
- limitacoes.

Snapshot e obrigatorio para operacoes `reversibility = Full` e para qualquer operacao cujo rollback dependa de valor anterior. Se snapshot obrigatorio falhar, a operacao nao executa.

## Restore Point

Nome proposto:

`BorealBoost - Pre Optimization - YYYY-MM-DD HH-mm`

Regras:

- criar antes de otimizacoes relevantes;
- verificar criacao;
- nao inventar percentual;
- se a API nao der progresso real, mostrar etapas reais;
- se falhar, nao continuar silenciosamente.

Limitacao importante:

- `Checkpoint-Computer` e suportado em Windows client, mas a Microsoft documenta limite de um restore point por dia a partir do Windows 8.

Politica proposta:

- tentar criar restore point;
- se falhar por limite de 24h, identificar restore point recente e exibir risco;
- seguir apenas se politica aprovada permitir confirmacao explicita do tecnico e snapshot estiver completo;
- registrar que restore point novo nao foi criado.

## Undo individual

Cada otimizacao deve declarar:

- supportsUndo;
- undoStrategy;
- requiredSnapshotItems;
- verificationAfterUndo;
- fallbackDefaultPolicy.

Undo nunca deve usar valor padrao arbitrario. Se nao houver snapshot e nao houver default oficial confiavel, UI informa impossibilidade de restauracao exata.

O contrato novo de `OperationSpec` tambem deve declarar:

- `reversibility`: Full, Partial ou None;
- `rollbackStrategy`;
- `snapshotRequirements`;
- `rebootBoundary`;
- `failurePolicy`;
- `timeout`.

Operacao `reversibility = None` nao aparece como reversivel na UI e exige confirmacao individual antes do apply.

## Rollback de sessao

Fluxo:

1. Selecionar sessao.
2. Mostrar detalhes de alteracoes.
3. Avaliar estado atual.
4. Marcar itens Applied/NotApplied/ChangedOutsideBorealBoost/Unknown.
5. Planejar rollback na ordem inversa.
6. Confirmar.
7. Reverter.
8. Verificar.
9. Registrar resultado.

## Estados

- SnapshotCaptured.
- RestorePointCreated.
- RestorePointFailed.
- ApplyStarted.
- Applied.
- VerificationPending.
- Verified.
- Failed.
- Interrupted.
- RecoveryPending.
- RebootPending.
- RollbackPlanned.
- RollbackStarted.
- RollbackInterrupted.
- RolledBack.
- RollbackVerified.
- RollbackFailed.
- ManualActionRequired.

## Recovery de sessao incompleta

Na inicializacao, antes de permitir nova otimizacao, o BorealBoost deve detectar sessoes sem commit final valido.

Regras:

- sessao sem `completedAt` e sem estado final valido e incompleta;
- sessao incompleta nunca aparece como concluida;
- journal e snapshots sao append-only para fins de auditoria;
- recovery deve revalidar catalogo, ExecutionPlan e estado atual antes de sugerir retomar ou reverter;
- se houver reboot pendente, o verify pos-reboot vem antes de qualquer novo apply.

Cenarios:

- crash antes do Apply: marcar `Interrupted`; permitir retomar do ponto seguro ou cancelar, sem assumir alteracao.
- crash durante Apply: marcar operacao como `UnknownAfterCrash`; detectar estado real antes de retry ou rollback.
- crash depois do Apply e antes do Verify: marcar `VerificationPending`; executar verify antes de commit.
- crash durante rollback: marcar `RollbackInterrupted`; continuar somente se estrategia for idempotente e segura, senao `ManualActionRequired`.
- encerramento inesperado do Windows: tratar como crash e recuperar via journal na proxima inicializacao.
- reboot obrigatorio no meio da sessao: gravar `RebootPending`; apos reiniciar, revalidar itens anteriores e continuar apenas com confirmacao/politica.

## Tratamento de falhas

- Se snapshot falhar, a operacao nao deve executar.
- Se restore point falhar, a politica define se bloqueia ou exige override explicito.
- Se undo falhar, registrar erro completo e orientar acao manual.
- Se estado mudou externamente depois da otimizacao, exigir confirmacao antes de sobrescrever.
- Se rollback falhar parcialmente, manter sessao em `RollbackFailed` ou `ManualActionRequired`, nunca `RolledBack`.

## UI de restauracao

Tela `Restaurar`:

- lista de sessoes por data/cliente;
- quantidade de alteracoes;
- restore point;
- snapshot;
- status de rollback;
- botoes Ver detalhes, Reverter sessao, Reverter selecionados;
- log expansivel.

## Pendencias

- Validar criacao de restore point em Windows 10/11 e comportamento do limite diario.
- Definir retencao de snapshots.
- Definir criptografia/ACL de snapshots em ProgramData.
