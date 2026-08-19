# Task 2 Final Audit

Audit scope: final Task 2 optimization, its evidence, research-only experiments, repository dependencies, and cleanup readiness. This audit does not change production behavior, delete files, stage files, or commit.

Evidence labels used below: `MEASURED`, `CALCULATED`, `EXPECTED`, `OBSERVED`, `NOT MEASURED`, and `BLOCKED`.

## Production Changes

The final implementation is represented by the optimized prefab and its deterministic validation/runtime support. The original imported prefab remains the baseline and was not modified.

| File | What changed | Why | Evidence | Visual evidence | Correctness evidence | Trade-offs |
|---|---|---|---|---|---|---|
| `Assets/Optimization/Prefabs/ParticleEffectsOptimized.prefab` | PL1/PL2/PL3 `lines` trail Minimum Vertex Distance changed from `0.2` to `0.4`. PL2/PL3 rain4 systems were replaced by one `rainCombined` system/renderer each. | Reduce trail tessellation; remove duplicate rain simulation/rendering while preserving the four-particle effect. | Trail geometry: PL1 126→86 triangles, PL2 196→156, PL3 410→224; PL3 vertices 460→274. Supplied live rain Frame Debugger: one rain draw, 16 vertices, 24 indices, 8 triangles. `MEASURED`. | Dedicated before/after geometry captures; final PL2/PL3 rain captures. Trail identity is phase-dependent; full pixel equivalence is not established. `OBSERVED`. | Final promotion replay: PL2/PL3 100/100 fixed cycles plus 32 additional seed pairs each; four particles, 2/2 side balance, no stale state or exceptions. `MEASURED`. | Coarser trail sampling can reduce smoothness or continuity if pushed further. Single-system rain depends on deterministic emitter behavior and retained seed slots. |
| `Assets/Optimization/Scripts/DeterministicRainEmitter.cs` | Runtime replay emits the final rain particles directly into `rainCombined` using four `EmitParams` calls. Fixed compatibility samples and an allocation-free deterministic fallback are retained. | Preserve deterministic replay after removing the duplicate rain simulation and particle-copy path. | Exact particle counts, side balance, seed behavior, and zero recurring allocation in warmed tests. `MEASURED`. | Rain captures show the retained visible effect. Full all-effect equivalence is not pixel-proven. `OBSERVED` / `NOT MEASURED`. | No `GetParticles`, `SetParticles`, copy arrays, or `LateUpdate` polling; promotion validation passed. `MEASURED`. | More replay-specific code and seed compatibility logic; correctness depends on deterministic invocation and timing. |
| `Assets/Optimization/Scripts/OptimizationBenchmarkRunner.cs` | Caches hierarchy order, clears/stops replay systems, assigns deterministic seeds, preserves rain seed slots, and emits final rain after playback starts. | Make before/after and final replay comparisons repeatable and prevent stale/accumulated particle state. | Final promotion JSON records 100 fixed cycles, 32 additional seed pairs, and zero recurring allocation. `MEASURED`. | Supports the captured comparison states; runner code itself is not a visual asset. | Transition, PL1, PL2, and PL3 replayed; zero missing MonoBehaviours and no reported exceptions. `MEASURED`. | Benchmark/validation code adds test-path complexity; it is not Android GPU proof and does not isolate `ParticleSystem.Update`. |
| `Assets/Optimization/Scenes/OptimizeAfter.unity` | Validation fixture points at the final optimized prefab and benchmark runner. It is not in `EditorBuildSettings` and is not a shipped gameplay scene. | Provide an isolated final validation scene. | `final_health_check.json`, `final_promotion_validation.json`, and `final_live_render_validation.json`. `MEASURED`. | Final evidence captures. `OBSERVED`. | Scene transition and replay checks passed with zero missing scripts. `MEASURED`. | Requires Unity 2022.3 to rerun; should remain a validation fixture unless explicitly archived. |

`Docs/Optimization/OPTIMIZATION.md` is the authoritative implementation report, not production behavior. It records the history from Build B to promoted Build C, the trail geometry measurements, final replay evidence, and the limitations of Editor-only and supplied live-capture evidence.

No current production `ParticleSystemRendererConsolidator.cs` remains. The final implementation does not use the superseded Build B copy-and-consolidate path.

### Production evidence summary

