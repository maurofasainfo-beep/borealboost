# CURRENT_TASK.md
# BorealBoost — Fase 5
# Optimization Catalog — Basic + Medium + Advanced/Aggressive + Custom

> Fases 0, 1, 2, 3 e 4: APROVADAS.
>
> A infraestrutura de execução, safety, snapshot, verification, rollback,
> recovery, Agent e concorrência foi aprovada.
>
> Esta é a primeira fase destinada a implementar otimizações reais.
>
> Não transformar o BorealBoost em uma coleção de tweaks aleatórios.

---

# 1. STATUS

Concluído:

✅ FASE 0 — Discovery e Arquitetura
✅ FASE 1 — Foundation
✅ FASE 2 — System Scanner
✅ FASE 3 — Analysis + Recommendation Engine
✅ FASE 4 — Optimization Engine + Safety + Snapshot + Rollback

Fase atual:

🚧 FASE 5 — OPTIMIZATION CATALOG

Próximas:

FASE 6 — Drivers + Benchmark + Results + Reporting
FASE 7 — Installer + Hardening + Production Readiness

---

# 2. OBJETIVO

Implementar o primeiro catálogo REAL de otimizações do BorealBoost para:

- Windows 10;
- Windows 11;
- desktops;
- notebooks;
- PCs gaming;
- máquinas de uso geral.

O catálogo deve alimentar o pipeline já aprovado:

SystemSnapshot
↓
Analysis
↓
Recommendation
↓
OptimizationDefinition
↓
Preset / Custom Selection
↓
ExecutionPlan
↓
Preflight
↓
Snapshot
↓
Agent
↓
Apply
↓
Verify
↓
Journal
↓
Rollback

Nenhuma otimização pode contornar esse pipeline.

---

# 3. OBJETIVO DE PRODUTO

O BorealBoost será usado pelo técnico presencialmente nos computadores dos clientes.

O software deve permitir:

1. analisar a máquina;
2. identificar oportunidades;
3. escolher Windows 10 ou Windows 11 quando necessário;
4. detectar automaticamente quando possível;
5. selecionar um preset;
6. revisar o que será alterado;
7. executar otimizações;
8. acompanhar progresso real;
9. verificar resultados;
10. permitir rollback.

Presets principais:

BÁSICO
MÉDIO
AVANÇADO

Além de:

PERSONALIZADO

---

# 4. PRINCÍPIO FUNDAMENTAL

Uma OptimizationDefinition somente entra no catálogo se puder responder:

- O que detectamos?
- Por que isso pode ser otimizado?
- Em quais máquinas isso é válido?
- Qual é o benefício esperado?
- Qual é o risco?
- O que exatamente será alterado?
- Como verificamos sucesso?
- Como desfazemos?
- Exige reboot?
- Pode afetar segurança?
- Pode afetar compatibilidade?

Se essas perguntas não puderem ser respondidas:

NÃO incluir automaticamente no catálogo.

---

# 5. LEITURA OBRIGATÓRIA

Antes de implementar qualquer otimização, leia integralmente:

- BOREALBOOST_MASTER_SPEC.md
- CODEX_BOOTSTRAP.md
- CURRENT_TASK.md
- REQUIREMENTS.md
- ARCHITECTURE.md
- ARCHITECTURE_DECISION_RECORD.md
- DOMAIN_MODEL.md
- SYSTEM_SCANNER.md
- ANALYSIS_ENGINE.md
- OPTIMIZATION_ENGINE.md
- OPTIMIZATION_EXECUTION.md
- ROLLBACK_ENGINE.md
- ROLLBACK.md
- SECURITY.md
- COMPATIBILITY_MATRIX.md
- IMPLEMENTATION_ROADMAP.md
- UX_SPECIFICATION.md
- BOREAL_SCORE.md
- WINUTIL_ANALYSIS.md
- PHASE4_AUDIT.md
- PHASE4_REVALIDATION.md

Depois inspecione integralmente:

- Core
- Analysis
- Optimization
- Restore
- Infrastructure
- System
- Agent
- App
- tests

Não enfraquecer mecanismos aprovados na Fase 4.

---

# 6. PESQUISA TÉCNICA OBRIGATÓRIA

Antes de adicionar uma otimização real, pesquise e valide sua origem.

Priorizar:

1. documentação oficial Microsoft;
2. documentação oficial do fabricante;
3. APIs/documentação Windows;
4. comportamento tecnicamente verificável;
5. projetos reconhecidos apenas como referência secundária.

WinUtil pode ser usado como referência funcional.

Também podem ser estudadas ideias de ferramentas reconhecidas de otimização, desde que:

- não copiar código;
- não copiar UI;
- não copiar marca;
- não importar scripts cegamente;
- cada técnica seja reavaliada individualmente.

