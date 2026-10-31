using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public enum EliteAffix
    {
        None,
        Swift,
        Armored,
        Berserk,
        Regenerating,
        Volatile
    }

    [CreateAssetMenu(fileName = "DifficultyEliteConfig", menuName = "Survivor/Difficulty Elite Config")]
    public class DifficultyEliteConfigSO : ScriptableObject
    {
        [Header("Difficulty Settings")]
        public GameDifficulty difficulty = GameDifficulty.Normal;
        public float unlockTimeInSeconds = 120f;
        public float eliteChance = 0.03f;
        public int maxElitesOnScreen = 2;
        public float eliteCooldown = 18f;

        [Header("Stat Multipliers (v2.1 Standardized)")]
        public float hpMultiplier = 2.5f;
        public float damageMultiplier = 1.30f;
        public float speedMultiplier = 1.00f;
        public float scaleMultiplier = 1.20f;
        public float knockbackResistance = 0.25f;

        [Header("Affix Multipliers & Settings")]
        public float swiftSpeedBonus = 0.12f;
        public float armoredDamageReduction = 0.25f;
        public float berserkDamageBonus = 0.35f;
        public float regenPercentPerSecond = 0.01f;
        public float volatileDamage = 30f;
        public float volatileRadius = 2.0f;

        [Header("Chest Drop Settings")]
        public float chestBaseChance = 0.20f;
        public float chestPityStep = 0.10f;
        public int maxChestsPerRun = 10;
        public float chestMinGap = 20f;

        [Header("Miniboss Rules")]
        public bool canEliteMiniboss = false;
        public float minibossEliteChance = 0f;
    }
}
