# NEON TILES

NEON TILES is a portrait-oriented Unity rhythm game created for the UPLIVE Unity Developer home test. It combines a data-driven four-song library, preprocessed MIDI charts, DSP-clock timing, touch/mouse input, and lightweight neon presentation.

## Overview

- Engine: Unity `2022.3.62f2`
- Primary target: Android, portrait orientation
- Entry scene: `Assets/Scenes/MainMenu.unity`
- Gameplay scene: `Assets/Scenes/Gameplay.unity`
- Input: mouse in the Unity Editor; touch on mobile

The repository contains the Unity project source. Generated builds under `Builds/` are local artifacts and are not part of the project source.

## Features

- Four selectable songs with catalog-driven audio, chart, cover, and background-theme data
- Four-lane tap gameplay with Good, Great, Perfect, and Miss judgements
- Score, Perfect-streak combo, lane feedback, pooled hit bursts, and failure feedback
- Video-backed menu and gameplay themes with a readiness/fallback gate
- Explicit loading, ready, and tap-to-start flow
- Pause, Resume, Restart, Home, Win, and Game Over flows
- One-shot UI, failure, victory, and game-over SFX
- Safe-area-aware portrait UI

## How to Run

1. Clone or download the repository.
2. Open the project in Unity `2022.3.62f2`.
3. Open `Assets/Scenes/MainMenu.unity`. It is also the first enabled scene in Build Settings; `Assets/Scenes/Gameplay.unity` is second.
4. Enter Play Mode.
5. Select a song with its **PLAY** button.
6. Wait while the gameplay background prepares.
7. When the **START** tile appears on the third visual lane, click or tap that tile.
8. Tap notes as they reach the lane hit points.

All required Unity packages are recorded in `Packages/manifest.json`; no separate package-install step is expected.

## Controls

| State | Editor | Mobile |
|---|---|---|
| Select a song | Mouse click | Touch |
| Ready to start | Click the START tile | Touch the START tile |
| Play | Click a note | Touch a note |
| Pause | Click the pause button | Touch the pause button |
| Pause menu | Resume, Restart, or Home buttons | Resume, Restart, or Home buttons |
| Result screen | Retry or Home buttons | Retry or Home buttons |

The START tile is on the third visual lane (zero-based lane index `2`). It must be tapped directly; clicking elsewhere during the ready state neither starts the song nor counts as failed gameplay input.

## Gameplay Rules

- Each chart contains single tap notes assigned to four lanes.
- A note is judged from its position relative to the lane hit point when tapped.
- Missing a note ends the run and opens Game Over.
- Tapping an invalid location during active gameplay also ends the run.
- A run is won after every chart note has been hit successfully and the song has finished.
- Retry reloads Gameplay while preserving the selected song; Home returns to MainMenu.
- Production gameplay does not include hold notes.

## Scoring and Judgement

| Judgement | Score | Combo behavior |
|---|---:|---|
| Perfect | +100 | Increases by 1 |
| Great | +75 | Resets to 0 |
| Good | +50 | Resets to 0 |
| Miss | +0 | Resets to 0 and ends the run |

The displayed combo therefore represents consecutive Perfect hits. Result screens present the final score; no persistent best-score or judgement-statistics system is implemented.

## Songs and Selection

`Assets/GameData/Songs/Catalogs/MainSongCatalog.asset` currently contains:

| Display name | Song ID |
|---|---|
| Demo Song | `demo_song` |
| Sonata 01 | `sonata_01` |
| Sonata 02 | `sonata_02` |
| Sakura | `sakura` |

Each `SongDefinition` supplies its audio clip, generated JSON chart, cover sprite, and background theme. `MainMenuController` binds the catalog to `SongCardView` instances, and `SelectedSongContext` carries the chosen definition into Gameplay without hardcoded per-song loading branches.

## Song Selection and Startup Flow

The gameplay startup sequence is:

```text
MainMenu selection
-> Gameplay scene and song configuration
-> background video Prepare()
-> first usable video frame (or fallback)
-> Ready state and START tile
-> user tap
-> scheduled music and chart timeline
-> normal spawning and input
```

`DynamicBackgroundController` reports readiness after the configured `VideoPlayer` has prepared, playback has begun, and a frame-ready callback has arrived. A one-shot warning plus a 10-second timeout/error fallback prevents a decorative video failure from trapping the player on Loading. The background loop is independent of the song and is not restarted by the START tap.