Não assumir:

"WinUtil faz, portanto é seguro."

---

# 7. DOCUMENTAÇÃO DA EVIDÊNCIA

Para cada otimização, registrar:

EvidenceLevel

e:

EvidenceReferences

quando aplicável.

Classificação:

Strong
Moderate
Experimental
Unknown

Unknown:

não entra automaticamente em preset.

Experimental:

somente Advanced/Custom quando tecnicamente justificável.

---

# 8. PROIBIÇÃO DE CLAIMS FALSOS

Não documentar:

+30 FPS
+50 FPS
-20 ms latency
+40% performance

sem benchmark reproduzível e específico.

Utilizar:

ExpectedImpact:

Low
Moderate
PotentiallyHigh
WorkloadDependent
Unknown

E especificar domínio:

Responsiveness
BackgroundContention
Startup
Storage
PowerBehavior
GamingConsistency
NetworkBehavior
Privacy
VisualEffects
Maintenance

Não chamar tudo de FPS boost.

---

# 9. PRESETS

Implementar política real para:

Basic
Medium
Advanced
Custom

Preset não é lista duplicada de operações.

É uma política de seleção sobre o catálogo.

---

# 10. BASIC

Objetivo:

baixo risco e ampla compatibilidade.

Pode conter somente operações que sejam:

- Safe;
- reversíveis quando modificadoras;
- verificáveis;
- amplamente compatíveis;
- sem redução relevante de segurança;
- sem remover funcionalidades importantes;
- sem comportamento hardware-specific arriscado.

Basic deve ser adequado para cliente comum.

---

# 11. MEDIUM

Objetivo:

otimização mais perceptível sem entrar automaticamente em alterações de alto risco.

Pode conter:

Safe
+
Medium

desde que compatíveis.

Pode alterar comportamento não essencial do Windows quando:

- benefício for defensável;
- side effect estiver documentado;
- rollback existir.

Não reduzir segurança silenciosamente.

---

# 12. ADVANCED

Objetivo:

máxima possibilidade de otimização controlada.

Pode expor:

Safe
Medium
Advanced
Aggressive

Mas Advanced/Aggressive exigem:

- compatibilidade explícita;
- warning;
- side effects;
- confirmation;
- snapshot;
- rollback quando tecnicamente possível;
- indicação clara quando rollback não for perfeito;
- justificativa técnica.

Advanced NÃO significa:

"sempre melhor".

---

# 13. CUSTOM

Permitir selecionar individualmente OptimizationDefinitions.

Agrupar por categoria.

Mostrar:

- estado;
- recomendação;
- risco;
- evidência;
- impacto;
- compatibilidade;
- reboot;
- rollback;
- detalhes.

---

# 14. CATEGORIAS DO CATÁLOGO

Estruturar catálogo para:

System
Performance
Gaming
Power
Startup
Services
Visual
Storage
Network
Privacy
Windows
Maintenance
Security
Graphics

Não é obrigatório preencher todas com operações reais nesta primeira versão.

Qualidade > quantidade.

---

# 15. IMPLEMENTAÇÃO EM CAMADAS

Não implementar 100 tweaks de uma vez.

Trabalhar em waves.

WAVE A — Safe Foundation

WAVE B — Medium

WAVE C — Advanced

WAVE D — Aggressive / Experimental

Cada wave deve passar testes antes da próxima.

---

# 16. WAVE A — SAFE

Priorizar operações de baixo risco.

Avaliar tecnicamente candidatos como:

- configurações visuais opcionais;
- redução de animações quando desejado;
- transparência;
- algumas opções de background não essenciais;
- limpeza de recursos temporários quando puder ser feita com segurança;
- ajustes de startup somente quando item for conhecido e selecionado;
- configurações de experiência Windows claramente opcionais;
- power behavior seguro e contextual.

NÃO implementar todos automaticamente.

Pesquisar cada candidato.

---

# 17. VISUAL EFFECTS

Avaliar configurações como:

- animations;
- transparency;
- menu effects;
- taskbar effects;
- window animation.

Considerar:

PC moderno
≠
PC fraco.

Não prometer ganho grande de FPS.

Impacto provável:

responsiveness / UI overhead.

Rollback obrigatório.

---

# 18. BACKGROUND ACTIVITY

Avaliar configurações que realmente reduzam atividade de background.

Não desabilitar componentes essenciais.

Não usar heurística:

"background = ruim".

Considerar:

- Store apps;
- notifications;
- sync;
- gaming dependencies;
- OEM utilities.

---

# 19. STARTUP

Integrar com análise existente.

Não desabilitar automaticamente tudo.

Itens devem possuir classificação como:

KnownSafeToDisable
UserApplication
SystemCritical
DriverRelated
SecurityRelated
Unknown

