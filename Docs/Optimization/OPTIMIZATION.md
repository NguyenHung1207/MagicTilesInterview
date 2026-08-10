# Task 2 — UI/VFX Optimization

## 1. Goal

This task evaluates an inherited, unoptimized particle-effect package under a repeatable workload. The goal is to identify measurable rendering costs, make the smallest defensible optimization without unnecessarily degrading the effect, and validate every retained decision with the Unity Profiler, Frame Debugger, and visual comparison.

## 2. Benchmark Methodology

| Item | Configuration |
| --- | --- |
| Unity | 2022.3.62f2 |
| Render pipeline | Built-in Render Pipeline |
| Benchmark resolution | 1080 × 1920 |
| Camera | Orthographic |
| Baseline scene | `Assets/Optimization/Scenes/Optimization.unity` |
| Optimized scene | `Assets/Optimization/Scenes/OptimizationOptimized.unity` |
| Replay seed | 1337 |
| Replay count | Three deterministic replays per variant |

The benchmark harness isolates and replays four independent variants under identical conditions: `Transition`, `PerfectLevel1`, `PerfectLevel2`, and `PerfectLevel3`. Peak values were collected across each replay, so the peak geometry and peak draw-state values do not necessarily occur in the same frame.

`PerfectLevel3` is the primary workload because it ties for the highest Draw Calls and Batches while producing the largest geometry workload. `PerfectLevel2` remains the secondary validation workload because it has the highest SetPass count.

These results are Editor profiling evidence. Final Android Development Build profiling is still pending and must not be compared directly with Editor numbers.

## 3. Baseline Results

| Variant | Peak Draw Calls | Peak Batches | Peak SetPass | Peak Triangles | Peak Vertices |
| --- | ---: | ---: | ---: | ---: | ---: |
| Transition | 6 | 6 | 6 | 160 | 248 |
| PerfectLevel1 | 7 | 7 | 7 | 174 | 212 |
| PerfectLevel2 | 9 | 8 | 8 | 348 | 392 |
| **PerfectLevel3** | **9** | **8** | **7** | **424** | **472** |

## 4. Issues Found

### Excessive trail geometry

Frame Debugger inspection identified `PerfectLevel3/lines` as the largest dynamic-geometry contributor in the primary workload. In the inspected frame, the trail submission used approximately 284 vertices and 816 indices, while the main particle submission used approximately 24 vertices and 36 indices. The trail is visually important, but its tessellation density was higher than necessary.

### Material and texture fragmentation

The effect uses multiple transparent materials, textures, shaders, and sorting orders. These differences constrain batching and produce distinct render submissions. A compatible-looking subgroup was profiled with an atlas and shared material, but compatibility alone did not reduce the measured workload.

### Intentional mirrored emitters

`PerfectLevel3/rain3` and `PerfectLevel3/rain4` participate in the same dynamic-rendering group, but they are not accidental duplicates. They are mirrored emitters used to preserve the composition:

- `rain3`: position X `+2`, position Y `-1.55`, shape rotation Z `-15°`.
- `rain4`: position X `-2`, position Y `-1.55`, shape rotation Z `-195°`.

Consolidating or removing either emitter would be a VFX redesign with visual-regression risk.

## 5. Accepted Optimization — Trail Tessellation

### Diagnosis

The trail topology was the largest geometry contribution. Increasing the minimum distance between generated trail vertices directly targets that cost while preserving the same emitter, material, shader, timing, and trail lifetime.

### Exact change

`PerfectLevel3/lines` → **Trails** → **Minimum Vertex Distance**:

```text
0.2 → 0.4
```

`0.3` was also tested as an intermediate value. The final value of `0.4` produced the larger measurable reduction without a noticeable visual regression.

### Result

| Metric | Before | Current optimized | Change |
| --- | ---: | ---: | ---: |
| Draw Calls | 9 | 9 | No change |
| Batches | 8 | 8 | No change |
| SetPass | 7 | 7 | No change |
| Peak Triangles | 424 | ~244 | **~−42%** |
| Peak Vertices | 472 | ~292 | **~−38%** |

