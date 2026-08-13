# BorealBoost - Domain Model

Data: 2026-08-12
Status: modelo conceitual com contratos implementados para Foundation, System Scanner, Analysis/Recommendation Engine e Optimization/Rollback Foundation.

## Agregados principais

### TechnicianSession

Representa o atendimento presencial.

Campos conceituais:

- sessionId;
- technicianName;
- clientName;
- notes;
- usageProfile;
- startedAt;
- endedAt;
- status.

UsageProfile:

- Gaming;
- GamingAndGeneral;
- Work;
- General;
- LowEndPC.

### SystemProfile / SystemSnapshot

Snapshot de leitura da maquina em um momento.

Composto por:

- OperatingSystemInfo;
- DeviceInfo;
- CpuInfo;
- GpuInfo[];
- MemoryInfo;
- StorageInfo;
- DisplayInfo;
- NetworkInfo;
- PowerInfo;
- SecurityInfo;
- ServicesInventory;
- ProcessesInventory;
- StartupInventory;
- DriverInventory;
- PendingRebootInfo.

Na Fase 2, o modelo implementado recebe o nome `SystemSnapshot` e representa somente fatos detectados. `SystemSnapshot` nao contem recommendations, Boreal Score, benchmarks nem plano de otimizacao.

Campos implementados:

- `ScanMetadata`;
- `OperatingSystem`;
- `Hardware`;
- `Processors`;
- `Graphics`;
- `Memory`;
- `Storage`;
- `Motherboard`;
- `Firmware`;
- `Devices`;
- `Drivers`;
- `Network`;
- `Displays`;
- `Power`;
- `Services`;
- `Processes`;
- `StartupItems`;
- `Capabilities`.

`ScanMetadata` contem `ScanId`, inicio/fim UTC, duracao, versao do app, schema, arquitetura da maquina, resultados por provider, `PartialScan`, warnings e errors.

Campos de memoria da Fase 2:

- `InstalledPhysicalBytes`: soma dos modulos fisicos quando todos reportam capacidade;
- `VisiblePhysicalBytes`: memoria fisica visivel/utilizavel pelo Windows.

Campos de GPU da Fase 2:

- `AdapterRamBytes`;
- `AdapterRamStatus`: `Known`, `Estimated` ou `Unknown`.

Regra: `Win32_VideoController.AdapterRAM` sozinho nao torna VRAM conhecida.

Capabilities possuem `DetectionStatus`: `Known`, `Unknown`, `Unavailable`, `NotSupported` ou `Deferred`.

`SystemScanSessionService` controla uma unica sessao ativa por vez com estados `Idle`, `Running`, `Cancelling`, `Completed`, `Failed` e `Cancelled`.

Regras da Fase 2:

- `Unknown`/`null` e estado valido quando a fonte nao e confiavel ou nao esta disponivel.
- Provider falho nao invalida necessariamente o snapshot completo.
- Provider nao suportado ou timed out torna o snapshot parcial.
- Snapshot cancelado pelo usuario nao deve ser apresentado como concluido.
- Device/driver facts podem conter Device Instance ID, Hardware IDs e Compatible IDs porque sustentam o Driver Engine futuro; serial numbers, MAC, SSID, product key e machine GUID ficam fora do modelo.
- `SystemSnapshotPrivacyPolicy` define quais campos sao publicos, internos, sensiveis, nao persistiveis ou nao reportaveis. Relatorios futuros devem usar snapshot sanitizado.

### AnalysisResult / Finding / Recommendation

Na Fase 3, `AnalysisResult` representa a interpretacao read-only de um `SystemSnapshot`.

Campos implementados em `AnalysisResult`:

- `AnalysisId`;
- `ScanId`;
- `StartedAtUtc`;
- `CompletedAtUtc`;
- `Duration`;
- `EngineVersion`;
- `RuleCatalogVersion`;
- `RuleResults`;
- `Findings`;
- `Recommendations`;
- `RecommendationPlan`;
- `Summary`;
- `Warnings`.

