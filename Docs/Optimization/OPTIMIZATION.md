# VFX Optimization Report

## 1. Goal

The goal was to profile the inherited particle effects, reduce measurable rendering cost where visual correctness allowed it, and retain only changes supported by repeatable evidence. The imported source prefab remains unchanged. `OptimizeBefore.unity` reproduces the original configuration, while `OptimizeAfter.unity` uses a separate optimized prefab.

## 2. Profiling Baseline

All four variants were profiled in Unity 2022.3.62f2 using the Built-in Render Pipeline at 1080 x 1920. The benchmark runner replays each effect with a fixed seed so that comparisons use deterministic simulation. Unity Profiler provided the overall rendering counters, and Frame Debugger identified the individual ParticleSystem submissions behind them.

| Variant | Draw Calls | Batches | SetPass |
| --- | ---: | ---: | ---: |
| Transition | 6 | 6 | 6 |
| PerfectLevel1 | 7 | 7 | 7 |
| PerfectLevel2 | 9 | 8 | 8 |
| PerfectLevel3 | 9 | 8 | 7 |

PerfectLevel3 was selected as the primary investigation workload because it had one of the highest Draw Call counts and the heaviest observed trail geometry.

![PerfectLevel3 baseline Profiler draw-call evidence](FinalEvidence/before_pl3_draw_calls.png)

The baseline PerfectLevel3 Profiler frame records 9 Draw Calls, 8 Batches, and 7 SetPass Calls. Its 320 triangles and 374 vertices are specific to that draw-call evidence frame and are not used for the geometry comparison.

![PerfectLevel3 baseline Profiler geometry evidence](FinalEvidence/before_pl3_geometry.png)

The selected baseline geometry comparison frame records 410 triangles and 460 vertices. These manually captured values are the authoritative baseline for the final geometry comparison.

## 3. Investigation and Decisions

Frame Debugger showed that the rendering cost was distributed across separate transparent ParticleSystem submissions. Low-risk material and batching approaches were tested first. Normalizing mask state, sharing compatible materials, and testing an atlas did not reduce Draw Calls. Disabling visual layers did remove submissions, but it also weakened the impact, glow, or silhouette, so those changes were rejected.

Transition contained two similar zap systems. Consolidating `zap1` and `zap2` reduced Draw Calls from 6 to 4, but merging their independent trail histories created incorrect horizontal connections. The numerical improvement did not justify invalid trail topology, so the experiment was reverted and Transition remains at 6 Draw Calls.

PerfectLevel2 and PerfectLevel3 contained a useful structural opportunity: mirrored `rain3` and `rain4` simulations with compatible output. Both simulations were preserved, but their final particles were consolidated through the `rain3` renderer. This removed one renderer submission in each variant without removing either mirrored side.

![PerfectLevel3 rain submissions before consolidation](FinalEvidence/before_pl3_rain_frame_debugger.png)

Before consolidation, the selected rain event required 2 Draw Calls for 16 vertices and 24 indices. The Frame Debugger `Camera.Render` count is not used as the overall metric; the overall baseline remains the Profiler value of 9 Draw Calls.

![PerfectLevel3 rain submission after consolidation](FinalEvidence/after_pl3_rain_frame_debugger.png)

After consolidation, the equivalent `rain3` output uses 1 renderer Draw Call for the same 16 vertices and 24 indices.

Finally, trail tessellation was reviewed across variants. Increasing `lines` Trail Minimum Vertex Distance from 0.2 to 0.4 reduced generated geometry while preserving the intended appearance. It was validated and retained for PerfectLevel1, PerfectLevel2, and PerfectLevel3.

## 4. Changes Retained

PerfectLevel1 retains only the `lines` Trail Minimum Vertex Distance change from 0.2 to 0.4. In its controlled comparison window, triangles changed from 126 to 86, approximately 32%, and vertices changed from 164 to 124, approximately 24%. Its draw-state result remains 7 Draw Calls, 7 Batches, and 7 SetPass Calls; no Draw Call reduction is claimed.

PerfectLevel2 retains mirrored-rain renderer consolidation and the same trail setting change. Draw Calls changed from 9 to 8. Batches and SetPass remain 8. In its controlled trail comparison, triangles changed from 196 to 156, approximately 20%, and vertices changed from 240 to 200, approximately 17%.

PerfectLevel3 retains mirrored-rain renderer consolidation and the trail setting change. Draw Calls changed from 9 to 8, while Batches remain 8 and SetPass remains 7. Its final geometry result is documented from the dedicated before-and-after screenshot frames in Section 6.

Transition retains no optimization. The rejected zap consolidation is not present in the final prefab.

## 5. Implementation Validation

`ParticleSystemRendererConsolidator` uses fixed buffers allocated in `Awake`, cached references, and no LINQ or hierarchy search in its hot path. It does not call `Instantiate` or `Destroy`. Editor validation attributed 0 B recurring managed GC to the validated runtime path. Deterministic replay preserved both mirrored sides.

The audit found a concrete CPU inefficiency: source particles continued to be read after the one-shot transfer completed. A `transferComplete` guard reduced source `GetParticles` calls over five replays from 235 to 5 for PerfectLevel2 and from 234 to 5 for PerfectLevel3. The final behavior is effectively one source read, one target read, and one target `SetParticles` call per playback. Ten rapid replays passed for both variants, and Editor profiling showed small, bounded overhead. No Android CPU claim is made.

## 6. Final Result

| Variant | Before Draw Calls | After Draw Calls | Retained Optimization |
| --- | ---: | ---: | --- |
| Transition | 6 | 6 | None |
| PerfectLevel1 | 7 | 7 | Trail tessellation |
| PerfectLevel2 | 9 | 8 | Renderer consolidation + trail tessellation |
| PerfectLevel3 | 9 | 8 | Renderer consolidation + trail tessellation |

![PerfectLevel3 final Profiler draw-call evidence](FinalEvidence/after_pl3_draw_calls.png)

The final PerfectLevel3 Profiler frame records 8 Draw Calls, 8 Batches, and 7 SetPass Calls. Its 216 triangles and 270 vertices are specific to that draw-call evidence frame and are not used for the geometry comparison.

![PerfectLevel3 final Profiler geometry evidence](FinalEvidence/after_pl3_geometry.png)

The selected final geometry comparison frame records 224 triangles and 274 vertices.

PerfectLevel3 geometry comparison:

- Triangles: 410 -> 224, approximately 45.4% reduction.
- Vertices: 460 -> 274, approximately 40.4% reduction.

These are the final documented values from the manually captured submission evidence.

## 7. Learnings

Material sharing does not automatically reduce ParticleSystem Draw Calls. Frame Debugger was necessary to identify the actual submissions behind aggregate Profiler counters. A numerical improvement is not acceptable when visual correctness degrades, and trails impose topology constraints that can invalidate otherwise plausible consolidation. Custom renderer consolidation must also be checked for CPU and GC overhead after the rendering benefit is established.

## 8. AI Usage

AI helped inspect serialized ParticleSystem configuration, organize hypotheses, compare profiling results, and review the consolidator code. Actual measurements and retained decisions were validated in Unity using Profiler, Frame Debugger, deterministic replay, and manual visual inspection.

## 9. Remaining Validation

Android device validation remains pending. This report does not claim Android 60 FPS or Android CPU performance.