The exact reductions represented by these recorded peaks are approximately 42.5% for triangles and 38.1% for vertices.

### Visual validation

Repeated Editor playback showed no noticeable degradation in trail shape, timing, continuity, or the overall VFX composition. The change was retained because it produced a substantial geometry reduction through one small, reversible setting change.

## 6. Rejected Experiment — Mask Interaction

### Hypothesis

`PerfectLevel3/lines` used **Visible Outside Mask**, while `rain3` and `rain4` used **No Masking**. Mask Interaction was investigated as a possible batching-key difference.

### Experiment and measurement

`PerfectLevel3/lines` was temporarily changed from **Visible Outside Mask** to **No Masking**.

```text
Before: 9 Draw Calls / 8 Batches / 7 SetPass
After:  9 Draw Calls / 8 Batches / 7 SetPass
```

Repeated profiling produced the same result. The experiment was rejected and reverted; it is not part of the optimized scene.

## 7. Rejected Experiment — Particle Atlas + Shared Material

### Why this group was selected

`outline_circle`, `rain3`, and `rain4` shared the most promising renderer state:

- `Mobile/Particles/Additive` shader.
- Default Sorting Layer and Order in Layer `9`.
- Billboard render mode with View alignment.
- Max Particle Size `0.5`.
- No trails in this subgroup.

The principal material difference was the source texture: `outline_circle` used `triangle_2` / `fx_circle_line2`, while `rain3` and `rain4` used `triangle_1` / `top_tile_short`.

### Experiment

The test temporarily normalized `outline_circle` masking, enabled Texture Sheet Animation in Sprite mode, created an atlas for the two textures, created a shared `Mobile/Particles/Additive` material, and assigned it to the three renderers.

### Result

| Metric | Before experiment | With atlas/shared material |
| --- | ---: | ---: |
| Draw Calls | 9 | 9 |
| Batches | 8 | 8 |
| SetPass | 7 | 7 |
| Peak Triangles | ~244 | ~244 |
| Peak Vertices | ~292 | ~292 |

Frame Debugger inspection also showed that `outline_circle` remained a separate render event. The experiment added asset and configuration complexity without a measurable benefit, so all prototype assets and scene overrides were removed.

## 8. Draw-Call Investigation and Decision

Draw-call reduction was investigated rather than assumed. These Particle Systems generate geometry dynamically and already participate in Unity's dynamic batching path. Sharing a shader, sorting state, atlas, and material did not reduce raw Draw Calls, Batches, or SetPass in the measured workload.

The remaining events reflect meaningful differences, including textures, shaders, transparent sorting orders, trail topology, and intentionally separate emitters. GPU Instancing was not adopted because the current ParticleSystem renderers use Billboard render mode. SRP Batcher is not applicable because the project uses the Built-in Render Pipeline. `Mesh.CombineMeshes` and static draw-call-minimizer techniques are not appropriate for this dynamically generated ParticleSystem workload.

The final low-risk investigation tested whether one small renderer layer could be removed. Each isolated renderer toggle reduced the raw counters, but every candidate produced a visible compositional change. Further reduction would therefore require consolidating intentional emitters or redesigning the VFX/render architecture. That carries disproportionate implementation and visual-regression risk relative to the measured benefit. Visual composition was prioritized over artificially lowering a profiler counter. All renderer-removal tests were reverted, while the measurable trail-geometry optimization was retained.

## Renderer Reduction Investigation

The optimized scene was tested with runtime-only `ParticleSystemRenderer.enabled` toggles; no source prefab or scene setting was changed during measurement. Candidates were evaluated one at a time in the required order. Each control and disabled state used three deterministic PerfectLevel3 replays at seed `1337` and 1080 × 1920. The table lists the per-replay peak geometry samples rather than collapsing them into an invented single frame.

