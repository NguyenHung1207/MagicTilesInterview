# Overdraw / Fill-rate Research

## Objective

Research-only audit of `Assets/Optimization/Prefabs/ParticleEffectsOptimized.prefab`.
No production prefab, material, shader, particle setting, scene, or runtime code
was changed. No optimization implementation or commit was made.

The project is Unity 2022.3.62f2, Built-in Render Pipeline, Linear color space,
with no active URP/HDRP asset in `GraphicsSettings.asset`. The prefab contains
28 ParticleSystems and 28 ParticleSystemRenderers: 24 potentially visible effect
layers plus four non-emitting root systems. It contains no LineRenderer.

## Final VFX Inventory

All 28 renderers use `m_RenderMode: 0` (billboard), a null authored mesh, and
camera-facing quad geometry. The serialized `m_MaxParticleSize` is a renderer
clamp, not the authored particle size; the table records authored Start Size.
Screen coverage is a hypothesis until captured from the final camera.

Material aliases resolved from the project assets:

- M1 = `triangle_ 1.mat`, `top_tile_short.png`, built-in Mobile/Particles/Additive.
- M2 = `triangle_ 2.mat`, `fx_circle_line2.png`, built-in Mobile/Particles/Additive.
- M3 = `triangle_ 3.mat`, `hud_light_line.png`, built-in Particles/Standard Unlit.
- M4 = `triangle_ 4.mat`, `StyledConfetti.png`, built-in Particles/Standard Unlit.
- M5 = `triangle_ 5.mat`, `effect_sunray 1.png`, built-in Mobile/Particles/Additive.
- MX = `triangle_.mat`, `triangles.png`, built-in Mobile/Particles/Additive.
- E = `explode0.mat`, `note_long_dot_active.png`, built-in Particles/Standard Unlit.
- T = `trail.mat`, `trail.tga`, built-in Particles/Standard Unlit.
- The four non-emitting roots carry Unity's serialized default particle material
  (`fileID 10308`); visible `rainCombined` uses M1.
- Project-authored transparent materials use additive-style `SrcAlpha/One` and
  `ZWrite Off`; the default rain pass is intentionally marked unresolved above.

