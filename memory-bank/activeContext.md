# PIXEL SURVIVOR - ACTIVE CONTEXT

## Current Focus & Status
* **Recent Milestones Achieved:**
  - **Complete 12 Enemy Roster (Completed & Verified in Scene):**
    - **Batch 1 (Standard Core):** `Enemy_Zombie` (50 HP, basic mob), `Enemy_Spider` (30 HP, 4.2 m/s flanker), `Enemy_Creeper` (60 HP, 1.2s fuse, 45 AoE dmg).
    - **Batch 2 (Specialist Mobs):** `Enemy_Slime` (60 HP, X-axis parabolic hop, splits into 2 small slimes on defeat), `Enemy_Witch` (40 HP, amethyst spikes with ground circle telegraph), `Enemy_HuskShovel` (85 HP tank, 50% knockback resistance, heavy shovel slam).
    - **Batch 3 (Elite & Airborne Mobs):** `Enemy_IceCreeper` (65 HP, 35 AoE dmg + 45% slow for 1.8s), `Enemy_Vex` (25 HP, 0.65x scale flying minion, 16f swoop attack), `Enemy_Phantom` (35 HP, 4.0 m/s prowl hover, 6.5 m/s dive attack), `Enemy_Evoker` (150 HP elite summoner, golden summoning circle, summons 2-3 Vex).
    - **Batch 4 (Early-game Variants):** `Enemy_ZombieV2` (65 HP, 2.0 m/s, 8 dmg), `Enemy_Husk` (75 HP, 1.85 m/s, 9 dmg).
    - Early-Game Diversity: Spawn ratio in WaveSpawner updated (Zombie 50%, Zombie V2 25%, Husk 25%) ensuring Zombie is no longer 100% early game.
  - **Complete 6 Weapons & Upgrade System (Completed & Verified):**
    - **All 6 Weapons Built as Prefabs:** `Weapon_MagmaBroadsword`, `Weapon_BattleAxe`, `Weapon_TwinDaggers`, `Weapon_DarkScythe`, `Weapon_CrystalLaser`, `Weapon_Crossbow`.
    - **Hero Starting Loadouts (Option A):** Steve $\rightarrow$ Magma Broadsword; Alex $\rightarrow$ Twin Daggers; Villager $\rightarrow$ Crossbow.
    - **Level Up Progression (Lv 1 $\rightarrow$ 8):** Scaled damage, cooldown, reach, and unique unlocks (Twin Daggers Lv5 3-combo, Lv8 4-combo; Crossbow Lv4 2 arrows, Lv7 3-arrow fan spread, Lv8 5-target pierce; Crystal Laser beam thickness & reach; Axe & Scythe wide area knockback).
    - **Strict 4-Weapon Limit:** WeaponController enforces `MaxWeapons = 4`. LevelUpManager dynamic option pool generates New Weapon cards only when slots $< 4$, switching strictly to weapon upgrades and passive stat buffs once full.
  - **Weapon Visual & Animation Polish:**
    - **Twin Daggers:** 35 FPS animation speed (0.20s per slash), sequential delay (`strikeDelay = 0.20s` so strike 2 only begins after strike 1 finishes), alternate horizontal flip (`sr.flipY = true` on strike 2 for scissor X-slash combo).
    - **Crystal Laser:** Slices pivot re-centered to emitter tip `(12px, 20px)`. Spawn origin placed at `player.position + beamDir * 0.45f`, completely eliminating rear artifacts behind the character.
    - **Crossbow Projectile:** Replaced with newly exported green `energy-orb.png` (8 frames, 48x48) scaled down by half (`0.5x` scale, 0.22m collision radius).
  - **Unified Elite Enemy System (Refactored to User Specification):**
    - Single unified Elite archetype: Any Mob + `vfx_elite_aura_ring` (12-frame animated ring beneath feet at 12 fps, PPU 32, sortingOrder 4) + Sleek overhead small purple health bar (`#A855F7`).
    - Original mob sprite coloration 100% retained (no yellow tinting).
    - Stat scaling: HP $\times 2.5$, Damage $\times 1.25$, Scale $1.20\times$, Knockback resistance $+0.25f$.
    - Elite Drop Table: 150 EXP Huge Yellow Gem + Special drop (Lucky block, Steak, Emeralds) via `ExperienceManager.Instance.SpawnEliteDrops()`.
  - **Level Up Free Reroll & Emerald Economy Balancing:**
    - First reroll of EVERY level up is strictly FREE (0 💎).
    - Paid rerolls start at only 1 💎 (+1 💎 per paid roll, capped at 5 💎, Villager 50% discount).
    - Emerald drops rebalanced: Regular mobs 5% base (1-2 💎), Specialist mobs 15% base (2-3 💎), Elite mobs guaranteed 3, 5, 10 💎 burst.
  - **HUD Improvements:**
    - Counters row added under HP bar in `Player_Stats`: `[💀] <kills>` and `[💎] <emeralds>`.
    - Top-right rounded pause button with white border, dark transparent background, and anti-aliasing via `RoundedBorderImage`.

## Current Priority Queue (Ready for Next Session)
1. **15-Minute Wave Timeline Finalization:**
   - Finalize the full minute-by-minute timeline (Minutes 0 to 15) for all 12 mobs, wave density, and miniboss waves according to user's final plan.
2. **Phase 5 Boss Encounter (The Warden):**
   - Minute 15:00 Final Boss arrival, custom boss arena / music transition, special attacks (sonic boom, ground slam).
