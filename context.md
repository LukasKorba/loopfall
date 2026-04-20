# Loopfall — Project Context & Handoff Document

**Last updated:** 2026-04-19
**Last version tag:** v0.9.2 (refactor branch `refactor/scoresync-split` pending merge as v0.9.3)
**Current branch:** `refactor/scoresync-split`
**Unity version:** 6000.4.1f1 (Unity 6)

> **Regenerate this doc after:** branch switch, tag bump, >5 new commits, or any architectural change. If you're reading this more than ~2 weeks after the "Last updated" date, spot-check Section 15 (Fact Table) before trusting specifics.

---

## 1. Project Overview

**Loopfall** is an endless tunnel runner built in Unity 6. A ball rolls inside a half-torus (major radius 10, minor radius 1, center at `(0,-10,0)`). The player taps left/right to swing through gaps in obstacles. The game targets iOS (primary), tvOS, Android, macOS, and Steam.

- **Single scene:** `Assets/Scenes/Main.unity`
- **No networking, no ads, no analytics**
- **Dependencies:** TextMeshPro 3.2.0-pre.12 only (Steamworks.NET via preprocessor guard `#if !DISABLESTEAMWORKS`)
- **Input:** Legacy Input system, tap left/right, MFi gamepad + Siri Remote support
- **Audio:** 20 background tracks, ~11 SFX, 10 tier1 + 8 tier2 voice lines, velocity-modulated rolling sound

---

## 2. Game Modes

### Pure Hell (original, complete)
Endless mode. Gate-count scoring. Full cinematic rewind on death (`RewindSystem.cs` records every frame, plays back with time manipulation + visual effects). The torus IS the challenge.

### Time Warp (complete)
Time-attack frenzy. Starts with 10s countdown. Green strips (+2s) and red strips (-1s) on the track surface. Speed accelerates over time. Score = elapsed time × 10. Uses `FrenzyTimer.cs`.

### BLITZ (in active development)
Space Invaders on a torus. Ball auto-fires laser beams forward (+X direction). Swing = aim. Destroy targets for points or dodge electric gates. No rewind — pure arcade. This is where all active development is focused.

### Mode selection
`Assets/Scripts/GameConfig.cs` — static `GameConfig.ActiveMode` enum (`PureHell | TimeWarp | Blitz`). Title screen writes it before loading a run; `SceneSetup.Awake()` reads it and branches system setup (obstacle spawner, HUD, music, etc.). **There is no scene switch** — everything runs inside `Main.unity`.

---

## 3. Architecture

### Scripts (37 files under `Assets/Scripts/`)

**Core gameplay:**
- `Torus.cs` — Central hub. Track rotation, obstacle spawning, scoring, all BLITZ logic (spawning, collection, upgrades, difficulty phases). **957 lines.**
- `Sphere.cs` — Ball controller, physics, input handling, death detection, shield visual, play-stat persistence.
- `Obstacle.cs` — Pure Hell gate geometry (procedural mesh with gap).
- `CameraSwing.cs` — Camera follows ball with smooth swing.

**BLITZ-specific (4 files):**
- `BlitzBeam.cs` — Auto-firing projectiles. Pool of 12 LineRenderers (core + halo). Multi-beam support (1/2/3 spread). SphereCast for box hit detection, Raycast for surface endpoint.
- `BlitzBox.cs` — Shootable targets. Three types: 1HP octahedron "crystal", 3HP cube "sentinel" with shield ring, pyramid "button". Per-frame animation (spin, bob, pulse).
- `BlitzGate.cs` — Electric gate arcs. LineRenderer shimmer (core + halo), BoxColliders along arc, endpoint cubes. Half-gate variant for buttonless gates. Connection line to linked button.
- `BlitzOrb.cs` — Collectible upgrade pickups. Floating 3D shapes per type (yellow octahedron, blue ring, green sphere) with TrailGlow additive shader. Spin + bob + breathing pulse.

