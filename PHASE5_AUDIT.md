# Executive Summary

Phase 5 was audited as an external technical review of the Optimization Catalog V1. The software safety baseline is largely preserved: no arbitrary execution was found, Registry targets are allowlisted and canonicalized, snapshot precedes write for the controlled flow, rollback preserves value kind/value/view for supported Registry kinds, and cross-process locking remains validated.

The catalog is not yet ready for Phase 6 without corrections. The main issues are product and Windows-correctness issues, not broad engine-safety failures: HKLM operations are still not validated with real elevated apply/rollback in a safe Windows VM, several automatic preset choices are mostly UX/privacy preferences rather than performance optimizations, some Evidence=Strong classifications conflate "Microsoft documents this setting" with "this is a defensible optimization", activation boundaries are not modeled beyond "no reboot", and rollback validation is overstated for the 12 optimization definitions because only one HKCU catalog item was exercised end-to-end.

# Verdict

APPROVED WITH CORRECTIONS

# Scope

Audited:

- code under `src/BorealBoost.Core`, `src/BorealBoost.Analysis`, `src/BorealBoost.Optimization`, `src/BorealBoost.Restore`, `src/BorealBoost.Infrastructure`, `src/BorealBoost.System`, `src/BorealBoost.Agent`, `src/BorealBoost.App`;
- tests under `tests`;
- Phase 5 catalog documentation and prior Phase 4 audit/revalidation documents;
- official Microsoft sources for Policy CSP and Settings registry references;
- build, tests, dependency audit, security search and safe runtime tests.

No code, existing documentation, tests, dependencies or commits were changed. This report is the only file created in this execution.

# Catalog Inventory

Real code inventory:

| Metric | Count |
| --- | ---: |
| `OptimizationDefinition` total including internal integration proof | 13 |
| Real user-facing modifying definitions | 12 |
| Internal proof definition excluded from catalog counts | 1 |
| Safe | 6 |
| Medium | 5 |
| Advanced | 1 |
| Aggressive | 0 |
| Experimental | 0 |
| SecurityTradeoff | 0 |
| Windows 10 compatible by catalog model | 5 |
| Windows 11 compatible by catalog model | 11 |
| Requires elevation | 2 |
| Requires reboot | 0 |
| Declares `SupportsUndo=true` | 12 |
| Shares implemented Registry rollback handler | 12 |
| Proven by real catalog-specific HKCU end-to-end apply/verify/rollback in this audit | 1 |
| HKLM real elevated apply/rollback validated in this audit | 0 |

The inventory matches the documented count of 12 real definitions in `OPTIMIZATION_CATALOG.md`, excluding `BB.OPT.INTEGRATION.REGISTRY_PROOF`.

# Catalog Architecture

The V1 catalog is built in `BuiltInOptimizationCatalog` and enforced by:

- `TrustedRegistryOperationTargets` as the exact Registry allowlist;
- `CanonicalOperationSpecValidator` for catalog version, optimization id, operation id and exact `OperationSpec` equality;
- `ExecutionPlanValidator` and `ExecutionPlanHasher` for plan integrity;
- `AgentIpcSession` for Agent-side canonical validation and elevation checks;
- `BorealIntegrationRegistryOperationHandler` for typed Registry detection, snapshot, apply, verify and rollback.

No second operational catalog source was found in the UI. There is duplication between `BuiltInOptimizationCatalog` and `TrustedRegistryOperationTargets`, but the Agent validates against the canonical catalog and the allowlist. This duplication is manageable in V1 but creates drift risk as the catalog grows.

# Source Validation Method

Sources were classified as:

- `DOCUMENTED_SUPPORTED`: Microsoft documents the exact Registry setting and value semantics for direct settings access.
- `DOCUMENTED_POLICY`: Microsoft documents the exact Policy CSP / Group Policy mapping and Registry value.
- `OBSERVED_IMPLEMENTATION_DETAIL`: Microsoft documents a related setting but not the exact Registry write used as a stable contract.
- `COMMUNITY_KNOWN`: common Windows Registry practice or Microsoft Q&A, but not a formal support contract.
- `UNVERIFIED`: no sufficient primary source found.

Primary sources used:

- Microsoft Learn ApplicationManagement Policy CSP: `https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement`
- Microsoft Learn Privacy Policy CSP: `https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy`
- Microsoft Learn Windows 11 settings reference: `https://learn.microsoft.com/en-us/windows/apps/develop/settings/settings-windows-11`
- Microsoft Learn common Windows settings reference: `https://learn.microsoft.com/en-us/windows/apps/develop/settings/settings-common`
- Microsoft Windows privacy/services guidance: `https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services`

# Optimization-by-Optimization Audit

