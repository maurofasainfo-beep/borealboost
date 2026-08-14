# BorealBoost - Architecture Decision Record

Data: 2026-08-13
Status: aprovado e atualizado ate a Fase 5

## ADR-001 - Stack de aplicacao desktop

### Decisao

Usar C# com .NET 10 LTS e WinUI 3/Windows App SDK para a aplicacao desktop do BorealBoost.

### Contexto

O produto e exclusivamente Windows, precisa de aparencia premium moderna, acesso a APIs Windows, manutencao de longo prazo, empacotamento profissional e suporte a Windows 10/11 x64.

Fontes relevantes:

- .NET 10 e LTS ativo ate 2028-11-14 conforme politica oficial da Microsoft.
- WinUI 3 e framework nativo recomendado pela Microsoft para novas aplicacoes desktop Windows, com suporte a Windows 10 1809 build 17763+ e Windows 11.
- WPF e maduro, Windows-only, com bom binding/layout, mas nao e a recomendacao principal da Microsoft para novos apps Fluent modernos.
- Avalonia e forte para multiplataforma, mas o BorealBoost nao precisa ser multiplataforma.

### Opcoes avaliadas

#### WPF

Vantagens:

- maduro;
- facil empacotamento desktop;
- bom ecossistema;
- menor complexidade de runtime que WinUI 3;
- acesso .NET direto a APIs Windows.

Desvantagens:

- visual moderno exige mais customizacao;
- menor alinhamento com Fluent/Windows App SDK;
- risco de parecer utilitario legado se design system nao for forte.

#### WinUI 3

Vantagens:

- moderno, Fluent e nativo para novas experiencias Windows;
- bom encaixe com UI premium;
- suporte oficial a Windows 10/11;
- integra Windows App SDK e APIs modernas;
- bom caminho para app comercial atual.

Desvantagens:

- deployment exige atencao ao Windows App SDK runtime;
- alguns controles e tooling podem ser menos maduros que WPF;
- exige decisao clara entre packaged, unpackaged e packaged-with-external-location.

#### Avalonia

Vantagens:

- multiplataforma;
- XAML/C#;
- renderizacao consistente.

Desvantagens:

- requisito nao e multiplataforma;
- adiciona camada nao nativa para produto que modifica profundamente Windows;
- menos alinhado a APIs/UX nativas do Windows.

### Resultado

Escolha: WinUI 3 + Windows App SDK.

Justificativa: melhor equilibrio para uma aplicacao Windows nova, premium, SaaS-like, com UI moderna e suporte Windows 10/11. A complexidade de deployment e aceitavel desde que planejada desde a Fase 1.

## ADR-002 - Runtime .NET

### Decisao

Usar .NET 10 LTS como target inicial.

### Justificativa

Em 2026-08-12, .NET 10 esta em suporte LTS ativo ate 2028-11-14. .NET 8 LTS esta em manutencao e termina em 2026-11-10. Para produto comercial iniciado agora, .NET 10 reduz risco de migracao precoce.

### Pendencia

Validar em maquina Windows 10 22H2 x64 real/VM se todos os workloads WinUI 3, Windows App SDK e bibliotecas escolhidas funcionam sem dependencia operacional indesejada.

## ADR-003 - Modelo de elevacao

### Decisao

Projetar UI e agente separados. `BorealBoost.Agent` e requisito arquitetural da V1 e deve ser elevado quando a operacao exigir privilegio.

### Justificativa

O BorealBoost precisa executar operacoes administrativas, mas nao deve pedir UAC a cada comando nem elevar a UI inteira. Um agente dedicado por sessao permite:

- concentrar operacoes de risco;
- auditar comandos aceitos;
- reduzir acoplamento da UI com Windows internals;
- testar operacoes por contratos;
- bloquear execucao fora do ExecutionPlan;
- validar identidade do cliente, protocolo, catalogo confiavel e plano antes do apply.

### Alternativa rejeitada

App inteiro sempre elevado. Simples, mas aumenta risco da superficie UI e torna mais facil misturar ViewModel com operacao destrutiva. Tambem foi rejeitado como fallback temporario: se o Agent nao estiver pronto, a Fase 1 nao deve avancar para apply privilegiado.

