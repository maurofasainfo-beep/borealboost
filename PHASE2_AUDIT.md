# Executive Summary

Auditoria tecnica da Fase 2 - System Scanner executada em 2026-08-12 no workspace `C:\Users\Mauro\borealboost`.

Foram lidos os documentos obrigatorios, solution/projetos, arquivos em `src/` e `tests/`. A validacao real confirmou que o scanner executa em modo read-only, produz `SystemSnapshot`, passa build/test e nao introduz otimizacoes, drivers operacionais, Windows Update, Registry write, mutacao de services/power/DNS ou execucao arbitraria.

O scanner esta funcional e modular, mas nao deve avancar para a Fase 3 sem correcoes. Os principais riscos sao: cancelamento/timeout WMI nao interrompe necessariamente a chamada nativa subjacente; a UI ainda permite scans concorrentes por navegacao; VRAM de GPU e aceita diretamente de `Win32_VideoController.AdapterRAM`, uma fonte que nao deve ser tratada como fato confiavel sem normalizacao/confidence; e o bloco de capabilities/security da Fase 2 esta incompleto para Defender, Firewall, Memory Integrity, VBS, BitLocker e TPM.

# Verdict

APPROVED WITH CORRECTIONS

# Build Validation

Comandos executados:

| Comando | Resultado |
| --- | --- |
| `dotnet --info` | PASS. SDK 10.0.400 em `C:\Program Files\dotnet\sdk`; runtime .NET 10.0.11; OS 10.0.26200 x64. |
| `dotnet --list-sdks` | PASS. `10.0.400 [C:\Program Files\dotnet\sdk]`. |
| `dotnet restore .\BorealBoost.sln` | PASS. Todos os projetos atualizados para restauracao. |
| `dotnet build .\BorealBoost.sln --no-restore` | PASS. 0 warnings, 0 errors. |

Warnings relevantes: nenhum warning de restore/build/XAML/analyzers/NuGet foi emitido no build obrigatorio.

# Test Validation

Comando executado:

`dotnet test .\BorealBoost.sln --no-build --logger "console;verbosity=detailed"`

Resultado:

| Projeto | Testes | Resultado |
| --- | ---: | --- |
| `BorealBoost.Tests.Unit` | 50 | PASS |
| `BorealBoost.Tests.Integration` | 13 | PASS |
| `BorealBoost.Tests.System` | 17 | PASS |
| Total | 80 | PASS |

Cobertura real observada:

- Bom: normalizacao Windows/device/storage, Result/OperationResult, protocolo/Agent foundation, logging concorrente, grafo arquitetural, providers reais read-only, scan completo real, partial scan por provider failed, timeout e cancellation mockados.
- Insuficiente: VRAM/GPU confiavel, cenarios multi-GPU/notebook/VM, Windows 10 real, DPI real, privacy de processos/servicos, cancellation real de WMI travado, `NotSupported`, security capabilities, UI navigation concurrency.

# Architecture Findings

Grafo real de dependencias de produto:

- `BorealBoost.Core -> (none)`
- `BorealBoost.System -> BorealBoost.Core`
- `BorealBoost.Analysis -> BorealBoost.Core`
- `BorealBoost.Infrastructure -> BorealBoost.Core`
- `BorealBoost.Agent -> BorealBoost.Core, BorealBoost.Infrastructure`
- `BorealBoost.App -> BorealBoost.Analysis, BorealBoost.Core, BorealBoost.Infrastructure, BorealBoost.System`
- `BorealBoost.Optimization -> BorealBoost.Core`
- `BorealBoost.Restore -> BorealBoost.Core`
- `BorealBoost.Benchmark -> BorealBoost.Core`
- `BorealBoost.Drivers -> BorealBoost.Core`
- `BorealBoost.Reporting -> BorealBoost.Core`

Grafo real de testes:

- `Unit -> Analysis, Core, Infrastructure, System, Agent`
- `Integration -> Core, System`
- `System -> Analysis, Core, Infrastructure, System`

Conclusao arquitetural:

- `Core` permanece puro e nao conhece Infrastructure/System/UI.
- `System` encapsula WMI, Registry read-only, Win32, .NET BCL e process/network/drive APIs.
- `Analysis` orquestra o scanner sem depender de `System`; os providers chegam por DI.
- `App` apresenta resultados e registra providers concretos no composition root. ViewModels nao conhecem WMI/Registry/Win32 diretamente.
- Nao ha dependencia circular.
- Nao ha vazamento de WMI para o dominio: o dominio usa snapshots e `DataSourceKind`.

Observacao: o App conhecer `BorealBoost.System` no composition root e coerente com a implementacao atual, mas deve permanecer limitado ao registro DI. Views/ViewModels nao devem passar a instanciar providers Windows diretamente.

# Read-Only Safety

Busca global executada por APIs/termos proibidos:

- `Registry.SetValue`
- `CreateSubKey`
- `DeleteSubKey`
- `DeleteValue`
- `RegistryKey writable`
- `ServiceController.Start/Stop/Pause`
- `powercfg`
- `netsh`
- `Set-DnsClientServerAddress`
- `DISM`
- `SFC`
- `PnPUtil`
- `SetupAPI mutation`
- `Windows Update mutation`
- `AppX mutation`
- `Process.Start`
- `cmd.exe`
- `powershell.exe`
- `pwsh.exe`
- `ExecuteCommand`
- `ExecutePowerShell`
- `ExecuteProcess`

Classificacao das ocorrencias em `src/`:

| Ocorrencia | Arquivo | Classificacao |
| --- | --- | --- |
| `Process.Start` | `src/BorealBoost.App/Agent/AgentBootstrapService.cs:159` | Permitida; bootstrap interno conhecido do Agent Foundation, sem payload do Scanner/UI. |
| `OpenSubKey(..., writable: false)` | `src/BorealBoost.System/Registry/ReadOnlyRegistryReader.cs:15`, `:34` | Permitida; leitura HKLM. |
| `OpenSubKey(..., writable: false)` | `src/BorealBoost.System/Scanner/StartupScanProvider.cs:57` | Permitida; leitura HKCU/HKLM Run/RunOnce. |

Nao foram encontradas mutacoes do Windows no Scanner. Nenhuma chamada operacional de Registry write, service mutation, power mutation, DNS, drivers, Windows Update, DISM/SFC, AppX, PowerShell/cmd ou shell execution foi encontrada em `src/`.

# SystemSnapshot Findings

Pontos positivos:

- `SystemSnapshot` e `ScanMetadata` sao records imutaveis em `Core`.
- Ha `ScanId`, `StartedAtUtc`, `CompletedAtUtc`, `Duration`, `SchemaVersion`, `MachineArchitecture`, `ProviderResults`, `PartialScan`, warnings e errors.
- Timestamps sao UTC.
- `Unknown`/`null`/listas vazias sao usados em varias areas para dados nao detectados.
- `PartialScan` e calculado quando provider retorna `Partial`, `Failed`, `TimedOut` ou `Canceled`.

Riscos:

- Alguns campos numericos nao anulaveis podem representar ausencia como `0`, especialmente `MemorySnapshot.ModuleCount` em `src/BorealBoost.Core/Scanner/SystemSnapshot.cs:85`.
- `CpuSnapshot.LogicalProcessors` e nao anulavel, mas o provider usa fallback de `Environment.ProcessorCount`; isso e um fato real do processo, mas precisa ficar claro como fonte/fallback.
- `MemorySnapshot.TotalPhysicalBytes` representa memoria fisica visivel ao OS (`Win32_ComputerSystem.TotalPhysicalMemory`) e nao soma instalada dos DIMMs; o modelo nao diferencia explicitamente OS-visible vs installed-module total.

# OS Findings

Implementacao:

- WMI: `Win32_OperatingSystem` com `Caption`, `Version`, `BuildNumber`, `OSArchitecture`.
- Registry read-only: `UBR`, `DisplayVersion`, `ReleaseId`, `EditionID`, `ProductName`.
- Classificacao: `WindowsCompatibilityClassifier`.

Validacao:

- Real scan: `Microsoft Windows 11 Pro build=26200`.
- Unit test cobre Windows 10 build 19045 como `LegacySupported`.
- Unit test cobre Windows 11 build 26200 como `Supported`.

Pontos positivos:

- Nao depende exclusivamente de `Environment.OSVersion`.
- Preserva Windows 10 22H2/build 19045 como target legado funcional.
- Diferencia `Supported`, `LegacySupported`, `Unsupported`, `Unknown`.

Limites:

- Windows 10 22H2 x64/build 19045 nao foi validado em VM real nesta auditoria.
- Windows 11 23H2/24H2 e notebooks nao foram validados nesta maquina.

# CPU Findings

Implementacao:

- WMI: `Win32_Processor` com `Manufacturer`, `Name`, `NumberOfLogicalProcessors`, `NumberOfCores`, `SocketDesignation`, clocks, `ProcessorId`, `Family`, `VirtualizationFirmwareEnabled`.

Pontos positivos:

- Vendor Intel/AMD/NVIDIA/Microsoft/etc. e classificado por helper central.
- Valores ausentes em cores/clocks ficam `null`.
- Fallback para CPU usa `Environment.ProcessorCount` quando WMI nao retorna linhas.
- Nao ha parsing fragil de geracao comercial da CPU.

Limites:

- `Sockets` e inferido como `1` por linha quando `SocketDesignation` existe; isso nao e um contador global de sockets e deve ser documentado antes de analises futuras.

# GPU Findings

Implementacao:

- WMI: `Win32_VideoController` com `Name`, `AdapterCompatibility`, `PNPDeviceID`, `DeviceID`, `DriverVersion`, `DriverDate`, `AdapterRAM`, `Status`.
- Real scan: 1 GPU; WMI reportou `NVIDIA GeForce RTX 3050`, `AdapterRAM=4293918720`.

Pontos positivos:

- Enumera todas as linhas de `Win32_VideoController`; nao assume GPU unica.
- Nao classifica integrada/dedicada por heuristica fraca; deixa `Unknown` exceto virtual/basic/remote.
- Coleta driver version/date e PNP Device ID.
- Nao infere driver desatualizado.

Problema principal:

- `AdapterRAM` e aceito diretamente como `AdapterRamBytes` em `src/BorealBoost.System/Scanner/GraphicsScanProvider.cs:46`. Para GPUs modernas, esse campo WMI e frequentemente limitado/inconfiavel; a Fase 2 exige "Unknown e preferivel a informacao inventada". O valor deve ter confidence/fonte clara ou ser `Unknown` quando nao houver validacao confiavel.

# Memory Findings

Implementacao:

- WMI: `Win32_ComputerSystem.TotalPhysicalMemory`.
- WMI: `Win32_PhysicalMemory.Capacity`, `Manufacturer`, `PartNumber`, `ConfiguredClockSpeed`, `Speed`.
- Real scan: `RamBytes=17099431936`; modulos WMI: 2x 8 GiB, `TEAMGROUP-UD4-3000`.

Pontos positivos:

- Serial number nao e consultado.
- Module capacity/speed/part number sao coletados por modulo.
- UI converte para unidade amigavel em GiB-like decimal de bytes/1024.

Riscos:

- A UI exibe `TotalPhysicalMemory` como "RAM", mas esse valor e memoria fisica visivel ao OS, nao soma instalada dos DIMMs. Isso pode explicar o valor real auditado `17,099,431,936` bytes contra 16 GiB instalados com reserva de hardware/OS.
- `PartNumber` pode ser identificador de componente; nao e serial, mas deve ser tratado como detalhe tecnico e nao ir para relatorio publico por padrao.

# Storage Findings

Implementacao:

- Preferencia por `MSFT_PhysicalDisk` em `root\Microsoft\Windows\Storage` com `FriendlyName`, `Manufacturer`, `Size`, `MediaType`, `BusType`, `HealthStatus`.
- Fallback para `Win32_DiskDrive`.
- Volumes via `DriveInfo.GetDrives()`.
- Real scan: 2 discos; 1 HDD SATA e 1 SSD SATA; 1+ volumes.

Pontos positivos:

- Nao executa benchmark.
- Discos fisicos e volumes sao modelos separados, evitando duplicacao conceitual.
- `StorageMediaClassifier` usa `MediaType` e `BusType`; nao depende apenas de nome contendo SSD/NVMe.
- `NVMe` prioriza BusType 17.

Limites:

- Fallback `Win32_DiskDrive` marca `MediaKind.Unknown`, conservador.
- Nao ha associacao disco-volume; isso e aceitavel para Fase 2, mas limita analise futura.

# Firmware Findings

Implementacao:

- WMI: `Win32_ComputerSystem`, `Win32_BaseBoard`, `Win32_BIOS`, `Win32_SystemEnclosure`.
- Win32: `GetFirmwareType`.
- Registry read-only: Secure Boot state.

Pontos positivos:

- Nao coleta serial de motherboard/BIOS.
- Firmware type e Secure Boot sao read-only.
- Desktop/laptop/tablet/convertible usa chassis e `PCSystemTypeEx`, nao apenas bateria.

Limites:

- Query inclui `HypervisorPresent`, mas o valor nao e usado. Isso evita classificar PC fisico com Hyper-V/VBS como VM, mas tambem deixa VM detection dependente de strings de fabricante/modelo.

# Device/Driver Findings

Implementacao:

- Devices: `Win32_PnPEntity` com `DeviceID`, `HardwareID`, `CompatibleID`, `ConfigManagerErrorCode`, status e class.
- Drivers: `Win32_PnPSignedDriver` com provider, version, date, INF, signer e signed flag.
- Health merge por `DeviceInstanceId`.
- Real scan: `ProblemDeviceCount=2`, ambos baseados em `ConfigManagerErrorCode=22` em leitura WMI.

Pontos positivos:

- "Dispositivo com problema" e baseado em evidencia objetiva: problem code/status.
- Missing driver usa code 28.
- Driver desatualizado nao e inferido.
- Nao ha busca externa, download, install, update ou removal de driver.

Limites:

- `Device Instance ID`, `Hardware IDs` e `Compatible IDs` sao dados tecnicos potencialmente fingerprinting. A coleta tem justificativa para Driver Engine futuro, mas esses dados nao devem ser logados/expostos por padrao.
- SetupAPI/CfgMgr32 ainda nao existe; isso e esperado para Driver Engine futuro, mas WMI nao deve ser usado sozinho para decisoes criticas futuras.

# Display Findings

Implementacao:

- Win32: `EnumDisplayDevices` e `EnumDisplaySettings`.
- Campos: device name, friendly name, width, height, refresh rate, primary.

Pontos positivos:

- Nao usa fallback silencioso de 60 Hz; refresh desconhecido fica `null`.
- Ignora dispositivos desconectados/inativos.
- Suporta enumeracao de multiplos monitores.

Limites:

- DPI nao e exposto no snapshot, embora `DEVMODE.LogPixels` exista no struct em `src/BorealBoost.System/Scanner/DisplayScanProvider.cs:124`.
- Validacao real nesta maquina teve 1 display; multiplos monitores e DPI 125/150/200 nao foram validados.

# Network Findings

Implementacao:

- .NET `NetworkInterface.GetAllNetworkInterfaces()`.
- Coleta name, description, kind, operational status, speed e virtual heuristic.

Pontos positivos:

- Nao coleta MAC address.
- Nao coleta SSID.
- Nao coleta IP publico ou IP local.
- Nao altera DNS/rede.

Riscos:

- Nome/descricao de adaptador pode revelar VPN, empresa, produto de seguranca ou contexto do cliente. Como snapshot ainda e em memoria, risco atual e moderado; antes de persistir/relatar, aplicar politica de minimizacao/mascara.

# Services/Processes/Startup Findings

Services:

- WMI `Win32_Service` com `Name`, `DisplayName`, `State`, `StartMode`, `ServiceType`, `Started`.
- Read-only e util como fato para fases futuras.

Processes:

- `.NET Process.GetProcesses()` com PID, process name e working set.
- Nao coleta command line/path/user/session.
- Nao finaliza nem altera processo.

Startup:

- Registry read-only HKCU/HKLM `Run` e `RunOnce`, Registry64.
- Coleta nome e source location; nao coleta command path.

Riscos:

- Inventariar todos os processos por nome/PID/working set amplia exposicao de informacao. Para snapshot persistente/relatorio, considerar agregados/top N/categorias em vez de lista completa.
- Startup cobre apenas Run/RunOnce Registry64. Nao cobre Startup folders, Scheduled Tasks, `StartupApproved`, Registry32/Wow6432Node ou impactos de startup.

# WMI Findings

Adapter WMI:

- `WmiQueryService` encapsula WMI.
- Usa `EnumerationOptions.Timeout`.
- Usa `ManagementObjectSearcher`, `ManagementScope`, `ObjectQuery`.
- Usa projection explicita; nao ha `SELECT *`.
- `searcher` e `collection` sao disposed.

Queries encontradas:

| Provider | Namespace | Classe | Campos | Timeout |
| --- | --- | --- | --- | --- |
| OS | `root\cimv2` | `Win32_OperatingSystem` | `Caption`, `Version`, `BuildNumber`, `OSArchitecture` | 8s |
| CPU | `root\cimv2` | `Win32_Processor` | manufacturer/name/logical/core/socket/clocks/id/family/virtualization | 8s |
| GPU | `root\cimv2` | `Win32_VideoController` | name/vendor/pnp/device/driver/AdapterRAM/status | 8s |
| Memory | `root\cimv2` | `Win32_ComputerSystem` | `TotalPhysicalMemory` | 8s |
| Memory | `root\cimv2` | `Win32_PhysicalMemory` | capacity/manufacturer/part number/speeds | 8s |
| Storage | `root\Microsoft\Windows\Storage` | `MSFT_PhysicalDisk` | friendly/manufacturer/size/media/bus/health | 6s |
| Storage fallback | `root\cimv2` | `Win32_DiskDrive` | model/manufacturer/size/media/interface/status | 10s |
| Hardware | `root\cimv2` | `Win32_ComputerSystem` | manufacturer/model/PCSystemTypeEx/HypervisorPresent | 8s |
| Motherboard | `root\cimv2` | `Win32_BaseBoard` | manufacturer/product/version | 8s |
| BIOS | `root\cimv2` | `Win32_BIOS` | manufacturer/version/release date | 8s |
| Chassis | `root\cimv2` | `Win32_SystemEnclosure` | chassis types | 8s |
| Devices | `root\cimv2` | `Win32_PnPEntity` | name/id/hardware/compatible/class/status/problem code | 12s |
| Drivers | `root\cimv2` | `Win32_PnPSignedDriver` | device/provider/version/date/inf/signer/signed | 12s |
| Services | `root\cimv2` | `Win32_Service` | name/display/state/start mode/type/started | 10s |

Problemas:

- WMI e executado via `Task.Run` e `WaitAsync` em `src/BorealBoost.System/Wmi/WmiQueryService.cs:18` e `:39`. Se a chamada WMI subjacente travar, o timeout pode devolver controle ao orquestrador enquanto a operacao COM continua em threadpool.
- Itens `ManagementBaseObject` do foreach nao sao explicitamente disposed por item em `src/BorealBoost.System/Wmi/WmiQueryService.cs:32`.

# Registry Findings

Caminhos consultados:

| Hive/View | Caminho | Valores | Uso |
| --- | --- | --- | --- |
| HKLM Registry64 | `SOFTWARE\Microsoft\Windows NT\CurrentVersion` | `UBR`, `DisplayVersion`, `ReleaseId`, `EditionID`, `ProductName` | OS metadata |
| HKLM Registry64 | `SYSTEM\CurrentControlSet\Control\SecureBoot\State` | `UEFISecureBootEnabled` | Secure Boot |
| HKLM Registry64 | `SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes` | `ActivePowerScheme` | Power |
| HKCU Registry64 | `SOFTWARE\Microsoft\Windows\CurrentVersion\Run` | value names only | Startup |
| HKCU Registry64 | `SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce` | value names only | Startup |
| HKLM Registry64 | `SOFTWARE\Microsoft\Windows\CurrentVersion\Run` | value names only | Startup |
| HKLM Registry64 | `SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce` | value names only | Startup |

