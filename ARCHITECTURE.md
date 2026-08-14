# BorealBoost - Architecture

Data: 2026-08-13
Status: arquitetura aprovada; Fases 1, 2, 3, 4 e 5 implementadas.

## Visao geral

BorealBoost sera uma aplicacao Windows desktop modular composta por UI moderna, dominio independente, adapters de sistema operacional, engines de analise/otimizacao/rollback e infraestrutura de logging/persistencia.

A UI nunca deve conter logica de tweak. Ela orquestra casos de uso e apresenta estados. Alteracoes no Windows passam por engines e adapters testaveis.

## Stack proposta

- Linguagem: C#.
- Runtime: .NET 10 LTS.
- UI: WinUI 3 com Windows App SDK.
- Padrao UI: MVVM.
- DI/config/logging: Microsoft.Extensions.*.
- Logging estruturado: Serilog ou Microsoft.Extensions.Logging com sinks locais, decisao final na Fase 1.
- Persistencia local: arquivos JSON estruturados em V1; SQLite fica pendente caso consultas e historico crescam.
- Installer V1: MSI via WiX Toolset, com avaliacao futura de MSIX.
- Arquitetura de privilegio: UI sem privilegio + `BorealBoost.Agent` obrigatorio; Agent elevado quando a operacao exigir privilegio.

## Projetos propostos

- `BorealBoost.App`: WinUI 3, Shell, Views, ViewModels, navigation, theme, components.
- `BorealBoost.Agent`: processo elevado para operacoes administrativas, exposto por named pipe local com ACL restrita.
- `BorealBoost.Core`: entidades, value objects, contratos, enums, regras puras.
- `BorealBoost.System`: adapters Windows para WMI/CIM, Registry, Services, Power, Storage, Network, Processes, Startup, Security.
- `BorealBoost.Analysis`: scanner, health analyzer, bottleneck analyzer, recommendation engine.
- `BorealBoost.Optimization`: catalogo, compatibility, execution planner, apply, verify, undo.
- `BorealBoost.Restore`: restore point, snapshot, rollback por item/sessao.
- `BorealBoost.Benchmark`: baseline, post-optimization collector, comparison.
- `BorealBoost.Drivers`: scanner, device identity, official source resolver, installer flow.
- `BorealBoost.Reporting`: HTML/PDF report generation.
- `BorealBoost.Infrastructure`: logging, persistence, update manifest, security helpers.
- `BorealBoost.Tests.Unit`: regras puras.
- `BorealBoost.Tests.Integration`: adapters Windows em ambiente controlado.
- `BorealBoost.Tests.System`: VMs e cenarios destrutivos.
- `BorealBoost.Installer`: empacotamento MSI.

## Dependencias entre camadas

`App` depende de contratos de `Core`, casos de uso ja implementados de `Analysis`, `Optimization` e `Restore`, alem de comunicacao com `Agent`. Dependencias de `Drivers`, `Benchmark` e `Reporting` permanecem futuras.

`Agent` executa operacoes privilegiadas chamando apenas handlers tipados allowlisted. Na Fase 5 implementada ele referencia `Core`, `Infrastructure`, `Optimization` e `System`; chamadas de `Restore`, `Drivers` e outros modulos dependem das fases futuras correspondentes.

`Core` nao depende de Windows, UI, storage, logging concreto ou PowerShell.

`System` encapsula APIs Windows e comandos nativos. Ele retorna resultados estruturados e nunca escreve diretamente na UI.

`Optimization` depende de contratos e adapters, mas nao conhece controles WinUI.

## Fluxo principal

