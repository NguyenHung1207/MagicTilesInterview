# VFX Optimization Report

## 1. Goal

The goal was to profile the inherited particle effects, identify measurable rendering overhead, reduce Draw Calls where the visual design allowed it, and avoid changes that weakened the effects. I kept the imported prefab as an untouched baseline and applied retained changes only to an optimized duplicate.

## 2. Profiling Baseline

I profiled the four independent effect variants in Unity 2022.3.62f2 using the Built-in Render Pipeline. The benchmark uses an orthographic camera at 1080 x 1920 and a deterministic replay harness with seed 1337. `OptimizeBefore.unity` contains the baseline prefab, while `OptimizeAfter.unity` contains the optimized duplicate. Unity Profiler supplied the frame counters and Frame Debugger was used to identify the renderer submissions behind those counters.

| Variant | Draw Calls | Batches | SetPass | Triangles | Vertices |
| --- | ---: | ---: | ---: | ---: | ---: |
| Transition | 6 | 6 | 6 | 160 | 248 |
| PerfectLevel1 | 7 | 7 | 7 | 174 | 212 |
| PerfectLevel2 | 9 | 8 | 8 | 348 | 392 |
| PerfectLevel3 | 9 | 8 | 7 | 424 | 472 |

PerfectLevel3 was the primary workload because it tied for the highest Draw Calls and Batches and had the largest geometry count. PerfectLevel2 was also important because it had the highest SetPass count.

## 3. Investigation and Decisions

Profiler data showed that the cost was distributed across several transparent ParticleSystem renderers rather than one isolated CPU bottleneck. Frame Debugger was necessary to map those submissions to their materials, sorting states, billboard geometry, and trails. I used that mapping to test changes individually and reverted any experiment that did not improve the target metric or preserve the intended image.

### Material and batching investigation

I first tested whether small renderer-state differences were preventing batching. Normalizing Mask Interaction did not change Draw Calls, Batches, or SetPass, so the change was reverted.

I then tested a particle atlas and shared additive material on a compatible-looking group. The renderers used the same shader, sorting order, billboard mode, and alignment, but Frame Debugger still showed separate submissions and the measured counters remained unchanged. The atlas and shared-material prototype added configuration and asset complexity without a performance result, so it was removed. This confirmed that material compatibility alone was not sufficient for this dynamically generated ParticleSystem workload.

### Visual-layer removal

The `init`, `glow`, and `outline_line` layers were each disabled separately. Removing any one of them eliminated a renderer submission, but the visual comparison showed a clear loss: the opening impact weakened without `init`, the central halo disappeared without `glow`, and the silhouette lost structure without `outline_line`. I rejected these changes because lowering a profiler number by deleting important visual information did not meet the task goal.

### Transition consolidation

The Transition variant contained two similar zap systems, `zap1` and `zap2`. Consolidating them reduced Draw Calls from 6 to 4, but their independent trail histories became connected by incorrect horizontal trail segments. The metric improved while the effect became visually wrong, so the experiment was reverted. Transition remains at 6 Draw Calls.

### Successful structural opportunity

PerfectLevel2 and PerfectLevel3 both contained two mirrored rain systems, `rain3` and `rain4`. Both sides were visually necessary, so deleting one emitter was not acceptable. Their post-emission simulation and renderer configuration were compatible, however, which allowed both simulations to remain active while their generated particles were submitted through the `rain3` renderer.

In the optimized prefab, `rain4` still simulates with its original transform, shape, timing, and deterministic seed, but its renderer is disabled. `ParticleSystemRendererConsolidator` copies its emitted particles into `rain3` with the required simulation-space conversion. This changes the renderer count without removing either mirrored side.

### PerfectLevel1

PerfectLevel1 did not contain the mirrored rain pair. Its remaining submissions represented different textures, visual layers, or main-versus-trail topology. No tested low-risk structural change reduced Draw Calls without changing the effect, so it remains at 7 Draw Calls.

## 4. Changes Retained

### PerfectLevel3 trail tessellation

Frame Debugger showed that the `PerfectLevel3/lines` trail was the largest geometry contributor. I increased Trails > Minimum Vertex Distance from 0.2 to 0.4, allowing Unity to add trail vertices less frequently while preserving the same material, timing, and overall path.

The measured peak geometry changed from 424 to approximately 244 triangles and from 472 to approximately 292 vertices, reductions of about 42% and 38% respectively. Draw Calls remained unchanged. The setting was retained because the geometry reduction was measurable and manual playback did not show an unacceptable change in the trail.

### PerfectLevel2 and PerfectLevel3 renderer consolidation

The mirrored-rain consolidation reduced PerfectLevel2 from 9 to 8 Draw Calls and PerfectLevel3 from 9 to 8 Draw Calls. Batches and SetPass were unchanged. The optimization was retained because it removes one real renderer submission while preserving both simulations and their mirrored visual footprint.

## 5. Implementation Validation

The consolidator caches its ParticleSystem references and allocates fixed particle and seed buffers once in `Awake`. Its runtime path uses no LINQ, hierarchy search, collection creation, `Instantiate`, or `Destroy`. During the performed Editor validation, recurring managed allocation attributable to its steady-state and transfer paths was 0 B.

A final implementation audit found that `GetParticles` continued to run after the one-shot transfer had completed. Over 240 frames and five deterministic replays, source reads were 235 for PerfectLevel2 and 234 for PerfectLevel3. I added a `transferComplete` guard that resets when the source system is stopped for replay. After the change, each variant performed five source reads, five target reads, and five target writes across the same five replays: effectively one transfer per playback.

Ten rapid deterministic replays passed for both variants without stale particles, duplicate transfers, accumulation, a missing mirrored side, transform drift, or changing density. Instrumented Editor validation measured approximately 0.024 ms average per `LateUpdate` for PerfectLevel2, with an observed peak of approximately 0.577 ms, and approximately 0.012 ms average for PerfectLevel3, with an observed peak of approximately 0.081 ms. These values validate bounded Editor overhead only; they are not Android device measurements.

## 6. Final Result

| Variant | Before Draw Calls | After Draw Calls | Decision |
| --- | ---: | ---: | --- |
| Transition | 6 | 6 | Visual correctness prioritized |
| PerfectLevel1 | 7 | 7 | No safe reduction found |
| PerfectLevel2 | 9 | 8 | Mirrored-rain renderer consolidation |
| PerfectLevel3 | 9 | 8 | Renderer consolidation and trail geometry reduction |

For PerfectLevel3, peak geometry also changed from 424 to approximately 244 triangles and from 472 to approximately 292 vertices. No Batch or SetPass reduction is claimed for PerfectLevel2 or PerfectLevel3.

## 7. Learnings

Sharing a material does not guarantee fewer ParticleSystem draw submissions, even when shader and sorting settings appear compatible. Frame Debugger was essential for identifying the actual renderer and topology boundaries behind the Profiler counters. A lower metric was not useful when it introduced missing visual layers or invalid trail connections. Trails require particular care because their history and topology can prevent otherwise similar systems from sharing a renderer. Finally, the CPU and GC cost of the custom consolidation helper needed validation after the GPU-side submission was reduced.

## 8. AI Usage

AI assistance was used to help inspect serialized ParticleSystem configuration, organize optimization hypotheses, review profiling results, and review the consolidation implementation. All measurements and retained decisions were validated directly in Unity using Profiler, Frame Debugger, deterministic replay, and manual visual inspection. AI did not generate the reported measurements.

## 9. Remaining Validation

Final Android Development Build profiling and the target-device 60 FPS smoke test remain pending. The results in this report are controlled Editor measurements and should not be presented as Android performance evidence.
