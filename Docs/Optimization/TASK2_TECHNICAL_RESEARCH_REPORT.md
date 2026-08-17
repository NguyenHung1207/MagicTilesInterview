# Task 2 Technical Research Report

## Scope and evidence rules

This report audits only the dedicated `task2/ui-vfx-optimization` branch. It does not use Task 1 gameplay, HUD, video-background, note-spawning, or other `main`-branch production systems as optimization evidence.

Source-of-truth evidence, in descending order of authority:

1. Current serialized Task 2 scenes and prefabs.
2. Current Task 2 scripts, materials, texture import metadata, and final screenshots.
3. Git history from the Task 2 import commit through the final report commit.
4. `Docs/Optimization/OPTIMIZATION.md`, reconciled against the current branch and Git history.

No Android/device profiling result exists in the branch. Editor evidence is never presented here as proof of mobile frame time or 60 FPS.

## 1. TASK2_BRANCH_MAP

```text
Task 2
├── Original imported package
│   └── Assets/0_Mep/General/Prefabs/Gameplay/GameplayEffect/
│       ParticleEffectUnOptimizeExport/
│       ├── ParticleEffectsUnoptimize.prefab
│       ├── Materials/ (9 materials)
│       └── Textures/  (8 textures)
├── BEFORE scene
│   └── Assets/Optimization/Scenes/OptimizeBefore.unity
├── Optimized prefab
│   └── Assets/Optimization/Prefabs/ParticleEffectsOptimized.prefab
├── AFTER scene
│   └── Assets/Optimization/Scenes/OptimizeAfter.unity
├── Benchmark runner
│   └── Assets/Optimization/Scripts/OptimizationBenchmarkRunner.cs
├── Optimization helper
│   └── Assets/Optimization/Scripts/ParticleSystemRendererConsolidator.cs
├── Final evidence
│   └── Docs/Optimization/FinalEvidence/ (6 PNG files)
└── Documentation
    ├── Docs/Optimization/OPTIMIZATION.md
    └── Docs/Optimization/TASK2_TECHNICAL_RESEARCH_REPORT.md
```

### Exact important paths

- Original prefab: `Assets/0_Mep/General/Prefabs/Gameplay/GameplayEffect/ParticleEffectUnOptimizeExport/ParticleEffectsUnoptimize.prefab`
- Original materials: `Assets/0_Mep/General/Prefabs/Gameplay/GameplayEffect/ParticleEffectUnOptimizeExport/Materials/`
- Original textures: `Assets/0_Mep/General/Prefabs/Gameplay/GameplayEffect/ParticleEffectUnOptimizeExport/Textures/`
- Before scene: `Assets/Optimization/Scenes/OptimizeBefore.unity`
- Optimized prefab: `Assets/Optimization/Prefabs/ParticleEffectsOptimized.prefab`
- After scene: `Assets/Optimization/Scenes/OptimizeAfter.unity`
- Benchmark runner: `Assets/Optimization/Scripts/OptimizationBenchmarkRunner.cs`
- Consolidator: `Assets/Optimization/Scripts/ParticleSystemRendererConsolidator.cs`
- Final evidence: `Docs/Optimization/FinalEvidence/`
- Existing final report: `Docs/Optimization/OPTIMIZATION.md`

No custom Task 2 shader asset exists. The materials use Unity Built-in Render Pipeline shaders.

### Final evidence inventory

| File | Evidence |
|---|---|
| `before_pl3_draw_calls.png` | PL3 Before: 9 Draw Calls, 8 Batches, 7 SetPass; selected-frame 320 triangles/374 vertices |
| `before_pl3_geometry.png` | Dedicated Before geometry: 410 triangles/460 vertices |
| `before_pl3_rain_frame_debugger.png` | Rain group Before: 2 Draw Calls, 16 vertices, 24 indices |
| `after_pl3_draw_calls.png` | PL3 After: 8 Draw Calls, 8 Batches, 7 SetPass; selected-frame 216 triangles/270 vertices |
| `after_pl3_geometry.png` | Dedicated After geometry: 224 triangles/274 vertices |
| `after_pl3_rain_frame_debugger.png` | Rain group After: 1 Draw Call, 16 vertices, 24 indices |

Git history shows earlier `Before`, `After`, `DrawCall`, and `Experiments` evidence folders. Commit `925273c` intentionally removed these intermediate artifacts after the final six screenshots were produced; they remain historical, not current deliverables.

## 2. TASK2_CURRENT_FINAL_STATE

The serialized comparison between the imported and optimized prefabs is precise. Apart from two added consolidator components, the only ParticleSystem/renderer differences are:

| Hierarchy | Property | Before | Final After |
|---|---|---:|---:|
| `PerfectLevel1/lines` | `TrailModule.minVertexDistance` | 0.2 | 0.4 |
| `PerfectLevel2/lines` | `TrailModule.minVertexDistance` | 0.2 | 0.4 |
| `PerfectLevel3/lines` | `TrailModule.minVertexDistance` | 0.2 | 0.4 |
| `PerfectLevel2/rain4` | Renderer enabled | Yes | No |
| `PerfectLevel3/rain4` | Renderer enabled | Yes | No |

Consolidators are attached to the PL2 and PL3 root systems:

- PL2 target `rain3`, source `rain4`.
- PL3 target `rain3`, source `rain4`.

Every other normalized ParticleSystem and ParticleSystemRenderer value matches the imported prefab.

### Final measured results

| Variant | Metric | Before | After | Change |
|---|---|---:|---:|---:|
| PL1 | Draw Calls / Batches / SetPass | 7 / 7 / 7 | 7 / 7 / 7 | None |
| PL1 | Triangles / Vertices | 126 / 164 | 86 / 124 | -31.7% / -24.4% |
| PL2 | Draw Calls / Batches / SetPass | 9 / 8 / 8 | 8 / 8 / 8 | One Draw Call only |
| PL2 | Triangles / Vertices | 196 / 240 | 156 / 200 | -20.4% / -16.7% |
| PL3 | Draw Calls / Batches / SetPass | 9 / 8 / 7 | 8 / 8 / 7 | One Draw Call only |
| PL3 | Triangles / Vertices | 410 / 460 | 224 / 274 | -45.4% / -40.4% |

