# BorealBoost

BorealBoost e uma aplicacao desktop Windows para diagnostico, otimizacao planejada, validacao e rollback seguro em computadores de clientes. O produto deve ser modular, auditavel e conservador por padrao.

## Estado Atual

Fase atual: **Fase 5 - Optimization Catalog**.

As bases da Fase 1 foram preservadas, a Fase 2 adicionou scanner somente leitura, a Fase 3 adicionou analise/recomendacoes read-only, a Fase 4 adicionou o motor transacional seguro e a Fase 5 adiciona o primeiro catalogo real de otimizacoes:

- solution .NET;
- projetos principais;
- shell WinUI 3;
- sidebar, Dashboard e pagina Scanner funcional;
- DI, configuracao, logging e paths;
- status administrativo real;
- contratos fundamentais de dominio;
- foundation do `BorealBoost.Agent` com IPC local tipado por named pipe;
- scanner modular de OS, CPU, GPU, RAM, storage, motherboard/firmware, displays, network, devices, drivers, power, services, processes e startup;
- Analysis Engine modular baseado em `SystemSnapshot`;
- Recommendation model com risk, evidence, compatibility, expected impact e preset preview;
- pagina `Analise` com recomendacoes estruturadas sem apply;
- OptimizationDefinition e OperationSpec tipados;
- ExecutionPlan, PlanValidator, Dry Run e Preflight;
- OptimizationSession com state machine, journal persistido e recovery foundation;
- OperationSnapshot, verification e rollback por snapshot;
- catalogo built-in V1 versionado com 12 OptimizationDefinitions reais;
- PresetEngine deterministico para Basic, Medium, Advanced e Custom;
- RegistryValue handler allowlisted, validado pelo Agent contra catalogo canonico;
- paginas `Otimizacao` e `Restauracao` para Review Plan, Dry Run, execucao controlada, rollback e recovery;
- testes unitarios, integracao e system tests.

O Catalog V1 esta documentado em `OPTIMIZATION_CATALOG.md`. Ele inclui ajustes pequenos e reversiveis de Visual, Windows, Gaming e Privacy. O projeto nao altera Services, Power, DNS, Drivers, Windows Update, Defender, Firewall, VBS, Memory Integrity, BCD ou pagefile.

## Requisitos de Desenvolvimento

- Windows 10 22H2 x64/build 19045 ou Windows 11 x64.
- .NET SDK **10.0.400** ou superior compativel dentro da feature band permitida por `global.json`.
- Visual Studio 2022 ou Build Tools com suporte a projetos .NET/Windows.
- Acesso NuGet para restaurar pacotes.

## Verificar SDK

O repositorio fixa a feature band aprovada em `global.json`:

```powershell
dotnet --info
dotnet --list-sdks
```

Se `dotnet --list-sdks` nao listar `10.0.400` ou SDK compativel por `rollForward: latestFeature`, instale o SDK oficial do .NET 10 pela Microsoft antes de restaurar/buildar. Nao inclua SDK portatil ou binarios .NET no repositorio.

## Abrir a Solution

Abra:

```powershell
BorealBoost.sln
```

## Restore

```powershell
dotnet restore .\BorealBoost.sln
```

## Build

```powershell
dotnet build .\BorealBoost.sln
```

## Testes

```powershell
dotnet test .\BorealBoost.sln
```

## Arquitetura Resumida

Projetos da Foundation:

- `BorealBoost.App`: shell WinUI 3, navigation, views, viewmodels e tema base.
- `BorealBoost.Agent`: processo de Agent foundation, com IPC tipado e handlers allowlisted de Registry para prova controlada e catalogo V1.
- `BorealBoost.Core`: contratos, resultados, IDs fortes, protocolo App-Agent, scanner e tipos puros.
- `BorealBoost.Infrastructure`: paths, configuracao, application info, IPC foundation e logging JSONL.
- `BorealBoost.System`: adapters Windows read-only para Foundation e Scanner.
- `BorealBoost.Analysis`: orquestracao do System Scanner, Analysis Engine e snapshot/analysis em memoria.
- `BorealBoost.Optimization`: catalogo built-in V1, preset engine, planner, dry run, preflight, sessao transacional e recovery.
- `BorealBoost.Restore`: restore point policy modelada e rollback foundation.
- `BorealBoost.Benchmark`, `Drivers`, `Reporting`: fronteiras de modulo sem implementacao operacional nesta fase.
- `BorealBoost.Tests.*`: validacao de contratos, dependencias e fronteiras de seguranca.

