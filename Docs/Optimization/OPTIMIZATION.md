# Task 2 — UI/VFX Optimization

## Objective

Profile the inherited particle-effect package, reduce measurable rendering cost without weakening the intended image, and document only changes supported by repeatable Unity Profiler, Frame Debugger, benchmark, and visual evidence.

The imported source prefab remains unchanged. The final comparison uses:

- Before: `Assets/Optimization/Scenes/OptimizeBefore.unity` with `ParticleEffectsUnoptimize.prefab`.
- After: `Assets/Optimization/Scenes/OptimizeAfter.unity` with `ParticleEffectsOptimized.prefab`.

## Test Setup

| Item | Configuration |
| --- | --- |
| Unity | 2022.3.62f2 |
| Render pipeline | Built-in Render Pipeline |
| Resolution | 1080 × 1920 |
| Camera | Same orthographic camera in Before and After scenes |
| Replay harness | `OptimizationBenchmarkRunner` |
| Random seed | 1337 |
| Primary workload | `PerfectLevel3` (PL3) |
| Measurement tools | Unity Profiler and Frame Debugger in the Editor |

The runner stops and clears all four effects, assigns deterministic seeds in a stable hierarchy order, and plays only the selected variant. The two scenes share the same benchmark configuration and differ in the particle prefab and corresponding root references.

The captured Draw Call frame and captured geometry frame are intentionally separate samples. Particle trails change geometry over their lifetime, so the highest/comparable geometry sample does not necessarily occur on the selected Draw Call frame. The report never combines those counters as though they came from one frame.

These are controlled Editor measurements. They are not Android-device performance or 60 FPS claims.

## Before

The initial four-variant rendering baseline was:

| Variant | Draw Calls | Batches | SetPass Calls |
| --- | ---: | ---: | ---: |
| Transition | 6 | 6 | 6 |
| PerfectLevel1 (PL1) | 7 | 7 | 7 |
| PerfectLevel2 (PL2) | 9 | 8 | 8 |
| PerfectLevel3 (PL3) | 9 | 8 | 7 |

PL3 was the primary workload because it tied for the highest Draw Call count and produced the heaviest observed trail geometry.

![Before — PL3 Draw Calls](FinalEvidence/before_pl3_draw_calls.png)

The selected baseline Profiler frame shows **9 Draw Calls, 8 Batches, and 7 SetPass Calls**. Its 320 triangles and 374 vertices belong only to that selected frame.

![Before — PL3 geometry](FinalEvidence/before_pl3_geometry.png)

The dedicated baseline geometry frame shows **410 triangles and 460 vertices**. These are the authoritative geometry values used in the final PL3 comparison.

## Issues Found

- **Dense trail tessellation:** the `lines` trails generated more vertices than were visually necessary.
- **Two compatible rain renderer submissions:** PL2 and PL3 each used mirrored `rain3` and `rain4` simulations whose output could be submitted through one renderer while preserving both sides.
- **Avoidable particle-read work:** the first consolidation implementation continued calling `GetParticles` after its one-shot transfer was complete.
- **Unsafe trail consolidation candidate:** the Transition `zap1` and `zap2` systems looked structurally similar, but joining their independent trail histories produced invalid connecting segments.
- **Material sharing was insufficient:** mask normalization and shared material/atlas experiments did not reduce the measured ParticleSystem submissions.

## Changes Made

### Trail Geometry Optimization

For the `lines` trail in PL1, PL2, and PL3:

```text
Trails > Minimum Vertex Distance: 0.2 -> 0.4
```

A larger minimum distance makes Unity add trail vertices less frequently, reducing trail geometry without changing the emitter, material, timing, or lifetime. Fixed-phase visual comparisons and repeated deterministic playback found `0.4` to be an acceptable visual/performance compromise; pushing this setting too high would make the trail visibly coarse.

### Rain Renderer Consolidation

PL2 and PL3 contain mirrored `rain3` and `rain4` systems. Both simulations remain active with their original transforms, shape rotations, timing, modules, and deterministic seeds. In the optimized prefab, the `rain4` renderer is disabled and `ParticleSystemRendererConsolidator` transfers its emitted particles into `rain3` in the correct simulation space.

This preserves both mirrored sides while removing one compatible renderer submission. It is a targeted optimization, not a claim that arbitrary ParticleSystems can be merged safely.

![Before — PL3 rain renderer group](FinalEvidence/before_pl3_rain_frame_debugger.png)

