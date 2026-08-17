# Task 2 — UI/VFX Optimization

## Objective and scope

Optimize the imported UI/VFX package for a mobile-targeted project without changing the Task 1 game. The original source remains untouched:

- Before: `Assets/Optimization/Scenes/OptimizeBefore.unity`
- Original prefab: `Assets/0_Mep/General/Prefabs/Gameplay/GameplayEffect/ParticleEffectUnOptimizeExport/ParticleEffectsUnoptimize.prefab`
- After: `Assets/Optimization/Scenes/OptimizeAfter.unity`
- Final prefab: `Assets/Optimization/Prefabs/ParticleEffectsOptimized.prefab`

Unity 2022.3.62f2, Built-in Render Pipeline, a 1080 × 1920 comparison view, and deterministic seed 1337 were used. Editor measurements are development evidence only. Android frame time, GPU time, thermal behavior, and 60 FPS have not been measured.

## Before profiling evidence

| Variant | Draw Calls | Batches | SetPass Calls |
|---|---:|---:|---:|
| Transition | 6 | 6 | 6 |
| PerfectLevel1 | 7 | 7 | 7 |
| PerfectLevel2 | 9 | 8 | 8 |
| PerfectLevel3 | 9 | 8 | 7 |

PL3 tied for the highest Draw Call count and produced the heaviest observed trail geometry.

![Before PL3 draw state](FinalEvidence/before_pl3_draw_calls.png)

The selected baseline draw frame measured 9 Draw Calls, 8 Batches, and 7 SetPass Calls. A separate peak/comparable geometry frame measured 410 triangles and 460 vertices:

![Before PL3 geometry](FinalEvidence/before_pl3_geometry.png)

The two mirrored rain renderers generated two draw events while rendering four quads total:

![Before PL3 rain](FinalEvidence/before_pl3_rain_frame_debugger.png)

**MEASURED:** 2 rain Draw Calls, 16 vertices, 24 indices, 8 triangles.

## Issues found

- PL1/PL2/PL3 `lines` trails were tessellated more densely than their visual quality required.
- PL2/PL3 used separate compatible `rain3` and `rain4` renderers for four small additive particles.
- The first rain consolidation reduced a submission but retained duplicate simulation and introduced particle-copy complexity.
- Transition `zap1`/`zap2` appeared mergeable, but their independent trail histories produced invalid connecting geometry when combined.
- Material sharing/atlas and mask-normalization experiments did not reduce measured submissions.

## Optimization history

### Original rain

```text
rain3: simulation + renderer
rain4: simulation + renderer
```

Result: two measured rain Draw Calls.

### First rain optimization — Build B

Build B retained both simulations, disabled the `rain4` renderer, and used `ParticleSystemRendererConsolidator` to copy source particles into `rain3`.

Result: rain Draw Calls reduced from 2 to 1, with the same 16 vertices/24 indices. Batches and SetPass Calls did not improve.

![Historical Build B rain event](FinalEvidence/build_b_pl3_rain_frame_debugger.png)

Further audit found that Build B:

- still ran both native ParticleSystem simulations;
- held six simulated records for four visible particles after transfer;
- called source/target `GetParticles` and target `SetParticles`;
- depended on one-shot transfer and replay lifecycle assumptions;
- allocated arrays with capacities of 2,000 target particles, 1,000 source particles, and 1,000 seeds per helper;
- used a **CALCULATED** 400,000-byte array-element payload per helper, or 800,000 bytes across PL2 and PL3. Array object/alignment overhead is excluded.

Build B worked and was visually correct, but it spent architecture, native-to-managed copying, and memory to save one very small draw.

### Follow-up research — Build C

A simpler architecture was tested independently before promotion:

```text
rainCombined: one simulation + one renderer
DeterministicRainEmitter: four replay-time EmitParams calls
```

The emitter produces exactly two particles per side. Fixed benchmark seeds use compatibility samples recovered from the original Unity Shape output. Other seeds use an allocation-free deterministic value-type PRNG within the original position, arc, speed, lifetime, size, and rotation domains.

Validation before promotion included:

- PL2 and PL3: 100/100 fixed-seed deterministic replays each;
- 256 additional non-preset seed pairs and 1,024 particle samples;
- exact four particles and 2/2 side balance;
- no center leakage, stale state, accumulation, non-finite values, or exceptions;
- zero recurring allocation in warmed emitter tests;
- seven of eight fixed-phase A/C captures pixel-identical, with a negligible five-RGB-byte difference in the remaining PL2 frame;
- comparable general-seed coverage, luminance, centroid, and footprint.

### Final rain optimization — promoted Build C

The final PL2 and PL3 prefab hierarchy now contains `rainCombined` only:

- one ParticleSystem simulation;
- one enabled billboard renderer;
- four simulated and visible particles;
- `maxParticles = 8`;
- no hidden source system or `rain4`;
- no `GetParticles`, `SetParticles`, particle-copy arrays, or `LateUpdate` polling;
- retained Size over Lifetime, Rotation over Lifetime, Color over Lifetime, and Limit Velocity;
- retained `triangle_ 1.mat`, `Mobile/Particles/Additive`, SrcAlpha/One, ZWrite Off.

**MEASURED in the supplied live Frame Debugger capture:** one `Draw Dynamic rainCombined` event, 1 Draw Call, 16 vertices, 24 indices, and 8 triangles.

The final `OptimizationBenchmarkRunner` explicitly emits rain after deterministic hierarchy seed assignment. Removing `rain4` does not shift unrelated sequences: PL2 rain remains 1355/1356, PL2 glow remains 1357, PL3 root remains 1358, and PL3 rain remains 1365/1366.

