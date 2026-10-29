# PIXEL SURVIVOR - SYSTEM PATTERNS & ARCHITECTURE

## 1. Directory Structure (`Assets/PixelSurvivor/`)
* `Animations/`: Animation clips and Animator Controllers for Player, Enemies, VFX.
* `Characters/`: Player prefabs, spritesheets, and base controllers.
* `Combat/` & `Scripts/`: C# scripts partitioned by domains (Player, Combat, Enemies, UI, Managers).
* `Enemies/`: Enemy prefabs, sprite variations (Zombies, Skeletons, Spiders, Bosses).
* `Items/`: Drop items (EXP Gems, Emeralds, Potions, Lucky Blocks).
* `Maps/`: Tilemaps, environment rule tiles, background textures.
* `Particles/VFX/Slashes/`: Melee slash sprite sheets (`vfx_slash_axe`, `vfx_slash_scythe`, `vfx_slash_sword`, `vfx_slash_daggers`).
* `Resources/Fonts/`: TextMeshPro Font Assets (`VCR_OSD_MONO SDF.asset`).
* `UI/`: UI prefabs, modular canvas HUDs, Level Up cards, Pause & Defeat modals, Settings modal.
* `Weapons/`: Weapon prefabs, weapon data ScriptableObjects, projectiles.

## 2. Graphic & Pixel Art Standards
* **Pixels Per Unit (PPU):** Exactly **32** for all in-game sprites (Characters, Enemies, Tiles, Slash VFX). Never use default 100 PPU.
* **Sprite Import Settings:**
  - `Texture Type`: Sprite (2D and UI)
  - `Sprite Mode`: Single or Multiple (grid slices)
  - `Filter Mode`: Point (no filter) - Crucial for sharp pixel art!
  - `Compression`: None
* **VFX Sorting & Alignment:**
  - Animated combat VFX must use `sortingOrder >= 25` with `Sprite-Unlit` material to glow in dark environments.
  - All slash VFX sprites are authored facing Right (0°), using `vfxAngleOffset = 0f` to align accurately with attack vectors.

## 3. UI Architecture & Design System (Locked: Dark Obsidian & Gemstone)
* **Modular Canvas Architecture:**
  - `Canvas`: Main menu Canvas (MainMenuPanel, CharacterSelectPanel, SettingsPanel).
  - `InGame_Canvas`: Gameplay UI Canvas holding:
    - `HUD_Root`: Top HUD (Timer, Biome, Level EXP bar, Player HP bar, Character Name, Level Text).
    - `Pause_Modal_Panel`: 8-stat overview, equipped weapons, item slots.
    - `GameOver_Modal_Panel`: Defeat modal with emerald summary and formulas.
    - `LevelUp_Modal_Panel`: Upgrade cards presentation modal (2-click selection).
* **Procedural UI Smoothing (`RoundedBorderImage.cs`):**
  - Component that renders rounded rectangles directly onto Canvas mesh with built-in sub-pixel edge feathering (1px alpha fringe).
  - Ensures clean anti-aliasing without requiring separate textures or shaders.
* **Typography (TextMeshPro Standard):**
  - All text must use `TextMeshProUGUI` (`TMP_Text`).
  - Font: `Assets/PixelSurvivor/Resources/Fonts/VCR_OSD_MONO SDF.asset`.
* **Palette & Colors:**
  - `BackgroundPanel`: `#0B0F19` (92% Alpha dark obsidian)
  - `CardSurface`: `#1E293B` (90% Alpha slate)
  - `BorderSubtle`: `#334155` | `BorderHighlight`: `#38BDF8` (Hover/Click 1) | `BorderSelected`: `#FACC15` (Click 2)
* **Semantic Gemstone Coding:**
  - 💎 **Emerald Green (`#10B981`):** Currency, stat bonuses (`+28% 🟢`), heal.
  - ⚡ **Diamond Cyan (`#38BDF8`):** EXP fill bar, Dash button, Level Up hover.
  - 👑 **Aurum Gold (`#FACC15`):** Level text, weapon card borders, CTA buttons.
  - 💖 **Crimson Red (`#EF4444`):** Player HP bar, critical damage text.
  - 🔮 **Void Amethyst (`#A855F7`):** Weapon evolution, ultimate tier.

## 4. Unity Interaction & MCP Conventions
* Always use Unity MCP for live Editor inspection and modifications when connected.
* When executing changes via MCP or Editor tools, responses **must provide clear, step-by-step instructions teaching the user how to do it manually** (Hierarchy -> Inspector -> Component -> Field values).
* Never edit raw `.unity` scene or `.prefab` YAML text directly.
* Preserve user custom layout tweaks 100% when configuring or automating scene elements.