PL3 draw-state and geometry numbers come from separate frames because trail topology varies over its lifetime. This separation is correct and explicitly documented.

### Benchmark quality

Strengths:

- Unity 2022.3.62f2, Built-in Render Pipeline, common orthographic camera and 1080 x 1920 context.
- Stable hierarchy-order random seeds beginning at 1337.
- All four variants are stopped and cleared before one selected variant is played.
- Playback is deferred one frame after reset.
- Before references the untouched imported prefab; After references the optimized duplicate.

Limitations:

- No automated warm-up or fixed measurement window.
- No automatic replay count, phase marker, percentile export, or raw trace.
- No retained CPU or GPU timing screenshot.
- GetParticles/GC validation is textual; raw instrumentation was removed.
- Editor Play Mode is not target-device isolation.
- Task 2 scenes are not currently included in `ProjectSettings/EditorBuildSettings.asset`.

## 3. TASK2_EXPERIMENT_HISTORY

| Class | Experiment | Problem/hypothesis | Exact change and target | Result | Visual/decision |
|---|---|---|---|---|---|
| **FINAL** | PL3 trail MVD | `PerfectLevel3/lines` trail was the largest geometry contributor | MVD 0.2 -> 0.3 (intermediate) -> 0.4 | Final evidence 410/460 -> 224/274; draw state unchanged | 0.4 retained as acceptable visual/geometry compromise |
| **FINAL** | PL1/PL2 MVD generalization | Same `lines` trail structure might benefit | MVD 0.2 -> 0.4 | PL1 126/164 -> 86/124; PL2 196/240 -> 156/200 | Fixed-phase review reportedly acceptable |
| **FINAL** | Mirrored rain renderer consolidation | PL2/PL3 rain3 and rain4 used compatible material/modules but two renderers | Disable rain4 renderer; copy its particles into rain3 | Rain group 2 -> 1 Draw Call; variant 9 -> 8; geometry preserved; Batches/SetPass unchanged | Both mirrored sides reportedly preserved |
| **FINAL** | Transfer-complete guard | Initial helper called source GetParticles on most frames | Return after first successful transfer; reset on observed source stop | Five replays: 235/234 source reads -> 5/5 | Retained; removes self-introduced polling work |
| **REJECTED/REVERTED** | Mask normalization | `lines` mask interaction might break batching | Visible Outside Mask -> No Masking | 9/8/7 remained 9/8/7 | No benefit; reverted |
| **REJECTED/REVERTED** | Atlas/shared material | `outline_circle`, rain3, rain4 looked renderer-compatible | Atlas two textures, normalize mask, enable texture-sheet sprite path, share additive material | Draw Calls/Batches/SetPass and geometry unchanged; outline remained separate | Added complexity; all prototype assets/overrides removed |
| **REJECTED/REVERTED** | Disable `init` | Delete one renderer submission | Disable only init renderer | Counter reduced by one submission | Opening impact visibly weakened; reverted |
| **REJECTED/REVERTED** | Disable `glow` | Delete one renderer submission | Disable only glow renderer | Counter reduced | Colored halo/impact lost; reverted |
| **REJECTED/REVERTED** | Disable `outline_line` | Delete one renderer submission | Disable only outline_line renderer | Counter reduced | Silhouette structure lost; reverted |
| **REJECTED/REVERTED** | Transition zap consolidation | zap1/zap2 appeared structurally similar | Disable one renderer and combine output | Transition 6 -> 4 Draw Calls | Independent ribbon histories were joined by invalid horizontal segments; reverted |
| **REJECTED/REVERTED** | PL1 trail main-material reassignment | Shared material might reduce PL1 draw state | Reassign main while retaining trail material | Stayed 7/7/7 | Reverted |
| **TEMPORARY/EXPERIMENTAL** | Consolidator instrumentation | Quantify custom helper overhead and replay correctness | Temporary counters/timers | Old report: about 0.024 ms average/0.577 ms peak PL2 and 0.012/0.081 ms PL3 in Editor; ten rapid replays passed | Raw instrumentation/evidence does not survive; not authoritative Android data |
| **TEMPORARY/EXPERIMENTAL** | Intermediate scenes/evidence | Establish deterministic benchmark and document iterations | Earlier `Optimization*.unity` scenes and screenshot folders | Later renamed/cleaned | Superseded by current paired scenes and six final files |

The final report supersedes earlier intermediate geometry peaks such as 424/472 and approximately 244/292. Current screenshot-supported PL3 geometry is 410/460 versus 224/274.

## 4. TASK2_PARTICLE_SYSTEM_AUDIT

There are 30 ParticleSystems in both prefabs. Common settings unless noted:

- Non-looping; Play On Awake off; scaled time.
- Local simulation space, Hierarchy scaling, Automatic culling.
- Billboard renderer, View alignment, Sort Mode None.
- Custom particle/trail vertex streams disabled.
- Noise, Collision, Trigger, Lights, Sub Emitters, External Forces, Velocity over Lifetime, Inherit Velocity, Force over Lifetime, Lifetime by Emitter Speed, Size/Rotation/Color by Speed, and Custom Data are disabled.

These already-disabled modules are not optimization opportunities.

### Material abbreviations

- `M1`: `triangle_ 1.mat`, `top_tile_short.png`, Mobile/Particles/Additive.
- `M2`: `triangle_ 2.mat`, `fx_circle_line2.png`, Mobile/Particles/Additive.
- `M3`: `triangle_ 3.mat`, `hud_light_line.png`, Particles/Standard Unlit.
- `M4`: `triangle_ 4.mat`, `StyledConfetti.png`, Particles/Standard Unlit.
- `M5`: `triangle_ 5.mat`, `effect_sunray 1.png`, Mobile/Particles/Additive.
- `MX`: `triangle_.mat`, `triangles.png`, Mobile/Particles/Additive.
- `E`: `explode0.mat`, `note_long_dot_active.png`, Particles/Standard Unlit.
- `T`: `trail.mat`, `trail.tga`, Particles/Standard Unlit.