| OptimizationId | Risk | Source class | Target summary | Audit conclusion |
| --- | --- | --- | --- | --- |
| `BB.OPT.VISUAL.TRANSPARENCY.DISABLE` | Safe | DOCUMENTED_SUPPORTED for setting; impact only Moderate | HKCU `Themes\Personalize\EnableTransparency` DWORD `0` | VALIDATED_WITH_CAVEAT: registry target is documented; performance/responsiveness value is low and not FPS evidence. |
| `BB.OPT.WINDOWS.EXPLORER.SHOW_EXTENSIONS` | Safe | OBSERVED_IMPLEMENTATION_DETAIL / COMMUNITY_KNOWN for `HideFileExt` | HKCU `Explorer\Advanced\HideFileExt` DWORD `0` | QUESTIONABLE for Basic auto-apply: behavior is useful, but it is a UX/security-visibility preference and the exact Registry target is not as strongly supported by the cited source. |
| `BB.OPT.WINDOWS.AUTOPLAY.DISABLE` | Safe | DOCUMENTED_SUPPORTED | HKCU `AutoplayHandlers\DisableAutoplay` DWORD `1` | VALIDATED_WITH_CAVEAT: target is documented for AutoPlay toggling; it is maintenance/security-posture, not a performance optimization. |
| `BB.OPT.PRIVACY.START.RECOMMENDATIONS.DISABLE` | Safe | DOCUMENTED_SUPPORTED for Win11 setting | HKCU `Explorer\Advanced\Start_IrisRecommendations` DWORD `0` | VALIDATED_WITH_CAVEAT: target is documented; Windows 11 build-specific behavior still needs broader validation. |
| `BB.OPT.WINDOWS.START.MORE_PINS` | Safe | DOCUMENTED_SUPPORTED for Win11 setting | HKCU `Explorer\Advanced\Start_Layout` DWORD `1` | QUESTIONABLE for automatic presets: it is a layout preference, not an optimization. |
| `BB.OPT.GAMING.GAMEBAR.CONTROLLER_SHORTCUT.DISABLE` | Safe | DOCUMENTED_SUPPORTED for setting | HKCU `GameBar\UseNexusForGameBarEnabled` DWORD `0` | VALIDATED_WITH_CAVEAT: prevents controller shortcut; not background work reduction. |
| `BB.OPT.GAMING.GAMEBAR.RECORDING_SHORTCUT.DISABLE` | Medium | DOCUMENTED_SUPPORTED for toggle setting | HKCU `GameDVR\VKMToggleRecording` DWORD `0` | VALIDATED_WITH_CAVEAT: Microsoft documents the toggle, but the catalog text should avoid implying proven performance/background contention gain. |
| `BB.OPT.GAMING.GAMEBAR.BROADCAST_SHORTCUT.DISABLE` | Medium | DOCUMENTED_SUPPORTED for toggle setting | HKCU `GameDVR\VKMToggleBroadcast` DWORD `0` | VALIDATED_WITH_CAVEAT: same caveat as recording. |
| `BB.OPT.PRIVACY.GAMEBAR.CAMERA_CAPTURE_SHORTCUT.DISABLE` | Medium | DOCUMENTED_SUPPORTED for toggle setting | HKCU `GameDVR\VKMToggleCameraCapture` DWORD `0` | VALIDATED_WITH_CAVEAT: privacy/user-preference impact is clearer than performance value. |
| `BB.OPT.PRIVACY.GAMEBAR.MIC_CAPTURE_SHORTCUT.DISABLE` | Medium | DOCUMENTED_SUPPORTED for toggle setting | HKCU `GameDVR\VKMToggleMicrophoneCapture` DWORD `0` | VALIDATED_WITH_CAVEAT: privacy/user-preference impact is clearer than performance value. |
| `BB.OPT.PRIVACY.ADVERTISING_ID.DISABLE` | Medium | DOCUMENTED_POLICY | HKLM `Policies\Microsoft\Windows\AdvertisingInfo\DisabledByGroupPolicy` DWORD `1` | VALIDATED_WITH_CAVEAT: policy mapping is official; real elevated HKLM apply/rollback remains unvalidated in a safe VM. It is privacy, not performance. |
| `BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE` | Advanced | DOCUMENTED_POLICY | HKLM `Policies\Microsoft\Windows\GameDVR\AllowGameDVR` DWORD `0` | VALIDATED_WITH_CAVEAT: Microsoft documents policy, desktop-only Windows 10 enforcement, and value semantics; real elevated HKLM apply/rollback remains unvalidated. |

# Windows Policy vs Registry Analysis

The two HKLM entries are policies:

- `DisableAdvertisingId` maps to `Software\Policies\Microsoft\Windows\AdvertisingInfo`, value `DisabledByGroupPolicy`, and Microsoft documents that enabling the policy turns off the advertising ID.
- `AllowGameDVR` maps to `Software\Policies\Microsoft\Windows\GameDVR`, value `AllowGameDVR`, and Microsoft documents value `0` as not allowed.

The HKCU entries are user preferences / settings state, not enterprise policy. They should not be represented as equivalent to Policy CSP. This distinction is mostly documented, but the UI/preset flow still risks presenting preference changes as optimization actions.

# Windows 10 Compatibility

Catalog model:

- Windows 10 22H2 x64 build 19045 is preserved as legacy target.
- Windows 10-compatible definitions: transparency, file extensions, AutoPlay, Advertising ID policy and Game DVR policy.
- Game DVR policy is restricted to Windows 10 build 19045 desktop and `NotVirtualMachine`.

Validation status:

- Unit-tested by fixture.
- No Windows 10 VM/hardware was available in this audit.
- Windows 10 real validation remains UNVALIDATED.

# Windows 11 Compatibility

Catalog model:

- Windows 11-compatible definitions: all except `BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE`.
- Current machine used for real Scanner -> Analysis -> Preset Preview reports OS version `10.0.26200`.

Validation status:

- Scanner -> Analysis -> Preset Preview was validated on the current Windows 11 machine.
- Current build `26200` should not be generalized to every stable Windows 11 release without a build matrix.
- Start layout/recommendations and Game Bar settings are explicitly documented in Microsoft Learn for Windows 11, but behavior across releases still needs validation.

# Build Awareness

Build constraints exist, but they are coarse for Windows 11:

- Windows 11 definitions use minimum build `22000`.
- The current tested build is `26200`.
- The catalog does not model narrower release/build activation boundaries for Start and Game Bar settings.

This is not a safety blocker because the operations are allowlisted and reversible, but it is a readiness gap before Phase 6.

# Preset Engine

The preset engine is deterministic and policy-based. It filters by:

- current snapshot compatibility;
- stale analysis result;
- unsupported or unknown Windows facts;
- dependencies and conflicts;
- evidence unknown;
- preset risk policy;
- security tradeoff exclusion;
- rollback/restart policy.

Runtime preview on the current machine:

- Basic: 5 selected, 0 requires confirmation, 0 blocked, 1 not applicable.
- Medium: 11 selected, 0 requires confirmation, 0 blocked, 1 not applicable.
- Advanced: 11 selected, 0 requires confirmation, 0 blocked, 1 not applicable.

# Basic Audit

Basic currently selects 5 items on the current machine:

- transparency;
- file extensions;
- AutoPlay;
- Start recommendations;
- Start more pins.

Policy constraints are enforced: no Medium/Advanced/Aggressive, no Experimental, no SecurityTradeoff, no reboot, full rollback required.

Finding: Basic includes UX/preferences (`Show file extensions`, `Start more pins`, arguably Start recommendations) that are not universal performance optimizations. They may be appropriate tools/custom choices, but automatic Basic application should be narrower or explicitly framed as preference hardening.

# Medium Audit

Medium selects Safe + Medium compatible items and excludes SecurityTradeoff. It currently selects 11 items on the current Windows 11 machine, effectively almost the entire Windows 11 catalog.

Finding: Medium includes several personal preference/privacy changes: Advertising ID policy, Game Bar camera/microphone capture toggles, Start layout and file extensions. These are defensible options, but applying them automatically as an optimization preset is too broad without explicit user intent.

# Advanced Audit

Advanced does not blindly select all items. Advanced/confirmation-required items are modeled as `RequiresConfirmation`; the current Windows 11 machine marks the Windows 10-only Game DVR policy as `NotApplicable`.

Finding: UI currently disables `RequiresConfirmation` items rather than providing a completed explicit confirmation flow. This is safe, but Advanced/Custom Phase 5 usability remains incomplete.

# Custom Audit

Custom is not a bypass:

- blocked/incompatible/unknown items remain non-selectable;
- `SelectedOptimizationIds()` only includes items with `CanSelect=true`;
- planner and Agent still revalidate exact canonical operations.

No evidence was found that Custom can execute `Blocked` or arbitrary targets.

# Risk Classification

The technical risk classifications are mostly conservative:

- Safe for low-impact HKCU preferences is acceptable.
- Medium for capture/privacy toggles is acceptable due functional side effects.
- Advanced for HKLM Game DVR policy is acceptable.

Product caveat: risk level is being used to drive automatic preset suitability, but side effect category and user preference impact are not first-class enough. A Safe UX preference can still be inappropriate for automatic broad application.

# Evidence Classification

Evidence=Strong is overused for items where Microsoft documents the setting target, but not the optimization/performance value.

Examples:

- Game Bar `VKM*` toggles are documented, but "background contention" and "gaming optimization" value is workload-dependent and not proven by the source.
- Start layout and Start recommendations are documented settings, but the source does not make them performance optimizations.
- Advertising ID policy is strongly documented as privacy policy, not performance optimization.

Recommendation: split source reliability from optimization impact evidence, or downgrade evidence where the method is documented but the optimization value is weak.

# Expected Impact

No numeric FPS, latency, percentage or millisecond claims were found in the catalog/UI. That is correct.

However, several `ExpectedImpact` labels still blur the product message:

- `BackgroundContention` for Game Bar shortcut/toggle items needs careful wording.
- `VisualEffects` and `Responsiveness` for UI preferences are qualitative and low.
- Advertising ID and Start layout are privacy/UX preference items, not performance items.

# User Preference vs Optimization

The V1 catalog is safe but commercially mixed. Classification:

| OptimizationId | Primary classification |
| --- | --- |
| `BB.OPT.VISUAL.TRANSPARENCY.DISABLE` | ResponsivenessOptimization / UX tradeoff |
| `BB.OPT.WINDOWS.EXPLORER.SHOW_EXTENSIONS` | UXPreference / SecurityVisibility |
| `BB.OPT.WINDOWS.AUTOPLAY.DISABLE` | Maintenance / SecurityPosture |
| `BB.OPT.PRIVACY.START.RECOMMENDATIONS.DISABLE` | PrivacyPreference / UXPreference |
| `BB.OPT.WINDOWS.START.MORE_PINS` | UXPreference |
| `BB.OPT.GAMING.GAMEBAR.CONTROLLER_SHORTCUT.DISABLE` | GamingFeaturePreference |
| `BB.OPT.GAMING.GAMEBAR.RECORDING_SHORTCUT.DISABLE` | GamingFeaturePreference / accidental capture prevention |
| `BB.OPT.GAMING.GAMEBAR.BROADCAST_SHORTCUT.DISABLE` | GamingFeaturePreference / accidental streaming prevention |
| `BB.OPT.PRIVACY.GAMEBAR.CAMERA_CAPTURE_SHORTCUT.DISABLE` | PrivacyPreference |
| `BB.OPT.PRIVACY.GAMEBAR.MIC_CAPTURE_SHORTCUT.DISABLE` | PrivacyPreference |
| `BB.OPT.PRIVACY.ADVERTISING_ID.DISABLE` | PrivacyPreference / Policy |
| `BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE` | GamingFeatureConfiguration / Policy |

# Commercial Optimization Value

Reasonable performance/responsiveness value:

- Directly plausible: transparency disable.
- Workload-dependent/plausible only if capture/broadcast is otherwise active or accidentally toggled: Game DVR policy and some Game Bar toggles.
- Mostly privacy/UX/preference: 8 to 10 of 12 depending on classification.

Conclusion: Engine/catalog safety is good enough to continue with corrections, but the current catalog does not yet deliver strong commercial performance optimization value. It is still mostly safe Windows preferences and privacy/UX/gaming feature toggles.

# Gaming Value

Game DVR policy has the strongest gaming relevance because Microsoft documents it as disabling Windows Game Recording and Broadcasting on Windows 10 desktop.

The five Windows 11 Game Bar items mostly affect toggles/shortcut paths. They may reduce accidental recording/capture paths, but should not be marketed as FPS or background service optimizations without behavioral measurement.

# Agent Canonical Validation

Pass.

Agent validation path:

- protocol/session checks;
- `ValidateOperation` on every privileged message;
- catalog version match;
- optimization id exists;
- operation id exists in that definition;
- `OperationSpec` equals canonical operation spec;
- handler exists;
- Agent-side security validator;
- elevation required check for HKLM definitions.

Tamper tests cover target change, desired value change, operation type mismatch, catalog version downgrade and unknown operation. No UI trust bypass was found.

# Registry Allowlist

Pass.

The real catalog allowlist contains exactly the 12 real V1 Registry targets. No free Registry path was found in the UI or IPC payload path that can pass Agent validation.

`TrustedRegistryOperationTargets.IsTrustedCatalogOperation` matches exact operation id, operation type, hive, key path, value name, view and desired state.

# Arbitrary Execution Validation

Pass.

Security search found:

- product `Process.Start` only in `AgentBootstrapService`, used to launch BorealBoost.Agent with internally constructed typed arguments;
- product `CreateSubKey` and `DeleteValue` only in the typed Registry handler;
- no `ExecuteCommand`, `ExecutePowerShell`, `ExecuteProcess`, `cmd.exe`, `powershell.exe`, `pwsh.exe`, generic shell or script execution in product code;
- `powershell.exe` appears only in a system test helper for cross-process lock validation;
- `cmd.exe` appears only in negative test payload strings.

# Snapshot

Pass with caveat.

