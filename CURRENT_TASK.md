# CURRENT_TASK.md
## BorealBoost — Fase 1: Foundation

> Esta tarefa inicia oficialmente a implementação do BorealBoost.
>
> O objetivo desta fase é criar exclusivamente a fundação técnica da aplicação.
>
> NÃO implementar otimizações reais do Windows nesta fase.

---

# STATUS

Fase anterior:

✅ FASE 0 — Discovery e Arquitetura — APROVADA

Fase atual:

🚧 FASE 1 — FOUNDATION

---

# DOCUMENTOS OBRIGATÓRIOS

Antes de escrever qualquer código, leia integralmente:

- BOREALBOOST_MASTER_SPEC.md
- CODEX_BOOTSTRAP.md
- DISCOVERY.md
- REQUIREMENTS.md
- ARCHITECTURE.md
- ARCHITECTURE_DECISION_RECORD.md
- DOMAIN_MODEL.md
- OPTIMIZATION_ENGINE.md
- DRIVER_ENGINE.md
- ROLLBACK_ENGINE.md
- BOREAL_SCORE.md
- UX_SPECIFICATION.md
- COMPATIBILITY_MATRIX.md
- SECURITY.md
- IMPLEMENTATION_ROADMAP.md
- THIRD_PARTY_NOTICES.md

Considere esses documentos a fonte oficial da arquitetura.

Não contradiga decisões já aprovadas silenciosamente.

---

# OBJETIVO PRINCIPAL

Criar a fundação compilável, testável e organizada do BorealBoost utilizando a arquitetura aprovada.

Ao final desta fase deve existir uma aplicação desktop funcional capaz de:

- iniciar;
- carregar a interface base;
- navegar entre módulos;
- carregar DI/configuração/logging;
- identificar informações básicas da aplicação;
- detectar se há privilégios administrativos;
- possuir contratos fundamentais do domínio;
- possuir estrutura inicial do BorealBoost.Agent;
- compilar;
- executar testes básicos.

Ainda NÃO deve modificar o Windows.

---

# STACK APROVADA

Utilizar:

- C#
- .NET 10 LTS
- WinUI 3
- Windows App SDK
- MVVM
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Logging

Bibliotecas adicionais somente quando justificadas.

Antes de adicionar qualquer pacote NuGet:

1. verificar necessidade;
2. verificar licença;
3. verificar manutenção;
4. verificar compatibilidade;
5. registrar em THIRD_PARTY_NOTICES.md quando aplicável.

---

# SOLUTION

Criar a solution:

BorealBoost.sln

Criar inicialmente os projetos necessários para Foundation.

Estrutura esperada conceitualmente:

src/
├── BorealBoost.App
├── BorealBoost.Agent
├── BorealBoost.Core
├── BorealBoost.System
├── BorealBoost.Analysis
├── BorealBoost.Optimization
├── BorealBoost.Restore
├── BorealBoost.Benchmark
├── BorealBoost.Drivers
├── BorealBoost.Reporting
└── BorealBoost.Infrastructure

tests/
├── BorealBoost.Tests.Unit
├── BorealBoost.Tests.Integration
└── BorealBoost.Tests.System

Não é obrigatório preencher todos os projetos com implementação nesta fase.

Porém, a estrutura deve respeitar o grafo de dependências definido em ARCHITECTURE.md.

Evite dependências circulares.

---

# CORE

Implementar apenas os contratos e tipos fundamentais necessários à fundação.

Priorizar:

- Result / OperationResult
- identificadores fortes quando adequado
- enums fundamentais
- contratos de logging/operações
- informações da aplicação
- conceitos mínimos de sessão

Não tente implementar todo DOMAIN_MODEL.md nesta fase.

Criar somente o necessário para estabelecer os padrões arquiteturais.

---

# DEPENDENCY INJECTION

Configurar DI central.

A composição deve acontecer na camada apropriada da aplicação.

Registrar:

- configuration;
- logging;
- navigation;
- application services;
- adapters permitidos nesta fase.

Evitar Service Locator.

Evitar acesso global estático ao container.

---

# CONFIGURAÇÃO

Criar mecanismo inicial para configuração local.

Definir claramente:

- configurações da aplicação;
- configurações de desenvolvimento;
- configurações futuras do técnico.

Não armazenar secrets no repositório.

Não criar sistema excessivamente complexo.

---

# LOGGING

Implementar structured logging desde o início.

Requisitos:

- timestamp;
- level;
- source/context;
- correlation quando aplicável;
- exception completa em arquivo;
- mensagem amigável na UI.

Logs devem ir para diretório apropriado de dados da aplicação.

NÃO armazenar logs dentro de Program Files.

Não registrar:

- senha;
- token;
- secret;
- conteúdo pessoal do usuário.

Criar abstração suficiente para permitir evolução posterior.

---

# BOREALBOOST.APP

Criar o shell principal WinUI 3.

A interface inicial deverá possuir identidade visual BorealBoost.

