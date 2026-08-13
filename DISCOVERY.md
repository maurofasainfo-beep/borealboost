# BorealBoost - Discovery

Data da analise: 2026-08-12
Escopo: Fase 0 - discovery, arquitetura e planejamento. Nenhum codigo foi implementado.

## Escopo da sessao

Esta sessao executa apenas o que esta definido em `CURRENT_TASK.md`:

- leitura integral de `BOREALBOOST_MASTER_SPEC.md`, `CODEX_BOOTSTRAP.md` e `CURRENT_TASK.md`;
- analise da estrutura atual do repositorio;
- pesquisa tecnica do WinUtil oficial;
- pesquisa em documentacao oficial da Microsoft;
- definicao de arquitetura, dominio, UX, mecanismos de otimizacao, drivers, rollback, compatibilidade e roadmap;
- registro de riscos e pendencias.

## Estado atual do repositorio

Arquivos encontrados na raiz:

- `BOREALBOOST_MASTER_SPEC.md`
- `CODEX_BOOTSTRAP.md`
- `CURRENT_TASK.md`

Nao foram encontrados:

- solution `.sln`;
- projetos .NET;
- codigo de aplicacao;
- diretorio `/docs`;
- testes;
- instalador;
- assets visuais;
- catalogo de otimizacoes;
- scripts operacionais.

Conclusao: o projeto esta em estado inicial de especificacao. A Fase 0 ainda nao tinha sido materializada em documentos arquiteturais antes desta sessao.

## Diagnostico inicial exigido pelo bootstrap

### Estado atual

- Arquitetura encontrada: inexistente em codigo; apenas especificacao.
- Modulos existentes: nenhum modulo implementado.
- Stack: ainda nao definida em artefatos do repositorio.
- Estado do projeto: pre-implementacao.

### Relacao com o Master Spec

- Discovery e arquitetura: em execucao nesta sessao.
- Aplicacao desktop: nao implementado.
- Scanner: nao implementado.
- Optimization Engine: nao implementado.
- Driver Engine: nao implementado.
- Rollback: nao implementado.
- Boreal Score: nao implementado.
- UX: nao implementado.
- Testes: nao implementado.
- Documentacao: criada nesta sessao.

### Riscos encontrados

- Arquitetura: ausencia de decisoes formais antes desta sessao.
- Seguranca: produto pretende modificar Windows real; exige modelo de privilegio, snapshot, logs e politicas de bloqueio.
- Windows 10: suporte publico de Windows 10 22H2 terminou em 2025-10-14 para edicoes gerais; precisa ser tratado como target legado/ESU.
- Windows 11: builds 23H2/24H2/25H2/26H1 tem diferencas de suporte e disponibilidade; regras por build sao obrigatorias.
- Rollback: restore point sozinho nao basta e possui limitacoes operacionais.
- Drivers: risco alto de baixar/instalar drivers de fontes inseguras.
- Performance: risco de prometer FPS sem benchmark real.
- Manutencao: risco de virar wrapper de PowerShell ou lista de tweaks nao auditados.

### Plano da sessao

1. Alterar somente documentacao Markdown na raiz.
2. Justificar arquitetura e stack antes de qualquer codigo.
3. Criar os entregaveis obrigatorios de Fase 0.
4. Registrar fontes, riscos, pendencias e roadmap.
5. Validar por revisao dos documentos e inventario de arquivos, sem build/runtime.

## Pesquisa WinUtil

Fonte oficial pesquisada: `https://github.com/ChrisTitusTech/winutil`

Estado verificado em 2026-08-12:

- branch `main`: commit `aee3e7a1f4a3249ff2f95e75b5bd3768626a21b6`;
- release mais recente vista no GitHub: `26.08.04`;
- licenca: MIT, copyright CT Tech Group LLC.

Arquivos analisados:

- `README.md`
- `SPEC.md`
- `AGENTS.md`
- `LICENSE`
- `config/tweaks.json`
- `config/preset.json`
- `config/feature.json`
- `config/dns.json`
- `config/applications.json`
- `config/appx.json`
- funcoes publicas/privadas de tweaks, undo, restore point, DNS, features, updates, install, O&O ShutUp10++ e deteccao de estado.

Resumo quantitativo do WinUtil no snapshot analisado:

- `tweaks.json`: 67 entradas.
- `feature.json`: 33 entradas.
- `dns.json`: 14 provedores/perfis.
- `applications.json`: 227 aplicacoes.
- `appx.json`: 33 apps/provisioned packages.
- `preset.json`: 4 grupos (`Standard`, `Minimal`, `Advanced`, `AppxDefault`).