ou modelo equivalente.

Unknown:

não desabilitar automaticamente.

Security/Driver:

não entrar em preset automático sem justificativa excepcional.

---

# 20. SERVICES

Esta é uma área de alto risco.

Criar ServiceOptimizationDefinition com:

- ServiceName;
- current startup type;
- desired startup type;
- current state;
- dependencies;
- dependents;
- OS/build;
- feature requirements;
- side effects;
- rollback.

NÃO criar lista gigante de "serviços inúteis".

---

# 21. SERVICE SAFETY

Antes de alterar Service:

validar:

- serviço existe;
- Windows correto;
- dependências;
- dependentes;
- feature associada;
- hardware relacionado;
- estado atual;
- startup type atual.

Nunca assumir que serviço presente é desnecessário.

---

# 22. SERVIÇOS CRÍTICOS

Criar denylist explícita para impedir operações perigosas em serviços críticos.

O Agent deve rejeitar qualquer tentativa de alterar serviço fora do catálogo confiável.

Não depender apenas da UI.

---

# 23. POWER

Criar arquitetura real para power optimizations.

Objetivo futuro inclui perfil BorealBoost Performance.

Mas não assumir:

"100% CPU sempre = melhor gaming."

Considerar:

Desktop
Laptop
AC
Battery
CPU vendor
thermal behavior
sleep
modern standby
OEM policies.

---

# 24. BOREALBOOST PERFORMANCE POWER PLAN

Avaliar tecnicamente criação de:

BorealBoost Performance

baseado em plano Windows apropriado.

Requisitos:

- criar sem destruir plano original;
- guardar GUID anterior;
- guardar plano ativo anterior;
- verificar criação;
- ativar somente com consentimento/preset apropriado;
- rollback restaura plano anterior;
- remoção do plano BorealBoost quando undo exigir;
- evitar duplicatas em execuções repetidas.

Se implementação segura não estiver madura:

adiar para Wave C.

---

# 25. ULTIMATE PERFORMANCE

Não habilitar automaticamente apenas porque existe.

Avaliar:

- disponibilidade;
- edição Windows;
- desktop/notebook;
- consumo;
- temperatura;
- comportamento real.

Se entrar:

Advanced/Custom.

Não Basic.

---

# 26. CPU SCHEDULING

Pesquisar cuidadosamente qualquer ajuste relacionado a:

- scheduling;
- foreground boost;
- multimedia scheduling;
- timer behavior;
- processor power management.

Não implementar "tweak de internet" sem documentação/evidência.

Especial cuidado com:

Win32PrioritySeparation
SystemResponsiveness
NetworkThrottlingIndex

e técnicas semelhantes.

Pesquisar antes.

---

# 27. TIMER TWEAKS

Não implementar automaticamente:

- HPET toggles;
- bcdedit timer modifications;
- useplatformclock;
- disabledynamictick;
- synthetic timer hacks;

sem evidência extremamente forte.

Por padrão:

EXCLUDE.

Qualquer inclusão futura:

Experimental/Aggressive + benchmark necessário.

---

# 28. BCD

Alterações de boot configuration são de alto risco.

Nesta fase:

não usar BCD como parte dos presets automáticos.

Se catalogado apenas para pesquisa:

DisabledByPolicy.

---

# 29. MEMORY

Não implementar tweaks como:

- DisablePagingExecutive;
- LargeSystemCache;
- arbitrary pagefile disable;
- memory compression disable;

apenas porque são populares.

Pesquisar individualmente.

Não desabilitar pagefile automaticamente.

---

# 30. PAGEFILE

Política padrão:

não remover/desabilitar pagefile.

Qualquer recomendação futura deve considerar:

- RAM;
- crash dumps;
- commit limit;
- workload;
- storage.

Não entrar automaticamente em Basic/Medium.

---

# 31. STORAGE

Avaliar otimizações seguras relacionadas a:

- temporary files;
- delivery optimization cache quando apropriado;
- recycle bin apenas com consentimento;
- storage cleanup;
- stale temp resources.

Nunca apagar:

- Downloads;
- Documents;
- Desktop;
- user data;
- game saves;
- browser profiles;
- arbitrary AppData.

---

# 32. FILE DELETE SAFETY

Qualquer FileOperation futura deve possuir allowlist estrita.

Proibir:

path arbitrário vindo da UI.

Canonicalizar path.

Validar root.

Impedir:

..\ traversal
junction escape
symlink/reparse escape

quando aplicável.

Não implementar FileOperation genérico inseguro.

---

# 33. NETWORK

Tratar "network optimization" com ceticismo técnico.

Não assumir que Registry tweak reduz ping.

Avaliar separadamente:

- DNS;
- adapter power saving;
- network throttling;
- Nagle-related settings;
- TCP autotuning;
- RSS;
- RSC;
- offloads.

Cada item depende de contexto.

---

# 34. DNS

Se implementar troca de DNS:

não chamar automaticamente de FPS boost.

DNS normalmente afeta resolução de nomes, não latência contínua do jogo.

Deve:

- detectar adapters ativos;
- preservar configuração original;
- permitir DHCP/original;
- verificar;
- rollback.

Preferencialmente Custom/Medium conforme caso.

---

# 35. TCP/IP

Não aplicar pacote universal de:

netsh tweaks

em todo PC.

Configurações de TCP devem considerar:

- Windows version;
- adapter;
- workload;
- estado atual.

Sem evidência:

não incluir.

---

# 36. GAMING

Criar categoria Gaming com rigor.

Gaming optimization pode focar:

- redução de background contention;
- power behavior;
- graphics readiness;
- Windows gaming features;
- startup/background;
- visual overhead;
- driver readiness futuramente.

Não usar tweak placebo.

---

# 37. GAME MODE

Avaliar estado e comportamento do Windows Game Mode.

Não assumir que desligar ou ligar é universalmente melhor.

Usar documentação Microsoft atual.

Se recomendação depender de versão:

modelar compatibilidade.

---

# 38. GAME DVR / CAPTURE

Avaliar captura/background recording.

Pode haver benefício em desabilitar gravação que o cliente não usa.

Mas isso remove funcionalidade.

Portanto:

- detectar estado;
- explicar side effect;
- rollback;
- não tratar como universal.

---

# 39. HAGS

Hardware-Accelerated GPU Scheduling deve ser tratado como hardware/driver/workload dependent.

Não aplicar universalmente.

Se incluído:

Advanced/Custom ou Conditional.

Não prometer aumento garantido de FPS.

Pode exigir reboot.

---

# 40. GPU VENDOR SETTINGS

Não modificar diretamente configurações específicas NVIDIA/AMD/Intel sem documentação e mecanismo robusto.

Drivers serão aprofundados na Fase 6.

Não criar hacks de Registry do driver.

---

# 41. SECURITY TRADE-OFFS

Esta é uma regra crítica.

Não desabilitar automaticamente:

- Defender;
- Firewall;
- Secure Boot;
- SmartScreen;
- UAC;
- VBS;
- Memory Integrity;
- Credential Guard;
- exploit protections.

para obter performance.

---

# 42. SECURITY REDUCTION

Se alguma configuração de segurança for estudada por impacto de performance:

classificar como:

SecurityTradeoff

e no mínimo:

Advanced/Aggressive.

Exigir:

- warning explícito;
- impacto de segurança;
- consentimento;
- compatibilidade;
- rollback;
- documentação.

Preferência nesta fase:

não incluir em preset automático.

---

# 43. DEFENDER EXCLUSIONS

Não criar exclusões amplas como:

C:\
Games\
Steam\

automaticamente.

Isso reduz segurança.

Por padrão:

EXCLUDE do catálogo V1.

---

# 44. TELEMETRY / PRIVACY

Separar:

Privacy optimization

de:

Performance optimization.

Uma configuração de privacidade não deve ser vendida como FPS tweak sem evidência.

Pode existir categoria Privacy.

---

# 45. WINDOWS UPDATE

Não desabilitar Windows Update permanentemente.

Pode futuramente existir comportamento controlado durante sessão de benchmark/otimização, mas não nesta fase sem necessidade.

Não quebrar servicing do Windows.

---

# 46. APPX / DEBLOAT

Debloat exige cuidado.

Não remover pacote apenas porque é pré-instalado.

Criar classificação:

SafeOptional
UserFacingOptional
DependencySensitive
SystemComponent
Unknown

Unknown:

não remover automaticamente.

---

# 47. APPX REMOVAL

Se AppX entrar no catálogo:

- detectar instalação;
- identificar package family;
- verificar dependências;
- distinguir provisioned/current user;
- registrar rollback limitations;
- não afirmar reversibilidade total quando reinstalação depender da Store/rede.

Não incluir cedo apenas para aumentar quantidade.

---

# 48. ONEDRIVE

Não remover/desinstalar automaticamente.

Pode conter dados sincronizados do cliente.

Qualquer alteração:

Custom e confirmação explícita.

---

# 49. SEARCH / INDEXING

Não desabilitar Windows Search universalmente.

Impacto depende do usuário.

Pode prejudicar experiência.

Se catalogado:

Conditional/Custom.

---

# 50. PRINT / BLUETOOTH / XBOX ETC.

Não desabilitar serviço porque "o gamer talvez não use".

Scanner deve comprovar contexto suficiente ou usuário deve escolher explicitamente.

