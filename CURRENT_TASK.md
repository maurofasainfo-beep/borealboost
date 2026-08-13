# CURRENT_TASK.md
# BorealBoost — Fase 2: System Scanner

> A Fase 1 — Foundation foi concluída, auditada, corrigida, validada e aprovada.
>
> Esta tarefa inicia a Fase 2 — System Scanner.
>
> O objetivo desta fase é construir um scanner confiável, normalizado e somente leitura capaz de produzir um retrato técnico do computador.
>
> NÃO implementar otimizações nesta fase.

---

# 1. STATUS

Fase anterior:

✅ FASE 1 — FOUNDATION — APROVADA

Validação final da Fase 1:

- restore: PASS
- build: PASS
- tests: 52/52 PASS
- Agent arbitrary execution: NÃO
- destructive functionality: NÃO

Fase atual:

🚧 FASE 2 — SYSTEM SCANNER

---

# 2. OBJETIVO PRINCIPAL

Implementar o System Scanner do BorealBoost.

O Scanner será responsável por descobrir e normalizar informações relevantes do computador para que fases posteriores possam realizar:

- análise;
- recomendações;
- compatibilidade;
- seleção de otimizações;
- drivers;
- benchmark;
- rollback;
- relatórios.

O Scanner NÃO decide o que otimizar.

O Scanner apenas:

DETECTA
→ COLETA
→ NORMALIZA
→ VALIDA
→ REPORTA

Separar claramente:

FACTS

de:

RECOMMENDATIONS

Nesta fase trabalhamos apenas com FACTS.

---

# 3. LEITURA OBRIGATÓRIA

Antes de modificar código, leia integralmente:

- BOREALBOOST_MASTER_SPEC.md
- CODEX_BOOTSTRAP.md
- CURRENT_TASK.md
- DISCOVERY.md
- REQUIREMENTS.md
- ARCHITECTURE.md
- ARCHITECTURE_DECISION_RECORD.md
- DOMAIN_MODEL.md
- SECURITY.md
- COMPATIBILITY_MATRIX.md
- IMPLEMENTATION_ROADMAP.md
- UX_SPECIFICATION.md
- PHASE1_AUDIT.md
- PHASE1_REVALIDATION.md

Depois analise o código atual integralmente.

Não presuma que documentação e implementação continuam idênticas.

---

# 4. REGRA FUNDAMENTAL

O System Scanner deve ser:

- read-only;
- determinístico quando possível;
- tolerante a hardware desconhecido;
- tolerante a APIs indisponíveis;
- cancelável;
- testável;
- observável;
- modular;
- normalizado;
- compatível com Windows 10 e Windows 11 suportados pelo projeto.

O Scanner não pode modificar o sistema.

---

# 5. PROIBIÇÃO DE EFEITOS COLATERAIS

Durante scanning, NÃO:

- alterar Registry;
- alterar Services;
- alterar power plan;
- instalar driver;
- atualizar driver;
- remover driver;
- executar Windows Update;
- alterar DNS;
- alterar rede;
- remover AppX;
- executar debloat;
- executar tweaks;
- alterar Defender;
- alterar Firewall;
- modificar BIOS/UEFI;
- modificar GPU settings;
- alterar Windows Features;
- criar Restore Point;
- executar otimizações.

A coleta deve ser somente leitura.

---

# 6. PRINCÍPIO DE PRIVILÉGIO

O Scanner deve funcionar com o menor privilégio possível.

Não elevar o BorealBoost.Agent apenas para obter informações que APIs normais do processo do usuário conseguem fornecer.

Se determinado dado realmente exigir privilégio elevado:

1. documentar;
2. classificar necessidade;
3. avaliar se o dado é realmente necessário;
4. não criar operação privilegiada sem respeitar a arquitetura do Agent.

Não transformar o Agent em ferramenta genérica de consulta.

---

# 7. ARQUITETURA DO SCANNER

Projetar e implementar scanner modular.

Exemplo conceitual:

SystemScanner

composto por providers especializados:

OperatingSystemProvider
CpuProvider
GpuProvider
MemoryProvider
StorageProvider
MotherboardProvider
FirmwareProvider
DeviceProvider
DriverInventoryProvider
NetworkProvider
DisplayProvider
PowerProvider
SecurityCapabilityProvider

Os nomes podem variar conforme a arquitetura existente.

Evitar um único:

SystemScanner.cs

com centenas ou milhares de linhas.

---

# 8. PIPELINE

O fluxo conceitual deve ser:

Start Scan
↓
Create ScanSession
↓
Collect Providers
↓
Normalize
↓
Validate
↓
Aggregate
↓
Generate SystemSnapshot
↓
Present Result

Falha de um provider não deve necessariamente invalidar todo o scan.

Exemplo:

falha ao identificar monitor

não deve impedir:

CPU
GPU
RAM
OS

de serem reportados.

---

# 9. RESULTADO DO SCAN

Criar um modelo normalizado semelhante conceitualmente a:

SystemSnapshot

contendo grupos como:

OperatingSystem
Hardware
Processors
Graphics
Memory
Storage
Motherboard
Firmware
Devices
Drivers
Network
Displays
Power
Capabilities
ScanMetadata

Seguir DOMAIN_MODEL.md quando já houver modelo aprovado.

Não duplicar entidades existentes sem necessidade.

---

# 10. SCAN METADATA

Registrar metadados do scan.

No mínimo:

- ScanId
- StartedAtUtc
- CompletedAtUtc
- Duration
- AppVersion
- SchemaVersion
- MachineArchitecture
- ProviderResults
- PartialScan
- Errors/Warnings

Não registrar dados pessoais desnecessários.

---

# 11. SISTEMA OPERACIONAL

Detectar de forma confiável:

- Windows 10 ou Windows 11;
- edição;
- versão;
- build;
- revision quando disponível;
- arquitetura;
- display version;
- instalação x64/ARM64 quando aplicável;
- estado de compatibilidade com BorealBoost.

Não depender exclusivamente de:

Environment.OSVersion

se isso puder produzir identificação incorreta.

Utilizar mecanismos Windows apropriados e documentados.

---

# 12. COMPATIBILIDADE DO WINDOWS

Classificar conceitualmente:

Supported
LegacySupported
Unsupported
Unknown

Conforme COMPATIBILITY_MATRIX.md.

Windows 10 22H2 x64/build 19045 permanece target legado.

Não confundir:

"Microsoft ainda oferece suporte"

com

"BorealBoost consegue operar".

---

# 13. CPU

Detectar:

- fabricante;
- nome/modelo;
- arquitetura;
- logical processors;
- physical cores quando disponível;
- sockets;
- max/current clock quando confiável;
- processor identifier;
- família/modelo quando disponível;
- virtualization capability quando relevante.

Identificar pelo menos:

Intel
AMD
Unknown

Não inferir geração da CPU por parsing frágil do nome se não houver necessidade.

---

# 14. GPU

Detectar todas as GPUs.

Não assumir uma GPU única.

Coletar quando disponível:

- nome;
- vendor;
- device ID;
- PNP Device ID;
- driver version;
- driver date;
- adapter RAM quando confiável;
- status;
- integrada/dedicada quando puder ser determinado com evidência adequada.

Suportar cenários:

Intel + NVIDIA
AMD iGPU + AMD dGPU
Intel + AMD
uma única GPU
Microsoft Basic Display Adapter
VM GPU
GPU desconhecida

Não inventar VRAM.

---

# 15. MEMÓRIA RAM

Detectar:

- total físico;
- módulos;
- capacidade por módulo;
- quantidade de módulos;
- slots usados quando disponível;
- fabricante;
- part number;
- velocidade configurada quando confiável;
- velocidade nominal quando disponível.

Não calcular automaticamente XMP/EXPO ativo sem evidência suficiente.

Não recomendar upgrade nesta fase.

