# PIXEL SURVIVOR - DECISION & CONSTRAINTS LOG

This document locks all fundamental decisions made across all previous development conversations. The Agent MUST NOT deviate from or contradict these decisions.

---

## 1. Core Architecture & Physics
* **[LOCKED] Movement Implementation:** Player uses 2D top-down continuous movement via `Rigidbody2D.linearVelocity` (never use `AddForce` or direct `transform.position` translate for combat physics). Gravity Scale must be set to `0`.
* **[LOCKED] Weapons Auto-Attack Lifecycle:** All weapons inherit from `WeaponBase.cs` and must auto-acquire `PlayerController` in `Awake()` via `GetComponentInParent<PlayerController>()` to ensure immediate firing upon scene load.
* **[LOCKED] Targeting & Swing Range:** Weapons must only trigger attack animations and slash VFX when at least one enemy is within attack range (`Vector2.Distance(player, enemy) <= attackRange`). Attacks must aim toward the closest enemy rather than arbitrary orientation.
* **[LOCKED] VFX Orientation Standard (Right 0°):** All melee slash and projectile sprites must be authored facing Right (0°, +X axis). `vfxAngleOffset = 0f` is the universal standard across weapons, eliminating arbitrary angle compensations.
* **[LOCKED] Enemy Directional Flip (Hysteresis):** Enemy horizontal sprite flipping must use a hysteresis deadzone (`flipThreshold = 0.25f`) and interval rate limiting (`MinFlipInterval = 0.12s`) to prevent erratic flipping when moving vertically along the Y-axis. `EnemyAI.cs` is the single source of truth for enemy flipping.
* **[LOCKED] Collision & Layering:**
  - Weapons hit detection must use `~LayerMask.GetMask("UI", "Ignore Raycast")` or dedicated `Enemy` layer.
  - Floating damage numbers must be spawned on a separate Sorting Layer or `sortingOrder >= 30` to stay above characters and sprites.

---

## 2. Visual & Gameplay Standards
* **[LOCKED] PPU = 32:** All sprites across the entire game (characters, tiles, props, enemies, VFX) must strictly maintain `Pixels Per Unit = 32`.
* **[LOCKED] Floating Damage Numbers:** Implemented via `DamageNumber.cs`:
  - Normal damage: Bright yellow (`#FACC15`)
  - Critical hits: Bright red (`#EF4444`)
  - Animation: Punch scale (1.4x to 1.0x), float upwards 1.4m, fade out and auto-destroy after 0.8s.
* **[LOCKED] Combat VFX:** Slash VFX (`vfx_slash_sword`, `axe`, `scythe`, `daggers`) scaled to `1.4x`, `sortingOrder = 25`, using `Sprite-Unlit` shader, aligned in the forward attack direction.
* **[LOCKED] UI System:** Tone 1 (Dark Obsidian & Gemstone) locked in `UI_Style_Guide_PixelSurvivor.md`.
* **[LOCKED] Text Rendering Standard (TextMeshPro):**
  - All text components in UI must use `TextMeshProUGUI` (`TMPro.TMP_Text`).
  - Standard font asset: `Assets/PixelSurvivor/Resources/Fonts/VCR_OSD_MONO SDF.asset`.
  - Legacy `UnityEngine.UI.Text` is strictly deprecated to prevent blurriness and pixelation.
* **[LOCKED] Procedural UI Smoothing (`RoundedBorderImage`):**
  - All procedural rounded cards, modals, tabs, and buttons must use `RoundedBorderImage` with `antiAliased = true`, `featherWidth = 1f`, and `cornerSegments >= 20` to guarantee smooth, non-jagged borders.
* **[LOCKED] Zero IMGUI Fallback:** Legacy `OnGUI()` rendering loops in `InGameHUD.cs` and `InGamePauseManager.cs` are permanently disabled. All UI rendering must route strictly through Unity Canvas hierarchy.
* **[LOCKED] Level Up Selection:** 2-click card selection:
  - Click 1: Highlight border Cyan `#38BDF8`, preview stat delta in green `➔ 128% (+28%) 🟢`, show confirmation hint.
  - Click 2: Confirm selection, grant upgrade, resume game.
* **[LOCKED] Language:** 100% in-game English (HUD, card titles, descriptions, menus).
* **[LOCKED] Emerald Economy & Zero-DB Architecture:** Emeralds are strictly an **in-run / in-match session currency**, collected during gameplay and used exclusively to **Reroll** upgrade cards in the Level Up modal. Emeralds reset upon run end/game over. There is **no persistent database (DB)** or cross-run meta-progression shop required, keeping architecture clean, local, and lightweight.
* **[LOCKED] Game Balancing & HP Scaling:** Regular mobs HP scaling is capped strictly in the hundreds (150 - 450 HP at minute 15:00) using controlled linear scaling `HP(t) = HP_base * (1 + 0.25 * t)`, preventing damage sponge mobs. Late game player damage peaks at 150 - 350 base hit with 400 - 750 critical damage numbers for satisfying horde clear without numerical inflation. Supported across 3 difficulty tiers (Normal, Hard, Hardcore).
* **[LOCKED] EXP Bar & Pickup Balancing:** Top EXP Bar stretches 100% across the screen using anchor-based RectTransform fill (`anchorMax.x = expPct`), starting with a dark obsidian background (`#0B0F19`) and filling with Diamond Cyan (`#38BDF8`). Base magnet pickup radius is tuned to `1.7m` (reduced by 1/3, `2.38m` for Villager). Drop orbs scaled to `1.25x - 1.45x` for clear visibility. Damage numbers punch-scaled to `1.65x` with enlarged typography.
* **[LOCKED] Lightning Staff Archetype:** Lightning Staff operates as a directional piercing Laser Beam weapon (`LaserBeamWeapon.cs` using `Physics2D.CircleCastAll`), firing a concentrated electrical laser beam cutting through all mobs in a straight line, rather than random chain lightning.

---

## 3. Tooling & Workspace Rules
* **[LOCKED] Manual Guidance Output:** When Unity MCP is executed, response text must teach the user how to configure the exact same setup manually in Unity Editor.
* **[LOCKED] Subfolder File Export:** All generated artifacts/reports must be exported to:
  `C:\Users\PC\OneDrive\Desktop\AntigravityAgent\PixelSurvivor\` (or conversation topic subfolder).
* **[LOCKED] User Customization Preservation:** Whenever automating UI updates or migrations, user-defined positions, anchors, sizes, and layout tweaks must be 100% preserved.
