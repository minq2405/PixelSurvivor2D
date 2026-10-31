# PIXEL SURVIVOR - PROGRESS & ROADMAP (v2.1 SPEC ALIGNED)

## 1. What Works & Is Verified in Code (Baseline)
* [x] **Project Foundation & Controls:** Unity 6 LTS URP 2D, Dual-Input movement (New Input System + Legacy Input fallback), linear velocity 2D physics.
* [x] **Character Prefabs & Movement:** Steve, Alex, Villager base setups, animations, and starting weapon bindings.
* [x] **12 Enemy Mob Roster & Wave Spawner Foundation:** 12 enemy prefabs, basic wave timeline, directional movement towards player.
* [x] **6 Weapons System & Visuals:** 6 active weapon prefabs with auto-aim towards `IDamageable`, 0° right sprite baseline, slash VFX.
* [x] **Single Unified Elite Visuals & Alignment:** 12-frame amethyst aura ring with mathematically exact $0.0000$ foot offset across all mobs, purple overhead health bar (`#A855F7`).
* [x] **Breakable Golden Lucky Block Chest:** In-world prop requiring 3 hits (60 HP) to open with `CRACK! (1/3)` feedback and drop bursts.
* [x] **HUD System & Styling:** Tone 1 Dark Obsidian & Gemstone styling, full-screen EXP bar, HP bar, Counters Row (`[💀] Kills`, `[💎] Emeralds`), anti-aliased rounded pause button.
* [x] **Combat Feedback & Floating Text:** `DamageNumber.cs` floating damage numbers with crit styling and play-mode safety checks.

---

## 2. Updated Game Design Documentation (v2.1 Patched Completed)
* [x] **Full Game Design Spec v2.1 Patched Synced:** Synchronized `PixelSurvivor_Core_Game_Design_v2.1_Patched.md` and `PixelSurvivor_Core_Game_Design_Script.md` across both repositories and project directories.
* [x] **Memory Bank Fully Aligned:** `projectbrief.md`, `decisionLog.md`, `systemPatterns.md`, `progress.md`, and `activeContext.md` updated with all v2.1 locked constraints and formulas.

---

## 3. Implementation Roadmap (Awaiting User Confirmation Before Coding)

Once confirmed by the user, code modifications will be executed sequentially:

### Phase 1: Core Engine & Architecture Hardening
* [ ] **TimeScale Architecture (`TimeScaleManager.cs`):** Implement centralized reference-counted pause lock tokens; remove rogue `Time.timeScale` calls across managers.
* [ ] **Player Centralized Stats (`PlayerStats.cs`):**
  - Implement hard caps: Armor 35%, CDR 30%, Speed +40%, Crit 60%, Bonus DMG 60%.
  - Global contact I-frame (0.35s) + surround stacking (+25%/touching mob up to +100%).
  - Non-contact source locks (0.25s).
  - 1.0s regen pause on damage taken.
* [ ] **EXP Curve Update:** Implement $\text{EXP} = \text{Round}(20 \times Lv^{1.20})$ and mode multipliers (1.00 / 1.10 / 1.25). Target 45–46 picks at 15:00.

### Phase 2: Weapons, Passives & Evolution Balancing
* [ ] **Weapon DPS Budget Rebalancing:** Adjust all 6 weapons to v2.1 $B(L)$ formula (Daggers 171.4 DPS, Crossbow 87.5 DPS with 3-pierce -25%/pierce, Broadsword Burn included in budget, Axe stagger 0.4s).
* [ ] **Weapon Targeting Priorities:** Enforce `Boss > Elite > Closest Mob` on Daggers and Crossbow.
* [ ] **9 Passive Items System (Lv 1 to Lv 5):**
  - Enforce 4-passive slot limit.
  - Implement Lv 1 to Lv 5 stat scaling.
  - Ensure Lv 5 passives retain their slot while exiting the pool.
* [ ] **Evolution Gating & Pity System:**
  - Enforce evolution unlock at $\ge 09:00$ and max 2 evolutions per run.
  - Evolution Pity at 08:00 (weight x3, guarantee within $\le 3$ rolls, or offer Swap card if 4 slots filled).
  - Backup card pool (Hearty Meal, Emerald Pouch, Scholar's Tome, Bounty Mark).
* [ ] **6 Evolved Weapons Implementation:** Solar Flare, Executioner, Shadow Phantom, Reaper's Harvest, Oblivion Death Ray (0.8m wide, 90°/s, slows to 30°/s on boss), Gatling Repeater.

### Phase 3: Mob Hierarchy, Spawning & Elite Budget
* [ ] **Enemy HP & Speed Scaling:** Implement Final Surge HP formula ($HP(t,m) = HP_{base} \times H0_m \times (1 + s_m \cdot t) \times Surge(t)$), contact damage $(1+0.05t)$, chaser acceleration $+0.02\text{ m/s/min}$.
* [ ] **Global Mob Cap & Summon Exploits Guard:** Enforce global cap (150/180/220) and summon sub-cap 25. Summoned mobs (Vex, Slimelets, Golem zombies, Sculk Burst) strictly drop 0 EXP and 0 Emeralds.
* [ ] **Elite Budget Spawner:** Roll every 2s, first Elite guaranteed, max active cap (2/4/6), affixes (Swift +12% excluding fast mobs, Armored, Berserk, Regenerating 1%, Volatile), Lucky Block chest drop rate (20%/17%/15%, pity +10%, max 10/9/8 per run).
* [ ] **Spawning Events:** Implement Pincer event at 1:30 (radius 4.5m, 1.5s telegraph, 60° escape angle, 6/8 zombies), Swarm Rush (4:00 & 7:30), Siege (12:30).

### Phase 4: Boss Encounters & Economics Tuning
* [ ] **Mini-Bosses & Final Boss:**
  - 05:00 Nether Golem I (3,300 HP, Slam 3.0m, Lava Throw, chest grants +1 item level, no evolution).
  - 10:00 Nether Golem II Enraged (9,600 HP, Slam 3.6m, Enrage <50% HP with flame trail, chest guarantees evolution).
  - 15:00 The Warden (42,000 HP, Smash 60, Sonic Boom 45, Charge 55, Sculk Tendril Pull 25 when >5.5m away, Enrage timer 180s/150s/130s, wave spawn reduced by 60%).
* [ ] **Props & Economy Refinements:**
  - Breakable Lucky Block: 60 HP, 3 hits, drops 25% of next level EXP (min 5) + 2–5 emeralds.
  - TNT: 12.0m fixed radius, wipes regular mobs, 35% HP to Elites, immune Bosses, caps EXP at 50% next level & 15 emeralds, no lifesteal, min 45s gap.
  - Reroll costs: [2, 3, 5, 8, 12, 18] 💎 (Villager 50% discount).
  - Gem auto-merge when total active gems > 250.
* [ ] **Difficulty Selection UI:** Implement 3-card difficulty modal (Normal, Hard, Hardcore).
