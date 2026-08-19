# MaxParticles Research

## Objective

Determine whether the final optimized prefab has unnecessarily high particle
capacities and whether lowering them is a worthwhile Task 2 follow-up.

Scope: `Assets/Optimization/Prefabs/ParticleEffectsOptimized.prefab` only.
No project asset was changed for this research.

## Serialized Findings

`maxNumParticles` is Unity's serialized representation of Main Module
`maxParticles` in this prefab. All systems have `looping: 0`, `prewarm: 0`,
and a zero rate-over-time (the serialized `minScalar` is inactive because the
rate curve mode is Constant). Each listed burst has probability 1.

`Life` and `Speed` use the authored constant/range. `Trail` is
`off`, or `on (lifetime multiplier)`.

| Hierarchy path | System | Max | Duration / loop | Life; speed | Emission | Trail |
|---|---|---:|---|---|---|---|
| `Transition` | Transition | 1000 | 1 s / no | 1; 0 | disabled | off |
| `Transition/outline_circle` | outline_circle | 1000 | 1 s / no | 0.4; 0 | burst 1 at 0 | off |
| `Transition/init` | init | 1000 | 1 s / no | 0.3; 0 | burst 1 at 0 | off |
| `Transition/zap1` | zap1 | 1000 | 0.2 s / no | 0.3; -150 | 20 x burst 1, 0.01 s | on (1) |
| `Transition/zap2` | zap2 | 1000 | 0.2 s / no | 0.3; -150 | 20 x burst 1, 0.01 s | on (1) |
| `PerfectLevel1` | PerfectLevel1 | 1000 | 1 s / no | 1; 0 | disabled | off |
| `PerfectLevel1/outline_line` | outline_line | 1000 | 1 s / no | 0.5; 0 | burst 1 at 0 | off |
| `PerfectLevel1/outline_circle` | outline_circle | 1000 | 1 s / no | 0.5; 0 | burst 1 at 0 | off |
| `PerfectLevel1/glow` | glow | 1 | 1 s / no | 1; 0 | burst 1 at 0 | off |
| `PerfectLevel1/lines` | lines | 1000 | 2 s / no | 0.4; 30 | burst 4 at 0 | on (0.5) |
| `PerfectLevel1/triangle` | triangle | 30 | 1 s / no | 0.3-1; 50-100 | burst 7 at 0 | off |
| `PerfectLevel1/init` | init | 1000 | 1 s / no | 0.3; 0 | burst 1 at 0 | off |
| `PerfectLevel2` | PerfectLevel2 | 1000 | 1 s / no | 1; 0 | disabled | off |
| `PerfectLevel2/rainCombined` | rainCombined | 8 | 1 s / no | 0.4-0.6; 50-100 | module disabled; scripted 4 | off |
| `PerfectLevel2/outline_circle` | outline_circle | 1000 | 1 s / no | 0.5; 0 | burst 1 at 0 | off |
| `PerfectLevel2/outline_line` | outline_line | 1000 | 1 s / no | 0.5; 0 | burst 1 at 0 | off |
| `PerfectLevel2/glow` | glow | 1 | 1 s / no | 1; 0 | burst 1 at 0 | off |
| `PerfectLevel2/lines` | lines | 1000 | 2 s / no | 0.7; 50 | burst 4 at 0 | on (0.5) |
| `PerfectLevel2/triangle` | triangle | 30 | 1 s / no | 0.3-1; 50-100 | burst 7 at 0 | off |
| `PerfectLevel2/init` | init | 1000 | 1 s / no | 0.3; 0 | burst 1 at 0 | off |
| `PerfectLevel3` | PerfectLevel3 | 1000 | 1 s / no | 1; 0 | disabled | off |
| `PerfectLevel3/rainCombined` | rainCombined | 8 | 1 s / no | 0.4-0.6; 50-100 | module disabled; scripted 4 | off |
| `PerfectLevel3/outline_circle` | outline_circle | 1000 | 1 s / no | 0.5; 0 | burst 1 at 0 | off |
| `PerfectLevel3/outline_line` | outline_line | 1000 | 1 s / no | 0.5; 0 | burst 1 at 0 | off |
| `PerfectLevel3/glow` | glow | 1 | 1 s / no | 1; 0 | burst 1 at 0 | off |
| `PerfectLevel3/lines` | lines | 1000 | 2 s / no | 0.7; 50 | burst 6 at 0 | on (0.5) |
| `PerfectLevel3/triangle` | triangle | 30 | 1 s / no | 0.3-1; 50-150 | burst 7 at 0 | off |
| `PerfectLevel3/init` | init | 1000 | 1 s / no | 0.3; 0 | burst 1 at 0 | off |

