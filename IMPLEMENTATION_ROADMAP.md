# BorealBoost - Implementation Roadmap

Data: 2026-08-12
Status: roadmap proposto. Implementacao depende de aprovacao da arquitetura.

## Gate atual

Fase 0 deve ser aprovada antes de qualquer codigo.

Nao iniciar Fase 1 ate:

- arquitetura aprovada;
- stack aprovada;
- modelo de privilegio aprovado;
- matriz Windows 10/11 aprovada;
- politica de restore point/rollback aprovada;
- escopo V1 confirmado;
- contrato obrigatorio do `BorealBoost.Agent` aprovado;
- catalogo confiavel e modelo transacional aprovados.

## Ordem obrigatoria do roadmap

A ordem da implementacao deve permanecer:

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

Tweaks agressivos nao devem ser antecipados. Presets Advanced/Aggressive dependem de engine, safety, recovery e validacao em VM.

## Fase 0 - Discovery e arquitetura

Status: entregue nesta sessao.

Entregas:

- Discovery.
- Requirements.
- WinUtil analysis.
- Architecture.
- ADR.
- Domain model.
- Optimization Engine.
- Driver Engine.
- Rollback Engine.
- Boreal Score.
- UX specification.
- Compatibility Matrix.
- Security.
- Third-party notices.
- Roadmap.

## Fase 1 - Foundation

Objetivo:

- criar solution .NET;
- projetos base;
- DI;
- logging;
- configuracao;
- WinUI shell;
- navigation;
- tema;
- contratos Core;
- `BorealBoost.Agent` elevado obrigatorio, ainda sem aplicar tweaks reais;
- contrato App-Agent: pipe, protocolo, handshake, ACL, timeout e validacao inicial;
- testes unitarios iniciais.

Arquivos esperados:

- `.sln`;
- projetos `BorealBoost.*`;
- estrutura de pastas;
- build/test pipeline local;
- README inicial.

Validacao:

- build;
- testes unitarios;
- app abre;
- tema base carrega;
- navigation basica;
- admin status detectado sem executar tweaks;
- Agent sobe elevado em fluxo controlado e encerra por timeout, sem aceitar comandos arbitrarios.

## Fase 2 - Scanner

Objetivo:

- coletar SystemProfile real.

Modulos:

- OS;
- device;
- CPU;
- GPU;
- RAM;
- storage;
- network;
- power;
- services;
- processes;
- startup;
- security;
- drivers inventory basico.

Validacao:

- testes unitarios de parsing;
- smoke em Windows 10/11 VM;
- logs sem secrets;
- UI nao trava.

Status atual: implementado e revalidado na Fase 2 como scanner read-only modular. A correcao pos-auditoria adicionou sessao unica de scan, WMI sem tarefas abandonadas por timeout, VRAM conservadora, memoria instalada/visivel separada, capabilities de seguranca read-only e politica explicita de redaction. Validacao local real feita na maquina disponivel; Windows 10 22H2 e matriz completa de VMs permanecem pendentes.

## Fase 3 - Analysis e Recommendations

Objetivo:

- transformar scanner em findings e recomendacoes.

Entregas:

- HealthAnalyzer;
- BottleneckAnalyzer;
- RecommendationEngine;
- PresetEngine inicial sem apply real;
- Boreal Score beta.

Validacao:

- regras unitarias;
- fixtures de SystemProfile;
- nenhum FPS inventado.

## Fase 4 - Optimization Engine Core

Objetivo:

- implementar catalogo confiavel, detection, compatibility e execution plan sem aplicar tweaks destrutivos inicialmente.

Entregas:

- schema;
- validacao de assinatura/hash/schema/catalogVersion;
- separacao built-in vs updated;
- validator;
- CompatibilityEngine;
- DetectionEngine;
- ExecutionPlanner;
- UI de revisao.

Validacao:

- preset referencia IDs validos;
- regras Windows 10/11;
- blocked/incompatible visiveis;
- Agent revalida ExecutionPlan e rejeita operacao fora da allowlist.

## Fase 5 - Safety

Objetivo:

- snapshot, restore point, rollback framework, logs transacionais e recovery.

Entregas:

- SnapshotService;
- RestorePointService;
- RollbackEngine;
- session persistence;
- transaction journal;
- incomplete session recovery;
- UI Restaurar.

Validacao:

- testes de serializacao;
- teste de restore point em VM;
- rollback de operacoes simuladas;
- falha nao marca sucesso;
- crash/reboot simulado nao marca sessao como concluida.

## Fase 6 - Primeiro lote de otimizacoes Safe

Objetivo:

- implementar poucos tweaks seguros, totalmente auditados.
- nenhum tweak agressivo nesta fase.

Candidatos:

- temp cleanup com escopo seguro;
- detectar/aplicar configuracoes simples e reversiveis;
- Storage Sense conservador;
- Game Mode detect/toggle quando documentado;
- DNS profile com reset.

Validacao:

- Before/Apply/Verify/Undo/Verify original state.
- Windows 10 e Windows 11.

## Fase 7 - Drivers

Objetivo:

- Driver Engine diagnostico e fluxo assistido.

Entregas:

- DriverScanner;
- DriverHealthAnalyzer;
- SourceResolver;
- signature checks;
- INF/CAT, Authenticode, publisher validation e hardware ID matching;
- plano de instalacao;
- UI Drivers.

Validacao:

- dispositivos com problem code simulados/VM;
- PnPUtil/SetupAPI em Windows 10/11;
- sem downloads de fonte nao oficial;
- sem scraping generico.

## Fase 8 - Results, Benchmark e Reporting

Objetivo:

- baseline antes/depois;
- calibracao do Boreal Score para candidato v1 quando houver dataset suficiente;
- relatorio HTML/PDF.

Validacao:

- metricas reais;
- PDF profissional;
- relatorio nao promete ganhos nao medidos.

## Fase 9 - Installer e Release Readiness

Objetivo:

- MSI;
- signing readiness;
- uninstall;
- diretorios corretos;
- logs/snapshots fora de binarios;
- update check.

Validacao:

- install/uninstall em VM limpa;
- UAC correto;
- app abre apos install;
- rollback ainda funciona.

## Fase 10 - Hardening

Objetivo:

- teste de sistema;
- VMs;
- falhas;
- accessibility;
- performance UI;
- production checklist.

Entregas:

- `TESTING.md`;
- `PRODUCTION_READINESS.md`;
- matriz validada;
- release checklist.

## Criterios para V1

V1 so pode ser considerada vendavel quando:

- instala e remove corretamente;
- abre e solicita admin corretamente;
- scanner confiavel;
- presets funcionam;
- cada tweak possui compatibility, detection, apply, verify e undo;
- restore point e snapshot funcionam;
- rollback validado;
- logs estruturados;
- UI nao trava;
- reports funcionam;
- nenhum download inseguro;
- testes essenciais passam;
- documentacao existe.

## Proximo passo recomendado

Apos aprovacao explicita: iniciar Fase 1 com solution .NET 10 + WinUI 3, shell, tema, DI, logging, contratos Core e contrato App-Agent stub, sem ainda implementar tweaks reais.