Direção visual:

- dark;
- SaaS premium;
- azul;
- roxo;
- rosa;
- tecnológica;
- limpa;
- minimalista.

Não copiar WinUtil.

---

# SHELL

Criar sidebar inicial com:

- Dashboard
- Análise
- Otimização
- Drivers
- Personalizado
- Ferramentas
- Resultados
- Restauração
- Logs
- Configurações

Neste momento as telas podem ser placeholders profissionais.

Não implementar funcionalidades reais dos módulos.

---

# DASHBOARD DA FOUNDATION

Implementar apenas uma versão inicial.

Mostrar:

BorealBoost

Status da aplicação.

Informações como:

- versão;
- status administrativo;
- sistema operacional básico quando disponível de maneira segura;
- botão futuro de análise em estado ainda não funcional ou claramente marcado.

Não inventar:

- Boreal Score;
- ganhos;
- diagnósticos;
- quantidade de otimizações.

Se o Scanner ainda não existe, mostrar estado vazio adequado.

---

# DESIGN SYSTEM

Criar a base visual reutilizável.

Definir:

## Colors

Background
Surface
Primary
Secondary
Accent
Success
Warning
Danger
TextPrimary
TextSecondary
Border

Basear-se na UX_SPECIFICATION.md.

---

# TYPOGRAPHY

Definir estilos consistentes para:

- Display
- Heading
- Subheading
- Body
- Caption
- Button

---

# SPACING

Criar escala consistente.

Evitar valores aleatórios espalhados pelo XAML.

---

# COMPONENTES BASE

Criar componentes reutilizáveis quando fizer sentido:

- PageHeader
- SidebarItem
- StatusBadge
- SectionCard
- EmptyState
- LoadingState
- ErrorState

Não criar abstrações excessivas.

---

# RESPONSIVIDADE

Validar:

- tamanho mínimo definido na especificação;
- 100% DPI;
- 125%;
- 150%;
- comportamento de resize.

Evitar:

- elementos cortados;
- sidebar quebrada;
- textos sobrepostos;
- scroll incorreto.

---

# NAVEGAÇÃO

Implementar NavigationService ou abstração equivalente.

A View não deve decidir regras de negócio.

ViewModels não devem acessar diretamente Registry, Services ou PowerShell.

---

# MVVM

Usar MVVM consistentemente.

Separar:

View
ViewModel
Services

Evitar code-behind para lógica de negócio.

Code-behind pode existir para comportamento puramente visual quando justificável.

---

# AGENT

Criar somente a FUNDAÇÃO do:

BorealBoost.Agent

IMPORTANTE:

NÃO implementar ainda operações reais privilegiadas de Registry, Services, Power, Drivers ou tweaks.

Criar arquitetura inicial respeitando os ADRs.

---

# CONTRATOS APP ↔ AGENT

Nesta fase definir e, quando adequado, implementar os tipos fundamentais do protocolo.

Incluir:

- ProtocolVersion
- MessageType
- RequestId
- SessionId
- CorrelationId
- Timestamp
- PayloadType
- Result
- Error

Não permitir payload arbitrário.

---

# REGRA CRÍTICA DO AGENT

O Agent jamais deve aceitar algo equivalente a:

ExecuteCommand(string command)

ExecutePowerShell(string script)

ExecuteProcess(string executable)

ou qualquer API genérica capaz de executar conteúdo enviado pela UI.

Não criar isso nem temporariamente.

---

# ALLOWLIST

A arquitetura deve permitir futuramente operações explicitamente modeladas.

Exemplo conceitual:

RegistryOperation
ServiceOperation
PowerOperation

Mas NÃO implementar essas alterações reais nesta fase.

---

# ADMIN STATUS

A aplicação deve conseguir informar:

Administrador:

Ativo

ou

Administrador:

Necessário

Esse status deve ser real.

Não elevar automaticamente o Agent apenas para mostrar o Dashboard.

---

# AGENT LIFECYCLE

Implementar somente o necessário para provar a arquitetura.

Se for seguro e adequado nesta fase, criar um handshake mínimo sem operações destrutivas.

Exemplo:

App
→ inicia Agent elevado
→ estabelece canal
→ valida protocolo
→ Agent responde status/versão
→ encerra

Isso deve ser tratado como prova arquitetural.

Não adicionar comandos administrativos reais.

---

# SEGURANÇA DO PIPE

Se o named pipe for implementado nesta fase:

seguir SECURITY.md e ADR.

Obrigatório:

- ACL;
- usuário esperado;
- Administrators;
- validação de sessão;
- protocolo versionado;
- payload validado;
- limits;
- timeout.

Criar testes quando possível.

---

# SYSTEM

Nesta fase não criar o Scanner completo.

Pode implementar somente informações triviais necessárias à foundation, por exemplo:

- versão do OS;
- arquitetura;
- privilégios;
- nome da máquina quando realmente necessário.

Scanner completo pertence à Fase 2.

