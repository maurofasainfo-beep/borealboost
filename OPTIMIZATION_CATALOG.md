# BorealBoost - Optimization Catalog

Data: 2026-08-14
Status: Catalog V1 reclassificado apos auditoria da Fase 5.

## Escopo

O Catalog V1 contem 12 `OptimizationDefinition` comerciais pequenas, reversiveis e verificaveis por estado. A revalidacao da Fase 5 separa otimizacao tecnica de preferencia pessoal: itens de UX, privacidade e atalhos de Game Bar continuam disponiveis, mas nao sao apresentados como ganho de FPS ou performance quando a evidencia nao sustenta isso.

Fora do escopo do Catalog V1:

- Service, Power, DNS, AppX, drivers, Windows Update, Defender, Firewall, BCD e timer tweaks;
- download de catalogo remoto;
- assinatura runtime de catalogo updated;
- Boreal Score operacional;
- claims numericos de FPS, latencia ou porcentagem de melhoria.

## Manifest

- `schemaVersion`: `5.1.0`
- `catalogVersion`: `5.1.0-built-in-v1`
- `publisher`: `BorealBoost BuiltIn`
- `source`: `BorealBoost.Optimization.Catalog.BuiltInOptimizationCatalog`
- `contentHash`: SHA-256 canonico calculado sobre campos semanticos e de seguranca, incluindo IDs, descricoes, risco, evidencia, classificacao tecnica, mecanismo de configuracao, build constraints, fronteira de ativacao, verificacao, rollback, elevacao e `OperationSpec`.
- `builtAtUtc`: `2026-08-13T00:00:00Z`

O catalogo built-in e confiavel por estar empacotado com o binario/release assinado do produto. Um catalogo updated futuro so podera ser usado com schema, hash, assinatura digital, publisher confiavel e anti-downgrade.

## Classification Model

- `TechnicalCategory`: `Performance`, `Responsiveness`, `GamingPerformance`, `GamingFeaturePreference`, `Privacy`, `UXPreference`, `Security`, `Maintenance`, `SystemBehavior`.
- `PerformanceRelevance`: `None`, `Low`, `Moderate`, `WorkloadDependent`, `Unknown`.
- `AutomaticPresetSuitability`: `Automatic`, `OptIn`, `CustomOnly`, `AdvancedOnly`.
- `UserPreferenceImpact`: `None`, `Low`, `Medium`, `High`.
- `ConfigurationMechanism`: `Policy`, `Preference`, `ImplementationDetail`.
- `ConfigurationEvidence`: `DocumentedSupportedMechanism`, `DocumentedPolicy`, `ObservedRegistryBehavior`, `CommunityKnown`, `Experimental`.
- `ActivationBoundary`: `Immediate`, `ExplorerRestart`, `ApplicationRestart`, `SignOut`, `PolicyRefresh`, `Reboot`, `Unknown`.
- `VerificationLevel`: `StateVerified`, `BehaviorVerified`, `RequiresActivationBoundary`, `NotFullyBehaviorVerified`.
- `RollbackValidationLevel`: `HandlerValidated`, `OptimizationUnitValidated`, `OptimizationIntegrationValidated`, `OptimizationVMValidated`, `OptimizationHardwareValidated`.

`RequiresReboot=false` nao significa efeito imediato. `StateVerified` significa que o estado persistido foi verificado; nao prova comportamento final quando a propria definicao declara uma fronteira de ativacao.

## Catalog V1 Table

