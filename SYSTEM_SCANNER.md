# BorealBoost - System Scanner

Data: 2026-08-12
Status: Fase 2 implementada e corrigida como scanner somente leitura.

## Objetivo

O System Scanner produz um retrato tecnico normalizado do computador atual. Ele coleta fatos para fases futuras, mas nao interpreta esses fatos como recomendacoes e nao executa otimizacoes.

Pipeline:

1. criar `ScanId`;
2. iniciar uma unica `ScanSession` ativa;
3. executar providers registrados;
4. aplicar timeout/cancellation por provider sem abandonar chamada nativa em background;
5. normalizar dados;
6. agregar `SystemSnapshot`;
7. marcar scan parcial quando algum provider falhar, expirar ou nao for suportado;
8. publicar resultado na UI e manter o ultimo snapshot em memoria.

## Providers

Providers implementados:

- `OperatingSystemScanProvider`;
- `CpuScanProvider`;
- `GraphicsScanProvider`;
- `MemoryScanProvider`;
- `StorageScanProvider`;
- `HardwareFirmwareScanProvider`;
- `DisplayScanProvider`;
- `NetworkScanProvider`;
- `DevicesScanProvider`;
- `DriverInventoryScanProvider`;
- `PowerScanProvider`;
- `ServicesScanProvider`;
- `ProcessesScanProvider`;
- `StartupScanProvider`;
- `SecurityCapabilitiesScanProvider`.

Cada provider declara:

- `Name`;
- `Weight`;
- `Timeout`;
- fonte de dados principal;
- resultado `Success`, `Partial`, `Failed`, `NotSupported`, `TimedOut` ou `Canceled`.

## Fontes

Fontes usadas nesta fase:

- WMI/CIM via `System.Management` para OS, CPU, GPU, RAM, storage fisico, motherboard, BIOS, PnP devices, driver inventory, services, TPM e Device Guard quando disponiveis.
- APIs .NET para volumes (`DriveInfo`) e rede (`NetworkInterface`).
- APIs .NET para inventario de processos (`Process.GetProcesses`), sem finalizar ou modificar processos.
- Win32 read-only para displays (`EnumDisplayDevices`/`EnumDisplaySettings`), firmware type (`GetFirmwareType`) e power status (`GetSystemPowerStatus`).
- Registry read-only para `DisplayVersion`, `UBR`, Secure Boot state, VBS/Memory Integrity configurados, active power scheme e nomes de entradas `Run`/`RunOnce` em views 64 e 32 bits.
- File system read-only para nomes de itens nas pastas Startup do usuario e comuns, sem ler conteudo de atalhos ou argumentos.

Registry e usado somente com `OpenSubKey(..., writable: false)`. Nao ha escrita, criacao ou exclusao de chaves.

## Snapshot

`SystemSnapshot` contem:

- `ScanMetadata`;
- `OperatingSystem`;
- `Hardware`;
- `Processors`;
- `Graphics`;
- `Memory`;
- `Storage`;
- `Motherboard`;
- `Firmware`;
- `Devices`;
- `Drivers`;
- `Network`;
- `Displays`;
- `Power`;
- `Services`;
- `Processes`;
- `StartupItems`;
- `Capabilities`.

`ScanMetadata` registra `ScanId`, inicio/fim UTC, duracao, versao do app, schema, arquitetura, resultados por provider, `PartialScan`, warnings e errors.

Memoria diferencia:

- `InstalledPhysicalBytes`: soma dos modulos fisicos quando todos os modulos reportam capacidade;
- `VisiblePhysicalBytes`: memoria fisica visivel/utilizavel pelo Windows via `Win32_ComputerSystem.TotalPhysicalMemory`.

GPU diferencia `AdapterRamBytes` de `AdapterRamStatus`. Na Fase 2, `Win32_VideoController.AdapterRAM` sozinho nao e fonte confiavel de VRAM; quando nao houver fonte melhor, o valor permanece `null` com status `Unknown`.

Displays incluem DPI quando `EnumDisplaySettings` fornece `LogPixels`; caso contrario, DPI fica `null`.

Capabilities possuem `DetectionStatus`: `Known`, `Unknown`, `Unavailable`, `NotSupported` ou `Deferred`.

