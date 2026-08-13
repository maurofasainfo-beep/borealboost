# BorealBoost - Optimization Engine

Data: 2026-08-12
Status: arquitetura implementada parcialmente na Fase 4; catalogo amplo de tweaks fica para Fase 5.

## Objetivo

Executar otimizacoes Windows de forma declarativa, compativel, auditavel, reversivel e verificavel.

O engine nao deve ser uma lista de comandos. Ele deve transformar recomendacoes em um plano seguro, capturar estado anterior, aplicar operacoes oficiais quando possivel, verificar resultado e permitir rollback.

## Pipeline

1. LoadBuiltInCatalog
2. LoadUpdatedCatalog
3. ValidateCatalogIntegrity
4. ValidateCatalogSchema
5. DetectCurrentState
6. EvaluateCompatibility
7. BuildRecommendations
8. ComposePreset
9. CreateExecutionPlan
10. ValidateExecutionPlanInAgent
11. PresentPlanToTechnician
12. BeginTransactionJournal
13. CaptureSnapshot
14. CreateRestorePoint
15. CollectBaseline
16. ExecuteOperations
17. VerifyOperations
18. CommitSession
19. CollectPostState
20. GenerateResults

## Componentes

## Implementacao Fase 4

A Fase 4 implementa o nucleo operacional seguro com escopo propositalmente pequeno:

- `OptimizationDefinition` e `OperationSpec` tipados em `BorealBoost.Core`;
- `BuiltInOptimizationCatalog` com apenas `BB.OPT.INTEGRATION.REGISTRY_PROOF`;
- `ExecutionPlanner` e `ExecutionPlanValidator`;
- `DryRunService` e `PreflightService`;
- `OptimizationSessionService` com state machine e exclusao de concorrencia;
- `FileOptimizationSessionStore` com escrita temp + flush + rename e hash SHA-256 de integridade;
- `OperationSnapshot` com hash por item antes de qualquer mutacao;
- `BorealIntegrationRegistryOperationHandler` para prova controlada em HKCU;
- verification obrigatoria;
- rollback por snapshot;
- recovery foundation para sessoes incompletas;
- mensagens IPC tipadas no Agent para validate/capture/execute/verify/rollback.

Nao implementado na Fase 4:

- catalogo updated remoto;
- assinatura digital de catalogo em runtime;
- catalogo real Safe/Medium/Advanced/Aggressive;
- restore point real;
- Service/Power/DNS/AppX/Driver operations;
- benchmark, Boreal Score operacional ou reporting.

### Operacao real controlada

Recurso usado:

`HKCU\Software\BorealBoost\IntegrationTest\Phase4ControlledValue`

Regras:

- handler valida que hive, key, value name, view, timeout, retry, snapshot e rollback sao exatamente permitidos;
- snapshot captura existencia, tipo, valor anterior bruto e RegistryView;
- apply grava somente o valor de prova do BorealBoost;
- verify le o valor de volta;
- rollback restaura exatamente existencia/tipo/valor original ou remove o valor se ele nao existia;
- se o valor mudar externamente entre apply e rollback, rollback nao sobrescreve cegamente;
- tipos preservados: `String`, `ExpandString`, `DWord`, `QWord`, `MultiString` e `Binary`;
- `REG_EXPAND_SZ` e capturado sem expandir variaveis de ambiente.

### OptimizationCatalog

Carrega definicoes versionadas de otimizacao.

Regras:

- `schemaVersion` obrigatorio;
- `catalogVersion` obrigatorio;
- IDs estaveis;
- links de documentacao;
- risk/evidence obrigatorios;
- supported OS/build/hardware obrigatorio;
- detection/apply/verify/undo definidos quando aplicavel.

### Trusted Optimization Catalog

O catalogo e uma fonte de politica privilegiada. O Agent nao deve confiar em um JSON apenas porque esta em ProgramData.

Campos de manifesto:

- `schemaVersion`;
- `catalogVersion`;
- `catalogId`;
- `publisher`;
- `channel`;
- `minimumAppVersion`;
- `minimumAgentVersion`;
- `createdAt`;
- `expiresAt` quando aplicavel;
- `contentHash`;
- `signature`;
- `signingCertificateThumbprint`;
- `previousCatalogVersion`;
- lista de definicoes e presets.

Separacao de origem:

- built-in: empacotado com os binarios assinados do BorealBoost;
- updated: baixado/instalado depois e armazenado em `%ProgramData%\BorealBoost\Catalog\Updates`;
- o catalogo updated so pode sobrepor built-in quando assinatura, publisher, schema, hash, versao e politica de canal forem validos;
- se updated falhar validacao, o Agent ignora o updated, registra evento e usa apenas built-in valido;
- se built-in falhar validacao, o engine bloqueia apply e permite apenas diagnostico/restore seguro quando possivel.

Integridade:

- hash canonico do conteudo do catalogo;
- assinatura digital sobre manifesto e hash;
- cadeia de certificado/publisher confiavel definida pelo produto;
- protecao contra catalogo expirado quando `expiresAt` existir;
- revocation list futura deve poder bloquear catalogos/operacoes.

Protecao contra downgrade:

- Agent persiste a maior `catalogVersion` confiavel ja aceita por canal;
- versao menor e rejeitada, salvo manifest de rollback assinado e explicitamente permitido;
- schema major desconhecido bloqueia o catalogo;
- schema minor desconhecido pode ser aceito apenas quando marcado como backward-compatible.

Politica de atualizacao:

- V1 pode verificar catalogos, mas a aplicacao de update de catalogo exige validacao completa antes de uso;
- catalogo atualizado nunca adiciona nova capacidade privilegiada fora dos handlers allowlisted existentes no Agent;
- falha de update nao impede uso do built-in valido;
- relatorio registra `catalogVersion`, `schemaVersion` e hash usados na sessao.

### CatalogValidator

Falha o carregamento se:

- assinatura, hash, publisher ou schema forem invalidos;
- tentativa de downgrade nao autorizada;
- ID duplicado;
- risco ausente;
- evidencia ausente;
- operacao sem undo quando marcada reversivel;
- operacao sem contrato transacional quando aplicavel;
- compatibilidade ausente;
- link de documentacao ausente;
- preset referencia ID inexistente;
- conflito/dependencia invalido.

### DetectionEngine

Le estado real, nao historico do BorealBoost.

Estados:

- Applied;
- NotApplied;
- PartiallyApplied;
- Unknown;
- NotApplicable.

Fontes:

- Registry;
- Services;
- Windows Optional Features;
- Power plans;
- DNS;
- AppX/provisioned apps;
- Policy;
- CIM/WMI;
- comandos oficiais quando nao houver API melhor.

### CompatibilityEngine

Entrada:

- SystemProfile;
- OptimizationDefinition;
- TechnicianSession;
- current state.

Saida:

- Compatible;
- Incompatible;
- NeedsConfirmation;
- NotApplicable;
- Unknown.

Se a compatibilidade nao for comprovada, nao executa.

### RecommendationEngine

Nao deve mapear preset para "habilitar tudo".

Exemplos de regras:

- notebook bloqueia plano desktop-only;
- Hyper-V/WSL ativos bloqueiam desligamento cego de virtualizacao;
- HDD/NVMe geram recomendacoes diferentes;
- GPU e driver determinam regras de gaming;
- Windows 10 e Windows 11 usam catalogos/regras por build;
- dominio corporativo/VPN/RDP/impressora/Bluetooth evitam remocao automatica de recursos.

### ExecutionPlanner

Ordena operacoes:

1. preflight;
2. safety;
3. snapshot;
4. restore point;
5. baseline;
6. apply safe;
7. apply advanced/aggressive;
8. verify;
9. reboot-required marking;
10. report.

Bloqueia conflitos e dependencias ausentes.

### TransactionCoordinator

Formaliza apply/verify/rollback como transacao auditavel por sessao. Nao ha transacao atomica global do Windows; por isso cada operacao declara limites, snapshot e recovery.

Antes de aplicar qualquer operacao:

- cria `OptimizationSession` em estado `Planned`;
- grava ExecutionPlan, `catalogVersion`, `catalogHash` e `planHash`;
- valida `planHash` canonico antes de preflight/apply;
- cria journal duravel;
- grava entrada `SnapshotCaptured` por operacao antes do apply;
- grava `ApplyStarted` antes de tocar no sistema;
- grava `ApplyCompleted` somente apos retorno estruturado do handler;
- grava `VerificationPending` antes do verify;
- grava `Verified` somente quando a estrategia de verificacao confirmar o estado.