### Implicacoes

- UI nao executa comandos privilegiados.
- Agent nao aceita command line, PowerShell, script ou executavel arbitrario vindo da UI.
- Comunicacao App-Agent usa named pipe local com ACL restrita, handshake, protocolo versionado, replay protection, timeout e limites de payload.
- Agent revalida ExecutionPlan, catalogo e allowlist antes de executar.
- Operacoes nao administrativas tambem passam pelo Agent, mas podem usar token nao elevado para preservar HKCU do usuario correto.

## ADR-004 - Empacotamento

### Decisao

Planejar V1 com MSI via WiX Toolset e app Windows App SDK em modo unpackaged ou packaged-with-external-location.

### Justificativa

MSI e adequado para app tecnico instalado em PCs de clientes, com Start Menu, uninstall, Program Files e ProgramData. MSIX sera reavaliado depois, pois o produto exige elevacao, acesso amplo ao sistema e fluxos presenciais que podem ser mais simples em MSI.

### Pendencia

Validar assinatura de codigo, publisher, UAC manifest, Windows App SDK runtime e estrategia self-contained/framework-dependent.

## ADR-005 - Catalogo declarativo proprio

### Decisao

Criar catalogo BorealBoost proprio em formato estruturado validado por schema, em vez de copiar `tweaks.json` do WinUtil.

### Justificativa

WinUtil confirma valor de uma configuracao declarativa, mas seu schema nao possui todos os campos exigidos pelo Master Spec: supported_os, build ranges, hardware rules, evidence level, detection independente, verify, undo por snapshot, dependencies, conflicts e laptop policy.

## ADR-006 - PowerShell como adapter, nao arquitetura

### Decisao

PowerShell pode ser usado por adapters especificos, mas toda execucao deve ser modelada como operacao estruturada.

### Justificativa

O Master Spec proibe transformar BorealBoost em wrapper de PowerShell. APIs .NET/Win32/CIM devem ser preferidas quando razoaveis. Quando PowerShell/DISM/powercfg/PnPUtil forem a melhor ferramenta oficial, o retorno deve virar `OperationResult`, com stdout/stderr, exit code, duration e requiresRestart.

## ADR-007 - Boreal Score

### Decisao

Boreal Score sera calculado por subscores mensuraveis e penalidades documentadas, sem inferir FPS. O algoritmo inicial fica formalmente experimental/beta ate calibracao.

### Justificativa

Pontuacao precisa explicar saude e oportunidades de configuracao, nao prometer ganho de jogos sem benchmark real. O algoritmo inicial deve ser versionado, calibrado em VMs/hardware real e documentado no relatorio.

### Implicacoes

- Versao inicial: `BBScore-v0-beta`.
- Aumento do Boreal Score nao equivale automaticamente a aumento de FPS.
- Comparacoes devem registrar a versao do algoritmo.

## ADR-008 - Catalogo confiavel de otimizacoes

### Decisao

Tratar o Optimization Catalog como artefato confiavel e assinado, com separacao entre catalogo built-in e catalogo atualizado.

### Justificativa

O catalogo define operacoes que podem acionar handlers privilegiados no Agent. Um JSON em ProgramData pode ser alterado por software local ou usuario com permissao suficiente; portanto localizacao nao e prova de confianca.

### Implicacoes

- Todo catalogo deve declarar `schemaVersion`, `catalogVersion`, hash, assinatura e publisher.
- Catalogo atualizado invalido e ignorado; built-in invalido bloqueia apply.
- Downgrade e bloqueado salvo manifest assinado de rollback.
- Catalogo atualizado nao pode criar novos tipos de operacao privilegiada fora da allowlist do Agent.

## ADR-009 - Modelo transacional de otimizacoes

### Decisao

Cada `OperationSpec` deve declarar idempotencia, reversibilidade, reboot boundary, retry, timeout, failure policy, verification, rollback e snapshot requirements.

### Justificativa

O Windows nao oferece transacao atomica global para registry, servicos, drivers, features, DNS e power plans. O BorealBoost precisa de journal duravel por operacao para detectar crash, reboot e falhas parciais.

