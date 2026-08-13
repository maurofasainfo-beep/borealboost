# BorealBoost - Architecture

Data: 2026-08-12
Status: arquitetura aprovada; Fase 1 Foundation e Fase 2 System Scanner implementadas.

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
- Arquitetura de privilegio: UI sem privilegio + `BorealBoost.Agent` elevado por sessao, obrigatorio.

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

`App` depende de contratos de `Core`, casos de uso de `Analysis`, `Optimization`, `Restore`, `Drivers`, `Reporting` e comunicacao com `Agent`.

`Agent` executa operacoes privilegiadas chamando `System`, `Optimization`, `Restore`, `Drivers` e `Infrastructure`.

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

## Contrato arquitetural do BorealBoost.Agent

`BorealBoost.Agent` e requisito arquitetural da V1. O aplicativo inteiro elevado nao e fallback aceito. A UI deve permanecer sem privilegio permanente; toda operacao administrativa passa pelo Agent elevado e pelo ExecutionPlan validado.

### Trust boundary

A fronteira de confianca fica entre `BorealBoost.App` e `BorealBoost.Agent`.

- App: coleta intencao do tecnico, exibe dados, monta solicitacao e envia plano candidato.
- Agent: revalida catalogo, politica, identidade do cliente, compatibilidade, snapshots e ExecutionPlan antes de qualquer alteracao.
- UI nao e fonte confiavel para comandos, paths executaveis, scripts, valores fora do schema ou decisao de autorizacao.
- ProgramData nao e fonte confiavel por si so; arquivos carregados dali exigem schema, hash, assinatura e politica de versao.

### Lifecycle

1. App cria `sessionId`, `correlationId` inicial e nonce de bootstrap em memoria.
2. App solicita elevacao do binario instalado e conhecido `BorealBoost.Agent` usando mecanismo Windows apropriado de UAC.
3. Agent inicia elevado, cria named pipe local de sessao e aguarda handshake.
4. App conecta, executa handshake autenticado e negocia `protocolVersion`.
5. Agent aceita somente uma sessao ativa por instancia.
6. Agent processa requisicoes allowlisted, persiste journal transacional e retorna resultados estruturados.
7. Agent encerra apos commit/rollback, cancelamento confirmado ou idle timeout.
8. Se Agent cair, App marca a sessao como `Interrupted` e o recovery sera feito na proxima inicializacao antes de qualquer nova execucao.

### Inicio elevado pelo App

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