**UI & presentation:**
- `ScoreSync.cs` + 6 partial files — UI system, split into a `public partial class ScoreSync` across 7 files. Total ~4461 lines. Responsibilities per file (same class, same state; partials share all private fields):
  - `ScoreSync.cs` (1114 lines) — **entry point**. Fields (color palette, state, canvas, all group refs), `Start` / `OnDestroy` / `Update`, main state machine, `BuildUI` construction method, `OnLanguageChanged` handler, `FormatTimestamp`, cached refs. When looking for the class root and its lifecycle, start here.
  - `ScoreSync.UIHelpers.cs` (771 lines) — UI primitives (`CreateText`, `SetAnchored`, `StretchFull`, icon sprite builders), procedural textures (vignette, scanlines, circle), `ApplyDropShadow`, easing curves, VHS glitch engine.
  - `ScoreSync.Screens.cs` (1332 lines) — per-state display groups: `ShowGroup`/`AnimateState`, splash, title, playing, BLITZ upgrade HUD (slot rows + orb sparks + kill-streak), rewinding, game over (leaderboard coloring, new-best animation), new-best glitter, pause.
  - `ScoreSync.Tutorial.cs` (251 lines) — `BuildTutorialGroup`, `AnimateTutorial`, per-platform tutorial visuals (tap, Siri Remote, keyboard, gamepad).
  - `ScoreSync.Panels.cs` (673 lines) — overlay panels: settings (audio icons, display section, theme + language cycle rows, quit button) and stats (session + lifetime totals, best scores per mode).
  - `ScoreSync.L10nHelpers.cs` (213 lines) — platform-conditional helpers that pick localized strings: `GetTapPrompt`, `GetResumeHint`, `GetTutorialInstruction`, `GetTutorialReadyHint`, `GetCloseHint` (compile-time platform branches inside each).
  - `ScoreSync.Scores.cs` (107 lines) — leaderboard persistence. CSV-encoded `TopScores` PlayerPrefs per mode; `LoadScores`, `SaveScores`, `InsertScore`, `GetBestForMode`, timestamp handling.
  
  **Contract:** all partial files declare `public partial class ScoreSync` with the same 6 `using` statements. No file contains a second class. Adding new UI features: pick the partial by responsibility (state-specific → Screens, overlay → Panels, reusable primitive → UIHelpers) rather than growing one file indefinitely.
- `SceneSetup.cs` — Initialization. Creates all GameObjects, materials, wires everything. Runs in `Awake()`.
- `GameConfig.cs` — Mode selection + per-mode PlayerPrefs keys + leaderboard IDs (static).
- `ThemeData.cs` — 8 visual themes with color schemes (theme index persisted).
- `DisplaySettings.cs` — Resolution/quality settings (persisted).
- `L10n.cs` — 7-language localization (EN/DE/ES/FR/IT/RU/PT-BR). Key-based `T(key)` lookup, system-locale detection, `OnLanguageChanged` event, `MonthAbbr` for localized dates. IL2CPP-safe (no `CultureInfo` dependency).

**Visual systems:**
- `BackgroundRings.cs` — Procedural nebula, stars, god rays.
- `BlackHoleWarp.cs` — Gravitational-lens post-process.
- `DepthHueShift.cs` — Depth-based hue rotation + fog post-process.
- `DeathEffect.cs` — Death flash/effect.

**Audio:**
- `GameAudio.cs` — Music, SFX, voice lines, rolling sound.

**Systems:**
- `RewindSystem.cs` — Frame recording + cinematic rewind (Pure Hell only).
- `FrenzyTimer.cs` — Time Warp countdown logic.
- `TrackItem.cs` — Time Warp bonus/penalty strips.
- `DebugPanel.cs` — Dev tools.

**Platform abstraction (5 files):**
- `IPlatformService.cs` — Interface: `ReportScore`, `ShowLeaderboard`, `UnlockAchievement`, etc.
- `PlatformManager.cs` — Singleton façade. Game code calls `PlatformManager.Instance.ReportScore(...)`. Picks a concrete service per platform at startup.
- `GameCenterManager.cs` — `IPlatformService` implementation for Apple platforms.
- `SteamService.cs` — `IPlatformService` implementation for Steam (depends on Steamworks under `#if !DISABLESTEAMWORKS`). Also persists swing stats.
- `SteamManager.cs` — Steamworks.NET bootstrap (AppID init, callback pump). **Distinct from `SteamService`** — this file is the SDK lifecycle; `SteamService` is the game-facing leaderboard/achievement adapter.

**Spline system (present on current `spline-track` branch, not wired into any released mode):**
- `SplineTrack.cs`, `SplineGameController.cs`, `SplineCameraFollow.cs` — Infrastructure for future modes with non-torus geometry. Not used by BLITZ, Pure Hell, or Time Warp.

