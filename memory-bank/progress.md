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

### Phase 1: Core Engine & Architecture Hardening (COMPLETED)
* [x] **TimeScale Architecture (`TimeScaleManager.cs`):** Centralized reference-counted pause lock tokens (`PauseToken` pattern via `AcquirePause` / `ReleasePause`); removed all rogue `Time.timeScale` calls across `PlayerController`, `WaveSpawner`, `PlayerHealth`, `InGamePauseManager`, `LevelUpManager`, and `CombatDebugOverlay`.
* [x] **Player Centralized Stats (`PlayerStats.cs`):**
  - Enforced v2.1 hard caps: Armor $\le 35\%$, CDR $\le 30\%$ (Twin Daggers floor $0.25s$), Move Speed $\le +40\%$, Crit Chance $\le 60\%$, Bonus DMG $\le +60\%$.
  - Character base profiles integrated (Steve 120 HP / 0.5 regen / 4.0 spd / 10% armor / 5% crit; Alex 85 HP / 4.8 spd / 12% crit; Villager 100 HP / 0.2 regen / 4.2 spd / 8% crit / 2.2m magnet).
  - Global contact I-frame ($0.35s$) + surround stacking ($+25\%/\text{touching mob up to } +100\%$).
  - Non-contact source locks ($0.25s$ per instance).
  - 1.0s regen pause on any damage taken.
* [x] **EXP Curve Update (`ExperienceManager.cs`):** Implemented $\text{EXP} = \text{Round}(20 \times Lv^{1.20})$ and mode multipliers (Normal 1.00 / Hard 1.10 / Hardcore 1.25). Target 45–46 picks at 15:00.

### Phase 2: Weapons, Passives & Evolution Balancing (COMPLETED)
* [x] **Weapon DPS Budget Rebalancing:** Adjusted all 6 weapons to v2.1 $B(L)$ formula (Broadsword Lv1 20->Lv8 54, CD 0.9s, Burn 4 dmg/s; Battle Axe Lv1 34->Lv8 86, CD 1.35s, Stagger 0.4s; Twin Daggers Lv1 12x2->Lv8 28x2, CD 0.42s, Crit +8%; Dark Scythe Lv1 24->Lv8 65, CD 1.05s, 1 HP/kill capped at 5 HP/s; Laser Lv8 92, CD 1.10s; Crossbow Lv8 3x17.5, CD 0.60s, pierce 3 with -25%/pierce mathematical falloff).
* [x] **Weapon Targeting Priorities:** Enforced `Boss > Elite > Closest Mob` on Twin Daggers and Crossbow.
* [x] **9 Passive Items System (Lv 1 to Lv 5):**
  - Enforced 4-passive slot limit via `PassiveController.cs`.
  - Implemented Lv 1 to Lv 5 stat scaling across all 9 passives.
  - Ensured Lv 5 passives retain their slot while exiting the pool.
* [x] **Evolution Gating & Pity System:**
  - Enforced evolution unlock at $\ge 09:00$ and max 2 evolutions per run.
  - Evolution Pity at 08:00 (weight x3, guarantee within $\le 3$ rolls, or offer Swap card if 4 slots filled).
  - Backup card pool implemented: Hearty Meal (+40% HP), Emerald Pouch (+25 💎), Scholar's Tome (+150 EXP), Bounty Mark (+10% Boss/Elite dmg).
* [x] **6 Evolved Weapons Implementation:** Solar Flare Greatsword, Executioner Greataxe, Shadow Phantom Daggers, Reaper's Harvest, Oblivion Death Ray (0.8m wide, 90°/s, slows to 30°/s on boss), Gatling Repeater (5 bolts, pierce 4, 1.0m AoE explosion). All verified in Unity Play Mode.