| Hierarchy path | System | Renderer / texture usage | Material / blend | Start size | Trails | Coverage / overlap |
|---|---|---|---|---:|---|---|
| `Transition` | root | Billboard quad; no emission | none | 1 | off | no visible layer |
| `Transition/outline_circle` | outline_circle | Billboard quad | M2; additive-style | 40 | off | very large single quad |
| `Transition/init` | init | Billboard quad | E; transparent/additive-style | 40 | off | very large single quad |
| `Transition/zap1` | zap1 | Billboard quads; null mesh | M4 + T; transparent/additive-style | 1.5 | Particle trail, ratio 1, lifetime 1x | 20 particles plus overlapping trails |
| `Transition/zap2` | zap2 | Billboard quads; null mesh | M4 + T; transparent/additive-style | 1.5 | Particle trail, ratio 1, lifetime 1x | 20 particles plus overlapping trails |
| `PerfectLevel1` | root | Billboard quad; no emission | none | 1 | off | no visible layer |
| `PerfectLevel1/outline_line` | outline_line | Billboard quad | M3; transparent/additive-style | 10 | off | broad single line quad |
| `PerfectLevel1/outline_circle` | outline_circle | Billboard quad | M2; additive-style | 9 | off | broad single quad |
| `PerfectLevel1/glow` | glow | Billboard quad | M5; additive | 15 | off | broad glow layer |
| `PerfectLevel1/lines` | lines | Billboard quads; null mesh | M1 + M2; additive-style | 4 | Ribbon trail, ratio 1, lifetime 0.5x | 4 particles plus trails |
| `PerfectLevel1/triangle` | triangle | Billboard quads; texture-sheet UVs | MX; additive | 2 | off | 7 overlapping quads; size modules vary apparent area |
| `PerfectLevel1/init` | init | Billboard quad | E; transparent/additive-style | 20 | off | broad single burst quad |
| `PerfectLevel2` | root | Billboard quad; no emission | none | 1 | off | no visible layer |
| `PerfectLevel2/rainCombined` | rainCombined | 4 billboard quads; null mesh | M1; additive-style | 1.5 | off | four small quads; low overlap |
| `PerfectLevel2/outline_circle` | outline_circle | Billboard quad | M2; additive-style | 9 | off | broad single quad |
| `PerfectLevel2/outline_line` | outline_line | Billboard quad | M3; transparent/additive-style | 10 | off | broad single line quad |
| `PerfectLevel2/glow` | glow | Billboard quad | M5; additive | 20 | off | very broad glow layer |
| `PerfectLevel2/lines` | lines | Billboard quads; null mesh | M1 + M2; additive-style | 3 | Ribbon trail, ratio 1, lifetime 0.5x | 4 particles plus trails |
| `PerfectLevel2/triangle` | triangle | Billboard quads; texture-sheet UVs | MX; additive | 2 | off | 7 overlapping quads; size modules vary apparent area |
| `PerfectLevel2/init` | init | Billboard quad | E; transparent/additive-style | 20 | off | broad single burst quad |
| `PerfectLevel3` | root | Billboard quad; no emission | none | 1 | off | no visible layer |
| `PerfectLevel3/rainCombined` | rainCombined | 4 billboard quads; null mesh | M1; additive-style | 1.5 | off | four small quads; low overlap |
| `PerfectLevel3/outline_circle` | outline_circle | Billboard quad | M2; additive-style | 9 | off | broad single quad |
| `PerfectLevel3/outline_line` | outline_line | Billboard quad | M3; transparent/additive-style | 10 | off | broad single line quad |
| `PerfectLevel3/glow` | glow | Billboard quad | M5; additive | 20 | off | very broad glow layer |
| `PerfectLevel3/lines` | lines | Billboard quads; null mesh | M1 + M1; additive-style | 4 | Ribbon trail, ratio 1, lifetime 0.5x | 6 particles plus trails |
| `PerfectLevel3/triangle` | triangle | Billboard quads; texture-sheet UVs | MX; additive | 2 | off | 7 overlapping quads; size modules vary apparent area |
| `PerfectLevel3/init` | init | Billboard quad | E; transparent/additive-style | 20 | off | broad single burst quad |

There are five enabled Trail modules: `zap1`, `zap2`, and the three `lines`
systems. The three `lines` systems use minimum vertex distance 0.4; the zaps
use 0.2. Their serialized `dieWithParticles: 0` means trail history can remain
until the trail lifetime expires. No production maxParticles value is changed
or proposed by this report.

## Technical Background

Overdraw is repeated rasterization and fragment shading of the same screen pixel
by multiple transparent layers in one frame. Fill-rate is the GPU's capacity to
process covered fragments and perform texture, shader, blend, and framebuffer
work. They are related, but fill-rate is a hardware/work-rate limit and overdraw
is one source of excess covered-pixel work.

Transparent particles are costly on mobile because a large billboard can cover
many pixels, alpha-blended or additive layers normally cannot be rejected by an
opaque depth test, and overlapping particles repeat fragment shader, texture,
blend, and framebuffer bandwidth work. A small quad count can still cover most
of the display. Cost varies with resolution, MSAA, shader, texture alpha,
blending hardware, tile architecture, bandwidth, and thermal state.

This phase optimizes the screen-space fragment workload: overdraw/fragment
activity, with device GPU frame time as the outcome. It does not optimize draw
submission count by assumption.

- Draw Calls are render submissions; one draw can still be heavily overdrawn.
- Batches group compatible submissions; batching does not remove covered pixels.
- SetPass counts shader/render-state switches; it is CPU/render-state overhead.
- Vertices and triangles describe submitted geometry; low geometry can cover a
  large area, while trails can increase geometry without proportional coverage.
- CPU `ParticleSystem.Update` is simulation/module work, not fragment shading.

Reducing Draw Calls does not automatically reduce overdraw. The candidate
parameter must reduce covered transparent pixels or repeated fragment work, then
the GPU result must be measured.

## Candidate Ranking

