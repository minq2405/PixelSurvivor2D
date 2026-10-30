# PIXEL SURVIVOR - DECISION & CONSTRAINTS LOG

This document locks all fundamental decisions made across all previous development conversations. The Agent MUST NOT deviate from or contradict these decisions.

---

## 1. Core Architecture & Physics
* **[LOCKED] Movement Implementation:** Player uses 2D top-down continuous movement via `Rigidbody2D.linearVelocity` (never use `AddForce` or direct `transform.position` translate for combat physics). Gravity Scale must be set to `0`. Dual-input fallback ensures WASD, Arrow keys, and Gamepad function regardless of project input manager configuration.
* **[LOCKED] Weapons Auto-Attack Lifecycle:** All weapons inherit from `WeaponBase.cs` and must auto-acquire `PlayerController` in `Awake()` via `GetComponentInParent<PlayerController>()` to ensure immediate firing upon scene load.
* **[LOCKED] Targeting & Swing Range:** Weapons must only trigger attack animations and slash VFX when at least one damageable target is within attack range (`Vector2.Distance(player, target) <= attackRange`). Attacks must aim toward the closest `IDamageable` rather than arbitrary orientation.
* **[LOCKED] VFX Orientation Standard (Right 0°):** All melee slash and projectile sprites must be authored facing Right (0°, +X axis). `vfxAngleOffset = 0f` is the universal standard across weapons, eliminating arbitrary angle compensations.
* **[LOCKED] Enemy Directional Flip (Hysteresis):** Enemy horizontal sprite flipping must use a hysteresis deadzone (`flipThreshold = 0.25f`) and interval rate limiting (`MinFlipInterval = 0.12s`) to prevent erratic flipping when moving vertically along the Y-axis. `EnemyAI.cs` is the single source of truth for enemy flipping.
* **[LOCKED] Collision & Layering:**
  - Weapons hit detection must query `~LayerMask.GetMask("UI", "Ignore Raycast")` or dedicated `Enemy` layer.
  - Floating damage numbers must be spawned on a separate Sorting Layer or `sortingOrder >= 30` to stay above characters and sprites.

---

## 2. Visual & Gameplay Standards
* **[LOCKED] PPU = 32:** All sprites across the entire game (characters, tiles, props, enemies, VFX, aura rings) must strictly maintain `Pixels Per Unit = 32`.
* **[LOCKED] Floating Damage Numbers:** Implemented via `DamageNumber.cs`:
  - Normal damage: Bright yellow (`#FACC15`)
  - Critical hits: Bright red (`#EF4444`)
  - Animation: Punch scale (1.4x to 1.0x), float upwards 1.4m, fade out and auto-destroy after 0.8s.
  - Scene pollution safety: Guarded by `if (!Application.isPlaying) return;` so edit-mode code never spawns stray objects into the scene.
* **[LOCKED] Combat VFX:** Slash VFX (`vfx_slash_sword`, `axe`, `scythe`, `daggers`) scaled to `1.4x`, `sortingOrder = 25`, using `Sprite-Unlit` shader, aligned in the forward attack direction.
* **[LOCKED] UI System:** Tone 1 (Dark Obsidian & Gemstone) locked in `UI_Style_Guide_PixelSurvivor.md`.
* **[LOCKED] Text Rendering Standard (TextMeshPro):**
  - All text components in UI must use `TextMeshProUGUI` (`TMPro.TMP_Text`).
  - Standard font asset: `Assets/PixelSurvivor/Resources/Fonts/VCR_OSD_MONO SDF.asset`.
  - Legacy `UnityEngine.UI.Text` is strictly deprecated to prevent blurriness and pixelation.
* **[LOCKED] Level Up Free Reroll & Emerald Economy Balancing:** The first reroll of EVERY level up is strictly FREE (`hasUsedFreeRerollThisLevel == false`, 0 💎, interactable). Subsequent rolls within the session start at a modest 1 💎 (+1 💎 per paid roll, capped at 5 💎, Villager 50% discount). Emerald drops are rebalanced: regular mobs drop at 5% base (14% Villager, 1-2 💎), specialist mobs drop at 15% base (28% Villager, 2-3 💎), and Elite mobs guarantee a burst of 3, 5, or 10 💎 based on difficulty.
* **[LOCKED] Procedural UI Smoothing (`RoundedBorderImage`):**
  - All procedural rounded cards, modals, tabs, and buttons must use `RoundedBorderImage` with `antiAliased = true`, `featherWidth = 1f`, and `cornerSegments >= 20` to guarantee smooth, non-jagged borders.
* **[LOCKED] Zero IMGUI Fallback:** Legacy `OnGUI()` rendering loops in `InGameHUD.cs` and `InGamePauseManager.cs` are permanently disabled. All UI rendering must route strictly through Unity Canvas hierarchy.
* **[LOCKED] Level Up Selection:** 2-click card selection:
  - Click 1: Highlight border Cyan `#38BDF8`, preview stat delta in green `➔ 128% (+28%) 🟢`, show confirmation hint.
  - Click 2: Confirm selection, grant upgrade, resume game.