### Implicacoes

- Sessao incompleta nunca aparece como concluida.
- `Completed` so ocorre apos verify e commit duravel.
- Recovery roda na proxima inicializacao antes de nova otimizacao.
- Crash durante apply/rollback produz estados explicitos como `UnknownAfterCrash`, `VerificationPending`, `RecoveryPending` ou `ManualActionRequired`.

## ADR-010 - Driver Engine V1 assistido e oficial

### Decisao

Driver Engine V1 sera diagnostico e assistido, usando Windows Update, SetupAPI/CfgMgr32, PnPUtil e fontes oficiais verificaveis. Nao havera scraping generico para drivers.

### Justificativa

Drivers sao area critica de estabilidade. Versao maior ou link encontrado na web nao basta para instalar. Pacotes precisam de match de hardware, assinatura, publisher e politica OEM/vendor.

### Implicacoes

- Notebook prioriza OEM para drivers de plataforma e componentes customizados.
- Hardware ID match exato vence compatible ID.
- INF/CAT, Authenticode, publisher e hash sao validados quando aplicaveis.
- Quando nao houver fonte oficial automatizavel, o fluxo vira orientacao manual.

## ADR-011 - Windows 10 como target legado funcional

### Decisao

Manter Windows 10 22H2 x64/build 19045 como target legado funcional do BorealBoost.

### Justificativa

O publico do produto pode incluir PCs de clientes ainda em Windows 10. Remover suporte reduziria a utilidade tecnica, mas o produto deve distinguir compatibilidade funcional do estado de suporte Microsoft.

### Implicacoes

- Toda otimizacao declara suporte Windows 10 explicitamente.
- Recursos Windows 11-only sao bloqueados no Windows 10.
- Relatorios devem diferenciar "suportado funcionalmente pelo BorealBoost" de "estado de suporte Microsoft".

## ADR-012 - Logging Foundation separado por processo

### Decisao

Na Foundation, App e Agent gravam JSONL em arquivos separados por papel e processo: `app-YYYYMMDD-PID.jsonl` e `agent-YYYYMMDD-PID.jsonl`.

### Justificativa

App e Agent sao processos distintos e podem emitir logs simultaneamente durante bootstrap/handshake. Um unico arquivo compartilhado exigiria sink multi-processo mais complexo e criou risco real de lock. A separacao por processo e simples, previsivel e suficiente para a Foundation.

### Implicacoes

- Logging nao deve derrubar processo critico por falha de IO.
- Falhas de logging usam fallback controlado.
- Correlacao entre arquivos deve usar `sessionId`, `correlationId` e timestamp quando o fluxo envolver IPC.
- Um sink centralizado pode ser reavaliado em fase futura sem alterar o contrato de seguranca do Agent.

## ADR-013 - Scanner read-only modular

### Decisao

Implementar a Fase 2 como `SystemScanner` modular, somente leitura, com modelo `SystemSnapshot` em `Core`, providers Windows em `System`, orquestracao em `Analysis` e apresentacao na pagina `Scanner` do App.

### Justificativa

O Scanner e insumo para Analysis, Recommendations, Drivers, Benchmark, Rollback e Reporting. Ele deve coletar fatos reais sem misturar interpretacao ou apply. WMI/CIM e Registry read-only sao permitidos pela arquitetura quando encapsulados e justificados; PowerShell/cmd e execucao externa nao sao usados.

### Implicacoes

- Provider falho gera `PartialScan`, nao crash do scan completo.
- Progresso da UI e ponderado por providers reais.
- Timeout e cancelamento sao parte do contrato.
- `Unknown`/`null` e preferivel a informacao inventada.
- O Agent nao e usado para coleta comum da Fase 2.
- Boreal Score, Recommendation Engine, Optimization Engine operacional e Driver Engine operacional permanecem fora da Fase 2.

## ADR-014 - Revalidacao do Scanner apos auditoria

### Decisao

Corrigir a Fase 2 mantendo o scanner somente leitura, com quatro contratos adicionais:

- WMI/CIM nao pode usar `Task.Run`/`WaitAsync` de forma que uma chamada nativa continue abandonada apos timeout/cancelamento;
- a UI deve iniciar scans por `SystemScanSessionService` singleton, com single-flight global e estados `Idle`, `Running`, `Cancelling`, `Completed`, `Failed` e `Cancelled`;
- VRAM reportada por `Win32_VideoController.AdapterRAM` nao e tratada como valor conhecido; quando nao houver fonte confiavel, `AdapterRamBytes=null` e `AdapterRamStatus=Unknown`;
- snapshots e relatorios futuros devem respeitar `SystemSnapshotPrivacyPolicy`.

### Justificativa

A auditoria da Fase 2 aprovou o Scanner com correcoes. Os achados de maior risco estavam ligados a confiabilidade operacional do scanner e qualidade dos fatos coletados. Fases posteriores nao podem consumir dados potencialmente falsos como base de recommendation, driver planning ou optimization planning.

### Implicacoes

- Timeout de provider representa estado real: o scanner so avanca depois que o provider retorna, observa cancellation ou falha.
- Cancelamento de WMI pode aguardar o timeout nativo da chamada atual, mas nao deixa tarefas orfas nem excecoes nao observadas.
- Dois scans simultaneos sao rejeitados, inclusive apos navegacao/recriacao da pagina.
- Memoria instalada e memoria visivel pelo Windows sao fatos diferentes no dominio.
- Capabilities de seguranca read-only podem ser `Known`, `Unknown`, `NotSupported` ou `Deferred`; `Deferred` nao e recomendacao.
- Defender, Firewall e BitLocker permanecem diferidos na Fase 2.

## ADR-015 - Analysis + Recommendation Engine read-only

### Decisao

Implementar a Fase 3 como engine de analise puro, baseado no `SystemSnapshot`, com regras modulares code-first e recomendacoes estruturadas sem qualquer capacidade de apply.

### Justificativa

Fases futuras precisam de recomendacoes tecnicamente defensaveis antes de existir Optimization Engine operacional. Misturar analise com novos scanners escondidos, comandos ou tweaks anteciparia riscos e quebraria a separacao aprovada:

- Scanner = fatos;
- Analysis = interpretacao;
- Recommendation = sugestao;
- Optimization = execucao futura;
- Rollback = recuperacao futura.

### Implicacoes

- `BorealBoost.Core` contem contratos de analysis e recommendation sem dependencias Windows/UI.
- `BorealBoost.Analysis` executa regras ordenadas por `RuleId` e nao referencia `BorealBoost.System`.
- `AnalysisResult` registra `EngineVersion` e `RuleCatalogVersion`.
- `Unknown` nao gera oportunidade automatica.
- Recomendacoes Advanced/Aggressive exigem risco, justificativa e confirmacao futura.
- Presets Basico, Medio, Avancado e Custom sao apenas preview.
- Nenhuma recomendacao executa Registry, Services, Power, DNS, Drivers, Windows Update, Benchmark, Boreal Score operacional, Optimization ou Rollback.
- Falha isolada de uma regra vira warning tecnico e nao recomendacao falsa.

## ADR-016 - Fase 4 como motor transacional antes do catalogo real

### Decisao

Implementar a Fase 4 como infraestrutura de `OptimizationDefinition`, `OperationSpec`, `ExecutionPlan`, Dry Run, Preflight, `OptimizationSession`, journal, snapshot, verification, rollback e recovery, com apenas uma operacao real controlada de integracao em HKCU proprio do BorealBoost.

### Justificativa

O produto precisa provar o pipeline de seguranca antes de qualquer tweak real. Uma colecao de otimizacoes sem transacao, snapshot e rollback aumentaria risco operacional. A chave `HKCU\Software\BorealBoost\IntegrationTest\Phase4ControlledValue` permite validar mutacao, verify e undo sem tocar configuracoes reais de performance, seguranca, drivers, rede ou servicos.

### Implicacoes

- Catalogo amplo de tweaks fica bloqueado ate a Fase 5.
- `BorealBoost.Agent` aceita somente mensagens IPC tipadas e OperationType allowlisted.
- O handler da Fase 4 revalida target, timeout, retry, snapshot e rollback mesmo apos validacao do plano.
- Persistencia de sessao usa envelope versionado e hash de integridade para detectar corrupcao acidental.
- Restore Point real permanece modelado como policy, mas nao e criado automaticamente na Fase 4.
- Uma sessao sem conclusao duravel entra em recovery e nunca aparece como `Completed`.