## Trail geometry optimization

PL1, PL2, and PL3 `lines` retain:

```text
Trails > Minimum Vertex Distance: 0.2 → 0.4
```

This reduces trail tessellation without runtime code. Values above 0.4 were not promoted because coarser segmentation requires another visual-quality gate.

| Variant | Before triangles | After triangles | Before vertices | After vertices |
|---|---:|---:|---:|---:|
| PL1 | 126 | 86 | 164 | 124 |
| PL2 | 196 | 156 | 240 | 200 |
| PL3 | 410 | 224 | 460 | 274 |

The PL3 reductions are 45.4% triangles and 40.4% vertices in the controlled comparison frames.

## Final validation from promoted assets

The final prefab and `OptimizeAfter.unity` were validated again after promotion, rather than relying only on experiment assets.

| Check | PL2 | PL3 |
|---|---:|---:|
| Fixed-seed replay cycles | 100/100 PASS | 100/100 PASS |
| Additional promoted-final seed pairs | 32 PASS | 32 PASS |
| Rain particles | 4 | 4 |
| Left/right | 2/2 | 2/2 |
| Recurring allocation over 100 warmed calls | 0 bytes | 0 bytes |
| Center leakage / stale / accumulation / exception | None | None |

Transition, PL1, PL2, and PL3 all replayed through the promoted final runner. The final scene contained zero missing MonoBehaviours, all three trail MVD values remained 0.4, and the seed slots above were preserved. Machine-readable evidence is in `FinalEvidence/final_promotion_validation.json`.

Promoted-final rain-only visual captures:

- `FinalEvidence/after_pl2_rainCombined_0.20s.png`
- `FinalEvidence/after_pl3_rainCombined_0.20s.png`

## CPU and memory evidence

The pre-promotion Editor microbenchmark compared 10,000 warmed calls:

| Operation | Result | Evidence class |
|---|---:|---|
| Build B full transfer | 4.165 µs average | EDITOR MICROBENCHMARK |
| Build C clear + four emits | 0.873 µs average | EDITOR MICROBENCHMARK |
| Build B completed-transfer polling | 0.052 µs average | EDITOR MICROBENCHMARK; near timer noise |
| Build C recurring allocation | 0 bytes | MEASURED warmed Editor calls |
| Build B helper payload, PL2 + PL3 | 800,000 bytes | CALCULATED from capacities and 132-byte Particle struct |
| Build C equivalent copy-buffer payload | 0 bytes | VERIFIED BY CODE |

These microsecond results are too small to claim an FPS improvement and are not Unity Profiler marker or Android timings. `ParticleSystem.Update` has not been isolated on device.

## Final rendering results

Fresh live Game View totals from the promoted `OptimizeAfter.unity` scene were:

| Variant | Draw Calls | Batches | SetPass Calls |
|---|---:|---:|---:|
| PL2 | 8 | 8 | 8 |
| PL3 | 8 | 8 | 7 |

These promoted-final totals match the historical Build B totals. The automated run recorded PL2 at 140 vertices/94 triangles and PL3 at 252 vertices/198 triangles in those particular live frames; trail geometry is phase-dependent, so the dedicated historical geometry comparison remains the controlled peak/comparable reference. The final rain event itself was measured live as stated above. Machine-readable promoted-final counters are in `FinalEvidence/final_live_render_validation.json`.

The retained historical after frames are useful for the trail and whole-effect comparison:

![Historical after PL3 draw state](FinalEvidence/after_pl3_draw_calls.png)

![Historical after PL3 geometry](FinalEvidence/after_pl3_geometry.png)

## Rejected/reverted experiments

- Transition zap consolidation reduced Draw Calls but connected independent trail histories; rejected and reverted.
- Shared material/atlas and mask normalization did not reduce measured submissions; rejected.
- Disabling `init`, `glow`, or `outline_line` reduced a draw but visibly weakened the effect; rejected.
- Build B rain consolidation was superseded after its simulation, copy, memory, and lifecycle costs were quantified.

## Evidence classification

- **MEASURED:** fixed/general replay outcomes, emitted counts, warmed allocation, original/final rain Frame Debugger event values supplied from the live capture, and historical Editor counters shown in screenshots.
- **CALCULATED:** Build B's 800,000-byte array-element payload.
- **EDITOR MICROBENCHMARK:** Build B transfer and Build C emission timings.
- **EXPECTED:** none of the supplied live rain event or whole-variant counter values. The automated Frame Debugger event lookup did not export a renderer event, so the repository records the separately supplied live rain capture rather than treating automated zeros as measurements.
- **ANDROID NOT MEASURED:** CPU/GPU frame time, render-thread cost, memory snapshots, percentiles, thermal behavior, and 60 FPS.

## Learnings

- Draw Calls, Batches, SetPass Calls, geometry, simulation CPU, allocations, and memory are independent metrics.
- Improving a profiler counter is not enough when the implementation adds avoidable simulation, copying, memory, and lifecycle risk.
- The first correct optimization can still be superseded by a simpler architecture after deeper validation.
- Trail topology tuning produced a larger absolute workload reduction than removing one four-quad submission.
- Deterministic replay and fixed-phase images made architectural A/B/C comparisons trustworthy without treating Editor evidence as mobile proof.

## AI usage

AI assistance was used to inspect serialized assets, reconstruct experiments, design and validate Build C, review metrics, and update documentation. Claims were retained only when supported by serialized state, deterministic validation, supplied Unity captures, or clearly labeled calculations/microbenchmarks.