1. App inicia.
2. App carrega configuracao, tema, historico e estado de admin.
3. Tecnico informa cliente e tipo de uso.
4. Scanner coleta `SystemSnapshot` read-only.
5. Analysis gera findings, oportunidades e recomendacoes.
6. PresetEngine monta selecao candidata.
7. CompatibilityEngine marca cada item como Recommended, Optional, NotRecommended ou Incompatible.
8. ExecutionPlanner gera plano antes de qualquer alteracao.
9. Tecnico revisa e confirma.
10. Agent elevado cria snapshot e restore point.
11. BaselineCollector captura metricas reais.
12. OptimizationEngine executa operacoes.
13. VerificationEngine valida cada item.
14. Session commit grava logs/snapshot/resultados.
15. Benchmark pos-execucao e comparacao.
16. Reporting gera HTML/PDF.
17. Restore screen permite rollback.

## System Scanner - Fase 2

O Scanner e o primeiro modulo operacional apos a Foundation. Ele e somente leitura e nao usa o Agent para dados que o processo do usuario consegue obter com APIs normais.

Distribuicao por camadas:

- `Core`: contratos e modelo normalizado `SystemSnapshot`, `ProviderResult`, `ISystemScanner`, `ISystemScanProvider` e classificadores puros.
- `System`: adapters Windows read-only para WMI/CIM, Win32, Registry read-only, `DriveInfo` e `NetworkInterface`.
- `Analysis`: orquestrador `SystemScanner`, `SystemScanSessionService` singleton, progresso ponderado, timeout/cancelamento e cache em memoria do ultimo snapshot.
- `App`: pagina `Scanner` e Dashboard consumindo fatos reais do snapshot.

Providers V1 da Fase 2:

- OperatingSystem;
- Cpu;
- Graphics;
- Memory;
- Storage;
- Hardware/Firmware;
- Displays;
- Network;
- Devices;
- Drivers;
- Power;
- Services;
- Processes;
- Startup;
- SecurityCapabilities.

O resultado de um provider pode ser `Success`, `Partial`, `Failed`, `NotSupported`, `TimedOut` ou `Canceled`. Falha isolada nao derruba o scan inteiro; o snapshot fica `PartialScan=true` e conserva os fatos ja coletados.

O scanner aceita uma unica sessao ativa por vez. `SystemScanSessionService` rejeita starts concorrentes com `scanner.already_running`, inclusive quando a pagina Scanner e recriada por navegacao.

WMI/CIM e executado sem `Task.Run`/`WaitAsync` no adapter. A operacao usa `EnumerationOptions.Timeout` e a sessao nao e marcada como cancelada/concluida enquanto a chamada WMI nativa ainda nao retornou. Quando o provider ignora cancellation e retorna depois do timeout, o resultado e `TimedOut` e o patch nao e usado.

Memoria separa `InstalledPhysicalBytes` de `VisiblePhysicalBytes`. VRAM usa `AdapterRamStatus`; `Win32_VideoController.AdapterRAM` sozinho nao e aceito como VRAM conhecida.

Privacidade do snapshot e formalizada por `SystemSnapshotPrivacyPolicy`, que classifica campos tecnicos internos e fornece copia sanitizada para relatorios futuros.

Fontes permitidas nesta fase:

- WMI/CIM encapsulado via `System.Management`;
- Win32 APIs read-only;
- .NET BCL para rede e volumes;
- Registry somente leitura quando houver justificativa tecnica.

Proibido no scanner:

- executar PowerShell/cmd;
- iniciar processos externos;
- consultar ou instalar drivers;
- escrever Registry;
- alterar servicos, power plan, DNS, Windows Update, AppX, Defender, Firewall, features ou firmware;
- calcular Boreal Score;
- gerar recomendacoes.

## Analysis + Recommendation Engine - Fase 3

A Fase 3 interpreta o `SystemSnapshot` produzido pelo Scanner e retorna `AnalysisResult`. Ela permanece read-only e nao consulta Windows diretamente.

Distribuicao por camadas:

- `Core`: contratos `IAnalysisEngine`, `IAnalysisRule`, `IAnalysisSessionService`, `IAnalysisResultStore`, `AnalysisResult`, `AnalysisFinding`, `Recommendation`, `RecommendationPlan`, enums de categoria, status, risco, evidencia, impacto, compatibilidade e estado de sessao.
- `Analysis`: `AnalysisEngine`, `AnalysisSessionService`, `InMemoryAnalysisResultStore`, `RecommendationModelValidator` e regras modulares code-first em `RecommendationEngine/Rules`.
- `App`: pagina `Analise`, filtros, preset preview e cards de recomendacao somente leitura.
- `System`: nao e referenciado por regras de analise; continua apenas fornecendo facts via Scanner.

Pipeline:

1. Scanner coleta fatos e atualiza `ISystemSnapshotStore`.
2. `AnalysisSessionService` singleton recebe o snapshot existente e rejeita starts concorrentes.
3. Regras independentes avaliam o snapshot em ordem deterministica por `RuleId`.
4. Cada regra produz `Healthy`, `Opportunity`, `Warning`, `Blocked`, `Unknown` ou `NotApplicable`.
5. `RecommendationModelValidator` valida IDs, duplicidade, presets, compatibilidade, conflitos e requisitos.
6. Duplicidade de `RecommendationId` ou invariante invalida bloqueia a analise com falha observavel.
7. `RecommendationPlan` prepara preview de Basico, Medio, Avancado e Custom.
8. UI apresenta resultados, sem botao funcional de apply.

Contratos de versionamento:

- `EngineVersion = 3.0.0`;
- `RuleCatalogVersion = 3.0.0-code-first`;
- `AnalysisResult` registra `AnalysisId`, `ScanId`, inicio/fim UTC e duracao.

Regras iniciais:

- partial scan guard;
- compatibilidade Windows;
- dispositivo sem driver;
- dispositivo com problema/desabilitado;
- Microsoft Basic Display Adapter/GPU generica;
- pouco espaco no volume do sistema;
- maquina virtual;
- contexto portatil/energia;
- volume alto de startup;
- guardrail de Secure Boot;
- memoria instalada versus memoria visivel.

Regras de seguranca:

- `Unknown` nunca vira `Opportunity`;
- nenhuma regra consulta WMI, Registry, processos, rede ou servicos por conta propria;
- recomendacoes Advanced exigem risco, justificativa e confirmacao futura;
- recomendacoes nao prometem FPS, percentual de desempenho ou benchmark;
- nenhuma recomendacao executa Optimization, Rollback, driver install/update ou comando;
- GPU virtual/generica em VM nao gera recomendacao de driver grafico fisico por si so;
- a regra de startup e observacional/experimental quando baseada apenas em contagem agregada.

## Optimization Engine + Safety + Snapshot + Rollback - Fase 4

A Fase 4 implementa a infraestrutura transacional antes do catalogo real de tweaks.

Distribuicao por camadas:

- `Core`: contratos `OptimizationDefinition`, `OperationSpec`, `ExecutionPlan`, `OptimizationSession`, `OperationSnapshot`, policies, IDs e validacao de seguranca de operacao.
- `Optimization`: catalogo built-in minimo de prova, `ExecutionPlanner`, `ExecutionPlanValidator`, `DryRunService`, `PreflightService`, state machine, session service e recovery foundation.
- `Restore`: `RestorePointService` modelado e `RollbackEngine` foundation.
- `System`: handler Windows controlado `BorealIntegrationRegistryOperationHandler` para `HKCU\Software\BorealBoost\IntegrationTest`.
- `Infrastructure`: persistencia atomica de `OptimizationSession` com envelope versionado e hash SHA-256 de integridade.
- `Agent`: IPC tipado para validar/capturar/aplicar/verificar/rollback de operacao allowlisted.
- `App`: paginas `Otimizacao` e `Restauracao` para Review Plan, Dry Run, prova controlada e recovery.

Limites da Fase 4:

- 1 operacao real controlada de integracao em HKCU proprio do BorealBoost;
- 0 tweaks reais de performance;
- 0 alteracoes de Services, Power, DNS, Drivers, Windows Update, Defender, Firewall, VBS ou Memory Integrity;
- catalogo amplo e presets operacionais ficam para Fase 5.