- Trail optimization: `MEASURED` geometry reduction in controlled comparison frames; visual acceptability is `OBSERVED`, not universal pixel equivalence.
- Final rain architecture: `MEASURED` one live rain draw and `MEASURED` deterministic replay correctness; Android GPU and fill-rate impact are `NOT MEASURED`.
- CPU/memory: isolated Editor microbenchmark timings are `MEASURED` evidence for the benchmark operation; FPS, Unity Profiler `ParticleSystem.Update`, native memory, and Android frame time are `NOT MEASURED`.
- `maxParticles`: no production values were changed by the research pilot. Any max-particle conclusions beyond serialized inventory are `BLOCKED`.

## Evidence Inventory

### Keep

| File or group | Decision | Reason |
|---|---|---|
| `Docs/Optimization/OPTIMIZATION.md` | KEEP | Authoritative final implementation/evidence report; linked to the final evidence directory. |
| `Docs/Optimization/FinalEvidence/*` | KEEP | Final JSON validation, live counters, supplied Frame Debugger measurement, before/after geometry, draw-state, and rain captures. These are the final evidence set, not duplicate temporary outputs. |
| `Assets/Optimization/Prefabs/ParticleEffectsOptimized.prefab` and `.meta` | KEEP | Final implementation and required Unity asset identity. |
| `Assets/Optimization/Scenes/OptimizeBefore.unity` and `.meta` | KEEP | Untouched baseline fixture used for before/after comparison. |
| `Assets/Optimization/Scenes/OptimizeAfter.unity` and `.meta` | KEEP | Reproducible final validation fixture. |
| `Assets/Optimization/Scripts/DeterministicRainEmitter.cs` and `.meta` | KEEP | Final runtime replay support referenced by the optimized prefab. |
| `Assets/Optimization/Scripts/OptimizationBenchmarkRunner.cs` and `.meta` | KEEP | Final deterministic validation runner referenced by the optimization scenes. |
| `Docs/Optimization/MAX_PARTICLES_RESEARCH_REPORT.md` | KEEP | Research-only serialized maxParticles inventory; clearly records that runtime measurement is unavailable. |
| `Docs/Optimization/Experiments/MAX_PARTICLES_PILOT_REPORT.md` | KEEP | Records the blocked maxParticles pilot and prevents an unsupported production conclusion. |
| `Docs/Optimization/OVERDRAW_RESEARCH_REPORT.md` | KEEP | Research-only overdraw candidate ranking and measurement plan; no production change. |
| `Docs/Optimization/Experiments/OVERDRAW_PL3_LINES_PILOT_REPORT.md` | KEEP | Records the isolated A/B configuration and the blocked runtime result. |
| `Assets/Optimization/Experiments/Overdraw/*` | KEEP for now | Isolated A/B scenes/controller are reproducibility artifacts. Their scenes reference the optimized prefab and pilot script by GUID. |

### Move / archive

| File | Decision | Reason |
|---|---|---|
| `Docs/Optimization/TASK2_TECHNICAL_RESEARCH_REPORT.md` | MOVE after review | Useful historical reasoning, but its current-state sections still mention the deleted consolidator, rain3/rain4, “30 ParticleSystems in both prefabs,” and a provisional Build B conclusion. No external repository links were found, so it can be archived after its historical status is made explicit. |

### Conditional deletion candidates

No file is immediately safe to delete while the current reports still reference the MaxParticles pilot harness and while the overdraw pilot is retained as a reproducibility artifact.

The following are conditional cleanup candidates, not deletion actions taken in this audit:

| File/group | Condition before deletion | Current dependency result |
|---|---|---|
| `Assets/Optimization/Experiments/MaxParticles/MaxParticlesPilotExperiment.cs` | Update/archive `MAX_PARTICLES_PILOT_REPORT.md`, confirm the pilot will not be rerun, then repeat repository search. | No C# or Unity scene/GUID references found; documentation path references exist. No `.meta` files are present. |
| `Docs/Optimization/Archive/MaxParticlesPilotBatchMeasurement.cs` | Same condition as above. | No C# or Unity scene/GUID references found; documentation path references exist. No `.meta` files are present. |

There are no Build C runtime leftovers in `Assets/Optimization`; the old consolidator is already absent from the current tree. The historical Build B Frame Debugger image is part of final evidence and should not be treated as a duplicate.

