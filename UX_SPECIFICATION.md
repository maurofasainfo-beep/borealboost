# BorealBoost - UX Specification

Data: 2026-08-12
Status: especificacao de experiencia atualizada ate a Fase 5.

## Direcao visual

- Dark mode padrao.
- Aparencia SaaS premium.
- Minimalista, tecnica e limpa.
- Sem copiar WinUtil.
- Gradientes controlados.
- Sem excesso de glow.

Paleta base:

- Background: `#080B14`, `#0D1020`
- Surface: `#111629`, `#151B32`
- Blue: `#3B82F6`
- Electric Blue: `#2563EB`
- Violet: `#7C3AED`
- Purple: `#9333EA`
- Pink: `#EC4899`
- Success: `#22C55E`
- Warning: `#F59E0B`
- Danger: `#EF4444`
- Text Primary: `#F8FAFC`
- Text Secondary: `#94A3B8`

## Layout base

- Janela minima proposta: 1180x760.
- Suporte alvo: Full HD e superior.
- DPI: 100%, 125%, 150%, 200%.
- Sidebar fixa com conteudo adaptavel.
- Conteudo principal com scroll vertical controlado.
- Cards com raio maximo 8px.
- Estados Loading, Empty, Success, Warning, Error e Disabled.

## Navegacao

Sidebar:

- Dashboard
- Scanner
- Analise
- Otimizacao
- Drivers
- Personalizado
- Ferramentas
- Resultados
- Restaurar
- Logs
- Configuracoes

Rodape:

- BorealBoost
- versao
- Admin ativo/necessario

## Dashboard

Antes do scan:

- identificacao da maquina se disponivel;
- card principal "Seu PC esta pronto para analise";
- CTA "Analisar computador";
- status de admin.

Apos scan:

- Boreal Score quando houver algoritmo calibrado em fase futura;
- subscores quando houver algoritmo calibrado;
- perfil recomendado quando presets operacionais existirem;
- oportunidades encontradas via AnalysisResult;
- cards CPU, GPU, RAM, Storage, Windows, Drivers;
- alertas principais.

Nao exibir numero inventado. Se dados insuficientes, exibir estado parcial. Na Fase 3, Dashboard pode continuar sem Boreal Score e a pagina `Analise` e a superficie principal para recomendacoes.

## Scanner

Mostra scan em andamento por etapas reais:

- Windows;
- dispositivo;
- CPU;
- GPU;
- memoria;
- storage;
- rede;
- energia;
- drivers;
- servicos;
- startup;
- seguranca.

Nao usar porcentagem falsa. Barra pode representar etapas concluidas.

Status da Fase 2:

- pagina Scanner funcional;
- botao "Analisar computador";
- progresso ponderado por providers reais;
- cancelamento;
- cards factuais de Sistema, CPU, GPU, Memoria, Storage, Dispositivos, Monitores e Rede;
- resultado tecnico por provider.

Ainda nao ha findings, recommendations, Boreal Score ou benchmarks nesta tela.

## Analise

Conteudo:

- resumo dos findings;
- severidade;
- evidencia;
- impacto;
- recomendacao;
- filtros por categoria.

Estados:

- nenhum problema;
- dados parciais;
- erro de permissao;
- reboot pendente.

Status da Fase 3:

- pagina `Analise` funcional;
- consome o ultimo `SystemSnapshot` real em memoria;
- executa `AnalysisEngine` read-only;
- mostra resumo de regras, oportunidades, avisos, bloqueios, unknown e recomendacoes;
- mostra preview de presets Basico, Medio, Avancado e Custom sem apply;
- filtros por preset, categoria e risco;
- cards de recomendacao com categoria, risco, evidencia, impacto esperado, compatibilidade, motivo tecnico, estado detectado e estado desejado;
- recomendacoes Advanced exibem aviso visual de cautela;
- nenhum botao executa otimizacao;
- Boreal Score, FPS e benchmark permanecem ausentes.

## Otimizacao

Cards de preset:

- Basico - Safe Boost.
- Medio - Performance.
- Avancado - Extreme Performance.
- Personalizado.

Ao selecionar preset:

- quantidade de otimizacoes;
- Safe/Advanced/Aggressive/Experimental;
- categorias afetadas;
- reboot/logout necessario;
- bloqueios de compatibilidade.

Lista de alteracoes:

- busca;
- filtros por risco/evidencia/categoria/estado;
- toggle por item;
- detalhe expandivel com o que faz, por que pode ajudar, o que muda, riscos, compatibilidade e undo.

