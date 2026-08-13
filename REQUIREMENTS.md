# BorealBoost - Requirements

Data: 2026-08-12
Status: atualizado ate a Fase 3. Este documento consolida requisitos e estado atual.

## Objetivo do produto

BorealBoost sera uma aplicacao desktop comercial para tecnico presencial diagnosticar, recomendar, aplicar e validar otimizacoes em Windows 10 x64 e Windows 11 x64, com reversibilidade, logs, evidencias e relatorio.

O produto nao deve vender "magia". Toda melhoria deve ser explicavel, compativel, mensuravel quando alegar performance e reversivel quando tecnicamente possivel.

## Escopo V1

Incluido:

- diagnostico da maquina;
- dashboard premium;
- scanner de sistema, hardware, drivers, servicos, startup, armazenamento, energia e seguranca;
- presets Basico, Medio, Avancado e Personalizado;
- catalogo de otimizacoes declarativo;
- compatibility rules por OS/build/hardware;
- snapshot antes/depois;
- restore point antes de operacoes relevantes;
- rollback por item e sessao;
- logs estruturados;
- Boreal Score com metodologia documentada;
- antes/depois com metricas reais;
- relatorio HTML/PDF;
- instalador profissional;
- arquitetura preparada para updates futuros.

Fora da V1:

- SaaS remoto;
- marketplace;
- multiusuario;
- cobranca/assinatura;
- conta publica;
- overclock;
- BIOS/UEFI tuning;
- driver updater baseado em mirrors genericos;
- execucao de scripts remotos arbitrarios.

## Requisitos funcionais

| Area | Requisito | Status atual |
| --- | --- | --- |
| Produto desktop | Aplicacao Windows comercial premium | Nao implementado |
| Stack | Escolha documentada de tecnologia | Documentado nesta fase |
| Dashboard | Boreal Score, resumo da maquina, CTA de analise | Nao implementado |
| Scanner | OS, hardware, storage, network, devices, drivers, displays, power, services, processes, startup e capabilities | Implementado na Fase 2 como inventario read-only |
| Drivers | Diagnosticar ausentes, erro, genericos, versao e IDs | Nao implementado |
| Otimizacao | Catalogo declarativo com detect/apply/verify/undo | Nao implementado |
| Analysis/Recommendation | Transformar `SystemSnapshot` em findings, oportunidades, warnings e recomendacoes estruturadas | Implementado na Fase 3 como engine read-only, sem apply |
| Presets | Safe Boost, Performance, Extreme, Personalizado | Parcial: preview Basico/Medio/Avancado/Custom, sem aplicacao |
| Compatibilidade | Regras por Windows 10/11, build, hardware e notebook/desktop | Parcial: compatibilidade de recomendacao na Fase 3; compatibility operacional de otimizacoes ainda futura |
| Restore Point | Criar antes de operacoes relevantes | Nao implementado |
| Snapshot | Capturar valores anteriores por item | Nao implementado |
| Rollback | Reverter item ou sessao e verificar | Nao implementado |
| Benchmark | Baseline e comparacao sem inventar metricas | Nao implementado |
| Reporting | PDF/HTML profissional | Nao implementado |
| Logs | Structured logs por sessionId/operationId | Nao implementado |
| Admin | Elevar corretamente sem prompt por comando | Nao implementado |
| Installer | Instalacao, uninstall, atalhos, dados em ProgramData/AppData | Nao implementado |
| Updates | Arquitetura para manifest, hash, assinatura, release notes | Nao implementado |

## Requisitos nao funcionais

- Seguridad operacional acima de quantidade de tweaks.
- Reversibilidade como requisito de negocio.
- UI responsiva em DPI 100%, 125%, 150% e 200%.
- Sem congelar UI durante operacoes longas.
- Logs sem secrets.
- Mensagens de erro amigaveis com detalhes tecnicos expansiveis.
- Testes unitarios, integracao e sistema em VMs.
- Validacao Windows 10 e Windows 11 separada.
- Documentacao atualizada antes de declarar V1 pronta.

## Politicas de risco

RiskLevel:

- `Safe`: baixo risco, conserva funcionalidade.
- `Medium`: alteracao futura mais relevante, normalmente reversivel, exige revisao.
- `Advanced`: pode alterar funcionalidade secundaria, compatibilidade ou comportamento.
- `Aggressive`: prioriza performance sobre conveniencia, exige confirmacao forte.

EvidenceLevel:

- `Strong`: fato objetivo e justificativa tecnica direta.
- `Moderate`: beneficio plausivel, dependente de contexto.
- `Experimental`: evidencia limitada.
- `Unknown`: nao usar para recomendacao automatica.

## Requisitos de cada otimizacao

Cada entrada do catalogo deve responder:

1. O que muda.
2. Onde muda.
3. Qual API/comando.
4. Valor atual.
5. Valor proposto.
6. Por que pode ajudar.
7. Evidencia/documentacao.
8. Windows 10.
9. Windows 11.
10. Build minimo/maximo.
11. Desktop/notebook.
12. Dependencias.
13. Conflitos.
14. Risco.
15. Reboot/logout.
16. Como detectar aplicado.
17. Como verificar.
18. Como desfazer.
19. Como testar.
20. Como registrar em log.

Se nao puder responder, a otimizacao nao entra em preset automatico.

## Classificacao frente ao Master Spec

| Requisito macro | Classificacao atual |
| --- | --- |
| Fase 0 Discovery | Parcial antes da sessao, documentado nesta sessao |
| Arquitetura modular | Documentado, nao implementado |
| Stack C#/.NET/Windows nativo | Decidido, nao implementado |
| UX premium | Especificado, nao implementado |
| Scanner | Implementado na Fase 2 como `SystemSnapshot` read-only |
| Driver Engine | Projetado, nao implementado |
| Optimization Engine | Projetado, nao implementado |
| Analysis/Recommendation Engine | Implementado na Fase 3 como read-only sobre `SystemSnapshot` |
| Compatibility Engine | Parcial em recomendacoes da Fase 3; operacional de otimizacoes ainda futuro |
| Rollback Engine | Projetado, nao implementado |
| Boreal Score | Metodologia inicial documentada, precisa calibracao; nao operacional |
| Testes | Foundation, Scanner e Analysis possuem testes locais; matriz completa de VM ainda pendente |
| Licencas | Pesquisa inicial feita, sem incorporacao de terceiros |

## Pendencias

- Validar todas as otimizacoes candidatas em VMs antes de escrever catalogo.
- Confirmar policy comercial para continuar sem restore point caso o Windows bloqueie novo ponto no mesmo dia.
- Confirmar suporte oficial a Windows 10 fora do ciclo regular Microsoft.