`AnalysisSessionService` controla a execucao read-only da analise sobre o snapshot corrente.

Estados:

- Idle;
- Running;
- Cancelling;
- Completed;
- Failed;
- Cancelled.

Regras:

- uma unica analise ativa por vez;
- starts concorrentes retornam `analysis.already_running`;
- cancelamento nao publica resultado parcial como concluido;
- se o `SystemSnapshot` corrente mudar durante a analise, o resultado e descartado com `analysis.snapshot_changed`;
- o ViewModel pode ser recriado por navegacao sem criar ownership paralelo da sessao.

`AnalysisFinding` representa uma observacao tecnica derivada de regra.

Campos:

- `FindingId`;
- `RuleId`;
- `Category`;
- `Status`: NotApplicable, Healthy, Opportunity, Warning, Blocked ou Unknown;
- `Title`;
- `Summary`;
- `TechnicalDetails`;
- `EvidenceLevel`;
- `Evidence`;
- `RelatedRecommendationIds`.

`Recommendation` representa sugestao estruturada de acao futura. Ela nao e uma otimizacao executavel.

Campos:

- `RecommendationId`;
- `RuleId`;
- `Title`;
- `ShortDescription`;
- `TechnicalReason`;
- `Category`;
- `RiskLevel`: Safe, Medium, Advanced ou Aggressive;
- `EvidenceLevel`: Strong, Moderate, Experimental ou Unknown;
- `Compatibility`;
- `DetectedState`;
- `DesiredState`;
- `ExpectedImpact`: Low, Moderate, PotentiallyHigh, WorkloadDependent ou Unknown;
- `ImpactAreas`;
- `SideEffects`;
- `RebootRequired`;
- `Reversible`;
- `PresetEligibility`;
- `UserConfirmationRequired`;
- `FutureOptimizationId`;
- `Evidence`;
- `ConflictsWith`;
- `Requires`.

Invariantes da Fase 3:

- `Unknown` nao vira `Opportunity`;
- recommendation nao contem command line, script, PowerShell ou path executavel;
- Advanced/Aggressive exigem justificativa e confirmacao futura;
- expected impact e qualitativo, sem prometer FPS;
- `FutureOptimizationId` pode existir futuramente, mas nao cria operacao.
- `RecommendationId` duplicado e falha de validacao, nao deduplicacao silenciosa;
- evidencia Experimental nao entra automaticamente em Basico/Medio;
- referencias `ConflictsWith`/`Requires` nao podem apontar para si mesmas ou IDs inexistentes;
- recomendacao invalida bloqueia publicacao do plano para evitar ambiguidade futura.

### OptimizationDefinition

Entrada declarativa do catalogo.

Na Fase 4, o modelo implementado inclui:

- `OptimizationId`;
- `Version`;
- `Title`;
- `Description`;
- `Category`;
- `RiskLevel`;
- `EvidenceLevel`;
- `SupportedWindows`;
- `CompatibilityRequirements`;
- `RequiredCapabilities`;
- `Conflicts`;
- `Dependencies`;
- `RequiresElevation`;
- `RequiresRestart`;
- `SupportsUndo`;
- `RestorePointRequirement`;
- `SnapshotRequirements`;
- `OperationSpecs`;
- `VerificationSpecs`;
- `RollbackSpecs`;
- `FailurePolicy`;
- `TimeoutPolicy`;
- `SourceMetadata`.

O catalogo built-in da Fase 4 possui apenas `BB.OPT.INTEGRATION.REGISTRY_PROOF`. Ele nao representa tweak de performance.

Campos obrigatorios:

- id;
- name;
- description;
- category;
- riskLevel;
- evidenceLevel;
- impactAreas;
- supportedOS;
- supportedBuildRange;
- supportedEditions;
- architecture;
- supportedHardware;
- laptopPolicy;
- desktopPolicy;
- requiresAdmin;
- requiresInternet;
- requiresRestart;
- requiresLogout;
- dependencies;
- conflicts;
- detection;
- compatibilityRules;
- operations;
- verification;
- undoStrategy;
- documentationLinks;
- testPlan;
- changelog;

