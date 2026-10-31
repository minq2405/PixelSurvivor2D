# PIXEL SURVIVOR - DECISION & CONSTRAINTS LOG (v2.1 SPEC LOCKED)

This document locks all fundamental technical, mathematical, and design decisions from `PixelSurvivor_Core_Game_Design_v2.1_Patched.md`. The Agent MUST NOT deviate from or contradict these decisions.

---

## 1. Core Architecture & Physics
* **[LOCKED] Movement Implementation:** Player uses 2D top-down continuous movement via `Rigidbody2D.linearVelocity` (never use `AddForce` or direct `transform.position` translate for combat physics). Gravity Scale must be set to `0`. Dual-input fallback ensures WASD, Arrow keys, and Gamepad function regardless of project input manager configuration.
* **[LOCKED] Enemy Physics & Separation:** All enemies use Kinematic `Rigidbody2D` with velocity control. **Disable Enemy–Enemy collision** in the Unity 2D Physics Layer Collision Matrix to prevent CPU bottlenecks with large hordes (150–220 mobs). Separation is achieved via lightweight spatial grid boids/flocking updated at 10 Hz.
* **[LOCKED] TimeScale Architecture:** **Only ONE centralized class (`TimeScaleManager` or `InGamePauseManager`)** is permitted to write `Time.timeScale`, utilizing a reference-counting token system (`PauseToken` / `pauseLockCount`). `Time.timeScale = 1f` is only reset on `SceneManager.sceneLoaded`. **Setting `Time.timeScale` inside `Awake()` of any Manager is strictly forbidden**, preventing background menus from unpausing the game inadvertently.
* **[LOCKED] Weapons Auto-Attack Lifecycle:** All weapons inherit from `WeaponBase.cs` and must auto-acquire `PlayerController` in `Awake()`. All melee slash and projectile sprites are authored facing Right (0°, +X axis) with `vfxAngleOffset = 0f`.
* **[LOCKED] Weapons Targeting Priority:** Weapons auto-acquire `IDamageable` targets. Melee attacks swing toward the closest target. **Twin Daggers and Crossbow must strictly prioritize targets in the order: `Boss > Elite > Closest Mob`** to prevent DPS waste on minion swarms.
* **[LOCKED] Non-Allocating Queries & Pooling:** All runtime queries must use `Physics2D.OverlapCircleNonAlloc` or `ContactFilter2D` with preallocated buffers (e.g. 64 elements). Object pooling is mandatory for Enemies, Projectiles, Collectibles, and Damage Numbers.

---

## 2. Hard Stat Caps & Mathematical Formulas (v2.1)
* **[LOCKED] Global Player Hard Caps:**
  - **Armor Cap:** Maximum **35%** damage reduction.
  - **Cooldown Reduction (CDR) Cap:** Maximum **-30%** (Sàn tuyệt đối Twin Daggers: **0.25s**).
  - **Move Speed Bonus Cap:** Maximum **+40%** total (Steve max 5.0 m/s, Alex max 5.6 m/s, Villager max 5.2 m/s).
  - **Crit Chance Cap:** Maximum **60%**. (Any weapon stat "+X% Crit" strictly means +X percentage points of Crit Chance).
  - **Bonus Damage Cap:** Maximum **+60%** total.
* **[LOCKED] Contact Damage & Global I-Frame:**
  - **Global Contact I-Frame:** Set strictly to **0.35s** across all enemy contact hits.
  - **Surround Stacking Coefficient:** If surrounded by extra mobs touching simultaneously:
    $$\text{DMG}_{\text{hit}} = \text{DMG}_{\text{contact}}(t,m) \times \big(1 + 0.25 \times \min(N_{\text{extra}}, 4)\big)$$
    (Maximum +100% extra damage when surrounded by 4+ additional mobs).
  - **Non-Contact Damage Sources:** Creeper explosions, Witch poison puddles, enemy projectiles, and Boss abilities **bypass the contact I-frame**; each source has an independent 0.25s lock on the player.
  - **Enemy Contact Damage Scaling:** Scales over time as $\text{DMG}_{\text{contact}}(t,m) = \text{DMG}_{\text{base}} \times (1 + 0.05 \cdot t) \times \text{DamMult}_m$. Explosions scale at $(1 + 0.025 \cdot t)$.
  - **Regen Interrupt:** Innate character regen and Potion of Regen **pause for 1.0s** whenever damage is sustained.
