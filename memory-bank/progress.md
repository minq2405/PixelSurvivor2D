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
* [x] **Multi-Mob Spawner Expansion - Batch 1 (Completed & Verified):**
  - **Enemy Prefabs:** Built `Enemy_Zombie.prefab` (50 HP, 1.8 m/s, 6 dmg), `Enemy_Spider.prefab` (30 HP, 4.2 m/s, 8 dmg, 16 frames), `Enemy_Creeper.prefab` (60 HP, 3.2 m/s, 10 dmg) in `Assets/PixelSurvivor/Enemies/Prefabs/`.
  - **Creeper Kamikaze AI (`CreeperAI.cs`):** 3-state state machine (Chasing -> Priming -> Exploded), 1.6m trigger radius, 1.2s fuse countdown with transform swelling (1.0 -> 1.35x) and white blink, 45 AoE damage + knockback to Player, 55 friendly fire AoE damage to surrounding mobs (enables chain reactions!), shockwave and smoke VFX. Dies without explosion if killed before fuse finishes.
  - **Spider Agile Flanker:** 4.2 m/s sprint speed, tight 0.50m separation radius, 14 FPS walk animation cycle.
  - **Phased Wave Spawner Integration (`WaveSpawner.cs`):**
    - Minute 0–2: 100% Zombie.
    - Minute 2–5: 65% Zombie, 35% Spider.
    - Minute 5+: 50% Zombie, 30% Spider, 20% Creeper.
    - Controlled linear HP scaling: $\text{HP}(t) = \text{HP}_{\text{base}} \times (1 + 0.25t) \times \text{DiffMod}$ (caps in hundreds, strictly no 1000+ HP bullet sponges).
    - Difficulty modes: Normal, Hard, Hardcore with tailored speed, damage, and density multipliers.
  - **1-Click Setup Utility:** `PixelSurvivor/Setup Enemy Prefabs (Zombie, Spider, Creeper)` (`EnemyPrefabSetupHelper.cs`).
* [x] **Multi-Mob Spawner Expansion - Batch 2 (Completed & Verified):**
  - **Enemy Prefabs:** Built `Enemy_Slime.prefab` (60 HP, 4.8 m/s leap, 15 frames), `Enemy_Witch.prefab` (40 HP, 2.2 m/s, Amethyst Spikes 20 dmg), `Enemy_HuskShovel.prefab` (85 HP, 50% Knockback Res, 18 dmg shovel slam) in `Assets/PixelSurvivor/Enemies/Prefabs/`.
  - **Slime AI (`SlimeAI.cs`):** Jump physics synchronized with 15 frames (Frame 0-5 squash/windup v=0; Frame 6-10 leap impulse v=4.8 m/s toward player; Frame 11-14 land/rest v=0). On death, splits into 2 smaller slimes (25 HP, 0.58x scale, nippier leap speed).
  - **Witch AI (`WitchAI.cs`):** Mid-range caster (4.5m - 6.5m sweet spot, backs away if player gets too close). Casts `witch_attack` animation to summon Amethyst Spikes (`vfx_evoker_skill` tinted Amethyst Violet `#C084FC`) directly beneath player dealing 20 AoE damage.
  - **Husk Shovel AI (`HuskShovelAI.cs`):** Heavy frontline tank (85 HP, 50% knockback resistance). Approaches to 1.35m and executes heavy shovel slam attack (`husk_shovel_attack`) dealing 18 damage and 8.0f knockback.
  - **Debug & Rapid Testing:** Wired into `CombatDebugOverlay.cs` with hotkeys `[4] Slime`, `[5] Witch`, `[6] Husk Shovel` and `WaveSpawner.cs` ContextMenus.
  - **Wave Timeline:** Deferred per user instruction (will be finalized once all enemy types are complete).
* [ ] **Remaining Enemies (Batch 3 & Mini-Bosses):**
  - Flying units (Phantom, Vex).
  - Ice Creeper, Evoker.
  - Mini-Bosses (Hoglin, Ghast).
* [ ] **Secondary Weapons:**
  - Rapid Bow (Alex starting weapon).
  - Lightning Staff (Piercing laser beam `LaserBeamWeapon.cs`, not chain lightning).

## Backlog (Future Milestones)
* [ ] Character Selection Screen.
* [ ] In-Run Emerald Reroll Economy (In-match drops for Level Up card rerolls, No DB required).
* [ ] Boss Fights (Giant Nether Warden / Ender Dragon phase).
* [ ] Sound Effects & Retro 8-bit Music.