Snapshot captures:

- operation id;
- resource type and identity;
- Registry hive/key/value/view;
- previous existence;
- previous value kind;
- previous value payload;
- capture timestamp;
- integrity hash.

Caveat: snapshot tracks value existence, not Registry key existence. If BorealBoost creates a missing policy key to write a value and then rolls back a previously absent value, rollback deletes the value but can leave an empty key behind. This is not destructive for value state, but it is not exact resource restoration.

# Verification

Storage verification is implemented: Verify reads the Registry state after Apply and compares it to the desired state.

Behavior verification is not implemented. For many Windows settings, `Registry value == desired` proves storage state only, not that Explorer, Settings, policy refresh or Game Bar behavior already reflected the change.

# Rollback

Pass with caveat.

The shared handler preserves supported Registry kinds:

- String;
- ExpandString;
- DWord;
- QWord;
- MultiString;
- Binary.

External state protection remains present: rollback refuses to overwrite a current state that no longer matches BorealBoost's applied desired state.

Caveat: only one real catalog item, `BB.OPT.VISUAL.TRANSPARENCY.DISABLE`, was executed end-to-end through Agent IPC in this audit. The other 11 share the same handler and unit/system coverage, but are not individually proven by real apply/rollback on their exact targets.

# External State Protection

Pass.

The handler compares current state with the desired applied state before rollback. If a third party changes the value, rollback returns an outcome/unknown/manual state instead of overwriting blindly.

# Plan Integrity

Pass.

Plan hash validation and canonical operation validation are active before execution. Operation tampering after approval is rejected by plan validation and Agent canonical validation.

# Cross-Process Concurrency

Pass.

The cross-process lock system test passed:

- Process A acquires the lock.
- Process B is rejected while A holds it.
- The lock becomes available after A exits.

# Elevation

Partial.

Agent has elevation checks for definitions with `RequiresElevation=true`. Phase 4 validated elevated Agent infrastructure. For Phase 5, HKLM operation-specific real elevated apply/rollback was not executed because no safe Windows VM/client validation context was available.

# Group Policy / MDM Considerations

The two HKLM definitions are policy-backed and may be overwritten by GPO/MDM/Intune:

- `BB.OPT.PRIVACY.ADVERTISING_ID.DISABLE`;
- `BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE`.

The product should not promise permanent persistence on managed devices. Detection also does not currently distinguish "BorealBoost set it" from "enterprise policy enforces it". That matters for UX wording and rollback expectations.

# Activation Boundaries

Finding: activation boundaries are not modeled.

All 12 definitions declare no reboot. That is not the same as immediate behavioral effect. Potential boundaries include:

- Immediate storage state;
- Explorer restart;
- app restart;
- sign-out;
- policy refresh;
- unknown.

The current model can say "Sem reboot", but not "may require Explorer restart/app restart/policy refresh".

# UI Audit

The Optimization page is safe:

- no one-click immediate apply;
- Dry Run required before execute;
- `CanSelect` disables blocked/not applicable/confirmation-required items;
- no FPS/percentage gain claims found.

Gaps:

- side effects and source quality are too compressed for technician review;
- Advanced/Custom `RequiresConfirmation` has no complete confirmation flow and is effectively disabled;
- "Rollback completo" is shown for shared handler capability and may imply more operation-specific proof than exists.

# Privacy

The current catalog values are generally low sensitivity. Snapshot values may still include user preferences and policy state, and should not be logged wholesale.

No logs of full payload or sensitive Registry values were found in the audited search output.

# Logging

Structured logging remains focused on IDs and outcomes. No catalog payload dump was found.

Recommended before Phase 6: ensure catalog apply/rollback logs include IDs and outcomes, not full Registry values by default.

# Test Quality

Total tests executed: 202.

Coverage matrix:

| Area | Coverage |
| --- | --- |
| Catalog count/contract | GOOD |
| Preset policy | GOOD |
| Compatibility filtering | GOOD |
| Detection/dry run | PARTIAL |
| Plan validation/hash | GOOD |
| Agent tamper rejection | GOOD |
| Snapshot integrity | GOOD |
| Apply/verify | PARTIAL |
| Rollback exact supported kinds | GOOD for shared handler |
| External state protection | GOOD |
| Cross-process concurrency | GOOD |
| Windows 10 real validation | MISSING |
| Windows 11 current-machine validation | PARTIAL |
| HKLM elevated real apply/rollback | MISSING |
| Independent Registry target correctness | PARTIAL |

Several tests prove internal consistency rather than independent Windows correctness. Example: tests using catalog definitions to generate expected accepted operations prove catalog/Agent agreement, not that a given Registry path is the correct Windows contract.

# Controlled Runtime Validation

Executed safely:

- `Agent_executes_catalog_v1_hkcu_registry_operation_with_snapshot_verify_and_rollback`
- Result: passed.
- Operation: `BB.OPT.VISUAL.TRANSPARENCY.DISABLE`.
- Scope: HKCU only.
- Flow: Agent IPC -> snapshot -> apply -> storage verify -> rollback -> original state restored.

Executed read-only:

- `Real_scanner_analysis_flows_into_catalog_preset_preview_read_only`
- Result: passed.
- Output: Basic selected 5, Medium selected 11, Advanced selected 11, one NotApplicable item.

Executed lock validation:

- `Cross_process_optimization_lock_rejects_second_process_holder`
- Result: passed.

No HKLM write was executed.

# Windows 10 VM Validation

UNVALIDATED.

No Windows 10 22H2 build 19045 VM/hardware validation was available during this audit. Windows 10 compatibility remains model- and fixture-tested only.

# HKLM Validation

UNVALIDATED for real apply/rollback.

The following HKLM definitions were not applied in this audit:

- `BB.OPT.PRIVACY.ADVERTISING_ID.DISABLE`;
- `BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE`.

This is acceptable for audit safety, but must remain a required validation item before Phase 6/release readiness.

# Rejected Tweaks Validation

No hidden implementation of rejected tweaks was found.

Search/doc review found rejected or deferred status for:

- Defender disable/exclusions;
- Firewall disable;
- Windows Update permanent disable;
- pagefile disable;
- HPET/BCD/timer hacks;
- universal TCP/netsh tweaks;
- arbitrary service disable lists;
- AppX/debloat removal;
- OneDrive removal;
- Ultimate Performance plan;
- HAGS universal toggle.

# Security Search

Classified occurrences:

| Occurrence | Classification |
| --- | --- |
| `Process.Start` in `AgentBootstrapService` | Allowed known Agent bootstrap only. |
| `Process.Start` / `powershell.exe` in system tests | Test helper for cross-process lock only. |
| `cmd.exe` in unit tests | Negative payload string only. |
| `CreateSubKey` / `DeleteValue` in product Registry handler | Typed allowlisted Registry operation only. |
| `CreateSubKey` / `DeleteValue` / `DeleteSubKeyTree` in tests | Controlled HKCU test setup/cleanup only. |
| `Registry.SetValue` | Safety-test search token only; no product use found. |

No arbitrary execution or non-catalog mutation path was found.

# Build Validation

Commands executed:

- `dotnet --info`: SDK `10.0.400`; OS version `10.0.26200`; RID `win-x64`.
- `dotnet restore .\BorealBoost.sln`: success.
- `dotnet build .\BorealBoost.sln --no-restore`: success, 0 warnings, 0 errors.
- `dotnet test .\BorealBoost.sln --no-build`: success.

Test totals:

- Unit: 148 passed.
- Integration: 16 passed.
- System: 38 passed.
- Total: 202 passed, 0 failed, 0 skipped.

# Dependency Validation

`dotnet list .\BorealBoost.sln package --vulnerable`:

- no vulnerable packages reported for all projects using current NuGet sources.

`dotnet list .\BorealBoost.sln package --outdated`:

- test packages have updates: `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`;
- `BorealBoost.App` has `Microsoft.WindowsAppSDK` update available;
- no package was updated during audit.

# Phase Boundary

Pass.

Not found:

- operational drivers;
- benchmark final implementation;
- reporting final implementation;
- installer final implementation;
- Phase 6 implementation;
- catalog of services/power/DNS/debloat/telemetry/drivers/Windows Update tweaks.

# Product Classification