Pipeline implementado:

1. `ExecutionPlanner` cria plano versionado e hash deterministico.
2. `ExecutionPlanValidator` revalida catalogo, handler, OS/build, dependencias, conflitos, allowlist, OperationSpec canonica e `PlanHash`.
3. `DryRunService` calcula operacoes e blockers sem modificar Windows.
4. `PreflightService` bloqueia plano invalido antes de qualquer snapshot/apply.
5. `OptimizationSessionService` adquire lock cross-process e persiste sessao planejada.
6. Snapshot com hash por item e journal sao persistidos antes da mutacao.
7. Handler aplica apenas operacao tipada allowlisted.
8. Verification e obrigatoria.
9. Falha aciona rollback quando a policy declara `AttemptRollback`.
10. Recovery detecta sessoes sem conclusao duravel e artefatos corrompidos, sem permitir que aparecam como `Completed`.

Correcoes de revalidacao da Fase 4:

- rollback Registry preserva existencia, `RegistryValueKind`, valor bruto e `RegistryView` para `String`, `ExpandString`, `DWord`, `QWord`, `MultiString` e `Binary`;
- `REG_EXPAND_SZ` e capturado sem expandir variaveis;
- snapshot adulterado e rejeitado por hash e por binding de sessao/plano/operacao;
- Agent valida `CatalogVersion` e equivalencia exata da OperationSpec contra o catalogo built-in confiavel;
- `PlanHash` torna plano aprovado imutavel para campos transacionais;
- lock cross-process impede sessoes simultaneas entre duas instancias do App.

## Optimization Catalog - Fase 5

A Fase 5 adiciona o primeiro catalogo real, pequeno e defensavel. Ela nao altera a arquitetura transacional da Fase 4; apenas amplia o catalogo built-in e permite que a pagina `Otimizacao` componha presets reais.

Distribuicao por camadas:

- `Core`: campos adicionais de `OptimizationDefinition`, `CatalogManifestMetadata`, `OptimizationPresetSelection` e allowlist canonica de Registry.
- `Optimization`: `BuiltInOptimizationCatalog` V1, `OptimizationPresetEngine`, validacao de definicao, canonical OperationSpec validator e planejamento.
- `System`: mesmo handler de Registry controlado, agora instanciado tambem para `OperationType.RegistryValue`.
- `Agent`: revalida cada operacao recebida contra a definicao canonica do catalogo built-in, alem da allowlist de target/desired state.
- `App`: preset preview Basic/Medium/Advanced/Custom, Review Plan, Dry Run e execucao somente de itens `Selected`.

Catalog V1:

- `schemaVersion = 5.1.0`;
- `catalogVersion = 5.1.0-built-in-v1`;
- 12 OptimizationDefinitions reais, excluindo a prova de integracao;
- 6 Safe, 5 Medium, 1 Advanced, 0 Aggressive/Experimental;
- 12 reversiveis por `SnapshotRestore`;
- 0 SecurityTradeoff;
- 0 reboot automatico;
- classificacao explicita por `TechnicalCategory`, `PerformanceRelevance`, `AutomaticPresetSuitability`, `ConfigurationMechanism`, `ActivationBoundary`, `VerificationLevel` e `RollbackValidationLevel`;
- detalhes em `OPTIMIZATION_CATALOG.md`.

Operacoes reais permitidas:

- somente `OperationType.RegistryValue`;
- targets fixos em `TrustedRegistryOperationTargets.CatalogV1`;
- desired state fixo no catalogo;
- `RegistryValue` fora da allowlist e rejeitado pelo Planner, Agent e handler.

Preset policy:

- Basic seleciona somente itens `Automatic` que sejam Safe, reversiveis, sem reboot, sem Experimental e sem SecurityTradeoff;
- Medium seleciona itens `Automatic` Safe/Medium e mostra itens `OptIn` compativeis como `RequiresConfirmation`;
- Advanced pode mostrar itens `AdvancedOnly`/maior risco compativeis como `RequiresConfirmation`, nao como selecao automatica silenciosa;
- Custom expoe preferencias compativeis, mas nao bypassa `Blocked`;
- AnalysisResult stale ou Windows/build Unknown bloqueia selecao automatica.

