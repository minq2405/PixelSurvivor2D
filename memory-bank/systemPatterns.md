# PIXEL SURVIVOR - SYSTEM PATTERNS & ARCHITECTURE (v2.1 SPEC ALIGNED)

## 1. Directory Structure (`Assets/PixelSurvivor/`)
* `Animations/`: Animation clips and Animator Controllers for Player, 12 Enemies, VFX, Bosses.
* `Characters/`: Player prefabs, spritesheets, and core controllers (`PlayerController.cs`, `PlayerHealth.cs`, `PlayerCollector.cs`, `PlayerStats.cs`).
* `Combat/` & `Scripts/`: Domain-driven C# architecture (Player, Combat, Enemies, Bosses, UI, Managers).
* `Enemies/`:
  - 12 Enemy prefabs inheriting from `EnemyAI.cs`.
  - `EliteEnemyAffixController.cs`: Elite budget spawning, affix rules, and Amethyst foot ring attachment.
  - `Bosses/`: `NetherGolemBoss.cs` (Mini-boss at 5m/10m), `WardenBoss.cs` (Final boss at 15m).
* `Items/`:
  - `Drops/`: Collectibles (EXP Gems, Emeralds, Steak, Magnet, TNT, Breakable Golden Lucky Block).
  - `Passives/`: 9 Passive item ScriptableObjects/Controllers with Lv 1 to Lv 5 progression.
  - `Prefabs/`: In-world drop prefabs with auto-pooling.
* `Managers/`:
  - `TimeScaleManager.cs`: Exclusive controller of `Time.timeScale` via token-based pause lock counter.
  - `WaveSpawner.cs`: 15-minute timeline, global cap handling, directional spawning, event controller (Pincer, Swarm, Siege).
  - `LevelUpManager.cs`: 2-click card modal, dynamic pool (4 weapon / 4 passive caps), Evolution Pity, Swap card, Backup card pool, reroll economy.
  - `ExperienceManager.cs`: Manages level curve `Round(20 * Lv^1.20)` and in-run session Emeralds.
* `Particles/VFX/`: Melee slash sheets (PPU 32, facing Right 0°), `vfx_elite_aura_ring.png` (pivot `{x: 0.5, y: 0.2857}`, sortingOrder 4).
* `UI/`: Modular canvas system, TextMeshPro font (`VCR_OSD_MONO SDF.asset`), `RoundedBorderImage.cs` with 1px alpha fringe feathering.

---

## 2. Core Architectural Patterns

### A. Centralized Stat Engine (`PlayerStats.cs`)
All multipliers and caps funnel through a single authority:
* **Stat Caps:**
  - Armor $\le 35\%$.
  - CDR $\le 30\%$ (hard minimum 0.25s for Twin Daggers).
  - Move Speed $\le +40\%$.
  - Crit Chance $\le 60\%$.
  - Bonus Damage $\le +60\%$.
* **Contact Damage & I-Frame Processing:**
  - Global contact I-frame timer = 0.35s.
  - Surround calculation: $\text{DMG}_{\text{hit}} = \text{DMG}_{\text{contact}}(t,m) \times (1 + 0.25 \times \min(N_{\text{extra}}, 4))$.
  - Independent 0.25s locks for non-contact threats (explosions, poison puddles, projectiles, boss skills).
  - Regen pause: Interrupted for 1.0s after receiving damage.

### B. TimeScale & State Isolation (`TimeScaleManager.cs`)
* Prevents rogue scripts from overwriting `Time.timeScale`.
* Components request a pause lock via `AcquirePause(token)` and release via `ReleasePause(token)`.
* `Time.timeScale` is strictly set to `0f` when locks > 0, and `1f` when locks == 0.
* No `Awake()` or `OnEnable()` in any other manager touches `Time.timeScale`.

### C. Combat & Upgrades Pattern (Vampire Survivors Style)
* **Weapons Budget $B(L)$:**
  - Standardized DPS budget per tier: Lv 1 (22 DPS), Lv 4 (44 DPS), Lv 8 (88 DPS).
  - Daggers & Crossbow strictly prioritize `Boss > Elite > Closest Mob`.
* **Evolution Gating (v2.1):**
  - Gated to $\ge 09:00$. Max 2 evolved weapons per run.
  - Golem I chest (5:00) grants +1 level to up to 3 owned items without evolving.
  - Golem II chest (10:00) guarantees evolution if requirements are fulfilled.
* **Passive Retention & Pity:**
  - Lv 5 Passives permanently retain their equipped slot.
  - Evolution Pity active from 08:00 (weight x3, guarantee within $\le 3$ rolls, or offers a **Swap** card if slots full).
  - Backup pool when all slots are maxed: Hearty Meal (30% heal), Emerald Pouch (+10 💎), Scholar's Tome (25% EXP 60s), Bounty Mark (guarantee chest, max 3/run).

### D. Mob Hierarchy & Spawning Pipeline
* **Global Cap Enforcement:**
  - Mob budget cap is strictly global: Normal 150, Hard 180, Hardcore 220.
  - Regular wave mobs, Elites, and summoned mobs (Evoker Vex, Slimelets, Golem summons, Sculk Burst) share this single budget.
  - Summoned mobs have an additional sub-cap of 25 entities and **strictly drop 0 EXP and 0 Emeralds**.
* **Mob Speed Archetypes & Scaling:**
  - Chasers (Zombie, Husk, ZombieV2, HuskShovel) accelerate at $+0.02\text{ m/s/min}$.
  - Flankers/Fliers (Spider 4.3 m/s, Vex 4.5 m/s, Phantom 4.2 m/s) maintain low HP ($\le 0.6\times$ avg).
* **Physics & Performance Optimization:**
  - Enemy–Enemy layer collision is disabled in Unity Physics2D settings.
  - Spatial Grid separation runs at 10 Hz for smooth horde flow without physics engine locks.
  - `Physics2D.OverlapCircleNonAlloc` with preallocated 64-element buffer for targeting and AoE checks.
  - Auto-merge collectibles when total gems exceed 250 on the battlefield.