Exemplo:

não desabilitar Bluetooth em notebook com dispositivo Bluetooth.

---

# 51. HARDWARE AWARENESS

Cada optimization deve considerar quando relevante:

- Desktop;
- Laptop;
- VM;
- CPU vendor;
- GPU vendor;
- integrated/dedicated GPU;
- storage type;
- battery;
- OS;
- build;
- edition;
- capabilities.

---

# 52. WINDOWS 10 / WINDOWS 11

Não usar catálogo único cego.

OptimizationDefinition deve declarar compatibilidade.

Testar pelo menos:

Windows 10 22H2 / build 19045

Windows 11 suportado pelo projeto

e Unknown/Unsupported.

Windows 10 continua target legado conforme arquitetura aprovada.

---

# 53. DETECTION

Toda operação deve detectar estado atual antes de Apply.

Estados:

AlreadyOptimized
NeedsChange
NotApplicable
Blocked
Unknown

Unknown:

não executar automaticamente.

---

# 54. ALREADY OPTIMIZED

Se desired state já existe:

não escrever novamente.

Resultado:

AlreadySatisfied

ou equivalente.

Não gerar snapshot/mutação desnecessária quando não necessário.

---

# 55. SNAPSHOT

Toda operação modificadora reversível deve capturar estado original.

Snapshot deve ser:

- específico;
- suficiente;
- persistido;
- verificado;
- associado à sessão/operação.

Não depender apenas de Restore Point.

---

# 56. ROLLBACK

Toda otimização marcada SupportsUndo deve provar rollback.

Teste mínimo:

Detect
→ Snapshot
→ Apply
→ Verify
→ Rollback
→ Verify Original

Se não puder provar:

SupportsUndo=false

e UI deve deixar claro.

---

# 57. EXTERNAL CHANGE

Preservar comportamento aprovado da Fase 4.

Se estado foi alterado externamente após Apply:

não sobrescrever cegamente durante rollback.

---

# 58. AGENT

Toda operação privilegiada passa pelo Agent.

Agent deve validar contra catálogo canônico confiável.

Nunca confiar em:

target vindo da UI
desired state vindo da UI
service name vindo da UI
registry path vindo da UI
command vindo da UI

sem correspondência exata com definição canônica.

---

# 59. NOVOS HANDLERS

Adicionar handlers somente quando necessários.

Cada handler:

- tipado;
- pequeno;
- testável;
- allowlisted;
- sem execução genérica.

Possíveis handlers:

Registry
Service
Power
FileCleanup

somente se necessários para as operações aprovadas.

---

# 60. POWERSHELL

Não usar PowerShell como atalho arquitetural.

Preferir:

.NET API
Win32 API
documented Windows API

Se uma funcionalidade exigir PowerShell:

usar adapter específico com operação fixa e parâmetros allowlisted.

Nunca:

ExecutePowerShell(string script).

---

# 61. COMMAND-LINE TOOLS

Para ferramentas oficiais como:

powercfg
DISM
PnPUtil

se necessárias:

criar adapter específico.

Argumentos devem ser construídos por código a partir de enums/IDs validados.

Não aceitar command line da UI.

---

# 62. PRESET ENGINE

Implementar seleção determinística.

Input:

SystemSnapshot
AnalysisResult
Catalog
Preset

Output:

PresetSelection

Cada item:

Selected
Excluded
Blocked
NotApplicable
RequiresConfirmation

com razão.

---

# 63. BASIC POLICY

Basic deve excluir:

- Advanced;
- Aggressive;
- Experimental;
- SecurityTradeoff;
- irreversible;
- unknown compatibility;
- unknown detection;
- hardware-risky;
- reboot-heavy quando desnecessário.

---

# 64. MEDIUM POLICY

Medium pode incluir:

Safe
Medium

com:

Strong/Moderate evidence

e compatibilidade suficiente.

Experimental:

não automático.

---

# 65. ADVANCED POLICY

Advanced pode selecionar itens Advanced compatíveis.

Aggressive deve preferencialmente exigir confirmação individual ou grupo explicitamente destacado.

Não esconder riscos.

---

# 66. CUSTOM POLICY

Custom pode expor itens adicionais.

Mas:

Blocked continua impossível de executar.

Custom não significa bypass de segurança.

---

# 67. DEPENDÊNCIAS

Se Optimization A requer B:

planner deve ordenar.

Se B blocked:

A blocked.

Não executar A parcialmente ignorando requisito.

---

# 68. CONFLITOS

Se A conflita com B:

preset não pode selecionar ambos silenciosamente.

UI deve explicar.

---

# 69. REBOOT

Agrupar operações para evitar múltiplos reboots.

Não reiniciar automaticamente.

Ao final:

RebootRequired

quando necessário.