### Phase 3: Mob Hierarchy, Spawning & Elite Budget (COMPLETED)
* [x] **Enemy HP & Speed Scaling:** Implement Final Surge HP formula ($HP(t,m) = HP_{base} \times H0_m \times (1 + s_m \cdot t) \times Surge(t)$), contact damage $(1+0.05t)$, chaser acceleration $+0.02\text{ m/s/min}$. Verified live in Play Mode ($1.00\times \to 3.40\times \to 6.21\times$).
* [x] **Global Mob Cap & Summon Exploits Guard:** Enforce global cap (150/180/220) and summon sub-cap 25. Summoned mobs (Vex, Slimelets, Golem zombies, Sculk Burst) strictly drop 0 EXP and 0 Emeralds, with lifesteal exploit prevented.
* [x] **Spatial Separation & 10 Hz Boids:** Enemy-Enemy collision disabled, soft repulsion handled by `SpatialSeparationSystem` with 1.5m spatial hash grid at 10 Hz.
* [x] **Elite Budget Spawner:** Roll every 2s, first Elite guaranteed at $\ge 120s$, max active cap (2/4/6), 5 affixes (Swift +12% excluding fast mobs, Armored, Berserk, Regenerating 1%, Volatile), Amethyst Aura Ring, sleek overhead UI, Lucky Block chest drop rate (20%/17%/15%, pity +10%, max 10/9/8 per run).
* [x] **Spawning Events:** Implement Pincer event at 1:30 (radius 4.5m, 1.5s telegraph, 60° escape angle, 6/8 zombies), Swarm Rush (4:00 & 7:30), Siege (12:30).

### Phase 4: Boss Encounters & Economics Tuning (COMPLETED)
* [x] **Mini-Bosses & Final Boss:**
  - 05:00 Nether Golem I (3,300 HP, Slam 3.0m, Lava Throw, chest grants +1 item level, no evolution). Verified live in Play Mode.
  - 10:00 Nether Golem II Enraged (9,600 HP, Slam 3.6m, Enrage <50% HP with flame trail, chest guarantees evolution). Verified live in Play Mode.
  - 15:00 The Warden (42,000 HP, Smash 60, Sonic Boom 45, Charge 55, Sculk Tendril Pull 25 when >5.5m away, Enrage timer 180s/150s/130s, wave spawn reduced by 60%). Verified live in Play Mode.
* [x] **Props & Economy Refinements:**
  - Breakable Lucky Block: 60 HP, 3 hits, drops 25% of next level EXP (min 5) + 2–5 emeralds. Verified.
  - TNT: 12.0m fixed radius, wipes regular mobs, 35% HP to Elites, immune Bosses, caps EXP at 50% next level & 15 emeralds, no lifesteal, min 45s gap. Verified.
  - Reroll costs: [2, 3, 5, 8, 12, 18] 💎 (Villager 50% discount). Verified.
  - Gem auto-merge when total active gems > 250. Verified.
* [x] **Difficulty Selection UI:** Implement 3-card difficulty modal (Normal, Hard, Hardcore) integrated with MainMenuManager. Verified.

### Phase 5: Boss Arena Lockdown & Combat Polish (COMPLETED)
* [x] **Boss Arena Lockdown System (`BossArenaManager.cs`, `CameraFollow2D.cs`, `InGameHUD.cs`):**
  - 5-second pulsing countdown timer warning (Yellow for Mini-Boss, Red for Boss).
  - Dynamic camera punch zoom and boundary clamping (locks player inside camera bounds during boss fights).
  - Regular wave spawning paused and non-boss mobs cleared during arena encounter.
* [x] **Boss Sprite Meta Slicing & Clones Bug Fix:**
  - Fixed sprite meta frame slicing on `mutant_zombie_walk.png.meta`, `warden_walk.png.meta`, `warden_attack.png.meta`, `corrupted_iron_golem_attack.png.meta`.
  - Added programmatic size guards in boss scripts.
* [x] **Pillager Captain Weapon Rework:**
  - Removed crossbow icon; replaced with 3 Red Telegraph Lines -> 3 Arcane Energy Orbs synchronized to 12m range with obstacle-phasing `Physics2D.OverlapCircleAll`.
* [x] **Unified Crimson Red Visuals & Snappy Windups:**
  - Unified all 9 boss/mini-boss overhead health bars to Crimson Red (`#EA3333` / `Color(0.92f, 0.20f, 0.20f, 1f)`).
  - Unified all telegraph lines, beams, and shockwave indicators to Red (`#EF4444`).
  - Reduced telegraph windup durations by ~30-35% across all bosses for fast-paced combat.
* [x] **Continuous Contact Damage & Obstacle Phasing:**
  - Continuous proximity contact damage check in `Update()` on all 9 bosses; removed `!isBusy` gating from physical collisions.
  - Projectiles and lunges/charges phase through obstacles (`col.isTrigger = true`).
