using System;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// PlayerStats manages all player attributes and strictly enforces v2.1 Hard Caps:
    /// - Armor Cap: <= 35% (0.35f)
    /// - Cooldown Reduction (CDR) Cap: <= 30% (0.30f)
    /// - Bonus Move Speed Cap: <= +40% (0.40f)
    /// - Crit Chance Cap: <= 60% (0.60f)
    /// - Bonus Damage Cap: <= +60% (0.60f)
    /// Also manages character starting baselines (Steve, Alex, Villager) and passive traits.
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        // ==========================
        // HARD CAPS (v2.1 SPECIFICATION)
        // ==========================
        public const float ARMOR_CAP = 0.35f;             // Max 35%
        public const float CDR_CAP = 0.30f;               // Max 30%
        public const float MOVE_SPEED_BONUS_CAP = 0.40f;  // Max +40%
        public const float CRIT_CHANCE_CAP = 0.60f;       // Max 60%
        public const float BONUS_DAMAGE_CAP = 0.60f;      // Max +60%

        [Header("Character Identity")]
        [SerializeField] private string characterId = "Steve";

        [Header("Base Character Attributes")]
        [SerializeField] private float baseMaxHealth = 120f;
        [SerializeField] private float baseRegen = 0.5f;          // HP per second
        [SerializeField] private float baseMoveSpeed = 4.0f;      // m/s
        [SerializeField] private float baseArmor = 0.10f;         // 10%
        [SerializeField] private float baseCDR = 0.0f;            // 0%
        [SerializeField] private float baseCritChance = 0.05f;    // 5%
        [SerializeField] private float baseCritMultiplier = 1.80f;// 1.80x
        [SerializeField] private float baseMagnetRadius = 1.7f;   // 1.7m

        [Header("Passive Bonuses (From Cards / Upgrades)")]
        [SerializeField] private float bonusMaxHealth = 0f;
        [SerializeField] private float bonusRegen = 0f;
        [SerializeField] private float bonusMoveSpeedPercent = 0f;
        [SerializeField] private float bonusArmor = 0f;
        [SerializeField] private float bonusCDR = 0f;
        [SerializeField] private float bonusCritChance = 0f;
        [SerializeField] private float bonusDamagePercent = 0f;
        [SerializeField] private float bonusMagnetPercent = 0f;
        [SerializeField] private float bonusEXPPercent = 0f;
        [SerializeField] private float bonusBossEliteDamage = 0f;

        private PlayerHealth playerHealth;
        private PlayerController playerController;
        private PlayerCollector playerCollector;

        public event Action OnStatsChanged;

        public string CharacterId => characterId;

        // ==========================
        // EFFECTIVE VALUES WITH HARD CAPS
        // ==========================

        /// <summary>
        /// Total Armor reduction, strictly capped at 35% (v2.1).
        /// </summary>
        public float EffectiveArmor => Mathf.Clamp(baseArmor + bonusArmor, 0f, ARMOR_CAP);

        /// <summary>
        /// Total Cooldown Reduction, strictly capped at 30% (v2.1).
        /// </summary>
        public float EffectiveCDR => Mathf.Clamp(baseCDR + bonusCDR, 0f, CDR_CAP);

        /// <summary>
        /// Final Cooldown Multiplier applied to weapons (Sàn 0.70x).
        /// </summary>
        public float CooldownMultiplier => Mathf.Max(0.70f, 1f - EffectiveCDR);

        /// <summary>
        /// Bonus Move Speed percentage, strictly capped at +40% (v2.1).
        /// </summary>
        public float EffectiveBonusMoveSpeedPct => Mathf.Clamp(bonusMoveSpeedPercent, 0f, MOVE_SPEED_BONUS_CAP);

        /// <summary>
        /// Final movement speed in m/s, strictly capped by character limits:
        /// Steve max 5.0, Alex max 5.6, Villager max 5.2.
        /// </summary>
        public float EffectiveMoveSpeed
        {
            get
            {
                float calculated = baseMoveSpeed * (1f + EffectiveBonusMoveSpeedPct);
                float maxCeiling = 5.0f;
                if (characterId == "Alex") maxCeiling = 5.6f;
                else if (characterId == "Villager") maxCeiling = 5.2f;

                return Mathf.Min(calculated, maxCeiling);
            }
        }

        /// <summary>
        /// Total Crit Chance, strictly capped at 60% (v2.1).
        /// </summary>
        public float EffectiveCritChance => Mathf.Clamp(baseCritChance + bonusCritChance, 0f, CRIT_CHANCE_CAP);

        /// <summary>
        /// Crit Multiplier (Steve 1.80x, Alex 1.90x, Villager 1.70x).
        /// </summary>
        public float EffectiveCritMultiplier => baseCritMultiplier;

        /// <summary>
        /// Bonus Damage percentage, strictly capped at +60% (v2.1).
        /// Includes character traits (Steve Iron Will: +20% when HP < 50%).
        /// </summary>
        public float EffectiveBonusDamagePct
        {
            get
            {
                float totalBonus = bonusDamagePercent;

                // Steve's Iron Will: +20% Dmg when < 50% HP
                if (characterId == "Steve" && playerHealth != null && playerHealth.MaxHealth > 0f)
                {
                    if (playerHealth.CurrentHealth / playerHealth.MaxHealth < 0.5f)
                    {
                        totalBonus += 0.20f;
                    }
                }

                return Mathf.Clamp(totalBonus, 0f, BONUS_DAMAGE_CAP);
            }
        }

        /// <summary>
        /// Final Damage Multiplier applied to weapons.
        /// </summary>
        public float DamageMultiplier => 1f + EffectiveBonusDamagePct;

        /// <summary>
        /// Effective Max HP.
        /// </summary>
        public float EffectiveMaxHealth => baseMaxHealth + bonusMaxHealth;

        /// <summary>
        /// Effective HP Regeneration rate (HP/s).
        /// </summary>
        public float EffectiveRegen => Mathf.Max(0f, baseRegen + bonusRegen);

        /// <summary>
        /// Effective Magnet pickup radius.
        /// </summary>
        public float EffectiveMagnetRadius => baseMagnetRadius * (1f + bonusMagnetPercent);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            playerHealth = GetComponent<PlayerHealth>();
            playerController = GetComponent<PlayerController>();
            playerCollector = GetComponent<PlayerCollector>();

            ApplyCharacterProfile(characterId);
        }

        /// <summary>
        /// Applies the standard v2.1 base attributes for the selected character.
        /// </summary>
        public void ApplyCharacterProfile(string id)
        {
            characterId = id;
            bonusMaxHealth = 0f;
            bonusRegen = 0f;
            bonusMoveSpeedPercent = 0f;
            bonusArmor = 0f;
            bonusCDR = 0f;
            bonusCritChance = 0f;
            bonusDamagePercent = 0f;
            bonusMagnetPercent = 0f;

            switch (id)
            {
                case "Alex":
                    baseMaxHealth = 85f;
                    baseRegen = 0.0f;
                    baseMoveSpeed = 4.8f;      // +20% over 4.0
                    baseArmor = 0.0f;
                    baseCDR = 0.10f;           // Agility: -10% Cooldown
                    baseCritChance = 0.12f;    // 12%
                    baseCritMultiplier = 1.90f;// 190%
                    baseMagnetRadius = 1.7f;
                    break;

                case "Villager":
                    baseMaxHealth = 100f;
                    baseRegen = 0.2f;
                    baseMoveSpeed = 4.2f;      // +5% over 4.0
                    baseArmor = 0.0f;
                    baseCDR = 0.0f;
                    baseCritChance = 0.08f;    // 8%
                    baseCritMultiplier = 1.70f;// 170%
                    baseMagnetRadius = 2.2f;   // +30% magnet reach
                    break;

                case "Steve":
                default:
                    characterId = "Steve";
                    baseMaxHealth = 120f;
                    baseRegen = 0.5f;
                    baseMoveSpeed = 4.0f;
                    baseArmor = 0.10f;         // 10%
                    baseCDR = 0.0f;
                    baseCritChance = 0.05f;    // 5%
                    baseCritMultiplier = 1.80f;// 180%
                    baseMagnetRadius = 1.7f;
                    break;
            }

            SyncWithComponents();
            OnStatsChanged?.Invoke();

            Debug.Log($"<color=#10B981>[PlayerStats]</color> Applied profile '{characterId}': " +
                      $"HP={baseMaxHealth}, Spd={baseMoveSpeed}m/s, Armor={baseArmor * 100:F0}%, " +
                      $"Crit={baseCritChance * 100:F0}%, CDR={baseCDR * 100:F0}%, Magnet={baseMagnetRadius:F1}m");
        }

        private void SyncWithComponents()
        {
            if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerCollector == null) playerCollector = GetComponent<PlayerCollector>();

            if (playerHealth != null)
            {
                playerHealth.SetMaxHealthFromStats(EffectiveMaxHealth);
            }

            if (playerController != null)
            {
                playerController.SetMoveSpeedFromStats(EffectiveMoveSpeed);
            }
        }

        // ==========================
        // MODIFIER METHODS (UPGRADES / PASSIVES)
        // ==========================

        public void AddBonusArmor(float amount)
        {
            bonusArmor += amount;
            OnStatsChanged?.Invoke();
            Debug.Log($"<color=#10B981>[PlayerStats]</color> Armor changed to {EffectiveArmor * 100:F1}% (Cap: {ARMOR_CAP * 100:F0}%)");
        }

        public void AddBonusCDR(float amount)
        {
            bonusCDR += amount;
            OnStatsChanged?.Invoke();
            Debug.Log($"<color=#10B981>[PlayerStats]</color> CDR changed to {EffectiveCDR * 100:F1}% (Cap: {CDR_CAP * 100:F0}%)");
        }

        public void AddBonusMoveSpeed(float percentage)
        {
            bonusMoveSpeedPercent += percentage;
            SyncWithComponents();
            OnStatsChanged?.Invoke();
            Debug.Log($"<color=#10B981>[PlayerStats]</color> Speed Bonus changed to +{EffectiveBonusMoveSpeedPct * 100:F1}% -> {EffectiveMoveSpeed:F2} m/s");
        }

        public void AddBonusCritChance(float amount)
        {
            bonusCritChance += amount;
            OnStatsChanged?.Invoke();
            Debug.Log($"<color=#10B981>[PlayerStats]</color> Crit Chance changed to {EffectiveCritChance * 100:F1}% (Cap: {CRIT_CHANCE_CAP * 100:F0}%)");
        }

        public void AddBonusDamage(float percentage)
        {
            bonusDamagePercent += percentage;
            OnStatsChanged?.Invoke();
            Debug.Log($"<color=#10B981>[PlayerStats]</color> Bonus Damage changed to +{EffectiveBonusDamagePct * 100:F1}% (Cap: +{BONUS_DAMAGE_CAP * 100:F0}%)");
        }

        public void AddMaxHealth(float amount, bool healEquivalent = true)
        {
            bonusMaxHealth += amount;
            if (playerHealth != null)
            {
                playerHealth.IncreaseMaxHealth(amount, healEquivalent);
            }
            OnStatsChanged?.Invoke();
        }

        public void AddBonusRegen(float amount)
        {
            bonusRegen += amount;
            OnStatsChanged?.Invoke();
            Debug.Log($"<color=#10B981>[PlayerStats]</color> Regen changed to {EffectiveRegen:F2} HP/s");
        }

        public void AddBonusMagnet(float percentage)
        {
            bonusMagnetPercent += percentage;
            OnStatsChanged?.Invoke();
            Debug.Log($"<color=#10B981>[PlayerStats]</color> Magnet Radius changed to {EffectiveMagnetRadius:F2} m");
        }

        public void AddBonusEXP(float amount)
        {
            bonusEXPPercent += amount;
            OnStatsChanged?.Invoke();
            Debug.Log($"<color=#10B981>[PlayerStats]</color> Bonus EXP changed to +{bonusEXPPercent * 100:F0}%");
        }

        public void AddBonusBossEliteDamage(float amount)
        {
            bonusBossEliteDamage += amount;
            OnStatsChanged?.Invoke();
            Debug.Log($"<color=#10B981>[PlayerStats]</color> Boss/Elite Damage Bonus changed to +{bonusBossEliteDamage * 100:F0}%");
        }

        public float EffectiveEXPBonus => bonusEXPPercent;
        public float EffectiveBossEliteDamage => bonusBossEliteDamage;

        // ==========================
        // COMBAT CALCULATION HELPERS
        // ==========================

        /// <summary>
        /// Calculates incoming damage after applying armor reduction capped at 35%.
        /// Formula: max(1, incomingDmg * (1 - min(Armor, 0.35)))
        /// </summary>
        public float CalculateDamageTaken(float incomingDamage)
        {
            float reduction = EffectiveArmor;
            float reducedDmg = incomingDamage * (1.0f - reduction);
            return Mathf.Max(1.0f, reducedDmg);
        }

        /// <summary>
        /// Calculates weapon strike damage and evaluates critical hits.
        /// Respects the +60% bonus damage cap and 60% crit chance cap.
        /// </summary>
        public (float damage, bool isCrit) CalculateWeaponDamage(float baseWeaponDamage, float weaponCritBonus = 0f, bool isTargetBossOrElite = false)
        {
            float totalBonusDmgPct = EffectiveBonusDamagePct;
            if (isTargetBossOrElite)
            {
                totalBonusDmgPct = Mathf.Clamp(totalBonusDmgPct + bonusBossEliteDamage, 0f, BONUS_DAMAGE_CAP);
            }

            float hitDmg = baseWeaponDamage * (1f + totalBonusDmgPct);
            float totalCritChance = Mathf.Clamp(EffectiveCritChance + weaponCritBonus, 0f, CRIT_CHANCE_CAP);

            bool isCrit = UnityEngine.Random.value < totalCritChance;
            float finalDmg = isCrit ? (hitDmg * EffectiveCritMultiplier) : hitDmg;

            return (finalDmg, isCrit);
        }
    }
}