Itens rejeitados nesta fase incluem Defender disable, Firewall disable, Windows Update permanent disable, pagefile disable, HPET/BCD/timer hacks, netsh/TCP universal, service disable lists, debloat AppX, OneDrive removal e driver registry hacks.

## Contrato arquitetural do BorealBoost.Agent

`BorealBoost.Agent` e requisito arquitetural da V1. O aplicativo inteiro elevado nao e fallback aceito. A UI deve permanecer sem privilegio permanente; toda operacao administrativa passa pelo Agent elevado e pelo ExecutionPlan validado. Operacoes nao administrativas tambem passam pelo Agent, mas podem usar token nao elevado quando isso for necessario para preservar o escopo correto do usuario, como HKCU.

### Trust boundary

A fronteira de confianca fica entre `BorealBoost.App` e `BorealBoost.Agent`.

- App: coleta intencao do tecnico, exibe dados, monta solicitacao e envia plano candidato.
- Agent: revalida catalogo, politica, identidade do cliente, compatibilidade, snapshots e ExecutionPlan antes de qualquer alteracao.
- UI nao e fonte confiavel para comandos, paths executaveis, scripts, valores fora do schema ou decisao de autorizacao.
- ProgramData nao e fonte confiavel por si so; arquivos carregados dali exigem schema, hash, assinatura e politica de versao.

### Lifecycle

1. App cria `sessionId`, `correlationId` inicial e nonce de bootstrap em memoria.
2. App resolve o binario instalado e conhecido `BorealBoost.Agent`.
3. Se a definicao canonica declarar `RequiresElevation=true`, App solicita elevacao via UAC; caso contrario inicia o Agent sem elevacao.
4. Agent cria named pipe local de sessao e aguarda handshake.
5. App conecta, executa handshake autenticado e negocia `protocolVersion`.
6. Agent aceita somente uma sessao ativa por instancia.
7. Agent processa requisicoes allowlisted, persiste journal transacional e retorna resultados estruturados.
8. Agent encerra apos commit/rollback, cancelamento confirmado ou idle timeout.
9. Se Agent cair, App marca a sessao como `Interrupted` e o recovery sera feito na proxima inicializacao antes de qualquer nova execucao.

### Inicio pelo App

O App so pode iniciar o Agent a partir do caminho instalado e registrado pelo instalador. O caminho do executavel do Agent nao pode vir de input da UI, catalogo externo ou argumento editavel pelo usuario.

Argumentos permitidos ao Agent no bootstrap:

- `--pipeName` gerado pelo App para a sessao;
- `--sessionId`;
- `--bootstrapNonce`;
- `--protocolVersion`.

Nenhum argumento pode representar comando, PowerShell, script, executavel arbitrario, parametro de tweak ou payload operacional.

### Autenticacao App-Agent

O handshake deve validar:

- nonce de bootstrap de uso unico;
- `sessionId` e `protocolVersion`;
- identidade do processo cliente conectado ao named pipe;
- usuario/SID esperado da sessao interativa;
- caminho instalado e assinatura/hash do App quando assinatura de codigo estiver disponivel;
- caminho instalado e assinatura/hash do Agent validado pelo App.

Falha em qualquer etapa encerra o pipe e registra evento de seguranca.

### Autorizacao

O Agent autoriza por politica, nao por comando recebido:

- somente `messageType` conhecido;
- somente `operationId` presente no catalogo confiavel carregado pelo proprio Agent;
- somente operacao permitida para o `RiskLevel`, confirmacoes e perfil selecionado;
- somente targets aceitos pelo schema e pelas regras de compatibilidade;
- nenhuma elevacao adicional encadeada.

Operacoes relacionadas a seguranca critica, drivers criticos, rollback destrutivo ou reboot exigem confirmacao explicita registrada no ExecutionPlan.