| Candidate and state | Draw Calls | Batches | SetPass | Peak triangles by replay | Peak vertices by replay |
| --- | ---: | ---: | ---: | --- | --- |
| `init` control | 9, 9, 9 | 8, 8, 8 | 7, 7, 7 | 180, 244, 248 | 228, 292, 296 |
| `init` renderer disabled | 8, 8, 8 | 7, 7, 7 | 6, 6, 6 | 240, 244, 246 | 288, 292, 294 |
| `glow` control | 9, 9, 9 | 8, 8, 8 | 7, 7, 7 | 238, 242, 230 | 286, 292, 278 |
| `glow` renderer disabled | 8, 8, 8 | 7, 7, 7 | 6, 6, 6 | 236, 244, 240 | 282, 292, 288 |
| `outline_line` control | 9, 9, 9 | 8, 8, 8 | 7, 7, 7 | 238, 246, 244 | 286, 294, 294 |
| `outline_line` renderer disabled | 8, 8, 8 | 7, 7, 7 | 6, 6, 6 | 234, 240, 244 | 280, 286, 290 |

### `init` — REVERT

**Hypothesis:** the short initial flash might contribute little enough to remove one submission. **Exact toggle:** `PerfectLevel3/init` → `ParticleSystemRenderer.enabled = false` on the runtime instance. The raw counters decreased consistently from 9/8/7 to 8/7/6, but the A/B capture showed a changed central impact footprint during the opening flash. Because `init` supports the hit onset, the visual change was not accepted and the renderer was restored.

### `glow` — REVERT

**Hypothesis:** the large additive glow might be visually redundant with the other center layers. **Exact toggle:** `PerfectLevel3/glow` → `ParticleSystemRenderer.enabled = false` on the runtime instance. The raw counters again decreased from 9/8/7 to 8/7/6, but the red central sunray/halo disappeared clearly. This materially weakened the impact and color composition, so the renderer was restored.

### `outline_line` — REVERT

**Hypothesis:** the thin outline line was the lowest-footprint candidate and might be covered by the circle and center layers. **Exact toggle:** `PerfectLevel3/outline_line` → `ParticleSystemRenderer.enabled = false` on the runtime instance. The raw counters decreased from 9/8/7 to 8/7/6 in all three replays. A four-phase sweep across its 0.5-second lifetime showed that the surrounding white ring/horizontal line disappears, weakening the effect silhouette. The renderer was restored.

### Decision

All three candidates proved that removing one renderer can reduce Draw Calls, Batches, and SetPass by one, but none passed the visual acceptance criterion. No renderer-removal override is retained. The current optimized scene therefore remains at 9 Draw Calls, 8 Batches, and 7 SetPass, with only the accepted trail-tessellation setting retained.

## 9. Before / Current Optimized Result

| Metric | Before | Current optimized |
| --- | ---: | ---: |
| Draw Calls | 9 | 9 |
| Batches | 8 | 8 |
| SetPass | 7 | 7 |
| Peak Triangles | 424 | ~244 |
| Peak Vertices | 472 | ~292 |

The retained change reduces peak triangles by approximately **42%** and peak vertices by approximately **38%**. It does **not** reduce Draw Calls, Batches, or SetPass.

## 10. Evidence

### PerfectLevel3 baseline

![PerfectLevel3 baseline draw-state peak](Before/before_pl3_draw_peak.png)

This Profiler frame records the baseline draw-state peak of 9 Draw Calls, 8 Batches, and 7 SetPass, with 400 triangles and 454 vertices in that selected frame.

![PerfectLevel3 baseline geometry peak](Before/before_pl3_geometry_peak.png)

This separate Profiler frame records the baseline geometry peak of 424 triangles and 472 vertices. Peak geometry and peak draw-state were captured from different frames of the deterministic replay.

### PerfectLevel2 secondary validation

![PerfectLevel2 baseline SetPass peak](Before/before_pl2_setpass_peak.png)

This Profiler frame records PerfectLevel2 at 9 Draw Calls, 8 Batches, and the four-variant maximum of 8 SetPass, supporting its use as the secondary validation workload.