Conclusao: WinUtil e referencia funcional util para categorias, configuracao declarativa e fluxos de apply/undo, mas nao deve ser usado como arquitetura do BorealBoost. O BorealBoost deve ser uma aplicacao .NET modular, com modelo de dominio proprio, compatibilidade explicita e engine transacional.

## Pesquisa Microsoft

Fontes oficiais consultadas:

- .NET support policy: `https://dotnet.microsoft.com/en-us/platform/support/policy`
- WinUI 3: `https://learn.microsoft.com/en-us/windows/apps/winui/winui3/`
- WPF overview: `https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/`
- Windows 10 lifecycle: `https://learn.microsoft.com/en-us/lifecycle/products/windows-10-enterprise-and-education`
- Windows 11 release information: `https://learn.microsoft.com/en-us/windows/release-health/windows11-release-information`
- Registry API: `https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry`
- ServiceController: `https://learn.microsoft.com/en-us/dotnet/api/system.serviceprocess.servicecontroller`
- Powercfg: `https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options`
- DISM features: `https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/enable-or-disable-windows-features-using-dism?view=windows-11`
- PnPUtil: `https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/pnputil-command-syntax`
- SetupAPI: `https://learn.microsoft.com/en-us/windows-hardware/drivers/install/setupapi`
- Windows Update Agent API: `https://learn.microsoft.com/en-us/windows/win32/wua_sdk/using-the-windows-update-agent-api`
- Checkpoint-Computer: `https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/checkpoint-computer?view=powershell-5.1`
- WMI/CIM OS and computer system: `https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-operatingsystem`, `https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-computersystem`
- PerformanceCounter: `https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.performancecounter`
- ETW: `https://learn.microsoft.com/en-us/windows/win32/etw/about-event-tracing`
- DNS client cmdlet: `https://learn.microsoft.com/en-us/powershell/module/dnsclient/set-dnsclientserveraddress?view=windowsserver2025-ps`
- Storage Sense: `https://learn.microsoft.com/en-us/windows/configuration/storage/storage-sense`
- Game Mode: `https://learn.microsoft.com/en-us/previous-versions/windows/desktop/gamemode/game-mode-portal`
- Hardware Accelerated GPU Scheduling: `https://devblogs.microsoft.com/directx/hardware-accelerated-gpu-scheduling/`

## Decisoes principais

- Stack recomendada: C# + .NET 10 LTS + WinUI 3/Windows App SDK.
- Empacotamento recomendado para V1: app x64 unpackaged/packaged-with-external-location com MSI via WiX Toolset, avaliando MSIX apenas depois por causa de requisitos de elevacao e operacao presencial.
- Modelo de privilegio recomendado: UI principal + agente elevado por sessao, sem UAC por comando.
- Motor de otimizacao: catalogo declarativo proprio, com Detection, Compatibility, Apply, Verify, Undo, Snapshot, EvidenceLevel e RiskLevel.
- Fonte primaria de verdade para tweaks: documentacao oficial do Windows e validacao em VM, nao popularidade em internet.
- WinUtil: referencia funcional e de categorias; nenhum codigo ou identidade foi incorporado nesta fase.

## Inconsistencias e lacunas

- O Master Spec pede analise de imagens de referencia, mas nenhuma imagem foi encontrada no repositorio nem fornecida nesta sessao.
- O Master Spec lista `BOREAL_SCORE_METHODOLOGY.md`, enquanto `CURRENT_TASK.md` exige `BOREAL_SCORE.md`. Esta sessao cria `BOREAL_SCORE.md`.
- O Master Spec lista alguns documentos extras futuros (`TESTING.md`, `ROLLBACK.md`, `REPORTING.md`, `PRODUCTION_READINESS.md`) que nao estao no conjunto obrigatorio de `CURRENT_TASK.md`; eles ficam para roadmap.
- Windows 10 e requisito de negocio, mas ja esta fora do suporte geral Microsoft para 22H2 desde 2025-10-14; a arquitetura deve suportar, mas o produto precisa comunicar risco operacional.

## Pendencias

- Receber imagens de referencia citadas no Master Spec.
- Confirmar logo, icone, publisher, certificado de assinatura e nome de empresa para instalador.
- Confirmar se Windows 10 sera suportado apenas em 22H2 x64/ESU ou tambem em LTSC especificas.
- Confirmar hardware real disponivel para matriz de testes.
- Confirmar formato final do relatorio PDF/HTML e campos comerciais do tecnico.
- Validar em VM antes de aprovar qualquer tweak real.

