# BorealBoost - WinUtil Analysis

Data: 2026-08-12
Fonte: `https://github.com/ChrisTitusTech/winutil`

## Snapshot pesquisado

- Branch: `main`.
- Commit verificado: `aee3e7a1f4a3249ff2f95e75b5bd3768626a21b6`.
- Release mais recente observada no GitHub: `26.08.04`.
- Licenca: MIT, copyright CT Tech Group LLC.

Nenhum codigo, UI, marca ou asset do WinUtil foi incorporado ao BorealBoost nesta sessao.

## Arquitetura do WinUtil

WinUtil e uma utility Windows em PowerShell com UI WPF. O repositorio e modular durante desenvolvimento, mas o artefato distribuido e um `winutil.ps1` compilado a partir de:

- `scripts/start.ps1`;
- `functions/public`;
- `functions/private`;
- `config/*.json`;
- `xaml/inputXML.xaml`;
- `tools/autounattend.xml`;
- `scripts/main.ps1`.

Estado runtime:

- usa estado compartilhado `$sync`;
- usa WPF/XAML;
- usa runspaces para operacoes longas;
- usa JSON declarativo para apps, tweaks, features, DNS e presets.

## Configuracoes analisadas

Contagem no snapshot:

- 67 tweaks;
- 33 features/fixes/tools;
- 14 DNS profiles;
- 227 apps;
- 33 AppX/provisioned apps;
- presets `Standard`, `Minimal`, `Advanced`, `AppxDefault`.

Categorias principais de tweaks:

- Essential Tweaks;
- Advanced Tweaks - caution;
- Customize Preferences;
- Performance Plans - not for laptops.

Categorias de features:

- Features;
- Fixes;
- Legacy Windows Panels;
- PowerShell Profile;
- Remote Access.

## Mecanismos uteis ao BorealBoost

Incorporar conceitualmente, nao copiar:

- catalogo declarativo para escolhas de UI;
- presets que referenciam IDs;
- undo metadata (`OriginalValue`, `OriginalType`);
- deteccao de estado aplicado por registry/service;
- separacao entre tweaks, features, DNS, apps e AppX;
- runspace/background para evitar UI travada;
- logging por componente;
- restauracao de configuracoes de Windows Update;
- DNS profiles com IPv4/IPv6 e DoH;
- central de fixes como Network Reset, SFC/DISM, Windows Update Reset e WinGet repair;
- atalhos para ferramentas Windows.

## Tecnicas observadas

- Registry changes por PowerShell provider.
- Service startup type via `Get-Service`/`Set-Service` e fallback `sc.exe`.
- Windows Optional Features via `Enable-WindowsOptionalFeature`.
- DNS via `Set-DnsClientServerAddress`, netsh e DoH cmdlets.
- Restore Point via `Enable-ComputerRestore` e `Checkpoint-Computer`.
- Power plan via `powercfg`.
- AppX removal por cmdlets AppX/DISM.
- Installs via winget/chocolatey.
- O&O ShutUp10++ baixado sob demanda.

## Ajustes seguros candidatos para estudo

Devem ser revalidados antes de entrar no catalogo BorealBoost:

- criar restore point;
- limpeza de arquivos temporarios;
- detectar apps de startup;
- configurar exibicao de extensoes de arquivo;
- Storage Sense com politicas conservadoras;
- abrir ferramentas nativas do Windows;
- DNS profile com reset claro para DHCP;
- Windows Update recommended settings sem desativar seguranca.

## Ajustes avancados candidatos para estudo

Somente com compatibility rules e confirmacao quando aplicavel:

- Delivery Optimization;
- Consumer Features;
- Activity History;
- Telemetry limitada e documentada;
- Background Apps;
- Game DVR/capture;
- Fullscreen Optimizations;
- Windows AI features por build;
- OneDrive handling;
- Edge debloat/removal apenas com politica restritiva;
- IPv4/IPv6 preferencias;
- Teredo;
- Hibernation por desktop/notebook.

## Funcionalidades que BorealBoost nao deve incorporar como estao

- Distribuicao como script remoto `irm | iex`.
- Arquitetura baseada em estado global `$sync`.
- UI WPF do WinUtil ou identidade visual.
- "Ultimate Performance" com reset amplo de power schemes sem snapshot granular.
- Desabilitar BitLocker como Essential.
- Remover Edge por artificio/hack sem validacao forte.
- Desabilitar IPv6 genericamente.
- Baixar O&O ShutUp10++ sem decisao de licenca/distribuicao e hash/assinatura.
- Instalar Chocolatey por script remoto como caminho padrao.
- Presets sem regras de build/hardware/notebook.
- Tweaks sem verify independente.

## Drivers no WinUtil

Nao foi identificado Driver Engine geral para diagnostico e instalacao segura de drivers em PC vivo. O repositorio possui fluxo de Win11 Creator/ISO com exportacao/injecao de drivers, mas isso e outro caso de uso.

Para BorealBoost, driver management deve ser proprio, baseado em PnPUtil, SetupAPI/CfgMgr32, Windows Update Agent e fontes oficiais.

## Compatibilidade Windows 10/11

WinUtil declara e aplica muitos itens por caminho/heuristica, mas o schema observado nao possui matriz completa por:

- Windows 10 versus Windows 11;
- build minimo/maximo;
- edition;
- desktop/notebook;
- CPU/GPU;
- dependencias e conflitos.

BorealBoost precisa transformar cada otimizacao em regra explicita e bloqueante.

## Licenca

WinUtil usa MIT. Se qualquer codigo, trecho ou recurso for reutilizado no futuro:

- preservar copyright e permissao;
- incluir notice em `THIRD_PARTY_NOTICES.md`;
- identificar arquivos/trechos;
- verificar licenca vigente no commit usado;
- nao copiar marca, nome, UI ou assets.

Nesta sessao: nenhum codigo WinUtil foi incorporado.

## Melhorias que BorealBoost deve fazer

- Catalogo com schema mais rigoroso.
- CompatibilityEngine antes de apply.
- VerificationEngine apos apply.
- Snapshot por operacao.
- Restore point como camada adicional.
- Rollback por sessao.
- Driver Engine seguro.
- Boreal Score transparente.
- Relatorio de cliente.
- UI premium propria.
- Logs estruturados JSONL.
- Testes em VMs Windows 10/11.

## Pendencias

- Revisar cada tweak candidato individualmente antes de criar catalogo real.
- Validar licenca de O&O ShutUp10++ se houver intencao de integrar.
- Verificar futuras mudancas do WinUtil antes de qualquer reaproveitamento.

