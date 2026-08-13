# BorealBoost - Domain Model

Data: 2026-08-12
Status: modelo conceitual. Nenhum codigo foi implementado.

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

### SystemProfile

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

### Finding

Representa uma observacao tecnica detectada pelo scanner.

Campos:

- findingId;
- category;
- severity;
- title;
- evidence;
- affectedComponent;
- recommendationHint;
- confidence;
- source.

### Recommendation

Resultado do RecommendationEngine.

Estados:

- Recommended;
- Optional;
- NotRecommended;
- Incompatible.

Campos:

- optimizationId;
- reason;
- confidence;
- expectedImpactArea;
- riskLevel;
- evidenceLevel;
- blockingRules;
- requiredConfirmations.

### OptimizationDefinition

Entrada declarativa do catalogo.

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
