# BorealBoost - UX Specification

Data: 2026-08-12
Status: especificacao de experiencia, sem implementacao.

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

- Boreal Score;
- subscores;
- perfil recomendado;
- oportunidades encontradas;
- cards CPU, GPU, RAM, Storage, Windows, Drivers;
- alertas principais.

Nao exibir numero inventado. Se dados insuficientes, exibir estado parcial.

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

