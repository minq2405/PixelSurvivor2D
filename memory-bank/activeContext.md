# PIXEL SURVIVOR - ACTIVE CONTEXT

## Current Focus & Status
* **Recent Milestones Achieved:**
  - **User UI Customization & Polish (Locked & Preserved):**
    - `Player_Stats`: Fine-tuned to `pos=(210.00, -90.00)`, `size=(329.35, 66.01)`.
    - `HP_Text`: Size 14, crisp white `#FFFFFF`, format `100/100`.
    - `Level_Text`: Size 18, Aurum Gold `#F4B132`, dynamically synced to `ExperienceManager.CurrentLevel`.
    - `Character_Name`: Size 20, white `#FFFFFF`, dynamically synced to `PlayerController.characterName`.
    - `TopCenter_Container`: Position `(0.00, -63.00)`, size `(360, 75)`, `Time_Text` size 40 bold (`#F8FAFC`), `Biome_Text` size 18 (`#999999`).
    - `EXP_Bar_Container`: Stretches 100% across the top edge (`anchorMin = (0, 1)`, `anchorMax = (1, 1)`, height 16px), dark obsidian background (`#0B0F19`), dynamic Diamond Cyan fill (`#38BDF8`).
    - Floating damage numbers: Enlarged to size 44 (Crit: 60), `punchScale = 1.65x`.
    - Drop items: Enlarged to 1.25x - 1.45x scale with 0.45m - 0.50m collision radius.
    - Magnet collection radius: Reduced by 1/3 to 1.7m base (2.38m for Villager).
  - **EXP Gem Drops & Magnet Vacuum Pickup (Complete):**
    - Created `PickupItem.cs` with pop physics arc, vacuum acceleration physics, and instant collect dispatch.
    - Created `PlayerCollector.cs` on Player with 1.7m base radius (+40% for Villager).
    - Created `ExperienceManager.cs` on `[Game_Manager]` tracking smooth Roguelite EXP curve and in-run Emerald economy (Zero DB).
    - Built 4 drop prefabs in `Assets/PixelSurvivor/Items/Prefabs/` (`Drop_ExpOrb_Green`, `Drop_ExpOrb_Yellow`, `Drop_Emerald`, `Drop_LuckyBlock`).
    - Wired `InGameHUD` EXP fill bar and Level Text to `ExperienceManager`.
    - Wired `EnemyHealth.Die()` to drop 100% EXP orbs + 2-6% Emeralds + Elite/Boss drops.
    - Wired `LevelUpManager` Reroll button to consume in-run Emeralds (10 💎 base, 50% Villager discount).
  - **Multi-Mob Expansion - Batch 1 (Complete & Live in MainGame Scene):**
    - Created `Enemy_Zombie.prefab` (50 HP, 1.8 m/s, 6 dmg, 14 walk frames).
    - Created `Enemy_Spider.prefab` (30 HP, 4.2 m/s, 8 dmg, 16 walk frames, 14 FPS animation).
    - Created `Enemy_Creeper.prefab` (60 HP, 3.2 m/s, 10 dmg, 13 walk frames, 15 attack/swelling frames, 8 shockwave frames, 10 smoke frames).
    - `CreeperAI.cs`: Priming fuse (1.2s), visual scale swelling (1.0 -> 1.35x), red/white blink, 45 AoE dmg to Player with knockback, 55 friendly fire damage to nearby mobs (chain reactions), death without detonation if defeated before fuse finishes.
    - Updated `WaveSpawner.cs`: Dynamic phased mob spawning (Min 0-2: Zombie; Min 2-5: Zombie + Spider; Min 5+: Zombie + Spider + Creeper). Linear HP scaling HP(t) = HP_base * (1 + 0.25t) * DiffMod capping regular mob HP in the hundreds (max 237 HP Normal, max 356 HP Hardcore at min 15).
    - Setup Helper: `PixelSurvivor/Setup Enemy Prefabs (Zombie, Spider, Creeper)`.
    - Git commit `develop` (`ca7a0f1`) on `PixelSurvivor2D` and `master` (`3e339f7`) on `ProjectFinalPru`.

## Current Priority Queue
1. **Multi-Mob Spawner Expansion - Batch 2 (Awaiting User Confirmations):**
   - User confirmations on mob types: Witch (ranged potion thrower), Slime (splitting on defeat), or Evoker/Husk.
2. **Secondary Weapons:**
   - Rapid Bow (Piercing projectile weapon for Alex).
   - Lightning Staff (Piercing electrical laser beam via `LaserBeamWeapon.cs`, confirmed laser beam not chain lightning).
3. **Interactive Lucky Block System:**
   - Breakable / collectible lucky block with loot table (TNT nuke, steak heal, emerald stash, super magnet).
