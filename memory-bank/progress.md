# PIXEL SURVIVOR - PROGRESS & ROADMAP

## What Works (Completed & Verified)
* [x] **Project Foundation:** Unity 6 LTS URP 2D, New Input System, scene structure.
* [x] **Player Movement:** 8-directional top-down movement with linear velocity and walk cycle animations.
* [x] **Character Assets:** Steve, Alex, Villager spritesheets sliced and PPU normalized to 32.
* [x] **Weapon System & Combat Fixes:**
  - `WeaponBase.cs` & `WeaponController.cs` auto-attack pipeline.
  - Targeting fix: Only attacks when targets are in range, slashes toward closest mob.
  - **VFX Slash Normalization (Right 0° Standard):**
    - Remade sprite sheets for Battle Axe, Dark Scythe, Magma Broadsword, and Twin Daggers facing Right (0°).
    - Grid-sliced 7 frames (96x96, PPU = 32, Point filter, Uncompressed).
    - `vfxAngleOffset = 0f` applied to all melee weapons for true 360° accurate swings.
* [x] **Enemy AI & Movement Polish:**
  - `EnemyAI.cs` continuous chase + separation flocking physics.
  - **Directional Flip Jitter Fix:** Deadzone hysteresis (`flipThreshold = 0.25f`) and minimum flip interval (`0.12s`) to completely eliminate erratic X-axis flickering when moving along the Y-axis.
  - `EnemyAnimation.cs` synchronized with `EnemyAI.cs`.
* [x] **Combat Feedback:**
  - `DamageNumber.cs` floating numbers with bounce animation.
  - `AnimatedVFX.cs` glowing slash arcs and hit sparks.
  - `EnemyHealth.cs` hit flash, knockback impulse, and death smoke puff.
* [x] **Modular Canvas UI Overhaul:**
  - InGame HUD fully synchronized with user's custom layout (`TopCenter_Container` timer/biome, `Player_Stats` HP bar, Character Name, Level Text).
  - Obsolete IMGUI `OnGUI()` calls in `InGameHUD.cs` and `InGamePauseManager.cs` permanently disabled.
  - `InGame_Canvas` scale `(1,1,1)` and `HUD_Root` auto-activation wired to `MainMenuManager`.
* [x] **Pause & Defeat System (Tone 1: Dark Obsidian & Gemstone):**
  - Pause Menu: 8-stat grid, weapon stats & DPS calculation, empty item slot placeholders.
  - Defeat Menu: Emeralds earned, formula breakdown, replay & home actions.
* [x] **HD Settings Modal System:**
  - Created modal from JSON layout with Sound, Controls, and About tabs.
  - Integrated with `MainMenuManager` and `InGamePauseManager`.
* [x] **Anti-Aliasing & Broken Border Elimination (`RoundedBorderImage.cs`):**
  - Added Sub-Pixel Edge Feathering (1px alpha fringe) and default `cornerSegments = 20`.
  - Updated all 55 `RoundedBorderImage` instances across the scene for razor-sharp, smooth rounded corners without jagged aliasing.
* [x] **Level Up Modal (2-Click Mechanism):**
  - Card 1-click preview: cyan border highlight, stat delta preview (`+28% 🟢`), confirm hint.
  - Card 2-click confirm: applies upgrade, restores time scale.
* [x] **TextMeshPro Font Standardization:**
  - Standard font asset: `Assets/PixelSurvivor/Resources/Fonts/VCR_OSD_MONO SDF.asset`.
  - 1-click Editor utility `PixelSurvivor/UI: Convert All Legacy Text to TextMeshPro` (`TMPTextMigrationHelper.cs`).
  - 0 compiler warnings/errors on `Assembly-CSharp.csproj`.

## Current Priority Queue
* [x] **EXP Drop & Collect System (Completed & Verified):**
  - Gem prefabs (`Drop_ExpOrb_Green` 10 EXP, `Drop_ExpOrb_Yellow` 50 EXP, `Drop_Emerald` 1 💎, `Drop_LuckyBlock`).
  - Vacuum/Magnet pickup physics on Player (`PlayerCollector.cs`) with accelerating lerp.
  - Linked EXP collection to top HUD EXP Bar (`EXP_Bar_Container/Background/Fill`) and Level Text.
  - Auto-invokes `LevelUpManager.TriggerLevelUp()` on bar 100% fill.
  - Converted Reroll button in Level Up modal to use in-run Emeralds (10 💎 base, 50% discount for Villager).
* [x] **User UI Customization & Polish (Locked):**
  - Fine-tuned `Player_Stats` layout (`pos=(210, -90)`), HP text (`100/100`), Level text (`Lv.1` in Gold `#F4B132`), Character name sync (`Steve`).
  - TopCenter container restyled (`Time_Text` 40pt bold `#F8FAFC`, `Biome_Text` 18pt `#999999`).
  - EXP Bar stretches 100% across top edge with dark obsidian background and cyan dynamic fill.
  - Enlarged damage numbers (44/60pt, 1.65x punch) & drop items (1.25x - 1.45x scale).
  - Reduced pickup radius by 1/3 (1.7m base).
* [ ] **Multi-Mob Spawner Expansion (Next Immediate Task):**
  - Creeper runner with fuse countdown & AoE explosion.
  - Spider agile flanker (4.2 m/s).
  - Phased wave scaling across 15-minute survival timer.
* [ ] **Secondary Weapons:**
  - Rapid Bow (Alex starting weapon).
  - Lightning Staff / Fireball Wand.

## Backlog (Future Milestones)
* [ ] Character Selection Screen.
* [ ] In-Run Emerald Reroll Economy (In-match drops for Level Up card rerolls, No DB required).
* [ ] Boss Fights (Giant Nether Warden / Ender Dragon phase).
* [ ] Sound Effects & Retro 8-bit Music.