### Complete system inventory

| Hierarchy | Emission | Lifetime / speed | Max | Active modules | Final renderer/material |
|---|---|---|---:|---|---|
| `Transition` root | None | - | 1000 | None | Disabled |
| `Transition/init` | Burst 1 | 0.3 / 0 | 1000 | Shape, Size, Color | E |
| `Transition/outline_circle` | Burst 1 | 0.4 / 0 | 1000 | Shape, Size, Color | M2 |
| `Transition/zap1` | Burst 1 x20, interval 0.01; rate 0.1/s | 0.3 / -150..150 | 1000 | Shape, Size, Color, Limit Velocity, Trails | M4 + T |
| `Transition/zap2` | Same | Same | 1000 | Same | M4 + T |
| `PerfectLevel1` root | None | - | 1000 | None | Disabled |
| `PL1/glow` | Burst 1 | 1 / 0 | 1 | Shape, Size, Color, Limit Velocity | M5 |
| `PL1/init` | Burst 1 | 0.3 / 0 | 1000 | Shape, Size, Color | E |
| `PL1/outline_circle` | Burst 1 | 0.5 / 0 | 1000 | Shape, Size, Color | M2 |
| `PL1/outline_line` | Burst 1 | 0.5 / 0 | 1000 | Shape, Size, Color | M3 |
| `PL1/lines` | Burst 4 | 0.4 / 30 | 1000 | Shape, Size, Limit Velocity, Trails | M1 + M2 |
| `PL1/triangle` | Burst 7 | 0.3-1 / 50-100 | 30 | Shape, Rotation, Color, Texture Sheet, Limit Velocity | MX |
| `PerfectLevel2` root | None | - | 1000 | None | Disabled |
| `PL2/glow` | Burst 1 | 1 / 0 | 1 | Shape, Size, Color, Limit Velocity | M5 |
| `PL2/init` | Burst 1 | 0.3 / 0 | 1000 | Shape, Size, Color | E |
| `PL2/outline_circle` | Burst 1 | 0.5 / 0 | 1000 | Shape, Size, Color | M2 |
| `PL2/outline_line` | Burst 1 | 0.5 / 0 | 1000 | Shape, Size, Color | M3 |
| `PL2/lines` | Burst 4 | 0.7 / 50 | 1000 | Shape, Size, Limit Velocity, Trails | M1 + M2 |
| `PL2/rain3` | Burst 2; rate 0.1/s | 0.4-0.6 / 50-100 | 1000 | Shape, Size, Rotation, Color, Limit Velocity | M1 enabled |
| `PL2/rain4` | Same | Same | 1000 | Same | M1 renderer disabled |
| `PL2/triangle` | Burst 7 | 0.3-1 / 50-100 | 30 | Shape, Rotation, Color, Texture Sheet, Limit Velocity | MX |
| `PerfectLevel3` root | None | - | 1000 | None | Disabled |
| `PL3/glow` | Burst 1 | 1 / 0 | 1 | Shape, Size, Color, Limit Velocity | M5 |
| `PL3/init` | Burst 1 | 0.3 / 0 | 1000 | Shape, Size, Color | E |
| `PL3/outline_circle` | Burst 1 | 0.5 / 0 | 1000 | Shape, Size, Color | M2 |
| `PL3/outline_line` | Burst 1 | 0.5 / 0 | 1000 | Shape, Size, Color | M3 |
| `PL3/lines` | Burst 6 | 0.7 / 30-50 | 1000 | Shape, Size, Limit Velocity, Trails | M1 + M1 |
| `PL3/rain3` | Burst 2; rate 0.1/s | 0.4-0.6 / 50-100 | 1000 | Shape, Size, Rotation, Color, Limit Velocity | M1 enabled |
| `PL3/rain4` | Same | Same | 1000 | Same | M1 renderer disabled |
| `PL3/triangle` | Burst 7 | 0.3-1 / 50-150 | 30 | Shape, Rotation, Color, Texture Sheet, Limit Velocity | MX |

### Module assessment

- Shape, Size, Color, Rotation, and Texture Sheet modules define visible layout and animation.
- Limit Velocity has non-zero damping/drag and changes motion. Removal needs one-system-at-a-time visual A/B evidence.
- Trails are essential to `lines` and both zaps.
- Rain/zap rate 0.1/s may be ineffective during their short non-looping durations, but emitted counts must be measured before changing it.
- Automatic culling is already enabled. Custom bounds are unlikely to matter during the short, visible benchmark phase.
- Serialized GPU instancing is irrelevant to these Billboard renderers; mesh instancing applies to Mesh render mode.

## 5. TASK2_RENDERING_ANALYSIS

### Optimized PL3 remaining submissions

| Submission | Material/shader | Why separate | Realistically batchable? |
|---|---|---|---|
| `glow` | M5 / Mobile Additive | Unique texture and sorting order 1 | Not without redesign |
| `outline_circle` | M2 / Mobile Additive | Unique texture/layer; atlas test failed | No demonstrated path |
| `outline_line` | M3 / Standard Unlit | Different texture/shader | No |
| `lines` particle quads | M1 / Mobile Additive | Own dynamic billboard renderer | Can reuse pass, not necessarily draw |
| `lines` trail | M1 / Mobile Additive | Trail topology and generated trail streams | Separate from billboard geometry |
| `rain3` | M1 / Mobile Additive | Own dynamic renderer and different mask state | Current script combines rain only |
| `triangle` | MX / Mobile Additive | Texture-sheet UVs, unique texture/order 11 | No |
| `init` | E / Standard Unlit | Unique texture/shader/order 11 | No |

