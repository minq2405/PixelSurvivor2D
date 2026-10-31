# PIXEL SURVIVOR - ACTIVE CONTEXT (v2.1 SPEC ALIGNED)

## 1. Current Status & Focus
* **Current State:** **Game Documentation & Memory Bank Successfully Updated to v2.1 Patched.**
* **Code Modification Status:** **PAUSED / ON HOLD.** Per user instructions, no code or asset modifications will occur until the user reviews and confirms this documentation update.
* **Master Specification:**
  - `PixelSurvivor_Core_Game_Design_v2.1_Patched.md` has been integrated as the primary design authority across repositories and project directories.
  - Core Memory Bank files (`projectbrief.md`, `decisionLog.md`, `systemPatterns.md`, `progress.md`, `activeContext.md`) are 100% synchronized in both `git_repo/PixelSurvivor2D/memory-bank/` and `Unity/ProjectFinalPru/memory-bank/`.

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