| OptimizationId | Title | TechnicalCategory | Risk | Evidence | ConfigurationMechanism | PerformanceRelevance | AutomaticPresetSuitability | Windows | BuildRange | ActivationBoundary | VerificationLevel | RollbackValidationLevel | SideEffects | References |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `BB.OPT.VISUAL.TRANSPARENCY.DISABLE` | Disable transparency effects | Responsiveness | Safe | Moderate / DocumentedSupportedMechanism | Preference | Low | Automatic | Windows 10/11 x64 | Min 19045 | ExplorerRestart | RequiresActivationBoundary | OptimizationIntegrationValidated | Windows UI loses translucent surfaces. | Microsoft Windows personalization docs |
| `BB.OPT.WINDOWS.EXPLORER.SHOW_EXTENSIONS` | Show known file extensions | UXPreference | Safe | Moderate / ObservedRegistryBehavior | ImplementationDetail | None | CustomOnly | Windows 10/11 x64 | Min 19045 | ExplorerRestart | RequiresActivationBoundary | HandlerValidated | Known extensions become visible and names may look longer. | Microsoft File Explorer settings docs |
| `BB.OPT.WINDOWS.AUTOPLAY.DISABLE` | Disable removable media AutoPlay | SystemBehavior | Safe | Moderate / DocumentedSupportedMechanism | Preference | Low | Automatic | Windows 10/11 x64 | Min 19045 | Immediate | StateVerified | HandlerValidated | AutoPlay prompts for removable media/devices no longer open automatically. | Microsoft AutoPlay settings docs |
| `BB.OPT.PRIVACY.START.RECOMMENDATIONS.DISABLE` | Disable Start recommendations | Privacy | Safe | Moderate / DocumentedSupportedMechanism | Preference | None | OptIn | Windows 11 x64 | Min 22000 | ExplorerRestart | RequiresActivationBoundary | HandlerValidated | Start may show fewer suggested items. | Microsoft Windows 11 Start recommendations docs |
| `BB.OPT.WINDOWS.START.MORE_PINS` | Prefer more Start pins | UXPreference | Safe | Moderate / DocumentedSupportedMechanism | Preference | None | CustomOnly | Windows 11 x64 | Min 22000 | ExplorerRestart | RequiresActivationBoundary | HandlerValidated | Start layout changes to emphasize pinned apps. | Microsoft Windows 11 Start layout docs |
| `BB.OPT.GAMING.GAMEBAR.CONTROLLER_SHORTCUT.DISABLE` | Disable controller Game Bar shortcut | GamingFeaturePreference | Safe | Moderate / DocumentedSupportedMechanism | Preference | None | OptIn | Windows 11 x64 | Min 22000 | ApplicationRestart | RequiresActivationBoundary | HandlerValidated | Controller shortcut no longer opens Game Bar. | Microsoft Windows 11 Game Bar settings docs |
| `BB.OPT.GAMING.GAMEBAR.RECORDING_SHORTCUT.DISABLE` | Disable Game Bar recording shortcut | GamingFeaturePreference | Medium | Moderate / DocumentedSupportedMechanism | Preference | None | OptIn | Windows 11 x64 | Min 22000 | ApplicationRestart | RequiresActivationBoundary | HandlerValidated | Game Bar recording shortcut is disabled; this does not prove capture services are disabled. | Microsoft Windows 11 Game Bar settings docs |
| `BB.OPT.GAMING.GAMEBAR.BROADCAST_SHORTCUT.DISABLE` | Disable Game Bar broadcast shortcut | GamingFeaturePreference | Medium | Moderate / DocumentedSupportedMechanism | Preference | None | OptIn | Windows 11 x64 | Min 22000 | ApplicationRestart | RequiresActivationBoundary | HandlerValidated | Game Bar broadcast shortcut is disabled. | Microsoft Windows 11 Game Bar settings docs |
| `BB.OPT.PRIVACY.GAMEBAR.CAMERA_CAPTURE_SHORTCUT.DISABLE` | Disable Game Bar camera capture shortcut | Privacy | Medium | Moderate / DocumentedSupportedMechanism | Preference | None | OptIn | Windows 11 x64 | Min 22000 | ApplicationRestart | RequiresActivationBoundary | HandlerValidated | Camera capture shortcut is disabled. | Microsoft Windows 11 Game Bar settings docs |
| `BB.OPT.PRIVACY.GAMEBAR.MIC_CAPTURE_SHORTCUT.DISABLE` | Disable Game Bar microphone capture shortcut | Privacy | Medium | Moderate / DocumentedSupportedMechanism | Preference | None | OptIn | Windows 11 x64 | Min 22000 | ApplicationRestart | RequiresActivationBoundary | HandlerValidated | Microphone capture shortcut is disabled. | Microsoft Windows 11 Game Bar settings docs |
| `BB.OPT.PRIVACY.ADVERTISING_ID.DISABLE` | Disable Windows advertising ID policy | Privacy | Medium | Moderate / DocumentedPolicy | Policy | None | OptIn | Windows 10/11 x64 | Min 19045 | PolicyRefresh | RequiresActivationBoundary | HandlerValidated | Personalized in-app advertising may become less relevant; enterprise policy can override. | Microsoft Privacy Policy CSP docs |
| `BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE` | Disable Windows Game Recording policy | GamingPerformance | Advanced | Strong / DocumentedPolicy | Policy | WorkloadDependent | AdvancedOnly | Windows 10 x64 desktop | 19045 only | PolicyRefresh | RequiresActivationBoundary | HandlerValidated | Windows Game Recording/Broadcasting policy is disabled until rollback; enterprise policy can override. | Microsoft ApplicationManagement Policy CSP docs |