### Shaders (11 project shaders under `Assets/Shaders/`; TextMeshPro shaders excluded)

| Shader | Purpose | Key detail |
|--------|---------|------------|
| `Gate.shader` | Obstacles, boxes, buttons | Half-lambert + rim + emission + `_SpawnProgress`. Transparent blend. |
| `TrailGlow.shader` | Beams, gate arcs, orbs, shield | Additive blend (SrcAlpha One), no depth write, Cull Off. `_Color` + `_Intensity`. |
| `TrackGrid.shader` | Torus surface | Grid lines, traveling sparks, pulse rings. |
| `Rail.shader` | Edge rails | Distance-based fading. |
| `TrackItem.shader` | Time Warp pickups | Opaque unlit, Cull Off. |
| `Ball.shader` | Player ball | Metallic + Fresnel rim glow. |
| `DepthHueShift.shader` | Post-process | Hue shift + fog by depth. Luminance threshold preserves bright emissive pixels. |
| `BlackHoleWarp.shader` | Post-process | Gravitational-lens distortion. |
| `ObstacleShadow.shader` | Gate shadows | Dark transparent strip. |
| `Nebula.shader` | Skybox | Procedural nebula. |
| `GodRays.shader` | Background | Volumetric light rays. |

---

## 4. BLITZ Mode — Current State (Detailed)

### Fully working, released in v0.6.0
- Beam auto-fire with time-based rate scaling
- 1HP and 3HP destructible boxes
- Electric gates: gap-only, gap+button, full+button
- 4-phase difficulty scaling (obstacle mix, spacing, fire rate)
- Points scoring (10/20/50)
- Orb spawning with weighted type selection
- Orb collection via world-space distance check
- 3 upgrade tracks: Gun (multi-beam), Cadency (fire rate), Shield (absorb one hit)
- HUD with 3 rows of upgrade slot squares
- Spark-to-HUD animation on orb collection
- Torus speed ramp (`0.12 → 0.22` over time)
- Shield visual on ball (green sphere, 1.4× scale)

### Post-v0.6.0, committed on current branch (awaiting next tag)
Four commits past v0.6.0 — all orb-related polish/bugfixes that have been playtested:
- `94cae0f` Orb visual polish: additive glow, fade-out, hue-shift immunity
- `c590c3f` Orb fade-out: longer duration, earlier trigger, bright flash
- `ed0d318` Orb dismissal: cross-section check, two fade modes, spark-to-HUD
- `90bf99a` Orb collection: multi-frame window instead of single-frame check

### Uncommitted (7 modified files in working tree)

**`BlitzBeam.cs` — Star Wars blaster-bolt visual:**
- Halo LineRenderer behind each core beam (wider, dimmer glow)
- Core beam gradient: white-hot center at 30%, solid alpha
- `BEAM_WIDTH = 0.035`, `HALO_WIDTH = 0.09`
- Impact glow enlarged (`0.18` max scale)

**`BlitzBox.cs` — Three distinct target types:**
- 1HP "Crystal": procedural octahedron mesh, spinning 45°/s + levitation bob
- 3HP "Sentinel": cube tilted 45° (diamond stance), spinning 25°/s, orbiting shield ring (LineRenderer, TrailGlow)
- Shield ring damage states: 3HP = full bright, 2HP = dimmed + flicker, 1HP = ring gone + red emission
- Button "Beacon": spinning 60°/s + pulsing emission
- All lifted `0.75×` size above surface (was `0.5×` — partially below torus)
- Fragment counts scaled: 6 / 8 / 10 by type
- `Animate(float time)` method added (boxes were fully static before)

**`BlitzGate.cs` — Gate improvements:**
- Half-gate variant for buttonless gates (left-only or right-only, random)
- Endpoint cubes at arc start/end (pulsing with shimmer)
- Collision hitbox tightened: `COL_HEIGHT 0.12 → 0.06`, `COL_WIDTH 0.15 → 0.06`, `COL_LENGTH 0.15 → 0.08`

**`BlitzOrb.cs` — Continued iteration** on top of the 4 committed orb commits above. Floating 3D shapes per type (octahedron / ring / sphere), spin + bob + breathing pulse, intensity-flash on collect, gentle dim on miss.

**`SceneSetup.cs`:**
- Added `blitzRingMaterial` (TrailGlow, cyan-white, intensity 3.5) for sentinel shield rings.

