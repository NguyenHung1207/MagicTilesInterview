# MAGIC TILES — TECHNICAL INTERVIEW PROJECT AUDIT

> Tài liệu này mô tả **implementation hiện tại** của project Unity `MagicTilesInterview`. Nội dung được suy ra từ source code, scene/prefab serialization, ScriptableObject, chart/report và Project Settings đang có trong repository; không giả định thêm class hoặc behavior ngoài code.

## Mục lục

1. [Tổng quan Project](#1-tổng-quan-project)
2. [Architecture Map](#2-architecture-map)
3. [Main Menu → Gameplay Flow](#3-main-menu--gameplay-flow)
4. [Note Lifecycle](#4-note-lifecycle)
5. [Timing / DSP](#5-timing--dsp)
6. [Startup Warmup](#6-startup-warmup)
7. [MIDI → JSON Pipeline](#7-midi--json-pipeline)
8. [Data-Driven Song System](#8-data-driven-song-system)
9. [Input](#9-input)
10. [Judgement / Scoring](#10-judgement--scoring)
11. [Note Movement](#11-note-movement)
12. [VFX](#12-vfx)
13. [SFX](#13-sfx)
14. [Pause](#14-pause)
15. [Result System](#15-result-system)
16. [Video Background](#16-video-background)
17. [Android / Mobile](#17-android--mobile)
18. [Design Patterns](#18-design-patterns)
19. [Separation of Concerns](#19-separation-of-concerns)
20. [Events / Unity Lifecycle](#20-events--unity-lifecycle)
21. [Production Performance](#21-production-performance)
22. [Task 2 Optimization](#22-task-2-optimization)
23. [Top 10 Technical Decisions](#23-top-10-technical-decisions)
24. [Reviewer Feedback Analysis](#24-reviewer-feedback-analysis)
25. [Code Quality](#25-code-quality)
26. [25 Interview Hotspots](#26-25-interview-hotspots)
27. [Important Files / Classes](#27-important-files--classes)
28. [Study Order](#28-study-order)

---

## 1. Tổng quan Project

### 1.1 Project này là gì?

Đây là game rhythm bốn lane, màn hình dọc, làm bằng Unity `2022.3.62f2` cho bài test “Magic Tiles 3 (Simplified)”. Gameplay loop chính là:

```text
Chọn bài hát
→ chờ media/chart sẵn sàng
→ bấm START
→ note di chuyển theo timeline của nhạc
→ bấm trực tiếp lên note
→ nhận Perfect / Great / Good
→ tăng score/combo hoặc Game Over khi Miss/Wrong Tap
→ Win khi tất cả note đã hit và bài nhạc kết thúc
```

Project có bốn bài: Demo Song, Sonata 01, Sonata 02 và Sakura. Hai production scene là `MainMenu.unity` và `Gameplay.unity`; thứ tự trong Build Settings lần lượt là build index 0 và 1.

Từ góc nhìn Unity Developer, đây không chỉ là một scene gameplay đơn lẻ. Project được chia thành các layer kỹ thuật:

- **Data:** `SongDefinition`, `SongCatalog`, `BackgroundTheme`, JSON chart.
- **Editor pipeline:** MIDI được đọc, normalize, group chord, chọn representative note, map lane, validate và xuất JSON trước runtime.
- **Loading/context:** `SelectedSongContext` chuyển lựa chọn qua scene; `GameplaySongLoader` phân phối AudioClip, chart và theme.
- **Startup coordination:** `GameplayStartupController` chặn START cho tới khi video, audio, chart, spawner và prewarm đã sẵn sàng.
- **Timing:** `SongConductor` dùng DSP clock — đồng hồ audio có độ chính xác cao — làm nguồn thời gian trung tâm.
- **Gameplay:** `NoteSpawner`, `Note`, `NoteInputController`, `HitPositionJudge`, `ScoreManager`, `GameplayController`.
- **Presentation:** HUD, VFX, SFX, video, pause và result screens nhận event từ gameplay.
- **Mobile:** portrait layout, touch, Safe Area, Android media transcoding và responsive world layout.
- **Task 2:** benchmark/optimization riêng cho một particle-effect package, không nằm trong production gameplay.

### 1.2 Inventory các subsystem thực tế

| Subsystem | Class / asset | Trách nhiệm | Dependency / Input | Output / Event | Không nên chịu trách nhiệm |
|---|---|---|---|---|---|
| Main Menu | `MainMenuController`, `SongCardView`, `SongCard.prefab` | Sinh card từ catalog và nhận lựa chọn | `SongCatalog`, prefab, content transform | Load scene Gameplay | Load chart hoặc điều khiển gameplay |
| Song data | `SongDefinition`, `SongCatalog`, `MainSongCatalog.asset` | Mô tả content và danh sách song | AudioClip, TextAsset, Sprite, theme | Dữ liệu read-only cho menu/loader | Chứa runtime behavior |
| Selected song | `SelectedSongContext` | Giữ reference song qua scene | `SongDefinition` được chọn | `SelectedSong` static | Load asset hoặc save lâu dài |
| Gameplay loading | `GameplaySongLoader` | Chọn selected/default song và phân phối data | Context, conductor, chart loader, background | Gọi `SetSong`, `Load`, `ApplyTheme` | Quyết định readiness/START |
| Startup warmup | `GameplayStartupController`, `StartTileController` | Điều phối Loading/Ready/Playing | Background, audio, chart, spawner, input, pause | Hiện START, gọi `StartSong` | Parse MIDI, judge hoặc score |
| Music sync | `SongConductor` | Schedule audio và cung cấp `SongTime` | `AudioSource`, `AudioClip`, DSP time | Clock/state properties | Spawn/move/judge note |
| Chart runtime | `JsonChartLoader`, `SongChartData`, `NoteData` | Deserialize JSON | Chart `TextAsset` | `Notes`, `IsReady`, `HasFailed` | MIDI analysis |
| Note spawning | `NoteSpawner`, `LaneView`, `Note.prefab` | Tính spawnTime, chọn lane, instantiate | Chart, conductor, four lanes | Các `Note` runtime | Score/judgement/session result |
| Note movement | `Note` | Derive position từ timeline | SongTime, spawn/hit position/time | Position, resolved lifecycle | Global game state |
| Input | `NoteInputController`, `StartTileController` | Mouse/touch → world → collider | Camera, EventSystem, Physics2D | `TryHit` hoặc `FailedInput` | Tìm note theo lane/timeline |
| Judgement | `HitPositionJudge`, `HitJudgement`, `Note.TryHit` | Phân loại khoảng cách tới HitPoint | noteY, hitY | Perfect/Great/Good/Miss | Tính score |
| Score/combo | `ScoreManager` | Áp dụng điểm và combo | `Note.Judged` | `ScoreChanged` | Kết thúc run |
| Session | `GameplayController` | Win/Game Over, stop/cancel session | Conductor, spawner, input, note count | `GameOver`, `GameWon` | Animate UI |
| VFX | `HitBurstController`, `LaneFlashController`, `FailureFeedbackController` | Visual feedback theo event | Note/input events, particle/sprite refs | Particle, flash, shake | Thay đổi gameplay rules |
| SFX | `SfxController` | One-shot short audio xuyên scene | Một AudioSource và bốn clip | Âm Miss/UI/Victory/GameOver | Music timeline |
| Pause | `PauseMenuController` | Block gameplay và điều phối pause UI | Session, conductor, input, spawner, result | Pause/resume/navigation | Tự tính DSP time |
| Result | `ResultScreenController` | Win/GameOver panel, score animation, Retry/Home | ScoreManager, GameplayController | Scene reload/load | Quyết định win/fail |
| Background | `MenuVideoBackground`, `DynamicBackgroundController`, `BackgroundTheme` | Video/procedural presentation và readiness | VideoClip, camera, theme | `BackgroundReady` | Nhạc hoặc chart timing |
| Mobile | `GameplayResponsiveLayout`, `SafeAreaFitter`, Player Settings | Portrait/responsive layout/Safe Area | Screen, camera, safe-area rect | Transform/anchor configuration | Gameplay logic |
| Task 2 | `OptimizationBenchmarkRunner`, `ParticleSystemRendererConsolidator` | Benchmark deterministic và consolidate rain renderer | Before/after prefabs, ParticleSystem | Profiler/Frame Debugger evidence | Production gameplay |

### 1.3 Dữ liệu production chính

| Chart | Số note | First hit | Last hit | Lane count 0–3 |
|---|---:|---:|---:|---|
| Demo Song | 62 | 0 s | 32.72724 s | 17, 16, 15, 14 |
| Sakura | 140 | 0 s | 59.25 s | 34, 35, 36, 35 |
| Sonata 01 | 821 | 0 s | 272 s | 207, 206, 204, 204 |
| Sonata 02 | 145 | 0 s | 178 s | 39, 37, 35, 34 |

---

## 2. Architecture Map

```text
MainMenu.unity
├── MenuVideoBackground
├── SfxController (DontDestroyOnLoad)
└── MainMenuController
    └── MainSongCatalog : SongCatalog
        └── SongDefinition[]
            ├── AudioClip
            ├── JSON chart : TextAsset
            ├── cover Sprite
            └── BackgroundTheme
                    │
SongCardView.HandleClick()
→ MainMenuController.SelectSong()
→ SelectedSongContext.Select()
→ SceneManager.LoadScene("Gameplay")
                    │
                    ▼
Gameplay.unity
├── GameplaySongLoader.Awake()
│   ├── SongConductor.SetSong(AudioClip)
│   ├── JsonChartLoader.Load(TextAsset)
│   └── DynamicBackgroundController.ApplyTheme(BackgroundTheme)
│
├── GameplayStartupController
│   ├── Background readiness
│   ├── AudioClip readiness
│   ├── Chart / NoteSpawner readiness
│   ├── representative Note prewarm
│   └── StartTileController
│           │
│           ▼
│       StartGameplay()
│           └── SongConductor.StartSong()
│
├── SongConductor (DSP clock)
│       ↓ SongTime
├── NoteSpawner
│   └── LaneView[] → Instantiate Note.prefab
│                       ↓
├── NoteInputController → Note.TryHit()
│                       ↓
├── Note.Judged / Note.HitSucceeded
│   ├── ScoreManager → GameplayHUD
│   ├── GameplayController → GameWon / GameOver
│   ├── HitBurstController
│   ├── LaneFlashController
│   └── FailureFeedbackController
│
├── PauseMenuController
└── ResultScreenController → Retry / Home
```

Task 2 là nhánh tách biệt:

```text
ParticleEffectsUnoptimize.prefab
→ OptimizeBefore.unity
→ OptimizationBenchmarkRunner
→ Profiler + Frame Debugger
→ trail tuning / rain experiment / rejected zap experiment
→ ParticleEffectsOptimized.prefab
→ OptimizeAfter.unity
```

---

## 3. Main Menu → Gameplay Flow

### 3.1 Mở game và Main Menu

1. Unity load `Assets/Scenes/MainMenu.unity` vì đây là scene đầu tiên trong `EditorBuildSettings.asset`.
2. `SfxController.Awake()` kiểm tra singleton. Instance đầu tiên được giữ bằng `DontDestroyOnLoad`.
3. `MenuVideoBackground.Awake()` lấy hoặc add `VideoPlayer`, cấu hình loop, `CameraFarPlane`, `FitOutside`, không audio, rồi gọi `Play()` với `MainMenuBackground.mp4`.
4. `MainMenuController.Start()` validate `songCatalog`, `songCardPrefab`, `songListContent` và tên scene.
5. `ClearSongCards()` gọi `Destroy()` cho các child placeholder hiện có.
6. Controller duyệt `songCatalog.Songs`, instantiate `SongCardView`, rồi gọi `card.Bind(song, SelectSong)`.
7. `SongCardView.Bind()` gán display name, artist, cover/fallback và button listener.

### 3.2 Chọn bài và chuyển scene

Nút PLAY thực tế là button của từng song card. Flow là:

```text
SongCardView.HandleClick()
→ onSelected?.Invoke(song)
→ MainMenuController.SelectSong(song)
→ SfxController.PlayUIClick()
→ SelectedSongContext.Select(song)
→ SceneManager.LoadScene("Gameplay")
```

`SelectedSongContext` là static holder. Nó giữ chính reference `SongDefinition`, không copy ID, không serialize, không load lại từ disk. Retry Gameplay vì thế giữ nguyên bài đang chọn; Home quay lại menu nhưng context chỉ bị overwrite khi chọn bài khác, không tự `Clear()`.

### 3.3 Gameplay scene nhận data

`GameplaySongLoader.Awake()` chọn:

```text
SelectedSongContext.SelectedSong != null
    ? SelectedSongContext.SelectedSong
    : defaultSong
```

`defaultSong` được scene serialize là `DemoSong.asset`. Sau đó loader gọi ba nhánh độc lập:

```csharp
songConductor.SetSong(song.AudioClip);
chartLoader.Load(song.ChartAsset);
backgroundController.ApplyTheme(song.BackgroundTheme);
```

- `SetSong()` chỉ gán clip và reset cờ báo lỗi load.
- `JsonChartLoader.Load()` parse JSON đồng bộ bằng `JsonUtility.FromJson<SongChartData>()`, copy notes vào list nội bộ và set `IsReady`.
- `ApplyTheme()` áp màu/procedural settings và gọi `VideoPlayer.Prepare()` nếu theme có video.

Gameplay scene cũng chứa một `SfxController`. Vì instance từ Main Menu còn sống, copy mới tự `Destroy(gameObject)` để tránh hai service phát âm đồng thời.

### 3.4 Startup, START và DSP timeline

`GameplayStartupController.Awake()`:

- set state `PreparingBackground`;
- disable `NoteSpawner`;
- disable `NoteInputController`;
- gọi `pauseMenuController.SetGameplayStarted(false)`;
- hide START;
- show Loading.

`Start()` prewarm Note và chạy coroutine readiness. Khi background, audio và spawner cùng ready, `EnterReadyToStart()` hide Loading và gọi:

```csharp
startTile.Show(startLane.HitPoint.position, StartGameplay);
```

Scene serialize `startLane` là `Lane2`, tức lane thứ ba nếu index từ 0.

Khi người chơi bấm đúng START collider, `StartTileController.TryPress()` gọi callback `StartGameplay()`. Method này gọi `songConductor.StartSong()`. Nếu thành công:

- state thành `Playing`;
- START bị hide;
- UI click SFX phát;
- `NoteSpawner` được enable;
- pause button được enable;
- gameplay input được enable ở frame kế tiếp.

`SongConductor.StartSong()` là object bắt đầu DSP timeline. Giá trị scene thực tế là:

```text
songStartDspTime = AudioSettings.dspTime + 2.5
```

Note đầu tiên có `hitTime = 0`, `travelTime = 2.0`, nên cần spawn tại `SongTime = -2.0`. Với pre-roll 2.5 giây, spawner có khoảng 0.5 giây sau START trước khi note đầu tiên cần xuất hiện.

---

## 4. Note Lifecycle

### 4.1 Từ JSON tới GameObject

```text
JSON { lane, hitTime }
→ JsonUtility
→ SongChartData.notes : List<NoteData>
→ JsonChartLoader.Notes
→ NoteSpawner.Update()
→ spawnTime = hitTime - travelTime
→ LaneView[lane]
→ Instantiate(Note.prefab)
→ Note.Initialize(...)
→ Note.Update()
→ input/judgement
→ resolved
→ Destroy
```

`NoteData` chỉ có hai field: `int lane` và `double hitTime`. `NoteSpawner.Update()` nhìn phần tử `nextNoteIndex`, tính spawn time, rồi break nếu còn quá sớm. Nếu đã tới hạn, nó lấy `lanes[nextNote.lane]`, instantiate prefab ở `SpawnPoint`, gọi `Initialize()` và tăng index. Vòng `while` cho phép spawn nhiều note trong cùng frame nếu frame bị trễ hoặc nhiều note cùng tới hạn.

`Note.Initialize()` lưu:

- reference `SongConductor`;
- `spawnPosition` và `hitPosition`;
- `spawnTime` và `hitTime`;
- `laneIndex`;
- position ban đầu.

### 4.2 Successful Hit

1. `NoteInputController.TryHitAtScreenPosition()` dùng `Physics2D.OverlapPoint`.
2. Collider phải có component `Note`; nếu có, controller gọi `note.TryHit()`.
3. `TryHit()` return `None` nếu `isResolved` đã true, bảo vệ double resolution ở level của Note.
4. `HitPositionJudge.Evaluate(transform.position.y, hitPosition.y)` trả Perfect, Great hoặc Good.
5. `isResolved = true`.
6. `SetResolvedVisual()` disable collider và start `FadeResolvedVisual()`.
7. `Note.Judged?.Invoke(judgement)` được phát.
8. `Note.HitSucceeded?.Invoke(laneIndex, judgement, transform.position)` được phát.

State/data bị tác động bởi subscriber:

- `ScoreManager.ApplyJudgement()` đổi Score/Combo rồi phát `ScoreChanged`.
- `GameplayController.HandleJudgement()` tăng `successfulHitCount`.
- `GameplayHUD.ShowJudgement()` hiện text.
- `HitBurstController.HandleHit()` lấy particle từ pool.
- `LaneFlashController.HandleHit()` flash đúng lane.

Không có hit SFX cho successful note trong production hiện tại. VFX có, nhưng `SfxController` chỉ chứa Miss/UI/Victory/GameOver.

Sau hit, Note không bị destroy ngay. Collider đã tắt, sprite fade về alpha `0.25`, Note tiếp tục derive position từ SongTime. Khi progress đạt `resolvedCleanupProgress = 1.2`, `Destroy(gameObject)` được gọi.

### 4.3 Natural Miss

Trong `Note.Update()`:

```csharp
if (currentSongTime > hitTime + MissDelay)
```

với `MissDelay = 0.15`:

1. `isResolved = true`.
2. Phát `Note.Judged(HitJudgement.Miss)`.
3. `ScoreManager` reset combo về 0, score không đổi, phát `ScoreChanged`.
4. `GameplayController.HandleJudgement()` gọi `EndGame()`.
5. `FailureFeedbackController.HandleJudgement()` phát miss flash/shake và `SfxController.PlayMiss()`.
6. Note gọi `Destroy(gameObject)`.
7. `EndGame()` disable input/spawner, stop song, cancel tất cả active Note khác và phát `GameOver`.

Thứ tự subscriber của một C# event không nên được dùng như một gameplay contract. Các subscriber hiện tại đều phản ứng độc lập với cùng judgement.

### 4.4 Wrong Tap

Wrong Tap không phải một `HitJudgement`. Nếu `OverlapPoint()` không tìm thấy collider, hoặc collider tìm thấy không có `Note`, `NoteInputController` phát static event `FailedInput`.

- `GameplayController.HandleFailedInput()` gọi `EndGame()`.
- `FailureFeedbackController.PlayFailedInput()` dùng màu/alpha/shake mạnh hơn miss và phát cùng Miss SFX.
- Không có `Note.Judged`, nên `ScoreManager` không reset combo; tuy nhiên run kết thúc ngay.

`GameplayController.CancelActiveNotes()` dùng `FindObjectsByType<Note>(FindObjectsSortMode.None)`, rồi gọi `Note.Cancel()` trên từng object. `Cancel()` set resolved và destroy.

---

## 5. Timing / DSP

### 5.1 `AudioSettings.dspTime` là gì?

`AudioSettings.dspTime` là thời gian của audio DSP system trong Unity. “DSP” ở đây là Digital Signal Processing: clock mà audio engine dùng để schedule và xử lý âm thanh. Nó có độ chính xác cao, dùng `double`, và cùng clock domain với `AudioSource.PlayScheduled()`.

Project dùng DSP time vì note, audio start và pause/resume cần dựa trên cùng một origin. Nếu dùng clock render rồi bắt đầu AudioSource riêng, sai lệch giữa frame và audio thread có thể tạo offset hoặc drift cảm nhận được trong rhythm game.

### 5.2 `songStartDspTime` và `SongTime`

Khi START được bấm:

```csharp
songStartDspTime = AudioSettings.dspTime + startDelay;
musicSource.PlayScheduled(songStartDspTime);
```

Scene override `startDelay` thành `2.5` giây, dù field initializer trong code là `0.5`.

`songStartDspTime` là timestamp DSP tuyệt đối mà logical song time 0 và audio playback được dự kiến bắt đầu.

Khi đang chơi bình thường:

```text
SongTime = AudioSettings.dspTime - songStartDspTime
```

Vì được schedule ở tương lai, `SongTime` ban đầu âm. Đây là behavior có chủ ý, không phải bug. Nó tạo pre-roll để note có thể xuất hiện trước beat đầu tiên.

Các branch khác của property:

- chưa schedule → `0.0`;
- đã stop → `stoppedSongTime`;
- đang pause → `pausedSongTime`;
- đang chạy → DSP formula ở trên.

### 5.3 `hitTime`, `travelTime`, `spawnTime`

- `hitTime`: timestamp theo giây lấy từ generated chart, chỉ thời điểm note cần tới HitPoint.
- `travelTime`: thời lượng note đi từ SpawnPoint tới HitPoint; scene dùng `2.0` giây.
- `spawnTime`: thời điểm logical để instantiate note.

Formula thật:

```text
spawnTime = hitTime - travelTime
```

Movement:

```text
progress = (SongTime - spawnTime) / (hitTime - spawnTime)
position = Vector3.LerpUnclamped(spawnPosition, hitPosition, progress)
```

Vì `hitTime - spawnTime = travelTime`, note đạt progress 0 tại spawn và progress 1 đúng hit time.

### 5.4 Judgement window thực tế

Project không có ba time window Perfect/Great/Good bằng millisecond. Nó dùng khoảng cách world-space:

```text
distance = abs(noteY - hitY)

distance <= 0.35 → Perfect
distance <= 0.90 → Great
otherwise        → Good
```

Natural Miss mới dùng time threshold:

```text
SongTime > hitTime + 0.15
```

Do đó “hit window” thành công thực tế là kết hợp giữa collider có thể bấm và position threshold; Good là fallback không có maximum distance nếu người chơi chạm được collider Note.

### 5.5 Vì sao không dùng `Time.time`?

`Time.time` là game/render timeline, chịu ảnh hưởng của `timeScale` và được quan sát theo frame. Nếu tích lũy movement bằng `velocity * deltaTime`, sai số và frame hitch có thể làm visual timeline lệch khỏi audio.

Thiết kế hiện tại derive lại position từ absolute `SongTime` mỗi frame. Khi frame hitch xảy ra, Note sẽ “catch up” tới vị trí đúng theo nhạc thay vì làm nhạc chậm theo rendering. Visual có thể jump trong hitch lớn, nhưng synchronization không tích lũy drift.

Các failure mode nếu timing viết sai:

- schedule audio và set origin bằng hai clock khác nhau;
- không cho đủ pre-roll khiến note đầu spawn muộn;
- dùng float lâu dài gây precision loss;
- tích lũy deltaTime tạo drift;
- không đóng băng logical time khi pause;
- rebase sai khiến Note nhảy hoặc miss hàng loạt;
- đặt `travelTime <= 0` gây denominator bằng 0 — `NoteSpawner.Awake()` hiện chặn case này.

### 5.6 Pause / Resume và DSP rebase

Pause flow trong `SongConductor.PauseSong()`:

```csharp
pausedSongTime = SongTime;
isSongPaused = true;
musicSource.Pause();
```

Trong lúc pause, getter `SongTime` luôn return `pausedSongTime`. DSP clock hệ thống vẫn tăng nhưng logical rhythm time đứng yên.

Resume flow:

```csharp
songStartDspTime = AudioSettings.dspTime - pausedSongTime;
isSongPaused = false;
musicSource.UnPause();
```

Đây là DSP rebase: tính lại origin sao cho ngay sau resume, formula `dspTime - songStartDspTime` vẫn bằng thời điểm đã pause.

Ví dụ pause tại SongTime 12 giây trong 10 giây. Nếu không rebase, DSP time đã tăng thêm 10; logical SongTime sẽ nhảy từ 12 lên khoảng 22. Note đang đứng yên trên màn hình sẽ lập tức vượt HitPoint, nhiều note có thể thỏa miss condition và player Game Over. Rebase loại bỏ 10 giây pause khỏi logical timeline.

`Time.timeScale` không được đổi. Vì vậy pause UI dùng `unscaledDeltaTime`, còn Note vẫn chạy `Update` nhưng nhận SongTime đóng băng nên position không tiến.

---

## 6. Startup Warmup

### 6.1 State flow

```text
PreparingBackground
→ WarmingUp
→ ReadyToStart
→ Playing
```

`GameplayStartupController` là state owner. Đây là enum-based state machine nhỏ, không phải State Pattern bằng các state object riêng.

### 6.2 `PreparingBackground`

Trong `Awake()` controller:

- disable spawner và gameplay input;
- disable pause availability;
- hide START;
- show Loading;
- set state `PreparingBackground`.

`OnEnable()` subscribe `DynamicBackgroundController.BackgroundReady`. `Start()` đồng thời đọc `backgroundController.IsReady`, nên nếu readiness đã resolve trước subscription do Unity execution order, polling state vẫn bắt được.

### 6.3 Background readiness và four advancing frames

`DynamicBackgroundController.ApplyVideo()` reset readiness, bật `sendFrameReadyEvents`, gọi `Prepare()` và start timeout 10 giây.

`prepareCompleted` **chưa đủ**. Callback này chỉ:

```text
IsVideoPrepared = true
→ VideoPlayer.Play()
```

`frameReady` chỉ count frame index hợp lệ và tiến lên. Scene không serialize override nên `stableAdvancingFrameCount` dùng initializer `4`. Sau bốn observed advancing frame, `ResolveReadiness()` set `IsReady` và phát `BackgroundReady`.

Gate này xác minh video không chỉ “prepared” về API mà đã bắt đầu cung cấp frame visual. Nếu error hoặc quá 10 giây, controller warning một lần, stop video và resolve fallback để decorative media không khóa gameplay vô hạn.

Một edge case nhỏ: khi frame index đi lùi, count reset nhưng `lastObservedFrame` không được gán lại trong branch đó; normal first playback phải đạt bốn frame trước loop, còn lỗi sequence sẽ đi tới timeout fallback.

### 6.4 Audio gate

Production music import settings dùng `preloadAudioData: 0` và `loadInBackground: 1`. Coroutine `WaitForRequiredReadiness()` yield một frame trước để Loading có cơ hội render, rồi nếu state là `Unloaded` gọi:

```csharp
songConductor.PrepareAudioData();
```

`PrepareAudioData()`:

- `Loaded` hoặc `Loading` → request hợp lệ;
- `Unloaded` → gọi `AudioClip.LoadAudioData()`;
- `Failed` hoặc request fail → log error một lần.

START chỉ hiện khi `IsAudioReady`, nghĩa là clip tồn tại và `loadState == AudioDataLoadState.Loaded`.

### 6.5 Chart và NoteSpawner gate

`JsonChartLoader.Load()` đã chạy trong `GameplaySongLoader.Awake()`. Nó chỉ ready nếu JSON parse được và có ít nhất một note.

`NoteSpawner.IsReady` yêu cầu đồng thời:

```text
configurationReady
&& IsChartReady
&& IsPrewarmed
```

Configuration validation gồm conductor/chart/prefab tồn tại, travelTime > 0 và đúng bốn `LaneView` không null.

### 6.6 Representative Note prewarm

`NoteSpawner.Prewarm()`:

```text
Instantiate Note.prefab dưới spawner
→ đổi tên NotePrewarm
→ SetActive(false)
→ Destroy
→ prewarmComplete = true
```

Mục đích là trả một phần chi phí tạo prefab/component/native object trước START. Đây là representative prewarm có giới hạn: object bị disable ngay nên không chứng minh toàn bộ shader/renderer path đã được warm đầy đủ.

### 6.7 Input và pause gating

- Gameplay input bị disable từ `Awake` đến frame sau khi `StartSong()` thành công.
- START dùng controller/collider riêng, không thể emit `Note.Judged`.
- Enable input ở next frame tránh START gesture bị gameplay controller đọc lại.
- Pause button ẩn/không available cho tới state Playing.
- Nếu terminal state đã xảy ra, pause không thể bật lại.

Mục tiêu của architecture là: **khi START xuất hiện, các công việc nặng cần thiết đã hoàn thành**.

Những việc không còn nằm trên START callback:

- parse JSON;
- `VideoPlayer.Prepare()` và chờ frame decode;
- `AudioClip.LoadAudioData()`;
- chart/config readiness;
- representative Note prewarm.

START callback chỉ schedule audio, đổi state/presentation và enable các system đã sẵn sàng.

---

## 7. MIDI → JSON Pipeline

### 7.1 Boundary tổng thể

```text
Source MIDI (*.mid.bytes)
→ Editor preprocessing bằng DryWetMIDI
→ generated SongChartData JSON
→ runtime JsonChartLoader
```

`MidiChartImporter` là Unity Editor menu tool. `BuildAllMusicCharts()` gọi pipeline cho bốn source; `BuildDemoSongChart()` dành cho Demo. DryWetMIDI không nằm trên gameplay runtime path.

### 7.2 Extraction

`MidiChartImporter.BuildChart()`:

1. Load MIDI dưới dạng `TextAsset` qua `AssetDatabase`.
2. Tạo `MemoryStream` từ bytes.
3. `MidiFile.Read(stream)`.
4. Đếm track.
5. Gọi `MidiNoteExtractor.Extract(midiFile)`.

`MidiNoteExtractor` lấy `TempoMap`. Mỗi MIDI note được đổi thành `MidiExtractedNote`:

- `Tick`: vị trí integer trong timeline MIDI;
- `TimeSeconds`: `MetricTimeSpan.TotalSeconds` sau khi áp tempo map;
- `NoteNumber`: pitch MIDI;
- `TrackIndex`: track nguồn.

Tick là source-time coordinate rời rạc/chính xác hơn để group sự kiện đồng thời. Seconds là kết quả conversion dùng ở runtime, không phải key normalization.

### 7.3 Deterministic ordering và dedup

Extractor sort theo:

```text
Tick
→ NoteNumber
→ TrackIndex
```

`ChartBuilder` copy và sort lại cùng comparator để không phụ thuộc thứ tự caller. Exact duplicate dùng key:

```text
(long Tick, int NoteNumber)
```

TrackIndex cố ý không nằm trong key: cùng pitch/cùng tick bị coi là layer/copy của cùng musical event dù xuất hiện ở nhiều track.

Sau dedup, `SortedDictionary<long, List<MidiExtractedNote>>` group theo Tick. `SortedDictionary` đảm bảo group đi theo thời gian tăng dần.

### 7.4 Chord handling và representative note

Nhiều pitch cùng Tick là chord/multi-pitch group, không phải duplicate. Gameplay giản lược chỉ cho một note tại một Tick, nên builder chọn một representative:

1. Ưu tiên `PreferredMelodyTrackIndex = 1`.
2. Nếu track 1 polyphonic, loop trên list đã sort và giữ note cuối của track đó, tức pitch cao nhất.
3. Nếu track 1 im lặng, chọn pitch cao nhất toàn group làm top-voice melody proxy.

Đây là heuristic theo evidence của source hiện tại, không phải MIDI standard nói melody luôn nằm ở track 1.

### 7.5 Pitch analysis và base lane

Builder lấy toàn bộ representative pitch, sort, rồi derive ba threshold bằng floor-index quantile:

```text
Q25, Q50, Q75
```

Base mapping:

```text
pitch <= Q25 → lane 0
pitch <= Q50 → lane 1
pitch <= Q75 → lane 2
otherwise    → lane 3
```

Cách này giữ bias “pitch thấp ở trái, pitch cao ở phải” và thích nghi với range của từng bài, thay vì `pitch % 4` hoặc random.

### 7.6 Adaptive lane assignment

`AdjustForPlayability()` chỉ override base lane khi có một trong các điều kiện:

- base lane nhiều hơn lane ít nhất trên 1 note;
- consecutive run hiện tại đã ít nhất 2;
- sau ít nhất bốn assignment vẫn còn lane trống và base lane không phải lane ít nhất.

Mỗi candidate 0–3 nhận score penalty/reward:

- xa base lane: `abs(candidate - baseLane) * 2`;
- lặp previous lane: +3, hoặc +12 nếu run đã >= 2;
- candidate nhiều hơn minimum: cộng phần chênh;
- candidate đang minimum: −3;
- đi ngược pitch direction: +2;
- jump lane lớn hơn 1 trong khi pitch chỉ đổi <= 2 semitone: +4.

Lane có score nhỏ nhất thắng. Vì duyệt từ 0 lên 3 và chỉ replace khi `score < bestScore`, tie cũng deterministic. Kết quả current reports có longest consecutive lane run là 2 cho cả bốn chart.

### 7.7 Validation

`ChartValidator.Validate()` kiểm tra:

- build/chart/list không null;
- chart không rỗng;
- từng note không null;
- lane trong `[0, 3]`;
- hitTime finite, không âm;
- thứ tự time không giảm;
- không trùng runtime `hitTime`;
- `BuiltNotes.Count == Chart.notes.Count`;
- representative count bằng Tick group count;
- gameplay note count bằng Tick group count;
- không trùng source Tick trong built context.

Với chart >= 20 note, lane dưới 15% hoặc trên 40% tạo warning. Validation error vẫn ghi report nhưng không ghi JSON mới.

### 7.8 Output và report

Nếu PASS:

```text
JsonUtility.ToJson(buildResult.Chart, true)
→ Assets/GameData/Generated/Charts/*_Chart.json
```

`ChartImportReportWriter` ghi source, raw count, dedup, group/chord stats, representative policy, lane counts, first/last time, minimum interval, longest run, ba transformation sample và validation messages.

Current evidence:

| Bài | Raw | Exact duplicate removed | Tick group / gameplay note | Multi-pitch group | Preferred track / fallback | Min interval |
|---|---:|---:|---:|---:|---:|---:|
| Demo | 181 | 29 | 62 | 33 | 33 / 29 | 0.272727 s |
| Sakura | 143 | 0 | 140 | 3 | 140 / 0 | 0.125 s |
| Sonata 01 | 1,142 | 0 | 821 | 136 | 717 / 104 | 0.083333 s |
| Sonata 02 | 373 | 0 | 145 | 123 | 115 / 30 | 0.5 s |

Tất cả current report PASS và không warning.

### 7.9 Tại sao Editor thay vì runtime?

Lợi ích:

- runtime chỉ parse cấu trúc `{lane, hitTime}` đơn giản;
- không chạy tempo/chord/lane heuristic khi vào game;
- không phụ thuộc MIDI parser trong gameplay path;
- deterministic output dễ diff/debug;
- malformed chart bị chặn trước khi JSON được accept;
- report giữ evidence cho quyết định chuyển đổi.

Trade-off:

- source MIDI đổi thì phải regenerate;
- tooling phức tạp hơn runtime schema;
- generated JSON/report cần được quản lý/version cùng source;
- heuristic track 1/highest pitch có thể không phù hợp source mới;
- harmony bị mất vì one Tick → one note;
- chưa có chart/audio offset calibration.

---

## 8. Data-Driven Song System

### 8.1 ScriptableObject data

`SongDefinition` chứa:

```text
songId
displayName
artist
AudioClip
chartAsset : TextAsset
coverSprite
BackgroundTheme
```

`SongCatalog` chứa `List<SongDefinition>` và expose bằng `IReadOnlyList`. `MainSongCatalog.asset` serialize bốn definition theo thứ tự Demo, Sonata 01, Sonata 02, Sakura.

`BackgroundTheme` chứa VideoClip, overlay color, ba procedural color, drift speed, rotation speed, pulse speed và pulse amount.

### 8.2 Data và Behavior

**Data:** `SongDefinition`, `SongCatalog`, `BackgroundTheme`, JSON, AudioClip, Sprite, VideoClip. Chúng trả lời “content nào và parameter nào?”.

**Behavior:** `MainMenuController`, `GameplaySongLoader`, `SongConductor`, `JsonChartLoader`, `DynamicBackgroundController`. Chúng trả lời “load/chạy/hiển thị bằng cách nào?”.

`SongDefinition` không schedule audio; `SongConductor` không biết song ID/cover/theme. Boundary này giúp một gameplay scene xử lý nhiều song.

### 8.3 Thêm song theo architecture hiện tại

Flow content là:

1. Có AudioClip và source MIDI.
2. Dùng Editor tool tạo JSON/report.
3. Tạo/chọn `BackgroundTheme` và cover.
4. Tạo `SongDefinition` reference các asset.
5. Add definition vào `MainSongCatalog`.

Không cần branch `if song == Sakura` trong gameplay code. Demo/Sonata 01/Sonata 02 còn chứng minh theme reuse: cùng `DemoBackground.mp4` nhưng overlay/procedural parameter khác.

### 8.4 Trade-off

ScriptableObject phù hợp Inspector workflow và reference integrity trong Unity, nhưng dependency bị serialize trong asset/scene. Missing reference chỉ được phát hiện khi validation chạy. `SelectedSongContext` là static global context đơn giản, phù hợp scope test nhưng không có lifetime/persistence contract mạnh như production session service.

---

## 9. Input

### 9.1 Mouse

Trong `NoteInputController.Update()`:

```text
Input.GetMouseButtonDown(0)
→ EventSystem.current.IsPointerOverGameObject() ? ignore
→ TryHitAtScreenPosition(Input.mousePosition)
```

### 9.2 Touch và multi-touch

Nếu `Input.touchCount > 0`, controller duyệt tất cả touch. Chỉ `TouchPhase.Began` được xử lý. Mỗi touch dùng `fingerId` để gọi:

```csharp
EventSystem.current.IsPointerOverGameObject(touch.fingerId)
```

Nhiều finger Began có thể hit nhiều Note trong cùng frame. Sau mỗi attempt, code kiểm tra `!enabled`; nếu một Wrong Tap đã trigger Game Over và disable input synchronously, loop return ngay.

Touch branch return trước mouse branch, tránh synthetic mouse event trên mobile xử lý cùng gesture lần hai.

### 9.3 Screen → world → collider

```csharp
Vector3 worldPosition = gameplayCamera.ScreenToWorldPoint(screenPosition);
Collider2D hit = Physics2D.OverlapPoint(worldPosition);
Note note = hit.GetComponent<Note>();
```

Gameplay dùng orthographic 2D camera, nên x/y từ `ScreenToWorldPoint` phù hợp `OverlapPoint`. Input không raycast một lane rồi tìm “note gần beat nhất”; nó yêu cầu điểm touch overlap collider của chính `Note`.

### 9.4 Vì sao bấm Lane không đủ?

Rules hiện tại mô hình hóa “bấm tile đang rơi”, không phải “bấm phím lane”. Vì vậy:

- bấm khoảng trống trong đúng lane vẫn là Wrong Tap;
- bấm separator/non-Note collider cũng fail;
- chỉ collider có component `Note` mới gọi `TryHit()`.

### 9.5 START input

`StartTileController` có input loop riêng, cũng filter UI. Nó dùng `tapCollider.OverlapPoint(worldPosition)`, không gọi `Physics2D.OverlapPoint` toàn scene. `Show()` nhận callback; valid press disable `acceptingInput` và collider trước khi invoke, tránh START resolve hai lần.

Taps ngoài START ở Ready state bị bỏ qua, không tạo `FailedInput`. Gameplay input còn disabled ở state này.

### 9.6 Edge cases

- **UI click thành gameplay click:** giảm rủi ro bằng `IsPointerOverGameObject`, nhưng phụ thuộc UI raycast configuration và EventSystem tồn tại.
- **Multi-touch:** một touch đúng và một touch sai cùng frame có thể vẫn kết thúc run tùy thứ tự finger được trả về.
- **Note overlap:** `OverlapPoint` trả một collider, không có logic chọn note gần hitTime nhất; collider ordering có thể quan trọng.
- **Non-Note overlap:** nếu query chọn một collider khác chồng lên Note, input có thể bị coi sai.
- **Double resolution:** `Note.isResolved` và collider disable bảo vệ cùng Note; START cũng disable trước callback.
- **Early tap:** nếu collider Note được chạm ở xa HitPoint, `HitPositionJudge` vẫn trả Good vì không có outer success limit.
- **Touch ngoài Note:** phát `FailedInput`, dẫn tới Game Over ngay.

---

## 10. Judgement / Scoring

### 10.1 Judgement thật trong code

| Judgement | Điều kiện | Điểm | Combo |
|---|---|---:|---|
| Perfect | `abs(noteY-hitY) <= 0.35` | +100 | +1 |
| Great | distance `<= 0.9` | +75 | reset 0 |
| Good | mọi distance lớn hơn còn lại khi đã bấm trúng Note | +50 | reset 0 |
| Miss | `SongTime > hitTime + 0.15` | +0 | reset 0 |

Score values được verify trong `ScoreManager.ApplyJudgement()`. Combo là **Perfect streak**, không phải successful-hit streak.

### 10.2 Position judgement và timing

Perfect/Great/Good được judge bằng world-space y distance. Vì position được derive từ SongTime, khoảng cách gián tiếp phản ánh timing, nhưng không phải fixed milliseconds. Nếu travel distance thay đổi theo responsive layout, temporal equivalent của 0.35/0.9 world unit cũng thay đổi.

Good là catch-all, nên successful hit window không có maximum time/distance riêng miễn là touch vẫn overlap collider.

### 10.3 Wrong Tap, Game Over và Win

Game Over khi:

- một Note natural Miss;
- một input không trúng Note.

`EndGame()` disable input/spawner, stop song, cancel Note và phát `GameOver`.

Win chỉ khi:

```text
successfulHitCount >= noteSpawner.TotalNoteCount
&& songConductor.IsSongFinished
```

Perfect, Great và Good đều tăng successful count. Sau note cuối, game chờ `SongTime >= AudioClip.length`. Great/Good không làm fail.

### 10.4 Advanced Timing extension

Baseline assignment đơn giản có thể chỉ Hit +100/Miss 0. Project mở rộng thành ba quality tier, position-derived judgement, Perfect-only combo, natural miss grace 0.15 giây, Wrong Tap terminal rule và explicit Win/GameOver flow. “Advanced” ở đây là so với baseline của bài test; nó chưa phải calibration/difficulty system đầy đủ của commercial rhythm game.

---

## 11. Note Movement

`LaneView` chỉ expose hai scene references:

- `SpawnPoint`: nơi Note xuất hiện;
- `HitPoint`: target visual/judgement của lane.

`GameplayResponsiveLayout.Awake()` đặt y của bốn spawn point theo viewport `1.05` và hit point theo viewport `0.10`, đồng thời width-lock camera dựa trên reference 1080×1920 và orthographic size 5.

Formula movement:

```text
progress = (currentSongTime - spawnTime) / (hitTime - spawnTime)
transform.position = Vector3.LerpUnclamped(spawnPosition, hitPosition, progress)
```

Không có clamp: `LerpUnclamped` cho phép Note tiếp tục đi qua HitPoint khi progress > 1. Hit thành công cleanup ở progress >= 1.2. Natural miss xảy ra theo time trước khi một unresolved Note đi quá xa:

```text
currentSongTime > hitTime + 0.15
```

Ưu điểm của timeline-derived movement:

- không tích lũy sai số velocity/deltaTime;
- mọi Note dùng cùng clock với scheduled audio;
- pause chỉ cần đóng băng SongTime;
- frame drop không làm chart chậm dần so với nhạc;
- spawn trễ một frame vẫn được đặt vào đúng progress logical.

Trade-off là frame hitch có thể tạo visual jump, và position-based judgement phụ thuộc hình học lane/travel distance.

---

## 12. VFX

### 12.1 HitBurst Object Pool

`HitBurstController.Awake()` instantiate trước `poolSize = 16` bản `HitBurst.prefab`, stop/clear rồi deactivate. Object Pool là kỹ thuật tái sử dụng object thay vì tạo/hủy lại cho mỗi event.

`AcquireBurst()`:

1. scan từ `nextPoolIndex` để tìm entry inactive;
2. nếu hết, recycle entry kế tiếp;
3. reset particle trước khi dùng.

`Update()` scan cố định 16 entry; effect không còn `IsAlive(true)` sẽ được reset/deactivate.

### 12.2 Khác biệt theo judgement

| Tier | Scale | Main particles | Accent |
|---|---:|---|---|
| Good | 0.72 | 6 green, lifetime 0.24, speed 2.1 | không |
| Great | 0.96 | 11 blue, lifetime 0.34, speed 2.8 | 4 violet accent |
| Perfect | 1.18 | 17 gold, lifetime 0.44, speed 3.4 | 8 white-gold + 1 central flash |

`HitSucceeded` cung cấp lane, judgement và world position. Burst đặt đúng vị trí Note tại lúc hit.

### 12.3 Lane flash

`LaneFlashController` giữ array SpriteRenderer và một coroutine mỗi lane:

- Perfect: alpha 0.75, duration 0.20 s;
- Great: alpha 0.60, duration 0.16 s;
- Good: alpha 0.40, duration 0.12 s.

Hit mới cùng lane stop coroutine cũ rồi bắt đầu flash mới, tránh hai routine cùng ghi alpha.

### 12.4 Failure feedback

`FailureFeedbackController` subscribe `Note.Judged` và `NoteInputController.FailedInput`.

- Miss: red flash, max alpha 0.34, 0.18 s, shake strength 0.045.
- Wrong input: magenta flash, max alpha 0.42, 0.14 s, shake strength 0.065.
- Shake duration chung 0.14 s.

New failure stop/reset flash và shake cũ. Camera base position được cache ở `Awake`.

### 12.5 Vì sao pool VFX nhưng không pool Note?

Particle hierarchy thường có nhiều module/native resource và hit feedback có thể overlap; pool 16 giúp successful hit không tạo/destroy particle object liên tục. Note đơn giản hơn và số đồng thời bị giới hạn bởi travel window, nên implementation giữ Instantiate/Destroy để giảm complexity. Tuy vậy Sonata 01 có 821 note và interval tối thiểu 0.083333 s, nên Note churn là performance hotspot thực tế hơn nếu đưa lên production dài hạn.

---

## 13. SFX

`SfxController` là sealed MonoBehaviour singleton:

```csharp
public static SfxController Instance { get; private set; }
```

`Awake()`:

- nếu có instance khác, destroy GameObject mới;
- nếu chưa có, set `Instance = this`, gọi `DontDestroyOnLoad`;
- validate một AudioSource và đủ bốn clip;
- force `playOnAwake = false`, `loop = false`, `spatialBlend = 0`.

`OnDestroy()` chỉ clear static Instance nếu object bị destroy chính là instance hiện tại. Đây là duplicate protection quan trọng vì cả MainMenu và Gameplay scene đều serialize một SFX object.

Các API static:

- `PlayMiss()` — volume 0.35;
- `PlayUIClick()` — 0.25;
- `PlayVictory()` — 0.5;
- `PlayGameOver()` — delay realtime 0.16 s rồi volume 0.45.

Tất cả short clips dùng `AudioSource.PlayOneShot`, cho phép phát one-shot mà không thay music clip hoặc cần một GameObject riêng cho từng sound. Với scope bốn SFX, một cached 2D AudioSource là đơn giản và đủ. Trade-off: global singleton ẩn dependency, không có mixer/category/voice priority, và không có successful hit clip.

---

## 14. Pause

### 14.1 Architecture

`PauseMenuController` phụ thuộc:

- `GameplayController` để biết terminal state;
- `SongConductor` để pause/rebase audio timeline;
- `NoteSpawner` và `NoteInputController` để block gameplay;
- `ResultScreenController` để dùng navigation Retry/Home;
- Button, panel, CanvasGroup và card cho UI.

### 14.2 Pause flow

```text
Pause button
→ PauseMenuController.PauseGame()
→ reject nếu chưa start / đã pause / terminal
→ UI click SFX
→ isPaused = true
→ disable pause button
→ disable input + spawner
→ SongConductor.PauseSong()
→ show panel, block raycast
→ unscaled entrance animation
```

Existing Note vẫn có `Update()`, nhưng `SongTime` bị frozen. Background video/procedural animation không bị pause vì project không dùng `Time.timeScale` và không pause VideoPlayer.

### 14.3 Resume flow

```text
ResumeGame()
→ disable panel interaction
→ unscaled exit animation
→ HideImmediately()
→ SongConductor.ResumeSong()
→ enable spawner + input + pause button
→ isPaused = false
```

Input chỉ được enable sau khi pause panel đã hide, tránh click Resume rơi xuyên xuống gameplay. `CanvasGroup.blocksRaycasts` được bật khi panel hiện và tắt khi hide.

### 14.4 Restart và Home

- `RestartGame()` chỉ hợp lệ khi paused; `PrepareSceneExit()` disable interaction/button và stop song, sau đó gọi `ResultScreenController.RestartGame()`.
- `ReturnHome()` làm tương tự rồi load MainMenu.
- Khi `GameOver` hoặc `GameWon`, `HandleTerminalState()` đánh dấu terminal, hide pause UI và disable pause availability.

---

## 15. Result System

### 15.1 Event/data flow

`ResultScreenController.OnEnable()` subscribe hai instance event của `GameplayController`:

```text
GameplayController.GameOver → ShowGameOver()
GameplayController.GameWon  → ShowWin()
```

Hai flow cùng đi qua `ShowResult(...)`, nhưng dùng panel/card/final-score text khác nhau. `resultVisible` bảo đảm chỉ một terminal panel được present.

### 15.2 Presentation

Khi hiện result:

1. Panel tương ứng active.
2. Canvas alpha = 0.
3. Interaction tạm disable nhưng `blocksRaycasts = true`, tránh input xuyên xuống gameplay.
4. Card bắt đầu ở scale 0.88.
5. Score text bắt đầu từ `0`.
6. `PresentResult()` dùng `Time.unscaledDeltaTime`.

Entrance duration là 0.34 giây; score count duration là 0.45 giây. Card grow tới overshoot 1.03 tại 72% progress rồi settle về 1. Khi entrance đã hoàn tất, CanvasGroup được interactable; kết thúc coroutine, score được set chính xác bằng `scoreManager.Score`.

### 15.3 Victory / Game Over / navigation

- `ShowGameOver()` gọi `SfxController.PlayGameOver()` sau khi panel được accept.
- `ShowWin()` gọi `PlayVictory()`.
- `RestartGame()` disable interaction, phát UI click, rồi gọi `GameplayController.RestartGame()`, reload active scene.
- `ReturnHome()` disable interaction, phát UI click, load `MainMenu`.

Retry giữ selected song vì `SelectedSongContext` là static và không bị clear. Home không tự clear context, nhưng menu selection tiếp theo sẽ overwrite nó.

`GameplayController.Update()` cũng hỗ trợ phím `R` khi đã Win/GameOver, một editor/debug convenience ngoài result button.

---

## 16. Video Background

### 16.1 Asset và theme mapping

Production có ba MP4:

| Video | Vai trò | Source size xấp xỉ |
|---|---|---:|
| `MainMenuBackground.mp4` | Main Menu | 3.6 MB |
| `DemoBackground.mp4` | Demo + Sonata 01 + Sonata 02 | 44.9 MB |
| `SakuraBackground.mp4` | Sakura | 298.9 MB |

Demo/Sonata share một gameplay video nhưng theme overlay và procedural color/speed khác. Sakura có video riêng.

### 16.2 Main Menu implementation

`MenuVideoBackground.Awake()`:

- validate clip/camera;
- fallback camera reference bằng `Camera.main` nếu chưa serialize;
- lấy hoặc add `VideoPlayer`;
- set `playOnAwake = false`, loop, `waitForFirstFrame`, `skipOnDrop`;
- render `CameraFarPlane`, aspect `FitOutside`;
- target camera alpha 1;
- `audioOutputMode = None`;
- assign clip và gọi `Play()`.

Main Menu không có `Prepare()`, `prepareCompleted`, `frameReady`, timeout hoặc explicit fallback. Nếu video lỗi, scene chỉ còn presentation bên dưới/camera clear; không có controller báo readiness.

### 16.3 Gameplay implementation

`DynamicBackgroundController` cũng lấy hoặc add `VideoPlayer`, dùng `Camera.main`, render far plane, loop, `FitOutside`, không audio, `waitForFirstFrame` và `skipOnDrop`.

Khác biệt quan trọng là startup protocol:

```text
ApplyTheme()
→ ApplyVideo()
→ ResetReadiness()
→ VideoPlayer.Prepare()
→ prepareCompleted
→ VideoPlayer.Play()
→ frameReady × 4 advancing frames
→ ResolveReadiness()
→ BackgroundReady event
```

Nếu theme null hoặc VideoClip null, controller resolve readiness ngay và dùng sprite/procedural background. Nếu `errorReceived` hoặc timeout 10 giây, video stop nhưng startup vẫn được unblock.

Fallback không hoàn toàn tái-theme lại base color: với một non-null clip bị fail, `ApplyTheme()` đã dùng `VideoOverlayColor`; controller giữ procedural/decorative elements nhưng không chuyển base color sang nhánh `Color.Lerp(black, PrimaryColor, 0.22)` dành cho clip null.

### 16.4 Android transcoding

Ba `.mp4.meta` đều có Android target setting với `enableTranscoding: 1`, codec serialized value 1 và không import audio.

- Main menu và Demo target 720×1280.
- Sakura target 1280×720, khác orientation với portrait gameplay; `FitOutside` sẽ crop để fill camera.

Project documentation ghi nhận Windows Media Foundation đã cảnh báo corrected H.264 timestamps và unknown color primaries cho Main Menu video. Playback đã chạy trong Editor/build, nhưng warning này là lý do cần test decoder/device thật thay vì coi Editor là đủ.

### 16.5 Lợi ích và trade-off khách quan

Lợi ích:

- tạo chuyển động nền nhanh;
- dễ tạo mood khác nhau giữa song;
- ít custom shader/animation authoring;
- hardware video decoder có thể hiệu quả trên device phù hợp.

Trade-off:

- source Sakura rất lớn, ảnh hưởng repository/build pipeline và có thể tăng package;
- startup phải chờ prepare/decode frame;
- decoder cần native buffers/memory, có battery/thermal cost;
- codec/color/timestamp behavior khác nhau giữa Android device;
- video có thể cạnh tranh bandwidth/compositing với gameplay;
- aspect crop có thể làm mất visual content;
- background không sync beat và vẫn chạy khi pause;
- lợi ích chỉ decorative nhưng lại trở thành startup dependency;
- chưa có Android-device profiling nên chưa chứng minh cost/benefit thực tế.

Reviewer concern ở đây hợp lý: implementation đã xử lý failure tốt, nhưng engineering complexity và media cost có thể lớn hơn giá trị gameplay mà video mang lại.

---

## 17. Android / Mobile

### 17.1 Settings có thể xác minh

- Unity: `2022.3.62f2`.
- Product: `Magic Tiles`.
- Android application identifier: `com.nguyenhung.magictiles`.
- Version: `1.0.0`, bundle version code 1.
- Minimum SDK: 22.
- Target SDK: serialized `0`, nghĩa là Unity chọn Automatic theo installed toolchain.
- Architecture mask: `1`, project documentation xác định là ARMv7.
- Android fullscreen bật; resizable window tắt.
- Android Swappy bật.
- Incremental GC bật.
- Portrait là orientation chính; chỉ portrait được cho phép trong các autorotate flags, upside-down/landscape tắt.
- Build Settings chỉ enable MainMenu và Gameplay.

Source-controlled settings **không chứng minh một final non-Development APK**. `Docs/README.md` hiện chỉ ghi local **Development APK** đã build thành công và build artifact bị exclude. Vì vậy khi phỏng vấn không nên claim repository chứng minh release/non-Development build. Câu trả lời chính xác là cấu hình Android đã có, nhưng physical-device và final release validation còn pending.

### 17.2 Safe Area và responsive layout

`SafeAreaFitter` đọc `Screen.safeArea`, convert pixel rect thành normalized anchors, set offsets về zero và chỉ update nếu resolution/safe area thay đổi. `isApplying` ngăn callback `OnRectTransformDimensionsChange()` tự lặp khi component đang set anchor.

`GameplayResponsiveLayout`:

- width-lock orthographic camera so với reference 1080×1920;
- đặt spawn/hit y theo viewport;
- scale base background phủ màn hình với overscan 1.01;
- scale bốn lane flashes và ba separators theo visible world height.

### 17.3 Mobile-specific considerations trong project

- Touch loop hỗ trợ multi-touch và finger-specific UI filtering.
- Video được transcode cho Android và tắt audio track.
- Audio data load background/preload gate giảm khả năng START trong lúc clip chưa sẵn sàng.
- Safe Area xử lý notch/cutout ở UI layer.
- `androidRenderOutsideSafeArea = 1` cho phép background phủ toàn màn hình trong khi UI root tự fit Safe Area.
- ARMv7 giúp hỗ trợ thiết bị cũ hơn nhưng device capability/video decoder cần kiểm tra thật.
- Chưa có evidence physical Android cho touch feel, audio latency, thermal, video decode, Safe Area và sustained performance.

---

## 18. Design Patterns

### 18.1 Observer / Event-driven

**Class:** `Note`, `NoteInputController`, `ScoreManager`, `GameplayController`, HUD/VFX/result/pause controllers.

**Cách dùng:** Publisher phát C# event; subscriber attach trong `OnEnable` và detach trong `OnDisable`.

**Phù hợp vì:** Note không cần biết HUD, score, particle hay result UI cụ thể.

**Trade-off:** Static event có global lifetime; subscriber order implicit; flow khó trace hơn direct call.

**Mức độ:** Observer/event-driven thật, dù không có interface Observer formal.

### 18.2 Singleton

**Class:** `SfxController`.

**Cách dùng:** static `Instance`, duplicate tự destroy, instance sống qua scene.

**Phù hợp vì:** short SFX cần dùng từ menu, startup, pause và result mà không nối reference qua mọi scene transition.

**Trade-off:** global dependency, khó isolate test, scene vẫn tạo duplicate rồi destroy.

**Mức độ:** Singleton formal ở MonoBehaviour level.

`SelectedSongContext` là static global context/service-like object, nhưng không phải Singleton instance formal.

### 18.3 Object Pool

**Class:** `HitBurstController`.

**Cách dùng:** pre-instantiate 16 ParticleSystem, acquire/recycle/reset.

**Phù hợp vì:** hit effect lặp lại và có thể overlap; tránh particle hierarchy Instantiate/Destroy trên hit path.

**Trade-off:** capacity cố định; full pool recycle effect còn active; tốn upfront objects.

**Mức độ:** Object Pool thật.

### 18.4 State-like flow

**Class:** `GameplayStartupController`.

**Cách dùng:** private enum và guard theo `PreparingBackground`, `WarmingUp`, `ReadyToStart`, `Playing`.

**Phù hợp vì:** startup có các phase và chỉ một transition hợp lệ tại mỗi thời điểm.

**Trade-off:** behavior vẫn nằm trong một class; thêm nhiều state có thể làm controller phình.

**Mức độ:** state-machine-like, không phải State Pattern bằng polymorphic state object.

### 18.5 Data-driven design / ScriptableObject configuration

**Class/asset:** `SongDefinition`, `SongCatalog`, `BackgroundTheme`, JSON chart.

**Cách dùng:** behavior chung đọc data reference; không branch theo từng song.

**Phù hợp vì:** content mở rộng độc lập với scene/gameplay code.

**Trade-off:** serialized reference có thể thiếu/sai; schema migration cần quản lý.

**Mức độ:** data-driven architecture thật, kết hợp ScriptableObject-based configuration.

### 18.6 Factory-like creation

**Vị trí:** `MainMenuController` instantiate card, `NoteSpawner` instantiate Note, `HitBurstController` tạo pool.

**Cách dùng:** prefab đóng vai trò prototype/template cho object runtime.

**Trade-off:** caller biết concrete prefab và policy lifecycle.

**Mức độ:** chỉ factory-like creation qua Unity `Instantiate`, không có Factory class/Factory Method formal.

Không nên gọi các `switch` judgement hoặc lane algorithm là Strategy Pattern: policy không được encapsulate sau interface và không replace runtime. Chúng chỉ là branch/heuristic rõ ràng.

---

## 19. Separation of Concerns

| Class | Chịu trách nhiệm | Phụ thuộc | Ai dùng nó | Vì sao không nằm ở class khác |
|---|---|---|---|---|
| `SongConductor` | Audio schedule, SongTime, pause/resume/stop | AudioSource, AudioClip, DSP clock | Startup, spawner, Note, pause, session | Nếu spawner hoặc Note tự quản clock sẽ tạo nhiều source of truth |
| `JsonChartLoader` | Parse chart JSON và readiness | TextAsset, JsonUtility | GameplaySongLoader, NoteSpawner | MIDI/build logic không nên nằm runtime; spawner không nên parse data |
| `GameplaySongLoader` | Resolve selected/default song, route assets | Context, conductor, chart/background loader | Unity lifecycle | Nó là composition/loading boundary, không phải readiness coordinator |
| `GameplayStartupController` | Điều phối readiness và START state | Background, conductor, spawner, input, pause, tile | Scene startup | Media owners tự report readiness; controller chỉ compose gates |
| `DynamicBackgroundController` | Video/procedural configuration và visual readiness | BackgroundTheme, VideoPlayer, renderers | Song loader/startup | Không được quyết định khi nhạc/gameplay bắt đầu trực tiếp |
| `NoteSpawner` | Schedule/instantiate notes | Conductor, chart, prefab, LaneView[] | Startup/session | Không judge hoặc score để chart scheduling độc lập rules |
| `Note` | Movement, own resolved state, emit result | Conductor, renderer/collider, judge | Input, spawner, event subscribers | Không biết global score/UI/session để prefab giữ focused behavior |
| `NoteInputController` | Pointer conversion và direct collider hit | Camera, Input, EventSystem, Physics2D | Startup/pause/session | Không tính score/judgement; chỉ route input tới Note |
| `ScoreManager` | Score/Perfect combo | `Note.Judged` | HUD/result | Session controller chỉ quyết định terminal state, không presentation score |
| `GameplayController` | Win/GameOver và session stop | Conductor, spawner, input | Pause/result | Không animate UI hoặc play VFX, giúp rules tách presentation |
| `HitBurstController` | Pooled successful-hit particle | Prefab, `Note.HitSucceeded` | Presentation layer | Note không nên instantiate/configure particle effects |
| `SfxController` | Persistent short SFX playback | AudioSource/clips | Menu/gameplay UI/failure/result | Music AudioSource cần lifecycle/timing riêng trong conductor |

Separation nhìn chung tốt: data owner, time owner, spawning, note behavior, rules và presentation không bị gom vào một `GameManager`. Điểm coupling còn lại chủ yếu đến từ serialized scene references và static events/context.

---

## 20. Events / Unity Lifecycle

### 20.1 Event flow

| Event | Publisher | Subscriber | Khi phát | Mục đích |
|---|---|---|---|---|
| `Note.Judged` | `Note` | `ScoreManager`, `GameplayController`, `GameplayHUD`, `FailureFeedbackController` | Successful hit hoặc natural Miss | Phân phối judgement cho rules/UI/failure |
| `Note.HitSucceeded` | `Note` | `HitBurstController`, `LaneFlashController` | Sau successful judgement | VFX theo lane/quality/position |
| `NoteInputController.FailedInput` | Input controller | `GameplayController`, `FailureFeedbackController` | Touch/click không trúng Note | Game Over và wrong-input feedback |
| `ScoreManager.ScoreChanged` | `ScoreManager` | `GameplayHUD` | Sau Perfect/Great/Good/Miss | Update score/combo presentation |
| `GameplayController.GameOver` | `GameplayController` | `ResultScreenController`, `PauseMenuController` | Terminal failure | Result panel và disable pause |
| `GameplayController.GameWon` | `GameplayController` | `ResultScreenController`, `PauseMenuController` | Tất cả note hit + song finished | Victory panel và disable pause |
| `DynamicBackgroundController.BackgroundReady` | Background controller | `GameplayStartupController` | Four-frame success hoặc fallback | Unblock startup gate |

### 20.2 Event lifetime

Các subscriber component pair `+=` trong `OnEnable` và `-=` trong `OnDisable`. Đây là practice tốt vì object inactive không tiếp tục nhận static event. `SongCardView` remove button listener trong `OnDestroy` và remove-before-add trong `Bind`.

Risk chính:

- `Note.Judged`, `Note.HitSucceeded`, `FailedInput` là static; nếu subscriber quên unsubscribe, static delegate có thể giữ reference sau scene unload.
- Current subscribers đều unsubscribe, nên risk đã được quản lý.
- C# event subscriber order không phải explicit contract; gameplay không nên phụ thuộc ScoreManager chạy trước GameplayController.
- `ScoreChanged`, `GameOver`, `GameWon`, `BackgroundReady` là instance event nên lifetime tự nhiên gắn với owner rõ hơn.

### 20.3 Unity lifecycle theo class quan trọng

| Class | Lifecycle | Logic và lý do |
|---|---|---|
| `SfxController` | `Awake` | Singleton phải được resolve trước component khác gọi static playback |
|  | `OnDestroy` | Clear static Instance an toàn |
| `MainMenuController` | `Start` | Sinh UI sau toàn bộ scene `Awake` và reference initialization |
| `GameplaySongLoader` | `Awake` | Gán song/chart/background sớm trước startup `Start` readiness loop |
| `GameplayStartupController` | `Awake` | Gate input/spawner/pause ngay trước frame đầu |
|  | `OnEnable/OnDisable` | Quản lý `BackgroundReady` subscription |
|  | `Start` | Prewarm và bắt đầu coroutine sau mọi `Awake` |
|  | `Update` | Chỉ pulse Loading khi chưa Ready/Playing |
| `DynamicBackgroundController` | `Awake` | Cache VideoPlayer và transform state |
|  | `OnEnable/OnDisable` | Attach/detach VideoPlayer callbacks, stop timeout |
|  | `Update` | Continuous procedural transform animation |
| `SongConductor` | `Awake` | Validate AudioSource trước mọi call |
| `NoteSpawner` | `Awake` | Validate config và bốn lane |
|  | `Update` | So sánh continuous SongTime với next spawnTime |
| `Note` | `Update` | Derive position và natural miss mỗi frame |
| `NoteInputController` | `Awake` | Validate camera |
|  | `Update` | Poll legacy mouse/touch input |
| `ScoreManager` | `OnEnable/OnDisable` | Event listener active đúng theo component lifetime |
| `GameplayController` | `OnEnable/OnDisable` | Subscribe judgement/failed input |
|  | `Update` | Check Win khi note count và song completion thay đổi theo thời gian |
| `HitBurstController` | `Awake` | Pool allocation trước gameplay |
|  | `OnEnable/OnDisable` | Event lifetime và reset pool |
|  | `Update` | Reclaim completed particles |
| UI controllers | `Awake` | Cache base scale/hide initial panels/validate refs |
|  | `OnEnable/OnDisable` | Subscribe event và stop/reset coroutine |

Một subtle lifecycle point: thứ tự `Awake()` giữa `GameplaySongLoader` và `GameplayStartupController` không được set bằng Script Execution Order. Thiết kế bù bằng việc mọi `Awake` hoàn thành trước `Start`, và startup `Start` poll property readiness ngoài event.

---

## 21. Production Performance

### 21.1 Hot Path

- `Note.Update()` cho mỗi active Note: đọc SongTime, tính progress, Lerp, miss/cleanup check.
- `NoteSpawner.Update()`: đọc SongTime và spawn tất cả due notes.
- `NoteInputController.Update()`: poll touch/mouse; physics query chỉ khi input bắt đầu.
- `GameplayController.Update()`: terminal guard và win conditions.
- `DynamicBackgroundController.Update()`: sin/cos/rotation/scale cho animated transforms.
- `HitBurstController.Update()`: scan fixed pool 16.
- Active VFX/HUD coroutines.
- `VideoPlayer` decode/composite là native/media hot workload dù không biểu hiện thành C# allocation đơn giản.

### 21.2 One-time Path

- Main Menu instantiate bốn song card và destroy placeholder children.
- JSON deserialize/list population.
- `GetComponent<VideoPlayer>()` hoặc `AddComponent<VideoPlayer>()` khi setup.
- `Camera.main` lookup khi background target chưa có/cache setup.
- HitBurst instantiate 16 pool objects ở `Awake`.
- Responsive layout và Safe Area calculation.
- SFX duplicate destroy khi đổi scene.

### 21.3 Startup Path

- Video `Prepare`, decode initial frames và timeout coroutine.
- `AudioClip.LoadAudioData()`.
- Note representative instantiate/destroy prewarm.
- Loading pulse `Update` trong lúc chờ.
- Chart parse chạy trước readiness coroutine trong `GameplaySongLoader.Awake()`.

### 21.4 Terminal Path

- `GameplayController.CancelActiveNotes()` gọi `FindObjectsByType<Note>` và tạo array result.
- Mỗi active Note gọi `Destroy`.
- Result panel coroutine và numeric score animation.
- GameOver SFX tạo một delayed coroutine/`WaitForSecondsRealtime`.

Đây không phải hot gameplay path vì chỉ chạy một lần khi fail/result.

### 21.5 Pooled Path

- HitBurst: pooled hoàn toàn sau `Awake`.
- Lane flash: reuse SpriteRenderer nhưng tạo/restart coroutine.
- Failure feedback: reuse Image/camera transform nhưng tạo coroutine khi fail.
- Note: **không pooled**.

### 21.6 API/allocation audit

| Hạng mục | Hiện trạng |
|---|---|
| `Instantiate` | SongCard one-time; 16 HitBurst startup; one prewarm Note; một lần cho mỗi gameplay Note |
| `Destroy` | Menu placeholders, SFX duplicate, prewarm Note, resolved/cancelled Notes |
| `GetComponent` | Per tap trên collider; VideoPlayer setup one-time |
| `Find` | Không có string-based `Find` trong production |
| `FindObjectsByType` | Chỉ terminal Game Over để cancel Notes |
| LINQ | Không có trong production hoặc Task 2 hot path |
| Coroutine | Startup, Note fade, HUD punches, lane/failure feedback, pause/result animation, delayed SFX |
| Strings | `ToString`, `$"x{combo}"`, score updates tạo managed allocation nhỏ |
| Physics | `Physics2D.OverlapPoint` một query mỗi gameplay press |
| Audio | Music scheduled riêng; short SFX `PlayOneShot` |
| Video | Native decoder/buffer/render cost, cần profile trên device |

Không phải mọi Instantiate đều xấu: card/pool/prewarm xảy ra một lần ở non-hot path. Per-note Instantiate/Destroy mới cần đánh giá theo mật độ và profiler.

### 21.7 Note Instantiate / Destroy trade-off

Với home test, lựa chọn này có thể acceptable vì:

- Note prefab nhỏ: transform, SpriteRenderer, BoxCollider2D và `Note` behavior;
- chỉ note trong travel window 2 giây tồn tại cùng lúc;
- code lifecycle rất dễ hiểu và ít reset bug;
- run kết thúc ngay ở một miss, nhiều run không đi hết chart;
- scope ưu tiên correctness/clarity hơn infrastructure.

Rủi ro:

- mỗi Note tạo/hủy managed wrapper và native Unity object;
- `Destroy` deferred, tạo churn và khả năng spike;
- successful hit còn tạo `FadeResolvedVisual` coroutine;
- frequent retry lặp lại toàn bộ lifecycle;
- low-end Android nhạy hơn Editor.

Sonata 01 là stress case rõ nhất: 821 note, dài 272 giây, minimum interval 0.083333 giây. Dù lane balance tốt và average density thấp hơn peak, đoạn dày có thể spawn khoảng 12 note/giây; với travel 2 giây có thể có nhiều Note overlap về lifetime.

Production nên chuyển sang Note Object Pool khi device Profiler cho thấy spawn/destroy hoặc GC/native allocation gây frame spike, hoặc khi chart dài/dày hơn, thêm simultaneous notes, hold notes, effect child, hay target hardware thấp hơn. Pool cần reset đầy đủ conductor, timing, lane, collider, renderer alpha, coroutine và resolved state; nếu chưa có measured bottleneck, complexity đó có thể chưa đáng.

---

## 22. Task 2 Optimization

### 22.1 Tổng quan methodology

Source package gốc được giữ nguyên:

```text
Original ParticleEffectsUnoptimize.prefab
→ OptimizeBefore.unity
→ deterministic benchmark/profile
→ identify dense trail + compatible rain renderers
→ experiment
→ Profiler + Frame Debugger + visual validation
→ keep trail/rain changes
→ reject zap/material/mask/visual-loss experiments
→ ParticleEffectsOptimized.prefab
→ OptimizeAfter.unity
```

`OptimizationBenchmarkRunner` cache ParticleSystem theo depth-first/sibling order, stop/clear cả bốn variant, set deterministic seeds bắt đầu từ 1337, rồi chỉ play selected variant ở frame kế tiếp. PL1/PL2/PL3 là ba effect level/workload tăng dần, không phải ba version code nối tiếp.

Test là controlled Editor measurement 1080×1920 trong Built-in Render Pipeline; không phải Android FPS claim.

### 22.2 Draw Call, Batch và SetPass Call bằng evidence thật

- **Draw Call:** một rendering submission. PL3 tổng giảm `9 → 8` vì rain group từ hai renderer submission thành một.
- **Batch:** đơn vị Unity gom/submit sau batching decision. PL3 vẫn `8 → 8`; giảm Draw Call được quan sát không đồng nghĩa batch counter phải giảm cùng cách.
- **SetPass Call:** lần đổi shader/material pass state. PL3 vẫn `7 → 7`; rain dùng compatible render setup nên consolidate submission không tạo thêm material-state saving.

Kết quả:

| Variant | Draw Calls | Batches | SetPass Calls |
|---|---:|---:|---:|
| PL1 before → after | 7 → 7 | 7 → 7 | 7 → 7 |
| PL2 before → after | 9 → 8 | 8 → 8 | 8 → 8 |
| PL3 before → after | 9 → 8 | 8 → 8 | 7 → 7 |

Điểm phỏng vấn quan trọng: ba counter liên quan nhưng không interchangeable. Phải dùng Frame Debugger để map aggregate counter về renderer/event cụ thể.

### 22.3 Geometry optimization: trail Minimum Vertex Distance

Ba `lines` trail trong PL1/PL2/PL3 đổi:

```text
Trails > Minimum Vertex Distance: 0.2 → 0.4
```

Minimum Vertex Distance quyết định emitter phải đi bao xa trước khi trail thêm vertex mới. Tăng từ 0.2 lên 0.4 làm trail sample thưa hơn, giảm generated vertices/triangles nhưng không xóa emitter, material, lifetime hay renderer.

| Variant | Triangles | Vertices |
|---|---:|---:|
| PL1 | 126 → 86 | 164 → 124 |
| PL2 | 196 → 156 | 240 → 200 |
| PL3 | 410 → 224 | 460 → 274 |

PL3 giảm 186 triangles (45.4%) và 186 vertices (40.4%) trong dedicated geometry samples. Geometry frame và draw-call frame được capture riêng vì trail geometry thay đổi theo lifetime; report không trộn counter giữa hai frame.

Không tăng value quá cao vì trail sẽ trở nên gãy/coarse, làm visual silhouette thay đổi. `0.4` là compromise được fixed-phase visual comparison chấp nhận, không phải “càng lớn càng tốt”.

### 22.4 Rain consolidation

PL2 và PL3 có hai mirrored simulation `rain3` và `rain4`. Final optimized prefab:

- vẫn chạy cả hai simulation với transform/shape/timing/seed gốc;
- disable `rain4` `ParticleSystemRenderer`;
- add `ParticleSystemRendererConsolidator` cho PL2 và PL3;
- copy particle source sang target renderer `rain3`.

`ParticleSystemRendererConsolidator.Awake()` allocate:

```text
targetParticles[target.maxParticles + source.maxParticles]
sourceParticles[source.maxParticles]
transferredSourceSeeds[source.maxParticles]
```

`LateUpdate()`:

1. Reset state nếu source stopped.
2. Return nếu transfer complete.
3. `source.GetParticles()`.
4. Nếu có particle, `target.GetParticles()`.
5. Bỏ source seed đã transfer.
6. Convert position, velocity, axis-of-rotation giữa Local/Custom/World simulation spaces.
7. Append vào target buffer.
8. `target.SetParticles()` một lần và set `transferComplete = true`.

Đây là targeted one-shot consolidation. Nó chỉ an toàn vì rain systems tương thích và emission behavior phù hợp assumption “transfer xong một lần”. Không phải generic merger cho mọi ParticleSystem.

Frame Debugger evidence cho rain group:

```text
Before: 2 Draw Calls, 16 vertices, 24 indices
After:  1 Draw Call,  16 vertices, 24 indices
```

Geometry hai bên vẫn còn, chỉ renderer submission được hợp nhất.

### 22.5 `GetParticles` / CPU metric

Qua năm deterministic replay:

```text
PL2: 235 → 5 source GetParticles calls
PL3: 234 → 5 source GetParticles calls
```

Final có năm target reads và năm target writes: một completed transfer mỗi replay. `transferComplete` loại polling lặp lại sau khi copy.

**`GetParticles` call không phải Draw Call.** Đây là CPU/API work trong C# helper. Draw Call là GPU/render submission được ghi qua rendering counters/Frame Debugger. Giảm `GetParticles` bảo vệ CPU hot path; nó không tự động chứng minh GPU call giảm.

### 22.6 GC Allocation claim

Claim chính xác là:

> `0 B recurring managed allocation` trong tested Editor steady-state và transfer hot path.

Không được nói “scene zero allocation” hoặc “toàn bộ game zero GC”. Ba buffer array vẫn allocate một lần trong `ParticleSystemRendererConsolidator.Awake()`. Benchmark cache arrays cũng allocate khi initialize. Wording scoped vì evidence chỉ đo đoạn hot path/replay đã định nghĩa, không bao gồm Editor overhead, initialization hay toàn scene.

### 22.7 Rejected optimization

#### Zap consolidation

Gộp Transition `zap1` và `zap2` từng giảm:

```text
6 → 4 Draw Calls
```

Nhưng hai system có independent trail history. Khi append particle vào cùng renderer/simulation, trail kết nối sai giữa hai history và tạo horizontal links không có trong visual gốc. Đây là visual regression; revert là quyết định đúng vì numerical improvement không còn preserve intended output. Final Transition giữ 6 Draw Calls và không có zap consolidator.

#### Các experiment khác

- Mask-interaction normalization không đổi Draw Calls/Batches/SetPass.
- Shared material/atlas prototype không giảm measured submissions và tăng configuration complexity.
- Disable `init`, `glow` hoặc `outline_line` giảm một submission nhưng làm yếu impact/color/silhouette.

Rule rút ra: optimization chỉ accept khi có measured benefit **và** visual correctness.

### 22.8 Ý nghĩa thực tế

Final PL3:

```text
Draw Calls: 9 → 8  (11.1%)
Triangles:  410 → 224 (45.4%)
Vertices:   460 → 274 (40.4%)
```

Percentage geometry đẹp vì denominator nhỏ. Absolute saving chỉ là một Draw Call và 186 triangles/vertices — rất nhỏ so với một full mobile frame có UI, video decoder/composite và toàn gameplay. Batches/SetPass không đổi. Vì benchmark chỉ isolate effect trong Editor, chưa biết production bottleneck là CPU, GPU, fill-rate, video, memory hay thermal.

Reviewer nói improvement khoảng 5–10%/không đáng kể có thể đang đánh giá **practical frame impact**, không phủ nhận correctness của measurement. Điểm tốt là methodology trung thực, rejected experiment hợp lý và metric separation rõ. Điểm hạn chế là effort/complexity của custom consolidator có thể lớn hơn absolute saving.

Kết luận cần nhớ khi phỏng vấn:

> Optimization tốt không phải chỉ tạo percentage đẹp; phải tìm frame-time bottleneck thật, đo absolute milliseconds trên target device, giữ visual correctness và cân complexity bảo trì.

---

## 23. Top 10 Technical Decisions

### 23.1 DSP làm source of truth

**DECISION:** Dùng `AudioSettings.dspTime` và `PlayScheduled`.

**CONTEXT:** Rhythm game cần Note/audio cùng timeline.

**WHY:** DSP clock cùng domain với audio scheduler, chính xác hơn frame clock.

**BENEFIT:** Không tích lũy drift; pause rebase rõ; pre-roll hỗ trợ note đầu.

**TRADE-OFF:** Logic negative SongTime/rebase khó hơn `AudioSource.Play()` đơn giản.

**ALTERNATIVE:** `AudioSource.timeSamples`, `AudioSource.time` hoặc deltaTime clock.

**WHAT I WOULD DO IN PRODUCTION:** Giữ DSP, thêm per-device latency calibration, chart offset và automated sync tests.

### 23.2 Preprocess MIDI trong Editor

**DECISION:** DryWetMIDI chỉ dùng ở Editor tool.

**CONTEXT:** Source MIDI có tempo, track, duplicate và chord không phù hợp trực tiếp simplified gameplay.

**WHY:** Chuyển complexity/validation sang authoring time.

**BENEFIT:** Runtime nhỏ, deterministic và dễ debug.

**TRADE-OFF:** Source đổi phải regenerate; heuristic source-specific.

**ALTERNATIVE:** Parse runtime hoặc author chart thủ công.

**PRODUCTION:** Giữ preprocessing nhưng biến track/policy/offset thành import profile per song và có batch regression tests.

### 23.3 JSON làm runtime chart

**DECISION:** Runtime consume `TextAsset` JSON `{lane, hitTime}`.

**CONTEXT:** Gameplay chỉ cần hai field.

**WHY:** `JsonUtility` đủ đơn giản và output readable/diffable.

**BENEFIT:** Không mang MIDI semantics vào runtime.

**TRADE-OFF:** Loader runtime chưa revalidate toàn bộ invariant; JSON dài hơn binary.

**ALTERNATIVE:** Binary blob hoặc ScriptableObject chart.

**PRODUCTION:** Thêm schema/version/offset metadata và defensive runtime validation.

### 23.4 ScriptableObject song catalog

**DECISION:** `SongCatalog` + `SongDefinition` + `BackgroundTheme`.

**CONTEXT:** Bốn song dùng cùng scene/behavior.

**WHY:** Inspector-friendly content composition, tránh per-song branch.

**BENEFIT:** Data-driven extension và asset references rõ.

**TRADE-OFF:** Missing serialized reference; bundle/package vẫn chứa media trực tiếp.

**ALTERNATIVE:** Addressables/content database/remote config.

**PRODUCTION:** Giữ schema data-driven, cân nhắc Addressables cho media lớn.

### 23.5 Static selected-song context

**DECISION:** Chuyển `SongDefinition` qua `SelectedSongContext` static.

**CONTEXT:** Chỉ có MainMenu → Gameplay và Retry/Home.

**WHY:** Cách nhỏ nhất để giữ lựa chọn qua scene.

**BENEFIT:** Không cần persistent manager phức tạp; Retry tự giữ song.

**TRADE-OFF:** Global state, không tự clear, khó test/lifetime implicit.

**ALTERNATIVE:** Persistent session service hoặc scene transition payload.

**PRODUCTION:** Dùng explicit application/session state với lifecycle rõ.

### 23.6 Direct Note collider + position judgement

**DECISION:** Touch đúng Note GameObject; judgement theo y distance.

**CONTEXT:** Visual interaction của Magic Tiles là bấm tile đang rơi.

**WHY:** Input mapping rất trực tiếp, VFX có đúng world position.

**BENEFIT:** Code nhỏ, dễ hiểu, hỗ trợ mouse/touch giống nhau.

**TRADE-OFF:** Overlap query ambiguous; Good không outer limit; spatial threshold phụ thuộc layout.

**ALTERNATIVE:** Lane input chọn candidate gần hitTime nhất theo temporal window.

**PRODUCTION:** Kết hợp direct visual với deterministic candidate selection và explicit early/late windows.

### 23.7 Scheduled pre-roll 2.5 s / travel 2 s

**DECISION:** Schedule audio tương lai và cho Note spawn trong negative SongTime.

**CONTEXT:** Chart có note tại hitTime 0.

**WHY:** Note phải có đủ 2 giây travel trước beat đầu.

**BENEFIT:** First note không spawn trễ; cùng DSP origin.

**TRADE-OFF:** Người chơi thấy delay sau START; constants nằm ở scene.

**ALTERNATIVE:** Chart offset, count-in hoặc pre-spawn state.

**PRODUCTION:** Derive pre-roll từ maximum travel/warm state và làm count-in rõ về UX.

### 23.8 Explicit startup warmup gate

**DECISION:** Chờ video frames, loaded audio, chart, spawner và prewarm trước START.

**CONTEXT:** Tránh start hitch/blank background.

**WHY:** START cần là lightweight transition vào ready gameplay.

**BENEFIT:** Predictable first interaction và fallback media failure.

**TRADE-OFF:** Decorative video làm tăng latency/complexity.

**ALTERNATIVE:** Start trên static fallback, video fade-in async.

**PRODUCTION:** Giữ gameplay-critical gates; đánh giá liệu decorative gate có cần blocking hay không.

### 23.9 Pool HitBurst, không pool Note

**DECISION:** Particle effect pool 16; Note Instantiate/Destroy.

**CONTEXT:** Scope home test, VFX hierarchy khác Note đơn giản.

**WHY:** Giảm churn ở repeated hit effect mà không thêm reset contract cho Note.

**BENEFIT:** VFX stable, Note implementation rõ.

**TRADE-OFF:** Sonata 01 vẫn tạo/hủy 821 Note nếu complete.

**ALTERNATIVE:** Pool cả Note và VFX.

**PRODUCTION:** Profile device; chỉ pool Note khi measured spike hoặc content density yêu cầu.

### 23.10 Video background

**DECISION:** MP4 loop render trên camera far plane.

**CONTEXT:** Cần animated visual nhanh cho menu/song themes.

**WHY:** Rich movement với ít custom rendering code.

**BENEFIT:** Visual differentiation nhanh, theme data reuse.

**TRADE-OFF:** File size, decoder/startup/memory/device risk; không sync gameplay.

**ALTERNATIVE:** Shader/procedural/sprite animation/static art.

**PRODUCTION:** Đặt media/performance budget, profile thiết bị, chỉ giữ video nếu visual value vượt cost.

---

## 24. Reviewer Feedback Analysis

### 24.1 Điểm tốt: convention, structure, folder/module

Reviewer có cơ sở đánh giá tốt vì source chia `Runtime/Chart`, `Gameplay`, `Songs`, `Timing`, `Visual`, `UI`, `VFX`, `Audio`, `Editor/ChartImport`. Naming phần lớn nói đúng intent; dependency được serialize/validate; event presentation tách rules. Không có một “God GameManager” ôm mọi việc.

Bài học: tiếp tục giữ class nhỏ và boundary rõ, nhưng cần cân bằng kiến trúc đẹp với product impact.

### 24.2 Điểm tốt: MIDI parser tool

Tool có extraction, tempo conversion, deterministic sorting, dedup, tick grouping, representative policy, adaptive lanes, validation, report và tests. Đây là engineering evidence tốt hơn một script conversion ad-hoc.

Bài học: trong interview cần nói rõ heuristic/limitation, không mô tả nó như general-purpose melody detector.

### 24.3 Concern: gameplay còn hạn chế

**Reviewer có thể thấy:** Gameplay chỉ có single tap, bốn lane, không hold/chord runtime/difficulty curve; một lỗi kết thúc run; successful Good không có outer limit.

**Project support concern:** Editor pipeline còn chủ động loại harmony bằng one Tick → one note. Input không có lane candidate/timing calibration. Win condition đơn giản.

**Vẫn làm tốt:** Bốn chart thật, DSP sync, three-tier scoring, combo, pause, result và deterministic content.

**Decision chưa tối ưu:** Nhiều effort ở content/tool/presentation hơn depth và feel của core loop.

**Senior có thể ưu tiên:** Trước hết verify hit feel, latency, chart readability, difficulty và repeated play value trên device.

**Bài học:** Technical sophistication không thay thế gameplay depth; architecture phải phục vụ player experience.

### 24.4 Concern: UI/VFX basic, chưa ăn khớp

**Reviewer có thể thấy:** Video, procedural neon shapes, imported Vector UI, TMP punch, generic particles và screen flash là nhiều visual source khác nhau.

**Project support concern:** Không có successful-hit SFX; VFX phân tier chủ yếu bằng color/scale/count; animation UI là coroutine scale/alpha cơ bản.

**Vẫn làm tốt:** Feedback đầy đủ, judgement color nhất quán giữa HUD/burst/lane; failure miss/wrong khác nhau; pool VFX; Safe Area.

**Decision chưa tối ưu:** Thêm nhiều lớp effect không tự động tạo cohesive art direction.

**Senior có thể ưu tiên:** Một visual/audio language thống nhất, readability tại HitPoint và feedback timing trước số lượng effect.

**Bài học:** Aesthetic judgement là chọn lọc và consistency, không chỉ thêm animation/VFX.

### 24.5 Concern: video không tương xứng performance trade-off

**Reviewer có thể thấy:** File Sakura ~299 MB source, startup gate phức tạp, video không sync beat, Android chưa profile.

**Project support concern:** Decoder trở thành readiness dependency; video vẫn chạy khi pause; Main Menu thiếu fallback protocol; orientation Sakura khác portrait.

**Vẫn làm tốt:** Transcoding, audio disabled, four-frame readiness, timeout/error fallback và theme reuse thể hiện awareness.

**Decision chưa tối ưu:** Decorative feature chiếm package/startup/performance budget đáng kể.

**Senior có thể ưu tiên:** So sánh static/shader/procedural alternative bằng device metrics và visual goal trước khi commit video pipeline.

**Bài học:** Feature phải chứng minh value/cost trên target platform.

### 24.6 Concern: Task 2 improvement thực tế nhỏ

**Reviewer có thể thấy:** PL3 chỉ `9 → 8` Draw Calls; Batches/SetPass không đổi; absolute geometry rất nhỏ.

**Project support concern:** Custom particle transfer/space conversion có maintenance cost; chỉ Editor benchmark.

**Vẫn làm tốt:** Measurement disciplined, metrics tách đúng, deterministic replay, Frame Debugger evidence, visual regression được reject.

**Decision chưa tối ưu:** Có thể dành quá nhiều effort cho micro-optimization trước khi biết full-frame bottleneck.

**Senior có thể ưu tiên:** Profile target device/full frame và xếp issue theo milliseconds, memory hoặc thermal impact.

**Bài học:** Optimization process tốt nhưng target selection cũng quan trọng như execution.

### 24.7 Concern: effort prioritization

**Reviewer có thể thấy:** Adaptive MIDI/report, four-frame video gate và specialized consolidator khá sâu, trong khi core play/aesthetic/device validation còn thiếu.

**Project support concern:** Project docs ghi physical Android validation pending; final release build không được chứng minh; gameplay vẫn minimal.

**Vẫn làm tốt:** Những phần đã làm có evidence và code quality tốt.

**Decision chưa tối ưu:** Tối ưu “interesting engineering problems” hơn player-facing risk cao nhất.

**Senior có thể ưu tiên:** Core loop → device correctness → cohesive feedback → content/polish → measured optimization.

**Bài học:** Technical Lead đánh giá khả năng chọn đúng vấn đề, không chỉ khả năng giải vấn đề khó.

### 24.8 Concern: experience / aesthetic judgement

**Reviewer có thể thấy:** Một số solution đúng kỹ thuật nhưng chưa đạt product-level restraint: video lớn, nhiều effect source, one-error Game Over và Good unbounded.

**Project support concern:** UI/VFX chưa tạo một hierarchy/identity mạnh; device constraints chưa drive art choice.

**Vẫn làm tốt:** Có ý thức feedback, responsive layout và failure handling.

**Decision chưa tối ưu:** Visual decision chưa được benchmark bằng clarity/cohesion/value tương tự code metrics.

**Senior có thể ưu tiên:** Prototype visual direction nhỏ, test readability/feel, chọn ít element nhưng nhất quán.

**Bài học:** Seniority gồm judgement về scope, UX và trade-off, không chỉ code correctness.

---

## 25. Code Quality

### 25.1 Điểm mạnh

- **Naming:** `GameplayStartupController`, `SelectedSongContext`, `HitPositionJudge`, `ParticleSystemRendererConsolidator` mô tả đúng role.
- **Convention:** private serialized fields, public read-only properties, early validation/guard, sealed ở controller không cần inheritance.
- **Folder structure:** Runtime/Editor/UI/VFX/Audio/Optimization tách rõ.
- **Responsibilities:** `SongConductor` chỉ timing/audio; `ScoreManager` chỉ điểm; `GameplayController` chỉ terminal rules.
- **Event lifetime:** subscribe/unsubscribe paired trong `OnEnable/OnDisable`.
- **Validation:** Awake checks cho scene reference; `ChartValidator` có invariant rõ; failure log chỉ một lần ở audio/video warmup.
- **Determinism:** MIDI sort/dedup/tie-break; optimization seed/hierarchy order.
- **Pooling:** `HitBurstController` prealloc/recycle/reset rõ ràng.
- **Data-driven:** bốn song dùng một gameplay flow.
- **Evidence discipline:** Task 2 không đánh đồng Draw Call, geometry, API call và GC.
- **Tests:** `ChartBuilderTests` kiểm tra lane coverage, collapse, ascending bias, determinism/timing.
- **Production hot path:** không LINQ, không per-frame `Find`, không routine Debug.Log.

### 25.2 Điểm có thể cải thiện / risk

- **Overengineering:** Adaptive lane heuristic/report model khá lớn so với bốn source cố định; four-frame video gate phức tạp vì decorative media.
- **Premature optimization:** Task 2 custom consolidator tiết kiệm một call nhưng có logic space/seed/buffer cần bảo trì.
- **Remaining Instantiate/Destroy:** Một Note mỗi chart event; Sonata 01 có 821 Note.
- **Tight coupling:** `NoteSpawner` trực tiếp biết prefab, four-lane array và chart/conductor; phù hợp scope nhưng khó swap creation policy.
- **Runtime trust:** `JsonChartLoader` không validate lane/sort/duplicate; malformed JSON có thể làm `lanes[index]` throw.
- **Static globals:** `SelectedSongContext` và static events tạo implicit lifetime/dependency.
- **Scene dependency:** Nhiều controller yêu cầu serialized wiring đúng; không có composition root/installer formal.
- **Lifecycle ordering:** Gameplay loader/startup dựa trên all-Awake-before-Start và readiness polling, không có explicit script order.
- **Input ambiguity:** `OverlapPoint` một collider; không candidate sort theo time.
- **Judgement semantics:** Good catch-all; world-distance window phụ thuộc layout.
- **Singleton behavior:** Gameplay scene vẫn instantiate SFX copy rồi destroy; static API log error nếu service absent.
- **Coroutines/allocations:** Per-hit Note fade và HUD routine; string update; acceptable nhưng cần profile ở density cao.
- **Mobile:** Video source size/decode, ARMv7/device variation và no physical-device evidence.
- **No namespace/asmdef:** Tất cả production class ở global namespace; Editor assembly chỉ được Unity tách theo folder convention.
- **Optional reference behavior:** Một số visuals cho phép camera null/fallback hoặc dynamic AddComponent; tiện nhưng dependency bớt explicit.

“Unnecessary abstraction” không phải concern lớn: project thực ra tránh interfaces/DI/factory layers. Concern lớn hơn là specialized behavior complexity so với measured product value.

---

## 26. 25 Interview Hotspots

1. **Topic:** DSP clock làm source of truth
   **Related:** `Assets/Scripts/Runtime/Timing/SongConductor.cs`
   **Tại sao interviewer có thể hỏi:** Rhythm synchronization phụ thuộc trực tiếp vào lựa chọn clock.

2. **Topic:** Negative `SongTime` và scheduled pre-roll
   **Related:** `SongConductor.cs`, `NoteSpawner.cs`, `Gameplay.unity`
   **Tại sao:** Giá trị 2.5/2.0 giải quyết note tại hitTime 0.

3. **Topic:** DSP rebase khi Resume
   **Related:** `SongConductor.ResumeSong()`
   **Tại sao:** Sai một formula có thể tạo mass miss sau pause.

4. **Topic:** Position window so với temporal window
   **Related:** `HitPositionJudge.cs`
   **Tại sao:** Là design trade-off quan trọng về fairness/layout.

5. **Topic:** Good không có maximum distance
   **Related:** `HitPositionJudge.Evaluate()`, `Note.TryHit()`
   **Tại sao:** Expose behavior thật và limitation của current rules.

6. **Topic:** Movement derive từ absolute timeline
   **Related:** `Note.Update()`
   **Tại sao:** Cho thấy cách tránh deltaTime drift.

7. **Topic:** Natural miss threshold 0.15 s
   **Related:** `Note.cs`
   **Tại sao:** Boundary giữa position judgement và time-based failure.

8. **Topic:** Direct collider input
   **Related:** `NoteInputController.cs`
   **Tại sao:** Khác lane-input architecture phổ biến.

9. **Topic:** Collider overlap/candidate ambiguity
   **Related:** `Physics2D.OverlapPoint` usage
   **Tại sao:** Dense same-lane note có thể tạo edge case.

10. **Topic:** Multi-touch event ordering
    **Related:** `NoteInputController.Update()`
    **Tại sao:** Một touch sai có thể terminal trong cùng frame nhiều touch.

11. **Topic:** START input isolation và next-frame gating
    **Related:** `StartTileController.cs`, `GameplayStartupController.cs`
    **Tại sao:** Tránh START thành gameplay Wrong Tap.

12. **Topic:** Four advancing video frames
    **Related:** `DynamicBackgroundController.cs`
    **Tại sao:** Readiness mạnh hơn `prepareCompleted` và có edge cases.

13. **Topic:** AudioClip load-state gate
    **Related:** `SongConductor.PrepareAudioData()`
    **Tại sao:** Production clips tắt preload và load background.

14. **Topic:** Unity Awake/Start ordering trong startup
    **Related:** `GameplaySongLoader`, `GameplayStartupController`
    **Tại sao:** Scene initialization không có explicit Script Execution Order.

15. **Topic:** Runtime chart trust boundary
    **Related:** `JsonChartLoader.cs`, `ChartValidator.cs`, `NoteSpawner.cs`
    **Tại sao:** Editor validate sâu nhưng runtime chỉ check parse/non-empty.

16. **Topic:** Tick grouping thay vì seconds grouping
    **Related:** `ChartBuilder.GroupByTick()`
    **Tại sao:** Integer source coordinate tránh floating-point identity issue.

17. **Topic:** Preferred melody track 1
    **Related:** `ChartBuilder.SelectRepresentativeNote()`
    **Tại sao:** Heuristic source-specific, không universal.

18. **Topic:** Adaptive lane score/tie determinism
    **Related:** `ChartBuilder.AdjustForPlayability()`
    **Tại sao:** Cân bằng musical mapping, ergonomics và distribution.

19. **Topic:** Perfect-only combo
    **Related:** `ScoreManager.cs`
    **Tại sao:** Rule khác combo thông thường và cần giải thích intent.

20. **Topic:** Win chờ AudioClip hoàn tất
    **Related:** `GameplayController.Update()`, `SongConductor.IsSongFinished`
    **Tại sao:** Note completion và song completion là hai condition riêng.

21. **Topic:** Static event lifetime
    **Related:** `Note`, `NoteInputController`, các subscriber
    **Tại sao:** Scene unload/event leak là Unity concern phổ biến.

22. **Topic:** HitBurst pool capacity/recycling
    **Related:** `HitBurstController.AcquireBurst()`
    **Tại sao:** Fixed pool behavior khi overload.

23. **Topic:** Pool VFX nhưng Instantiate/Destroy Note
    **Related:** `HitBurstController.cs`, `NoteSpawner.cs`, `Note.cs`
    **Tại sao:** Performance decision cần evidence, không có một rule chung.

24. **Topic:** Video background trên mobile
    **Related:** `DynamicBackgroundController.cs`, video `.meta`, theme assets
    **Tại sao:** Package/decode/startup/visual value trade-off trực tiếp từ reviewer.

25. **Topic:** Rain consolidation và rejected zap
    **Related:** `ParticleSystemRendererConsolidator.cs`, `OPTIMIZATION.md`
    **Tại sao:** Phân biệt valid renderer consolidation với visual regression.

---

## 27. Important Files / Classes

### P0 — Phải hiểu rất sâu

| Priority | File/Class | Vì sao phải học |
|---|---|---|
| P0 | `SongConductor.cs` | DSP timing, schedule, pause/rebase, finish state là lõi rhythm game |
| P0 | `NoteSpawner.cs` | Chuyển chart time thành runtime spawn; prewarm và density trade-off |
| P0 | `Note.cs` | Movement, miss, judgement events, resolved lifecycle |
| P0 | `GameplayStartupController.cs` | Latest stabilization, readiness gates, START/input/pause state |
| P0 | `NoteInputController.cs` | Mouse/touch/UI filter/direct collider/wrong input |
| P0 | `HitPositionJudge.cs` | Exact judgement rule và limitation position-based |
| P0 | `GameplayController.cs` | Terminal rules, win condition, active Note cancellation |
| P0 | `ChartBuilder.cs` | Core MIDI grouping/representative/lane heuristic/determinism |
| P0 | `ChartValidator.cs` | Authoring-time invariants và trust boundary |
| P0 | `Docs/Optimization/OPTIMIZATION.md` | Toàn bộ Task 2 evidence, scope và rejected decisions |

### P1 — Phải hiểu

| Priority | File/Class | Vì sao phải học |
|---|---|---|
| P1 | `GameplaySongLoader.cs` | Composition boundary của selected song |
| P1 | `JsonChartLoader.cs` | Runtime chart parsing/readiness/failure |
| P1 | `SongDefinition.cs`, `SongCatalog.cs` | Data-driven content design |
| P1 | `SelectedSongContext.cs` | Cross-scene state và static trade-off |
| P1 | `DynamicBackgroundController.cs` | Prepare/frameReady/fallback và reviewer video concern |
| P1 | `ScoreManager.cs` | Exact score và Perfect-only combo |
| P1 | `PauseMenuController.cs` | UI blocking kết hợp DSP pause |
| P1 | `ResultScreenController.cs` | Event-driven terminal presentation/navigation |
| P1 | `HitBurstController.cs` | Object Pool và judgement-specific VFX |
| P1 | `SfxController.cs` | Persistent singleton/duplicate protection/PlayOneShot |
| P1 | `ParticleSystemRendererConsolidator.cs` | Task 2 CPU/renderer consolidation mechanics |
| P1 | `OptimizationBenchmarkRunner.cs` | Deterministic measurement setup |

### P2 — Biết overview

| Priority | File/Class | Vì sao phải học |
|---|---|---|
| P2 | `MainMenuController.cs`, `SongCardView.cs` | Menu data binding và scene transition |
| P2 | `StartTileController.cs` | START input isolation |
| P2 | `BackgroundTheme.cs` | Song visual data |
| P2 | `GameplayHUD.cs` | Score/judgement presentation và coroutine |
| P2 | `LaneFlashController.cs` | Per-lane feedback/event subscription |
| P2 | `FailureFeedbackController.cs` | Miss/wrong feedback separation |
| P2 | `GameplayResponsiveLayout.cs` | Camera/lane layout cho portrait aspect |
| P2 | `SafeAreaFitter.cs` | Mobile Safe Area implementation |
| P2 | `MidiChartImporter.cs` | Entry point/output behavior của Editor tool |
| P2 | `MidiNoteExtractor.cs` | TempoMap extraction và deterministic sort |
| P2 | `ChartImportReportWriter.cs` | Evidence/report output |
| P2 | Song/chart/theme assets | Hiểu wiring thật giữa content references |

---

## 28. Study Order

### Buổi 1 — Architecture + runtime flow

Đọc sections 1–4, vẽ lại Architecture Map và tự trace MainMenu → START → spawn note.

### Buổi 2 — DSP / rhythm timing

Đọc sections 5, 6, 11 và 14; ghi nhớ toàn bộ formula, negative SongTime và DSP rebase.

### Buổi 3 — MIDI → JSON

Đọc sections 7–8; trace một Tick group qua dedup, representative, quartile, adaptive lane, validator và JSON.

### Buổi 4 — Input / Note / Judgement

Đọc sections 4, 9–13; trace Successful Hit, Natural Miss, Wrong Tap và các event phát sinh.

### Buổi 5 — Design patterns / code architecture

Đọc sections 18–20 và tự giải thích responsibility/dependency của các class P0/P1.

### Buổi 6 — Performance / Task 2

Đọc sections 21–22; phân biệt hot/one-time/terminal path và Draw Call/Batch/SetPass/geometry/GetParticles/GC.

### Buổi 7 — Reviewer concerns / trade-offs

Đọc sections 23–27; luyện nói khách quan về quyết định, limitation, production alternative và bài học. Chưa bắt đầu mock interview ở giai đoạn này.
