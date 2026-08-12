```text
# PROJETO: BOREALBOOST
## Aplicativo profissional de diagnóstico, otimização e validação de desempenho para Windows 10 e Windows 11

Você atuará como uma equipe sênior formada por:

- Software Architect
- Senior Windows Systems Engineer
- Senior .NET Desktop Engineer
- Windows Internals Engineer
- Performance Engineer
- Security Engineer
- UI/UX Designer
- QA Engineer
- DevOps / Release Engineer
- Technical Writer

Sua missão é projetar e implementar do zero o **BorealBoost**, uma aplicação desktop comercial destinada ao uso presencial de um técnico em computadores de clientes.

O BorealBoost NÃO deve ser tratado como um simples script com interface gráfica.

Ele deve ser desenvolvido como uma aplicação profissional, modular, auditável, reversível, segura, mensurável e preparada para expansão futura.

O objetivo principal é:

> analisar a configuração real da máquina, identificar oportunidades legítimas de melhoria, recomendar e aplicar otimizações compatíveis com aquele hardware e versão do Windows, medir os resultados e permitir reversão segura das alterações.

O foco principal é:

- desempenho geral;
- gaming;
- FPS;
- estabilidade de FPS;
- 1% lows;
- frametime;
- responsividade;
- input latency quando mensurável;
- processos desnecessários;
- consumo de RAM;
- serviços;
- inicialização;
- armazenamento;
- energia;
- drivers;
- rede;
- privacidade;
- debloat;
- configurações do Windows;
- integridade do sistema.

Não prometa ganhos de FPS que não possam ser medidos.

Não implemente tweaks placebo simplesmente porque são populares na internet.

O sistema pode possuir otimizações agressivas, mas elas precisam ser:

- identificadas;
- classificadas;
- verificadas;
- compatíveis com a máquina;
- documentadas;
- reversíveis;
- registradas em log;
- precedidas de aviso.

=====================================================================
1. CONTEXTO COMERCIAL
=====================================================================

O BorealBoost será inicialmente uma ferramenta de uso exclusivo do proprietário/técnico.

Não haverá nesta primeira versão:

- marketplace;
- sistema multiusuário;
- assinatura;
- pagamentos;
- conta pública;
- painel SaaS remoto;
- venda direta da licença ao consumidor.

O técnico instalará o BorealBoost no computador do cliente, executará o diagnóstico, selecionará/configurará a otimização e aplicará as alterações.

Mesmo sendo desktop, sua interface deve possuir aparência premium semelhante a aplicações SaaS modernas.

A aplicação deve transmitir:

- confiança;
- qualidade;
- profissionalismo;
- segurança;
- velocidade;
- clareza.

=====================================================================
2. NOME E IDENTIDADE
=====================================================================

Nome:

BorealBoost

Direção visual:

- dark mode como padrão;
- aparência SaaS premium;
- moderna;
- minimalista;
- tecnológica;
- limpa;
- sem aparência de utilitário antigo do Windows.

Paleta principal:

- azul;
- azul elétrico;
- roxo;
- violeta;
- rosa/magenta.

Use gradientes de forma controlada.

Sugestão de direção:

Background:
#080B14
#0D1020

Surface:
#111629
#151B32

Blue:
#3B82F6

Electric Blue:
#2563EB

Violet:
#7C3AED

Purple:
#9333EA

Pink:
#EC4899

Success:
#22C55E

Warning:
#F59E0B

Danger:
#EF4444

Text Primary:
#F8FAFC

Text Secondary:
#94A3B8

Não transforme a interface em um festival de glow.

Usar:

- cards;
- bordas suaves;
- sombras discretas;
- gradientes pontuais;
- progress bars;
- indicadores;
- badges;
- ícones consistentes;
- microinterações discretas.

Evitar:

- animações excessivas;
- glassmorphism exagerado;
- interfaces lotadas;
- elementos sobrepostos;
- contraste ruim;
- layout semelhante visualmente ao WinUtil.

A inspiração no WinUtil deve ser FUNCIONAL, não uma cópia visual.

=====================================================================
3. REFERÊNCIA FUNCIONAL: WINUTIL
=====================================================================

Antes de implementar o mecanismo de otimização:

1. Analise a versão atual do projeto oficial:
   ChrisTitusTech/winutil

2. Leia principalmente:

- arquitetura;
- config/tweaks.json;
- config/preset.json;
- config/feature.json;
- config/dns.json;
- aplicações;
- módulo Tweaks;
- módulo Config;
- módulo Updates;
- módulo Install;
- mecanismos de Apply;
- mecanismos de Undo;
- detecção de tweaks instalados;
- automação;
- restore points.

3. Analise as funcionalidades presentes nas imagens de referência fornecidas pelo proprietário do projeto.

4. Produza inicialmente:

WINUTIL_ANALYSIS.md

 contendo:

- funcionalidades úteis ao BorealBoost;
- técnicas utilizadas;
- ajustes seguros;
- ajustes avançados;
- riscos;
- compatibilidade Windows 10;
- compatibilidade Windows 11;
- funcionalidades que não devemos incorporar;
- funcionalidades que podem ser melhoradas.

IMPORTANTE:

Não copie cegamente o projeto.

Se código do WinUtil for reutilizado:

- identificar exatamente os arquivos/trechos;
- verificar a licença vigente;
- preservar todas as atribuições e avisos obrigatórios;
- documentar a origem em THIRD_PARTY_NOTICES.md;
- evitar copiar branding, nome, identidade visual ou marca.

Preferencialmente, implemente um engine próprio usando documentação oficial do Windows e APIs próprias sempre que isso for tecnicamente razoável.

=====================================================================
4. REGRA CRÍTICA: WINDOWS 10 E WINDOWS 11
=====================================================================

BorealBoost deverá suportar:

- Windows 10 x64;
- Windows 11 x64.

NÃO presuma que uma otimização válida no Windows 11 é válida no Windows 10.

Cada otimização deverá declarar explicitamente:

supported_os:
- Windows 10?
- Windows 11?

supported_builds:
- mínimo;
- máximo quando necessário.

Cada tweak deve passar por uma Compatibility Rule.

Criar:

CompatibilityEngine

responsável por determinar:

- Windows;
- edição;
- build;
- arquitetura;
- hardware;
- notebook/desktop;
- recursos existentes;
- dependências;
- contraindicações.

Se uma alteração não for comprovadamente compatível:

NÃO EXECUTAR.

Mostrar:

"Esta otimização não é compatível com esta configuração."

=====================================================================
5. TECNOLOGIA
=====================================================================

Antes de implementar, faça uma decisão arquitetural documentada.

Preferência inicial:

- C#
- .NET moderno/LTS apropriado ao projeto
- Windows desktop nativo
- arquitetura preparada para privilégios elevados

Avaliar tecnicamente:

- WPF;
- WinUI 3;
- Avalonia apenas se houver justificativa real.

Como o produto é exclusivamente Windows, não escolher tecnologia multiplataforma apenas por moda.

Escolher a tecnologia que ofereça melhor equilíbrio entre:

- visual SaaS moderno;
- acesso às APIs do Windows;
- desempenho;
- estabilidade;
- manutenção;
- empacotamento;
- execução elevada;
- suporte Windows 10/11.

Gerar antes de implementar:

ARCHITECTURE_DECISION_RECORD.md

explicando a escolha.

=====================================================================
6. ARQUITETURA PROPOSTA
=====================================================================

Não implementar tudo dentro da interface.

Criar arquitetura modular semelhante conceitualmente a:

BorealBoost
│
├── BorealBoost.App
│   ├── Views
│   ├── ViewModels
│   ├── Components
│   ├── Themes
│   └── Navigation
│
├── BorealBoost.Core
│   ├── Models
│   ├── Contracts
│   ├── Rules
│   └── Shared
│
├── BorealBoost.System
│   ├── Hardware
│   ├── OperatingSystem
│   ├── Drivers
│   ├── Processes
│   ├── Services
│   ├── Registry
│   ├── Storage
│   ├── Network
│   └── Power
│
├── BorealBoost.Analysis
│   ├── SystemScanner
│   ├── HealthAnalyzer
│   ├── BottleneckAnalyzer
│   └── RecommendationEngine
│
├── BorealBoost.Optimization
│   ├── OptimizationEngine
│   ├── OptimizationCatalog
│   ├── CompatibilityEngine
│   ├── PresetEngine
│   ├── ExecutionPlanner
│   └── VerificationEngine
│
├── BorealBoost.Restore
│   ├── RestorePointService
│   ├── SnapshotService
│   └── RollbackEngine
│
├── BorealBoost.Benchmark
│   ├── BaselineCollector
│   ├── PostOptimizationCollector
│   └── ComparisonEngine
│
├── BorealBoost.Drivers
│   ├── DriverScanner
│   ├── MissingDriverDetector
│   ├── DriverSourceResolver
│   └── DriverInstaller
│
├── BorealBoost.Reporting
│   ├── ReportGenerator
│   ├── PdfExporter
│   └── HtmlExporter
│
└── BorealBoost.Infrastructure
    ├── Logging
    ├── Persistence
    ├── Updates
    └── Security

Adapte se necessário, mas mantenha separação de responsabilidades.

=====================================================================
7. SYSTEM SCANNER
=====================================================================

Ao iniciar uma análise, detectar automaticamente:

WINDOWS

- Windows 10/11;
- edição;
- versão;
- build;
- arquitetura;
- UEFI/Legacy;
- Secure Boot;
- estado de ativação, apenas informativo;
- última inicialização;
- uptime.

DISPOSITIVO

- desktop;
- notebook;
- fabricante;
- modelo;
- motherboard;
- BIOS/UEFI;
- versão da BIOS quando disponível.

CPU

- fabricante;
- modelo;
- geração quando identificável;
- cores;
- threads;
- clock;
- arquitetura;
- recursos relevantes.

GPU

- NVIDIA;
- AMD;
- Intel;
- modelo;
- VRAM quando disponível;
- driver;
- versão;
- data;
- estado.

RAM

- capacidade total;
- módulos;
- frequência quando disponível;
- uso atual;
- utilização idle.

ARMAZENAMENTO

- HDD;
- SATA SSD;
- NVMe;
- capacidade;
- espaço livre;
- partições relevantes;
- TRIM;
- saúde disponível sem dependência insegura.

DISPLAY

- monitor;
- resolução;
- refresh rate;
- múltiplos monitores.

REDE

- adaptadores;
- Ethernet/Wi-Fi;
- driver;
- configuração;
- DNS;
- link speed quando disponível.

POWER

- plano atual;
- configurações relevantes;
- estado AC/bateria;
- notebook vs desktop.

WINDOWS SERVICES

- serviços ativos;
- startup type;
- serviços considerados relevantes ao diagnóstico.

PROCESSOS

- quantidade;
- principais consumidores de CPU;
- RAM;
- disco;
- processos de inicialização.

STARTUP

- aplicativos de startup;
- impacto quando disponível.

SECURITY

Somente analisar de forma transparente:

- Windows Defender;
- Firewall;
- Memory Integrity;
- VBS;
- Hyper-V;
- recursos de virtualização;
- BitLocker.

NÃO desabilitar recursos de segurança automaticamente.

Alterações de segurança devem pertencer apenas ao nível apropriado, com explicação específica, riscos e confirmação.

=====================================================================
8. DETECÇÃO DE DRIVERS
=====================================================================

Implementar módulo de diagnóstico de drivers.

Detectar:

- dispositivos sem driver;
- dispositivos com erro no Device Manager;
- drivers ausentes;
- drivers potencialmente genéricos;
- versão instalada;
- hardware IDs;
- vendor IDs;
- device IDs.

Não criar um "driver updater" inseguro que baixa executáveis de sites aleatórios.

Prioridade das fontes:

1. Windows Update / Microsoft;
2. fabricante oficial do dispositivo;
3. NVIDIA;
4. AMD;
5. Intel;
6. fabricante do notebook;
7. fabricante da motherboard.

Nunca instalar driver vindo de:

- mirrors desconhecidos;
- sites de download genéricos;
- repositórios não verificados.

Antes da instalação:

- mostrar driver;
- versão atual;
- versão proposta;
- fabricante;
- fonte;
- assinatura;
- necessidade de reinicialização.

Validar assinatura digital quando tecnicamente possível.

Para drivers críticos:

- chipset;
- GPU;
- rede;
- áudio;
- storage;

obter confirmação antes da instalação, salvo se configurado explicitamente pelo técnico.

=====================================================================
9. DASHBOARD
=====================================================================

Tela inicial premium.

Topo:

BorealBoost

Mostrar máquina atual:

- nome/identificação;
- Windows;
- CPU;
- GPU;
- RAM.

Card principal:

"Seu PC está pronto para análise"

CTA:

ANALISAR COMPUTADOR

Depois do scan:

Boreal Score

Exemplo visual:

68 / 100

Subscores:

- Sistema
- Gaming
- Inicialização
- Memória
- Serviços
- Drivers
- Armazenamento
- Energia

Não inventar pontuações.

Criar metodologia documentada para cálculo.

Mostrar:

"27 oportunidades encontradas"

"Perfil recomendado: Médio"

Cards resumidos:

CPU
GPU
RAM
Storage
Windows
Drivers

Mostrar alertas:

- driver faltando;
- reinicialização pendente;
- pouco espaço;
- HDD;
- plano de energia inadequado;
- serviços excessivos;
- muitos aplicativos de startup;
- driver antigo somente quando puder ser determinado de maneira confiável.

=====================================================================
10. FLUXO PRINCIPAL
=====================================================================

Fluxo:

Instalação
↓
Primeira execução
↓
Solicitar elevação administrativa
↓
Dashboard
↓
Analisar PC
↓
Scanner
↓
Diagnóstico
↓
Recommendations
↓
Selecionar Preset
↓
Revisar alterações
↓
Criar Restore Point
↓
Criar Snapshot
↓
Benchmark inicial
↓
Executar otimizações
↓
Progress
↓
Verificação pós-tweak
↓
Reboot quando necessário
↓
Novo diagnóstico/benchmark
↓
Comparação
↓
Relatório
↓
PDF/HTML
↓
Possibilidade de rollback

=====================================================================
11. PRESETS
=====================================================================

Criar quatro modos.

--------------------------------------------------
BÁSICO — SAFE BOOST
--------------------------------------------------

Características:

- baixo risco;
- mudanças conservadoras;
- preservar funcionalidades;
- melhorar Windows sem alterações agressivas.

Interface:

verde/azul.

Descrição:

"Melhorias seguras de desempenho, limpeza e responsividade."

--------------------------------------------------
MÉDIO — PERFORMANCE
--------------------------------------------------

Perfil recomendado para a maioria dos clientes.

Otimizar:

- processos;
- serviços;
- startup;
- telemetria;
- apps desnecessários;
- gaming;
- energia;
- UI;
- armazenamento;
- rede;
- background;
- recursos opcionais aplicáveis.

Descrição:

"Equilíbrio entre máximo desempenho, estabilidade e funcionalidade."

--------------------------------------------------
AVANÇADO — EXTREME PERFORMANCE
--------------------------------------------------

Perfil realmente agressivo.

Pode modificar recursos que diminuem compatibilidade/conveniência.

ANTES de habilitar:

mostrar modal:

"Modo Avançado"

"Este perfil realiza alterações profundas no Windows para priorizar desempenho. Algumas funcionalidades podem ser desativadas ou alteradas. Um ponto de restauração e snapshot serão criados antes da execução."

Exigir:

[ ] Entendo os riscos e desejo continuar

Aplicar somente otimizações consideradas compatíveis.

NUNCA confundir "agressivo" com "irresponsável".

--------------------------------------------------
PERSONALIZADO
--------------------------------------------------

Permitir seleção individual.

Categorias:

- Gaming
- CPU
- GPU
- RAM
- Energia
- Windows
- Serviços
- Inicialização
- Debloat
- Privacidade
- Rede
- Armazenamento
- Interface
- Explorer
- Xbox
- Apps
- Drivers
- Features
- Updates
- Fixes
- Advanced
- Experimental

=====================================================================
12. CATÁLOGO DE OTIMIZAÇÕES
=====================================================================

Não programar tweaks diretamente nos botões.

Criar um catálogo declarativo.

Cada optimization deverá possuir algo equivalente a:

Id
Name
Description
Category
RiskLevel
ImpactArea
SupportedOS
SupportedBuilds
SupportedHardware
LaptopAllowed
RequiresRestart
RequiresLogout
RequiresAdmin
RequiresInternet
Dependencies
Conflicts
Detection
BeforeValue
Apply
Verify
Undo
AfterValue
Documentation
EvidenceLevel

Exemplo conceitual:

{
  "id": "BB-POWER-001",
  "name": "BorealBoost Maximum Performance",
  "category": "Power",
  "risk": "Medium",
  "desktop": true,
  "laptop": false,
  "requiresRestart": false,
  "supportsUndo": true
}

Não usar exatamente essa estrutura caso outra seja arquiteturalmente superior.

=====================================================================
13. RISK LEVELS
=====================================================================

Classificar toda otimização:

SAFE

Baixo risco.

ADVANCED

Pode alterar funcionalidade secundária.

AGGRESSIVE

Prioriza desempenho sobre conveniência/recursos.

EXPERIMENTAL

Evidência ou compatibilidade limitada.

Experimental:

- nunca entrar automaticamente no preset Básico;
- nunca entrar automaticamente no preset Médio;
- exigir seleção consciente;
- mostrar explicação.

=====================================================================
14. EVIDENCE LEVEL
=====================================================================

Criar classificação independente do risco:

A — documentação oficial / comportamento comprovado
B — forte evidência técnica
C — resultado dependente de hardware/configuração
D — experimental
X — não usar

Não incorporar tweak somente por existir em:

- TikTok;
- YouTube;
- fórum;
- script desconhecido;
- pacote "FPS Boost".

Se não houver mecanismo técnico plausível:

não incluir como otimização de desempenho.

=====================================================================
15. TWEAKS DO WINUTIL A CONSIDERAR
=====================================================================

Mapear e estudar funcionalidades equivalentes às presentes nas referências visuais e na versão atual do WinUtil.

ESSENTIAL / WINDOWS

Avaliar:

- Activity History
- Consumer Features
- Delivery Optimization
- Disk Cleanup
- End Task With Right Click
- Automatic Folder Discovery
- Hibernation
- Location Tracking
- Microsoft Store Recommended Search Results
- Device Companion Apps
- Restore Point
- Services Set To Manual
- Start Menu Previous Layout quando aplicável
- Telemetry
- Temporary Files
- Widgets
- Windows Platform Binary Table
- Game Mode

ADVANCED

Avaliar individualmente:

- Background Apps
- Date/Time UTC
- Reserved Storage
- Explorer Home/Gallery
- Fullscreen Optimizations
- IPv6
- IPv4 preferred
- Edge debloat/removal
- OneDrive
- Razer software auto-install blocking
- RDP warnings
- Storage Sense
- Notifications/Calendar
- Teredo
- Visual Effects
- Windows AI features quando aplicável à build
- O&O ShutUp10++ integration, somente se licenciamento/distribuição permitirem

CUSTOMIZATION

Avaliar:

- BSOD verbose mode
- Windows dark theme
- long paths
- file extensions
- hidden files
- Game Mode
- lock screen
- acrylic blur
- logon verbose
- Outlook New
- mouse acceleration
- multiplane overlay
- Num Lock
- sleep network connectivity
- S3 sleep
- scrollbars
- Settings home
- Bing search
- Start recommendations
- Sticky Keys
- battery percentage
- centered taskbar
- taskbar search
- task view
- snapping

Não incluir ajustes puramente estéticos em cálculo de performance.

Mantê-los em:

"Personalização"

=====================================================================
16. FEATURES DO WINDOWS
=====================================================================

Criar seção equivalente em funcionalidade, porém com UX própria.

Permitir analisar/gerenciar:

- .NET Framework versões compatíveis;
- Hyper-V;
- Legacy F8 Boot Recovery;
- Legacy Media Components;
- NTFS-related optional functionality quando aplicável;
- Windows Sandbox;
- WSL;
- demais Windows Optional Features relevantes.

Antes de alterar:

detectar estado atual.

Mostrar:

Enabled
Disabled
Unavailable
Requires Restart

=====================================================================
17. FIXES
=====================================================================

Adicionar central de reparos.

Incluir, após validar implementação:

- Network Reset;
- NTP/time synchronization;
- System Corruption Scan;
- SFC;
- DISM;
- Windows Update Reset;
- WinGet repair/reinstall;
- Store repair quando aplicável;
- DNS flush;
- network stack repair;
- component store diagnosis.

Cada ação deve mostrar:

- o que será executado;
- risco;
- duração estimada aproximada sem falsa precisão;
- reboot necessário;
- resultado.

=====================================================================
18. WINDOWS TOOLS
=====================================================================

Criar área "Ferramentas do Windows".

Atalhos para:

- Computer Management
- Control Panel
- Mouse Properties
- Network Connections
- Power Options
- Programs and Features
- Region
- Security and Maintenance
- Sound Settings
- System Properties
- Time and Date
- Windows Defender Firewall
- Windows Restore
- Device Manager
- Task Manager
- Services
- Disk Management
- Event Viewer
- Windows Update
- Optional Features
- Advanced System Properties

=====================================================================
19. DNS
=====================================================================

Criar seletor de DNS.

Detectar configuração atual.

Possíveis perfis após validação:

- Default/Automatic
- Cloudflare
- Google
- Quad9
- AdGuard

Permitir:

- aplicar;
- testar resolução;
- medir latência DNS somente se metodologia for válida;
- restaurar.

Não afirmar que DNS aumenta FPS.

=====================================================================
20. BOREALBOOST MAXIMUM PERFORMANCE POWER PLAN
=====================================================================

Criar um plano próprio:

BorealBoost Maximum Performance

Objetivo:

priorizar desempenho máximo em desktops compatíveis.

O plano NÃO deve simplesmente inventar dezenas de valores agressivos.

Criar baseado nas APIs/comandos oficiais de gerenciamento de energia do Windows.

Detectar:

- desktop;
- notebook;
- CPU;
- disponibilidade de estados;
- plano atual.

Em DESKTOP:

permitir perfil de máxima performance.

Em NOTEBOOK:

não habilitar automaticamente.

Mostrar:

"Este plano aumenta consumo energético e pode aumentar temperatura."

Ao aplicar:

- registrar plano anterior;
- criar/ativar BorealBoost Maximum Performance;
- verificar que foi aplicado;
- permitir rollback.

Não:

- alterar BIOS automaticamente;
- realizar overclock;
- alterar voltagem;
- desativar proteções térmicas.

=====================================================================
21. GPU
=====================================================================

Detectar:

NVIDIA
AMD
Intel

Criar:

GPU Optimization

Modo:

AUTO
NVIDIA
AMD
INTEL

AUTO:

usar GPU detectada.

Manual:

permitir ao técnico selecionar.

Separar:

Windows GPU optimizations

de

vendor-specific optimizations.

Não editar perfis proprietários obscuros sem documentação/validação.

Caso seja necessário recomendar ajuste manual:

mostrar orientação, não falsificar automação.

=====================================================================
22. GAMING OPTIMIZATION
=====================================================================

Otimização independente de jogo.

Avaliar tecnicamente:

- Game Mode;
- Game DVR;
- Xbox-related background functionality;
- fullscreen behavior;
- graphics preferences;
- HAGS quando suportado;
- background processes;
- power profile;
- startup;
- GPU scheduling;
- overlays;
- capture features;
- unnecessary apps;
- memory pressure;
- storage;
- driver state.

Não presumir que:

- HAGS é sempre melhor;
- desabilitar fullscreen optimization é sempre melhor;
- qualquer registry tweak reduz input lag.

Criar Compatibility + Recommendation Rules.

=====================================================================
23. SERVIÇOS
=====================================================================

Criar Service Analyzer.

Nunca usar:

"desabilitar todos os serviços desnecessários"

sem definição.

Catalogar serviços em:

Critical
Core
Conditional
Optional
ThirdParty
Unknown

Não alterar:

Critical
Unknown

automaticamente.

As regras podem considerar:

- notebook;
- impressora;
- Bluetooth;
- Xbox;
- Hyper-V;
- WSL;
- biometria;
- touchscreen;
- Wi-Fi;
- VPN;
- domínio corporativo;
- RDP;
- Store;
- OneDrive.

Isso evita quebrar funções necessárias ao cliente.

=====================================================================
24. DEBLOAT
=====================================================================

Criar scanner de apps.

Categorias:

Recommended to keep
Optional
Safe to remove
Advanced removal
Never auto-remove

Mostrar aplicação antes de remover.

Permitir seleção.

Nunca remover cegamente componentes essenciais.

Registrar:

- package;
- versão;
- comando;
- resultado.

=====================================================================
25. WINDOWS UPDATE
=====================================================================

Criar uma área dedicada.

Mostrar:

- estado;
- serviço;
- reboot pendente;
- updates quando possível por APIs adequadas.

Permitir:

- abrir Windows Update;
- reparar componentes;
- configurar opções suportadas;
- detectar problemas.

Não desativar permanentemente updates de segurança no preset padrão.

Alterações agressivas de update:

apenas Advanced/Custom.

=====================================================================
26. RESTORE POINT
=====================================================================

OBRIGATÓRIO.

Antes de qualquer otimização relevante:

Etapa:

"Criando ponto de restauração"

Mostrar progress UI.

Exemplo:

Preparando segurança
██████████████░░░░
72%

IMPORTANTE:

Não inventar percentual caso a API do Windows não forneça progresso real.

Se o processo não fornecer percentual mensurável:

usar etapas reais:

1/4 Verificando System Restore
2/4 Preparando snapshot
3/4 Criando restore point
4/4 Validando

A barra pode representar progresso por etapas, documentando isso.

Nome:

BorealBoost - Pre Optimization - YYYY-MM-DD HH-mm

Verificar se a criação foi bem-sucedida.

Se falhar:

não continuar silenciosamente.

Mostrar:

"Não foi possível criar o ponto de restauração."

Permitir:

- tentar novamente;
- corrigir System Restore;
- continuar somente mediante confirmação explícita do técnico, se a política permitir.

=====================================================================
27. SNAPSHOT E ROLLBACK
=====================================================================

Ponto de restauração sozinho não basta.

Antes de aplicar alterações:

capturar estado anterior de cada item.

Snapshot:

- registry values;
- services startup type;
- power plan;
- DNS;
- Windows features;
- app configuration;
- relevant policies;
- settings modified.

Salvar localmente.

Rollback deve funcionar por sessão.

Tela:

Restauração

Mostrar:

Otimização de 12/08/2026 15:32
31 alterações

[VER DETALHES]
[REVERTER]

Permitir:

- reverter alteração individual;
- reverter sessão completa.

Após rollback:

executar Verify.

=====================================================================
28. TRANSACTION-LIKE EXECUTION
=====================================================================

Implementar plano de execução antes de modificar o sistema.

Fluxo:

Analyze
↓
Plan
↓
Compatibility Check
↓
Snapshot
↓
Restore Point
↓
Execute
↓
Verify
↓
Commit Session

Se algo crítico falhar:

interromper.

Registrar falha.

Não continuar cegamente aplicando ajustes dependentes.

Onde possível:

rollback das alterações da execução atual.

=====================================================================
29. PROGRESSO DA OTIMIZAÇÃO
=====================================================================

Tela full workflow.

Título:

"Otimizando seu computador"

Mostrar:

42%

Abaixo:

"Configurando plano de energia..."

Lista:

✓ Ponto de restauração criado
✓ Snapshot criado
✓ Serviços analisados
✓ Startup otimizado
→ Configurando energia
○ Aplicando Gaming
○ Limpando arquivos temporários
○ Validando alterações

Mostrar:

X / Y otimizações concluídas.

Não congelar UI.

Operações longas:

async/background.

Permitir log expansível.

Não permitir fechar a aplicação no meio sem aviso.

=====================================================================
30. LOG
=====================================================================

Logging é obrigatório.

Cada ação deve possuir:

timestamp
sessionId
optimizationId
category
action
oldValue
newValue
command/API
result
duration
error

Nunca registrar:

- senhas;
- tokens;
- secrets.

Criar:

Logs
└── Sessions
    └── session-YYYYMMDD-HHmmss.json/log

Tela Log:

- filtro;
- pesquisa;
- sucesso;
- aviso;
- falha;
- exportar.

=====================================================================
31. PERFORMANCE BASELINE
=====================================================================

Antes da otimização, coletar baseline.

Somente métricas reproduzíveis.

Possíveis:

- processos;
- serviços;
- RAM idle;
- CPU idle;
- startup apps;
- tempo de boot obtido de fonte adequada;
- espaço;
- driver status;
- power plan;
- indicadores de integridade.

Para FPS:

NÃO inventar um benchmark interno genérico se ele não representa jogos.

Se implementar benchmark gráfico:

documentar metodologia.

Idealmente separar:

System Score

de

Gaming Benchmark.

=====================================================================
32. ANTES x DEPOIS
=====================================================================

Criar tela premium:

RESULTADOS

Antes                 Depois

Boreal Score
64                     82

RAM Idle
5.2 GB                 3.9 GB

Processos
178                    132

Startup Apps
14                     6

Problemas de Driver
3                      0

Power Plan
Balanced               BorealBoost Maximum Performance

Otimizações aplicadas:
29

Requer reinicialização:
Sim

Nunca mostrar um ganho não medido.

=====================================================================
33. FPS / GAMING RESULTS
=====================================================================

Se houver dados legítimos de benchmark:

mostrar:

Average FPS
1% Low
0.1% Low quando metodologicamente válido
frametime médio
frametime percentiles quando válido

Antes x Depois.

Não dizer:

"+35% FPS"

a menos que esse resultado tenha sido realmente medido naquela máquina.

=====================================================================
34. BOREAL SCORE
=====================================================================

Criar algoritmo transparente.

Exemplo conceitual:

System Health
Startup
Background Load
Drivers
Storage
Power
Gaming Configuration
Memory Pressure

Não utilizar número arbitrário.

Criar documento:

BOREAL_SCORE_METHODOLOGY.md

Explicar:

- inputs;
- pesos;
- normalização;
- limitações.

=====================================================================
35. RELATÓRIO DO CLIENTE
=====================================================================

Criar:

BorealBoost Performance Report

Cabeçalho:

BorealBoost
Optimization Report

Dados:

Cliente:
Técnico:
Data:
Windows:
CPU:
GPU:
RAM:
Storage:

Antes:
...

Depois:
...

Otimizações:
...

Drivers:
...

Avisos:
...

Resultado:
...

Exportar:

PDF
HTML

PDF visualmente profissional.

Permitir logo BorealBoost.

=====================================================================
36. TECHNICIAN MODE
=====================================================================

Como o sistema é exclusivo para o técnico, adicionar uma camada operacional simples.

Antes da análise permitir:

Nome do cliente
Observações
Tipo de uso

Tipo de uso:

Gaming
Gaming + Uso Geral
Trabalho
Uso Geral
Low-End PC

Esses dados devem alimentar recomendações, mas não aplicar tweaks perigosos automaticamente.

=====================================================================
37. INSTALAÇÃO
=====================================================================

O BorealBoost deve ser instalável.

Criar instalador profissional.

Avaliar:

MSIX
MSI
Inno Setup
WiX Toolset

Escolher justificadamente.

Instalação:

- Start Menu;
- desktop shortcut opcional;
- uninstall;
- versão;
- publisher;
- icon;
- arquivos em local apropriado.

Dados:

ProgramData/AppData conforme natureza.

Logs/snapshots não devem ficar misturados no diretório de binários.

=====================================================================
38. ADMINISTRADOR
=====================================================================

A aplicação requer privilégios administrativos para as operações principais.

Implementar UAC corretamente.

Não pedir elevação repetidamente a cada comando.

Verificar ao iniciar fluxo que requer admin.

Mostrar status:

Administrador
✓ Ativo

ou

Administrador
! Necessário

=====================================================================
39. INTERNET
=====================================================================

Internet é permitida.

Usos:

- verificar versão;
- driver metadata;
- fontes oficiais;
- atualizações futuras do BorealBoost.

Não baixar e executar script remoto arbitrário.

Nunca implementar em produção algo equivalente a:

baixar texto da internet e executar diretamente sem validação.

Downloads executáveis:

- HTTPS;
- domínio permitido;
- hash quando disponível;
- assinatura;
- validação;
- timeout;
- tratamento de erro.

=====================================================================
40. UPDATE DO BOREALBOOST
=====================================================================

Preparar arquitetura para atualizações futuras.

V1 pode apenas:

"Verificar atualizações"

Mas o design precisa possibilitar:

- manifest;
- versão;
- release notes;
- assinatura;
- hash;
- rollback de update.

=====================================================================
41. TELA OTIMIZAÇÃO
=====================================================================

Criar UX própria inspirada conceitualmente nas funcionalidades vistas nas capturas, mas modernizada.

Topo:

Otimização

Cards de preset:

BÁSICO
Seguro

MÉDIO
Recomendado

AVANÇADO
Máximo desempenho

PERSONALIZADO
Controle total

Selecionar preset deve mostrar:

- quantidade de otimizações;
- risco;
- reboot;
- categorias afetadas.

Abaixo:

"Alterações selecionadas"

Com busca:

Pesquisar otimização...

Filtros:

Todos
Safe
Advanced
Aggressive
Experimental

Categorias laterais ou chips.

Cada item:

Nome
Descrição
Impacto
Risco
Compatibilidade
Estado atual

Toggle.

Botão:

VER DETALHES

Detalhes:

O que faz
Por que pode melhorar
O que será alterado
Compatibilidade
Riscos
Como desfazer

CTA:

APLICAR 27 OTIMIZAÇÕES

=====================================================================
42. TELA DRIVERS
=====================================================================

Título:

Drivers

Status:

✓ 18 drivers corretos
! 2 requerem atenção
× 1 ausente

Cards:

GPU
Chipset
Network
Audio
Other

Mostrar:

Installed
Missing
Attention
Update Available

Não usar cores alarmistas sem motivo.

=====================================================================
43. TELA PERSONALIZADO
=====================================================================

Essa tela deve oferecer controle semelhante em abrangência às capturas de referência, porém melhor organizada.

Não colocar 70 checkboxes numa página enorme.

Usar:

- categorias;
- pesquisa;
- filtros;
- cards compactos;
- accordions;
- detalhes;
- toggles.

Permitir:

Selecionar todos seguros
Limpar seleção
Detectar aplicados
Restaurar selecionados

=====================================================================
44. GET INSTALLED / DETECT APPLIED
=====================================================================

Criar função:

"Detectar otimizações aplicadas"

Ela deverá verificar o estado real.

Não considerar apenas histórico do BorealBoost.

Quando possível, detectar:

- Registry;
- Services;
- Features;
- Power;
- DNS;
- Settings.

Estados:

Applied
Not Applied
Partially Applied
Unknown

=====================================================================
45. UNDO SELECTED
=====================================================================

Criar:

"Reverter selecionados"

Não usar um valor padrão arbitrário como undo.

Sempre que possível, restaurar valor capturado no snapshot.

Se alteração foi aplicada antes da existência de snapshot:

usar default oficial somente se houver certeza.

Caso contrário:

informar impossibilidade de restauração exata.

=====================================================================
46. SEGURANÇA
=====================================================================

Não permitir que busca por performance comprometa irresponsavelmente:

- Defender;
- Firewall;
- Secure Boot;
- BitLocker;
- Windows Update;
- UAC;
- Credential Guard;
- VBS;
- Memory Integrity.

Se houver otimização potencialmente relacionada:

Advanced/Experimental

Explicar claramente:

- benefício hipotético;
- impacto de segurança;
- compatibilidade;
- reversão.

Exigir confirmação individual quando risco for significativo.

Nunca desabilitar proteções críticas escondido dentro de preset genérico.

=====================================================================
47. NÃO FAZER
=====================================================================

NÃO:

- inventar ganho de FPS;
- inventar benchmark;
- desabilitar Defender silenciosamente;
- desabilitar Firewall silenciosamente;
- executar scripts remotos sem validação;
- baixar drivers de sites desconhecidos;
- mexer em BIOS;
- fazer overclock;
- alterar tensão;
- desativar proteção térmica;
- deletar registry indiscriminadamente;
- remover serviços desconhecidos;
- remover componentes críticos;
- copiar interface do WinUtil;
- transformar a aplicação em wrapper de PowerShell;
- espalhar comandos shell pela UI;
- colocar toda lógica no ViewModel;
- ignorar erros;
- ignorar rollback;
- marcar uma operação como sucesso sem Verify;
- usar try/catch vazio;
- usar comandos mágicos não documentados.

=====================================================================
48. POWERSHELL
=====================================================================

PowerShell pode ser utilizado como adapter quando for a melhor ferramenta nativa.

Entretanto:

não construir toda a solução como centenas de strings PowerShell dentro de C#.

Criar abstração:

ISystemOperation
IRegistryOperation
IServiceOperation
IFeatureOperation
IPowerOperation
IPackageOperation

Cada execução deve retornar resultado estruturado.

Exemplo conceitual:

OperationResult
- Success
- ExitCode
- StdOut
- StdErr
- Duration
- RequiresRestart

=====================================================================
49. ERROR HANDLING
=====================================================================

Cada operação deve tratar:

- acesso negado;
- chave ausente;
- serviço inexistente;
- recurso indisponível;
- timeout;
- internet offline;
- reboot pendente;
- driver failure;
- processo bloqueado.

Não exibir stack trace bruto ao usuário.

UI:

"Não foi possível aplicar esta otimização."

Detalhes técnicos:

expansíveis.

Log:

erro completo.

=====================================================================
50. OBSERVABILIDADE
=====================================================================

Implementar:

structured logging;
session ID;
operation ID;
duration;
outcomes.

Modo desenvolvimento:

Debug logs.

Modo produção:

logs adequados sem informações sensíveis.

=====================================================================
51. TESTES
=====================================================================

Criar testes:

UNIT

- recommendation rules;
- compatibility rules;
- preset composition;
- Boreal Score;
- parsing;
- snapshot serialization.

INTEGRATION

- Registry;
- services;
- power;
- features;
- system detection.

SYSTEM

Executar em máquinas virtuais:

Windows 10
Windows 11

Cobrir builds suportadas.

Criar snapshots da VM antes dos testes destrutivos.

TESTE DE ROLLBACK:

obrigatório.

Para toda otimização reversível:

Before
Apply
Verify
Undo
Verify Original State

=====================================================================
52. MATRIZ DE TESTES
=====================================================================

Testar pelo menos cenários conceituais:

Win10 Desktop Intel + NVIDIA
Win10 Desktop AMD + AMD
Win11 Desktop AMD + NVIDIA
Win11 Desktop Intel + NVIDIA
Win11 Desktop AMD + AMD
Win11 Intel iGPU
Win11 AMD iGPU
Win11 Notebook Intel
Win11 Notebook AMD

Não é necessário possuir fisicamente todas essas máquinas no primeiro commit.

Documentar:

- VM;
- hardware real necessário;
- pendente;
- validado.

=====================================================================
53. PERFORMANCE TESTING
=====================================================================

Para cada tweak que afirma melhorar performance:

documentar hipótese.

Exemplo:

Optimization:
Disable X background feature

Hypothesis:
reduz atividade em background.

Expected measurable impact:
menor CPU/IO em determinada condição.

Validation:
A/B test.

Se não demonstrável:

classificar como UX/Privacy/System, não FPS.

=====================================================================
54. DOCUMENTAÇÃO
=====================================================================

Gerar:

README.md

ARCHITECTURE.md

ARCHITECTURE_DECISION_RECORD.md

SECURITY.md

OPTIMIZATION_ENGINE.md

OPTIMIZATION_CATALOG.md

COMPATIBILITY_MATRIX.md

TESTING.md

ROLLBACK.md

BOREAL_SCORE_METHODOLOGY.md

DRIVER_MANAGEMENT.md

REPORTING.md

WINUTIL_ANALYSIS.md

THIRD_PARTY_NOTICES.md

RELEASE_CHECKLIST.md

=====================================================================
55. PRODUCTION READINESS
=====================================================================

Antes de declarar V1 pronta:

validar:

Security
Logging
Error handling
Rollback
Installer
Update strategy
Crash behavior
Compatibility
Windows restart handling
Driver handling
Snapshots
Restore Point
Reports
Performance
Accessibility
Documentation

Criar:

PRODUCTION_READINESS.md

com checklist objetivo.

=====================================================================
56. UI/UX
=====================================================================

A interface desktop deve ser responsiva dentro das dimensões suportadas da janela.

Definir:

- tamanho mínimo;
- comportamento ao maximizar;
- DPI scaling;
- 100%;
- 125%;
- 150%;
- 200%.

Suportar monitores Full HD e superiores adequadamente.

Não permitir:

- texto cortado;
- cards sobrepostos;
- scroll quebrado;
- botões fora da tela.

Componentes reutilizáveis.

Tipografia consistente.

Spacing system consistente.

Estados obrigatórios:

Loading
Empty
Success
Warning
Error
Disabled

Acessibilidade:

- contraste;
- teclado;
- focus state;
- tooltips;
- labels;
- ícones acompanhados de contexto quando necessário.

=====================================================================
57. SIDEBAR
=====================================================================

Estrutura sugerida:

BorealBoost

Dashboard
Análise
Otimização
Drivers
Personalizado
Ferramentas
Resultados
Restauração
Logs

Configurações

Rodapé:

BorealBoost
v1.0.0

Admin ✓

=====================================================================
58. ANÁLISE
=====================================================================

Tela de análise deve visualizar o scan acontecendo.

Exemplo:

Analisando computador...

✓ Windows
✓ CPU
✓ GPU
✓ Memória
→ Drivers
○ Serviços
○ Inicialização
○ Armazenamento
○ Energia

Ao terminar:

"Análise concluída"

"Encontramos 24 oportunidades."

=====================================================================
59. RECOMMENDATION ENGINE
=====================================================================

Não simplesmente dizer:

se Advanced => habilitar tudo.

Criar regras.

Exemplos conceituais:

IF device.isLaptop
THEN disable desktop-only maximum power tweaks

IF noBluetoothHardware AND noBluetoothUsage
THEN optional service recommendation

IF HyperVEnabled
THEN don't recommend disabling virtualization-dependent services blindly

IF StorageType == HDD
THEN recommendations differ from NVMe

IF NVIDIA
THEN NVIDIA rule set

IF Windows11 build X
THEN applicable rules

IF Windows10
THEN Windows10 catalog

Recommendation deve informar:

Recommended
Optional
Not Recommended
Incompatible

=====================================================================
60. EXECUTION PLAN
=====================================================================

Antes do usuário confirmar:

gerar plano.

Exemplo:

27 alterações

Safe: 14
Advanced: 8
Aggressive: 5

Reinicialização: necessária

Estimativa:
não apresentar tempo preciso sem base.

Mostrar lista.

CTA:

CRIAR PONTO DE RESTAURAÇÃO E OTIMIZAR

=====================================================================
61. LICENCIAMENTO DE TERCEIROS
=====================================================================

Antes de incorporar:

- WinUtil;
- O&O ShutUp10++;
- bibliotecas;
- executáveis;
- scripts;
- ícones;
- fontes;

verificar licença atual.

Não redistribuir ferramenta sem permissão de licença.

Para MIT:

preservar notices aplicáveis.

Criar THIRD_PARTY_NOTICES.md.

=====================================================================
62. FASES DO PROJETO
=====================================================================

NÃO tente implementar o aplicativo inteiro numa única alteração desorganizada.

FASE 0 — DISCOVERY

Entregar:

- requisitos;
- escopo;
- riscos;
- decisões pendentes;
- análise WinUtil;
- arquitetura proposta.

NÃO programar ainda.

FASE 1 — FOUNDATION

- solution;
- projetos;
- DI;
- logging;
- navigation;
- theme;
- shell;
- models;
- system info.

FASE 2 — SCANNER

- OS;
- CPU;
- GPU;
- RAM;
- storage;
- drivers;
- services;
- startup;
- power.

FASE 3 — OPTIMIZATION ENGINE

- catalog;
- compatibility;
- detection;
- apply;
- verify;
- undo;
- presets.

FASE 4 — SAFETY

- snapshots;
- restore points;
- rollback;
- transaction/session.

FASE 5 — OPTIMIZATIONS

Adicionar em lotes.

Nunca adicionar 100 tweaks sem teste.

Cada lote:

implementação
test
documentation
rollback verification

FASE 6 — DRIVERS

- detection;
- official source resolution;
- install flow.

FASE 7 — BENCHMARK/RESULTS

- baseline;
- comparison;
- Boreal Score;
- reporting.

FASE 8 — INSTALLER

- packaging;
- signing preparation;
- uninstall.

FASE 9 — HARDENING

- integration tests;
- VMs;
- error cases;
- production readiness.

=====================================================================
63. V1 — PRIORIDADES
=====================================================================

A V1 deve ser vendável e utilizável em clientes reais.

Priorizar:

1. ótima interface;
2. diagnóstico confiável;
3. presets;
4. engine modular;
5. restore point;
6. snapshot;
7. rollback;
8. logs;
9. Windows 10/11 compatibility;
10. power plan;
11. services;
12. startup;
13. debloat;
14. privacy;
15. gaming;
16. drivers;
17. fixes;
18. reports;
19. results before/after.

Não sacrificar estabilidade para colocar 500 tweaks na primeira versão.

=====================================================================
64. CRITÉRIOS DE SUCESSO
=====================================================================

O projeto só pode ser considerado pronto quando:

- instala corretamente;
- abre corretamente;
- solicita admin corretamente;
- detecta Win10/Win11;
- detecta desktop/notebook;
- identifica hardware;
- identifica drivers problemáticos;
- presets funcionam;
- Personalizado funciona;
- cada tweak possui compatibilidade;
- cada tweak possui risco;
- alterações importantes são reversíveis;
- restore point funciona;
- snapshot funciona;
- rollback funciona;
- logs registram antes/depois;
- UI não trava durante otimização;
- progresso é verdadeiro ou baseado em etapas reais;
- reports funcionam;
- nenhuma métrica de performance é inventada;
- nenhum download inseguro é executado;
- testes essenciais passam;
- documentação existe.

=====================================================================
65. CRITÉRIO ESPECIAL — QUALIDADE DE OTIMIZAÇÃO
=====================================================================

O BorealBoost deve ir além das configurações superficiais recomendadas pelo Windows.

Isso significa que você deve pesquisar e avaliar:

- Windows Internals;
- políticas;
- serviços;
- scheduler-related configuration somente quando documentada;
- power management;
- background components;
- Game Mode;
- capture;
- networking;
- storage;
- startup;
- packages;
- Windows features;
- GPU;
- drivers.

Entretanto:

"ir além" NÃO significa executar tweaks obscuros sem evidência.

O objetivo é:

máximo desempenho justificável
+
máxima segurança operacional possível.

=====================================================================
66. AUDITORIA DE CADA TWEAK
=====================================================================

Antes de adicionar qualquer tweak ao catálogo, responder:

1. O que muda?
2. Onde muda?
3. Qual API/comando?
4. Qual valor atual?
5. Qual novo valor?
6. Por que isso pode ajudar?
7. Existe documentação?
8. Windows 10?
9. Windows 11?
10. Qual build?
11. Desktop?
12. Notebook?
13. Dependências?
14. Conflitos?
15. Risco?
16. Precisa reboot?
17. Como detectar aplicado?
18. Como verificar?
19. Como desfazer?
20. Como testar?

Se essas perguntas não puderem ser respondidas:

não adicionar ao preset automático.

=====================================================================
67. INFORMAÇÕES A CONFIRMAR
=====================================================================

Não interrompa o planejamento por esses itens.

Adote defaults seguros e registre-os como pendências.

Confirmar posteriormente:

- logo definitivo;
- ícone definitivo;
- nome do publisher;
- certificado de assinatura de código;
- diretório/empresa para instalador;
- política de retenção de logs;
- formato final do relatório;
- hardware físico disponível para testes;
- builds exatas do Windows 10 oficialmente suportadas pelo BorealBoost;
- builds exatas do Windows 11 oficialmente suportadas;
- estratégia futura de auto-update;
- eventual banco local SQLite ou arquivos estruturados para sessões;
- necessidade futura de licença comercial/autenticação.

=====================================================================
68. PRIMEIRA EXECUÇÃO DO AGENTE
=====================================================================

IMPORTANTE:

NÃO COMECE ESCREVENDO CENTENAS DE ARQUIVOS.

Na primeira etapa:

1. analise todos os requisitos deste documento;
2. analise as imagens de referência;
3. analise a versão atual oficial do WinUtil;
4. analise os arquivos declarativos relevantes do WinUtil;
5. identifique funcionalidades incorporáveis;
6. verifique licenças;
7. estude compatibilidade Windows 10/11;
8. escolha stack;
9. desenhe arquitetura;
10. desenhe modelo de domínio;
11. desenhe Optimization Engine;
12. desenhe Rollback Engine;
13. desenhe Driver Engine;
14. desenhe UX;
15. produza roadmap.

Entregue primeiro:

DISCOVERY.md
REQUIREMENTS.md
WINUTIL_ANALYSIS.md
ARCHITECTURE.md
ARCHITECTURE_DECISION_RECORD.md
DOMAIN_MODEL.md
OPTIMIZATION_ENGINE.md
COMPATIBILITY_MATRIX.md
UX_SPECIFICATION.md
SECURITY.md
IMPLEMENTATION_ROADMAP.md

Depois disso:

pare e apresente um resumo das decisões arquiteturais.

Não implemente a aplicação até que essa fase esteja coerente.

=====================================================================
69. SEGUNDA EXECUÇÃO — APÓS ARQUITETURA
=====================================================================

Depois que a arquitetura estiver validada:

implemente fase por fase.

A cada fase:

1. indicar objetivo;
2. arquivos criados;
3. arquivos modificados;
4. decisões;
5. implementação;
6. testes;
7. resultado;
8. problemas;
9. riscos;
10. próximo passo.

Nunca esconder falha.

=====================================================================
70. RESULTADO ESPERADO
=====================================================================

O produto final deverá parecer uma ferramenta comercial premium.

O cliente deverá visualizar algo semelhante a:

BorealBoost

PC analisado:
Ryzen 7 5700X
RTX 4060 Ti
32 GB RAM
Windows 11 Pro

Boreal Score:
67

Recomendação:
PERFORMANCE — MÉDIO

Oportunidades:
31

Drivers:
1 requer atenção

[REVISAR]
[OTIMIZAR]

Após execução:

Optimization Complete

Boreal Score
67 → 84

RAM Idle
4.8 GB → 3.6 GB

Startup
12 → 5

Drivers
1 problema → 0

Otimizações aplicadas
31

Restore Point
✓

Snapshot
✓

Rollback
Disponível

[VER RELATÓRIO]
[EXPORTAR PDF]
[FINALIZAR]

O BorealBoost não deve vender "magia".

Ele deve demonstrar:

diagnóstico
+
otimização
+
evidência
+
segurança
+
resultado.

Construa-o como um produto que será usado em computadores reais de clientes e pelo qual um profissional cobrará dinheiro.

Qualidade, reversibilidade e confiança são requisitos de negócio, não funcionalidades opcionais.
```