* [x] **Player-Aimed Leap-Slam Ground Shockwave Mechanics (Mutant Zombie Format):**
  - Unified `NetherGolemBoss`, `CorruptedIronGolemBoss`, `PiglinBruteBoss`, `WitherKingBoss`, `RavagerBoss`, and `MutantZombieBoss`:
    - Windup in place.
    - Player receives Red Oval Telegraph centered on current position.
    - Boss leaps/lunges dynamically through the air with an arc towards the player, phasing through obstacles.
    - Impacts landing zone, triggers `vfx_dia_chan` crimson shockwave and damage.

* [x] **HUD Feature - 2-Row Equipment Slots Bar:** Implemented in-game equipment HUD slots arranged in 2 horizontal rows (Top row: 4 Weapon slots; Bottom row: 4 Passive item slots with no border), styled with translucent black rounded squares (`CornerRadius = 6`, `Alpha = 0.45`, `Border = 0`) positioned Top-Down Center below the Timer/Biome container, displaying real-time weapon/passive icons and level/MAX badges.
* [x] **Bug Fix - 15:00 Warden Encounter & Victory End-Game:** Fixed bug where match froze at 15:00 without spawning the final boss. Removed premature early return in `WaveSpawner.Update()`, wired 5s red warning at 14:55, properly spawned The Warden at 15:00 with Boss Arena Lockdown, and wired `InGamePauseManager.Instance.TriggerVictory()` upon defeating The Warden to display the victory screen and grant +100 emeralds.
* [x] **Bug Fix - Pre-Match Runtime Timer Counting:** Fixed issue where `WaveSpawner` auto-started the match timer on frame 1 while still on the Main Menu. Made `WaveSpawner.StartRun()` strictly start when the player confirms character and difficulty in `MainMenuManager`, and added `StopRun()` to reset timer to `00:00` whenever the Main Menu is active.

---

### Phase 6: UI/UX Modernization, QoL & Boss Randomization System (COMPLETED)
* [x] **Difficulty Selection Fullscreen Flow:**
  - Redesigned into full-screen responsive view (`DifficultySelectionUI.cs`, `DifficultyCardItem.cs`).
  - Single-click card selection; eliminated tinted border overlay glitch.
  - Repaired missing script references on Card GameObjects in both `MainGame.unity` and `CombatSandbox.unity`.
* [x] **Character Selection Walk Animations & Dynamic Descriptions:**
  - Integrated walk animation on hover/selection via `CharacterPedestalUI.cs`.
  - Dynamically bound character descriptions to selected hero (Steve, Alex, Villager) replacing hardcoded strings.
* [x] **Level-Up Card Badges & Reroll Clean-Up:**
  - Added modern status badges to the right of item titles: "NEW" (emerald green `#10B981`), "Lv. X" (emerald green `#10B981`), and "MAX" (neon purple `#A855F7`).
  - Dynamically auto-aligned based on item name width.
  - Removed hotkey letter "R" from the "Reroll (FREE)" button.
* [x] **Pause Menu Alignment & Stats Binding Fix:**
  - 3-column table alignment for weapons (`Your Weapons | Damage | Per second`).
  - Removed intrusive `Badge_Melee`.
  - Widened passive item row width to 556px to eliminate awkward text wraps.
  - Dynamic pause modal height adapting to inventory count.
  - Fixed Column 1 stat binding where HP, Cooldown, and Critical labels were swapped; resolved in scene files and enforced at runtime via `EnsureStatsLabelsAndValues()`.
* [x] **Defeat & Run Summary Modal Rework:**
  - Removed redundant "Try Again" button (clean "Back to menu" button only).
  - Eliminated separate preview box text overlapping bug.
  - Split Run Summary into 2 separate structured containers:
    - Top Container: WEAPONS list with exact damage and DPS.
    - Bottom Container: ITEMS list with passive levels and descriptions.
  - Dynamic card height adapting to content height.