Before adds `rain4` as the ninth draw. The rain Frame Debugger evidence proves 2 draws/16 vertices/24 indices before and 1 draw with unchanged geometry after.

### Draw Calls versus Batches versus SetPass

PL3 Before is 9 Draw Calls, 8 Batches, 7 SetPass; After is 8/8/7.

- A Draw Call is an individual render submission.
- A Batch is Unity's grouped rendering work after batching decisions.
- A SetPass is shader/pass state activation.
- Removing the compatible second rain renderer removed one submission, but not a batch or pass switch.
- Identical material does not merge billboard and trail topology or different renderer-generated buffers.
- Transparent sort order, mask interaction, texture, shader, topology, and vertex layout are batch constraints.

The consolidation therefore reduces submission count only. It does not reduce particle simulations, geometry, SetPass, or measured batches.

## 6. TASK2_TRAIL_ANALYSIS

| System | Mode | MVD | Trail lifetime | Ribbons | Texture mode | Material | World space |
|---|---|---:|---:|---:|---|---|---|
| PL1 `lines` | Particle | 0.4 | 0.5x particle lifetime | 1 | Stretch | M2 | Local |
| PL2 `lines` | Particle | 0.4 | 0.5x | 1 | Stretch | M2 | Local |
| PL3 `lines` | Particle | 0.4 | 0.5x | 1 | Stretch | M1 | Local |
| Transition `zap1` | Ribbon | 0.2 | 1.0x | 1 | Stretch | T | Local |
| Transition `zap2` | Ribbon | 0.2 | 1.0x | 1 | Stretch | T | Local |

MVD 0.2 -> 0.4 reduced trail vertices/triangles, dynamic vertex/index uploads, GPU vertex work, and possibly CPU trail construction. It did not reduce particle count, simulation modules, draw state, material switches, texture memory, or necessarily fragment coverage.

`0.4` is a defensible current knee because all three `lines` variants showed measured geometry reductions and reportedly acceptable fixed-phase visuals. Testing 0.5 and 0.6 is useful as a quality-tier experiment, not an automatic recommendation.

Stop increasing MVD if trails show faceting, corners being cut, gaps, width/taper popping, discontinuous starts/ends, or changed silhouette. Shortening trail lifetime may reduce simultaneous segments and overdraw but has higher visual risk.

Transition zap consolidation should not be repeated without a new topology design; independent ribbon histories are structurally incompatible with the current copying approach.

## 7. TASK2_OVERDRAW_ANALYSIS

Overdraw is plausible, not measured. All used materials are transparent additive; overlapping pixels execute fragment shading repeatedly even where draw count is small.

| Candidate | Exact evidence | Impact / confidence | Tool and controlled experiment | Visual risk |
|---|---|---|---|---|
| Central glow/init/outlines | Multiple additive quads overlap at the same center; start sizes up to 20 | Low-Medium / Medium | Unity Overdraw view plus device GPU capture; disable one layer diagnostically and compare fragment/GPU time | High if retained |
| PL3 lines trail | Long, wide additive trail; largest geometry contributor | Medium / Medium | RenderDoc/AGI pixel inspection; hold MVD fixed while testing width/lifetime | Medium-High |
| Rain | Only four visible quads, size 1.5 with 1.3 transform scale | Low / High | Compare rain enabled/disabled over many replays | Low diagnostic risk |
| Triangle burst | Seven moving additive quads | Low / Medium | 7 -> 5 quality-tier A/B on device | Medium |
| Transition zaps | Two 20-particle ribbon systems | Medium during Transition / Medium | Profile Transition separately using GPU counters/Overdraw view | High |
| Empty transparent texels | Billboard textures contain transparent borders | Low-Medium / Low | RenderDoc quad coverage vs non-zero alpha; test tighter source artwork only if waste is large | Medium |

The camera vertical span is 800 units and the largest initial quads are small relative to it, so catastrophic full-screen fill-rate cost is not suggested by serialization. Device measurement is still required.

Recommended tools: Unity Overdraw view, GPU Profiler where supported, RenderDoc, Android GPU Inspector for Vulkan, and vendor GPU tooling.

## 8. TASK2_SHADER_MATERIAL_AUDIT

| Material | Texture | Shader | Relevant state/features | Use |
|---|---|---|---|---|
| `triangle_ 1.mat` | top tile | Mobile/Particles/Additive | SrcAlpha/One, transparent, unlit, ZWrite Off | Lines/rain |
| `triangle_ 2.mat` | circle | Mobile/Particles/Additive | Additive | Circles and PL1/2 trail |
| `triangle_ 5.mat` | sunray | Mobile/Particles/Additive | Additive | Glow |
| `triangle_.mat` | triangle sheet | Mobile/Particles/Additive | Additive | Triangle burst |
| `explode0.mat` | dot | Particles/Standard Unlit | Additive mode, ZWrite Off, Cull Back | Init |
| `triangle_ 3.mat` | light line | Particles/Standard Unlit | Additive transparent | Outline line |
| `triangle_ 4.mat` | confetti | Particles/Standard Unlit | Additive transparent | Zap particle |
| `trail.mat` | trail | Particles/Standard Unlit | Additive, Cull Off | Zap trails |
| `ParticleAdditive.mat` | none | Mobile/Particles/Additive | Unused; stale saved properties | No prefab use |

For all used Standard Unlit materials, soft particles, distortion, lighting, emission, and camera fading are disabled; GRABPASS is disabled. Mobile Additive is already a minimal one-texture additive shader. The captured Frame Debugger event uses pass 0 with SrcAlpha/One, ZWrite Off, ZTest LessEqual, and Cull Off.

Material/pass sharing does not imply one draw. PL3 lines main, lines trail, and rain3 share M1 but retain separate topology/renderer state. The atlas experiment failed because texture normalization did not remove renderer, topology, masking, stream, and transparent-order boundaries.

A minimal custom shader should be tested only after Android GPU evidence shows meaningful shader/fragment cost. Replacing already-cheap Mobile Additive shaders without that evidence is not justified. A one-texture additive replacement for the four Standard Unlit materials is technically possible but expected to have low absolute benefit at this screen coverage.