---

# 16. STORAGE

Detectar todos os discos físicos relevantes.

Coletar quando disponível:

- modelo;
- fabricante;
- capacidade;
- tipo de mídia;
- bus type;
- SSD/HDD/NVMe quando confiável;
- status;
- partitions/volumes relevantes;
- espaço total;
- espaço livre;
- system drive.

Evitar heurísticas frágeis baseadas apenas no nome do dispositivo.

Não executar benchmark de disco nesta fase.

---

# 17. MOTHERBOARD

Detectar quando disponível:

- manufacturer;
- product/model;
- version;
- serial apenas se realmente necessário.

Por padrão, evitar persistir/exibir serial number se não houver necessidade funcional.

O BorealBoost não deve coletar identificadores pessoais/hardware únicos sem motivo.

---

# 18. BIOS / UEFI

Detectar:

- fabricante;
- versão;
- data;
- firmware type quando disponível;
- UEFI/Legacy quando determinável;
- Secure Boot capability/state quando apropriado e read-only.

Não modificar firmware.

Não tentar atualizar BIOS.

---

# 19. DESKTOP VS NOTEBOOK

Implementar classificação robusta.

Possíveis resultados:

Desktop
Laptop
Convertible
Tablet
VirtualMachine
Unknown

Não depender apenas da existência de bateria.

Combinar evidências apropriadas quando necessário.

---

# 20. POWER / BATTERY

Quando notebook:

detectar informações read-only relevantes:

- bateria presente;
- AC conectado quando disponível;
- battery percentage quando apropriado;
- power source;
- power plan atual, se puder ser consultado de maneira segura.

Não alterar power plan.

Não criar Ultimate Performance nesta fase.

---

# 21. MONITORES

Detectar quando disponível:

- quantidade;
- resolução;
- refresh rate;
- primary;
- identificação básica.

Suportar múltiplos monitores.

Refresh rate deve representar valor real quando API permitir.

Não inventar 60 Hz como fallback silencioso.

Se desconhecido:

Unknown/null.

---

# 22. NETWORK

Detectar adaptadores relevantes.

Coletar:

- nome;
- tipo;
- status;
- link speed quando confiável;
- Wi-Fi/Ethernet quando determinável;
- physical/virtual quando determinável.

Evitar expor:

- IP público;
- SSID;
- MAC address;

sem necessidade funcional explícita.

Não executar alterações de rede.

---

# 23. DEVICES

Criar inventário necessário para diagnóstico futuro.

Detectar:

- dispositivos PnP relevantes;
- Device Instance ID;
- Hardware IDs quando necessário;
- Compatible IDs quando necessário;
- manufacturer;
- class;
- status;
- problem code quando disponível.

Isso será base para Driver Engine futuro.

Não instalar driver nesta fase.

---

# 24. DRIVER INVENTORY

Criar somente INVENTÁRIO read-only.

Para drivers/dispositivos relevantes, coletar quando disponível:

- device;
- provider;
- version;
- date;
- INF;
- signer/publisher quando acessível;
- device status;
- problem status.

IMPORTANTE:

Nesta fase NÃO:

- procurar driver na internet;
- baixar driver;
- instalar driver;
- atualizar driver;
- remover driver.

---

# 25. DRIVERS AUSENTES / PROBLEMÁTICOS

O Scanner deve conseguir identificar sinais objetivos de problemas de dispositivo.

Exemplos:

- dispositivo sem driver;
- Device Manager problem code;
- driver não iniciado;
- dispositivo desconhecido.

Representar isso como FACT.

Exemplo conceitual:

DeviceStatus:
MissingDriver

Não gerar ainda:

"Instale driver X"

Essa recomendação pertence às fases posteriores.

---

# 26. VIRTUAL MACHINE

Detectar quando houver evidência suficiente:

- Hyper-V
- VMware
- VirtualBox
- outras VMs conhecidas
- Unknown VM

Não depender de uma única string.

