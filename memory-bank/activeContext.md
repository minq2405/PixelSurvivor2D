# PIXEL SURVIVOR - ACTIVE CONTEXT (v2.1 SPEC ALIGNED)

## 1. Current Status & Focus
* **Current State:** **ALL 4 PHASES FULLY IMPLEMENTED & VERIFIED IN UNITY! READY FOR USER PLAYTEST.**
* **Phase 1 Completed:** `TimeScaleManager.cs` (Token pause), `PlayerStats.cs` (5 Hard Caps, 3 Character base profiles, 0.35s contact I-frame + surround multiplier, 0.25s separate lock, 1.0s regen pause), `ExperienceManager.cs` ($\text{EXP} = \text{Round}(20 \times Lv^{1.20})$, Mode EXP multipliers).
* **Phase 2 Completed:** 6 Weapons balanced to DPS budget $B(L)$, targeting priority `Boss > Elite > Closest`, 9 Passives with Lv1->5 progression and max 4 slots, Evolution Gating ($\ge 09:00$, max 2/run) and Evolution Pity (from 08:00), 6 Mythic Evolved Weapons, Backup card pool.
* **Phase 3 Completed:** Linear HP scaling with Final Surge, Global Mob Cap (150/180/220) and Summon Sub-Cap (25), anti-exploit, Spatial Grid Boids at 10 Hz, 2s Elite budget rolling with guaranteed first Elite, 5 Affixes, Pincer event (1:30 with 60° escape gap), Swarm Rush, Siege.
* **Phase 4 Completed:**
  - Mini-Bosses: Nether Golem I (05:00, 3,300 HP, Ground Slam 3.0m, Lava Throw, +1 Upgrade Chest) & Nether Golem II (10:00, 9,600 HP, Enrage <50% with Flame Trail, Mythic Evolution Chest).
  - Final Boss: The Warden (15:00, 42,000 HP, Smash 60, Sonic Boom 45, Charge 55, Sculk Tendril Pull 25 when >5.5m, Cataclysm Enrage 180s/150s/130s, wave spawn reduced by 60%).
  - Economics: Lucky Block drops 25% of next level EXP + 2–5 💎; TNT Nuke 12.0m fixed radius, wipes regular mobs, 35% HP to Elites, immune Bosses, caps EXP at 50% next level & 15 💎, 45s min gap; Reroll cost ladder [2, 3, 5, 8, 12, 18] 💎 (Villager 50% discount); Gem auto-merge > 250.
  - Difficulty Selection UI: 3-card modal (Normal, Hard, Hardcore) integrated with MainMenuManager.
* **Next Step:** Hand off to USER for the comprehensive End-to-End Playtest!
* **Master Specification:** `PixelSurvivor_Core_Game_Design_v2.1_Patched.md` and `Ke_Hoach_Chi_Tiet_4_Giai_Doan_v2.1.md`.

---

## 2. Key v2.1 Paradigm Shifts (What Changed from v1/v2.0)
1. **Mathematical Equilibrium & Anti-Inflation:**
   - Level target locked to **45–46 picks at 15:00 (Lv 46–47)** with smooth curve $\text{EXP} = \text{Round}(20 \times Lv^{1.20})$.
   - Hard caps on player scaling: Armor 35%, CDR 30%, MoveSpeed +40%, Crit 60%, Bonus DMG +60%.
   - Global contact I-frame set to **0.35s** with surround damage scaling ($+25\%/\text{touching mob up to } +100\%$).
   - Enemy contact damage scales continuously with time: $\text{DMG}(t) = \text{DMG}_{\text{base}} \times (1 + 0.05t) \times \text{DamMult}_m$.
2. **Weapons & Evolution Pacing:**
   - Overpowered weapons brought to standard DPS budget $B(L)$: Twin Daggers Lv8 reduced from 928 DPS to **171.4 DPS** (+8% Crit); Crossbow Lv8 reduced from 630 DPS to **87.5 DPS** (3-pierce with -25%/pierce).
   - Weapon Evolution strictly gated to **$\ge 09:00$** (max 2 evolutions per run).
   - Evolution Pity active from 08:00 (guarantee within $\le 3$ rolls, or offers a **Swap** card if 4 slots filled).
   - Passive items at Lv 5 **retain their equipped slot**.
3. **Bosses & Encounter Mechanics:**
   - Boss HP adjusted to real TTK targets: Nether Golem I (3,300 HP), Golem II (9,600 HP), The Warden (42,000 HP).
   - The Warden equipped with telegraph abilities: Sonic Boom, Charge (55 dmg), Sculk Tendril Pull (pulls player when distance > 5.5m), and an Enrage timer (180s/150s/130s).
   - Summoned mobs (Vex, Slimelets, Boss summons) **drop zero EXP and zero Emeralds** to prevent infinite farming.
4. **Architecture & Performance Guardrails:**
   - Centralized `TimeScaleManager` with reference-counted `PauseToken`.
   - Enemy-Enemy collision disabled; spatial grid separation at 10 Hz.
   - Fixed 12.0m radius for TNT (decoupled from screen ratio) with capped rewards.
   - Reroll costs: [2, 3, 5, 8, 12, 18] 💎 (Villager 50% discount).

---

## 3. Immediate Next Steps (Pending User Confirmation)
Once the user confirms the v2.1 plan:
1. **Step 1:** Implement `TimeScaleManager.cs` & `PlayerStats.cs` (Centralize timeScale, hard caps, 0.35s I-frame, surround damage, and regen pause).
2. **Step 2:** Update `ExperienceManager.cs` to the v2.1 curve ($20 \times Lv^{1.20}$) and mode EXP multipliers.
3. **Step 3:** Rebalance 6 weapon ScriptableObjects/controllers to v2.1 DPS budget and target priorities (`Boss > Elite > Closest`).
4. **Step 4:** Implement the 9 Passive Items (Lv 1 to Lv 5 progression, slot retention, Evolution Pity, Swap card, and Backup cards).
5. **Step 5:** Implement 6 Evolved Weapons with the $\ge 09:00$ gate.
6. **Step 6:** Implement Mob HP Final Surge scaling, chaser acceleration ($+0.02\text{ m/s/min}$), summon exploit prevention, and Elite budget rolling.
7. **Step 7:** Implement Mini-Bosses (Nether Golem I & II) and The Warden.
8. **Step 8:** Build Difficulty Selection UI and test run telemetry.
