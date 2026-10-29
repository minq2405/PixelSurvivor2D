using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public enum EliteAffix
    {
        None,
        Berserker,
        Volatile,
        ChillingAura,
        Shielded,
        Vampiric,
        MagneticPull
    }

    [CreateAssetMenu(fileName = "DifficultyEliteConfig", menuName = "Survivor/Difficulty Elite Config")]
    public class DifficultyEliteConfigSO : ScriptableObject
    {
        [Header("Difficulty Settings")]
        public GameDifficulty difficulty = GameDifficulty.Normal;
        [Tooltip("Thời điểm xuất hiện đầu tiên (giây) - Normal: 420s (7m), Hard: 240s (4m), Hardcore: 120s (2m)")]
        public float unlockTimeInSeconds = 420f;
        [Tooltip("Tỉ lệ biến đổi quái thường thành Elite - Normal: 0.03, Hard: 0.08, Hardcore: 0.15")]
        public float eliteChance = 0.03f;
        [Tooltip("Giới hạn số lượng Elite tối đa trên màn hình - Normal: 1, Hard: 3, Hardcore: 5")]
        public int maxElitesOnScreen = 1;
        [Tooltip("Khoảng nghỉ tối thiểu giữa 2 lần ra Elite - Normal: 30s, Hard: 15s, Hardcore: 0s")]
        public float eliteCooldown = 30f;

        [Header("Stat Multipliers")]
        [Tooltip("Hệ số máu tối đa - Normal: 2.5x, Hard: 3.5x, Hardcore: 5.0x")]
        public float hpMultiplier = 2.5f;
        [Tooltip("Hệ số sát thương va chạm - Normal: 1.25x, Hard: 1.40x, Hardcore: 1.60x")]
        public float damageMultiplier = 1.25f;
        [Tooltip("Hệ số tốc độ di chuyển - Normal: 1.10x, Hard: 1.15x, Hardcore: 1.25x")]
        public float speedMultiplier = 1.10f;
        [Tooltip("Hệ số phóng to kích thước - Normal: 1.15x, Hard: 1.25x, Hardcore: 1.30x")]
        public float scaleMultiplier = 1.15f;

        [Header("Affix Rules")]
        public int minAffixes = 0;
        public int maxAffixes = 1;
        public List<EliteAffix> availableAffixes = new List<EliteAffix>
        {
            EliteAffix.Berserker,
            EliteAffix.Volatile,
            EliteAffix.ChillingAura,
            EliteAffix.Shielded,
            EliteAffix.Vampiric,
            EliteAffix.MagneticPull
        };

        [Header("Miniboss Rules")]
        public bool canEliteMiniboss = false;
        public float minibossEliteChance = 0f;
    }
}