| OptimizationId | TechnicalCategory | AutomaticPresetSuitability | PerformanceRelevance | UserPreferenceImpact | AuditConclusion |
| --- | --- | --- | --- | --- | --- |
| `BB.OPT.VISUAL.TRANSPARENCY.DISABLE` | Responsiveness/Visual | Basic acceptable with wording | Low plausible | Medium | VALIDATED_WITH_CAVEAT |
| `BB.OPT.WINDOWS.EXPLORER.SHOW_EXTENSIONS` | UX/Security visibility | Questionable for Basic | None | Medium | QUESTIONABLE |
| `BB.OPT.WINDOWS.AUTOPLAY.DISABLE` | Maintenance/Security posture | Basic acceptable with wording | Low/indirect | Medium | VALIDATED_WITH_CAVEAT |
| `BB.OPT.PRIVACY.START.RECOMMENDATIONS.DISABLE` | Privacy/UX | Questionable for Basic | None/low | Medium | VALIDATED_WITH_CAVEAT |
| `BB.OPT.WINDOWS.START.MORE_PINS` | UX layout | Not suitable for automatic Basic | None | High | QUESTIONABLE |
| `BB.OPT.GAMING.GAMEBAR.CONTROLLER_SHORTCUT.DISABLE` | Gaming preference | Medium/custom better | None/direct prevention only | Medium | VALIDATED_WITH_CAVEAT |
| `BB.OPT.GAMING.GAMEBAR.RECORDING_SHORTCUT.DISABLE` | Gaming feature toggle | Medium/custom with clear side effect | Workload-dependent | Medium | VALIDATED_WITH_CAVEAT |
| `BB.OPT.GAMING.GAMEBAR.BROADCAST_SHORTCUT.DISABLE` | Gaming feature toggle | Medium/custom with clear side effect | Workload-dependent | Medium | VALIDATED_WITH_CAVEAT |
| `BB.OPT.PRIVACY.GAMEBAR.CAMERA_CAPTURE_SHORTCUT.DISABLE` | Privacy preference | Custom/Medium with consent | None | Medium | VALIDATED_WITH_CAVEAT |
| `BB.OPT.PRIVACY.GAMEBAR.MIC_CAPTURE_SHORTCUT.DISABLE` | Privacy preference | Custom/Medium with consent | None | Medium | VALIDATED_WITH_CAVEAT |
| `BB.OPT.PRIVACY.ADVERTISING_ID.DISABLE` | Privacy policy | Not performance preset | None | Medium | VALIDATED_WITH_CAVEAT |
| `BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE` | Gaming capture policy | Advanced only | Workload-dependent | High | VALIDATED_WITH_CAVEAT |

# Findings Table

| ID | Severity | OptimizationId | File/Region | Evidence | Impact | Scenario | Recommended correction |
| --- | --- | --- | --- | --- | --- | --- | --- |
| P5-HIGH-001 | HIGH | `BB.OPT.PRIVACY.ADVERTISING_ID.DISABLE`, `BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE` | `OPTIMIZATION_CATALOG.md`; `TrustedRegistryOperationTargets`; `BuiltInOptimizationCatalog` | HKLM operations are documented and dry-run/canonical validated, but real elevated apply/rollback is explicitly deferred. | Release readiness gap for elevated catalog items. | Medium/Advanced catalog item reaches client before HKLM rollback is validated in safe VM. | Validate HKLM apply/verify/rollback on safe Windows 10/11 VM/client context, or keep these non-auto/blocked until proven. |
| P5-HIGH-002 | HIGH | Multiple | `BuiltInOptimizationCatalog` risk/evidence/impact fields | `EvidenceLevel.Strong` is used where source proves setting existence, not optimization value. | Product may overstate technical confidence. | Technician sees Strong and assumes proven performance benefit. | Split `SourceReliability` from `OptimizationImpactEvidence`, or downgrade evidence/impact wording. |
| P5-MEDIUM-001 | MEDIUM | Basic preset items | `OptimizationPresetEngine`; `OptimizationViewModel` | Basic selects Start layout/file extensions/recommendations automatically. | Automatic preset applies personal preferences as optimization. | User expects safe performance baseline but gets layout and UX behavior changes. | Reclassify preference items to Custom/Tools or require explicit opt-in. |
| P5-MEDIUM-002 | MEDIUM | Medium preset items | `OptimizationPresetEngine` | Medium selects 11 Windows 11 items, including privacy/UX/preferences. | Medium is nearly "select all" for Win11 catalog. | Technician applies Medium and changes several personal preferences unrelated to performance. | Make Medium more selective or require category-level consent. |
| P5-MEDIUM-003 | MEDIUM | All 12 | `OptimizationModels`; `BuiltInOptimizationCatalog` | All declare no reboot; no activation boundary model exists. | UI can imply effect is immediate when app restart/sign-out/policy refresh may be needed. | Registry verify passes but feature behavior remains unchanged until shell/app/policy refresh. | Add activation/effect boundary metadata separate from reboot. |
| P5-MEDIUM-004 | MEDIUM | All 12 | `OPTIMIZATION_CATALOG.md`; tests | Only one catalog definition had real HKCU end-to-end apply/rollback; others rely on shared handler tests. | Rollback proof may be overstated. | Release notes claim all 12 individually proven. | Track rollback coverage as `PROVEN_REAL`, `SHARED_HANDLER_PROVEN`, `UNIT_ONLY`, `UNVALIDATED`. |
| P5-MEDIUM-005 | MEDIUM | HKCU/HKLM RegistryValue operations | `BorealIntegrationRegistryOperationHandler.RestoreSnapshot` | Snapshot tracks value existence but not key existence; rollback of absent value can leave a created empty key. | State restoration is not exact at key level. | Missing policy key is created, value rolled back/deleted, empty key remains. | Capture key existence and delete empty key created by BorealBoost when safe. |
| P5-MEDIUM-006 | MEDIUM | Catalog manifest | `BuiltInOptimizationCatalog.ComputeCatalogContentHash` | Hash covers IDs/risk/evidence/preset/security flag and operation target/value, but not title, description, side effects, supported Windows, elevation or confirmation flags. | Integrity hash may miss security/product-significant catalog drift. | Catalog content changes wording/compatibility/elevation without hash change. | Canonicalize and hash all security, compatibility and user-facing decision fields. |
| P5-LOW-001 | LOW | Advanced/Custom | `OptimizationViewModel`; `OptimizationPage.xaml` | `RequiresConfirmation` items are disabled, no completed explicit confirmation flow. | Safe but incomplete UX. | Advanced item is visible but cannot be executed after review. | Add explicit confirmation UX before Phase 6 or keep Advanced disabled intentionally. |
| P5-LOW-002 | LOW | UI catalog display | `OptimizationViewModel` | UI shows compact risk/evidence/impact but not source class, activation boundary or side effects in detail. | Technician review is less informed. | User sees category and risk but not the real side effect. | Add details panel before wider catalog release. |
| P5-LOW-003 | LOW | Windows 11 build validation | Runtime validation | Current validation machine is Windows build `26200`; stable Windows 11 build matrix not validated. | Compatibility confidence is narrower than catalog states. | Setting works on current build but differs on 22631/26100. | Add Windows 11 stable build validation matrix. |