Relevant count-affecting modules: no system enables sub-emitters, rate-over-
distance, or Lifetime by Emitter Speed. Trails are enabled only for `lines`
and `zap*`; their multiplier is at most 1. **MEASURED (serialized):** their
serialized `dieWithParticles` value is `0` (disabled). Therefore trail history
is not forced to end with the particle; a post-particle trail tail may remain
until the trail lifetime expires. This is serialized configuration evidence,
not a runtime timing or visual measurement.

## Peak-Concurrency Analysis

**PROJECT OBSERVATION:** `OptimizationBenchmarkRunner.Replay` stops emitting
and clears every cached system, then plays only the selected root next frame.
Therefore repeated benchmark replays do not accumulate particles.

**PROJECT OBSERVATION:** `DeterministicRainEmitter` clears `rainCombined` and
calls `Emit` exactly four times (two per side) for PL2 and PL3. Its disabled
serialized emission module does not add particles.

**INFERENCE:** For a one-shot burst, the conservative live peak is its burst
count when all particles are born together. For the zaps, 20 emissions occur
within 0.19 s and each lives 0.3 s, so all 20 can overlap. Lifetime modules
shown above do not increase particle count.

| System | Serialized maximum | Estimated peak live particles | Basis |
|---|---:|---:|---|
| Roots (all four) | 1000 | 0 | emission disabled |
| outline_circle / outline_line (7) | 1000 | 1 each | one one-shot burst |
| init (4) | 1000 | 1 each | one one-shot burst |
| glow (3) | 1 | 1 each | already exact |
| PL1 / PL2 / PL3 lines | 1000 | 4 / 4 / 6 | one burst; Trails do not create particles |
| triangle (3) | 30 | 7 each | one burst |
| rainCombined (2) | 8 | 4 each | exact scripted replay emission |
| zap1 / zap2 | 1000 | 20 each | 20 overlapping scheduled bursts |

These are configuration-derived estimates, not measured `particleCount` peaks.

## Candidate Systems

| System | Current maxParticles | Estimated Peak | Suggested Test Cap | Safety Margin | Risk |
|---|---:|---:|---:|---|---|
| outline_circle / outline_line (7) | 1000 | 1 | 2 | 1 particle | A: simple, one burst |
| init (4) | 1000 | 1 | 2 | 1 particle | A: simple, one burst |
| triangle (3) | 30 | 7 | 10 | 3 particles | A: randomized life but fixed burst |
| PL1 / PL2 lines | 1000 | 4 | 8 | 4 particles | B: confirm Trails visuals |
| PL3 lines | 1000 | 6 | 10 | 4 particles | B: confirm Trails visuals |
| zap1 / zap2 | 1000 | 20 | 32 | 12 particles | B: dense burst and Trails |
| Roots (4) | 1000 | 0 | unchanged | n/a | C: cleanup-only |
| glow (3) | 1 | 1 | unchanged | 0 | C: already exact |
| rainCombined (2) | 8 | 4 | unchanged | 4 particles | C: intentional 2x scripted headroom |

Classification totals: **A = 14**, **B = 5**, **C = 9**.

## Unity 2022.3 Technical Findings