## Blocked Experiments

| Item | Required for final documentation? | Required for reproducibility? | Temporary / duplicate / obsolete? | Dependency |
|---|---|---|---|---|
| MaxParticles pilot C# harnesses | Only while the pilot report describes the exact harness paths. | Useful only if the blocked pilot will be rerun. | Temporary; no result output was produced. Not duplicate. | Docs-only references; no scene/GUID/code references; missing `.meta`. |
| `MAX_PARTICLES_PILOT_REPORT.md` | Yes, as blocked research history. | Yes, if the pilot is revisited. | Not obsolete; its conclusion is `BLOCKED`. | References the harness paths. |
| Overdraw A/B scenes A/B | Yes, if the blocked experiment is part of the research record. | Yes; scenes encode the isolated variable and prefab GUID. | Not duplicate; intentionally isolated. | Each scene references the optimized prefab and pilot script; `.meta` files are present. |
| `OverdrawPL3LinesPilot.cs` and `.meta` | Yes, for rerunning the isolated experiment. | Yes. | Temporary research support, but not obsolete until the experiment is closed. | Referenced by both A/B scenes via GUID. |
| `OVERDRAW_PL3_LINES_PILOT_REPORT.md` | Yes, to document unavailable runtime validation. | Yes, for configuration and decision history. | Blocked, not duplicate. | Names both A/B scenes and the isolated variable. |
| Build C leftovers | No current leftover found. | No. | Already removed from production tree. | Historical documentation only. |
| Temporary benchmark JSON/screenshots/runners | Final JSON/screenshots are required evidence; no unclassified experiment output was found. | Final evidence is reproducibility/supporting evidence. | The final evidence set is not a duplicate set. | `OPTIMIZATION.md` links the final evidence files. |

## Dependency Audit

Repository-wide searches found the following relevant relationships:

| Asset/document | References found | Result |
|---|---|---|
| `ParticleEffectsOptimized.prefab` GUID `3e09cc39a930c164ba6eba58c4821bad` | `OptimizeAfter.unity`, both overdraw A/B scenes, and its `.meta` | Must not be deleted; required by validation and experiments. |
| `DeterministicRainEmitter.cs` GUID `5b198...` | Two component references in the optimized prefab and its `.meta` | Must remain with its `.meta`. |
| `OptimizationBenchmarkRunner.cs` GUID `31315496184d4ec9aa13b294e9f13179` | `OptimizeBefore.unity`, `OptimizeAfter.unity`, and its `.meta` | Must remain with its `.meta`. |
| `OverdrawPL3LinesPilot.cs` GUID `4a9d...` | Both overdraw A/B scenes and its `.meta` | Must remain while the pilot is retained. |
| Overdraw A/B scene GUIDs | Their own `.meta` files; no external scene/build references | No deletion recommendation until the report/archive decision is made. |
| `ParticleSystemRendererConsolidator` | Documentation/history only; no current source, prefab, scene, or meta reference | No current production dependency. Historical mentions need documentation cleanup. |
| `rain3` / `rain4` | Original prefab and historical evidence/docs only; no current optimized-prefab reference | Current final rain uses `rainCombined`; historical references are not live dependencies. |
| Optimization scenes | Not present in `ProjectSettings/EditorBuildSettings.asset` | They are validation fixtures, not shipped build scenes. |
| Final evidence files | Relative links from `OPTIMIZATION.md`; JSON paths also named by final reports | Keep linked evidence together. |
| Research report filenames | No README or external documentation links found; most references are internal to the reports | Archive/move only after updating internal links/text. |

The optimized prefab's current Git object matches `HEAD`, and its tracked working-tree content has no diff. The current untracked work is research/evidence material, not a production prefab modification.

## Documentation Consistency