### TrustedOptimizationCatalog

Conjunto validado de definicoes de otimizacao.

Campos:

- schemaVersion;
- catalogVersion;
- catalogId;
- channel;
- publisher;
- createdAt;
- expiresAt quando aplicavel;
- minimumAppVersion;
- minimumAgentVersion;
- sourceType: BuiltIn ou Updated;
- contentHash;
- signature;
- signingCertificateThumbprint;
- previousCatalogVersion;
- definitions;
- presets;
- revokedDefinitionIds;

Regras:

- catalogo built-in vem junto dos binarios assinados;
- catalogo atualizado fica separado em ProgramData;
- ProgramData nunca e confiavel por localizacao;
- Agent valida schema, hash, assinatura, publisher e versao antes de usar;
- downgrade e bloqueado salvo politica de recovery assinada.

### OperationSpec

Define uma operacao atomica modelada.

Na Fase 4, `OperationSpec` implementado declara:

- `OperationId`;
- `OperationType`;
- payload tipado de Registry quando aplicavel;
- `timeoutPolicy`;
- `retryPolicy`;
- `idempotency`;
- `reversibility`;
- `rebootBoundary`;
- `failurePolicy`;
- `verificationStrategy`;
- `rollbackStrategy`;
- `snapshotRequirements`.

A unica operacao real allowlisted e `BorealIntegrationRegistryValue`, restrita a `HKCU\Software\BorealBoost\IntegrationTest\Phase4ControlledValue`.

Tipos:

- RegistrySetValue;
- RegistryDeleteValue;
- ServiceSetStartupType;
- ServiceStartStop;
- PowerPlanCreate;
- PowerPlanActivate;
- DnsSetServers;
- OptionalFeatureEnableDisable;
- AppxRemove;
- ProcessAction;
- TrustedCommandAdapter;
- TrustedPowerShellAdapter;
- ManualInstruction.

Operacoes devem ser declarativas. UI nao deve carregar comandos soltos, scripts PowerShell, command line ou paths executaveis arbitrarios. `TrustedCommandAdapter` e `TrustedPowerShellAdapter` representam handlers internos allowlisted, com templates controlados pelo produto/catalogo confiavel e parametros tipados.

Campos obrigatorios ou condicionais:

- operationId;
- operationType;
- target;
- desiredState;
- idempotency: Idempotent, ConditionallyIdempotent ou NonIdempotent;
- reversibility: Full, Partial ou None;
- rebootBoundary: None, AllowedAfterOperation, RequiredAfterOperation ou RequiredBeforeContinue;
- retryPolicy;
- timeout;
- failurePolicy;
- verificationStrategy;
- rollbackStrategy;
- snapshotRequirements;
- requiredPrivileges;
- allowedWindowsBuilds;
- maxPayloadSize quando a operacao aceitar payload estruturado;
- safeCancellationPoints;

`reversibility = None` exige justificativa, aviso no plano e confirmacao explicita. `reversibility = Partial` exige documentar o que pode e o que nao pode ser restaurado.

### CompatibilityRule

Regra pura que decide se uma otimizacao pode rodar.

Entradas:

- SystemProfile;
- TechnicianSession;
- OptimizationDefinition;
- current state;
- policy flags.

Saidas:

- Compatible;
- Incompatible;
- NeedsConfirmation;
- NotApplicable;
- Unknown.

### ExecutionPlan

Plano gerado antes de alterar o sistema.

Na Fase 4, `ExecutionPlan` implementado contem:

- `PlanId`;
- `SessionId`;
- `ScanId`;
- `AnalysisId`;
- `SchemaVersion`;
- `EngineVersion`;
- `CatalogVersion`;
- `CreatedAtUtc`;
- alvo OS/build/architecture;
- `SelectedOptimizationIds`;
- `OrderedOperations`;
- dependencies/conflicts;
- `RiskSummary`;
- elevation/restart/restore point policy;
- snapshot requirements;
- reboot boundaries;
- estimated step count;
- warnings/blockers;
- `PlanHash`;
- `IsApproved`.

Plano invalido, obsoleto ou sem handler allowlisted nao executa.

Campos:

- planId;
- sessionId;
- selectedPreset;
- selectedOptimizationIds;
- orderedOperations;
- safetySteps;
- expectedRestart;
- riskSummary;
- blockedItems;
- confirmationRequirements;
- catalogVersion;
- catalogHash;
- protocolVersion;
- planHash;
- createdFromSystemProfileId;
- operationTransactionContracts.

### OptimizationSession

Execucao de uma otimizacao.

Estados implementados na Fase 4:

- Created;
- Planned;
- PreflightPassed;
- Snapshotting;
- Ready;
- Executing;
- Verifying;
- Completed;
- CompletedWithWarnings;
- Failed;
- RollbackPending;
- RollingBack;
- RolledBack;
- RollbackFailed;
- Cancelled;
- Interrupted;
- RecoveryRequired;
- RebootPending;
- ManualActionRequired.

Transicoes invalidas sao rejeitadas por state machine central. `Completed` so e usado apos verify obrigatorio e persistencia duravel.

Estados:

- Draft;
- Planned;
- AwaitingConfirmation;
- SafetyPreparing;
- SafetyPrepared;
- Running;
- RebootPending;
- VerificationPending;
- RecoveryPending;
- Interrupted;
- PartiallyFailed;
- Failed;
- Completed;
- RollbackRunning;
- RollbackInterrupted;
- RolledBack;
- RollbackFailed;
- ManualActionRequired;

Campos:

- sessionId;
- executionPlan;
- restorePoint;
- snapshot;
- baseline;
- transactionJournal;
- operationResults;
- verificationResults;
- finalSystemProfile;
- reportReferences;
- recoveryStatus;
- lastDurableState;
- completedAt.

`Completed` so pode ser gravado apos verify obrigatorio, commit duravel e ausencia de operacoes pendentes. Sessao interrompida, pendente de reboot, pendente de verify ou pendente de rollback nunca aparece como concluida.

### OperationJournalEntry

Registro transacional duravel por operacao.

Campos:

- journalEntryId;
- sessionId;
- operationId;
- attempt;
- state: Planned, SnapshotCaptured, ApplyStarted, ApplyCompleted, VerificationPending, Verified, Failed, RollbackStarted, RollbackInterrupted, RollbackVerified, RollbackFailed, UnknownAfterCrash;
- startedAt;
- endedAt;
- beforeSnapshotRef;
- afterObservationRef;
- resultHash;
- error;
- requiresReboot;
- recoveryAction.

### AgentProtocolMessage

Envelope de comunicacao App-Agent.

Campos:

- protocolVersion;
- messageType;
- sessionId;
- correlationId;
- requestId;
- sequenceNumber;
- timestampUtc;
- nonce;
- payloadHash;
- payload.

Regras:

- `requestId` e nonce nao podem repetir dentro da sessao;
- payload deve respeitar schema e limites de tamanho;
- payload operacional nunca contem comando arbitrario ou executavel definido pela UI;
- toda resposta do Agent preserva `correlationId`.

### Snapshot

Captura valores anteriores de cada item alteravel.

Na Fase 4, `OperationSnapshot` e separado de `SystemSnapshot`. Ele guarda itens por operacao com `SnapshotItemId`, `OperationId`, tipo de recurso, identidade, existencia anterior, valor/tipo anterior quando permitido, metodo de captura, estrategia de restauracao, limitacoes e metadata de verificacao.

Valores de snapshot sao classificados por `OptimizationPrivacyPolicy`; valores string anteriores sao redigidos para usos que nao precisam do dado bruto.

