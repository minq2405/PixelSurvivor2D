# PIXEL SURVIVOR - PROGRESS & ROADMAP

## What Works (Completed & Verified)
* [x] **Project Foundation & Controls:** Unity 6 LTS URP 2D, Dual-Input movement (New Input System + Legacy Input fallback), linear velocity 2D physics.
* [x] **Character Assets & Base Stats:**
  - Steve (The Iron Survivor): 4.0 m/s base speed, 120 HP, starts with Flaming Broadsword.
  - Alex (The Swift Ranger): 5.0 m/s base speed, 85 HP, starts with Twin Daggers.
  - Villager (The Fortune Merchant): 4.4 m/s base speed, 100 HP, starts with Crossbow, 50% reroll discount.
* [x] **Complete 12 Enemy Roster (Rebalanced Speeds & Wave Spawner):**
  - `Enemy_Zombie`: 50 HP, 3.50 m/s base (kiteable early game) $\rightarrow$ 3.95 m/s at 15:00.
  - `Enemy_Husk`: 75 HP, 3.50 m/s base $\rightarrow$ 3.95 m/s at 15:00.
  - `Enemy_ZombieV2`: 65 HP, 3.60 m/s base $\rightarrow$ 4.05 m/s at 15:00.
  - `Enemy_HuskShovel`: 85 HP, 3.60 m/s base $\rightarrow$ 4.05 m/s at 15:00.
  - `Enemy_Spider`: 30 HP, 4.50 m/s base (faster than Steve) $\rightarrow$ 4.88 m/s at 15:00.
  - `Enemy_Vex`: 25 HP, 4.80 m/s base (fastest mob, flying harasser) $\rightarrow$ 5.18 m/s at 15:00.
  - `Enemy_Phantom`: 35 HP, 4.40 m/s base $\rightarrow$ 4.78 m/s at 15:00.
  - `Enemy_Creeper` & `Enemy_IceCreeper`: 60-65 HP, 3.40 m/s base $\rightarrow$ 3.70 m/s at 15:00.
  - `Enemy_Slime`: 60 HP, 3.20 m/s base $\rightarrow$ 3.50 m/s at 15:00.
  - `Enemy_Witch`: 40 HP, 3.30 m/s base $\rightarrow$ 3.60 m/s at 15:00.
  - `Enemy_Evoker`: 150 HP, 3.20 m/s base $\rightarrow$ 3.50 m/s at 15:00.
  - 5-Phase Wave Spawner timeline scaling mob compositions naturally over 15 minutes.
* [x] **Complete 6 Weapons System & Upgrades (Lv 1 -> 8):**
  - `Weapon_MagmaBroadsword`: 25 DMG, 1.1s CD, frontline cleave.
  - `Weapon_BattleAxe`: 45 DMG, 1.65s CD, massive 7.0 knockback.
  - `Weapon_TwinDaggers`: 14 DMG, 0.65s CD, 35 FPS sequential alternate slash, Lv5 3-combo, Lv8 4-combo.
  - `Weapon_DarkScythe`: 32 DMG, 1.35s CD, immense 270-degree cleave.
  - `Weapon_CrystalLaser`: 22 DMG, 1.75s CD, piercing beam with emitter tip pivot (zero rear artifacts).
  - `Weapon_Crossbow`: 24 DMG, 0.95s CD, green energy orb (0.5x scale), Lv4 2 arrows, Lv7 3-arrow fan, Lv8 5 pierce.
  - Max 4 weapon slots limit enforced (`MaxWeapons = 4`).
* [x] **Single Unified Elite Enemy System:**
  - Any mob can spawn as Elite based on difficulty rate.
  - 12-frame animated amethyst ring (`vfx_elite_aura_ring`) at feet with exact $0.0000$ offset.
  - Small overhead health bar in Elite Amethyst Purple (`#A855F7`).
  - Stat buff: HP $\times 2.5$, Damage $\times 1.25$, Scale $1.20\times$, Knockback resistance $+0.25f$.
  - Original mob sprite coloration 100% preserved.
  - Elite Drop Table: 150 EXP Huge Yellow Gem, Breakable Lucky Chest, Steak, Emeralds.
* [x] **Breakable Golden Lucky Block Chest:**
  - Stationary breakable prop implementing `IDamageable` (3 hits / 60 HP to open).
  - Punch scale wobble and `CRACK! (1/3)` floating text on hit.
  - On break: showers 70 EXP gems + 2-5 Emeralds + bonus item (Steak, Magnet, TNT, or 10% Jackpot).
* [x] **Instant Screen Nuke TNT:**
  - Picking up TNT immediately eliminates all active on-screen enemies within camera viewport with 99,999 lethal DMG. Zero delay, no explosion animation.
* [x] **Level Up Free Reroll & Emerald Economy Balancing:**
  - 1st reroll of every level up is 100% FREE (0 💎).
  - Paid rerolls start at 1 💎 (+1 💎 per paid roll, capped at 5 💎, Villager 50% discount).
  - Emerald drop rates rebalanced: 5% regular mobs (1-2 💎), 15% specialist mobs (2-3 💎), 3-10 guaranteed emeralds from Elite mobs.
* [x] **HUD Improvements:**
  - `Counters_Row` under HP bar: `[💀] <kills>` and `[💎] <emeralds>`.
  - Top-Right anti-aliased rounded pause button below EXP bar with white border & transparent dark background.
* [x] **Combat Feedback & Floating Text:**
  - `DamageNumber.cs` floating numbers with critical hits, custom alert text, and play-mode safety checks.
* [x] **Economy & EXP:**
  - `ExperienceManager.cs` tracking EXP progression and in-run Emerald economy (Zero DB).

## Roadmap (Next Steps)
* [ ] **Passive Items System Implementation (Option 1 - User Confirmed):**
  - Implement 4-Passive Slot Limit (`MaxPassives = 4`).
  - Implement 5-Level progression (Lv 1 to Lv 5 MAX) for 9 passives using prepared sprites.
  - Level Up card pool dynamically offering new passives (slots $< 4$) and upgrades (level $< 5$).
  - Weapon Evolution synergy (Lv 8 Weapon + Lv 5 Passive $\rightarrow$ Evolved Weapon).
* [ ] **15-Minute Timeline Mini-Bosses & Final Boss:**
  - Minute 05:00 & 10:00: Nether Golem Mini-Boss (2,000 HP, slam AoE, high-tier loot).
  - Minute 15:00: The Warden Final Boss (12,000 HP, multi-phase).
* [ ] **Difficulty Modes Tuning (Normal, Hard, Hardcore):**
  - User review & custom adjustments.