| Document | Audit result | Required action |
|---|---|---|
| `OPTIMIZATION.md` | Consistent with the promoted final prefab, final emitter, final runner, and evidence. Explicitly limits Android/GPU claims. | KEEP authoritative. |
| `TASK2_TECHNICAL_RESEARCH_REPORT.md` | Remediated: current-state inventory now describes 28 final systems, `rainCombined`, `DeterministicRainEmitter`, and the superseded Build B helper as historical. Final claims are qualified with evidence status. | KEEP as historical research; do not treat its explicitly historical sections as current production architecture. |
| `MAX_PARTICLES_RESEARCH_REPORT.md` | Remediated: serialized `dieWithParticles: 0` is now stated correctly, and possible capacity/memory impact is labeled `EXPECTED` / `NOT MEASURED`. | KEEP with the blocked runtime limitation. |
| `MAX_PARTICLES_PILOT_REPORT.md` | Consistently `BLOCKED`; no runtime, memory, CPU, rendering, or visual result claimed. | KEEP as blocked evidence; update harness-path references if harnesses are later removed. |
| `OVERDRAW_RESEARCH_REPORT.md` | Consistently research-only; identifies PL3 lines as a candidate and does not claim an overdraw result. | KEEP. |
| `OVERDRAW_PL3_LINES_PILOT_REPORT.md` | Consistently `BLOCKED`; A/B values are statically verified, all runtime/visual/performance metrics are `NOT MEASURED`. | KEEP and repeat only when Unity 2022.3 runtime/GPU access exists. |

The previously identified stale technical report and `dieWithParticles` sentence have been corrected. No missing evidence should be filled by inference.

## Claim Audit

| Claim | Evidence | Status | Safe to say in interview? |
|---|---|---|---|
| Rain Draw Calls 2→1 | Before/after supplied Frame Debugger captures and final live rain measurement: one `rainCombined` event. | `MEASURED` | YES, qualified as Editor/supplied live capture; not a mobile FPS claim. |
| PL1/PL2/PL3 trail geometry reduction | Controlled before/after geometry measurements in `OPTIMIZATION.md` and evidence captures. | `MEASURED` | YES, with the measured frame/phase qualification. |
| PL3 trail triangles reduced 45.4% and vertices 40.4% | Calculated from measured 410→224 triangles and 460→274 vertices. | `CALCULATED` from `MEASURED` values | YES, as geometry reduction, not GPU improvement. |
| Rain geometry remained 16 vertices, 24 indices, 8 triangles | Supplied live Frame Debugger measurement. | `MEASURED` | YES, qualified to the captured rain event. |
| Final PL2/PL3 whole counters are 8/8/8 and 8/8/7 | Final live validation JSON. | `MEASURED` | YES, as phase-specific Editor counters. |
| Deterministic replay correctness | Final promotion JSON and health-check JSON: fixed/general replay passes, correct counts, no stale state/exceptions, no missing scripts. | `MEASURED` | YES. |
| Rain visual equivalence | Fixed-phase images and pixel comparison described in `OPTIMIZATION.md`; seven of eight fixed A/C captures were pixel-identical and one difference was negligible. | `OBSERVED` plus limited `MEASURED` image comparison | YES only for the qualified rain comparison; do not generalize to all VFX. |
| Full final-VFX visual equivalence | No complete pixel comparison of every effect/state. | `UNSUPPORTED` / `NOT MEASURED` | NO. |
| CPU improvement on Android or FPS improvement | Editor microbenchmarks only; `ParticleSystem.Update`, device CPU time, and FPS were not isolated. | `UNSUPPORTED` / `NOT MEASURED` | NO. |
| Build C isolated emission operation was faster than Build B transfer | 10,000-call Editor microbenchmark: 0.873 µs vs 4.165 µs. | `MEASURED` Editor microbenchmark | YES only with the Editor-microbenchmark qualifier. |
| Memory improvement in shipped runtime | Build B helper payload of 800,000 bytes is `CALCULATED`; no Memory Profiler snapshot or Android native-memory measurement. | `CALCULATED` only; runtime claim `UNSUPPORTED` | NO as a mobile memory claim. |
| GPU or fill-rate improvement | No GPU timing, overdraw numeric measurement, or Android profiling. | `UNSUPPORTED` / `NOT MEASURED` | NO. |
| 60 FPS or thermal improvement on Android | No connected Android validation or device profiling. | `UNSUPPORTED` / `NOT MEASURED` | NO. |
| maxParticles optimization benefit | Pilot was blocked; no production values changed. | `BLOCKED` / `NOT MEASURED` | NO. |
| PL3 trail lifetime 0.5→0.35 improves overdraw | A/B scenes were created, but runtime and GPU validation were unavailable. | `BLOCKED` / `NOT MEASURED` | NO. |

## Cleanup Recommendations