Na revalidacao da Fase 4, `OperationSnapshotItem` tambem registra campos tipados para `QWord`, `MultiString` e `Binary`, alem de `SnapshotHash`. O hash cobre identidade do recurso, tipo, valor bruto, capture method, restoration strategy e metadata. Snapshot sem hash valido, pertencente a outra sessao/plano ou semanticamente incoerente nao pode ser usado para apply/rollback.

Para o alvo Registry controlado da Fase 4, os tipos preservados exatamente sao:

- `String`;
- `ExpandString`;
- `DWord`;
- `QWord`;
- `MultiString`;
- `Binary`.

SnapshotItem deve conter:

- itemId;
- optimizationId;
- operationId;
- targetType;
- targetPath;
- beforeValue;
- beforeExists;
- captureMethod;
- capturedAt;
- restoreMethod;
- limitations.

### RestorePointRecord

Representa ponto de restauracao solicitado/validado.

Campos:

- restorePointId;
- description;
- requestedAt;
- createdAt;
- status;
- validationMethod;
- error;
- warnings.

### DriverDevice

Representa dispositivo e estado de driver.

Campos:

- instanceId;
- deviceName;
- deviceClass;
- manufacturer;
- hardwareIds;
- compatibleIds;
- vendorId;
- deviceId;
- installedDriver;
- problemCode;
- status;
- source;
- signatureStatus.

### BorealScore

Pontuacao versionada derivada de subscores.

Campos:

- algorithmVersion;
- totalScore;
- subscores;
- penalties;
- inputs;
- limitations;
- calculatedAt.

## Value Objects

- WindowsVersion;
- BuildRange;
- Edition;
- Architecture;
- HardwareId;
- VendorId;
- DeviceId;
- RegistryPath;
- RegistryValueName;
- ServiceName;
- PowerSchemeId;
- DnsProfile;
- SemanticVersion;
- RiskLevel;
- EvidenceLevel;
- ImpactArea;
- RebootRequirement;
- DurationMeasurement;
- PercentageMetric;
- ByteSize;
- ConfidenceScore.

## Domain services

- SystemScanner;
- HealthAnalyzer;
- BottleneckAnalyzer;
- RecommendationEngine;
- PresetEngine;
- CompatibilityEngine;
- OptimizationCatalogValidator;
- CatalogIntegrityValidator;
- AgentProtocolValidator;
- ExecutionPlanner;
- OptimizationEngine;
- VerificationEngine;
- SnapshotService;
- RestorePointService;
- RollbackEngine;
- DriverScanner;
- DriverSourceResolver;
- DriverInstallPlanner;
- BaselineCollector;
- ComparisonEngine;
- ReportGenerator;

## Invariantes

- Nenhuma otimizacao executa sem CompatibilityResult conhecido.
- Nenhuma operacao destrutiva executa sem ExecutionPlan confirmado.
- Nenhuma operacao executa se o Agent nao revalidar o ExecutionPlan e o catalogo confiavel.
- Nenhum catalogo atualizado e aceito apenas por estar em ProgramData.
- Nenhuma mensagem App-Agent e aceita fora do protocolo versionado e autenticado.
- Nenhuma command line, PowerShell ou executavel arbitrario recebido da UI e executado.
- Nenhuma operacao marcada como sucesso sem Verify quando verification e definida.
- Nenhuma sessao incompleta aparece como Completed.
- Undo usa snapshot quando disponivel; default oficial so pode ser usado quando documentado.
- Experimental nunca entra automaticamente em Basico ou Medio.
- Alteracoes de seguranca critica exigem confirmacao individual.
- Laptop nao recebe automaticamente plano agressivo de energia.
- Metricas de FPS so aparecem se houver benchmark real.

## Pendencias

- Definir schema JSON final do catalogo.
- Definir formato exato de snapshot e assinatura local.
- Confirmar se historico de sessao sera JSONL/JSON ou SQLite.
