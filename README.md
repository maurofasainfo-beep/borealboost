# BorealBoost

BorealBoost e uma aplicacao desktop Windows para diagnostico, otimizacao planejada, validacao e rollback seguro em computadores de clientes. O produto deve ser modular, auditavel e conservador por padrao.

## Estado Atual

Fase atual: **Fase 2 - System Scanner**.

As bases da Fase 1 foram preservadas e a Fase 2 adiciona scanner somente leitura:

- solution .NET;
- projetos principais;
- shell WinUI 3;
- sidebar, Dashboard e pagina Scanner funcional;
- DI, configuracao, logging e paths;
- status administrativo real;
- contratos fundamentais de dominio;
- foundation do `BorealBoost.Agent` com IPC local tipado por named pipe;
- scanner modular de OS, CPU, GPU, RAM, storage, motherboard/firmware, displays, network, devices, drivers, power, services, processes e startup;
- testes unitarios, integracao e system tests.

Ainda nao existem otimizacoes reais. O projeto nao altera Registry, Services, Power, DNS, Drivers, Windows Update, Defender, Firewall, VBS ou Memory Integrity.

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
- `BorealBoost.Agent`: processo de Agent foundation, sem handlers privilegiados.
- `BorealBoost.Core`: contratos, resultados, IDs fortes, protocolo App-Agent, scanner e tipos puros.
- `BorealBoost.Infrastructure`: paths, configuracao, application info, IPC foundation e logging JSONL.
- `BorealBoost.System`: adapters Windows read-only para Foundation e Scanner.
- `BorealBoost.Analysis`: orquestracao do System Scanner e snapshot em memoria; sem Recommendation Engine.
- `BorealBoost.Optimization`, `Restore`, `Benchmark`, `Drivers`, `Reporting`: fronteiras de modulo sem implementacao operacional nesta fase.
- `BorealBoost.Tests.*`: validacao de contratos, dependencias e fronteiras de seguranca.

## System Scanner

A pagina `Scanner` executa uma analise read-only do computador, com progresso por provider e cancelamento. O resultado gera `SystemSnapshot` em memoria contendo fatos detectados e resultados por provider.

Fontes usadas:

- WMI/CIM encapsulado via `System.Management`;
- APIs .NET (`DriveInfo`, `NetworkInterface`, `Process.GetProcesses`);
- Win32 read-only para monitores, firmware type e power status;
- Registry read-only apenas para dados de versao, Secure Boot, power scheme e nomes de startup.

O scanner nao usa PowerShell/cmd, nao executa benchmark, nao busca drivers na internet e nao altera o sistema.

## Agent

O `BorealBoost.Agent` nesta fase:

- aceita somente argumentos de bootstrap permitidos;
- valida `pipeName`, `sessionId`, `bootstrapNonce` e `protocolVersion`;
- abre um named pipe local de sessao com nome vinculado ao `sessionId` e token imprevisivel;
- valida handshake, nonce, protocolo, sessionId, requestId, sequenceNumber e tamanho maximo de mensagem;
- expoe status de foundation via IPC e encerra por `ShutdownRequest`;
- nao possui operacoes privilegiadas;
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

Analysis/Recommendation pertence a Fase 3. Optimization Engine operacional pertence a Fase 4. Safety/Rollback operacional pertence a Fase 5. Tweaks reais pertencem a Fase 6 ou posterior.
