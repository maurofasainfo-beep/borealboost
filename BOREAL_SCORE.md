# BorealBoost - Boreal Score

Data: 2026-08-12
Status: metodologia inicial experimental/beta para validacao.

## Objetivo

Boreal Score resume saude operacional e oportunidades de otimizacao do PC. Ele nao e promessa de FPS, nao substitui benchmark e nao deve esconder quais fatores compoem a nota.

O score so pode ser exibido com explicacao de subscores e limitacoes.

O algoritmo inicial e beta ate calibracao com dataset de VMs, maquinas reais e resultados de campo. Aumento do Boreal Score nao equivale automaticamente a aumento de FPS, 1% lows ou frametime melhor.

Versao inicial proposta: `BBScore-v0-beta`.

## Principios

- Usar apenas dados detectados.
- Penalizar problemas reais, nao preferencias esteticas.
- Separar score de sistema de benchmark gaming.
- Nao inferir ganhos de FPS.
- Versionar algoritmo.
- Calibrar pesos com testes em VMs/hardware real.
- Comparar antes/depois apenas com a mesma versao de algoritmo ou exibir aviso de incompatibilidade.

## Estrutura inicial

Total: 0 a 100.

Subscores propostos para V1:

- System Integrity: 15.
- Drivers: 15.
- Startup and Background Load: 15.
- Services: 10.
- Memory Pressure: 10.
- Storage: 10.
- Power Configuration: 10.
- Gaming Configuration: 10.
- Update and Reboot Readiness: 5.

Estes pesos sao proposta inicial de produto, nao validacao estatistica final. Antes de uso comercial, devem ser revisados com dados de teste e feedback tecnico.

Enquanto o algoritmo estiver `beta`, relatorios devem indicar:

- algoritmo experimental;
- versao usada;
- pesos sujeitos a calibracao;
- diferenca entre score operacional e benchmark de performance;
- que ganho de score nao garante ganho de FPS.

## Entradas

### System Integrity

Exemplos:

- SFC/DISM necessario ou com falha conhecida;
- Windows activation informativo, sem penalizar performance diretamente;
- pending reboot;
- erros criticos detectaveis.

### Drivers

Exemplos:

- dispositivo sem driver;
- Device Manager problem code;
- driver generico para componente critico;
- driver de GPU/chipset/rede ausente;
- assinatura invalida.

### Startup and Background Load

Exemplos:

- quantidade de startup apps;
- impacto alto quando disponivel;
- processos idle excessivos;
- consumo RAM/CPU idle medido.

### Services

Exemplos:

- servicos opcionais ativos sem hardware/uso correspondente;
- servicos desconhecidos nao penalizam automaticamente;
- servicos criticos nunca entram em sugestao automatica.

### Memory Pressure

Exemplos:

- RAM total;
- RAM livre/uso idle;
- page file pressure;
- processos dominantes.

### Storage

Exemplos:

- tipo HDD/SATA SSD/NVMe;
- espaco livre;
- TRIM;
- problemas de saude quando API confiavel indicar;
- cleanup seguro disponivel.

### Power Configuration

Exemplos:

- plano atual inadequado ao perfil;
- notebook em bateria;
- desktop com power plan conservador para gaming;
- suporte a plano performance.

### Gaming Configuration

Exemplos:

- Game Mode;
- captura/Game DVR;
- GPU detectada;
- driver GPU;
- overlays/processos conhecidos quando detectaveis;
- HAGS somente se hardware/driver suportar.

### Update and Reboot Readiness

Exemplos:

- reboot pendente;
- Windows Update quebrado;
- politica agressiva de update;
- bloqueios que impedem seguranca.

## Formula conceitual

Cada subscore inicia no maximo da categoria e recebe penalidades por findings confirmados. Penalidades tem severidade:

- Info: nao reduz score.
- Low: reducao pequena.
- Medium: reducao moderada.
- High: reducao alta.
- Critical: reducao maxima dentro do subscore.

Penalidade nao pode reduzir subscore abaixo de zero.

Score final = soma dos subscores.

## Versionamento do algoritmo

Formato:

`BBScore-vMAJOR.MINOR.PATCH` ou `BBScore-v0-beta` durante calibracao.

Regras:

- mudanca de pesos, categorias ou penalidades que altere comparabilidade incrementa major ou marca incompatibilidade;
- ajuste de penalidade dentro da mesma metodologia incrementa minor/patch conforme impacto;
- relatorio guarda `algorithmVersion`, inputs, subscores e limitacoes;
- comparacao historica entre versoes diferentes deve aparecer como contextual, nao como delta direto confiavel.

## Exibicao

Dashboard:

- total;
- subscores;
- quantidade de oportunidades;
- perfil recomendado;
- principais alertas.

Relatorio:

- algoritmo versionado;
- inputs usados;
- antes/depois;
- limitacoes;
- itens que exigem reboot.

## Antes x depois

O score pos-otimizacao so deve ser recalculado apos novo scanner e verification.

Metricas antes/depois permitidas:

- RAM idle medida;
- processos;
- startup apps;
- problemas de driver;
- espaco livre;
- plano de energia;
- pending reboot;
- servicos ajustados.

FPS, 1% lows e frametime so aparecem se benchmark real for executado.

## Limitacoes

- Score nao mede "qualidade gamer" sozinho.
- Score pode melhorar por remover problemas, mas nao garante FPS.
- Aumento de score nao garante aumento de FPS; FPS, 1% lows e frametime dependem de benchmark real.
- Sistemas corporativos podem ter politicas intencionais que parecem oportunidades; dominio/VPN/RDP devem reduzir agressividade.
- Windows 10 fora do suporte geral precisa de aviso contextual.

## Pendencias

- Calibrar pesos.
- Definir penalidades numericas por finding.
- Criar dataset de VMs e maquinas reais.
- Definir quando o score fica oculto por dados insuficientes.