Confirmado:

- `OpenSubKey(..., writable: false)` em todos os usos.
- Nenhuma criacao, escrita ou exclusao.
- Acesso negado/chave inexistente retornam null ou warning parcial.

Limite:

- Startup nao consulta Registry32/Wow6432Node, Startup folders ou Scheduled Tasks.

# Privacy Findings

Confirmado ausente no scanner/snapshot:

- username;
- email;
- IP publico;
- MAC;
- SSID;
- Windows product key;
- MachineGuid;
- motherboard serial;
- BIOS serial;
- RAM serial;
- tokens/secrets.

Dados sensiveis tecnicos presentes por necessidade futura:

- Device Instance ID;
- Hardware IDs;
- Compatible IDs;
- PNP Device ID;
- RAM part number;
- process names/PIDs/working set;
- network adapter name/description.

Riscos:

- `Environment.MachineName` e exibido no Dashboard via `BasicSystemInfoProvider`/`DashboardViewModel`; nao e snapshot do scanner, mas pode identificar cliente.
- Antes de persistir snapshots em ProgramData ou gerar relatorios, definir politica de redaction para IDs de hardware, processos, adaptadores e machine name.
- Logs do scanner nao despejam snapshot inteiro; registram ScanId, provider, status, duracao e contagens.

# Timeout/Cancellation Findings

Pontos positivos:

- Orquestrador cria timeout por provider.
- `TimedOut` gera scan parcial.
- Cancelamento do usuario retorna failure `scanner.canceled` e nao salva snapshot concluido.
- Providers simples chamam `ThrowIfCancellationRequested`.

Problemas:

- WMI cancellation nao e cooperativo de ponta a ponta. O wrapper usa `Task.Run` + `WaitAsync`; a thread WMI pode continuar depois de timeout.
- Teste de timeout usa provider mock que ignora cancellation; ele valida o orquestrador, nao uma chamada WMI real travada.
- UI nao cancela automaticamente scan ao sair da pagina.

# Parallelism Findings

Implementacao:

- Providers sao executados sequencialmente em `SystemScanner`.
- Isso evita saturacao de WMI e reduz risco de race em Fase 2.

Conclusao:

- Paralelismo e controlado por ausencia de paralelismo entre providers.
- Nao ha shared mutable state perigoso entre providers.
- Store em memoria usa lock.

# Progress Findings

Implementacao:

- Progresso ponderado por `Weight`.
- Etapas reais por provider.
- Nao usa timer ficticio.
- Percentual e clampado 0..100.

Validacao:

- Unit test confirma 0 e 100 e faixa 0..100.
- System test confirma update final 100 em scan concluido.

Limites:

- Nao ha teste de monotonicidade estrita.
- Cancelamento parcial de UI durante provider lento nao foi validado em runtime real.
- `NotSupported` nao tem caminho real/testado.

# UI Findings

Pontos positivos:

- `ScannerPage` funcional.
- Start/cancel existem.
- `CanStartScan` bloqueia duplo clique na mesma instancia.
- ViewModel consome `ISystemScanner` e `ISystemSnapshotStore`, nao WMI/Registry.
- Dashboard usa snapshot real quando existe e nao inventa Boreal Score, FPS, oportunidades ou recomendacoes.

Smoke UI:

- Janela encontrada: YES.
- Navegacao Scanner: YES.
- Scan iniciado: YES.
- Texto "Analise concluida." encontrado: YES.
- Sem processo residual `BorealBoost.App`/`BorealBoost.Agent`.
- Observacao: o harness PowerShell retornou exit code 1 apesar de imprimir `UI_RUNTIME_STATUS=PASS`; isso foi causado pela verificacao final de processo sem resultado, nao por crash observado do App.

Problemas:

- `ScannerViewModel` e `ScannerPage` sao transients em `src/BorealBoost.App/App.xaml.cs:95` e `:99`. Ao navegar para fora e voltar durante um scan, uma nova instancia pode permitir outro scan, violando o contrato de nao permitir scans concorrentes.
- `ScannerPage` nao cancela scan em `Unloaded` nem tem lifetime de scan centralizado.
- Dashboard chama `ProbeAgentAsync(CancellationToken.None)` em `src/BorealBoost.App/Pages/DashboardPage.xaml.cs:24` mesmo com `EnableAgentHandshakeProbe=false` em `appsettings.json`. Isso nao e destrutivo, mas a configuracao fica enganosa e o Agent e iniciado desnecessariamente no Dashboard.

# Performance Findings

Scan real obrigatorio:

- Windows: Microsoft Windows 11 Pro build 26200.
- CPU: AMD Ryzen 5 5600 6-Core Processor.
- GPU count: 1.
- RAM: 17,099,431,936 bytes.
- Disk count: 2.
- Display count: 1.
- Problem device count: 2.
- Providers: Success 14, Partial 0, Failed 0, TimedOut 0, NotSupported 0.

Cinco scans reais:

| Run | Duration | ProviderSuccess | Slow providers |
| ---: | ---: | ---: | --- |
| 1 | 1,528 ms | 14 | Drivers 979 ms; Services 193 ms; Devices 174 ms |
| 2 | 1,445 ms | 14 | Drivers 912 ms; Services 203 ms; Devices 158 ms |
| 3 | 1,369 ms | 14 | Drivers 845 ms; Services 200 ms; Devices 154 ms |
| 4 | 1,339 ms | 14 | Drivers 831 ms; Services 186 ms; Devices 153 ms |
| 5 | 1,389 ms | 14 | Drivers 878 ms; Services 194 ms; Devices 148 ms |

Resumo: min 1,339 ms; media 1,414 ms; max 1,528 ms.

Nao foi observada degradacao progressiva de duracao. Handle count/memoria do processo nao foram medidos diretamente.

# Dependency Findings

Pacotes em produto:

| Pacote | Versao | Projeto(s) | Motivo | Licenca documentada |
| --- | --- | --- | --- | --- |
| `Microsoft.Extensions.Hosting` | 10.0.11 | App, Agent | Host/DI/config/logging | MIT |
| `Microsoft.Extensions.Logging` | 10.0.11 | Analysis, Infrastructure | Logging abstractions | MIT |
| `Microsoft.Extensions.Configuration.Abstractions` | 10.0.11 | Infrastructure | Configuration abstractions | MIT |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.11 | Infrastructure | DI abstractions | MIT |
| `Microsoft.WindowsAppSDK` | 2.3.1 | App | WinUI 3 | Microsoft Software License Terms |
| `System.Management` | 10.0.11 | System | WMI/CIM read-only | MIT |

Pacotes de teste:

| Pacote | Versao | Projeto(s) | Motivo |
| --- | --- | --- | --- |
| `coverlet.collector` | 6.0.4 | Tests | Cobertura futura/test infra |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Tests | Test runner |
| `xunit` | 2.9.3 | Tests | Test framework |
| `xunit.runner.visualstudio` | 3.1.4 | Tests | VSTest integration |

Comandos:

- `dotnet list .\BorealBoost.sln package --vulnerable`: PASS, nenhum pacote vulneravel nas fontes atuais.
- `dotnet list .\BorealBoost.sln package --outdated`: PASS, updates somente em pacotes de teste:
  - `coverlet.collector` 6.0.4 -> 10.0.1
  - `Microsoft.NET.Test.Sdk` 17.14.1 -> 18.8.1
  - `xunit.runner.visualstudio` 3.1.4 -> 3.1.5

# Phase Boundary Findings

Confirmado que nao foi implementado em `src/`:

- Recommendation Engine operacional;
- Optimization Engine operacional;
- Boreal Score;
- benchmark;
- driver download;
- driver install/update/remove;
- Windows Update;
- tweaks;
- Registry write;
- Service mutation;
- power mutation;
- DNS mutation;
- PowerShell/cmd/pwsh arbitrario.

`AnalysisModule.RecommendationEngineIsOperational=false`. Modulos `Optimization`, `Restore`, `Drivers`, `Benchmark` e `Reporting` seguem como fronteiras sem comportamento operacional.

# Findings Table