VM deve ser representada no snapshot porque otimizações futuras poderão precisar ser bloqueadas.

---

# 27. CAPABILITIES

Criar modelo para capabilities técnicas observadas.

Exemplos possíveis:

- SecureBootAvailable
- SecureBootEnabled
- TpmPresent
- VirtualizationAvailable
- BatteryPresent
- MultipleGpus
- MultipleDisplays

Somente incluir capabilities justificadas pelos requisitos atuais/futuros.

Não transformar isso em Recommendation Engine.

---

# 28. FONTES DE DADOS

Priorizar APIs Windows estáveis e adequadas.

Podem ser utilizadas conforme necessidade:

- Win32 APIs;
- SetupAPI;
- Configuration Manager APIs;
- Windows.Devices;
- CIM/WMI quando apropriado;
- Performance/Power APIs read-only;
- Registry SOMENTE para leitura quando realmente necessário e documentado.

IMPORTANTE:

Registry read-only pode ser utilizado como fonte de informação nesta fase quando existir justificativa técnica.

Nenhuma escrita no Registry é permitida.

---

# 29. POWERSHELL

Evitar PowerShell como mecanismo primário do Scanner.

Não criar scanner baseado em:

powershell.exe
→ comando
→ parse de texto

Preferir APIs nativas/gerenciadas.

Se alguma informação só puder ser obtida de forma razoável via PowerShell:

documentar antes.

Não implementar automaticamente.

---

# 30. WMI / CIM

WMI/CIM pode ser utilizado quando apropriado.

Mas:

- encapsular;
- usar timeout/cancellation quando possível;
- tratar provider indisponível;
- não espalhar queries WMI pela aplicação;
- não bloquear UI;
- não confiar cegamente em valores;
- normalizar resultados.

Criar adapter/provider apropriado.

---

# 31. ASYNC

O scan não pode congelar a interface.

Implementar execução assíncrona quando apropriado.

Suportar:

CancellationToken

nos contratos relevantes.

Não usar:

async void

exceto event handlers estritamente necessários na UI.

---

# 32. PARALLELISM

Providers independentes podem executar em paralelo quando seguro.

Porém:

não criar paralelismo indiscriminado.

Evitar:

- saturação;
- corrida;
- acesso inseguro;
- dezenas de consultas WMI simultâneas.

Definir estratégia controlada.

---

# 33. TIMEOUTS

Providers potencialmente lentos devem possuir política de timeout.

Uma API travada não pode deixar BorealBoost indefinidamente em:

"Analisando..."

Quando timeout ocorrer:

- registrar;
- marcar provider;
- continuar quando seguro;
- refletir partial scan.

---

# 34. PROVIDER RESULT

Cada provider deve possuir resultado observável.

Exemplo conceitual:

Success
Partial
Failed
NotSupported
TimedOut

Com:

- duration;
- warnings;
- errors;
- source.

Não usar exceptions como fluxo normal.

---

# 35. NORMALIZAÇÃO

Separar:

raw data

de:

domain snapshot.

Exemplo:

WMI/Win32/SetupAPI
↓
Raw DTO
↓
Normalization
↓
Domain Model

Isso evita contaminar o domínio com detalhes das APIs Windows.

---

# 36. UNKNOWN É VÁLIDO

Regra importante:

UNKNOWN é melhor do que informação inventada.

Quando não for possível determinar:

- GPU type;
- media type;
- refresh rate;
- RAM speed;
- firmware;
- driver state;

representar explicitamente:

Unknown/null/Unavailable

conforme o modelo.

Não adivinhar.

---

# 37. PROVENANCE

Para dados importantes, permitir rastrear conceitualmente a origem quando isso ajudar diagnóstico.

Exemplo:

Source:
SetupAPI
WMI
Win32
RegistryReadOnly

Não é necessário mostrar tudo ao cliente.

Pode ser utilizado em logs/debug.

---

# 38. PRIVACIDADE

O SystemSnapshot não deve virar ferramenta de fingerprinting.

