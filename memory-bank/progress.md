# PIXEL SURVIVOR - PROGRESS & ROADMAP

## What Works (Completed & Verified)
* [x] **Project Foundation:** Unity 6 LTS URP 2D, New Input System, scene structure.
* [x] **Player Movement:** 8-directional top-down movement with linear velocity and walk cycle animations.
* [x] **Character Assets:** Steve, Alex, Villager spritesheets sliced and PPU normalized to 32.
* [x] **Complete 12 Enemy Roster:**
  - `Enemy_Zombie`: 50 HP, basic mob, 14 walk frames.
  - `Enemy_ZombieV2`: 65 HP (+30%), 2.0 m/s (+11%), 8 dmg (+33%), 12 walk frames.
  - `Enemy_Husk`: 75 HP (+50%), 1.85 m/s (+3%), 9 dmg (+50%), 13 walk frames.
  - `Enemy_Spider`: 30 HP, 4.2 m/s, 8 dmg, 16 walk frames.
  - `Enemy_Creeper`: 60 HP, 1.2s priming fuse, visual scale swelling (1.0 -> 1.35x), 45 AoE dmg to player, 55 friendly fire AoE.
  - `Enemy_Slime`: 60 HP, X-axis parabolic hop AI, splits into 2 small slimes on defeat.
  - `Enemy_Witch`: 40 HP, ranged amethyst spikes, ground circle telegraph.
  - `Enemy_HuskShovel`: 85 HP tank, 50% knockback resistance, heavy shovel slam.
  - `Enemy_IceCreeper`: 65 HP, 1.2s fuse, 35 AoE dmg + 45% slow for 1.8s.
  - `Enemy_Vex`: 25 HP, 0.65x scale flying minion, 16f swoop attack.
  - `Enemy_Phantom`: 35 HP, 4.0 m/s prowl hover, 6.5 m/s dive attack.
  - `Enemy_Evoker`: 150 HP elite summoner, golden circle telegraph, summons 2-3 Vex.
* [x] **Complete 6 Weapons System & Upgrades (Lv 1 -> 8):**
  - `Weapon_MagmaBroadsword`: 25 DMG, 1.1s CD, frontline cleave.
  - `Weapon_BattleAxe`: 45 DMG, 1.65s CD, massive 7.0 knockback.
  - `Weapon_TwinDaggers`: 14 DMG, 0.65s CD, 35 FPS sequential alternate slash, Lv5 3-combo, Lv8 4-combo.
  - `Weapon_DarkScythe`: 32 DMG, 1.35s CD, immense 270-degree cleave.
  - `Weapon_CrystalLaser`: 22 DMG, 1.75s CD, piercing beam with emitter tip pivot (zero rear artifacts).
  - `Weapon_Crossbow`: 24 DMG, 0.95s CD, green energy orb (0.5x scale), Lv4 2 arrows, Lv7 3-arrow fan, Lv8 5 pierce.
  - Starting Loadouts (Option A): Steve $\rightarrow$ Broadsword; Alex $\rightarrow$ Twin Daggers; Villager $\rightarrow$ Crossbow.
  - Max 4 weapon slots limit enforced (`MaxWeapons = 4`).
* [x] **Elite Enemy System (GDD Spec):**
  - 3 Difficulty Configs (`Config_Normal.asset`, `Config_Hard.asset`, `Config_Hardcore.asset`).
  - 6 Affixes (`Berserker`, `Volatile`, `ChillingAura`, `Shielded`, `Vampiric`, `MagneticPull`).
  - Overhead golden health bar with `★ ELITE ★` title and live HP.
  - Elite Drop Table: 150 EXP Huge Yellow Gem, Lucky Block, Steak (50 HP), Magnet, TNT.
* [x] **Combat Feedback & Floating Text:**
  - `DamageNumber.cs` floating numbers with critical hits and custom alert text (`BLOCKED!`, `BERSERK!`, `BOOM!`).
* [x] **Modular Canvas UI & Settings:**
  - InGame HUD, EXP bar top container, Pause Menu, Defeat Menu, HD Settings Modal.
  - 2-Click Level Up Card selection system.
* [x] **Economy & EXP:**
  - `ExperienceManager.cs` tracking EXP progression and in-run Emerald economy (Zero DB).
  - Drops: Green EXP (10), Yellow EXP (50), Huge EXP (150), Emeralds, Lucky Blocks, Steak, Magnet, TNT.

## Roadmap (Next Steps)
* [ ] **15-Minute Wave Timeline Finalization:**
  - Define exact minute-by-minute mob compositions (min 0 to 15), spawn rates, swarm events, and miniboss waves.
* [ ] **Boss Encounter (The Warden):**
  - Minute 15:00 Final Boss arrival, custom arena telegraphs, multi-stage attacks.