| ID | Severity | Arquivo/regiao | Descricao | Evidencia | Impacto | Correcao recomendada |
| --- | --- | --- | --- | --- | --- | --- |
| BB-P2-HIGH-001 | HIGH | `src/BorealBoost.System/Wmi/WmiQueryService.cs:18`, `:39` | Timeout/cancellation WMI e aplicado por `Task.Run`/`WaitAsync`, nao por cancelamento real da chamada WMI subjacente. | `ManagementObjectSearcher.Get()` roda dentro de `Task.Run`; timeout pode abandonar a tarefa. | Scan pode parecer recuperado enquanto uma query WMI continua consumindo thread/COM; risco cresce em maquinas com WMI travado. | Implementar adapter WMI com cancelamento/timeout mais robusto, isolamento de operacao e testes de WMI lento/travado; evitar tarefas abandonadas. |
| BB-P2-HIGH-002 | HIGH | `src/BorealBoost.App/App.xaml.cs:95`, `:99`; `src/BorealBoost.App/ViewModels/ScannerViewModel.cs:170` | UI bloqueia scan concorrente apenas por instancia de ViewModel; navegar para fora/voltar cria nova instancia e permite novo scan enquanto o anterior ainda roda. | `ScannerViewModel` e `ScannerPage` sao transients; nao ha scan session guard central nem cancelamento em `Unloaded`. | Corrida de snapshots, progresso invisivel e multiplas consultas WMI simultaneas pela UI. | Centralizar estado de scan em service singleton ou cancelar/impedir navegacao durante scan; adicionar teste de UI/lifetime. |
| BB-P2-HIGH-003 | HIGH | `src/BorealBoost.System/Scanner/GraphicsScanProvider.cs:29`, `:46` | VRAM e aceita diretamente de `Win32_VideoController.AdapterRAM`. | Real scan retornou `AdapterRAM=4293918720`; provider grava esse valor sem confidence/validacao. | Pode reportar VRAM truncada/incorreta e contaminar analise/recomendacoes futuras. | Tratar AdapterRAM como low-confidence; validar via fonte melhor ou retornar `Unknown` quando nao confiavel. |
| BB-P2-HIGH-004 | HIGH | `src/BorealBoost.Analysis/SystemScanner/SystemScanner.cs:201-206`; providers ausentes | Capabilities/security da Fase 2 nao cobrem Defender, Firewall, Memory Integrity, VBS, BitLocker ou TPM. | `BuildCapabilities` gera apenas SecureBoot, Battery, MultipleGpus, MultipleDisplays, VirtualizationAvailable e VirtualMachine. | Fase 3 pode analisar sistema sem fatos de seguranca exigidos pelo Master Spec. | Criar providers read-only de security capabilities ou documentar formalmente exclusao/adiamento antes da Fase 3. |
| BB-P2-MED-001 | MEDIUM | `src/BorealBoost.System/Scanner/ProcessesScanProvider.cs:23-36` | Scanner coleta todos os processos com PID/nome/working set. | `Process.GetProcesses()` materializa lista completa. | Aumenta exposicao de informacao e custo; snapshot persistente futuro pode virar inventario sensivel. | Reduzir para agregados/top N ou aplicar politica de privacidade antes de persistencia/relatorio. |
| BB-P2-MED-002 | MEDIUM | `src/BorealBoost.Core/Scanner/SystemSnapshot.cs:84`; `src/BorealBoost.System/Scanner/MemoryScanProvider.cs:29`, `:45` | Modelo/UI nao diferenciam memoria fisica visivel pelo OS e soma instalada dos DIMMs. | `TotalPhysicalBytes` vem de `Win32_ComputerSystem.TotalPhysicalMemory`; modulos tem capacidades separadas. | Relatorio pode confundir RAM instalada com RAM utilizavel/visivel. | Separar campos `OsVisiblePhysicalBytes` e `InstalledModuleBytes` ou renomear/formatar claramente. |
| BB-P2-MED-003 | MEDIUM | `src/BorealBoost.System/Wmi/WmiQueryService.cs:32` | `ManagementBaseObject` por item nao e explicitamente disposed. | `foreach (ManagementBaseObject item in collection)` chama `WmiRow.From(item)` sem `using`. | Possivel acumulacao de recursos COM/WMI em uso repetido ou WMI instavel. | Dispor cada item quando aplicavel e testar repeticao com contagem de handles. |
| BB-P2-MED-004 | MEDIUM | `src/BorealBoost.System/Scanner/StartupScanProvider.cs:8`, `:57` | Startup inventory cobre apenas HKCU/HKLM Run/RunOnce em Registry64. | Nao ha Startup folders, Scheduled Tasks, StartupApproved ou Registry32. | Inventario de startup incompleto para diagnostico real. | Expandir fontes read-only ou marcar escopo atual como parcial na UI/provider result. |
| BB-P2-MED-005 | MEDIUM | `src/BorealBoost.System/Scanner/DisplayScanProvider.cs:124`; `src/BorealBoost.Core/Scanner/SystemSnapshot.cs:170-176` | DPI nao e exposto no `DisplaySnapshot`. | `DEVMODE.LogPixels` existe no struct, mas `DisplaySnapshot` nao tem DPI. | UX/relatorio nao consegue validar DPI/monitor de forma completa. | Adicionar DPI quando confiavel ou registrar `Unknown`; validar 125/150/200%. |
| BB-P2-MED-006 | MEDIUM | `src/BorealBoost.App/appsettings.json:5`; `src/BorealBoost.App/Pages/DashboardPage.xaml.cs:24` | `EnableAgentHandshakeProbe=false` nao e respeitado; Dashboard sempre chama probe do Agent com `CancellationToken.None`. | Config existe e e validada, mas nao e consumida no Dashboard. | Configuracao enganosa, Agent desnecessario no Dashboard, cancellation/lifetime fraco. | Respeitar `ApplicationSettings.EnableAgentHandshakeProbe` e usar token cancelavel/lifetime-aware. |
| BB-P2-MED-007 | MEDIUM | `src/BorealBoost.Core/Scanner/ScanPrimitives.cs:25`; providers/testes | `ProviderResultStatus.NotSupported` existe, mas nenhum provider/teste exercita esse estado. | Busca encontrou enum, mas nenhum factory/caminho real. | APIs indisponiveis podem virar Failed em vez de NotSupported, reduzindo semantica de partial scan. | Adicionar factory/casos NotSupported e testes negativos por provider. |
| BB-P2-MED-008 | MEDIUM | `src/BorealBoost.System/Scanner/NetworkScanProvider.cs:23-32`; `src/BorealBoost.App/ViewModels/DashboardViewModel.cs:31` | Nomes/descricoes de adaptadores e machine name podem identificar ambiente/cliente. | Coleta `adapter.Name`, `adapter.Description`; Dashboard exibe `Environment.MachineName`. | Risco de privacidade quando snapshots forem persistidos ou relatorios exportados. | Definir redaction/retencao e separar dados tecnicos internos de relatorio publico. |
| BB-P2-LOW-001 | LOW | `src/BorealBoost.System/Scanner/HardwareFirmwareScanProvider.cs:35`, `:61-64` | Query coleta `HypervisorPresent`, mas o valor nao e usado na classificacao. | VM detection depende de manufacturer/model strings. | Pode perder algumas VMs ou deixar evidencia inutil. | Incorporar como evidencia secundaria sem classificar fisico com Hyper-V/VBS como VM por si so. |
| BB-P2-LOW-002 | LOW | `src/BorealBoost.System/Scanner/ServicesScanProvider.cs:29` | Inventario de services coleta todos os services. | WMI `Win32_Service` sem filtro. | Dados amplos e custo moderado, embora read-only. | Considerar filtros/categorias ou relatorio agregado antes de persistir. |
| BB-P2-LOW-003 | LOW | `.csproj` descriptions de modulos | Algumas descricoes ainda dizem "Phase 1" em projetos futuros/Analysis. | `BorealBoost.Analysis.csproj` e modulos futuros mantem descricao de Foundation/Phase 1. | Documentacao de metadata incoerente; sem impacto runtime. | Atualizar metadata em correcao documental futura. |