## 9. TASK2_TEXTURE_MEMORY_AUDIT

All textures have Read/Write disabled and mipmaps disabled.

| Texture | Source | Android override | Estimated imported dimensions | Approx. compressed payload |
|---|---:|---|---:|---:|
| `effect_sunray 1.png` | 105x512 RGBA | Max 256, ASTC 6x6 | ~53x256 | ~6.0 KiB |
| `fx_circle_line2.png` | 256x256 RGBA | Max 128, ASTC 6x6 | 128x128 | ~7.6 KiB |
| `hud_light_line.png` | 1125x15 RGBA | Max 128, ASTC 6x6 | ~128x2 | ~0.34 KiB |
| `note_long_dot_active.png` | 48x48 RGBA | Max 64, ASTC 5x5 | 48x48 | ~1.6 KiB |
| `StyledConfetti.png` | 150x150 RGBA | Max 256, ASTC 6x6 | 150x150 | ~9.8 KiB |
| `top_tile_short.png` | 105x105 RGBA | Max 128, ASTC 6x6 | 105x105 | ~5.1 KiB |
| `trail.tga` | 64x64 RGB | Max 256, ETC2 RGBA8 | 64x64 | ~4.0 KiB |
| `triangles.png` | 505x762 RGBA | Max 256, ASTC 6x6 | ~170x256 | ~19.5 KiB |

Estimated total compressed payload is about 54 KiB before engine metadata/alignment. The active PL3 subset estimates to approximately 41 KiB, consistent with the final Profiler screenshot's 41.4 KB used-texture value.

Conclusions:

- Textures are not oversized for Android.
- Mipmaps and Read/Write are already appropriately disabled.
- Android ASTC/ETC2 overrides are aggressive and reasonable.
- Texture memory is much smaller than the consolidator managed buffers.
- Further atlasing has no demonstrated submission benefit.
- `trail.tga` could use a non-alpha format because the source is RGB, but its absolute payload is about 4 KiB; this is not an FPS priority.
- Validate ASTC support/fallback on target devices, but do not claim a problem without device evidence.

## 10. TASK2_CPU_GC_AUDIT

### OptimizationBenchmarkRunner

- `Awake` recursively counts/caches ParticleSystems into fixed arrays.
- Replay does not perform hierarchy searches.
- Seed assignment is stable and allocation-free.
- `Update` polls keys and handles the one-frame deferred replay.
- No LINQ, Lists, Instantiate, Destroy, or obvious recurring managed allocation exists.

Benchmark limitations remain: no warm-up, sample window, percentile output, phase marker, timescale/vSync control, automated screenshot, or raw export.

### Realistic capacity

| System type | Serialized max | Realistic concurrency | Safe experimental cap |
|---|---:|---:|---:|
| Roots | 1000 | 0 | 1 or unchanged |
| init/outlines | 1000 | 1 | 2 |
| glow | 1 | 1 | 1 |
| PL1/PL2 lines | 1000 | 4 | 6-8 |
| PL3 lines | 1000 | 6 | 8-10 |
| triangle | 30 | 7 | 10 |
| Individual rain source | 1000 | 2 burst particles | 3-4 |
| Consolidated rain target | 1000 | 4 total after copying | 5-6 |
| Each zap | 1000 | 20 | 24-32 |

The rain3 target must accommodate its own particles plus copied rain4 particles. Reducing it to two would break the current solution.

Lower maxParticles may reduce native reserve/capacity and definitely shrinks consolidator managed arrays. At these tiny live counts it is unlikely to improve frame time materially; use Memory Profiler to measure native and managed deltas.

## 11. TASK2_CONSOLIDATOR_DEEP_DIVE

### Exact buffers and managed memory

Per helper:

- `targetParticles`: 2000 `ParticleSystem.Particle` elements.
- `sourceParticles`: 1000 particles.
- `transferredSourceSeeds`: 1000 `uint` values.

Unity's sequential Particle structure is approximately 132 bytes in the relevant reference layout. Payload per helper is therefore approximately:

- Target: 264,000 bytes.
- Source: 132,000 bytes.
- Seeds: 4,000 bytes.
- Total: ~400,000 bytes plus array headers.

Two helpers allocate approximately 800 KB decimal, about 781 KiB / 0.76 MiB managed payload, to copy two source particles per playback in each active variant.

### Runtime sequence

1. `Awake` validates distinct references and allocates all arrays.
2. `LateUpdate` waits for a running source with particles.
3. `source.GetParticles` copies native source state to managed memory.
4. `target.GetParticles` copies native target state to managed memory.
5. New source seeds are linearly checked, converted, and appended.
6. `target.SetParticles` writes the combined array to native target state.
7. `transferComplete` blocks further reads until `source.isStopped` is observed.

The source simulation continues after its renderer is disabled. Source particles are not moved or removed; copies are inserted into the target. The target then applies its post-emission modules to copied particles.

Rain3/rain4 modules are identical except Shape rotation. Current transforms have mirrored positions, equal `(1.3,1.3,1)` scale, and static rotation. This makes current post-emission compatibility credible.

### Simulation-space conversion

- Local/custom spaces resolve a Transform; World resolves null.
- Position: source local -> world -> target local.
- Velocity: source vector -> world -> target vector.
- Axis of rotation: source direction -> world -> target direction.

It does not explicitly transform particle size, scalar/3D rotation, angular velocity, or other custom state. Equal current scales/modules limit the problem, but future unequal scale or renderer changes can break equivalence.

### What transferComplete guarantees

It guarantees at most one successful transfer per playback where a stopped frame was observed. It does not guarantee that all future particles are copied.

Hidden assumptions:

- Both burst particles exist on the first non-zero read.
- No delayed burst or meaningful continuous emission occurs later.
- Stop/restart is visible to LateUpdate.
- Helper disable/enable does not happen while the source continues running.
- Source/target transforms remain static after transfer.
- Modules, material, streams, scaling, and cap remain compatible.
- Target capacity includes both systems.
- Random seed collision/correlation is harmless.
- Timescale/replay changes do not alter the one-shot timing.