1. Keep the final production prefab, its `.meta`, the deterministic emitter, benchmark runner, optimization scenes, final evidence, `OPTIMIZATION.md`, and the blocked research reports.
2. Retain the overdraw A/B scenes/controller until the blocked experiment is explicitly closed or rerun. They are isolated and have Unity GUID dependencies.
3. The MaxParticles harnesses are the only plausible deletion candidates, but not immediately: their report still references them. Archive/update the report, confirm no rerun is required, then rerun the repository dependency search before deletion.
4. Do not delete the historical Build B image; it supports the documented superseded architecture and is not duplicate final evidence.
5. Do not add Android, GPU, fill-rate, FPS, native-memory, or full visual-equivalence claims during cleanup.

## Pre-Commit Checklist

- [x] Production optimized prefab is unchanged during this audit.
- [x] Original unoptimized source remains untouched.
- [x] No production particle settings were changed during this audit.
- [x] No files were deleted.
- [x] No files were staged or committed.
- [x] Final evidence and blocked experiments were inventoried.
- [x] Unity scene, prefab, script, documentation, and GUID dependencies were searched.
- [x] Resolve the stale current-state statements in the technical research report and mark Build B material historical.
- [x] Correct the serialized `dieWithParticles` documentation inconsistency and qualify maxParticles impact.
- [x] Decide whether to retain or archive the isolated blocked experiments.
- [ ] If deleting MaxParticles harnesses, remove/update report path references and repeat dependency checks first.
- [ ] Re-run appropriate Unity 2022.3 validation after documentation/cleanup changes.
- [ ] Obtain Android GPU/frame-time evidence before making any mobile performance claim.
- [ ] Review `git status --short` and `git diff --stat` immediately before staging in a later, explicitly authorized cleanup step.

### Git status at audit time

`git status --short`:

```text
?? Assets/Optimization/Experiments/
?? Docs/Optimization/Experiments/
?? Docs/Optimization/MAX_PARTICLES_RESEARCH_REPORT.md
?? Docs/Optimization/OVERDRAW_RESEARCH_REPORT.md
```

`git diff --stat`:

```text
(empty; current changes are untracked)
```

## Final Audit Verdict

Task 2 is **READY FOR A SEPARATE AUTHORIZED CLEANUP PASS**. The retention
decision is now explicit, but no deletion, archive move, staging, or commit was
performed in this audit. GPU/mobile performance and overdraw claims remain
`NOT MEASURED`.

## Remediation Status

### Documentation fixes completed

- Updated `TASK2_TECHNICAL_RESEARCH_REPORT.md` to describe the current 28-system final prefab, `rainCombined`, `DeterministicRainEmitter`, and the promoted one-system rain architecture.
- Marked Build B consolidator details, copy buffers, and transfer guards as historical/superseded rather than current production architecture.
- Preserved measured trail geometry, rain Draw Call, Frame Debugger, deterministic replay, and Unity health-check evidence.
- Updated current-state tables, the rain design comparison, candidate backlog, final verdict, and key answers to distinguish final evidence from historical research.

### Unsupported claims removed or qualified

- Android GPU/FPS, fill-rate, native-memory, thermal, and full-VFX visual-equivalence claims remain explicitly `NOT MEASURED` or unsupported.
- maxParticles is documented as a research hypothesis only: possible capacity cleanup is `EXPECTED`; runtime memory/CPU/GPU benefit is `NOT MEASURED`; the pilot remains `BLOCKED`.
- PL3 Trail Lifetime `0.5 -> 0.35` remains an isolated research hypothesis and is explicitly `BLOCKED — RUNTIME VALIDATION UNAVAILABLE`.
- Editor microbenchmarks remain limited to their measured isolated operations and are not presented as Android frame-time or FPS evidence.

### dieWithParticles correction

`MAX_PARTICLES_RESEARCH_REPORT.md` now records `dieWithParticles: 0` for the enabled trail modules based on serialized prefab inspection. The report states that trail history may remain until trail lifetime expires. This is `MEASURED` serialized configuration evidence; runtime trail disappearance timing remains `NOT MEASURED`.

### Remaining cleanup blockers

