# PIXEL SURVIVOR - ACTIVE CONTEXT

## Current Focus & Status
* **Recent Milestones Achieved (Completed & Verified):**
  - **Rebalanced 12 Enemy Move Speeds (User Confirmed & Locked):**
    - `Enemy_Zombie`: Base 3.50 m/s (slower than Steve 4.0 m/s early game for kiteability) $\rightarrow$ 3.95 m/s at 15:00 (+0.03 m/s/min).
    - `Enemy_Husk`: Base 3.50 m/s $\rightarrow$ 3.95 m/s at 15:00 (+0.03 m/s/min).
    - `Enemy_ZombieV2`: Base 3.60 m/s $\rightarrow$ 4.05 m/s at 15:00 (+0.03 m/s/min).
    - `Enemy_HuskShovel`: Base 3.60 m/s $\rightarrow$ 4.05 m/s at 15:00 (+0.03 m/s/min).
    - `Enemy_Spider`: Base 4.50 m/s (faster than Steve, flanker) $\rightarrow$ 4.88 m/s at 15:00 (+0.025 m/s/min).
    - `Enemy_Vex`: Base 4.80 m/s (fastest mob, flying harasser) $\rightarrow$ 5.18 m/s at 15:00 (+0.025 m/s/min).
    - `Enemy_Phantom`: Base 4.40 m/s $\rightarrow$ 4.78 m/s at 15:00 (+0.025 m/s/min).
    - `Enemy_Creeper` & `Enemy_IceCreeper`: Base 3.40 m/s $\rightarrow$ 3.70 m/s at 15:00 (+0.02 m/s/min).
    - `Enemy_Slime`: Base 3.20 m/s $\rightarrow$ 3.50 m/s at 15:00 (+0.02 m/s/min).
    - `Enemy_Witch`: Base 3.30 m/s $\rightarrow$ 3.60 m/s at 15:00 (+0.02 m/s/min).
    - `Enemy_Evoker`: Base 3.20 m/s $\rightarrow$ 3.50 m/s at 15:00 (+0.02 m/s/min).
    - All 12 prefabs and `WaveSpawner.cs` updated, verified, and committed.
  - **Elite Aura Feet Alignment Fix (Completed & Verified):**
    - Fixed sprite pivot in `vfx_elite_aura_ring.png.meta` to exact ground ellipse center $\{X: 0.5, Y: 0.2857\}$ (pixel 64, 32).
    - Updated `EliteEnemyAffixController.cs` to dynamically anchor to `mobSr.bounds.min.y`.
    - Mathematical difference between mob feet and aura center = **0.0000** across all mob types!
  - **Breakable Golden Lucky Block Chest (Completed & Verified):**
    - Prop sitting on the ground implementing `IDamageable`.
    - Takes 3 hits to crack open with punch bounce and "CRACK! (1/3)" floating text.
    - On break: showers EXP gems (70 EXP), emeralds (2-5 💎), plus high-value bonus (Steak, Magnet, TNT, or 10% Jackpot).
    - Trigger collider prevents player path obstruction.
  - **Instant Screen Nuke TNT (Completed & Verified):**
    - Picking up TNT immediately eliminates all active on-screen enemies within camera viewport with 99,999 lethal DMG. Zero delay, no explosion animation. Off-screen mobs unaffected.
  - **Movement & TimeManager Fix:**
    - Fixed `ProjectSettings/TimeManager.asset` back to `m_TimeScale: 1`. Added defensive `Time.timeScale = 1f;` across all managers.
    - Added Dual-Input support in `PlayerController.cs` (New Input System + Legacy Input fallback).
    - Cleaned scene `MainGame.unity` from all stray edit-mode text objects.
  - **Passive Items System Plan Locked (Option 1):**
    - User finalized Option 1: Passives have levels (Lv 1 to Lv 5 MAX), strict 4-passive slot limit, and evolution synergy with Lv 8 weapons.

## Current Priority Queue
1. **Passive Items System Implementation (Option 1 - Next Up):**
   - Create `PassiveItemController.cs` / integrate into `LevelUpManager.cs` to track 4 passive slots.
   - Implement the 9 passives with Lv 1 to Lv 5 progression using prepared sprites from `Assets/PixelSurvivor/Items/Passives/`.
   - Update Level Up modal dynamic pool to offer New Passives (if slots $< 4$) and Upgrades (if owned and $< 5$).
   - Display equipped Passives on HUD / Pause menu.
2. **Mini-Boss & Final Boss Implementation:**
   - 5:00 & 10:00 Nether Golem Mini-Boss (2,000 HP).
   - 15:00 The Warden Final Boss (12,000 HP, multi-phase).
3. **Difficulty Modes Tuning (Normal, Hard, Hardcore):**
   - Pending user review and adjustment requests.
