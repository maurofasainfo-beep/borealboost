# BorealBoost - Implementation Roadmap

Data: 2026-08-13
Status: roadmap consolidado a partir da Fase 3.

## Principios de Ordem

A implementacao continua faseada. Nenhuma fase pode antecipar apply real, rollback, driver install, benchmark ou tweaks agressivos antes de seus contratos de safety estarem prontos.

Ordem historica aprovada ate a Fase 2:

1. Foundation
2. Scanner
3. Analysis
4. Optimization Engine Core
5. Safety
6. Safe Tweaks
7. Drivers
8. Benchmark/Reporting
9. Installer
10. Hardening

A partir da Fase 3, a execucao passa ao roadmap consolidado abaixo. Ele preserva a ordem logica e reduz fragmentacao operacional.

## Fase 0 - Discovery e Arquitetura

Status: aprovado.

Entregas:

- Discovery.
- Requirements.
- WinUtil analysis.
- Architecture.
- ADR.
- Domain model.
- Optimization Engine architecture.
- Driver Engine architecture.
- Rollback Engine architecture.
- Boreal Score methodology beta.
- UX specification.
- Compatibility Matrix.
- Security.
- Third-party notices.
- Roadmap.

## Fase 1 - Foundation

Status: aprovado.

Entregas:

- solution .NET 10;
- projetos base;
- DI/config/logging/paths;
- WinUI shell, navigation e tema;
- status administrativo real;
- `BorealBoost.Agent` foundation obrigatorio;
- IPC App-Agent tipado com handshake/status/shutdown;
- testes unitarios, integracao e sistema da Foundation.

Limite:

- 0 handlers administrativos reais;
- nenhuma execucao arbitraria.

## Fase 2 - System Scanner

Status: aprovado.

Entregas:

- `SystemSnapshot` read-only;
- providers OS, CPU, GPU, memory, storage, hardware/firmware, displays, network, devices, drivers, power, services, processes, startup e security capabilities;
- scan session singleton;
- WMI sem tarefas abandonadas por timeout;
- VRAM conservadora;
- memoria instalada/visivel separada;
- politica de redaction;
- UI Scanner e Dashboard factual.

Limite:

- sem recommendations;
- sem Optimization Engine;
- sem Driver Engine operacional;
- sem Boreal Score.

## Fase 3 - Analysis + Recommendation Engine

Status: implementada e corrigida apos auditoria final, pendente apenas de validacoes reais listadas nos relatorios.

Objetivo:

- transformar `SystemSnapshot` em findings, oportunidades, warnings, bloqueios e recomendacoes estruturadas.

Entregas:

- contratos `IAnalysisEngine`, `IAnalysisRule`, `AnalysisResult`, `Recommendation`, `RecommendationPlan`;
- regras modulares code-first com `RuleId` estavel;
- risk, evidence, compatibility e expected impact qualitativos;
- preset preview Basico, Medio, Avancado e Custom;
- UI `Analise` com filtros e cards;
- validacao central de invariantes de Recommendation/Plan;
- sessao singleton de Analysis para impedir execucoes concorrentes;
- testes de regras, determinismo, Unknown, presets, failure isolation, validacao, concorrencia de sessao e fluxo real Scanner -> Analysis.

Limite:

- read-only;
- sem tweaks;
- sem apply;
- sem rollback;
- sem drivers operacionais;
- sem benchmark;
- sem Boreal Score operacional;
- sem promessa de FPS.

## Fase 4 - Optimization Engine + Safety + Snapshot + Rollback

Objetivo:

- criar o nucleo operacional seguro antes de qualquer lote real de tweaks.

Entregas planejadas:

- Trusted Optimization Catalog com schema, hash, assinatura, publisher e protecao contra downgrade;
- Compatibility/Detection/ExecutionPlan para otimizacoes;
- validacao de ExecutionPlan pelo Agent;
- snapshot por operacao;
- restore point quando aplicavel;
- transaction journal;
- verify;
- rollback por item/sessao;
- recovery de sessao incompleta.

Limite:

- pode usar operacoes simuladas/test fixtures para validar safety;
- nao liberar tweaks reais ate o contrato transacional estar validado.

## Fase 5 - Optimization Catalog - Safe + Medium + Advanced/Aggressive

Objetivo:

- adicionar catalogo de otimizacoes em lotes pequenos, auditados e testados.

Entregas planejadas:

- primeiro lote Safe;
- depois Medium;
- Advanced/Aggressive somente apos testes em VM e warnings/confirmacoes;
- detection/apply/verify/undo por item;
- presets operacionais;
- UI de revisao e selecao.

Politica:

- tweaks agressivos nao sao antecipados;
- protecoes criticas de seguranca nao entram silenciosamente em Basic/Medium;
- cada item precisa de evidencia, risco, compatibilidade e rollback.

## Fase 6 - Drivers + Benchmark + Results + Reporting

Objetivo:

- implementar driver workflow oficial/assistido e resultados mensuraveis.

Entregas planejadas:

- Driver Engine diagnostico e assistido;
- Windows Update/SetupAPI/CfgMgr32/PnPUtil quando seguros e oficiais;
- fonte oficial, INF/CAT, Authenticode, publisher, hash e hardware ID matching;
- sem scraping generico;
- benchmark/baseline/post-state;
- Boreal Score candidato calibrado;
- relatorio HTML/PDF com redaction.

## Fase 7 - Installer + Hardening + Production Readiness

Objetivo:

- preparar release comercial.

Entregas planejadas:

- MSI/WiX;
- signing readiness;
- install/uninstall;
- ACLs finais de ProgramData/AppData;
- update check seguro;
- testes Windows 10/11 em matriz real/VM;
- accessibility/DPI;
- production checklist;
- threat model final.

## Pendencias Transversais

- Validar Windows 10 22H2 x64/build 19045 em VM real.
- Validar notebooks Intel/AMD.
- Validar multi-GPU e VMs Hyper-V/VMware/VirtualBox.
- Definir publisher/certificado oficial.
- Definir retencao de logs e snapshots.
- Escolher biblioteca final de PDF.
- Decidir se historico local exigira SQLite.
