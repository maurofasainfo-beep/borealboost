# BorealBoost - Driver Engine

Data: 2026-08-12
Status: arquitetura conceitual.

## Objetivo

Diagnosticar e, quando aprovado pelo tecnico, auxiliar instalacao segura de drivers vindos de fontes oficiais. O BorealBoost nao deve ser um driver updater generico.

## Escopo V1

Incluido:

- detectar dispositivos sem driver;
- detectar dispositivos com problema no Device Manager;
- detectar drivers genericos quando houver evidencia confiavel;
- coletar hardware IDs, compatible IDs, vendor ID e device ID;
- exibir versao, data, fornecedor e assinatura do driver instalado;
- apontar fonte oficial recomendada;
- instalar apenas quando fonte, assinatura e compatibilidade forem validadas.

Fora da V1:

- baixar executaveis de mirrors;
- instalar drivers por heuristica insegura;
- mexer em BIOS, firmware ou overclock;
- substituir driver critico sem confirmacao;
- prometer ganho de FPS por driver sem medicao.

## Decisoes V1

O Driver Engine V1 e diagnostico e assistido. Ele nao deve virar um driver updater baseado em scraping generico.

Estrategia Windows Update:

- usar Windows Update/Microsoft como fonte automatizavel preferencial quando a API/fluxo suportar scan e instalacao segura;
- nunca desabilitar Windows Update para buscar performance;
- quando a instalacao automatizada via Windows Update nao for confiavel na V1, exibir orientacao assistida e registrar como pendencia, sem baixar de fontes alternativas inseguras.

Estrategia SetupAPI/CfgMgr32:

- usar SetupAPI/CfgMgr32 como fonte primaria para identidade de dispositivo, hardware IDs, compatible IDs, device instance, problem codes e propriedades de driver;
- usar CIM/WMI como inventario complementar, nao como unica fonte para decisao critica;
- validar comportamento em Windows 10 22H2 build 19045 e Windows 11 24H2/25H2.

Uso de PnPUtil:

- permitido para inventario, exportacao, adicao/instalacao de pacote INF e consulta do driver store;
- chamadas devem ser feitas por handler interno do Agent, com argumentos tipados;
- nao usar PnPUtil para forcar pacote sem match e validacao;
- registrar published name (`oem#.inf`), exit code, stdout/stderr e reboot requerido.

## Fontes oficiais priorizadas

Prioridade para desktop/motherboard:

1. Windows Update / Microsoft.
2. Fabricante oficial da motherboard ou do dispositivo quando o componente for integrado a plataforma.
3. NVIDIA / AMD / Intel para GPU, chipset ou componente com pacote oficial aplicavel ao hardware.

Prioridade para notebook/OEM:

1. Fabricante oficial do notebook para chipset, power management, touchpad, hotkeys, audio, rede integrada, sensores, biometria e componentes customizados.
2. Windows Update / Microsoft.
3. NVIDIA / AMD / Intel somente quando o pacote oficial declarar suporte ao hardware/OEM ou quando a propria politica do fabricante permitir driver generico.

Regra OEM:

- em notebook, OEM vence vendor generico quando houver risco de quebrar energia, teclas de funcao, touchpad, audio customizado, camera, sensores, mux, perfis termicos ou conectividade;
- em desktop, vendor oficial pode vencer motherboard quando o componente e placa dedicada ou pacote generico oficial com match exato;
- se a origem oficial automatizavel nao existir, o BorealBoost mostra orientacao manual e nao substitui por scraping.

Nunca usar:

- sites genericos de download;
- mirrors desconhecidos;
- pacotes sem assinatura;
- repositorios nao verificados;
- executaveis sem hash/assinatura quando houver alternativa oficial;
- scraping generico de paginas web para descobrir drivers;
- instalador que nao permita validar origem, publisher e compatibilidade.

## Componentes

### DriverScanner

Coleta inventario usando:

- PnPUtil quando adequado;
- SetupAPI/CfgMgr32 para informacoes detalhadas;
- CIM/WMI para inventario complementar;
- Device Manager problem codes;
- driver store metadata.

### DeviceIdentityResolver

Normaliza:

- instanceId;
- hardware IDs;
- compatible IDs;
- vendor ID;
- device ID;
- class;
- manufacturer;
- modelo.

Matching:

- hardware ID match exato tem prioridade sobre compatible ID;
- compatible ID pode sugerir candidato, mas exige maior cautela e confirmacao;
- class/subclass/provider isolados nao bastam para instalar;
- notebook requer checagem de modelo/OEM quando o pacote declarar restricoes;
- pacote sem match claro fica como recomendacao manual, nao instalacao.

### DriverHealthAnalyzer

Classifica:

- Installed;
- Missing;
- Attention;
- Generic;
- UpdateAvailable;
- Unknown.

UpdateAvailable so pode aparecer quando houver fonte oficial confiavel e comparacao de versao valida.

Criticalidade:

- Critical: storage, chipset, GPU principal, rede principal, audio essencial em notebook e componentes platform-critical;
- Important: componentes com impacto em estabilidade/uso, mas com rollback viavel;
- Optional: melhorias sem problema detectado e sem risco relevante;
- Unknown: sem classificacao confiavel, nao instalar automaticamente.

### DriverSourceResolver

Define fontes candidatas:

- Windows Update Agent;
- site/API oficial do vendor quando disponivel;
- pagina/manual do fabricante para orientacao manual;
- pacote INF oficial ja baixado pelo tecnico.

Quando nao houver fonte oficial automatizavel:

- nao baixar de mirror;
- nao fazer scraping generico;
- exibir link/orientacao manual para a fonte oficial quando permitido;
- permitir pacote INF local somente se ele passar validacao de assinatura, publisher e match.

### SignatureVerifier

Valida:

- assinatura Authenticode quando houver executavel;
- assinatura do pacote INF/CAT;
- publisher esperado;
- hash quando publicado oficialmente.

Regras:

- driver INF deve ter CAT correspondente e assinatura valida;
- DCH deve ser preferido em Windows 10/11 quando o dispositivo/vendor oferecer pacote DCH aplicavel;
- publisher deve ser Microsoft, OEM esperado ou vendor esperado conforme fonte;
- Authenticode valido no instalador nao substitui validacao de INF/CAT quando o driver package for extraivel;
- pacote com assinatura invalida, publisher inesperado ou hash divergente e bloqueado.

### DriverInstallPlanner

Gera plano antes da instalacao:

- driver atual;
- driver proposto;
- origem;
- assinatura;
- risco;
- reboot necessario;
- rollback possivel;
- confirmacoes.

Comparacao de versao:

- usar metadata `DriverVer` do INF, data e versao do pacote quando disponiveis;
- comparar provider, target e rank/match alem de numero de versao;
- numero maior nao vence se o pacote for de publisher errado, OEM inadequado ou match inferior;
- downgrade so pode ocorrer para rollback ou correcao explicita, com confirmacao.

### DriverInstaller

Executa apenas planos aprovados. Para INF, usar PnPUtil/SetupAPI conforme adequado. Para executavel de vendor, preferir modo assistido e documentado, sem silencioso inseguro.

Instalacao:

- criar snapshot/restore point antes de driver critico;
- bloquear instalacao se hardware ID, assinatura, publisher ou fonte nao forem validos;
- para executavel vendor, preferir fluxo assistido visivel quando modo silencioso nao for oficialmente documentado;
- nao executar instalador arbitrario enviado pela UI; pacote local precisa ser selecionado pelo tecnico e passar validacao do Agent.

### DriverRollback

Estrategias:

- exportar estado/driver anterior quando possivel;
- registrar published name (`oem#.inf`);
- criar restore point tipo driver quando aplicavel;
- usar rollback do Device Manager quando suportado;
- reverter pacote instalado se seguro.

Rollback:

- registrar driver anterior, provider, versao, data, INF publicado e device instance;
- exportar pacote anterior quando possivel;
- rollback automatico so quando estrategia for segura e testada;
- caso rollback dependa de Device Manager/vendor, marcar como assistido/manual;
- reboot pos-rollback deve ser tratado como boundary da sessao.

## Fluxo

1. Scanner coleta dispositivos e drivers.
2. Analyzer identifica problemas.
3. SourceResolver busca fontes oficiais.
4. UI mostra Installed/Missing/Attention/UpdateAvailable.
5. Tecnico revisa detalhe.
6. Para driver critico, confirmacao obrigatoria.
7. Snapshot e restore point.
8. Instalacao.
9. Verificacao pos-instalacao.
10. Reboot se necessario.
11. Resultado no relatorio.

## Categorias de UI

- GPU.
- Chipset.
- Network.
- Audio.
- Storage.
- Other.

Cores devem comunicar estado sem alarmismo.

## Compatibilidade

Regras por:

- Windows 10/11;
- build;
- arquitetura x64;
- vendor;
- device ID;
- notebook/desktop;
- driver model/WDDM quando GPU;
- assinatura;
- requisitos de reboot.

Reboot:

- driver critico pode exigir reboot antes de verify final;
- sessao fica `RebootPending` quando necessario;
- resultado nao deve aparecer como totalmente verificado antes do verify pos-reboot quando a operacao exigir.

## Relacao com WinUtil

O WinUtil analisado nao possui Driver Engine geral para PCs de cliente. Ha logica de injecao/exportacao de drivers em fluxo de ISO, mas isso nao substitui diagnostico e instalacao segura de drivers no sistema vivo. BorealBoost deve implementar engine proprio.

## Pendencias

- Validar empiricamente quando Windows Update Agent sera usado para install automatizado versus orientacao assistida.
- Validar APIs SetupAPI/CfgMgr32 em Windows 10 22H2 e Windows 11 24H2/25H2.
- Definir lista inicial de fabricantes e URLs oficiais permitidas.
- Definir publisher allowlist inicial por OEM/vendor.