## Wave A - Safe

Implemented definitions:

- transparency effects disable;
- known file extensions visible;
- removable media AutoPlay disabled;
- Windows 11 Start recommendations disabled;
- Windows 11 Start more pins;
- Windows 11 controller Game Bar shortcut disabled.

Only `Automatic` items are candidates for Basic/Medium automatic selection. Safe preferences remain available through Custom or explicit confirmation.

## Wave B - Medium

Implemented definitions:

- Windows 11 Game Bar recording shortcut disabled;
- Windows 11 Game Bar broadcast shortcut disabled;
- Windows 11 Game Bar camera capture shortcut disabled;
- Windows 11 Game Bar microphone capture shortcut disabled;
- Windows advertising ID policy.

Medium can surface `OptIn` items as `RequiresConfirmation`; it does not silently apply privacy, UX or shortcut preferences.

## Wave C - Advanced

Implemented definition:

- Windows 10 desktop-only Game Recording policy disable.

The Advanced preset can expose the item only as confirmation-required when compatible. It remains blocked/not applicable outside Windows 10 build 19045 desktop.

## Wave D - Aggressive / Experimental

No Aggressive or Experimental optimization was accepted in Catalog V1.

## Preset Policy

`OptimizationPresetEngine` is deterministic and evaluates:

- `SystemSnapshot`;
- matching `AnalysisResult.ScanId`;
- `catalogVersion`;
- Windows compatibility/build/architecture;
- compatibility requirements;
- risk;
- evidence;
- `AutomaticPresetSuitability`;
- `TechnicalCategory` and user preference impact;
- SecurityTradeoff;
- dependencies and conflicts;
- user confirmation requirement.

Statuses:

- `Selected`;
- `Excluded`;
- `Blocked`;
- `NotApplicable`;
- `RequiresConfirmation`.

Rules:

- Basic selects only compatible `Automatic` Safe items with undo, no SecurityTradeoff and no restart.
- Medium selects compatible `Automatic` Safe/Medium items and surfaces compatible `OptIn` Safe/Medium items as `RequiresConfirmation`.
- Advanced may surface compatible `AdvancedOnly` and riskier items as `RequiresConfirmation`; it does not select `CustomOnly` preferences.
- Custom exposes compatible `OptIn` and `CustomOnly` preferences but does not bypass `Blocked`.
- stale `AnalysisResult` blocks all items.
- Unknown Windows/build compatibility blocks automatic selection.

## Agent Allowlist

Real OperationType used by Catalog V1:

- `RegistryValue`

Handlers:

- `BorealIntegrationRegistryOperationHandler(OperationType.RegistryValue)` for trusted registry catalog operations;
- existing `BorealIntegrationRegistryOperationHandler(OperationType.BorealIntegrationRegistryValue)` remains only for integration proof.

The Agent revalidates:

- `CatalogVersion`;
- `OptimizationId`;
- `OperationId`;
- `OperationType`;
- exact registry hive/key/value/view;
- exact desired state;
- timeout/retry/idempotency/reversibility/reboot/failure policy;
- snapshot requirements;
- rollback strategy;
- handler availability;
- elevation when `RequiresElevation=true`.

No registry path, value name, desired value, command line, executable path, shell, script, PowerShell or process arguments are accepted from UI as free-form target.