## Project Architecture

The main song-data path is:

```text
SongCatalog
-> SongDefinition
-> SelectedSongContext
-> GameplaySongLoader
-> SongConductor + JsonChartLoader
-> NoteSpawner
```

Key responsibilities are deliberately small:

- `GameplayStartupController` gates Loading, Ready, and Playing states.
- `StartTileController` owns only the pre-game tile presentation and direct collider input; it is not a chart `Note`.
- `SongConductor` owns the musical clock and audio playback.
- `NoteSpawner` creates chart notes against that clock; `NoteInputController` resolves mouse/touch input.
- `GameplayController` owns terminal Win/Game Over state and restart.
- `GameplayHUD`, `PauseMenuController`, and `ResultScreenController` present session UI.
- `SfxController` is a duplicate-safe persistent one-shot SFX service.

## MIDI / Chart Pipeline

MIDI is processed in the Unity Editor rather than parsed during gameplay:

```text
MIDI source (`Assets/GameData/Source/Midi/`)
-> MidiNoteExtractor
-> ChartBuilder
-> ChartValidator and import report
-> JSON (`Assets/GameData/Generated/Charts/`)
-> JsonChartLoader at runtime
```

`MidiChartImporter` uses DryWetMIDI to read note timing from the source files. The builder sorts notes, removes exact tick/pitch duplicates, groups simultaneous notes, and selects one representative note per MIDI tick. It prefers the configured melody track (track index `1`) and otherwise uses the highest pitch as a deterministic top-voice fallback.

Representative pitches are divided by chart-specific quartile thresholds into four pitch regions. A deterministic playability pass then limits repeated-lane runs, helps underrepresented lanes, and avoids unnecessary direction changes or large jumps for nearby pitches. `ChartValidator` checks lane bounds, ordering, uniqueness, timing, and chart/build consistency before JSON is accepted.

Preprocessing keeps runtime input deterministic, moves malformed-chart detection into authoring time, and avoids shipping MIDI interpretation logic in the gameplay path. Human-readable import reports remain under `Assets/GameData/Reports/`.

## Timing and Synchronization

`SongConductor` derives logical `SongTime` from `AudioSettings.dspTime`. The song is configured when Gameplay loads but is not scheduled until `StartSong()` succeeds after the START-tile tap. Music is scheduled slightly ahead on the DSP clock, and spawning/movement read the same centralized timeline.

Pause captures the current logical song time and pauses the music source. Resume rebases the DSP start time from the captured position before unpausing audio. This prevents DSP time spent in the pause menu from advancing notes or causing a timing jump, without using `Time.timeScale` as the rhythm clock.

## Gameplay Feedback and Performance Choices

- Hit bursts are pre-created in a fixed pool and cleared before reuse; successful hits do not instantiate/destroy particle hierarchies.
- Good, Great, and Perfect use progressively stronger burst and lane-flash settings.
- Miss and invalid input share one failure SFX but use distinct flash/shake tuning.
- Short SFX use one cached `AudioSource` with `PlayOneShot`; music volume is left independent.
- Result and pause transitions use lightweight coroutines and unscaled UI time.
- Production runtime folders contain no routine per-note `Debug.Log`/`print` calls; remaining logs report actionable setup, parsing, or video failures.
- Demo Song, Sonata 01, and Sonata 02 reuse one gameplay video with different overlays; Sakura uses its own video theme.

## Pause / Resume

Pause is available only after gameplay has started. It disables note input and spawning, pauses audio through `SongConductor`, and blocks gameplay raycasts behind the pause panel. Resume continues from the captured musical position. Restart uses the existing Gameplay scene reload, and Home uses the existing MainMenu navigation path.

## Android / Mobile Notes

- Portrait-only autorotation is configured.
- Android minimum SDK is `22`; target SDK is set to Unity's **Automatic** selection.
- The configured Android architecture is ARMv7.
- A local Development APK has been built successfully. Build output is intentionally untracked.
- No physical Android device or emulator was connected during automated validation; device-level video playback, touch feel, performance, and final SFX/music balance still require confirmation.

## Design Choices