**`ScoreSync.cs`:**
- HUD slot size halved: `28 → 14`.

**`Torus.cs`:**
- `AnimateBlitzBoxes()` call in Update loop
- `mBlitzRingMat` field wired through SceneSetup
- `SpawnBlitzOrb` updated for new constructor signature
- Half-gate spawning: `SpawnGateWithGap` now creates left-only or right-only gates

### Still needs on-device verification before the v0.6.1 cut
- Shield-ring visual on 3HP sentinels (is the damage-state flicker readable?)
- Shield-ring vs. beam hit detection — ring is non-collider, so this should be safe; worth one targeted playtest
- Half-gate spawning rate and placement feel
- HUD slot size `14` on tablet / landscape — readable, or too small?
- Orb collection radius (`ORB_COLLECT_RADIUS = 0.7f`) — was tuned for arc-strip orbs; may need re-tuning for floating 3D objects

---

## 5. How to Launch & Test

- **Editor:** open `Assets/Scenes/Main.unity`, press Play. Mode is whatever `GameConfig.ActiveMode` is at Awake. Default is `PureHell` — change by picking the mode from the title screen, or set the enum in code for a one-off test.
- **Debug panel:** `DebugPanel.cs` — used for dev-only shortcuts and god-mode toggles.
- **God mode:** `Sphere.mDebugGodMode` — when true, obstacle collisions are ignored (see `Sphere.cs:~522`).
- **Force a specific theme:** `PlayerPrefs.SetInt(PREF_THEME, n)` where `n` ∈ 0–7, or pick from the title screen.
- **iOS on-device:** Build & Run from Xcode. Graphics tier + HDR are forced in `SceneSetup.Awake()`; do not override in Player Settings.
- **Reset persistence:** `PlayerPrefs.DeleteAll()` (or delete the target's PlayerPrefs file) — useful when testing first-run flow.

---

## 6. Torus Geometry & Coordinate System

Critical for understanding all positioning code.

- **Torus center:** `(0, -10, 0)`
- **Major radius:** 10 (ring), **Minor radius:** 1 (tube)
- **Ball position:** `~(0, -10.9, 0)` (bottom of tube inner surface)
- **Camera position:** `(-1.727, -10.205, 0)` looking +X (track forward)
- **Track forward direction:** +X
- **Torus rotation:** `Rotate(0, 0, -mAngleStep)` — Z-axis rotation.
- **Physics layer 8:** Dedicated for BlitzBox beam detection (SphereCast).

### Two distinct angles — don't conflate them

- **Ring angle `θ`:** Position around the torus center, rotated on the Z-axis. Objects parented to the torus receive `Rotate(0, 0, angle - mAngle)`.
- **Cross-section angle `a`:** Position around the tube cross-section. Surface point: `(0, -10 - sin(a), -cos(a))`. Surface normal: `(0, sin(a), cos(a)).normalized`. Playable cross-section range: ~25° (top of tube) to ~155° (bottom). Ball sits at the bottom of the inner surface.

> `GetBallCrossSectionAngle()` attempts to compute `a` via `InverseTransformPoint` and is unreliable because torus Z-rotation mixes X/Y. For orb collection, use world-space distance (`Vector3.Distance < 0.7f`) instead.

---

## 7. Critical Gotchas & Lessons Learned

### Shader gotchas
- **NEVER use Unity Standard shader on iOS.** Mobile variants compile differently — darker, flatter results. Always use custom shaders (Gate, TrailGlow, etc.).
- **`Shader.Find()` gets stripped on iOS** for shaders only used in `Graphics.Blit()`. Use serialized shader references (public field on MonoBehaviour) instead.
- **`DepthHueShift` luminance threshold:** HDR-bright pixels (`lum > 1.0`) resist hue shift and fog. Keeps BLITZ orbs/beams (intensity 2.0+) color-stable for identity while letting regular emissive obstacles (Pure Hell gates) hue-shift with depth.

### Geometry gotchas
- **BlitzOrb arc mesh had absolute torus coordinates** (vertices at `y ≈ -11`). Any `localScale` change moved the mesh off the surface entirely → "blink out" bug. Fixed by moving to local-space floating 3D objects where `localScale` works correctly.
- **`GetBallCrossSectionAngle()` is unreliable** for precise positional matching. Use world-space distance checks instead.
- **Objects on torus surface clip through due to curvature.** Offset center by `normal * (size * 0.75f)` instead of `0.5f`.

### UI gotchas
- **Canvas is ScreenSpaceOverlay**, reference resolution `1920×1080`, match `0.5`.
- **Spark-to-HUD uses:** `WorldToScreenPoint` → `ScreenPointToLocalPointInRectangle(canvasRT, null camera)`.
- **Slot pop effect:** scale up to `1.6×` on arrival, decay via `Lerp(s.x, 1f, dt*8f)`.

### Platform gotchas
- **iOS haptics:** `AudioServicesPlaySystemSound(1519)` for light, `1520` for heavy. Guarded by `#if UNITY_IOS && !UNITY_EDITOR`.
- **Graphics tier forced to Tier3** in `SceneSetup.Awake()` — Tier2 on iOS disables HDR, killing emission glow.
- **Quality forced to Ultra (level 5)** — iOS defaults to Medium otherwise.

---

## 8. Design Rules (User Preferences)

These are firm — don't violate them:

1. **Dark themes always.** Low ambient, dark surfaces, neon accents. Darkness reveals flat-shaded geometry.
2. **Three-color contrast rule.** Track, gates, and trail must be three distinct hue families per theme. Same-hue = unreadable.
3. **Never remove chromatic aberration.** CMYK channel separation on all text is the game's visual identity.
4. **No tilt/accelerometer controls.** Tap-only. Fast impulses are the game's identity.
5. **Pure Hell stays torus.** Spline track is for Time Warp or future modes only.
6. **No Standard shader.** Custom shaders only for consistent cross-platform visuals.
7. **Pickup vs obstacle visual language:** pickups use TrailGlow (additive, ethereal, semi-transparent). Obstacles use Gate shader (opaque, solid, lit). This separation is intentional and critical.

> On conflict between this section and auto-memory, **this section wins.** It's curated per-commit; auto-memory can drift across sessions.

---

## 9. BLITZ Scoring

```
Small box (1HP):     10 points
Large box (3HP):     20 points
Gate disabled:       50 points (button destroyed)
Orb collected:        0 points (drives upgrade track only)
Passive score:       none
Combo bonus:         none (planned, see Section 12)
```

Score is written by `Torus` to a TextMesh; `ScoreSync` reads from that TextMesh (legacy pattern from Pure Hell).

---

## 10. BLITZ Difficulty Phases

```
Phase 1 (0–15s):   Sparse 1HP boxes only, gentle intro
Phase 2 (15–45s):  3HP boxes appear, first half-gates
Phase 3 (45–90s):  Button+gate combos, tighter spacing
Phase 4 (90s+):    Full gates, dense mix, everything
```

Speed ramp: `mAngleStep = min(BLITZ_BASE_SPEED + mBlitzTimer * 0.001, BLITZ_MAX_SPEED)`
- Base: `0.12`, Max: `0.22` (Pure Hell constant: `0.17`)

Fire-rate ramp: starts at `0.8s` interval, decreases to `0.4s` by 90s, further reduced by Cadency upgrade multiplier (L1: `0.7×`, L2: `0.5×`).

---

## 11. BLITZ Upgrade System

Three independent upgrade tracks powered by collectible orbs. `ORBS_PER_UPGRADE = 5`.

| Track | Orb color | Shape | L0 (default) | L1 (5 orbs) | L2 (10 orbs) |
|-------|-----------|-------|----|----|----|
| Gun | Yellow | Octahedron | 1 beam | 2 beams (V-spread) | 3 beams (center + sides) |
| Cadency | Blue | Ring | Base fire rate | `0.7×` interval | `0.5×` interval |
| Shield | Green | Sphere | No shield | 1 absorb charge | — (no L2 tier) |

**Shield is binary, not tiered.** Collecting 5 green orbs sets `mShieldLevel = 1, mShieldActive = true`. `ConsumeShield()` on a lethal hit zeros both fields **and resets `mShieldOrbCount = 0`** (see `Torus.cs:~949`), so the player can collect 5 more green orbs and re-earn the shield within the same run. There is no L2.

**Orb spawn:** 30% chance per obstacle. Type weighted toward incomplete tracks — maxed tracks get weight `0` and stop dropping. Shield "maxed" means `mShieldOrbCount >= 5`, i.e. shield is currently held; as soon as it's consumed, green orbs become eligible again.

**HUD:** 3 rows top-left. Gun: 10 slots (5 for L1 + 5 for L2). Cadency: 10 slots. Shield: 5 slots. Slot size `14` (post-uncommitted change; was `28`).

---

## 12. Future Plans & Ideas

### This week — ship the uncommitted BLITZ polish
- Playtest the 7 modified files on device (iPhone). Verify box/gate/orb feel.
- Commit in logical slices; tag `v0.6.1` if visual upgrades pass.
- Any remaining orb-collection-radius tuning (tuned for arc strips; may need re-tuning for 3D objects).

### Near-term — BLITZ depth
- **Combo system:** chain destroys in a short time window → score multiplier.
- **More obstacle variety** (Octagon inspiration — 70 parametrized types):
  - Lines/rows of boxes
  - Walls (shoot to carve gap)
  - Mixed formations
  - Moving obstacles

### Medium-term
- **BLITZ themes** — currently hardcoded colors; should integrate with the 8-theme system.
- **Sound design for BLITZ** — beam-fire SFX, impact SFX, gate shimmer ambient, orb-collection chime, upgrade-level-up fanfare.
- **BLITZ leaderboard** — GameCenter + Steam integration (platform abstraction already exists).
- **Screen shake / juice** — hit feedback beyond haptics.

### Long-term
- **Spline track mode** — infrastructure on current branch is the foundation. Could become a progression-based mode.
- **Steam release** — `SteamService` + `SteamManager` scaffold the integration; needs Steamworks AppID, depot config, build settings.
- **More game modes** — the mode architecture (`GameConfig`) supports arbitrary additions.
- **Obstacle parametrization** — Octagon-level variety (70 types) for BLITZ.

---

## 13. File Modification Cheat Sheet

When working on BLITZ you'll most commonly touch:

| What | File | Key methods |
|------|------|-------------|
| Obstacle spawning | `Torus.cs` | `SpawnNextBlitzObstacle`, `SpawnBlitzBox`, `SpawnGateWithGap`, `SpawnGateWithButton`, `SpawnFullGateWithButton`, `SpawnBlitzOrb` |
| Difficulty tuning | `Torus.cs` | `SpawnNextBlitzObstacle` (phase thresholds), `UpdateBlitzSpeed`, `UpdateBlitzFireRate` |
| Box visuals | `BlitzBox.cs` | constructor, `Animate`, `ApplyVisuals`, `CreateShieldRing` |
| Gate visuals | `BlitzGate.cs` | constructor, `CreateArcSegment`, `Animate` |
| Orb visuals | `BlitzOrb.cs` | constructor, `Animate`, `GetRingMesh` |
| Beam visuals | `BlitzBeam.cs` | `Initialize`, `Update`, `FireSingleBeam` |
| Materials | `SceneSetup.cs` | `CreateMaterials` (search for "blitz") |
| Material wiring | `SceneSetup.cs` | search for `torusScript.mBlitz` |
| HUD | `ScoreSync.Screens.cs` | `BuildBlitzUpgradeHUD`, `UpdateBlitzUpgradeHUD`, `TriggerOrbSpark`, `AnimateOrbSparks` |
| Settings / stats panels | `ScoreSync.Panels.cs` | `BuildSettingsPanel`, `BuildStatsPanel` |
| Tutorial | `ScoreSync.Tutorial.cs` | `BuildTutorialGroup`, `AnimateTutorial` |
| Leaderboard persistence | `ScoreSync.Scores.cs` | `LoadScores`, `SaveScores`, `InsertScore`, `GetBestForMode` |
| Localized string helpers | `ScoreSync.L10nHelpers.cs` | `GetTapPrompt`, `GetResumeHint`, `GetTutorialInstruction` |
| UI primitives / effects | `ScoreSync.UIHelpers.cs` | `CreateText`, `ApplyDropShadow`, VHS glitch engine, procedural textures |
| Upgrades | `Torus.cs` | `OnOrbCollected`, `ApplyGunUpgrade`, `ApplyCadencyUpgrade`, `ApplyShieldUpgrade` |
| Shield (trigger) | `Sphere.cs:~525` | death check calls `mTorusScript.ConsumeShield()` |
| Shield (state) | `Torus.cs:~949` | `ConsumeShield` resets level + active + orb count |
| Collection | `Torus.cs` | `CheckBlitzOrbPickups` — world-space distance, `ORB_COLLECT_RADIUS = 0.7f` |

---

## 14. Material Pipeline

All BLITZ materials are created in `SceneSetup.CreateMaterials()` and wired to `Torus` via public fields:

```
blitzBoxMaterial          → mBlitzBoxMat         (Gate shader, cyan)
blitzGateMaterial         → mBlitzGateMat        (Gate shader, purple)
blitzButtonMaterial       → mBlitzButtonMat      (Gate shader, orange)
blitzConnectionMaterial   → mBlitzConnectionMat  (TrailGlow, purple)
blitzRingMaterial         → mBlitzRingMat        (TrailGlow, cyan-white)
blitzOrbGunMaterial       → mBlitzOrbGunMat      (TrailGlow, yellow, intensity 2.5)
blitzOrbCadencyMaterial   → mBlitzOrbCadencyMat  (TrailGlow, blue,   intensity 2.5)
blitzOrbShieldMaterial    → mBlitzOrbShieldMat   (TrailGlow, green,  intensity 2.5)
beamMaterial              (via BlitzBeam)        (TrailGlow, white,  intensity 3.0)
blitzShieldVisualMaterial (via Sphere)           (TrailGlow, green,  intensity 1.5)
```

Rule: TrailGlow for anything that should glow / be transparent. Gate shader for anything that should look solid / lit.

---

## 15. Persistence (PlayerPrefs)

Loopfall has **no save files** — all persistence is via Unity `PlayerPrefs` (platform-native key-value store).

| Key(s) | Source file | Purpose |
|---|---|---|
| `TopScores`, `TopScores_TimeWarp`, `TopScores_Blitz` | `ScoreSync.cs` | Per-mode leaderboard (CSV of `score,timestamp` entries). Key chosen via `GameConfig.GetScoresKey()`. |
| `PREF_THEME` | `ThemeData.cs` | Selected theme index (0–7) |
| `PREF_MUSIC`, `PREF_SOUND` | `GameAudio.cs` | Mute toggles |
| `PREF_FULLSCREEN`, `PREF_VSYNC`, `PREF_RES_W`, `PREF_RES_H` | `DisplaySettings.cs` | Window preferences (desktop / macOS) |
| `PREF_FIRST_RUN` | `ScoreSync.cs` | Suppresses onboarding after first play |
| `STAT_RUNS`, `STAT_TAPS` | `Sphere.cs` | Aggregate play stats |
| `PREF_SWING_COUNT` | `SteamService.cs` | Steam achievement tracking |

**BLITZ upgrade state is NOT persisted.** It resets to L0 every run — intentional, because BLITZ is a score-attack mode.

Remote leaderboards (GameCenter / Steam) are pushed via `PlatformManager.Instance.ReportScore(...)`. The leaderboard ID per mode is in `GameConfig.GetLeaderboardID()`.

---

## 16. Fact Table (for spot-checking staleness)

Re-run these if the doc feels stale:

| Fact | Value | How to verify |
|---|---|---|
| Unity version | `6000.4.1f1` | `cat ProjectSettings/ProjectVersion.txt` |
| Current branch | `spline-track` | `git branch --show-current` |
| Last tag | `v0.6.0` | `git tag --sort=-creatordate \| head -1` |
| Commits since last tag | 4 | `git log v0.6.0..HEAD --oneline \| wc -l` |
| Scripts | 37 | `find Assets/Scripts -name '*.cs' \| wc -l` |
| Project shaders | 11 | `find Assets/Shaders -name '*.shader' \| wc -l` |
| `Torus.cs` lines | 957 | `wc -l Assets/Scripts/Torus.cs` |
| `ScoreSync*.cs` lines (7 files) | 4461 | `wc -l Assets/Scripts/ScoreSync*.cs` |
| Modified working tree | varies | `git status --short \| wc -l` |

---

## 17. User Profile

- **Lukas Korba** (lukas@zodl.com)
- Created **Octagon** — hit game with 70 parametrized obstacle types. Brings that variety philosophy to BLITZ.
- Strong visual taste — dark themes, neon accents, chromatic aberration.
- Prefers iterative development: implement, test in-game, adjust.
- Values game feel (haptics, juice, visual polish) highly.
- iOS-first mindset but targets all platforms.