If emission, lifetime, simulation space, transforms, delayed bursts, material, replay lifecycle, object activation, or timescale changes, the helper requires renewed validation. In particular, a later burst becomes invisible because `transferComplete` returns before `GetParticles`.

### Design comparison

| Design | Rain draws | Simulations | Copy APIs | Managed buffer | Correctness/maintenance |
|---|---:|---:|---|---:|---|
| A: Original two systems | 2 | 2 | None | None | Low risk, easiest editing |
| B: Current helper | 1 | 2 | Get source + Get target + Set target once/replay | ~0.76 MiB total | Medium-high assumptions |
| C: One authored system | 1 | 1 | None | None | Authoring equivalence must be proven |

### Consolidator verdict

The helper is competent for the exact current burst but weak as a production optimization. It saves one four-quad submission, with no Batch, SetPass, geometry, or simulation reduction, while adding memory, API copying, lifecycle assumptions, and maintenance risk. Keep it only provisionally until Android A/B/C testing is complete.

## 12. TASK2_AUTHOR_TIME_CONSOLIDATION_RESEARCH

Current rain construction:

- rain3 at `(+2,-1.55)`, Circle Shape radius 0.88, 30-degree arc, shape rotation -15 degrees.
- rain4 at `(-2,-1.55)`, same shape, rotation -195 degrees.
- Each bursts two particles with identical lifetime, speed, size, rotation, color, Limit Velocity, and M1 renderer settings.

A standard ParticleSystem Burst cannot carry a separate shape/position, so two displaced narrow arcs cannot be reproduced exactly merely by adding another burst to one Circle Shape.

### Preferred author-time experiment: Mesh Shape

1. Use one ParticleSystem at the shared PL root.
2. Author a small mesh whose vertices sample both original 30-degree emission arcs.
3. Place vertex groups around x +/-2, y -1.55 in root-local coordinates.
4. Encode the two opposing emission directions in vertex normals.
5. Use Mesh Shape vertex emission and a burst of four.
6. Preserve lifetime 0.4-0.6, speed 50-100, size 1.5, rotation, color, Limit Velocity, local simulation, and M1.
7. Test Mesh Spawn loop/burst-spread modes for balanced two-per-side output.
8. Compare deterministic fixed-phase frames and particle counts against Build A.

This removes one simulation, both helpers, all managed buffers, and all Get/SetParticles calls.

Risk: mesh vertex selection may not guarantee exactly two particles per side. A dense balanced mesh can preserve distribution statistically, not necessarily bit-for-bit.

### Other alternatives

- A centered Circle/Edge/Box shape is simpler but likely permits central emissions or wrong direction; visual risk is high.
- One ParticleSystem plus four explicit `EmitParams` calls can guarantee two positions/directions per side. It still uses runtime code, but needs one simulation, one renderer, no native-to-managed copying, and no large buffers. It is simpler than the current helper.

Conclusion: a one-system visual equivalent is likely achievable. Exact stock-module equivalence is not guaranteed and must be measured rather than assumed.

## 13. TASK2_ADDITIONAL_OPTIMIZATION_CANDIDATES

All rows below are **NEW RESEARCH CANDIDATE** items, not implemented results.

| Candidate | Exact evidence / why relevant | Cost category | Impact / confidence | Measure before / tool | Controlled experiment | Visual risk / complexity | Recommendation |
|---|---|---|---|---|---|---|---|
| One authored rain system | Current design retains two simulations and ~0.76 MiB buffers for one draw | Submission, CPU, memory | Medium / High | Draws, ParticleSystem.Update, helper time, memory; Profiler/Memory Profiler | Mesh Shape or EmitParams Build C vs A/B | Medium / Medium | **A - Test now** |
| Tight rain/helper capacities | max 1000 vs burst 2; arrays directly scale from max | Memory, initialization | Medium memory; Low FPS / High | Managed/native snapshots | rain3 6, rain4 3; 100 replays | Low-medium correctness / Low | **A** |
| MVD 0.5/0.6 tiers | 0.4 already cut geometry | GPU vertex, bandwidth, trail CPU | Low-medium / Medium | Phase geometry/GPU time | 0.4/0.5/0.6 same seeds | Medium / Low | **A** |
| Shorter lines trail lifetime | Current 0.5x retains segments | Vertex, fragment, bandwidth | Medium / Medium | Geometry, coverage, GPU time | 0.5 -> 0.4 -> 0.3, MVD fixed | High / Low | **B - Android** |
| Remove ineffective rate 0.1 | Duration 1.0/0.2s; burst supplies visible count | CPU, correctness | Low / Medium | Count emitted particles | Rate 0.1 -> 0, same seed/image | Low / Trivial | **A** |
| Low-end density tier | PL3 lines 6, triangle 7, Transition zaps 40 total | CPU, vertex, fragment | Medium on low-end / Medium | Device frame/GPU/overdraw | Reduce one emitter 15-25% | High / Low | **B** |
| Central coverage reduction | Additive center layers overlap | GPU fragment | Medium if fill-bound / Low | AGI/RenderDoc fragment cost | Scale diagnostic builds -10/-20% | High / Low | **B** |
| Minimal Standard Unlit replacement | Four generic unlit materials have features off | GPU shader | Low / Medium | Shader-stage/device capture | Equivalent one-texture additive shader | Medium / Medium | **B only with evidence** |
| Remove Limit Velocity | Active damping changes motion | CPU | Low / Low | ParticleSystem.Update | One-system disable A/B | High / Low | **C/D** |
| Custom bounds | Automatic culling already active; effect short/visible | CPU/submission | Low / High | Off-screen simulation test | Move effect off-screen | Medium / Medium | **D** |
| More atlas/material sharing | Prior measured experiment failed | Submission | Negligible / High | No new hypothesis | None | Medium / Medium | **D** |
| More texture compression | Whole set ~54 KiB | Memory/bandwidth | Negligible / High | Memory/fallback only | Only if target shows fallback | Medium / Low | **D** |
| Delete unused ParticleAdditive | No active prefab reference | Build size only | Negligible / High | Build report | Separate cleanup | Low / Low | **D** |