| Candidate | Why Overdraw Risk | Current Geometry | Screen Coverage | Risk | Measurement Needed |
|---|---|---|---|---|---|
| A — `Transition/zap1` + `zap2` | 40 additive particle quads and two active trail histories | 40 billboard quads plus Particle trails | High during the transition burst | High visual/timing risk | Overdraw view, candidate draw events, device GPU time/counters |
| A — `PerfectLevel3/lines` | Six wide particles with persistent Ribbon Trails; prior geometry evidence is highest here | 6 billboard quads plus trail tessellation | High along line paths | Medium-high silhouette risk | Same-phase overdraw, trail coverage, GPU time, visual replay |
| A — PL1/PL2/PL3 `glow` | Broad additive soft layers with large size | One billboard quad per glow | High local coverage | Medium; glow is intentional | Overdraw map, device GPU time, visual comparison |
| B — large init / Transition outline | Size 20–40 transparent quads | One billboard quad per effect | High local coverage, one layer | Medium-high silhouette risk | Camera-space coverage and GPU timing |
| B — `triangle` systems | Seven additive quads with variable size and overlap | 7 billboard quads with texture-sheet UVs | Medium, burst-dependent | Medium visual risk | Overdraw and deterministic screenshots |

`rainCombined` is C / low priority for overdraw: four small scripted quads and
no trails. The root systems are C because they emit no visible particles.

## Measurement Options

| Method | What it measures | Project use | Editor-only? | Android meaning |
|---|---|---|---|---|
| Scene View Overdraw | Accumulated transparent silhouette/relative screen coverage | Compatible Built-in diagnostic; inspect the final camera framing | Yes | Relative spatial clue only; not device GPU time |
| Frame Debugger | Ordered render events, material/shader, geometry, draw-state details | Compatible with Built-in RP; isolate candidate events | Editor and supported Development Player workflows | Submitted work, not a fragment counter |
| Unity Profiler GPU module | GPU timing and GPU marker hierarchy where supported | Available in 2022.3 when the API/device supports GPU profiling | No; Editor/connected Player | Real device timing when captured on the target, but support varies |
| Rendering Statistics | Draw Calls/Batches, SetPass, vertices, triangles, CPU/render timing | Available as control metrics in Game view/Profiler | Mostly Editor-facing | Useful controls, not overdraw measurement |
| URP Rendering Debugger | URP debug overlays and runtime display statistics | Not applicable; project is Built-in and no URP package/asset is active | No, when URP is configured | Not evidence without a pipeline change |
| RenderDoc | API frame capture, draw events, shader/resources, pixel inspection | Appropriate for desktop DX11/OpenGL Core capture if installed | Editor integration is Editor-only; standalone capture also possible | Not Android proof in this Unity 2022.3 setup |
| Android GPU profiling / AGI | Device GPU/CPU traces, frame profiling, GPU counters where supported | Requires Android build, USB/ADB, compatible device, and installed tool | No | Best route to real fragment/bandwidth/GPU evidence |

The project can use Scene View Overdraw, Frame Debugger, Rendering Statistics,
and Unity GPU profiling in a Unity 2022.3 session. Current workspace evidence is
only serialized settings and historical draw/geometry evidence; no overdraw or
GPU capture exists. `adb devices` reports no connected device. AGI, RenderDoc
CLI, and a Unity executable are not available in this environment.