`Completed` so e permitido apos todas as operacoes obrigatorias estarem `Verified`, itens opcionais falhos estarem explicitamente classificados, reboot pendente estar registrado e commit duravel concluido.

O Agent valida a OperationSpec recebida contra a definicao canonica do catalogo built-in confiavel. Mesmo `OptimizationId` e `OperationId` validos nao bastam: target, desired state, timeout, retry, snapshot, reversibilidade, rollback e handler precisam coincidir com a definicao aprovada.

### Contrato transacional de OperationSpec

Cada `OperationSpec` deve declarar, quando aplicavel:

- `idempotency`: `Idempotent`, `ConditionallyIdempotent` ou `NonIdempotent`;
- `reversibility`: `Full`, `Partial` ou `None`;
- `rebootBoundary`: `None`, `AllowedAfterOperation`, `RequiredAfterOperation` ou `RequiredBeforeContinue`;
- `retryPolicy`: tentativas, backoff, erros retryable e limite maximo;
- `timeout`: duracao maxima da operacao;
- `failurePolicy`: `StopSession`, `ContinueIfIndependent`, `AttemptRollback`, `MarkManualActionRequired`;
- `verificationStrategy`: leitura exata, estado de servico, feature state, hash, presence/absence, comando oficial ou verificacao manual;
- `rollbackStrategy`: snapshot restore, inverse operation, official default, vendor rollback, manual only ou none;
- `snapshotRequirements`: itens obrigatorios, metodo de captura, sensibilidade e bloqueio se snapshot falhar.

Regras:

- `Full` exige snapshot ou inverse operation confiavel.
- `Partial` exige declarar perda residual e apresentar isso no plano.
- `None` exige aviso, confirmacao individual e bloqueio em presets Basico/Medio salvo justificativa aprovada.
- Operacao `NonIdempotent` nao pode ser repetida automaticamente depois de crash; recovery deve primeiro detectar estado real.
- Operacao com `RequiredBeforeContinue` coloca a sessao em `RebootPending` e bloqueia proximas operacoes ate recovery pos-reboot.

### OperationExecutor

Executa somente operacoes modeladas.

O executor nao aceita command line, PowerShell, script ou executavel arbitrario vindo da UI. Quando uma ferramenta Windows oficial for necessaria, o handler interno monta argumentos a partir de parametros tipados e validados.

Resultado estruturado:

- success;
- exitCode;
- stdout;
- stderr;
- startedAt;
- duration;
- requiresRestart;
- errorType;
- errorMessage.

### VerificationEngine

Verifica o estado esperado apos aplicacao.

Tipos:

- exact value;
- service startup type;
- feature state;
- command result;
- package absence/presence;
- power plan active;
- DNS current servers;
- manual verification required.

### RollbackCoordinator

Aciona undo por item ou sessao usando snapshot. Se nao houver snapshot, so usa default oficial quando documentado com evidencia.

## Tipos planejados para fases futuras

Fora da prova controlada da Fase 4, os tipos abaixo permanecem planejados para fases futuras e exigem handlers especificos, validação, snapshot, verify e rollback antes de qualquer uso real:

- Registry read/set/delete fora da chave de integracao;
- Service read/start/stop/set startup type;
- Powercfg read/create/duplicate/activate/delete;
- DISM optional feature query/enable/disable;
- DNS query/set/reset;
- AppX/provisioned package query/remove;
- File cleanup com escopo controlado;
- System repair via handlers internos de fixes, nao como performance tweak e nunca como string livre;
- PnPUtil/driver flow no Driver Engine.

## Politica de falha

- Falha em preflight: nao executa.
- Falha em snapshot: nao executa.
- Falha em restore point: nao continua silenciosamente.
- Falha em operacao Safe isolada: registra, continua apenas se nao houver dependencia e politica permitir.
- Falha em operacao critica/dependente: interrompe plano.
- Falha em verification: marca item como FailedVerification, nao sucesso.
- Falha em rollback: registra e exibe risco residual.

## Crash, reboot e recovery

Na inicializacao, antes de permitir nova otimizacao, App e Agent devem procurar sessoes sem commit final em `%ProgramData%\BorealBoost\Sessions`. Uma sessao incompleta deve aparecer como `RecoveryPending`, `Interrupted`, `VerificationPending`, `RebootPending`, `RollbackFailed` ou `ManualActionRequired`, nunca como concluida.