Evitar armazenar sem necessidade:

- username;
- email;
- IP público;
- MAC;
- SSID;
- Windows product key;
- serial numbers;
- machine GUID;
- tokens;
- secrets.

Se algum identificador único for tecnicamente necessário futuramente, isso deverá ser decisão explícita.

---

# 39. LOGGING

Registrar:

- início do scan;
- ScanId;
- providers iniciados;
- providers concluídos;
- duração;
- falhas;
- timeouts;
- partial scan;
- conclusão.

Não registrar informações sensíveis.

Não despejar snapshot inteiro no log indiscriminadamente.

---

# 40. CACHE

Não implementar cache complexo prematuramente.

Durante uma única sessão, o snapshot pode ser mantido em memória.

Se persistência for necessária pela arquitetura:

- documentar;
- versionar schema;
- usar paths aprovados;
- evitar dados sensíveis.

Não criar banco de dados apenas para o Scanner nesta fase sem necessidade arquitetural clara.

---

# 41. UI — PÁGINA ANÁLISE

Transformar o placeholder de Análise em uma interface funcional do Scanner.

Fluxo esperado:

Estado inicial
↓
"Analisar computador"
↓
Scanning
↓
Progress
↓
Resultado

---

# 42. PROGRESSO

O usuário solicitou visualização percentual.

Implementar progresso honesto.

Não usar porcentagem fictícia baseada apenas em timer.

O progresso deve ser derivado de etapas/providers conhecidos.

Exemplo:

0%
Preparando análise

15%
Sistema operacional

30%
Processador

...

Os pesos podem ser definidos com base nas etapas reais.

Se execução paralela tornar percentual exato impossível, usar progresso ponderado por providers concluídos.

Nunca mostrar 99% artificialmente enquanto aguarda tarefa desconhecida.

---

# 43. STATUS VISUAL DO SCAN

Mostrar etapa atual de forma amigável.

Exemplos:

Analisando processador...
Analisando placa de vídeo...
Verificando memória...
Analisando armazenamento...
Verificando dispositivos...
Finalizando análise...

Não mostrar nomes internos de classes ao cliente.

---

# 44. RESULTADO VISUAL

Após scan, apresentar resumo organizado.

Cards sugeridos:

Sistema
CPU
GPU
Memória
Armazenamento
Dispositivos
Monitores
Rede

Não transformar essa tela em painel excessivamente técnico.

Detalhes avançados podem ficar em expansão/modal/seção técnica.

---

# 45. HARDWARE SUMMARY

Dashboard pode começar a consumir o último snapshot em memória para mostrar informações reais.

Exemplo:

CPU
GPU
RAM
Windows

Não mostrar valores fictícios.

Se não houver scan:

"Análise ainda não realizada."

---

# 46. DRIVER STATUS VISUAL

Pode mostrar fatos como:

"2 dispositivos apresentam problemas"

ou:

"Nenhum problema de dispositivo detectado"

somente quando derivado do scan real.

Não mostrar:

"2 drivers precisam ser atualizados"

porque isso exigiria comparação com fonte externa, que ainda não existe.

---

# 47. ERROS NA UI

Se provider falhar:

não mostrar stack trace.

Exemplo:

"Algumas informações não puderam ser identificadas."

Permitir detalhes técnicos apropriados em log.

Scan parcial deve ser claramente indicado.

---

# 48. CANCELAMENTO

Usuário deve poder cancelar scan quando tecnicamente viável.

Cancelamento deve:

- sinalizar providers;
- parar trabalho futuro;
- não corromper estado;
- não apresentar scan cancelado como concluído.

---

# 49. SCAN CONCURRENCY

Não permitir iniciar múltiplos scans concorrentes acidentalmente pela UI.

Definir comportamento:

- botão desabilitado durante scan;
ou
- cancelamento/substituição explícita.

Não permitir corrida de snapshots.

---

# 50. TESTES UNITÁRIOS

Criar testes para:

- normalização;
- OS classification;
- Windows compatibility;
- CPU model;
- GPU collection;
- memory normalization;
- storage normalization;
- device status;
- driver inventory;
- provider result;
- partial scan;
- timeout;
- cancellation;
- Unknown handling;
- progress calculation.

Mockar/adaptar APIs quando necessário.

Não depender exclusivamente da máquina do desenvolvedor.

---

# 51. TESTES DE INTEGRAÇÃO

Criar testes read-only quando possível para:

- OS provider;
- CPU provider;
- memory provider;
- storage provider;
- device provider;
- display provider.

Testes devem tolerar hardware diferente.

Não fazer assert como:

GPU == "RTX 4090"

ou qualquer hardware específico.

Validar invariantes.

---

# 52. SYSTEM TEST

Criar teste seguro que execute scan completo na máquina atual.

Validar:

- não crash;
- snapshot produzido;
- ScanId válido;
- OS identificado;
- CPU identificada quando API disponível;
- RAM > 0 quando API disponível;
- duração válida;
- providers reportados.

Não modificar sistema.

---

# 53. SAFETY TEST

Adicionar teste/checagem para garantir que o Scanner não introduziu chamadas operacionais proibidas.

Auditar ocorrências de:

- Registry write;
- ServiceController mutation;
- Process.Start;
- powershell;
- cmd.exe;
- powercfg;
- DISM;
- SFC;
- PnPUtil mutation;
- Windows Update mutation;
- AppX mutation.

Qualquer ocorrência deve ser explicada.

---

# 54. PERFORMANCE

Scanner não deve ser absurdamente lento.

Medir duração na máquina de desenvolvimento.

Não definir SLA artificial antes de medir.

Registrar:

- duração total;
- providers mais lentos;
- timeouts.

Otimizar apenas gargalos reais.

---

# 55. COMPATIBILIDADE

Validar conceitualmente e com testes disponíveis:

Windows 10 22H2 x64

Windows 11 suportado

Quando não houver VM disponível:

marcar como NÃO VALIDADO.

Não fingir teste de Windows 10 executando somente no Windows 11.

---

# 56. DOCUMENTAÇÃO

Atualizar conforme necessário:

README.md
ARCHITECTURE.md
DOMAIN_MODEL.md
SECURITY.md
COMPATIBILITY_MATRIX.md

Criar se útil:

SYSTEM_SCANNER.md

Documentar:

- providers;
- fontes;
- fallbacks;
- timeouts;
- normalização;
- limitações;
- privacidade;
- comportamento Unknown.

---

# 57. NÃO IMPLEMENTAR RECOMMENDATION ENGINE

Não criar nesta fase:

if (ram < X)
    recommend(...)

if (gpu == ...)
    optimize(...)

if (Windows11)
    tweak(...)

Essas decisões pertencem à Fase 3.

Scanner coleta fatos.

Analysis interpreta fatos.

Optimization executa mudanças.

Preservar essa separação.

---

# 58. NÃO IMPLEMENTAR DRIVER ENGINE OPERACIONAL

Mesmo que o Scanner encontre:

MissingDriver

não:

- procurar driver;
- abrir site;
- baixar;
- instalar;
- atualizar.

Isso pertence à fase Drivers.

---

# 59. NÃO IMPLEMENTAR BOREAL SCORE

Não calcular Boreal Score nesta fase.

O Scanner apenas fornecerá dados futuros para o algoritmo.

---

# 60. NÃO IMPLEMENTAR BENCHMARK

Não executar:

- CPU benchmark;
- GPU benchmark;
- disk benchmark;
- FPS benchmark;
- latency benchmark.

Isso pertence à fase apropriada.

---

# 61. BUILD E TESTES

Ao concluir executar no ambiente padrão:

dotnet --info

dotnet restore .\BorealBoost.sln

dotnet build .\BorealBoost.sln --no-restore

dotnet test .\BorealBoost.sln --no-build

O SDK global 10.0.400 já foi instalado e validado na conclusão da Fase 1.