## 14. TASK2_ANDROID_VALIDATION_PLAN

### Build matrix

- **Build A:** original `OptimizeBefore` scene and imported prefab.
- **Build B:** current `OptimizeAfter` scene and consolidator.
- **Build C:** best one-system rain alternative.

The project has automatic Android graphics APIs containing Vulkan and OpenGL ES 3. Pin one API for primary A/B/C comparison. If shipping remains automatic, repeat on both APIs.

### Controlled configuration

Keep Unity version, scripting backend, architecture, API, resolution/orientation, quality, MSAA, color space, vSync/frame cap, seed 1337, effect variant/phase, replay timing, device brightness, airplane mode, charging state, and starting thermal state identical.

Use a Development Build with Autoconnect Profiler and no Deep Profiling for final timing. Use Deep Profiling only in a separate diagnosis build because it adds overhead.

### Procedure

1. Record cold-launch/first playback separately.
2. Warm up 30-60 seconds.
3. Run at least 30 deterministic PL3 replays.
4. Capture a fixed 0.0-1.2 second window after Play.
5. Profile Transition separately because its trail workload differs.
6. Export all frame samples; report p50/p95/p99, not one selected frame.
7. Repeat each run at least three times.
8. If practical, repeat effects for 10 minutes and record thermal status/throttling.

### CPU measurements

- Main Thread and Render Thread frame time.
- `ParticleSystem.Update`.
- `ParticleSystemRendererConsolidator.LateUpdate`.
- `ParticleSystem.GetParticles` and `SetParticles`.
- Replay/reset cost.
- GC.Alloc/collections.
- First-play array allocation and later replay spikes.

Add explicit ProfilerMarkers around transfer and replay phases in a future instrumentation build; that is a proposed validation change, not part of this audit.

### Rendering/GPU

- Draw Calls, Batches, SetPass, vertices, triangles.
- Same-phase Player Frame Debugger mapping of every remaining event.
- GPU frame time, vertex time/work, fragment activity/overdraw, bandwidth, and tile load/store if available.
- Use Unity GPU Profiler, AGI for Vulkan, RenderDoc, or vendor tools as device support permits.

### Memory

Take Memory Profiler snapshots before first effect, immediately after first effect, and after 30 replays. Compare managed heap, consolidator arrays, native ParticleSystem memory, graphics texture memory, and growth/stale particles.

### Frame statistics and visual correctness

- Report median/p50, p95, p99.
- Separate first-play spike and steady replay.
- Capture fixed-phase frames near 0.05, 0.20, 0.40, and 0.70 seconds.
- Check rain count/balance, footprint, velocity, trail silhouette, brightness, end persistence, accumulation, pause/resume, disable/enable, and replay.
- Use tolerant image differences because additive particles are stochastic/driver-sensitive.

### Conclusions Editor profiling cannot prove

- Android 60 FPS or device percentile frame time.
- Android Main/Render Thread timing.
- GPU/fill-rate/bandwidth cost.
- Driver/API batching differences.
- IL2CPP allocation/GC behavior.
- ASTC residency/fallback.
- Thermal/power behavior.
- Whether one saved rain draw outweighs helper CPU/memory.

## 15. TASK2_PRIORITIZED_BACKLOG

| Priority | Optimization | Evidence | Cost Target | Expected Impact | Confidence | Measurement Needed | Risk | Recommendation |
|---:|---|---|---|---|---|---|---|---|
| 0 | Android A/B/C profiling | All final evidence is Editor-only | CPU/GPU/submission/memory | High decision value | High | Complete device suite and percentiles | Low | **MUST INVESTIGATE** |
| 1 | Replace helper with one authored rain system | Two simulations and ~0.76 MiB buffers save one small draw | Submission, CPU, memory | Medium | High | Build C vs A/B | Medium visual | **MUST INVESTIGATE** |
| 2 | Measure transparent overdraw/fill rate | Additive center layers/trails; no GPU evidence | Fragment, bandwidth | Medium if GPU-bound | Medium | AGI/RenderDoc/GPU Profiler | Low diagnostic | **MUST INVESTIGATE** |
| 3 | MVD 0.5/0.6 and trail-lifetime tiers | 0.4 produced large geometry reduction | Vertex, trail CPU, bandwidth | Low-medium | Medium | Same-phase geometry/GPU/visual | Medium | **GOOD EXPERIMENT** |
| 4 | Tighten rain capacities/helper buffers | 1000-capacity arrays copy two particles | Memory | Medium memory, Low frame | High | Managed/native snapshots | Low-medium | **GOOD EXPERIMENT** |
| 5 | Remove ineffective 0.1/s rate if count is zero | Short non-looping duration | CPU/correctness | Low | Medium | Emitted-count trace | Low | **GOOD EXPERIMENT** |
| 6 | Mobile particle-density quality tier | Lines 6, triangles 7, zaps 40 | CPU/GPU | Medium low-end | Medium | Device timing/visual A/B | High visual | **OPTIONAL** |
| 7 | Minimal shader for Standard Unlit materials | Features off but coverage small | GPU shader | Low | Medium | Shader/device capture | Medium | **OPTIONAL, conditional** |
| 8 | Active-module removal | Enabled modules visibly affect motion/shape | CPU | Low | Low | Isolated visual/timing A/B | High | **NOT WORTH without evidence** |
| 9 | More atlasing/material sharing | Already failed | Submission | Negligible | High | New hypothesis required | Medium | **NOT WORTH** |
| 10 | Further texture changes | Android payload ~54 KiB; settings already lean | Memory/bandwidth | Negligible | High | Fallback verification only | Medium quality | **NOT WORTH** |
| 11 | Custom culling bounds | Automatic culling; short visible burst | CPU | Negligible | High | Off-screen test only | Correctness | **NOT WORTH** |