### Crash antes do Apply

Se a ultima entrada duravel for `Planned`, `SafetyPreparing`, `SafetyPrepared` ou `SnapshotCaptured`, nenhuma alteracao deve ser assumida. Recovery revalida snapshot/restore point, marca a sessao como `Interrupted` ou permite retomar a partir do ponto seguro com confirmacao do tecnico.

### Crash durante Apply

Se a ultima entrada for `ApplyStarted` sem `ApplyCompleted`, o estado da operacao e `UnknownAfterCrash`. Recovery executa detection/verification da operacao:

- se estado esperado ja esta presente, marcar `VerificationPending` e verificar;
- se estado anterior esta presente, marcar `Failed` sem rollback;
- se estado parcial/desconhecido, aplicar `failurePolicy` e `rollbackStrategy`;
- se a operacao for `NonIdempotent`, nao repetir apply automaticamente.

### Crash depois do Apply e antes do Verify

Se existe `ApplyCompleted` sem `Verified`, a sessao entra em `VerificationPending`. Recovery executa verify antes de qualquer commit. Falha de verify nao pode ser convertida em sucesso.

### Crash durante rollback

Se a ultima entrada for `RollbackStarted` sem `RollbackVerified`, recovery marca `RollbackInterrupted`/`RollbackFailed` e tenta continuar rollback somente para operacoes com estrategia idempotente e segura. Caso contrario, exige acao manual.

### Encerramento inesperado do Windows

O comportamento e igual a crash: journal e snapshots sao fonte de verdade. A proxima inicializacao deve detectar a sessao incompleta antes de novo apply. Reboot/shutdown nao confirmado nao pode transformar operacao pendente em sucesso.

### Reboot obrigatorio no meio da sessao

Operacao com `rebootBoundary = RequiredBeforeContinue`:

- grava `RebootPending`;
- bloqueia operacoes seguintes;
- orienta reboot;
- na proxima inicializacao, recovery reabre a sessao, reexecuta scanner/verify dos itens anteriores e continua apenas apos confirmacao ou politica explicita.

Operacao com `RequiredAfterOperation` pode concluir apply, mas a sessao fica com `rebootRequired = true` e resultados devem indicar que parte da verificacao pode ser pos-reboot.

## Session recovery

Mecanismo:

1. listar sessoes sem `completedAt` e sem commit final valido;
2. validar integridade do arquivo de sessao e journal;
3. revalidar catalogo usado originalmente ou aplicar politica de catalogo substituto compativel;
4. executar detection/verify das operacoes pendentes;
5. propor retomar, verificar, rollback ou marcar acao manual;
6. registrar decisao do tecnico;
7. nunca sobrescrever historico anterior sem append no journal.

## Presets

### Basico - Safe Boost

- somente Safe;
- sem alteracao agressiva de seguranca;
- foco em limpeza, inicializacao, configuracoes conservadoras.

### Medio - Performance

- Safe + Advanced bem documentadas;
- recomendacao padrao;
- sem Experimental;
- sem desabilitar protecoes criticas.

### Avancado - Extreme Performance

- pode incluir Aggressive compativel;
- modal de risco;
- confirmacao explicita;
- nunca inclui tweak irresponsavel.

### Personalizado

- filtros por categoria, risco, evidencia, estado e compatibilidade;
- selecionar todos seguros;
- limpar selecao;
- detectar aplicados;
- reverter selecionados.

## Regras de seguranca

Nunca desabilitar silenciosamente:

- Defender;
- Firewall;
- UAC;
- Secure Boot;
- BitLocker;
- Windows Update;
- Credential Guard;
- VBS;
- Memory Integrity.

Qualquer item relacionado deve ser Advanced/Experimental, com beneficio hipotetico, impacto de seguranca e confirmacao individual.

## Fontes tecnicas

- Registry API Microsoft.
- ServiceController Microsoft.
- Powercfg Microsoft.
- DISM Microsoft.
- DNS Client Microsoft.
- WMI/CIM Microsoft.
- ETW e Performance Counters Microsoft.
- WinUtil apenas como referencia de categorias e anti-padroes.

## Pendencias

- Definir schema JSON final.
- Validar restore point em Windows 10/11 com a limitacao de 24h.
- Criar politica formal para operacoes que so podem ter undo parcial.