- **Data-driven songs:** one catalog/definition flow supports new songs without scene-specific loading code.
- **Editor-time MIDI preprocessing:** runtime consumes validated JSON rather than interpreting MIDI during play.
- **Deterministic adaptive lanes:** pitch structure is retained while lane balance and repeated runs are controlled predictably.
- **Central DSP timeline:** audio, spawning, note movement, pause, and resume share one timing source.
- **Explicit visual readiness:** music cannot advance behind an unprepared gameplay video.
- **Separate START interaction:** the pre-game tile cannot alter score, combo, chart counts, judgement, or win completion.
- **Focused presentation systems:** pooled VFX and small UI/audio controllers add feedback without a broad manager framework.

## Project Structure

```text
Assets/
|-- Audio/          Music and production SFX
|-- Fonts/          Production TMP source/font assets
|-- GameData/       Song definitions, MIDI sources, charts, reports, themes
|-- Prefabs/        Note and UI prefabs
|-- Scenes/         MainMenu and Gameplay
|-- Scripts/
|   |-- Audio/      SFX playback
|   |-- Editor/     MIDI chart import/build/validation
|   |-- Runtime/    Gameplay, songs, timing, chart loading, visuals
|   |-- UI/         Menu, HUD, pause, and result presentation
|   `-- VFX/        Hit, lane, and failure feedback
`-- UI/             Background videos and song covers
```

## AI Usage

AI assistance was used for code review and debugging, implementation suggestions, project/validation audits, and documentation support. The developer reviewed the resulting code, serialized wiring, gameplay behavior, and documentation; AI output was not accepted as a substitute for project inspection or validation.

## Asset Attributions

Only source information present in this repository is recorded below. Missing licenses are intentionally marked for confirmation rather than inferred from names.

### Confirmed license record

- **Liberation Sans:** bundled with TextMesh Pro as a fallback font. `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` records copyright for Google Corporation and Red Hat, Inc. and the SIL Open Font License 1.1.

### Partial source traceability

- **DryWetMIDI 8.0.3:** by Melanchall; project source is recorded as <https://github.com/melanchall/drywetmidi> in `Assets/Melanchall/DryWetMIDI/README.txt`. That imported package folder does not include a license file, so the applicable version's license still needs final confirmation.
- **Vector UI Pack:** the included developer note identifies “Vector UI Pack” by Duplo / `dobo_ui` on itch.io. Production UI references its purple effect/header, cyan item slot, dark modal, and black panel images. No license terms or direct asset URL are included.
- **Production SFX:** `Assets/Audio/SFX/SOURCES.md` preserves the original download filenames, but not confirmed platform URLs or license metadata:

| Production clip | Recorded original filename |
|---|---|
| `Miss.mp3` | `freesound_community-negative_beeps-6008.mp3` |
| `UIClick.mp3` | `soundshelfstudio-ui-click-futuristic-524742.mp3` |
| `Victory.mp3` | `eaglaxle-gaming-victory-464016.mp3` |
| `GameOver.mp3` | `freesound_community-game-over-arcade-6435.mp3` |

The filename tokens are retained for lookup and are not treated here as verified author, platform, or license claims.

### Attribution requiring confirmation

- Teko SemiBold and Oxanium Bold/SemiBold are used by production UI, but their source and license files are not included under `Assets/Fonts/`.
- The four song audio files and their MIDI source files do not include source/license notes.
- `MainMenuBackground.mp4`, `DemoBackground.mp4`, and `SakuraBackground.mp4` do not include source/license notes.
- The four song cover images do not include source/license notes.

These records should be completed before public distribution.

## Known Issues / Limitations

- Windows Media Foundation reports corrected H.264 timestamps and unknown color primaries for `MainMenuBackground.mp4`. Editor/build playback has succeeded, but device playback should be checked before release.
- The current Development APK is large (approximately 399 MiB), primarily due to bundled media plus development-build payload. Release compression/packaging has not been finalized.
- Physical Android validation is outstanding, including device-specific Safe Area, touch routing, video decoding, sustained performance, and audio balance.
- The Unity company setting still uses `DefaultCompany`; production application identity/signing is not finalized.
- No persistent save system, settings/volume menu, pause settings, or hold-note gameplay is implemented.
- Source/license confirmation remains outstanding for the assets listed above.

## Repository Notes

- Keep generated APK/AAB output outside `Assets/` and untracked.
- Detailed Task 2 optimization work is maintained separately; no optimization report is present on the current `main` branch, so benchmark claims are intentionally omitted here.