- **DOCUMENTED:** Main Module Max Particles is the maximum number of particles
  in the system at one time. When it is reached, Unity removes particles.
  [Unity Main Module](https://docs.unity3d.com/2022.3/Manual/PartSysMainModule.html)
- **DOCUMENTED:** `particleCount` is the current count and excludes child
  systems. It is the right direct measurement for each system, not a hierarchy
  total. [Unity ParticleSystem API](https://docs.unity3d.com/2022.3/ScriptReference/ParticleSystem.html)
- **DOCUMENTED:** A Trail is attached to a particle; its vertex lifetime is a
  multiplier of that particle's lifetime. Trails change geometry/work, not the
  number reported as ParticleSystem particles. [Unity Trails module](https://docs.unity3d.com/2022.3/Manual/PartSysTrailsModule.html)
- **DOCUMENTED:** Unity caches some particle rendering buffers on graphics APIs
  that use pre-mapped buffers; this cache can grow after a large visible count
  and is separate from `maxParticles`. Modern DX12, Vulkan, and Metal do not
  use that pre-allocated pool. [Unity buffer API](https://docs.unity3d.com/2022.3/ScriptReference/ParticleSystem.SetMaximumPreMappedBufferCounts.html)
- **INFERENCE:** `maxParticles` is an admission limit, not evidence that the
  system simulates or draws that many particles. If actual `particleCount` and
  visible particles stay unchanged, reducing the limit should not reduce normal
  simulation CPU work or GPU draw/fragment work.
- **REQUIRES MEMORY PROFILER:** Unity does not document that per-system native
  particle storage is eagerly allocated at `maxParticles`, nor its growth
  policy. Do not claim a native-memory saving until A/B snapshots show it.
- **REQUIRES MEMORY PROFILER:** A smaller maximum can reduce native memory only
  if it changes retained/reserved particle-system allocations on the target
  Unity version/device. Managed memory should not materially change: no managed
  particle buffers are sized from these caps in the final prefab scripts.

## Risks

- A cap below the true peak removes particles, changing the effect rather than
  merely saving memory.
- Trail-enabled systems need screenshot/frame-debugger comparison because fewer
  admitted particles also mean less trail geometry.
- Runtime calls outside the benchmark, changed seeds, or a future emission
  change can invalidate a cap derived from this prefab and current replay code.
- A Memory Profiler result can vary with warm-up, graphics API, and renderer
  buffer-cache history.

## Measurement Plan

1. Establish a warmed Android baseline (A) using the current prefab. Replay the
   selected variant with fixed `baseRandomSeed`; capture PL1, PL2, PL3, and
   Transition separately. Use the `R` replay path for deterministic repeats.
2. Create an uncommitted local B copy and change **only** the proposed
   `maxParticles` values. Test A candidates first; test each B candidate
   independently if an A/B difference needs attribution.
3. For every system, record the maximum sampled `particleCount`; for each
   variant, record the hierarchy sum as well. Capture frames at burst start,
   system peak, and after all particles/trails have expired.
4. Take comparable post-warm-up Memory Profiler snapshots. Compare native
   particle-related/reserved memory and total managed memory.
5. In Unity Profiler and Rendering stats/Frame Debugger, record CPU
   `ParticleSystem.Update`, frame time, Draw Calls, Batches, and SetPass.
6. Compare deterministic screenshots/replays, including both zaps in
   Transition and scripted rain in PL2/PL3. Reject B on any visible loss or
   changed replay count.

Expected not to change when B never reaches its cap: particleCount peak,
CPU simulation time, frame time, Draw Calls, Batches, SetPass, and visual
output. GPU work is likewise not expected to improve with an unchanged visible
particle count. Managed memory is expected not to change materially.

## Expected Impact (NOT MEASURED)

**EXPECTED (hypothesis only):** this is primarily a possible native-memory /
capacity-cleanup experiment. There is no configuration evidence for a CPU or
GPU win at equal live and visible counts. The large 1000-to-2/8/10/32 gaps make
it worth measuring, but the current effects have low configuration-derived
concurrency and the performance impact may be zero. No runtime memory or
performance improvement is claimed.

## Recommendation

Do not implement a cap reduction before Android profiling. Preserve the already
tight glow and rain caps. After baseline Android profiling, run the isolated
A/B experiment: prioritize the 14 A systems; measure the five Trail/dense B
systems only if native memory is relevant or A proves a real saving.

## Limitations

No runtime Android capture, `particleCount` sample, Memory Profiler snapshot,
or Frame Debugger comparison was performed in this research. Serialized values
and current benchmark code cannot establish Unity's native allocation policy or
prove visual equivalence under every runtime path.

## Final Verdict

1. Worth testing: seven outlines, four init systems, and three triangles (A);
   then PL1/PL2/PL3 lines and zap1/zap2 (B) if measurement warrants it.
2. Do not touch: four roots, three glows, and two `rainCombined` systems.
3. This is an **EXPECTED** possible capacity cleanup, not a measured memory,
   CPU, or GPU optimization at unchanged live counts.
4. Missing evidence: device `particleCount` peaks, native-memory A/B snapshots,
   CPU/GPU/rendering counters, and deterministic visual comparisons.
5. Do not implement before Android profiling; profile first, then decide from
   the measured native-memory delta.