- Before archiving the MaxParticles editor runner, update its report path to the archived location or explicitly mark the runner as no longer retained.
- Before deleting the MaxParticles runtime harness, repeat the repository dependency search; it currently has no code, scene, GUID, or documentation dependency.
- Keep Android validation status as `ANDROID VALIDATION = NOT AVAILABLE`; no mobile GPU/performance claim is permitted.
- No experiment files were deleted and no production assets were modified in this remediation.

### Git state after remediation

`git status --short`:

```text
 M Docs/Optimization/TASK2_TECHNICAL_RESEARCH_REPORT.md
?? Assets/Optimization/Experiments/
?? Docs/Optimization/Experiments/
?? Docs/Optimization/MAX_PARTICLES_RESEARCH_REPORT.md
?? Docs/Optimization/OVERDRAW_RESEARCH_REPORT.md
?? Docs/Optimization/TASK2_FINAL_AUDIT.md
```

`git diff --stat` contains only the tracked technical-report remediation:

```text
.../TASK2_TECHNICAL_RESEARCH_REPORT.md | 146 +++++++++++++--------
1 file changed, 92 insertions(+), 54 deletions(-)
```

## Final Retention Decision

No files were deleted or moved. This section records the cleanup decision for a
later explicitly authorized pass.

Scope: **12 experiment-related files reviewed** — 10 files under the requested
experiment directories plus the two root research reports that govern them.
The existing `Docs/Optimization/FinalEvidence/*` set is outside this count and
remains `KEEP-FINAL-EVIDENCE`.

| Path | Classification | Dependency | Reason |
|---|---|---|---|
| `Docs/Optimization/MAX_PARTICLES_RESEARCH_REPORT.md` | `KEEP-RESEARCH` | No external dependency; referenced by the audit only. | Important serialized inventory and explicit `NOT MEASURED`/`BLOCKED` conclusion. |
| `Docs/Optimization/Experiments/MAX_PARTICLES_PILOT_REPORT.md` | `KEEP-RESEARCH` | References the editor batch runner path; no Unity/GUID dependency. | Required blocked research history and exact A/B configuration. |
| `Assets/Optimization/Experiments/MaxParticles/MaxParticlesPilotExperiment.cs` | `DELETE-CANDIDATE` | No `.meta`, scene, GUID, C# call site, or report path reference found. | Abandoned runtime harness; the documented pilot used the editor batch runner and produced no runtime result. |
| `Docs/Optimization/Archive/MaxParticlesPilotBatchMeasurement.cs` | `ARCHIVE-CANDIDATE` | Referenced by `MAX_PARTICLES_PILOT_REPORT.md`; loads the optimized prefab by path and writes a future root-level `MaxParticlesPilotResult.txt`. No `.meta` or GUID dependency. | Temporary runner, but the only executable artifact that could reproduce the blocked 120-replay pilot. Archive or remove only after updating the report reference. |
| `Docs/Optimization/OVERDRAW_RESEARCH_REPORT.md` | `KEEP-RESEARCH` | No external dependency; referenced by audit/history. | Defines the overdraw hypothesis, measurement methods, and explicit `ANDROID VALIDATION = NOT AVAILABLE` status. |
| `Docs/Optimization/Experiments/OVERDRAW_PL3_LINES_PILOT_REPORT.md` | `KEEP-RESEARCH` | Names both A/B scenes and the tested values. | Required blocked A/B decision history; all runtime results remain `NOT MEASURED`. |
| `Assets/Optimization/Experiments/Overdraw/OverdrawPL3Lines_A.unity` | `KEEP-RESEARCH` | References optimized prefab GUID `3e09cc39a930c164ba6eba58c4821bad` and pilot script GUID `4a9de8d7f4e846a8a4d07c6e4a2f9b31`; has its own GUID `8c6c41a9c5464c8a9cde2bf5a07b9e11`. | Reproducible Build A scene with Trail Lifetime `0.5`. |
| `Assets/Optimization/Experiments/Overdraw/OverdrawPL3Lines_A.unity.meta` | `KEEP-RESEARCH` | Supplies the A scene GUID referenced by Unity asset identity. | Required companion metadata; deleting it breaks the scene asset identity. |
| `Assets/Optimization/Experiments/Overdraw/OverdrawPL3Lines_B.unity` | `KEEP-RESEARCH` | References the same optimized prefab and pilot script GUIDs; has its own GUID `93a7a90dc9f9415c8a2b07ef4de63d22`. | Reproducible Build B scene with only Trail Lifetime `0.35`. |
| `Assets/Optimization/Experiments/Overdraw/OverdrawPL3Lines_B.unity.meta` | `KEEP-RESEARCH` | Supplies the B scene GUID referenced by Unity asset identity. | Required companion metadata; deleting it breaks the scene asset identity. |
| `Assets/Optimization/Experiments/Overdraw/OverdrawPL3LinesPilot.cs` | `KEEP-RESEARCH` | GUID `4a9de8d7f4e846a8a4d07c6e4a2f9b31` is referenced by both A/B scenes; it also references `DeterministicRainEmitter` in production code. | Required to rerun the isolated A/B scenes and enforce MVD `0.4` without editing production assets. |
| `Assets/Optimization/Experiments/Overdraw/OverdrawPL3LinesPilot.cs.meta` | `KEEP-RESEARCH` | Supplies the script GUID referenced by both A/B scenes. | Required companion metadata. |