Não utilizar SDK portátil se o SDK global estiver funcionando.

---

# 62. WARNINGS

Build esperado:

0 errors.

Investigar warnings novos.

Não esconder warning causado pela implementação.

---

# 63. EXECUÇÃO REAL

Executar BorealBoost.App quando o ambiente permitir.

Realizar scan real seguro.

Registrar:

- Windows detectado;
- CPU;
- GPU(s);
- RAM;
- storage;
- devices;
- displays;
- duração;
- providers com falha.

Não precisa incluir identificadores sensíveis no relatório.

---

# 64. REVISÃO DO DIFF

Antes de concluir:

git diff

Revisar todas as alterações.

Confirmar:

- nenhuma otimização;
- nenhuma escrita no sistema;
- nenhum driver instalado;
- nenhuma execução arbitrária;
- nenhuma fase futura antecipada.

---

# 65. CRITÉRIOS DE ACEITAÇÃO

Fase 2 somente poderá ser considerada concluída quando:

- Scanner modular existir;
- SystemSnapshot existir;
- OS real for detectado;
- CPU real for detectada;
- GPU(s) forem enumeradas;
- RAM for detectada;
- storage for detectado;
- motherboard/firmware forem tratados;
- desktop/notebook/VM possuir classificação;
- displays forem tratados;
- network inventory básico existir;
- PnP devices forem inventariados;
- driver inventory read-only existir;
- problemas de dispositivos puderem ser representados;
- Unknown for tratado corretamente;
- provider failure não derrubar scan completo;
- timeout existir onde necessário;
- cancellation existir;
- UI não congelar;
- progresso real existir;
- scan parcial existir;
- logs existirem;
- privacidade for respeitada;
- testes passarem;
- build passar;
- nenhuma modificação operacional do Windows existir.

---

# 66. ENTREGA OBRIGATÓRIA

Ao concluir apresentar:

## Resumo

O que foi implementado.

## Arquivos criados/modificados

Separar por projeto.

## Scanner Architecture

Listar providers.

## Data Sources

Para cada provider informar fonte principal.

Exemplo:

CPU → ...
GPU → ...
Storage → ...
Devices → ...

## SystemSnapshot

Resumir estrutura final.

## Real Machine Validation

Informar, sem dados sensíveis:

- Windows;
- CPU;
- quantidade de GPUs;
- RAM total;
- quantidade de discos;
- quantidade de displays;
- quantidade de dispositivos com problema;
- scan duration.

## Provider Results

Informar:

Success
Partial
Failed
TimedOut
NotSupported

## Privacy

Confirmar quais identificadores foram deliberadamente excluídos.

## Tests

Informar:

- testes adicionados;
- total;
- pass/fail.

## Build

Informar:

restore
build
test

## Performance

Informar duração do scan e providers mais lentos.

## Compatibility

Informar o que foi realmente validado e o que permanece não validado.

## Safety

Responder explicitamente:

1. Scanner escreve no Registry?
2. Scanner altera Services?
3. Scanner altera Power?
4. Scanner altera DNS/rede?
5. Scanner instala/atualiza drivers?
6. Scanner executa Windows Update?
7. Scanner executa PowerShell/cmd arbitrário?
8. Scanner executa otimizações?
9. Alguma operação destrutiva foi adicionada?
10. Fase 3 foi iniciada?

Respostas esperadas:

1. NÃO
2. NÃO
3. NÃO
4. NÃO
5. NÃO
6. NÃO
7. NÃO
8. NÃO
9. NÃO
10. NÃO

## Pendências

Listar limitações reais.

---

# 67. REGRA FINAL

Não tente transformar o Scanner no produto inteiro.

A responsabilidade desta fase é responder com confiança:

"Qual é o estado atual deste computador?"

e não:

"O que devemos mudar neste computador?"

Essa segunda pergunta pertence à Fase 3.

Não faça commit automaticamente.

Não inicie a Fase 3.