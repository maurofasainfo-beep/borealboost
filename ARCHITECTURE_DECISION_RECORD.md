# BorealBoost - Architecture Decision Record

Data: 2026-08-12
Status: aprovado e atualizado ate a Fase 2

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

Projetar UI e agente elevado separados. `BorealBoost.Agent` elevado por sessao e requisito arquitetural da V1.

### Justificativa

O BorealBoost precisa executar operacoes administrativas, mas nao deve pedir UAC a cada comando. Um agente elevado por sessao permite:

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