CTA:

- "Criar ponto de restauracao e otimizar".

Status da Fase 4:

- pagina `Otimizacao` funcional para Review Plan e Dry Run;
- exige Scanner + Analise antes de criar plano;
- mostra operacoes planejadas, risco, reboot, restore point policy, snapshot e blockers;
- o botao de prova controlada executa somente o recurso de integracao HKCU proprio do BorealBoost;
- nenhuma selecao de preset aplica tweaks reais;
- nenhum ganho de FPS, performance ou Boreal Score e exibido.

Status da Fase 5:

- pagina `Otimizacao` calcula presets Basic, Medium, Advanced e Custom a partir do snapshot e analysis atuais;
- mostra `CatalogVersion`, quantidade de definicoes reais, selecionados, bloqueados, not applicable e itens que exigem confirmacao;
- lista cada OptimizationDefinition com categoria, classificacao tecnica, risco, evidencia, relevancia de performance, suitability automatica, mecanismo de configuracao, activation boundary, verification level, status, reboot e rollback;
- checkbox fica habilitado somente para itens `Selected`;
- `RequiresConfirmation`, `Blocked` e `NotApplicable` nao entram automaticamente em plano;
- Dry Run continua obrigatorio antes de executar;
- a execucao usa o pipeline seguro e o Agent, sem comando arbitrario;
- a UI nao mostra FPS gain, percentual de melhoria, Boreal Score comercial ou "PC otimizado" sem benchmark.

## Modal Avancado

Deve explicar:

- altera desempenho acima de conveniencia;
- pode desativar recursos;
- snapshot e restore point serao criados;
- exige checkbox "Entendo os riscos e desejo continuar".

## Drivers

Cards:

- GPU;
- Chipset;
- Network;
- Audio;
- Storage;
- Other.

Estados:

- Installed;
- Missing;
- Attention;
- Update Available;
- Unknown.

Antes de instalar:

- driver atual;
- driver proposto;
- versao;
- fabricante;
- fonte;
- assinatura;
- reboot;
- risco.

## Personalizado

Nao usar pagina com dezenas de checkboxes soltos.

Usar:

- categorias;
- pesquisa;
- filtros;
- cards compactos;
- accordions;
- detalhes;
- toggles.

Acoes:

- Selecionar todos seguros.
- Limpar selecao.
- Detectar aplicados.
- Reverter selecionados.

## Ferramentas

Atalhos para ferramentas do Windows:

- Computer Management;
- Device Manager;
- Services;
- Task Manager;
- Event Viewer;
- Disk Management;
- Windows Update;
- Optional Features;
- Power Options;
- Network Connections;
- Windows Defender Firewall;
- System Properties;
- Restore.

Abrir ferramentas nao deve contar como otimizacao.

## Resultados

Mostrar antes/depois:

- Boreal Score;
- RAM idle;
- processos;
- startup apps;
- problemas de driver;
- power plan;
- otimizacoes aplicadas;
- restore point;
- snapshot;
- rollback disponivel.

FPS so se benchmark real foi executado.

## Restaurar

Lista sessoes:

- data;
- cliente;
- preset;
- quantidade de alteracoes;
- status rollback.

Detalhe:

- valores antes/depois;
- estado atual;
- reverter item;
- reverter sessao.

Status da Fase 4:

- pagina `Restauracao` lista sessoes persistidas, estado, snapshot items e recovery candidates;
- nao promete rollback quando nao existe snapshot;
- recovery mostra acao sugerida, sem executar rollback automatico;
- rollback operacional existe no engine e e validado por testes controlados.
- preferencias de UX, privacidade e atalhos devem aparecer como preferencias/opt-in, nao como aumento de FPS ou performance.

## Logs

Recursos:

- filtro por sessao;
- categoria;
- sucesso/aviso/falha;
- busca;
- exportar;
- detalhes tecnicos expansivos.

## Configuracoes

V1:

- tema;
- diretorio de logs;
- retencao de logs;
- verificar atualizacoes;
- dados do tecnico;
- logo do relatorio;
- nivel padrao de preset.

## Acessibilidade

- contraste adequado;
- foco visivel;
- navegacao por teclado;
- tooltips;
- labels claros;
- icones com contexto quando necessario;
- textos sem corte em DPI alto.

## Pendencias

- Receber logo e icone definitivos.
- Receber imagens de referencia citadas no Master Spec.
- Definir fonte/tipografia final.
- Prototipar telas antes da Fase 1 visual final.