* [x] **Mini-Boss Randomization System (5:00 & 10:00):**
  - Random selection from pool of 8 Mini-Bosses (`Nether Golem`, `Mutant Zombie`, `Corrupted Iron Golem`, `Phantom Lord`, `Piglin Brute`, `Ravager`, `Wither King`, `Pillager Captain`).
  - Non-repeating rule: 10:00 encounter excludes the 5:00 mini-boss.
  - Mid-game Tier 2 stat scaling (+30% HP boost at 10:00).
  - Unified Lucky Block Chest (`BossRewardChest`) drops automatically upon defeating any mini-boss, guaranteeing weapon evolution at 10:00.
* [x] **Grand Final Boss Randomization System (15:00):**
  - Random selection from pool of 5 Grand Final Bosses (`The Warden`, `Grand Wither King`, `Grand Pillager Captain`, `Mutant Colossus`, `Corrupted Titan Golem`).
  - Grand Boss scaling: 38,000–44,000 base HP scaled by difficulty, Crimson Arena Lockdown, 18:00 timeout (3 minutes to defeat).
  - Slaying any Grand Boss automatically collapses the arena and triggers `InGamePauseManager.Instance.TriggerVictory()`!
* [x] **Bug Fixes:**
  - **Totem of Undying:** Fixed 0 HP revive bug; restored correct health restoration and reset death flags cleanly.
  - **Lucky Block Text:** Removed verbose floating text spam upon picking up lucky block chests.

---

### Phase 7: UI & Combat Bug Fixes, Font Cleanup, and Mobile APK Joystick Support (COMPLETED)
* [x] **In-Game Pause & Settings Tab Alignment:**
  - Fixed tab overlay bug by swapping content inside Pause card (Section_Settings).
  - Synced Sound sliders with AudioManager.Instance, added gold fill (#F7B731) and cyan percent labels.
  - Added dynamic modal height adjustment in SettingsModalUI.cs.
* [x] **Wave Spawner 15-Minute Timeout Fix (WaveSpawner.cs):**
  - Updated serialized duration in MainGame.unity from 900s to 1080s (18 mins total).
  - Enforced 	otalRunDuration = 1080f minimum in Awake() and StartRun().
  - Reordered CheckSpecialEvents() before timeout check so 15:00 Final Boss encounter always spawns before timeout.
* [x] **Evolved Weapons Layer & Attack Fix:**
  - Initialized enemyLayer = ~LayerMask.GetMask("UI", "Ignore Raycast") inside base class WeaponBase.cs so dynamically added evolved weapons never have empty layer masks.
  - Fixed Crossbow Evolution (GatlingRepeater) to properly aim, fire 5 explosive bolts with audio SFX, damage multiplier, and critical hit scaling.
* [x] **Laser Evolution Rework (OblivionDeathRay):**
  - Converted from 360-degree rotation into twin consecutive purple laser beams back-to-back (shot 1 then shot 2) targeting nearest enemies with evolved purple color (#D14DFF).
* [x] **Project Cleanup:**
  - Deleted 12 obsolete *SetupHelper.cs scripts from Assets/PixelSurvivor/Scripts/Editor/ to prevent accidental UI/Scene overwriting.
  - Deleted CombatSandbox.unity scene and updated EditorBuildSettings.scenes to only retain MainGame.unity.
* [x] **Font & Glyph Error Cleanup:**
  - Removed all unicode emojis and symbols causing square box errors in font PressStart2P across all UI and combat scripts.
  - Enlarged Title width and enabled AutoSize and NoWrap on LevelUpManager card headers to prevent text wrapping.
* [x] **Level-Up Card Badge Color Hierarchy:**
  - Configured 
ew -> Emerald Green (#20BF6B), Lv x -> Diamond Cyan (#0EA5E9), max -> Amethyst Purple (#B026FF), EVOLVE -> Mythic Gold (#F59E0B).
* [x] **Mobile APK Virtual Joystick (MobileJoystick.cs):**
  - Added dynamic floating joystick spawning at finger touch location on the left 60% of the screen.
  - Filtered out touches over interactive UI (buttons, cards, sliders).
  - Automatically hidden during pause, level-up, game over, or main menu.
  - Integrated into PlayerController.cs as mobile fallback with analog 0..1 movement.
  - Locked PlayerSettings orientation to Landscape Left/Right.

---

## 4. ALL 7 PHASES FULLY IMPLEMENTED & VERIFIED
Ready for User End-to-End Playtest or Android APK Build!
