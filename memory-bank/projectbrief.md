# PIXEL SURVIVOR - PROJECT BRIEF (v2.1 SPEC ALIGNED)

## 1. Project Overview
* **Project Name:** Pixel Survivor
* **Genre:** 2D Top-Down Fast-Paced Roguelite Action Survivor (Vampire Survivors gameplay + Minecraft Aesthetic)
* **Target Platforms:** PC (Keyboard/Mouse) & Mobile (Touch/Virtual Joystick)
* **Engine & Version:** Unity 6 LTS (6000.4.10f1)
* **Render Pipeline:** Universal Render Pipeline (URP 2D)
* **Sprite Standards:** Strict PPU = 32, Point (no filter) texture sampling, Compression None.
* **Input System:** Dual-Input Support (Unity New Input System Package `InputSystem_Actions` + Legacy Input Manager Fallback `Input.GetAxisRaw`).
* **In-Game Language:** 100% English.
* **Architecture:** Zero-DB (all resources, EXP, emeralds, upgrades live exclusively in the 15-minute match session). Single `TimeScaleManager` with reference-counted pause locks.
* **Run Duration:** 15:00 survival timer + The Warden final encounter.

## 2. Core Gameplay Loop
1. **Move & Kiting:** Player maneuvers across infinite tilemap chunks using `Rigidbody2D.linearVelocity`, automatically attacking threats within weapon range (weapons auto-acquire `IDamageable` and aim forward/at closest target, 0° right sprite baseline).
2. **Collect & Level Up:** Defeated regular enemies drop EXP gems/emeralds. Fills EXP bar calculated via v2.1 curve `EXP = Round(20 * Lv^1.20)`. Target progression: 45–46 level-up picks (Lv 46–47) at 15:00 on Normal mode.
3. **Choose Upgrades (Weapons & Passives):**
   - **Strict Slot Limits:** Max 4 Weapons (`MaxWeapons = 4`) and Max 4 Passives (`MaxPassives = 4`).
   - **Tiers:** Weapons upgrade Lv 1 ➔ Lv 8 (MAX); Passives upgrade Lv 1 ➔ Lv 5 (MAX).
   - **Passive Slot Rule:** Passives at Lv 5 (MAX) are removed from the pool but **strictly continue to occupy their passive slot**.
   - **Backup Card Pool:** When all equipped items reach max tier, backup cards appear (Hearty Meal, Emerald Pouch, Scholar's Tome, Bounty Mark max 3/run).
   - **Evolution Gating (v2.1):** Weapon Evolution is strictly gated to **≥ 09:00** with a ceiling of **max 2 evolutions per run**. Golem I chest (5:00) gives +1 level to items without evolution; Golem II chest (10:00) guarantees evolution if Lv 8 weapon + Lv 5 passive are met.
   - **Evolution Pity & Swap:** Active from 08:00; guarantees paired passive within ≤ 3 level-ups if slot open, or offers a **Swap** card if all 4 passive slots are filled without the matching pair.
4. **Boss Encounters & Timed Waves:**
   - 05:00: Nether Golem I Mini-Boss (3,300 HP Normal).
   - 10:00: Nether Golem II Enraged Mini-Boss (9,600 HP Normal).
   - 15:00: The Warden Final Boss (42,000 HP Normal) featuring multi-phase attacks, Sculk Tendril anti-kite pull, and a 180s/150s/130s Enrage timer.

## 3. Playable Characters (v2.1 Rebalanced)
* **Steve (The Iron Survivor):**
  - Melee brawler / tank.
  - Base Speed: 4.0 m/s (baseline standard, max 5.0 m/s with speed buffs).
  - Health: 120 HP, 10% Armor, 0.5 HP/s Regen (regen pauses 1.0s upon taking damage).
  - Starting Weapon: Magma Broadsword.
  - Innate Passive: *Iron Will* (+20% Damage when below 50% HP).
* **Alex (The Swift Ranger):**
  - High-mobility scout / glass cannon boss-slayer.
  - Base Speed: 4.8 m/s (+20% above Steve; max 5.6 m/s; v1 was 5.0).
  - Health: 85 HP, 0% Armor, 0.0 HP/s Regen.
  - Crit: 12% Crit Chance, 190% Crit Multiplier (v1 was 15% / 200%).
  - Starting Weapon: Twin Daggers.
  - Innate Passive: *Agility* (-10% Cooldown, respects the hard -30% CDR cap).
* **Villager (The Fortune Merchant):**
  - Economy & reroll specialist.
  - Base Speed: 4.2 m/s (+5% above Steve; max 5.2 m/s).
  - Health: 100 HP, 0% Armor, 0.2 HP/s Regen (regen pauses 1.0s upon hit).
  - Pickup Magnet: 2.2 m (+30%).
  - Starting Weapon: Crossbow.
  - Innate Passive: *Merchant Luck* (Reroll cost -50%, emerald drops ×1.5, +1 extra paid reroll cap per level-up).