* **[LOCKED] EXP Formula & Progression Target:**
  $$\text{EXP}_{\text{required}}(Lv) = \text{Round}(20 \times Lv^{1.20})$$
  - Cumulative EXP: Lv 5 = 247, Lv 10 = 1,286, Lv 16 = 3,778, Lv 33 = 19,262, Lv 46 = 40,384.
  - Target: **45–46 level-up picks at 15:00 (Lv 46–47)** on Normal.
  - Mode EXP Multipliers: Normal **1.00**, Hard **1.10**, Hardcore **1.25**.

---

## 3. Weapons & Evolution Decisions (v2.1)
* **[LOCKED] Slot Limits:** Strictly **4 Active Weapon Slots** (`MaxWeapons = 4`) and **4 Passive Slots** (`MaxPassives = 4`).
* **[LOCKED] Weapon Scaling (DPS Budget B):**
  - All weapons scale within DPS budget $B(L) = 22 \times 2^{(L-1)/3}$ (Lv 1–4) and $44 \times 2^{(L-4)/4}$ (Lv 5–8).
  - Lv 8 target DPS: Broadsword 87.5 (including Burn: 4/7/10 dmg/s non-stacking over 3s), Battle Axe 80.0 (Stagger 0.4s, KB 5.5), Twin Daggers 171.4 (+8% Crit), Dark Scythe 70.5 (Heal 1–1.5 HP/kill, max 5 HP/s), Crystal Laser 83.6, Crossbow 87.5 (max 3 pierce, -25% base dmg per pierce).
* **[LOCKED] Evolution Rules & Gating:**
  - **Gated to ≥ 09:00:** Evolution cannot occur before 9 minutes.
  - **Run Ceiling:** Maximum **2 evolved weapons per match**.
  - **Nether Golem I Chest (5:00):** Grants +1 Level to up to 3 owned items (no evolution).
  - **Nether Golem II Chest (10:00):** Guaranteed evolution if requirements (Lv 8 weapon + Lv 5 paired passive) are met.
  - **Evolved Weapons Profile (Target ≈ 1.5x Lv8):**
    - *Solar Flare Greatsword:* 360° Nova 3.0m (95 dmg, CD 0.8s) + Aura 12 dmg/s up to 12 closest mobs within 6m.
    - *Executioner's Greataxe:* Cone 3.6m (150 dmg, CD 1.25s) + Execute regular mobs below 10% HP.
    - *Shadow Phantom Daggers:* 8x16 every 0.50s (256 DPS) + 1.5m micro-dash.
    - *Reaper's Harvest:* 90 dmg every 0.85s (106 DPS) + 4% lifesteal (caps at 8 HP/s, replaces kill heal, excludes TNT/summons).
    - *Oblivion Death Ray:* 2 rotating beams 0.8m wide, 90°/s, tick 0.20s x 22 dmg (110 DPS), max 15 targets/tick, slows to 30°/s for 0.6s when sweeping Bosses.
    - *Gatling Repeater:* 5-arrow fan (20° cone, 5x13 every 0.50s, 130 DPS), 1.0m splash, pierce 4 with -15%/pierce.
* **[LOCKED] Passive Retention & Evolution Pity:**
  - Passive items at Lv 5 (MAX) **continue to occupy their passive slot**.
  - Evolution Pity starts at **08:00**. If player holds Lv 8 weapon and an open passive slot, matching passive appears with weight x3 and 100% guarantee within <= 3 level-ups.
  - If 4 passive slots are full without the matching passive, level-up presents a **Swap** card to trade 1 owned passive for the pair at Lv 1.
  - Backup pool when all slots maxed: Hearty Meal (heal 30%), Emerald Pouch (+10 💎), Scholar's Tome (+25% EXP 60s, non-stacking), Bounty Mark (guarantee chest next Elite, max 3/run).