# Blockers

Nenhum BLOCKER encontrado.

# High Priority

1. `BB-P2-HIGH-001`: tornar WMI timeout/cancellation efetivo e nao deixar tarefas abandonadas.
2. `BB-P2-HIGH-002`: impedir scans concorrentes via navegacao/lifetime da UI.
3. `BB-P2-HIGH-003`: corrigir tratamento de VRAM de GPU para nao confiar cegamente em `AdapterRAM`.
4. `BB-P2-HIGH-004`: completar ou formalmente replanejar security capabilities da Fase 2.

# Medium Priority

1. `BB-P2-MED-001`: reduzir/justificar inventario completo de processos antes de persistir.
2. `BB-P2-MED-002`: separar memoria OS-visible de memoria instalada.
3. `BB-P2-MED-003`: revisar disposal de objetos WMI por linha.
4. `BB-P2-MED-004`: ampliar ou marcar como parcial o inventario de startup.
5. `BB-P2-MED-005`: expor DPI quando confiavel ou `Unknown`.
6. `BB-P2-MED-006`: respeitar `EnableAgentHandshakeProbe` e cancellation do probe.
7. `BB-P2-MED-007`: implementar/testar `NotSupported`.
8. `BB-P2-MED-008`: definir redaction para machine name/adapters/processos/IDs tecnicos.

# Low Priority

1. `BB-P2-LOW-001`: usar `HypervisorPresent` como evidencia secundaria documentada.
2. `BB-P2-LOW-002`: avaliar filtro/agregacao de services.
3. `BB-P2-LOW-003`: ajustar descricoes de projeto que ainda dizem Phase 1.

# Unvalidated Items

- Windows 10 22H2 x64/build 19045 real/VM.
- Windows 11 23H2/24H2 separados.
- Notebook Intel/AMD.
- Multi-GPU Intel+NVIDIA, AMD iGPU+dGPU, Intel+AMD.
- VM Hyper-V/VMware/VirtualBox real.
- Multiplos monitores.
- DPI 125%, 150%, 200%.
- Cancellation de WMI realmente travado.
- Handle/memory growth medido por contadores entre muitos scans.
- Startup completo fora de Run/RunOnce.
- Security capabilities Defender/Firewall/Memory Integrity/VBS/BitLocker/TPM.
- UI navegando durante scan e tentando iniciar segundo scan.

# Required Corrections Before Phase 3

1. Corrigir timeout/cancellation WMI para nao abandonar chamadas nativas.
2. Garantir single-flight scanner global ou cancelamento seguro por lifetime de pagina/navegacao.
3. Corrigir VRAM de GPU: fonte confiavel, confidence explicita ou `Unknown`.
4. Implementar ou documentar ADR de adiamento para security capabilities exigidas pelo scanner.
5. Separar memoria visivel pelo OS de memoria instalada dos modulos.
6. Definir politica de privacidade/redaction antes de qualquer persistencia/relatorio do snapshot.
7. Adicionar testes para UI scan concurrency, NotSupported, GPU VRAM unknown/confidence e security capabilities.

# Final Recommendation

A Fase 2 esta funcional, read-only e nao destrutiva, por isso nao ha motivo para rejeicao total. Entretanto, a Fase 3 nao deve ser iniciada ate corrigir os HIGH findings. A principal recomendacao e endurecer a confiabilidade do scanner antes que Analysis/Recommendation consuma esses dados como base de decisao.