### Current optimized PerfectLevel3

![PerfectLevel3 optimized draw-state peak](After/after_trail_draw_peak.png)

After the trail-tessellation change, the selected draw-state frame still records 9 Draw Calls, 8 Batches, and 7 SetPass. Geometry is lower at 236 triangles and 290 vertices in this frame.

![PerfectLevel3 optimized geometry peak](After/after_trail_geometry_peak.png)

This separate optimized frame records the current geometry peak of approximately 244 triangles and 292 vertices, demonstrating the retained geometry reduction.

### Renderer-reduction A/B evidence

![PerfectLevel3 init renderer enabled](Experiments/RendererReduction/init_before.png)

The deterministic control frame includes the short `init` impact layer at the center of the composition.

![PerfectLevel3 init renderer disabled](Experiments/RendererReduction/init_after.png)

With only the `init` renderer disabled, the opening center footprint changes. The measurable draw-call reduction was rejected because this layer supports the initial hit impact.

![PerfectLevel3 glow renderer enabled](Experiments/RendererReduction/glow_before.png)

The control frame shows the red additive sunray/halo behind the central outline layers.

![PerfectLevel3 glow renderer disabled](Experiments/RendererReduction/glow_after.png)

With only the `glow` renderer disabled, the red halo is visibly absent. This was an unacceptable loss of color and impact.

![PerfectLevel3 outline line renderer enabled](Experiments/RendererReduction/outline_line_before.png)

The early-lifetime control frame shows the thin white surrounding ring/horizontal line that supports the silhouette.

![PerfectLevel3 outline line renderer disabled](Experiments/RendererReduction/outline_line_after.png)

With only `outline_line` disabled, that surrounding white structure is missing. Multi-phase inspection confirmed that the difference persists through the layer's lifetime, so the test was reverted.

### Evidence still to capture

- **TODO — Trail Frame Debugger diagnosis:** capture `PerfectLevel3/lines` main and trail render events with readable vertex/index counts.
- **TODO — Rejected atlas experiment:** if the experiment is reconstructed for documentation only, capture the Frame Debugger event proving `outline_circle` remains separate. Do not retain the experimental assets or configuration afterward.
- **TODO — Android final evidence:** capture equivalent Before and After Profiler evidence from the same target device and Development Build configuration.

## 11. Learnings

- Peak draw-state and peak geometry can occur in different frames; both should be recorded instead of combining unrelated counters into one claimed sample.
- Transparent ParticleSystem batching depends on more than shared shader/material compatibility. Sorting, topology, generated streams, and renderer state still matter.
- A targeted topology setting can produce a larger and safer benefit than a more complex atlas or renderer-consolidation change.
- A renderer can have a small pixel footprint and still provide important timing, color, or silhouette information; a lower draw-call number alone is not sufficient acceptance evidence.
- An unsuccessful experiment is useful evidence when it is measured, documented, and fully reverted.
- Editor profiling is appropriate for controlled iteration, but final mobile conclusions require a consistent on-device Development Build comparison.

## 12. AI Usage

AI assistance was used for technical review, hypothesis generation, documentation support, and interpretation of serialized Unity configuration and profiling evidence. Every retained change and reported result was manually validated through Unity Profiler measurements, Frame Debugger investigation, deterministic replay, and visual testing. AI-generated hypotheses were not accepted without measurement.

## 13. Next Investigation

A texture-memory audit identified possible Android memory and bandwidth experiments involving `trail.tga`, `effect_sunray 1`, `triangles`, and `hud_light_line`. No texture-memory optimization has been accepted or implemented. Any future experiment must change one setting at a time and measure runtime texture memory and visual quality on the target Android device.

## 14. Pending Final Validation

- Android Development Build profiling is pending.
- Final Android Before/After evidence is pending.
- Final four-variant visual-regression validation is pending.
- The Editor evidence in this report must not be presented as Android performance evidence.
