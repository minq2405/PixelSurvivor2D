# PIXEL SURVIVOR - PROJECT BRIEF

## 1. Project Overview
* **Project Name:** Pixel Survivor
* **Genre:** 2D Fast-Paced Roguelite Action Survivor (Vampire Survivors gameplay + Minecraft Aesthetic)
* **Target Platforms:** PC (Keyboard/Mouse) & Mobile (Touch/Virtual Joystick)
* **Engine & Version:** Unity 6 LTS (6000.4.10f1)
* **Render Pipeline:** Universal Render Pipeline (URP 2D)
* **Input System:** Dual-Input Support: Unity New Input System Package (`InputSystem_Actions`) + Legacy Input Manager Fallback (`Input.GetAxisRaw`)
* **In-Game Language:** 100% English

## 2. Core Gameplay Loop
1. **Move & Survive:** Player moves in an infinite 2D map, automatically attacking approaching waves of enemies with up to 4 active weapons.
2. **Collect & Level Up:** Defeated enemies drop EXP gems/emeralds. Collecting gems fills the top EXP bar to trigger the 2-click Level Up modal.
3. **Choose Upgrades (Weapons & Passives):**
   - **Strict 4-Weapon & 4-Passive Slot Limit:** Players can equip up to 4 active weapons and 4 passive items.
   - **Levels 1 to 5 for Passives:** Passive items upgrade from Lv 1 to Lv 5 (MAX), increasing stats progressively and disappearing from the pool once maxed.
   - **Weapon Evolution Synergy:** Maxed Lv 8 weapon + Maxed Lv 5 paired passive unlocks Legendary Evolved Weapons.
4. **Boss Encounters & Extraction:** Survive escalating enemy density (with 12 distinct mob archetypes and single-unified amethyst Elites) until timed Mini-Bosses (5:00, 10:00) and Final Boss (15:00) arrive.

## 3. Playable Characters
* **Steve (The Iron Survivor):** Balanced melee warrior (4.0 m/s base speed, 120 HP, starts with Flaming Broadsword).
* **Alex (The Swift Ranger):** High agility scout (5.0 m/s base speed, 85 HP, starts with Twin Daggers).
* **Villager (The Fortune Merchant):** Economy & reroll specialist (4.4 m/s base speed, 100 HP, bonus emerald drops, 50% reroll discount, starts with Crossbow).
