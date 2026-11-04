# PIXEL SURVIVOR - ACTIVE CONTEXT (v2.1 SPEC ALIGNED)

## 1. Current Status & Focus
* **Current State:** **PHASE 7 (POLISH, TEXT CLEANUP, BUG FIXES & MOBILE APK BUILD v4 VERIFIED) FULLY OPERATIONAL.**
* **Recent Achievements & Milestones:**
  - **APK Build & Distribution Setup:**
    - App icon custom artwork (1024x1024) embedded into Android launcher icons.
    - Build folder cleanup: structured builds into `PixelSuvivorAPK_v1/`, `PixelSurvivorAPK_v4/`.
    - Min 60, Max 90 FPS configuration with `QualitySettings.vSyncCount = 0` and Google Swappy pacing disabled.
    - Mobile UI CanvasScaler configured to 1600x900 Match Height (+20% scale boost for comfortable mobile viewing).
    - Mobile virtual joystick (`MobileJoystick.cs`) updated: drag deadzone removed, characters move immediately at 100% full speed.
    - In-Game Pause button enlarged to 52x52 at `(-42, -42)`, fixed double-toggle listener bug with idempotent `Pause()` / `Resume()` methods.
  - **Dynamic Resources Migration (Invisible Boss Fix):**
    - Root cause of invisible mini-bosses in standalone APK: textures resided outside `Resources/` and loaded only via `#if UNITY_EDITOR AssetDatabase`.
    - Relocated all MiniBosses, Bosses, Drops, Passives, Prefabs, and VFX into `Assets/PixelSurvivor/Resources/`.
    - Replaced all Editor-only sprite loading with `Resources.LoadAll<Sprite>(...)` across all 9 Bosses & Mini-Bosses, `BossRewardChest`, and `WaveSpawner`.
    - Double-guard: Both boss scripts (`EnsureSprites`) and `WaveSpawner` explicitly assign `sr.sprite` at spawn time. Tested 12/12 bosses: 100% PASS!
  - **Editor Phantom Lockdown & Level-Up Bug Fix:**
    - Cleaned phantom `BossArena_Visuals` and `BossArena_Colliders` left in `MainGame.unity` from edit-mode test.
    - Removed `[SerializeField]` from `BossArenaManager` runtime state variables (`isLockdownActive`, `chestDroppedThisLockdown`, `arenaCenter`).
    - Added `ResetArenaState()` auto-called on `Awake()` and `MainMenuManager.ShowMainMenu()`.
    - Added guard on chest drop failsafe: only allowed if `WaveSpawner.IsRunActive` and `TimeElapsed >= 290s`.
  - **Audio & Music Updates:**
    - Replaced in-game battle music with new C418-inspired track (`bgm_battle.wav`, 30s Stereo 48kHz, looping).
    - Created C418 and Dramatic Roguelite BGM prompt packs for ElevenLabs, Suno, and Udio.
* **Next Steps:**
  - In-game balance testing on mobile APK.
  - Add more custom audio tracks or sound effects as desired.

---

## 2. Key Architecture & Systems Overview
1. **Wave & Boss Spawner (`WaveSpawner.cs`):**
   - Central timeline controller running from 00:00 to 18:00 (1080s total).
   - Events: Pincer (1:30), Swarm Rush 1 (4:00), Mini-Boss 1 (5:00), Swarm Rush 2 (7:30), Mini-Boss 2 (10:00), Siege (12:30), Grand Final Boss (15:00), Timeout Defeat (18:00).
   - Random Mini-Boss & Final Boss pool with duplicate prevention.
2. **Boss Arena Lockdown (`BossArenaManager.cs`):**
   - 5-second pulsing HUD timer warning (Yellow for mini-boss, Crimson Red for grand boss).
   - Camera clamp & punch zoom, energy barrier perimeter, pauses regular mob spawning, cleans lingering regular mobs.
   - Drops `BossRewardChest` on mini-boss death; triggers `InGamePauseManager.Instance.TriggerVictory()` on grand boss death.
3. **Player Progression & Stats (`PlayerStats.cs`, `ExperienceManager.cs`):**
   - Mathematical equilibrium curve $\text{EXP} = \text{Round}(20 \times Lv^{1.20})$.
   - Hard caps: Armor 35%, CDR 30%, MoveSpeed +40%, Crit 60%, Bonus DMG +60%.
   - Global contact I-frame 0.35s with surround damage scaling.
4. **Weapons & Passives (`PassiveController.cs`, `LevelUpManager.cs`):**
   - 4 weapon slots + 4 passive item slots.
   - Evolution unlocked at $\ge 09:00$ (max 2 per run).
   - Evolution pity active from 08:00 with swap mechanics if slots full.

---

## 3. Verified Verification State
* **Compilation:** 0 errors in Unity Editor console.
* **Git Synchronization:** All code and assets fully synchronized between `Unity/ProjectFinalPru` and `git_repo/PixelSurvivor2D`.
* **Master Index:** `C:\Users\PC\OneDrive\Desktop\AntigravityAgent\PixelSurvivor\PIXEL_SURVIVOR_MASTER_INDEX.md`.