## ADR-017 - Revalidacao transacional da Fase 4

### Decisao

Endurecer a Fase 4 apos auditoria com quatro contratos adicionais:

- rollback Registry deve preservar exatamente existencia, tipo, valor bruto e `RegistryView` dos tipos suportados;
- `ExecutionPlan` aprovado deve ser protegido por hash canonico e OperationSpec canonica do catalogo built-in confiavel;
- `OperationSnapshotItem` deve possuir hash de integridade local e binding a sessao/plano/operacao antes de rollback;
- sessoes mutaveis usam lock cross-process por usuario e recovery expoe artefatos invalidos como acao manual.

### Justificativa

A infraestrutura da Fase 4 sera a base para catalogo real na Fase 5. Antes de ampliar qualquer tweak, o produto precisa provar que rollback exato, deteccao de adulteracao, recovery conservador e exclusao concorrente funcionam em um alvo controlado.

### Implicacoes

- `REG_EXPAND_SZ` e capturado com `DoNotExpandEnvironmentNames` e restaurado como `REG_EXPAND_SZ`.
- `String`, `ExpandString`, `DWord`, `QWord`, `MultiString` e `Binary` sao preservados no alvo HKCU controlado.
- Tipo unsupported e snapshot adulterado bloqueiam apply/rollback.
- Agent rejeita payload com mesmo `OperationId` e target/desired state adulterado.
- Plano alterado apos aprovacao e rejeitado por hash.
- JSON truncado, schema incompativel, hash divergente e `.tmp` residual aparecem como `ManualRecovery`, sem rollback automatico.
- Duas instancias do BorealBoost nao podem executar sessoes mutaveis simultaneas.

## ADR-018 - Catalog V1 conservador e allowlisted

### Decisao

Implementar a Fase 5 com um catalogo built-in V1 pequeno, versionado, conservador e explicitamente classificado, contendo apenas operacoes RegistryValue canonicas e reversiveis. O catalogo real possui 12 OptimizationDefinitions: 6 Safe, 5 Medium, 1 Advanced e 0 Aggressive/Experimental.

### Justificativa

A Fase 5 e a primeira fase com otimizacoes reais. A decisao privilegia tecnicas documentadas pela Microsoft ou verificaveis por estado local, evita listas grandes de tweaks populares e preserva o pipeline aprovado na Fase 4: Dry Run, Preflight, snapshot antes de write, Agent, apply, verify, journal e rollback.

### Implicacoes

- `BuiltInOptimizationCatalog` declara `schemaVersion = 5.1.0` e `catalogVersion = 5.1.0-built-in-v1`.
- `TrustedRegistryOperationTargets.CatalogV1` fixa exatamente hive, key, value, view e desired state.
- `OperationType.RegistryValue` e aceito apenas quando a OperationSpec coincide com a definicao canonica confiavel.
- Cada definicao declara `TechnicalCategory`, `PerformanceRelevance`, `AutomaticPresetSuitability`, `ConfigurationMechanism`, `ActivationBoundary`, `VerificationLevel` e `RollbackValidationLevel`.
- Basic nao seleciona preferencias pessoais por padrao; seleciona somente itens `Automatic` Safe compativeis.
- Medium seleciona itens `Automatic` Safe/Medium e mostra preferencias `OptIn` como `RequiresConfirmation`.
- Advanced pode expor itens de maior risco ou `AdvancedOnly` como `RequiresConfirmation`, sem selecao silenciosa.
- Custom pode expor preferencias compativeis, mas nao permite executar `Blocked`.
- Nenhum item desabilita Defender, Firewall, Windows Update permanentemente, pagefile, BCD ou timer behavior.
- Nenhum target vem livremente da UI e nenhuma execucao arbitraria foi introduzida.
- `OPTIMIZATION_CATALOG.md` passa a ser o documento de referencia do catalogo V1 e dos tweaks rejeitados/deferidos.