# Blockers

None.

# High Priority

- P5-HIGH-001: HKLM elevated real apply/rollback not validated for the two HKLM definitions.
- P5-HIGH-002: Evidence model overstates confidence by conflating source reliability with optimization value.

# Medium Priority

- P5-MEDIUM-001: Basic includes UX/preference items.
- P5-MEDIUM-002: Medium selects nearly the whole Windows 11 catalog and includes privacy/UX preferences.
- P5-MEDIUM-003: Activation boundaries are not modeled.
- P5-MEDIUM-004: Rollback proof is shared-handler proven, not individually proven for all 12 definitions.
- P5-MEDIUM-005: Rollback does not restore originally absent Registry key existence exactly.
- P5-MEDIUM-006: Catalog content hash omits several decision-relevant fields.

# Low Priority

- P5-LOW-001: Advanced/Custom confirmation UX is safe but incomplete.
- P5-LOW-002: UI lacks enough source/side-effect detail for technician review.
- P5-LOW-003: Windows 11 validation is current-machine only.

# Unvalidated Items

- Windows 10 22H2 build 19045 real VM/hardware validation.
- HKLM elevated apply/verify/rollback for Advertising ID policy.
- HKLM elevated apply/verify/rollback for Game DVR policy.
- Behavior-level verification for all settings after Registry storage verify.
- Stable Windows 11 build matrix beyond build `26200`.

# Engine/Catalog Safety Assessment

The engine/catalog safety architecture is acceptable to continue with corrections:

- no arbitrary execution;
- no free Registry target;
- Agent canonical validation is active;
- snapshot-before-write is present;
- storage verification is mandatory;
- rollback protects against external state change;
- cross-process session lock is validated;
- destructive rejected tweaks were not added.

The safety assessment is not a blanket release approval because HKLM real validation and product/evidence corrections remain required.

# Commercial Optimization Value Assessment

The catalog is safe and conservative, but current commercial optimization value is limited.

Counts by primary product value:

- Performance/responsiveness/gaming-resource relevance: 2 strong/plausible items plus 3 workload-dependent Game Bar/Game DVR items.
- Privacy/UX/preference/maintenance primary value: 7 to 10 items depending on classification.

Conclusion: Phase 5 V1 is more of a safe preference/policy catalog foundation than a strong performance optimization catalog.

# Required Corrections Before Phase 6

1. Validate HKLM elevated apply/verify/rollback in a safe VM/client environment, or keep HKLM items out of automatic presets.
2. Split source reliability from optimization-impact evidence and revise `EvidenceLevel.Strong` where appropriate.
3. Rework Basic/Medium preset suitability so personal UX/privacy preferences require explicit user intent.
4. Add activation/effect boundary metadata and UI wording separate from reboot.
5. Track rollback coverage per optimization instead of treating shared handler capability as individual proof.
6. Capture Registry key existence where exact rollback requires removing a key BorealBoost created.
7. Expand catalog content hash canonicalization to include all security/product-significant fields.
8. Add Windows 10 22H2 and stable Windows 11 validation matrix before release readiness.

# Final Recommendation

Do not start Phase 6 as a release-hardening/finalization pass until the HIGH findings are resolved and the Medium preset/evidence/activation issues are addressed. The Phase 5 engine path is technically safe enough to continue, but the catalog should be treated as APPROVED WITH CORRECTIONS, not fully approved for product claims or broad automatic application.