## Rollback Coverage

Counts exclude `BB.OPT.INTEGRATION.REGISTRY_PROOF`.

- Total modifying optimizations: 12
- Reversible: 12
- Irreversible: 0
- Requires reboot: 0
- SecurityTradeoff: 0
- Restore point required: 0
- Operation type coverage: `RegistryValue`

Rollback coverage is not reported as 12/12 end-to-end proven. The shared handler has system coverage for exact registry rollback, and each optimization declares its real validation level:

- `OptimizationIntegrationValidated`: 1 (`BB.OPT.VISUAL.TRANSPARENCY.DISABLE`);
- `HandlerValidated`: 11;
- `OptimizationVMValidated`: 0;
- `OptimizationHardwareValidated`: 0.

Rollback uses `OperationSnapshotItem` and preserves original key existence, value existence, kind, value and `RegistryView`. If BorealBoost created a previously absent key and the key is still empty after deleting the value, rollback removes the created key. It does not remove a key that contains third-party data.

Supported registry kinds:

- `String`;
- `ExpandString`;
- `DWord`;
- `QWord`;
- `MultiString`;
- `Binary`.

## Rejected / Excluded Tweaks

| Technique | Status | Reason |
| --- | --- | --- |
| Defender disable/exclusions | Rejected | Security reduction; not acceptable for automatic performance presets. |
| Firewall disable | Rejected | Security reduction and compatibility risk. |
| Windows Update permanent disable | Rejected | Breaks servicing/security; Microsoft guidance warns against this. |
| Pagefile disable | Rejected | Can reduce commit capacity and crash dump behavior; workload dependent. |
| HPET/BCD/timer hacks | Rejected | High boot/system risk and insufficient official evidence for universal benefit. |
| Universal TCP/netsh tweaks | Rejected | Network behavior is context dependent; no generic gaming benefit accepted. |
| Arbitrary service disable lists | Rejected | Service dependencies and OEM/user workflows require per-service proof. |
| AppX/debloat removal | Deferred | Rollback may be partial and can affect user data/features; needs separate model. |
| OneDrive removal | Rejected for automatic presets | User data/sync risk. |
| Search/indexing disable | Deferred | Can harm common workflows; requires better context and confirmation. |
| Ultimate Performance plan | Deferred | Power/thermal behavior requires laptop/desktop/OEM validation. |
| HAGS toggle | Deferred | Hardware, driver and workload dependent; requires reboot and validation matrix. |
| Driver registry hacks | Rejected | Driver Engine belongs to a future driver phase and must use official sources/validation. |

## Evidence Reference URLs

- ApplicationManagement Policy CSP AllowGameDVR: `https://learn.microsoft.com/windows/client-management/mdm/policy-csp-applicationmanagement#allowgamedvr`
- Privacy Policy CSP DisableAdvertisingId: `https://learn.microsoft.com/windows/client-management/mdm/policy-csp-privacy#disableadvertisingid`
- Windows 11 settings reference: `https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11`
- Common Windows settings reference: `https://learn.microsoft.com/windows/apps/develop/settings/settings-common`
- Windows privacy and general settings: `https://support.microsoft.com/windows/privacy-and-general-settings-in-windows`
- Windows colors/personalization settings: `https://support.microsoft.com/windows/change-colors-in-windows`
- Microsoft restricted traffic guidance: `https://learn.microsoft.com/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services`

## Validation Status

- Windows 11: unit-tested and system-tested on the current machine for Scanner -> Analysis -> Preset Preview; one safe HKCU item was integration-tested end-to-end and rolled back.
- Windows 10 22H2 build 19045: unit-tested by fixture; real Windows 10 VM/hardware validation remains `UNVALIDATED_FOR_RELEASE`.
- HKCU `RegistryValue` handler: end-to-end system-tested through Agent IPC using `BB.OPT.VISUAL.TRANSPARENCY.DISABLE`, then rolled back to exact original state.
- HKLM `RegistryValue` items: dry-run/plan/canonical validation tested; real elevated apply remains `UNVALIDATED_FOR_RELEASE` until safe VM/client validation.