---

# 70. PROGRESSO

Progresso deve usar operações reais.

Exemplo:

Preparando
Criando proteção
Aplicando 3/12
Verificando 3/12
Finalizando

Não usar progresso baseado em timer.

---

# 71. UI — OTIMIZAÇÃO

A página Otimização deve evoluir para uso real.

Mostrar:

Básico
Médio
Avançado
Personalizado

Para cada preset:

- descrição;
- risco;
- quantidade elegível;
- warnings;
- reboot;
- rollback coverage.

---

# 72. BASIC CARD

Exemplo de linguagem:

"Alterações de baixo risco e ampla compatibilidade."

Não:

"FPS EXTREMO".

---

# 73. MEDIUM CARD

Exemplo:

"Otimização equilibrada com alterações adicionais de sistema."

Mostrar possíveis side effects.

---

# 74. ADVANCED CARD

Exemplo:

"Configurações avançadas que podem alterar comportamento, recursos ou compatibilidade."

Exigir confirmação clara.

---

# 75. CUSTOM

Lista por categorias com:

checkbox
title
description
risk
impact
compatibility
rollback
details

Blocked:

checkbox desabilitado.

---

# 76. BEFORE APPLY

Tela Review deve mostrar:

- preset;
- itens;
- operations;
- risk summary;
- warnings;
- security tradeoffs;
- reboot;
- restore/snapshot status;
- rollback coverage.

---

# 77. CONFIRMATION

Basic/Medium:

confirmação normal.

Advanced:

confirmação reforçada.

Aggressive/SecurityTradeoff:

confirmação explícita individual ou equivalente.

---

# 78. AFTER APPLY

Mostrar:

Applied
AlreadySatisfied
Skipped
Blocked
Failed
VerificationFailed
OutcomeUnknown
RollbackAvailable
RebootRequired

Não mostrar apenas:

"100% otimizado".

---

# 79. RESULTS

Preparar dados para Fase 6.

Registrar:

BeforeState
Operation
AfterState
Verification
Duration
RollbackAvailability

Sem ainda implementar relatório final completo.

---

# 80. BOREAL SCORE

Ainda não transformar Boreal Score em claim comercial.

Pode integrar estruturalmente quando já previsto.

Não aumentar score simplesmente porque mais tweaks foram aplicados.

---

# 81. CATALOG MANIFEST

Criar catálogo estruturado/versionado.

Cada definição deve possuir versão.

Catálogo deve possuir:

CatalogVersion
SchemaVersion
Definitions
Hash
Source metadata

Built-in catalog é confiável por fazer parte do binário/release.

---

# 82. UPDATED CATALOG

Não implementar download remoto inseguro.

Se foundation existir:

UpdatedCatalog só pode ser aceito futuramente com:

- assinatura válida;
- publisher confiável;
- schema;
- version;
- anti-downgrade.

Sem assinatura:

não executar catálogo remoto.

---

# 83. CATÁLOGO V1 — TAMANHO

Não perseguir número artificial.

Objetivo sugerido:

aproximadamente 15–30 OptimizationDefinitions realmente defensáveis.

Se apenas 12 passarem nos critérios:

implementar 12.

Se 25 forem boas:

implementar 25.

Não criar 100 para marketing.

---

# 84. DISTRIBUIÇÃO SUGERIDA

Como objetivo, não obrigação:

Safe:
6–12

Medium:
5–10

Advanced:
3–8

Aggressive/Experimental:
0–5

É aceitável ter zero Aggressive se nenhuma técnica atingir padrão suficiente.

---

# 85. TABELA DO CATÁLOGO

Criar:

OPTIMIZATION_CATALOG.md

Para cada item:

OptimizationId
Title
Category
Preset
Risk
Evidence
ExpectedImpact
Windows
Detection
Operation
Verification
Rollback
Reboot
SideEffects
EvidenceReferences

---

# 86. REJECTED TWEAKS

Criar seção:

Rejected / Excluded Tweaks

Documentar técnicas pesquisadas e rejeitadas.

Exemplos possíveis:

HPET hacks
timer hacks
BCD hacks
pagefile disable
Defender disable
Firewall disable
arbitrary service disable
universal TCP tweaks

Registrar motivo.

Isso evita que técnicas ruins sejam reintroduzidas futuramente.

---

# 87. WINUTIL ANALYSIS

Atualizar WINUTIL_ANALYSIS.md quando uma função do WinUtil for estudada.

Classificar:

Adopt
Adapt
Reject
Deferred

Explicar.

Não copiar implementação cegamente.

---

# 88. TESTE POR OPTIMIZATION

Cada OptimizationDefinition real precisa de testes.

No mínimo:

Detection positive
Detection negative
Compatibility
Unknown
AlreadySatisfied
Plan
Snapshot
Apply
Verify
Rollback
OriginalStateRestored

Quando aplicável.

---

# 89. TESTE DE PRESET

Criar fixtures:

DesktopGaming
LaptopGaming
OfficeDesktop
VM
Windows10Legacy
Windows11
LowEndPC
UnknownHardware

Executar:

Basic
Medium
Advanced

Validar seleção.

---

# 90. LAPTOP SAFETY

Em Laptop:

não selecionar automaticamente ajustes agressivos de energia.

Battery deve restringir ainda mais.

Testar.

---

# 91. VM SAFETY

VM deve bloquear hardware tweaks incompatíveis.

Testar.

---

# 92. UNKNOWN SAFETY

Unknown:

não pode virar automatic selection.

Teste global no PresetEngine.

---

# 93. SECURITY SAFETY TEST

Criar teste que garanta que Basic e Medium nunca selecionem:

SecurityTradeoff

mesmo que definição seja adicionada acidentalmente.

---

# 94. AGENT CATALOG TAMPER

Testar:

OptimizationId válido
+
OperationId válido
+
target alterado

REJECT.

DesiredState alterado:

REJECT.

OperationType alterado:

REJECT.

CatalogVersion alterada:

REJECT.

---

# 95. CROSS-PROCESS

Preservar lock aprovado na Fase 4.

Não permitir duas sessões.

Testar após expansão do catálogo.

---

# 96. ROLLBACK MATRIX

Executar operações reais em ambiente seguro.

Para cada tipo de handler novo:

provar pelo menos uma operação end-to-end:

Before
→ Apply
→ Verify
→ Rollback
→ Exact Before

Não testar em configuração crítica sem proteção adequada.

---

# 97. REAL MACHINE VALIDATION

Executar Scanner + Analysis + Preset Preview na máquina atual.

Antes de aplicar:

registrar quais itens seriam selecionados.

Aplicar automaticamente somente itens que tenham sido classificados seguros para validação real.

Advanced/Aggressive:

não executar em lote na máquina de desenvolvimento sem revisão explícita.

---

# 98. WINDOWS 10 VALIDATION

Windows 10 22H2 real/VM continua pendência importante.

Não declarar uma OptimizationDefinition Windows 10 validated se só foi testada no Windows 11.

Distinguir:

SupportedByDesign
UnitTested
VMValidated
HardwareValidated

ou equivalente.

---

# 99. SAFETY GATE

Antes de cada Wave:

executar:

restore
build
tests
security tests

Após cada Wave:

repetir.

Não esperar implementar catálogo inteiro para descobrir quebra de rollback.

---

# 100. BUILD

Ao final executar:

dotnet --info

dotnet restore .\BorealBoost.sln

dotnet build .\BorealBoost.sln --no-restore

dotnet test .\BorealBoost.sln --no-build

Esperado:

0 errors.

Investigar warnings.

---

# 101. DEPENDÊNCIAS

Executar:

dotnet list .\BorealBoost.sln package --vulnerable

dotnet list .\BorealBoost.sln package --outdated

Não atualizar automaticamente.

---

# 102. SECURITY SEARCH

Buscar:

ExecuteCommand
ExecutePowerShell
ExecuteProcess
Process.Start
cmd.exe
powershell.exe
pwsh.exe
ShellExecute
Registry.SetValue
CreateSubKey
DeleteSubKey
ServiceController.Start
ServiceController.Stop
powercfg
netsh
DISM
SFC
PnPUtil
winget
AppX
bcdedit

Classificar toda ocorrência nova.

Uso de ferramenta oficial deve estar atrás de adapter tipado e allowlisted.

---

# 103. CÓDIGO PERIGOSO

Qualquer implementação equivalente a:

Execute(string command)

é proibida.

Qualquer implementação equivalente a:

Registry.SetValue(pathFromUI, ...)

é proibida.

Qualquer implementação equivalente a:

ServiceController(serviceFromUI)

sem validação canônica é proibida.

---

# 104. DOCUMENTAÇÃO

Atualizar:

- ARCHITECTURE.md
- ARCHITECTURE_DECISION_RECORD.md
- DOMAIN_MODEL.md
- OPTIMIZATION_ENGINE.md
- OPTIMIZATION_EXECUTION.md
- ROLLBACK.md
- SECURITY.md
- IMPLEMENTATION_ROADMAP.md
- UX_SPECIFICATION.md
- WINUTIL_ANALYSIS.md

Criar:

OPTIMIZATION_CATALOG.md

Documentar apenas comportamento realmente implementado.

---

# 105. CRITÉRIOS DE ACEITAÇÃO

Fase 5 somente pode ser considerada concluída quando:

- catálogo V1 real existir;
- catálogo for versionado;
- cada optimization possuir ID estável;
- evidence estiver documentada;
- risk estiver documentado;
- compatibility estiver documentada;
- detection existir;
- Unknown bloquear auto-apply;
- Basic existir;
- Medium existir;
- Advanced existir;
- Custom existir;
- PresetEngine for determinístico;
- conflitos forem respeitados;
- dependências forem respeitadas;
- operações passarem pelo Agent;
- Agent validar catálogo canônico;
- snapshot ocorrer antes de write;
- verification ocorrer depois;
- rollback funcionar para operações reversíveis;
- security tradeoffs não entrarem silenciosamente;
- UI permitir review antes de Apply;
- progresso for real;
- resultados forem estruturados;
- testes passarem;
- build passar;
- nenhuma execução arbitrária existir.

---

# 106. ENTREGA FINAL

Ao concluir, apresentar:

## Summary

## Research Performed

Listar fontes/técnicas pesquisadas.

## Catalog Architecture

## Catalog Version

## Optimizations Implemented

Criar tabela:

OptimizationId
Title
Category
Preset
Risk
Evidence
Impact
Windows
Rollback
Reboot

## Safe Wave

## Medium Wave

## Advanced Wave

## Aggressive / Experimental Wave

## Rejected Tweaks

Listar técnicas rejeitadas e motivo.

## Preset Engine

Informar quantidade selecionada em:

Basic
Medium
Advanced

nos principais fixtures.

## Agent Handlers

Listar handlers novos e allowlists.

## Safety

## Rollback Coverage

Informar:

Total modifying optimizations
Reversible
Irreversible
Requires reboot
SecurityTradeoff

## Real Machine Preview

Executar:

Scanner
→ Analysis
→ Basic Preview
→ Medium Preview
→ Advanced Preview

Informar quantidades e blockers.

Não expor dados pessoais.

## Controlled Runtime Validation

Informar quais otimizações foram realmente aplicadas para validação.

Para cada uma:

Before
Apply
Verify
Rollback
Final State

## Tests

- novos;
- total;
- pass/fail.

## Build

- restore;
- build;
- tests;
- warnings/errors.

## Dependencies

## Security Search

## Windows Compatibility

Separar:

Windows 10
Windows 11

e nível de validação.

## Remaining Risks

## Pending Validation

## Git Diff Review

---

# 107. RESPOSTAS OBRIGATÓRIAS

Responder explicitamente:

1. Basic pode selecionar Advanced/Aggressive?
2. Basic ou Medium podem selecionar SecurityTradeoff?
3. Unknown pode ser aplicado automaticamente?
4. Item Blocked pode ser executado via Custom?
5. Agent aceita target arbitrário?
6. Agent aceita command/script arbitrário?
7. Cada write possui detection?
8. Cada write reversível possui snapshot?
9. Apply exige verification?
10. SupportsUndo implica rollback realmente implementado?
11. Mudança externa continua protegida?
12. Duas sessões podem executar simultaneamente?
13. Existe tweak com claim numérico de FPS sem benchmark?
14. Defender foi desativado?
15. Firewall foi desativado?
16. Windows Update foi permanentemente desativado?
17. Pagefile foi desativado automaticamente?
18. Algum BCD/timer hack entrou em preset?
19. Quantas OptimizationDefinitions reais existem?
20. Quantas são Safe?
21. Quantas são Medium?
22. Quantas são Advanced?
23. Quantas são Aggressive/Experimental?
24. Quantas possuem rollback comprovado?
25. Fase 6 foi iniciada?

Esperado para 1–18:

1. NÃO
2. NÃO
3. NÃO
4. NÃO
5. NÃO
6. NÃO
7. SIM
8. SIM
9. SIM
10. SIM
11. SIM
12. NÃO
13. NÃO
14. NÃO
15. NÃO
16. NÃO
17. NÃO
18. NÃO

25. NÃO

Para 19–24:

responder valores reais, sem inflar números.

---

# 108. REGRA FINAL

A Fase 5 não deve tentar provar que o BorealBoost possui "mais tweaks".

Ela deve provar que possui:

MELHORES DECISÕES
+
OPERAÇÕES CONTROLADAS
+
COMPATIBILIDADE
+
SEGURANÇA
+
ROLLBACK
+
TRANSPARÊNCIA.

O objetivo comercial é obter melhora real quando houver oportunidade na máquina.

O objetivo técnico é evitar placebo, quebra de Windows e otimizações universais sem contexto.

Uma otimização agressiva tecnicamente defensável é melhor do que dez tweaks placebo.

Uma otimização insegura ou sem evidência deve ser rejeitada, mesmo que seja popular em ferramentas de "FPS boost".

Não faça commit automaticamente.

Não inicie a Fase 6.