## Unknown

Quando uma fonte nao fornece dado confiavel, o scanner grava `null`, `Unknown`, `Unavailable`, `NotSupported` ou `Deferred`. Ele nao inventa:

- VRAM;
- refresh rate;
- XMP/EXPO;
- media type de disco;
- suporte de firmware;
- estado de driver alem do observado;
- recomendacoes de seguranca.

## Privacidade

Deliberadamente excluidos do snapshot:

- username;
- email;
- IP publico;
- MAC address;
- SSID;
- Windows product key;
- machine GUID;
- tokens/secrets;
- serial number de motherboard/BIOS/RAM.

Device Instance ID e Hardware/Compatible IDs podem aparecer no inventario de dispositivos porque sao fatos tecnicos necessarios para o Driver Engine futuro. Esses dados nao sao despejados em log.

Politica implementada:

- `SystemSnapshotPrivacyPolicy` classifica campos como `PublicTechnical`, `InternalTechnical`, `Sensitive`, `DoNotPersist` ou `DoNotReport`.
- `CreateReportSafeSnapshot` remove Device Instance ID, Hardware IDs, Compatible IDs, INF, services, processos, startup items e detalhes de adaptadores de rede de uma copia preparada para relatorio futuro.
- Machine name nao faz parte do `SystemSnapshot` e nao e exibido por padrao no Dashboard.

## Timeouts e cancelamento

O scanner executa providers de forma controlada e sequencial na Fase 2. A UI recebe progresso ponderado por provider concluido. Timeout de provider marca `TimedOut` e gera scan parcial.

WMI e executado sem `Task.Run`/`WaitAsync` dentro do adapter. O scanner roda em uma `ScanSession` central em background, e a chamada WMI usa `EnumerationOptions.Timeout`. Se o usuario cancelar enquanto uma chamada WMI nativa estiver ativa, a sessao fica em cancelamento ate a chamada retornar, falhar ou atingir o timeout WMI; ela nao e declarada finalizada enquanto existe trabalho nativo em andamento.

Cancelamento do usuario retorna falha `scanner.canceled` e nao apresenta a sessao como concluida.

## Scan Session

`SystemScanSessionService` e singleton e controla os estados:

- `Idle`;
- `Running`;
- `Cancelling`;
- `Completed`;
- `Failed`;
- `Cancelled`.

Dois scans simultaneos sao rejeitados com `scanner.already_running`, inclusive quando a pagina Scanner e recriada por navegacao.

## Limites

- Scanner nao usa PowerShell ou cmd.
- Scanner nao executa benchmarks.
- Scanner nao consulta fontes externas de drivers.
- Scanner nao altera Registry, Services, Power, DNS, Drivers, Windows Update, Defender, Firewall, AppX, features ou firmware.
- O Agent nao e usado para scanning comum nesta fase.

## UI

A pagina `Scanner` executa o scan sem bloquear a UI, mostra progresso honesto por provider, permite cancelar e exibe resumo factual:

- Sistema;
- CPU;
- GPU;
- Memoria instalada e visivel pelo Windows;
- Storage;
- Dispositivos;
- Monitores;
- Rede;
- Servicos;
- Processos;
- Inicializacao;
- resultados por provider.

Dashboard consome o ultimo snapshot em memoria e mostra somente fatos reais. Se nao houver scan, exibe que a analise ainda nao foi realizada.

`EnableAgentHandshakeProbe=false` e respeitado no Dashboard; o Agent nao e iniciado apenas para a pagina inicial quando o probe esta desabilitado.

## Pendencias

- Validar Windows 10 22H2 x64/build 19045 em VM real.
- Validar Windows 11 24H2 separadamente; a maquina desta fase esta em build 26200.
- Validar DPI 125%, 150% e 200% com screenshot/manual.
- Substituir ou complementar WMI por SetupAPI/CfgMgr32 no Driver Engine futuro quando instalacao/diagnostico critico exigir matching mais forte.
- Scheduled Tasks permanecem fora do inventario de startup padrao da Fase 2 por custo/privacidade e devem ser avaliadas antes de persistencia ou relatorio.
- Defender, Firewall e BitLocker permanecem `Deferred` na Fase 2; nao sao usados para recomendacoes.