The baseline Frame Debugger event shows the rain group using **2 Draw Calls, 16 vertices, and 24 indices**.

![After — PL3 consolidated rain renderer](FinalEvidence/after_pl3_rain_frame_debugger.png)

The optimized equivalent shows **1 Draw Call, 16 vertices, and 24 indices**. The Frame Debugger `Camera.Render` tree count is not used as the overall Profiler Draw Call result.

### Runtime Particle Transfer / Allocation Cleanup

`ParticleSystemRendererConsolidator`:

- caches its two serialized `ParticleSystem` references;
- allocates fixed particle and seed buffers once in `Awake`;
- uses a `transferComplete` guard for the one-shot transfer;
- performs no LINQ, hierarchy search, `Instantiate`, or `Destroy` in its hot path;
- resets transfer state when the source stops so deterministic replay remains valid.

The final helper keeps GPU submission metrics and CPU/API-call measurements conceptually separate.

## Results

### PL1

PL1 retains the trail setting change only; it has no mirrored rain consolidation.

| Metric | Before | After | Change | Source |
| --- | ---: | ---: | ---: | --- |
| Draw Calls | 7 | 7 | 0.0% | Deterministic Editor benchmark |
| Batches | 7 | 7 | 0.0% | Deterministic Editor benchmark |
| SetPass Calls | 7 | 7 | 0.0% | Deterministic Editor benchmark |
| Triangles | 126 | 86 | **31.7% reduction** | Controlled trail comparison window |
| Vertices | 164 | 124 | **24.4% reduction** | Controlled trail comparison window |

No PL1 Draw Call reduction is claimed.

### PL2

PL2 retains both the trail setting and mirrored-rain renderer consolidation.

| Metric | Before | After | Change | Source |
| --- | ---: | ---: | ---: | --- |
| Draw Calls | 9 | 8 | **11.1% reduction** | Deterministic Editor benchmark |
| Batches | 8 | 8 | 0.0% | Deterministic Editor benchmark |
| SetPass Calls | 8 | 8 | 0.0% | Deterministic Editor benchmark |
| Triangles | 196 | 156 | **20.4% reduction** | Controlled trail comparison window |
| Vertices | 240 | 200 | **16.7% reduction** | Controlled trail comparison window |

### PL3

PL3 is the heaviest/final comparison and retains both optimizations.

| Metric | Before | After | Change | Source |
| --- | ---: | ---: | ---: | --- |
| Draw Calls | 9 | 8 | **11.1% reduction** | Final Profiler Draw Call screenshots |
| Batches | 8 | 8 | 0.0% | Final Profiler Draw Call screenshots |
| SetPass Calls | 7 | 7 | 0.0% | Final Profiler Draw Call screenshots |
| Triangles | 410 | 224 | **45.4% reduction** | Final dedicated geometry screenshots |
| Vertices | 460 | 274 | **40.4% reduction** | Final dedicated geometry screenshots |

## Final Before / After

| Phase | Draw Calls | Triangles | Vertices | Main retained change |
| --- | ---: | ---: | ---: | --- |
| PL1 Before | 7 | 126 | 164 | Baseline |
| PL1 After | 7 | 86 | 124 | Trail tessellation |
| PL2 Before | 9 | 196 | 240 | Baseline |
| PL2 After | 8 | 156 | 200 | Rain consolidation + trail tessellation |
| PL3 Before | 9 | 410 | 460 | Baseline; metrics sampled in separate final evidence frames |
| PL3 After | 8 | 224 | 274 | Rain consolidation + trail tessellation; metrics sampled in separate final evidence frames |

![After — PL3 Draw Calls](FinalEvidence/after_pl3_draw_calls.png)

The final Draw Call frame shows **8 Draw Calls, 8 Batches, and 7 SetPass Calls**. Its 216 triangles and 270 vertices are specific to that selected frame and are not substituted into the geometry comparison.

![After — PL3 geometry](FinalEvidence/after_pl3_geometry.png)

The dedicated final geometry frame shows **224 triangles and 274 vertices**.

The final screenshot-supported PL3 result is therefore:

- Draw Calls: **9 -> 8** (**11.1% reduction**).
- Triangles: **410 -> 224** (**45.4% reduction**).
- Vertices: **460 -> 274** (**40.4% reduction**).

## GetParticles and GC Validation

The following values describe CPU/API work inside the consolidation helper. **They are not Draw Calls.**

Across five deterministic replays:

| Variant | Source `GetParticles` calls before guard | After guard | Reduction |
| --- | ---: | ---: | ---: |
| PL2 | 235 | 5 | 97.9% |
| PL3 | 234 | 5 | 97.9% |

The same validation recorded five target reads and five target writes for each variant: one completed transfer per replay. Ten rapid replays passed without stale particles, duplicate transfers, accumulation, transform drift, or a missing mirrored side.

Unity Editor profiling attributed **0 B recurring managed allocation** to the tested steady-state and transfer hot path. This wording does not include the intentional initialization arrays allocated once in `Awake`, and it is not a general “zero allocations” claim for the scene or player build.

## Rejected Optimization and Trade-offs

### Transition Zap Consolidation — Rejected

Combining `zap1` and `zap2` reduced Transition from 6 to 4 Draw Calls, but it joined independent trail histories with incorrect horizontal links. The change was reverted. The final optimized prefab keeps Transition at 6 Draw Calls and contains no zap consolidator.

### Other Rejected Experiments

- Mask-interaction normalization did not change Draw Calls, Batches, or SetPass Calls.
- A shared material/atlas prototype did not reduce measured submissions and added configuration complexity.
- Disabling `init`, `glow`, or `outline_line` reduced a submission but visibly weakened impact, color, or silhouette.

These experiments reinforced the acceptance rule: a lower counter is not a valid optimization when the visual result is wrong or the added complexity has no measured benefit.

## Validation

- `OptimizeBefore.unity` references the untouched imported prefab; `OptimizeAfter.unity` references the optimized duplicate.
- Both scenes use the same camera, resolution context, runner configuration, selected PL3 variant, and random seed.
- The benchmark runner caches variant hierarchies and assigns deterministic seeds before playback.
- The optimized prefab serializes Minimum Vertex Distance `0.4` for PL1, PL2, and PL3 `lines` trails.
- Only PL2 and PL3 contain the retained rain consolidation components.
- All six final evidence files below exist and match the values stated in this report.
- Android-device profiling remains pending; no Android CPU, GPU, or frame-rate claim is made.

### Final Evidence Inventory

| File | State / source | Visible values | Supported claim |
| --- | --- | --- | --- |
| `FinalEvidence/before_pl3_draw_calls.png` | Before / Unity Profiler | 9 Draw Calls, 8 Batches, 7 SetPass; 320 triangles, 374 vertices in selected frame | Overall PL3 baseline Draw Calls |
| `FinalEvidence/before_pl3_geometry.png` | Before / Unity Profiler | 410 triangles, 460 vertices; selected frame also shows 8 Draw Calls, 7 Batches, 7 SetPass | Dedicated PL3 baseline geometry |
| `FinalEvidence/before_pl3_rain_frame_debugger.png` | Before / Frame Debugger | 2 Draw Calls, 16 vertices, 24 indices | Baseline rain renderer group |
| `FinalEvidence/after_pl3_draw_calls.png` | After / Unity Profiler | 8 Draw Calls, 8 Batches, 7 SetPass; 216 triangles, 270 vertices in selected frame | Final PL3 Draw Calls |
| `FinalEvidence/after_pl3_geometry.png` | After / Unity Profiler | 224 triangles, 274 vertices; selected frame also shows 7 Draw Calls, 7 Batches, 7 SetPass | Dedicated PL3 final geometry |
| `FinalEvidence/after_pl3_rain_frame_debugger.png` | After / Frame Debugger | 1 Draw Call, 16 vertices, 24 indices | Consolidated rain renderer group |

## Learnings

- Draw Calls, Batches, SetPass Calls, geometry, CPU API calls, and managed allocations are separate metrics; improving one does not imply that all improved.
- Frame Debugger is essential for mapping aggregate Profiler counters to the renderer submissions that caused them.
- Trail-heavy effects can benefit substantially from topology tuning even when Draw Calls do not change.
- Renderer consolidation is safe only when simulation, renderer compatibility, and visual equivalence are validated; independent trail histories are a concrete counterexample.
- Profiling should drive optimization decisions, and visual correctness must be checked after every numerical improvement.

## AI Usage

AI assistance was used to review profiler findings, organize optimization hypotheses, inspect serialized Unity configuration, review the consolidation code, recalculate metrics, and audit this report. Final changes and claims were accepted only after validation against Unity Profiler, Frame Debugger, deterministic replay, serialized assets, and manual visual evidence.