## System Scanner

A pagina `Scanner` executa uma analise read-only do computador, com progresso por provider e cancelamento. O resultado gera `SystemSnapshot` em memoria contendo fatos detectados e resultados por provider.

Fontes usadas:

- WMI/CIM encapsulado via `System.Management`;
- APIs .NET (`DriveInfo`, `NetworkInterface`, `Process.GetProcesses`);
- Win32 read-only para monitores, firmware type e power status;
- Registry read-only apenas para dados de versao, Secure Boot, power scheme e nomes de startup.

O scanner nao usa PowerShell/cmd, nao executa benchmark, nao busca drivers na internet e nao altera o sistema.

## Analysis + Recommendation Engine

A pagina `Analise` interpreta o ultimo snapshot real do Scanner e mostra findings, oportunidades, avisos, bloqueios, unknowns e recomendacoes estruturadas.

Esta fase:

- nao escreve Registry;
- nao altera Services, Power, DNS ou Network;
- nao instala ou atualiza drivers;
- nao executa comandos;
- nao aplica otimizacoes;
- nao executa rollback;
- nao calcula Boreal Score operacional;
- nao promete ganhos de FPS.

## Optimization Catalog + Rollback

A pagina `Otimizacao` monta presets Basic/Medium/Advanced/Custom a partir do snapshot/analysis atuais, apresenta Review Plan e executa Dry Run sem modificar Windows. A execucao passa pelo pipeline seguro:

- snapshot antes da mutacao;
- journal persistido antes/depois de cada etapa;
- apply tipado;
- verification obrigatoria;
- rollback com estado original capturado;
- recovery de sessao incompleta.

Catalog V1:

- `schemaVersion = 5.1.0`, `catalogVersion = 5.1.0-built-in-v1`;
- 12 OptimizationDefinitions reais;
- 6 Safe, 5 Medium, 1 Advanced, 0 Aggressive/Experimental;
- 12 reversiveis por snapshot;
- 0 SecurityTradeoff;
- 0 reboot automatico.
- classificacao explicita separa performance/responsiveness de UX, privacidade e preferencias;
- Basic/Medium nao aplicam preferencias pessoais silenciosamente.

## Agent

O `BorealBoost.Agent` nesta fase:

- aceita somente argumentos de bootstrap permitidos;
- valida `pipeName`, `sessionId`, `bootstrapNonce` e `protocolVersion`;
- abre um named pipe local de sessao com nome vinculado ao `sessionId` e token imprevisivel;
- valida handshake, nonce, protocolo, sessionId, requestId, sequenceNumber e tamanho maximo de mensagem;
- expoe status, validacao de operacao, snapshot, execute, verify e rollback tipados via IPC;
- possui handlers allowlisted `BorealIntegrationRegistryValue` e `RegistryValue`;
- valida CatalogVersion, OptimizationId, OperationId, target e desired state contra o catalogo built-in canonico;
- nao aceita `ExecuteCommand`, `ExecutePowerShell`, `ExecuteProcess` ou equivalente.

## Logs e Dados Mutaveis

Na Foundation, logs locais sao gravados em dados de usuario, separados por papel/processo para evitar lock entre App e Agent:

```text
%LocalAppData%\BorealBoost\Logs
app-YYYYMMDD-PID.jsonl
agent-YYYYMMDD-PID.jsonl
```

Paths futuros para configuracao, sessoes, snapshots e relatorios ja estao centralizados, sem conteudo falso.

## Aviso de Escopo

Drivers, benchmark, resultados e reporting pertencem a Fase 6. Installer/hardening pertence a Fase 7.