## 16. TASK2_FINAL_TECHNICAL_VERDICT

The trail MVD change is the strongest final optimization. It targets the actual heaviest geometry source, produces a large absolute PL3 geometry reduction, adds no runtime code or memory, and honestly leaves draw state unchanged.

Rain consolidation is a real submission reduction, but its absolute saving is one draw containing four quads. It does not improve Batches, SetPass, geometry, or simulation count. Its two helpers add approximately 0.76 MiB managed payload, Get/SetParticles copying, lifecycle assumptions, and maintenance risk. It should be described as a provisional experiment pending device A/B/C validation, not an unqualified production win.

The Task 2 branch meets the structural home-test deliverables well: untouched baseline, separate optimized prefab, paired scenes, deterministic benchmark, final evidence, rejected experiments, and documented learnings. It does not yet prove mobile optimization or 60 FPS because no Android CPU, GPU, frame-time, memory, or thermal evidence exists.

## Key Answers

### 1. What are the 3 strongest optimizations already implemented?

1. `lines` MVD 0.2 -> 0.4, especially PL3's 410 -> 224 triangles and 460 -> 274 vertices.
2. PL2/PL3 rain renderer consolidation, which removes one verified submission while preserving 16 vertices/24 indices.
3. `transferComplete`, which removes about 98% of repeated source reads in the documented five-replay Editor test.

### 2. What are the 3 weakest / least justified optimizations?

1. The consolidator as an overall production strategy: large relative complexity/memory for one tiny draw.
2. The transfer guard: useful, but it optimizes overhead created by the consolidator rather than the imported effect.
3. PL1/PL2 MVD generalization: valid counter reductions, but small absolute workloads and no frame-time evidence.

### 3. What are the top 5 optimization experiments to try next?

1. Android A/B/C profiling.
2. One authored rain ParticleSystem using a dual-region Mesh Shape, with EmitParams as fallback.
3. Device overdraw/fill-rate measurement.
4. MVD 0.5/0.6 and controlled trail-lifetime tiers.
5. Tight rain maxParticles/helper buffers with managed/native memory snapshots.

### 4. Is ParticleSystemRendererConsolidator worth keeping?

Provisionally only. Keep it until Build B is compared with A and C on Android. Remove it if one authored system is visually acceptable or if the saved draw does not produce a meaningful device benefit.

### 5. Can its result be achieved more simply at authoring time?

Probably. A Mesh Shape can encode the two emission regions/directions in one ParticleSystem. Exact two-per-side stochastic equivalence is not guaranteed by stock modules and needs validation. A single system with four EmitParams emissions is still simpler than copying native particle state.

### 6. What measurements are still missing before claiming mobile optimization?

- Android Main/Render Thread and GPU frame time.
- ParticleSystem.Update, helper, GetParticles, and SetParticles cost.
- GC.Alloc, first-play allocation, managed/native/graphics memory.
- Draw/Batch/SetPass/geometry from the Player.
- Fragment overdraw and bandwidth.
- p50/p95/p99, first-play and repeated replay.
- Vulkan/GLES behavior, thermal behavior, fixed-phase visual equivalence, and actual 60 FPS evidence.

### 7. Which changes may improve profiler counters without meaningfully improving frame time?

- Removing the one four-quad rain Draw Call.
- Tightening maxParticles at these tiny live counts.
- Further compression/atlasing of roughly 54 KiB of textures.
- Deleting a visually important one-particle layer.
- Increasing MVD beyond the visual knee when another stage is the bottleneck.
- Replacing already-cheap Mobile Additive shaders without measured shader cost.

## Final Prioritized Table

| Priority | Optimization | Evidence | Cost Target | Expected Impact | Confidence | Measurement Needed | Risk | Recommendation |
|---:|---|---|---|---|---|---|---|---|
| 0 | Android A/B/C validation | No device evidence exists | All | High decision value | High | Full Android CPU/GPU/memory/percentiles | Low | MUST INVESTIGATE |
| 1 | Author-time rain consolidation | Current helper saves one draw with two simulations and ~0.76 MiB buffers | Submission/CPU/memory | Medium | High | A/B/C device and visual comparison | Medium | MUST INVESTIGATE |
| 2 | Transparent overdraw measurement | Multiple additive layers/trails, unmeasured | Fragment/bandwidth | Medium if bound | Medium | AGI/RenderDoc/GPU Profiler | Low diagnostic | MUST INVESTIGATE |
| 3 | Trail MVD/lifetime quality tiers | MVD 0.4 already reduced geometry materially | Vertex/fragment/trail CPU | Low-medium | Medium | Phase-matched device geometry/GPU/visual | Medium | GOOD EXPERIMENT |
| 4 | Tight maxParticles/helper buffers | 1000 capacities versus 2-20 live particles | Memory | Medium memory; Low FPS | High | Memory Profiler snapshots and replay | Low-medium | GOOD EXPERIMENT |
| 5 | Remove ineffective rate emission | 0.1/s on 0.2-1.0s effects | CPU/correctness | Low | Medium | Emitted count and image comparison | Low | GOOD EXPERIMENT |
| 6 | Low-end particle-density tier | Small but overlapping additive bursts/trails | CPU/GPU | Medium on low-end | Medium | Device A/B and visual gate | High | OPTIONAL |
| 7 | Minimal Standard Unlit shader | Features disabled; absolute coverage small | Shader/fragment | Low | Medium | Shader-stage GPU evidence | Medium | CONDITIONAL |
| 8 | More atlas/texture work | Prior atlas failed; texture payload tiny | Submission/memory | Negligible | High | New technical hypothesis required | Medium | DO NOT PURSUE |
| 9 | Custom bounds/module stripping | Automatic culling and visually meaningful active modules | CPU | Negligible-low | High | Only targeted diagnostic tests | High correctness/visual | DO NOT PURSUE |