* **[LOCKED] Language:** 100% in-game English (HUD, card titles, descriptions, menus).
* **[LOCKED] Emerald Economy & Zero-DB Architecture:** Emeralds are strictly an **in-run / in-match session currency**, collected during gameplay and used exclusively to **Reroll** upgrade cards in the Level Up modal. Emeralds reset upon run end/game over. There is **no persistent database (DB)** or cross-run meta-progression shop required, keeping architecture clean, local, and lightweight.
* **[LOCKED] Game Balancing & HP Scaling:** Regular mobs HP scaling is capped strictly in the hundreds (150 - 450 HP at minute 15:00) using controlled linear scaling `HP(t) = HP_base * (1 + 0.25 * t)`, preventing damage sponge mobs. Late game player damage peaks at 150 - 350 base hit with 400 - 750 critical damage numbers for satisfying horde clear without numerical inflation. Supported across 3 difficulty tiers (Normal, Hard, Hardcore).
* **[LOCKED] EXP Bar & Pickup Balancing:** Top EXP Bar stretches 100% across the screen using anchor-based RectTransform fill (`anchorMax.x = expPct`), starting with a dark obsidian background (`#0B0F19`) and filling with Diamond Cyan (`#38BDF8`). Base magnet pickup radius is tuned to `1.7m` (reduced by 1/3, `2.38m` for Villager). Drop orbs scaled to `1.25x - 1.45x` for clear visibility.
* **[LOCKED] Lightning Staff Archetype:** Lightning Staff operates as a directional piercing Laser Beam weapon (`LaserBeamWeapon.cs` using `Physics2D.CircleCastAll`), firing a concentrated electrical laser beam cutting through all mobs in a straight line, rather than random chain lightning.

---

## 3. Tooling & Workspace Rules
* **[LOCKED] Manual Guidance Output:** When Unity MCP is executed, response text must teach the user how to configure the exact same setup manually in Unity Editor.
* **[LOCKED] Subfolder File Export:** All generated artifacts/reports must be exported to:
  `C:\Users\PC\OneDrive\Desktop\AntigravityAgent\PixelSurvivor\` (or conversation topic subfolder).
* **[LOCKED] User Customization Preservation:** Whenever automating UI updates or migrations, user-defined positions, anchors, sizes, and layout tweaks must be 100% preserved.

---

## 4. Rebalanced Enemy System & Elite Archetype
* **[LOCKED] Distinct Enemy Speed Archetypes & Gentle Late-Game Scaling:**
  - Standard base speed is calculated per mob archetype rather than flat 4.0 m/s:
    - *Pursuers (3.5 - 3.6 m/s):* Zombie (3.5), Husk (3.5), Zombie V2 (3.6), Husk Shovel (3.6). Zombie/Husk at 3.5 m/s allows early-game kiting against Steve (4.0 m/s).
    - *Fast Flankers & Fliers (4.4 - 4.8 m/s):* Spider (4.5), Vex (4.8), Phantom (4.4). Spiders and Vexes are faster than Steve, forcing player engagement.
    - *Tactical & Heavy (3.2 - 3.4 m/s):* Creeper (3.4), Ice Creeper (3.4), Slime (3.2), Witch (3.3), Evoker (3.2).
  - *Late-Game Speed Scaling:* Speed increases gently at $+0.02 - 0.03\text{ m/s}$ per minute. By minute 15:00, Zombie reaches 3.95 m/s (approaching Steve's 4.0 m/s), creating tense late-game pressure without absurd sprint speeds.
* **[LOCKED] Single Unified Elite Enemy Type:** Elite enemies are strictly ONE unified archetype: Any Enemy Mob + `vfx_elite_aura_ring` (12-frame animated ring beneath feet, sortingOrder = 4, PPU = 32) + Small Amethyst Purple Health Bar (`#A855F7`) + Stat Buffs (HP x2.5, Damage x1.25, Scale 1.2x) + Elite Drops (150 EXP Yellow Gem + Special Drops). Original mob sprite coloration is 100% preserved (no arbitrary yellow tinting). Complex multi-affix mechanics are deprecated.
* **[LOCKED] Elite Aura Ground Feet Alignment:** The sprite pivot of `vfx_elite_aura_ring` is set to $\{X: 0.5, Y: 0.2857\}$ (pixel 64, 32, the exact center of the ground oval). `EliteEnemyAffixController.cs` anchors dynamically to the mob's lowest foot boundary (`mobSr.bounds.min.y`), guaranteeing exact $0.0000$ offset on all 12 mob types.

---

## 5. Items, Drops & Progression Decisions
* **[LOCKED] Breakable Golden Lucky Block Chest:** Golden Lucky Block is an in-world prop implementing `IDamageable`, requiring 3 hits (60 HP) to break open. On hit: plays punch bounce and floating text `CRACK! (1/3)`. On break: spawns guaranteed 70 EXP (1 Yellow, 2 Green) + 2-5 Emeralds + bonus item (Steak, Magnet, TNT, or 10% Jackpot). Trigger collider prevents player trapping. It is not collectable on touch and cannot be magnetized.
* **[LOCKED] Instant Screen Nuke TNT:** Picking up a TNT drop immediately eliminates all active on-screen enemies within camera viewport (`Camera.main.WorldToViewportPoint`) with 99,999 lethal DMG. Zero delay, no explosion animation. Off-screen mobs are unaffected.
* **[LOCKED] Passive Items Progression (Option 1):**
  - Strict 4-Passive Slot Limit (`MaxPassives = 4`).
  - Passives have 5 Levels (Lv 1 to Lv 5 MAX).
  - Level 1 unlocks the passive item; levels 2-5 scale stats progressively.
  - Upon reaching Lv 5 (MAX), the item is permanently removed from the Level Up card pool.
  - Evolution Synergy: A Lv 8 Weapon paired with its designated Lv 5 Passive unlocks Legendary Weapon Evolution.
  - 9 Passives supported via prepared sprites in `Assets/PixelSurvivor/Items/Passives/`: Book of Damage, Feather Speed, Heart Max HP, Shield Armor, Clock Cooldown, Ender Eye Magnet, Potion Regen, Emerald EXP, Totem Revive.
