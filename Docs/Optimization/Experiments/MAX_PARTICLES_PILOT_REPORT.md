# MaxParticles Pilot

## Candidate

- `ParticleEffectsOptimized/Transition/init`
- **MEASURED (serialized):** `maxParticles = 1000`, one burst of one particle at time zero, 0.3 s lifetime, no Trails, no sub-emitters.
- **CALCULATED:** peak live-particle count is one. This is not runtime measurement evidence.

## A/B Configuration

| Build | Runtime clone configuration | Status |
|---|---|---|
| A | Candidate unchanged; `maxParticles = 1000` | MEASURED (source review) |
| B | Same clone, with only `main.maxParticles = 2` | MEASURED (harness source review) |

The source prefab is never saved or changed. The harness uses the same fixed seed for A and B; relative to A, B changes only `maxParticles`.

`Docs/Optimization/Archive/MaxParticlesPilotBatchMeasurement.cs` is the
archived runner source that was prepared to run 120 clear/reseed/simulate
replays per build in a compatible Unity editor. It records particle-count
min/max/average, cap hits, one-particle validation, repeat state, and
stale-particle checks. The runner was not executed successfully in Unity
2022.3, so this report remains `BLOCKED` and contains no runtime result.

## Measured Peak Particle Count

| Metric | A: 1000 | B: 2 | Evidence |
|---|---:|---:|---|
| Replays completed | 0 | 0 | NOT MEASURED |
| Peak minimum | Not measured | Not measured | NOT MEASURED |
| Peak maximum | Not measured | Not measured | NOT MEASURED |
| Peak average | Not measured | Not measured | NOT MEASURED |
| Distribution | Not measured | Not measured | NOT MEASURED |
| Replays reaching/exceeding cap | Not measured | Not measured | NOT MEASURED |

**NOT MEASURED:** the local project requires Unity `2022.3.62f2`. Only Unity `6000.3.15f1` is installed. An isolated temporary-project import was attempted, but Unity 6000 began upgrading project packages and did not execute the batch measurement. The process was stopped; no result file was produced. Unity 6000 would not be valid evidence for this Unity 2022.3 pilot.

## Replay Correctness

- **NOT MEASURED:** no 120-replay A or B run completed in Unity 2022.3.
- **CALCULATED:** one configured burst has one emitted particle.
- **INFERENCE:** a cap of two should admit that particle with one spare slot.
- **NOT MEASURED:** expected-particle appearance, missing particles, premature cut-off, accumulation, stale particles, exceptions, and deterministic state have not been observed in a compatible runtime.

## Visual Comparison

- **NOT MEASURED:** no A/B screenshots or rendered-frame comparisons exist.
- **INFERENCE:** if neither build reaches its cap, changing only the admission limit should not alter the generated particle state or rendered output.
- **NOT MEASURED:** this inference needs representative A/B captures in Unity 2022.3, including burst and expiry frames.

## Memory

| Metric | A: 1000 | B: 2 | Evidence |
|---|---:|---:|---|
| ParticleSystem native memory | Not measured | Not measured | NOT MEASURED |
| Total native memory | Not measured | Not measured | NOT MEASURED |
| Managed heap | Not measured | Not measured | NOT MEASURED |
| Graphics memory | Not measured | Not measured | NOT MEASURED |

**NOT MEASURED:** the Unity Memory Profiler was not available in a compatible Unity 2022.3 session. No memory saving is claimed.

## CPU

| Metric | A: 1000 | B: 2 | Evidence |
|---|---:|---:|---|
| `ParticleSystem.Update` | Not measured | Not measured | NOT MEASURED |
| Total frame time | Not measured | Not measured | NOT MEASURED |

The runtime harness can record the named profiler marker when it is available, but no compatible editor/device run completed. No Stopwatch or unrelated timing is used as substitute evidence.

## Rendering

| Metric | A: 1000 | B: 2 | Evidence |
|---|---:|---:|---|
| Draw Calls | Not measured | Not measured | NOT MEASURED |
| Batches | Not measured | Not measured | NOT MEASURED |
| SetPass | Not measured | Not measured | NOT MEASURED |
| Vertices / triangles | Not measured | Not measured | NOT MEASURED |

**INFERENCE:** these control metrics should remain unchanged if B admits the same one visible particle. Confirm with Frame Debugger/Rendering Stats.

## Interpretation

The A/B configuration is valid and isolated, but it has not produced actual Unity 2022.3 measurements. The serialized cap gap is large; that alone is not evidence of native-memory, CPU, GPU, or rendering savings.

## Recommendation

**BLOCKED — RUNTIME VALIDATION UNAVAILABLE**

- A = `maxParticles 1000`.
- B = `maxParticles 2`.
- Runtime replay = `0/120` because Unity 2022.3 was unavailable.
- Peak `particleCount` = **NOT MEASURED**.
- Memory = **NOT MEASURED**.
- CPU = **NOT MEASURED**.
- Rendering = **NOT MEASURED**.
- No performance improvement is claimed.
- No production change is recommended.

## Next Step

Repeat the A/B experiment only when the Unity 2022.3 runtime is available.
