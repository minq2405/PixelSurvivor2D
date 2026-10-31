using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public enum PassiveType
    {
        BookOfDamage,      // Pairs with Magma Broadsword -> Solar Flare Greatsword
        FeatherOfSpeed,    // Pairs with Twin Daggers -> Shadow Phantom Daggers
        HeartOfLife,       // Pairs with Battle Axe -> Executioner's Greataxe
        IronShield,        // Pairs with Dark Scythe -> Reaper's Harvest
        ChronosClock,      // Pairs with Crystal Laser -> Oblivion Death Ray
        EmeraldOfWisdom,   // Pairs with Crossbow -> Gatling Repeater
        EnderEye,          // Utility: Magnet Reach
        PotionOfRegen,     // Utility: HP Regen
        TotemOfUndying     // Utility: Revive
    }

    [System.Serializable]
    public class PassiveSlot
    {
        public PassiveType type;
        public int level = 1;
        public const int MaxLevel = 5;

        public bool IsMaxLevel => level >= MaxLevel;
    }

    /// <summary>
    /// Manages the 4 passive slots and level 1 to 5 scaling for Pixel Survivor (v2.1).
    /// Enforces:
    /// - Max 4 equipped passives.
    /// - Level 5 passives retain their slot while leaving the level-up pool.
    /// - Evolution pair associations.
    /// - Totem of Undying Revive mechanics.
    /// </summary>
    public class PassiveController : MonoBehaviour
    {
        public static PassiveController Instance { get; private set; }
        public const int MaxPassives = 4;

        [Header("Equipped Passives (Max 4)")]
        [SerializeField] private List<PassiveSlot> equippedSlots = new List<PassiveSlot>();

        [Header("Totem of Undying State")]
        [SerializeField] private bool hasTotemReviveUsed = false;

        private PlayerHealth playerHealth;
        private PlayerStats playerStats;

        public IReadOnlyList<PassiveSlot> EquippedSlots => equippedSlots;
        public bool CanEquipNewPassive => equippedSlots.Count < MaxPassives;
        public int EquippedCount => equippedSlots.Count;

        public static event Action OnPassivesUpdated;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(this); return; }

            playerHealth = GetComponent<PlayerHealth>();
            playerStats = GetComponent<PlayerStats>();
        }

        public bool HasPassive(PassiveType type)
        {
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                if (equippedSlots[i].type == type) return true;
            }
            return false;
        }

        public int GetLevel(PassiveType type)
        {
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                if (equippedSlots[i].type == type) return equippedSlots[i].level;
            }
            return 0;
        }

        public PassiveSlot GetSlot(PassiveType type)
        {
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                if (equippedSlots[i].type == type) return equippedSlots[i];
            }
            return null;
        }

        /// <summary>
        /// Equips a new passive or upgrades an existing one up to Lv5.
        /// </summary>
        public bool EquipOrUpgradePassive(PassiveType type)
        {
            var existing = GetSlot(type);
            if (existing != null)
            {
                if (existing.IsMaxLevel) return false;
                existing.level++;
                ApplyPassivePerLevelBonus(type, existing.level);
                OnPassivesUpdated?.Invoke();
                Debug.Log($"<color=#10B981>[Passive]</color> Upgraded {GetPassiveName(type)} to Level {existing.level}!");
                return true;
            }

            if (!CanEquipNewPassive)
            {
                Debug.LogWarning("[PassiveController] Passive slots are full (Max 4)!");
                return false;
            }

            PassiveSlot newSlot = new PassiveSlot { type = type, level = 1 };
            equippedSlots.Add(newSlot);
            ApplyPassivePerLevelBonus(type, 1);
            OnPassivesUpdated?.Invoke();
            Debug.Log($"<color=#10B981>[Passive]</color> Equipped new passive: {GetPassiveName(type)} (Slot {equippedSlots.Count}/{MaxPassives})!");
            return true;
        }

        /// <summary>
        /// Swap mechanism for Evolution Pity (v2.1 Section 5.2):
        /// Replaces an existing equipped passive with a new paired passive at Level 1.
        /// </summary>
        public bool SwapPassive(PassiveType oldType, PassiveType newType)
        {
            var oldSlot = GetSlot(oldType);
            if (oldSlot == null) return false;

            // Revert old passive stats
            RevertPassiveStats(oldType, oldSlot.level);

            // Replace with new type at Level 1
            oldSlot.type = newType;
            oldSlot.level = 1;

            // Apply new Level 1 stats
            ApplyPassivePerLevelBonus(newType, 1);
            OnPassivesUpdated?.Invoke();

            Debug.Log($"<color=#38BDF8>[Evolution Pity Swap]</color> Swapped {GetPassiveName(oldType)} for {GetPassiveName(newType)} Lv.1!");
            return true;
        }

        private void ApplyPassivePerLevelBonus(PassiveType type, int newLevel)
        {
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            if (playerStats == null) return;

            switch (type)
            {
                case PassiveType.BookOfDamage:
                    // +8% Damage per level (Lv5: +40%)
                    playerStats.AddBonusDamage(0.08f);
                    break;

                case PassiveType.FeatherOfSpeed:
                    // +5% Speed per level (Lv5: +25%)
                    playerStats.AddBonusMoveSpeed(0.05f);
                    break;

                case PassiveType.HeartOfLife:
                    // +15 Max HP per level (Lv5: +75 HP)
                    playerStats.AddMaxHealth(15f, healEquivalent: true);
                    break;

                case PassiveType.IronShield:
                    // +5% Armor per level (Lv5: +25% Armor)
                    playerStats.AddBonusArmor(0.05f);
                    break;

                case PassiveType.ChronosClock:
                    // -5% Cooldown per level (Lv5: -25% CDR)
                    playerStats.AddBonusCDR(0.05f);
                    break;

                case PassiveType.EnderEye:
                    // +20% Magnet per level (Lv5: +100%)
                    playerStats.AddBonusMagnet(0.20f);
                    break;

                case PassiveType.PotionOfRegen:
                    // +0.4 HP/s per level (Lv5: +2.0 HP/s)
                    playerStats.AddBonusRegen(0.40f);
                    break;

                case PassiveType.EmeraldOfWisdom:
                    // +8% EXP per level (Lv5: +40% EXP + 10% Boss/Elite Damage)
                    playerStats.AddBonusEXP(0.08f);
                    if (newLevel >= 5)
                    {
                        playerStats.AddBonusBossEliteDamage(0.10f);
                    }
                    break;

                case PassiveType.TotemOfUndying:
                    hasTotemReviveUsed = false;
                    Debug.Log($"<color=#FACC15>[Totem]</color> Revive active at {GetTotemRevivePercent(newLevel) * 100:F0}% HP!");
                    break;
            }
        }

        private void RevertPassiveStats(PassiveType type, int currentLevel)
        {
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            if (playerStats == null) return;

            switch (type)
            {
                case PassiveType.BookOfDamage:
                    playerStats.AddBonusDamage(-0.08f * currentLevel);
                    break;
                case PassiveType.FeatherOfSpeed:
                    playerStats.AddBonusMoveSpeed(-0.05f * currentLevel);
                    break;
                case PassiveType.HeartOfLife:
                    playerStats.AddMaxHealth(-15f * currentLevel, healEquivalent: false);
                    break;
                case PassiveType.IronShield:
                    playerStats.AddBonusArmor(-0.05f * currentLevel);
                    break;
                case PassiveType.ChronosClock:
                    playerStats.AddBonusCDR(-0.05f * currentLevel);
                    break;
                case PassiveType.EnderEye:
                    playerStats.AddBonusMagnet(-0.20f * currentLevel);
                    break;
                case PassiveType.PotionOfRegen:
                    playerStats.AddBonusRegen(-0.40f * currentLevel);
                    break;
                case PassiveType.EmeraldOfWisdom:
                    playerStats.AddBonusEXP(-0.08f * currentLevel);
                    if (currentLevel >= 5) playerStats.AddBonusBossEliteDamage(-0.10f);
                    break;
                case PassiveType.TotemOfUndying:
                    hasTotemReviveUsed = true;
                    break;
            }
        }

        /// <summary>
        /// Attempts to revive player upon lethal damage using Totem of Undying.
        /// </summary>
        public bool TryUseTotemRevive()
        {
            if (!HasPassive(PassiveType.TotemOfUndying) || hasTotemReviveUsed) return false;

            int level = GetLevel(PassiveType.TotemOfUndying);
            float revivePercent = GetTotemRevivePercent(level);
            hasTotemReviveUsed = true;

            if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                float restoredHp = playerHealth.MaxHealth * revivePercent;
                playerHealth.Heal(restoredHp);

                // Lv5 grants 2.0s invincibility
                float invulnDuration = (level >= 5) ? 2.0f : 1.0f;
                playerHealth.IsInvincible = true;
                this.StartCoroutine(DisableInvincibleAfterDelay(invulnDuration));

                DamageNumber.SpawnText(transform.position + Vector3.up * 0.8f, "✨ REVIVED! ✨", Color.yellow);
                Debug.Log($"<color=#F59E0B>[TOTEM OF UNDYING]</color> Player resurrected with {restoredHp:F0} HP ({revivePercent * 100:F0}%)!");
                return true;
            }

            return false;
        }

        private System.Collections.IEnumerator DisableInvincibleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (playerHealth != null) playerHealth.IsInvincible = false;
        }

        public static float GetTotemRevivePercent(int level)
        {
            // Lv1: 40%, Lv2: 50%, Lv3: 60%, Lv4: 70%, Lv5: 80%
            return Mathf.Clamp(0.30f + (level * 0.10f), 0.40f, 0.80f);
        }

        public static string GetPassiveName(PassiveType type)
        {
            switch (type)
            {
                case PassiveType.BookOfDamage: return "Book of Damage";
                case PassiveType.FeatherOfSpeed: return "Feather of Speed";
                case PassiveType.HeartOfLife: return "Heart of Life";
                case PassiveType.IronShield: return "Iron Shield";
                case PassiveType.ChronosClock: return "Chronos Clock";
                case PassiveType.EnderEye: return "Ender Eye";
                case PassiveType.PotionOfRegen: return "Potion of Regen";
                case PassiveType.EmeraldOfWisdom: return "Emerald of Wisdom";
                case PassiveType.TotemOfUndying: return "Totem of Undying";
                default: return type.ToString();
            }
        }

        public static string GetPairedWeaponName(PassiveType type)
        {
            switch (type)
            {
                case PassiveType.BookOfDamage: return "Magma Broadsword";
                case PassiveType.FeatherOfSpeed: return "Twin Daggers";
                case PassiveType.HeartOfLife: return "Battle Axe";
                case PassiveType.IronShield: return "Dark Scythe";
                case PassiveType.ChronosClock: return "Crystal Laser";
                case PassiveType.EmeraldOfWisdom: return "Crossbow";
                default: return null;
            }
        }

        public static PassiveType? GetPairedPassiveForWeapon(string weaponId)
        {
            switch (weaponId)
            {
                case "weapon_magma_broadsword": return PassiveType.BookOfDamage;
                case "weapon_twin_daggers": return PassiveType.FeatherOfSpeed;
                case "weapon_battle_axe": return PassiveType.HeartOfLife;
                case "weapon_dark_scythe": return PassiveType.IronShield;
                case "weapon_crystal_laser": return PassiveType.ChronosClock;
                case "weapon_crossbow": return PassiveType.EmeraldOfWisdom;
                default: return null;
            }
        }

        public static string GetEvolvedWeaponName(string weaponId)
        {
            switch (weaponId)
            {
                case "weapon_magma_broadsword": return "Solar Flare Greatsword";
                case "weapon_battle_axe": return "Executioner's Greataxe";
                case "weapon_twin_daggers": return "Shadow Phantom Daggers";
                case "weapon_dark_scythe": return "Reaper's Harvest";
                case "weapon_crystal_laser": return "Oblivion Death Ray";
                case "weapon_crossbow": return "Gatling Repeater";
                default: return null;
            }
        }
    }
}
