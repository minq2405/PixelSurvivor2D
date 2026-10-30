# PIXEL SURVIVOR - SYSTEM PATTERNS & ARCHITECTURE

## 1. Directory Structure (`Assets/PixelSurvivor/`)
* `Animations/`: Animation clips and Animator Controllers for Player, Enemies, VFX.
* `Characters/`: Player prefabs, spritesheets, and base controllers (`PlayerController.cs`, `PlayerHealth.cs`, `PlayerCollector.cs`).
* `Combat/` & `Scripts/`: C# scripts partitioned by domains (Player, Combat, Enemies, UI, Managers).
* `Enemies/`: Enemy prefabs, sprite variations (12 distinct mob archetypes, `EnemyAI.cs`, `EliteEnemyAffixController.cs`).
* `Items/`: 
  - `Drops/`: Collectibles (EXP Gems, Emeralds, Steak, Magnet, TNT, Breakable Golden Lucky Block).
  - `Passives/`: 9 Passive item sprites (`item_book_damage`, `item_feather_speed`, `item_heart_max_hp`, `item_shield_armor`, `item_clock_cooldown`, `item_ender_eye_pickup`, `item_potion_regen`, `item_emerald_exp`, `item_totem_revive`).
  - `Prefabs/`: Prefabs for all in-world drops and breakable chests.
* `Maps/`: Infinite map generator (`InfiniteMapGenerator.cs`), chunk pooling, tilemaps.
* `Particles/VFX/`: 
  - `Slashes/`: Melee slash sprite sheets (`vfx_slash_axe`, `vfx_slash_scythe`, `vfx_slash_sword`, `vfx_slash_daggers`).
  - `VFX/`: `vfx_elite_aura_ring.png` (12 frames, PPU 32, pivot `{x: 0.5, y: 0.2857}`).
* `Resources/Fonts/`: TextMeshPro Font Assets (`VCR_OSD_MONO SDF.asset`).
* `UI/`: UI prefabs, modular canvas HUDs, Level Up cards, Pause & Defeat modals, Settings modal, `RoundedBorderImage.cs`.
* `Weapons/`: 6 active weapon prefabs, `WeaponBase.cs`, `WeaponController.cs`.

## 2. Graphic & Pixel Art Standards
* **Pixels Per Unit (PPU):** Exactly **32** for all in-game sprites (Characters, Enemies, Tiles, Slash VFX, Aura Rings). Never use default 100 PPU.
* **Sprite Import Settings:**
  - `Texture Type`: Sprite (2D and UI)
  - `Sprite Mode`: Single or Multiple (grid slices)
  - `Filter Mode`: Point (no filter) - Crucial for sharp pixel art!
  - `Compression`: None
* **VFX Sorting & Alignment:**
  - Animated combat VFX must use `sortingOrder >= 25` with `Sprite-Unlit` material to glow in dark environments.
  - Elite Aura Ring: `sortingOrder = 4` (under mob feet at order 5). Pivot centered at ground ellipse $(64, 32) \rightarrow (0.5, 0.2857)$.
  - All slash VFX sprites are authored facing Right (0°), using `vfxAngleOffset = 0f` to align accurately with attack vectors.

## 3. Combat & Upgrades Architecture (Vampire Survivors Style)
* **Slot Limits (Locked Rule):**
  - **4 Active Weapon Slots:** `WeaponController.MaxWeapons = 4`.
  - **4 Passive Item Slots:** `LevelUpManager.MaxPassives = 4`.
* **Progression Tiers:**
  - **Weapons:** Upgrade from **Lv 1 to Lv 8 (MAX)**.
  - **Passives:** Upgrade from **Lv 1 to Lv 5 (MAX)**.
  - Once any Weapon or Passive reaches MAX level, it is permanently removed from the Level Up pool.
* **Weapon Evolution (Synergy):**
  - Pairing a Lv 8 Weapon with its paired Lv 5 Passive triggers Legendary Weapon Evolution in subsequent level ups or Lucky Chests.
* **Weapons Auto-Targeting `IDamageable`:**
  - All weapons query `hits[i].GetComponent<IDamageable>()`. Any damageable target that is not `PlayerHealth` (including all 12 enemy types and breakable Lucky Blocks) is automatically targeted and struck.

## 4. Enemy & Elite Architecture
* **12 Distinct Enemy Archetypes:**
  - All 12 mobs inherit from `EnemyAI.cs`.
  - **Speed Archetypes:**
    - *Pursuers (3.5 - 3.6 m/s):* Zombie (3.5), Husk (3.5), Zombie V2 (3.6), Husk Shovel (3.6).
    - *Fast Flankers & Fliers (4.4 - 4.8 m/s):* Spider (4.5), Vex (4.8), Phantom (4.4).
    - *Tactical & Heavy (3.2 - 3.4 m/s):* Creeper (3.4), Ice Creeper (3.4), Slime (3.2), Witch (3.3), Evoker (3.2).
  - **Gentle Linear Late-Game Scaling:** Speed scales smoothly at $+0.02 - 0.03\text{ m/s}$ per minute.
* **Single Unified Elite System (`EliteEnemyAffixController.cs`):**
  - Any mob can spawn as Elite based on difficulty rate.
  - Retains 100% original mob sprite tint (NO yellow body tint).
  - Stat buff: HP $\times 2.5$, Damage $\times 1.25$, Scale $1.20\times$, Knockback resistance $+0.25f$.
  - 12-frame animated amethyst ring (`vfx_elite_aura_ring`) at feet with exact $0.0000$ offset.
  - Small overhead purple health bar (`#A855F7`).
  - High-tier guaranteed drops (150 EXP Gem + Breakable Lucky Chest / Emeralds).

## 5. UI Architecture & Design System (Dark Obsidian & Gemstone)
* **Modular Canvas Architecture:**
  - `InGame_Canvas`:
    - `HUD_Root`: Top HUD (Timer, Biome, Level EXP bar, Player HP bar, Counters Row `[💀] <kills>` and `[💎] <emeralds>`, Top-Right Anti-Aliased Pause Button).
    - `LevelUp_Modal_Panel`: 2-click upgrade cards presentation modal with free 1st roll & paid emerald rolls.
    - `Pause_Modal_Panel`: 8-stat overview, equipped weapons, equipped passives.
    - `GameOver_Modal_Panel`: Defeat modal with run statistics.
* **Procedural Smoothing (`RoundedBorderImage.cs`):**
  - Built-in sub-pixel edge feathering (1px alpha fringe) for anti-aliasing without external shaders.
* **Typography & Semantic Colors:**
  - Font: `VCR_OSD_MONO SDF.asset` via `TextMeshProUGUI`.
  - 💎 Emerald Green (`#10B981`): Currency, Heal, Stats.
  - ⚡ Diamond Cyan (`#38BDF8`): EXP Bar, Level Up hover.
  - 👑 Aurum Gold (`#FACC15`): Level text, Lucky Chests, Cards.
  - 💖 Crimson Red (`#EF4444`): Player HP, Screen Nuke, Crits.
  - 🔮 Void Amethyst (`#A855F7`): Elite aura, weapon evolution.