### Named pipe ACL

Cada instancia usa pipe de sessao local, por exemplo:

`\\.\pipe\BorealBoost.Agent.{sessionId}`

ACL minima:

- SID do usuario interativo que iniciou a sessao: read/write;
- Administrators local: read/write;
- LocalSystem: read/write quando necessario;
- negar acesso remoto;
- negar Everyone/Users generico.

O Agent deve rejeitar conexoes cujo processo cliente nao corresponda ao App esperado, mesmo que o pipe permita conexao por ACL.

### Protocolo de mensagens

O protocolo deve ser estruturado e versionado, com mensagens length-prefixed e payload validado por schema.

Envelope minimo:

- `protocolVersion`;
- `messageType`;
- `sessionId`;
- `correlationId`;
- `requestId`;
- `sequenceNumber`;
- `timestampUtc`;
- `nonce`;
- `payloadHash`;
- `payload`.

Tipos iniciais:

- `HandshakeRequest` / `HandshakeResponse`;
- `ValidateExecutionPlanRequest`;
- `PrepareSafetyRequest`;
- `ApplyOperationRequest`;
- `VerifyOperationRequest`;
- `RollbackRequest`;
- `CancelRequest`;
- `ProgressEvent`;
- `OperationResult`;
- `SessionStatusRequest` / `SessionStatusResponse`;
- `ShutdownRequest`.

Na implementacao da Fase 4, o prototipo IPC operacional usa mensagens tipadas equivalentes por operacao:

- `ValidateOperationRequest` / `ValidateOperationResponse`;
- `CaptureSnapshotRequest` / `CaptureSnapshotResponse`;
- `ExecuteOperationRequest` / `ExecuteOperationResponse`;
- `VerifyOperationRequest` / `VerifyOperationResponse`;
- `RollbackOperationRequest` / `RollbackOperationResponse`.

Essas mensagens carregam `OperationSpec` tipado e `OperationSnapshotItem`, nunca command line, shell, script ou executable path fornecido pela UI.

### Versionamento do protocolo

`protocolVersion` usa SemVer. Mudanca incompatavel incrementa major. O Agent deve rejeitar major desconhecido e pode aceitar minor anterior quando o contrato for compativel. A versao negociada deve ficar registrada na sessao.

### Validacao, limites e timeout

- Mensagem individual: limite inicial de 1 MiB.
- ExecutionPlan serializado: limite inicial de 5 MiB.
- Anexos/binarios nao trafegam pelo pipe; sao referenciados por artefatos ja validados.
- Payload fora de schema, com campo desconhecido critico, tipo invalido ou valor fora de faixa e rejeitado.
- Handshake timeout: 30 segundos.
- Request timeout padrao: 5 minutos, substituivel apenas por `OperationSpec.timeout` com limite superior aprovado por politica.
- Idle timeout do Agent: 10 minutos sem requisicao ativa.

### Cancelamento e desconexao

Cancelamento e cooperativo. O Agent so interrompe em pontos seguros definidos pela operacao. Operacao sem ponto seguro deve completar, falhar ou entrar em estado `ManualActionRequired`.

Se o App desconectar:

- Agent nao marca sucesso por ausencia da UI;
- operacao em andamento segue a politica da operacao;
- journal persiste estado duravel;
- sessao fica `Interrupted`, `VerificationPending`, `RollbackPending` ou `ManualActionRequired`, nunca `Completed` sem verify e commit.

### Protecao contra replay

Cada request usa `requestId`, `sequenceNumber`, timestamp e nonce. O Agent rejeita:

- `requestId` repetido;
- sequencia fora de ordem;
- timestamp fora da janela configurada;
- nonce ja usado;
- payload cujo hash nao confere.

### Allowlist de operacoes privilegiadas

O Agent nao aceita command line, PowerShell, script ou executavel arbitrario enviados pela UI.

Permitido apenas:

- handlers internos nomeados por `operationType`;
- parametros tipados e validados por schema;
- scripts/templates internos assinados ou embutidos quando nao houver API melhor;
- ferramentas oficiais Windows chamadas por adaptadores fechados, com argumentos montados pelo proprio handler.

Proibido:

- executar string PowerShell recebida do App;
- executar `cmd.exe /c` recebido do App;
- executar path de `.exe`, `.ps1`, `.bat`, `.cmd` ou `.msi` enviado pela UI;
- aceitar catalogo atualizado nao assinado como fonte de novas capacidades privilegiadas.

### Validacao do ExecutionPlan

Antes de aplicar, o Agent deve:

- carregar catalogo built-in e catalogo atualizado confiavel;
- validar assinatura, hash, schema e versao do catalogo;
- resolver cada `optimizationId` e `operationId`;
- recalcular compatibilidade contra o `SystemProfile` atual ou snapshot recente;
- validar dependencias, conflitos, reboot boundary, risco, confirmacoes e snapshot requirements;
- garantir que a ordem do plano respeita preflight, snapshot, restore point, apply, verify e commit;
- bloquear qualquer operacao fora da allowlist.

Planos invalidos sao rejeitados e registrados como falha de seguranca/validacao, sem apply parcial.

## Sistema de configuracao

Configuracoes locais:

- `%ProgramData%\BorealBoost\Config\machine.json`
- `%ProgramData%\BorealBoost\Catalog\Updates\*.json`
- `%ProgramData%\BorealBoost\Sessions\*.json`
- `%ProgramData%\BorealBoost\Logs\*.jsonl`
- `%AppData%\BorealBoost\user-preferences.json`

Catalogo built-in fica junto dos binarios assinados do produto. Catalogos atualizados podem ficar em ProgramData, mas so entram em uso apos validacao de schema, hash, assinatura digital, publisher confiavel e protecao contra downgrade. Cada entrada deve possuir compatibilidade, risco, evidencia, detection, apply, verify, undo quando aplicavel e contrato transacional.

## Logging

Cada registro deve conter:

- timestamp UTC e local;
- sessionId;
- operationId;
- optimizationId;
- action;
- oldValue/newValue quando seguro;
- API/comando;
- duration;
- result;
- error completo quando houver;
- requiresRestart.

Logs nao devem conter senhas, tokens, chaves, cookies, conteudo pessoal desnecessario ou dumps de perfil.

Na Foundation, App e Agent gravam arquivos JSONL separados por papel/processo em `%LocalAppData%\BorealBoost\Logs`, por exemplo `app-YYYYMMDD-PID.jsonl` e `agent-YYYYMMDD-PID.jsonl`. Essa decisao evita lock entre processos no bootstrap App-Agent e mantem logs locais simples ate a escolha de sink definitivo.

## Sistema de updates

V1 pode apenas verificar atualizacao. Arquitetura deve prever:

- manifest HTTPS;
- versao semantica;
- release notes;
- hash;
- assinatura;
- canal stable/manual;
- rollback de update em fase futura.

Nao executar scripts remotos.

## Erros e cancelamento

Operacoes longas devem ser async/background. UI mostra progresso por etapa real, nao percentual falso.

Falha critica interrompe o plano. Quando possivel, o engine reverte itens ja aplicados na sessao atual.

## Estrutura de dados de sessao

Uma OptimizationSession deve armazenar:

- cliente e tecnico;
- sessionId/correlationIds;
- system profile antes/depois;
- preset selecionado;
- execution plan;
- protocolVersion;
- catalogVersion/catalogHash;
- snapshot;
- restore point;
- transaction journal;
- baseline;
- operation results;
- verification results;
- reboot required;
- recovery status;
- report artifacts;
- rollback status.

## Pendencias

- Escolher biblioteca concreta de PDF.
- Decidir Serilog versus logging nativo com JSONL.
- Validar custo real do Agent elevado antes da Fase 1.
- Confirmar se SQLite sera necessario para historico multi-sessao.
