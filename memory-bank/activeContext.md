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

## Current Priority Queue
1. **Multi-Mob Spawner Expansion (Immediate):**
   - Creeper prefab: High-speed runner (3.2 m/s), fuse countdown with white flash, 45 dmg AoE explosion.
   - Spider prefab: Agile swarm flanker (4.2 m/s), low HP (30 HP), quick melee nip.
   - WaveSpawner wave composition: Phased rollout (Min 0-2: Zombies; Min 2-5: Zombies + Spiders; Min 5: Mini-Boss; Min 6-9: Creepers + Spiders; Min 10-14: Full Horde; Min 15: Final Boss).
2. **Secondary Weapons:**
   - Rapid Bow (Piercing projectile weapon for Alex).
   - Lightning Staff (Piercing electrical laser beam via `LaserBeamWeapon.cs`).
3. **Interactive Lucky Block System:**
   - Breakable / collectible lucky block with loot table (TNT nuke, steak heal, emerald stash, super magnet).