References: [Unity View Modes](https://docs.unity3d.com/2022.3/Manual/ViewModes.html),
[Unity Frame Debugger](https://docs.unity3d.com/2022.3/Manual/FrameDebugger.html),
[Unity GPU Profiler](https://docs.unity3d.com/2022.3/Manual/ProfilerGPU.html),
[Unity Rendering Statistics](https://docs.unity.cn/Manual/RenderingStatistics.html),
[Unity RenderDoc integration](https://docs.unity3d.com/2022.3/Manual/RenderDocIntegration.html),
and [Android GPU Inspector](https://developer.android.com/agi).

## Recommended A/B Experiment

Use `PerfectLevel3/lines` as the first controlled candidate. It has the largest
known line emission in the final prefab (six particles), wide billboard quads,
Ribbon Trails, and a documented trail geometry history. It is a single effect
branch, so it is easier to phase-lock than the two-system transition zaps.

Build A is the current final VFX. Build B changes only
`PerfectLevel3/lines` Trail Module lifetime multiplier from `0.5` to `0.35`.
This is a research-copy value, not a production recommendation or implementation.

Trail lifetime is selected because it directly shortens active trail coverage and
the number of overlapping trail segments. It leaves emission, particle lifetime,
particle seed, particle speed, material/shader behavior, renderer alignment,
particle size, and gameplay timing unchanged. Do not combine it with MVD,
particle count, size, alpha, material, emission, or glow changes.

Replay the same fixed seed and capture three phase windows: trail birth, peak
coverage, and expiry. Keep camera, resolution, graphics API, quality, frame
rate, thermal state, and scene contents identical. A/B is not valid if the
replay phase or camera differs.

## Metrics

Primary metrics:

- Device overdraw/fragment/bandwidth counter when the GPU tool exposes one.
- GPU frame time or GPU completion time on the target Android device.
- Editor Overdraw heatmap only as a normalized spatial diagnostic, never as the
  sole performance result.

Secondary metrics:

- Total frame time, CPU main-thread time, and CPU render-thread time.
- Draw Calls, Batches, SetPass, vertices, and triangles.
- Measured non-zero-alpha screen coverage and trail footprint at each phase.

Correctness metrics:

- Deterministic replay and equal phase markers.
- No missing VFX, stale trails, accumulation, exceptions, or timing drift.
- Matched screenshots at birth, peak, and expiry.

Expected to change: trail footprint, trail fragment workload, overdraw, and GPU
time if the frame is fill-rate/GPU bound. Vertices and triangles may decrease
because fewer trail segments are alive. Expected to remain unchanged: emission,
particle lifetime, particle count at matched phase, seed, material behavior,
renderer alignment, gameplay timing, and usually Draw Calls/Batches/SetPass.
CPU `ParticleSystem.Update` should be similar; trail maintenance can change
slightly. These are hypotheses to verify, not measured results.

## Mobile Validation

Editor overdraw is not an Android performance result. Editor and desktop GPU
architecture, driver, resolution, shader compiler, tile memory, frame pacing,
and Editor overhead differ from a target handset. Android's basic GPU rendering
bars are also not a substitute for a device GPU counter or GPU frame capture.

Minimum evidence before claiming “GPU performance improved on mobile”:

1. Same physical Android device, OS, build, graphics API, resolution, refresh
   rate, quality settings, thermal state, and fixed replay.
2. A/B builds that differ only in the selected Trail lifetime value.
3. Repeated device GPU measurements showing a meaningful, repeatable reduction
   in GPU frame time or relevant fragment/bandwidth counters.
4. CPU/frame-time controls, rendering counters, and visual/deterministic checks
   recorded alongside the GPU result.

ANDROID VALIDATION = NOT AVAILABLE

## Risks

- Shorter trails can change silhouette, motion readability, brightness, or end
  persistence even when particle timing is unchanged.
- Overdraw colors depend on camera, resolution, and what else is rendered.
- GPU time can be hidden by CPU, VSync, thermal throttling, or another pass.
- Frame Debugger geometry and draw counts do not prove fragment savings.
- A device-specific result may not generalize across Adreno, Mali, or PowerVR.

## Recommendation

Do not modify production VFX yet. When Unity 2022.3 and an Android profiling
path are available, capture the current final baseline first, use Scene View
Overdraw and Frame Debugger to validate the spatial hypothesis, then run the
isolated PL3 `lines` Trail lifetime A/B. Use AGI or a vendor GPU tool for the
promotion decision; use Unity GPU Profiler as a timing supplement.

## Limitations

No live Unity session, Overdraw heatmap, Frame Debugger capture, GPU Profiler
capture, RenderDoc capture, Android GPU counter, or A/B visual replay was
available. Coverage labels are serialized-geometry hypotheses, not pixel
measurements. The report does not claim an Android bottleneck or speedup.

## Final Verdict

The strongest plausible overdraw sources are the Transition zap trails, PL3
lines trails, and the large glow layers. The first research A/B should change
only PL3 `lines` Trail lifetime `0.5 -> 0.35`, with primary measurement of
device fragment/overdraw workload and GPU frame time. No optimization should be
promoted until the change is repeatable on Android without visual or deterministic
regression.