---

# INFRASTRUCTURE

Preparar:

- logging;
- paths;
- configuration;
- application data;
- session directories futuras.

Definir corretamente:

Program Files

para binários.

ProgramData/AppData

para dados mutáveis conforme classificação.

---

# PATH SERVICE

Criar uma abstração central para paths.

Evitar strings de caminho espalhadas pelo código.

Preparar diretórios conceituais para:

- Logs
- Sessions
- Snapshots
- Reports
- Configuration

Não criar conteúdo falso.

---

# VERSIONAMENTO

A aplicação deve possuir versão claramente definida.

Preparar mecanismo para:

- versão do app;
- protocol version;
- catalog schema version futuramente.

---

# ERROR HANDLING

Criar estratégia inicial global.

Requisitos:

- exception logging;
- erros não devem fechar silenciosamente;
- UI deve ter mensagem adequada;
- stack trace não deve aparecer ao cliente por padrão.

---

# TESTES

Criar testes unitários iniciais.

Cobrir pelo menos componentes puros criados nesta fase.

Exemplos:

- protocol validation;
- Result;
- value objects;
- navigation metadata quando aplicável;
- configuration validation.

Não escrever testes artificiais apenas para aumentar número.

---

# BUILD

Ao concluir:

dotnet restore

dotnet build

dotnet test

devem ser executados quando tecnicamente aplicáveis.

Corrigir erros antes de declarar conclusão.

Warnings relevantes devem ser analisados.

Não esconder warnings críticos.

---

# README

Atualizar README.md com:

- descrição do BorealBoost;
- estado atual;
- requisitos de desenvolvimento;
- como abrir solution;
- como buildar;
- como testar;
- arquitetura resumida;
- aviso de que ainda não existem otimizações reais na Fase 1.

---

# GITIGNORE

Criar/validar .gitignore apropriado para:

- Visual Studio;
- .NET;
- WinUI;
- build artifacts;
- logs locais;
- arquivos temporários;
- configurações locais.

Não ignorar documentação importante.

---

# O QUE É PROIBIDO NESTA FASE

NÃO:

- editar Registry;
- alterar Services;
- criar plano de energia;
- modificar DNS;
- remover AppX;
- instalar/desinstalar programas;
- instalar drivers;
- executar DISM operacional;
- executar SFC operacional;
- modificar Windows Update;
- implementar debloat;
- desabilitar telemetria;
- desativar Defender;
- desativar Firewall;
- modificar VBS;
- modificar Memory Integrity;
- implementar tweaks;
- copiar código do WinUtil;
- criar Boreal Score fictício;
- criar benchmark fictício;
- inventar hardware;
- executar PowerShell arbitrário.

---

# NÃO ANTECIPAR FASES

Scanner completo pertence à:

FASE 2.

Analysis/Recommendation pertence à:

FASE 3.

Optimization Engine operacional pertence à:

FASE 4.

Safety/Rollback operacional pertence à:

FASE 5.

Tweaks pertencem à:

FASE 6+.

Não avance essas fronteiras.

---

# CRITÉRIOS DE ACEITAÇÃO

A Fase 1 somente pode ser considerada concluída quando:

- solution existe;
- arquitetura de projetos existe;
- dependências respeitam ARCHITECTURE.md;
- build funciona;
- testes básicos passam;
- WinUI App inicia;
- Shell funciona;
- sidebar funciona;
- páginas placeholder funcionam;
- tema BorealBoost base existe;
- DI funciona;
- configuração funciona;
- logging funciona;
- paths estão organizados;
- Admin Status é real;
- Agent foundation existe;
- protocolo inicial está modelado;
- Agent não aceita execução arbitrária;
- nenhuma otimização real foi implementada;
- README foi atualizado.

---

# ENTREGA OBRIGATÓRIA

Ao terminar esta sessão apresentar:

## Resumo

O que foi implementado.

## Arquivos e projetos criados

Separados por projeto.

## Dependências adicionadas

Nome
Versão
Licença
Motivo

## Arquitetura

Confirme se o grafo de dependências foi respeitado.

## Interface

Informe quais telas/placeholders foram criados.

## Agent

Informe exatamente o que foi implementado.

Declare explicitamente se existe ou não alguma capacidade de executar comando arbitrário.

A resposta correta deve ser NÃO.

## Testes

Liste:

- testes executados;
- comandos utilizados;
- resultados.

## Build

Informe resultado de:

- restore;
- build;
- test.

## Riscos/Pendências

Liste tudo que ainda não foi validado.

## Git diff

Faça revisão final do diff e confirme que nenhuma funcionalidade destrutiva foi adicionada.

---

# REGRA FINAL

Não priorize quantidade de código.

A Fase 1 existe para criar uma fundação limpa sobre a qual o BorealBoost será construído.

Se uma decisão exigir quebrar a arquitetura aprovada, interrompa essa parte, documente o conflito e apresente a decisão antes de criar um workaround inadequado.