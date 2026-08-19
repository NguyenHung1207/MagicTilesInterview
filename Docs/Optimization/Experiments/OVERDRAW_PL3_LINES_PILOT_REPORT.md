# PL3 Lines Trail Lifetime Pilot

## Hypothesis

Reducing only `PerfectLevel3/lines` Trail Lifetime from `0.5` to `0.35`
should shorten active trail history and screen coverage. If this effect is
fragment/fill-rate bound, Build B may reduce trail fragment workload and GPU
frame time. The change may also shorten the visible silhouette or alter
brightness/readability, so visual equivalence must be demonstrated rather than
assumed.

This is a research pilot. It does not modify the production optimized prefab,
final VFX, particle assets, materials, or runtime production scripts.

## A/B Configuration

| Item | Build A | Build B |
|---|---|---|
| Isolated scene | `OverdrawPL3Lines_A.unity` | `OverdrawPL3Lines_B.unity` |
| Candidate | `PerfectLevel3/lines` | `PerfectLevel3/lines` |
| Trail Lifetime multiplier | 0.5 | 0.35 |
| Minimum Vertex Distance | 0.4 | 0.4 |
| Seed | 1337 | 1337 |
| Prefab source | same optimized prefab GUID | same optimized prefab GUID |
| Other settings | unchanged | unchanged |

MEASURED — serialized A/B audit:

- The two scene files are identical after normalizing only pilot object name and
  `trailLifetimeMultiplier`.
- Both scenes reference `ParticleEffectsOptimized.prefab` by GUID
  `3e09cc39a930c164ba6eba58c4821bad`.
- No scene override changes MVD, emission, particle lifetime, speed, size,
  width, material, texture, renderer, seed, timing, or another module.
- The experiment controller asserts MVD `0.4` at runtime and refuses to change
  it if the instantiated prefab differs.
- The production prefab working-tree Git object matches `HEAD`.

The experiment-only controller applies the lifetime value to the instantiated
scene copy. It does not write the production prefab asset.

## Visual Results

NOT MEASURED — no Unity 2022.3 editor/player session is available in this
environment. No representative A/B frames were captured.

Required visual checks remain open:

- trail length and continuity at birth, peak, and expiry;
- brightness, screen coverage, disappearance timing, and clipping;
- popping, stale trail tails, accumulation, and gameplay readability.

EXPECTED — Build B should have a shorter active trail footprint and earlier trail
disappearance. A shorter footprint can reduce motion readability or apparent
brightness. B is not called visually equivalent.

## Overdraw Results

Scene View Overdraw: NOT MEASURED. Unity is not available on PATH, and no editor
capture exists. The experiment therefore has no heatmap evidence.

Frame Debugger: NOT MEASURED. No live event list, material pass, trail geometry,
or event-level coverage capture exists for A or B.

GPU fragment/overdraw counter: NOT MEASURED. No Android device, AGI, vendor GPU
tool, or RenderDoc capture is available.

OBSERVED — the only available result is serialized configuration. It does not
prove a numeric overdraw reduction. Scene View Overdraw intensity must not be
converted directly into a GPU percentage.

## GPU Results

| Metric | Build A | Build B | Status |
|---|---:|---:|---|
| GPU frame time | NOT MEASURED | NOT MEASURED | Unity GPU Profiler unavailable |
| Fragment/overdraw workload | NOT MEASURED | NOT MEASURED | no device counter/capture |

No GPU improvement is claimed.

## CPU Results

| Metric | Build A | Build B | Status |
|---|---:|---:|---|
| CPU frame time | NOT MEASURED | NOT MEASURED | no Unity Profiler capture |
| Total frame time | NOT MEASURED | NOT MEASURED | no Unity/Android runtime |
| `ParticleSystem.Update` | NOT MEASURED | NOT MEASURED | no profiler session |

EXPECTED — changing trail lifetime alone should not materially change particle
emission or simulation CPU cost. Trail maintenance may change slightly.

## Control Metrics

| Control metric | Build A | Build B | Expected effect of lifetime-only change |
|---|---:|---:|---|
| Draw Calls | NOT MEASURED | NOT MEASURED | usually unchanged |
| Batches | NOT MEASURED | NOT MEASURED | usually unchanged |
| SetPass | NOT MEASURED | NOT MEASURED | unchanged |
| Vertices | NOT MEASURED | NOT MEASURED | may decrease with fewer live trail segments |
| Triangles | NOT MEASURED | NOT MEASURED | may decrease with fewer live trail segments |
| Particle count | NOT MEASURED | NOT MEASURED | unchanged at matched replay phase |

EXPECTED — Trail Lifetime alone should not reduce Draw Calls automatically. Any
control-metric change must be captured from the same replay phase and camera.

## Replay Validation

MEASURED — both scenes serialize seed `1337` and use the same experiment-only
deterministic replay controller. The controller assigns seeds in hierarchy order,
replays PL3, and emits the deterministic rain pair using the existing emitter.

NOT MEASURED — Unity runtime replay was not executed. Therefore the following
remain unverified for both A and B:

- no exceptions;
- no stale state or accumulation;
- no timing divergence;
- expected particle count;
- no missing VFX or gameplay regression.

## Trade-off

OBSERVED — the A/B configuration changes one serialized experiment value only.

EXPECTED benefits: lower trail persistence, lower trail screen coverage, and
possibly lower fragment work if PL3 lines are GPU/fill-rate bound.

EXPECTED costs: shorter visual trails, reduced motion/readability, altered
brightness/coverage, or an earlier-looking disappearance. Vertices/triangles may
change, but Draw Calls/Batches/SetPass should not be assumed to change.

## Recommendation

BLOCKED - RUNTIME VALIDATION UNAVAILABLE.

The A/B configuration was created successfully and remains isolated:

- Build A Trail Lifetime = `0.5`.
- Build B Trail Lifetime = `0.35`.
- Only Trail Lifetime differs.
- No production VFX settings were changed.

Current evidence status:

- Visual result = NOT MEASURED.
- Overdraw = NOT MEASURED.
- GPU = NOT MEASURED.
- CPU = NOT MEASURED.
- Draw Calls / Batches / SetPass / Vertices / Triangles = NOT MEASURED.
- Runtime replay = NOT MEASURED.
- Deterministic configuration was verified statically.

No performance improvement is claimed. No visual equivalence is claimed. Do not
promote Build B or change Trail Lifetime in production.

## Next Step

Repeat the experiment when Unity 2022.3 runtime and GPU/profiling access are
available.

## Limitations

- Unity executable not found in the environment.
- RenderDoc CLI and AGI not found.
- `adb devices` reports no connected Android device.
- No representative A/B screenshots or video were captured.
- No Scene View Overdraw, Frame Debugger, GPU Profiler, CPU Profiler, or Android
  GPU capture was performed.
- No numeric GPU, fragment, frame-time, or control-metric result exists.
- Serialized equality is not visual or performance equivalence.

Final status: BLOCKED - RUNTIME VALIDATION UNAVAILABLE. The experiment remains
isolated, and no promotion or abandonment decision is justified without runtime
and device evidence.