---

## 4. Enemy, Elite & Boss Architecture (v2.1)
* **[LOCKED] 12 Enemy Mob Archetypes:**
  - Standard Chasers (Zombie, Husk, ZombieV2, HuskShovel) accelerate at $+0.02\text{ m/s/min}$.
  - Mob HP scales with Final Surge: $\text{HP}(t,m) = \text{HP}_{\text{base}} \times H0_m \times (1 + s_m \cdot t) \times \text{Surge}(t)$, with $\text{Surge}(t) = 1 + 0.35 \times \text{clamp}((t-12)/3, 0, 1)$.
  - Spider (4.3 m/s), Vex (4.5 m/s), Phantom (4.2 m/s) are faster flankers with low HP ($\le 0.6\times$ avg).
* **[LOCKED] Summoned Mobs & Loot Exploits:**
  - **Summoned mobs (Vex from Evokers, split Slimelets, Sculk Burst, Golem Zombies) DROP ZERO EXP AND ZERO EMERALDS**, and grant no Scythe/Reaper healing.
  - Global mob cap (150/180/220) applies to all active entities; summoned entities share a sub-cap of 25.
* **[LOCKED] Elite Budget & Affixes:**
  - Elite spawns on 2-second budget timer: Normal 3% (from 2:00, max 2 alive), Hard 5% (from 1:30, max 4 alive), Hardcore 8% (from 1:00, max 6 alive). First Elite is guaranteed.
  - Stats: HP x2.5, Damage x1.30, Size x1.20, KB Resist +0.25, ground feet offset 0.0000.
  - Lucky Block drop rate: Normal 20%, Hard 17%, Hardcore 15% (Pity +10%, max chests/run: 10/9/8, min gap 20s).
  - Affix Swift (+12% speed) excluded from Spider/Vex/Phantom and cannot roll with Berserk.
* **[LOCKED] Boss Specifications:**
  - Nether Golem I (5:00): 3,300 HP Normal (Hard x1.20, Hardcore x1.40). Slam 3.0m (35 dmg), Lava Throw 3 bullets.
  - Nether Golem II Enraged (10:00): 9,600 HP Normal. Slam 3.6m (50 dmg), Lava Throw 5 bullets, Enrage at <50% HP (+20% speed, flame trail).
  - The Warden (15:00): 42,000 HP Normal. Smash (60 dmg), Sonic Boom (45 dmg), Charge (55 dmg, 5.5 m/s), Sculk Tendril Pull (25 dmg, triggers when player > 5.5m away, pulls to 3.0m). Enrage timer: 180s Normal / 150s Hard / 130s Hardcore (+50% dmg, +35% move speed).
  - Boss damage does NOT multiply with elapsed match time $(1 + 0.05t)$. When Warden spawns, regular mob spawning decreases by 60%.

---

## 5. Economy & Collectibles Decisions (v2.1)
* **[LOCKED] Breakable Golden Lucky Block:** 60 HP, requires 3 hits (0.25s hit I-frame). Drops 25% of next level EXP (min 5 EXP) + 2–5 emeralds + bonus item.
* **[LOCKED] Screen Nuke TNT (v2.1 Adjusted):**
  - Radius fixed to **12.0m circular radius** from player center (decoupled from screen aspect ratio).
  - Regular mobs within radius destroyed (99,999 dmg); Elites take 35% Max HP; Bosses immune.
  - EXP reward capped at 50% of next level; emeralds capped at 15. No lifesteal triggered. Min 45s gap between TNT drops.
* **[LOCKED] Emerald Rerolls:** 1st reroll per level-up is FREE (0 💎). Subsequent paid rolls: [2, 3, 5, 8, 12, 18] 💎 (max 5 paid rolls; Villager max 6 paid rolls with 50% discount).
* **[LOCKED] Gem Auto-Merge:** When total active gems exceed 250 on the map, adjacent gems automatically merge into higher tier gems to safeguard frame rates.