### Folder classifications

| Folder | Classification | Reason |
|---|---|---|
| `Assets/Optimization/Experiments/` | `KEEP-RESEARCH` container | Contains the reproducible Overdraw experiment; do not remove while that experiment is retained. |
| `Assets/Optimization/Experiments/MaxParticles/` | `ARCHIVE-CANDIDATE` container | Contains only temporary MaxParticles harnesses; no folder or file `.meta` exists. |
| `Assets/Optimization/Experiments/MaxParticles/Editor/` | `ARCHIVE-CANDIDATE` container | Contains only the temporary editor batch runner. |
| `Assets/Optimization/Experiments/Overdraw/` | `KEEP-RESEARCH` container | Contains the two GUID-linked A/B scenes, pilot script, and their metadata. |
| `Docs/Optimization/Experiments/` | `KEEP-RESEARCH` container | Contains the two blocked pilot reports. |

### MaxParticles dependency result

- `MAX_PARTICLES_RESEARCH_REPORT.md` is a standalone research report; no
  production, scene, script, GUID, JSON, or screenshot dependency was found.
- `MAX_PARTICLES_PILOT_REPORT.md` is required for the blocked research record
  and references only the editor batch runner path.
- No MaxParticles experiment scenes exist.
- No MaxParticles JSON result, screenshot, or other generated evidence exists.
- No MaxParticles `.meta` files exist for either harness, its folders, or the
  parent experiment folder.
- `MaxParticlesPilotExperiment.cs` is an unreferenced temporary runtime
  harness and is the only `DELETE-CANDIDATE`.
- `MaxParticlesPilotBatchMeasurement.cs` is the only reproducibility runner;
  it is an `ARCHIVE-CANDIDATE` until its report reference is resolved.

### Overdraw dependency result

- Both A/B scenes reference the optimized prefab and the pilot script by GUID.
- Both scenes are intentionally isolated and are not in
  `ProjectSettings/EditorBuildSettings.asset`.
- The pilot script `.meta` and both scene `.meta` files are required Unity
  identity dependencies.
- The overdraw reports have no generated screenshots, JSON, or GPU captures to
  preserve; their value is the research decision and reproducible setup.
- No production prefab, scene, script, or gameplay reference points to the
  Overdraw experiment.

### Retention lists

KEEP:

- `Docs/Optimization/MAX_PARTICLES_RESEARCH_REPORT.md`
- `Docs/Optimization/Experiments/MAX_PARTICLES_PILOT_REPORT.md`
- `Docs/Optimization/OVERDRAW_RESEARCH_REPORT.md`
- `Docs/Optimization/Experiments/OVERDRAW_PL3_LINES_PILOT_REPORT.md`
- All six files under `Assets/Optimization/Experiments/Overdraw/`

ARCHIVE:

- `Docs/Optimization/Archive/MaxParticlesPilotBatchMeasurement.cs`
- Its `MaxParticles/` and `MaxParticles/Editor/` containers, after the report
  reference is updated or the runner is deliberately retained in an archive.

DELETE:

- `Assets/Optimization/Experiments/MaxParticles/MaxParticlesPilotExperiment.cs`
  (later, after one final dependency check; no deletion performed now).

KEEP-FINAL-EVIDENCE:

- None of the 12 experiment-related files is final production evidence. The
  separate `Docs/Optimization/FinalEvidence/*` set remains retained.
