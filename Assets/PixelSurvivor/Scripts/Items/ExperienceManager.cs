using System;
using UnityEngine;
using PixelSurvivor.UI;
using PixelSurvivor.Combat;

namespace PixelSurvivor
{
    /// <summary>
    /// Central manager for:
    /// - Level & EXP Progression (v2.1 formula: Round(20 * Lv^1.20), Target Lv46-47 at 15:00)
    /// - Mode EXP Multiplier (Normal 1.00, Hard 1.10, Hardcore 1.25)
    /// - In-Run Emerald Economy (tracking session emeralds used for rerolls, zero DB)
    /// - Item Drop spawning upon enemy death
    /// - Special item effects (Super Magnet, TNT Nuke)
    /// </summary>
    public class ExperienceManager : MonoBehaviour
    {
        public static ExperienceManager Instance { get; private set; }

        [Header("Level & EXP Progression (v2.1)")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentExp = 0;
        [SerializeField] private int expToNextLevel = 20;

        [Header("In-Run Economy (No DB)")]
        [SerializeField] private int currentEmeralds = 0;

        [Header("Prefabs / Drop Templates")]
        [SerializeField] private GameObject expOrbGreenPrefab;
        [SerializeField] private GameObject expOrbYellowPrefab;
        [SerializeField] private GameObject emeraldPrefab;
        [SerializeField] private GameObject luckyBlockPrefab;
        [SerializeField] private GameObject steakPrefab;
        [SerializeField] private GameObject magnetPrefab;
        [SerializeField] private GameObject tntPrefab;

        private LevelUpManager levelUpManager;
        private PlayerController player;
        private WaveSpawner waveSpawner;

        public int CurrentLevel => currentLevel;
        public int CurrentExp => currentExp;
        public int ExpToNextLevel => expToNextLevel;
        public int CurrentEmeralds => currentEmeralds;

        public event Action<int, int> OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action<int> OnEmeraldsChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            expToNextLevel = CalculateExpRequirement(currentLevel);
        }

        private void Start()
        {
            levelUpManager = FindAnyObjectByType<LevelUpManager>();
            player = FindAnyObjectByType<PlayerController>();
            waveSpawner = FindAnyObjectByType<WaveSpawner>();
        }

        /// <summary>
        /// v2.1 Mathematical EXP formula: EXP(Lv) = Round(20 * Lv^1.20)
        /// Lv 1: 20 | Lv 2: 46 | Lv 5: 138 | Lv 10: 317 | Lv 20: 728 | Lv 30: 1,185 | Lv 45: 1,927
        /// </summary>
        public int CalculateExpRequirement(int level)
        {
            int safeLevel = Mathf.Max(1, level);
            return Mathf.Max(1, Mathf.RoundToInt(20.0f * Mathf.Pow(safeLevel, 1.20f)));
        }

        /// <summary>
        /// Mode EXP Multiplier (Normal: 1.00, Hard: 1.10, Hardcore: 1.25)
        /// Compensates for tougher enemies in higher difficulty settings.
        /// </summary>
        public float GetModeExpMultiplier()
        {
            if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();
            if (waveSpawner != null)
            {
                switch (waveSpawner.Difficulty)
                {
                    case GameDifficulty.Hardcore: return 1.25f;
                    case GameDifficulty.Hard: return 1.10f;
                    default: return 1.00f;
                }
            }
            return 1.00f;
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            float modeMult = GetModeExpMultiplier();
            float expBonus = (Combat.PlayerStats.Instance != null) ? (1f + Combat.PlayerStats.Instance.EffectiveEXPBonus) : 1f;
            int effectiveExp = Mathf.Max(1, Mathf.RoundToInt(amount * modeMult * expBonus));

            currentExp += effectiveExp;
            OnExpChanged?.Invoke(currentExp, expToNextLevel);

            while (currentExp >= expToNextLevel)
            {
                currentExp -= expToNextLevel;
                currentLevel++;
                expToNextLevel = CalculateExpRequirement(currentLevel);

                OnLevelUp?.Invoke(currentLevel);
                OnExpChanged?.Invoke(currentExp, expToNextLevel);

                if (levelUpManager == null)
                {
                    levelUpManager = FindAnyObjectByType<LevelUpManager>();
                }

                if (levelUpManager != null)
                {
                    levelUpManager.TriggerLevelUp(currentLevel);
                }
            }
        }

        public void AddEmeralds(int amount)
        {
            if (amount <= 0) return;
            currentEmeralds += amount;
            OnEmeraldsChanged?.Invoke(currentEmeralds);
            Debug.Log($"<color=#10B981>[Emerald Picked]</color> +{amount} 💎 (Total: {currentEmeralds})");
        }

        public bool SpendEmeralds(int amount)
        {
            if (currentEmeralds < amount) return false;
            currentEmeralds -= amount;
            OnEmeraldsChanged?.Invoke(currentEmeralds);
            return true;
        }

        public void AttractAllPickups()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;

            PickupItem[] allPickups = FindObjectsByType<PickupItem>(FindObjectsInactive.Exclude);
            for (int i = 0; i < allPickups.Length; i++)
            {
                if (allPickups[i] != null && allPickups[i].CanBeMagnetized)
                {
                    allPickups[i].AttractTo(player.transform);
                }
            }
            Debug.Log($"<color=#38BDF8>[Super Magnet]</color> Attracted {allPickups.Length} items to player!");
        }

        private float lastTntTime = -999f;
        private const float TntMinGap = 45f;
        private const float TntRadius = 12.0f;
        private float mergeCheckTimer = 0f;
        private const int MaxActivePickupsThreshold = 250;

        private void Update()
        {
            CheckAutoMergePickups();
        }

        private void CheckAutoMergePickups()
        {
            mergeCheckTimer += Time.deltaTime;
            if (mergeCheckTimer < 2.0f) return;
            mergeCheckTimer = 0f;

            PickupItem[] allPickups = FindObjectsByType<PickupItem>(FindObjectsInactive.Exclude);
            if (allPickups.Length < MaxActivePickupsThreshold) return;

            // Merge nearby small exp orbs (< 2.5m)
            int mergedCount = 0;
            for (int i = 0; i < allPickups.Length; i++)
            {
                var p1 = allPickups[i];
                if (p1 == null || p1.ItemType != PickupType.ExpSmall) continue;

                for (int j = i + 1; j < allPickups.Length; j++)
                {
                    var p2 = allPickups[j];
                    if (p2 == null || p2.ItemType != PickupType.ExpSmall) continue;

                    if (Vector2.Distance(p1.transform.position, p2.transform.position) < 2.5f)
                    {
                        p1.Initialize(PickupType.ExpMedium, p1.Value + p2.Value);
                        Destroy(p2.gameObject);
                        mergedCount++;
                        if (allPickups.Length - mergedCount < 180) break;
                    }
                }
                if (allPickups.Length - mergedCount < 180) break;
            }

            if (mergedCount > 0)
            {
                Debug.Log($"<color=#38BDF8>[Auto-Merge]</color> Consolidated {mergedCount} EXP gems to preserve performance!");
            }
        }

        public void DetonateTnt(Vector2 position)
        {
            if (Time.time - lastTntTime < TntMinGap)
            {
                Debug.Log($"<color=#F59E0B>[TNT]</color> Detonated recently ({Time.time - lastTntTime:F1}s ago < 45s min gap), skipped.");
                return;
            }
            lastTntTime = Time.time;

            // v2.1: 12.0m fixed radius (independent of camera ratio)
            Combat.EnemyHealth[] allEnemies = FindObjectsByType<Combat.EnemyHealth>(FindObjectsInactive.Exclude);
            int count = 0;
            int totalTntExp = 0;
            int totalTntEmeralds = 0;
            int expCap = Mathf.Max(50, Mathf.RoundToInt(expToNextLevel * 0.50f)); // Max 50% next level EXP
            int emeraldCap = 15; // Max 15 emeralds

            for (int i = 0; i < allEnemies.Length; i++)
            {
                var enemy = allEnemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                float dist = Vector2.Distance(position, enemy.transform.position);
                if (dist <= TntRadius)
                {
                    // Boss/Miniboss immunity
                    if (enemy.IsBoss || enemy.gameObject.name.Contains("Boss") || enemy.gameObject.name.Contains("Golem") || enemy.gameObject.name.Contains("Warden"))
                    {
                        Combat.DamageNumber.SpawnText(enemy.transform.position + Vector3.up * 0.5f, "IMMUNE", Color.white);
                        continue;
                    }

                    // Elite takes 35% Max HP
                    var eliteCtrl = enemy.GetComponent<Combat.EliteEnemyAffixController>();
                    if (eliteCtrl != null && eliteCtrl.IsElite)
                    {
                        float eliteDmg = enemy.MaxHealth * 0.35f;
                        enemy.TakeDamage(eliteDmg, Vector2.zero, 0f, false, "tnt_nuke");
                        count++;
                        continue;
                    }

                    // Regular mob: instant kill (skips lifesteal)
                    enemy.TakeDamage(99999f, Vector2.zero, 0f, false, "tnt_nuke");
                    count++;

                    if (totalTntExp < expCap)
                    {
                        int dropExp = Mathf.Min(10, expCap - totalTntExp);
                        totalTntExp += dropExp;
                    }
                    if (totalTntEmeralds < emeraldCap && UnityEngine.Random.value < 0.20f)
                    {
                        totalTntEmeralds++;
                    }
                }
            }

            if (totalTntExp > 0) AddExp(totalTntExp);
            if (totalTntEmeralds > 0) AddEmeralds(totalTntEmeralds);

            if (player == null) player = FindAnyObjectByType<PlayerController>();
            Vector3 noticePos = player != null ? player.transform.position + Vector3.up * 1.2f : (Vector3)position + Vector3.up * 0.5f;
            Combat.DamageNumber.SpawnText(noticePos, $"💥 TNT NUKE! (+{totalTntExp} EXP, +{totalTntEmeralds} 💎)", new Color(1f, 0.25f, 0.25f));

            Debug.Log($"<color=#EF4444>[TNT SCREEN NUKE]</color> Blast radius 12m hit {count} enemies! Capped rewards: {totalTntExp}/{expCap} EXP, {totalTntEmeralds}/{emeraldCap} 💎");
        }

        public void SpawnLuckyChestLoot(Vector2 position)
        {
            // v2.1: 25% of next level's EXP (min 5 EXP)
            int luckyExp = Mathf.Max(5, Mathf.RoundToInt(expToNextLevel * 0.25f));
            AddExp(luckyExp);
            Combat.DamageNumber.SpawnText((Vector3)position + Vector3.up * 0.8f, $"<color=#FACC15>+{luckyExp} EXP</color>", Color.yellow);

            // Guaranteed 2 to 5 Emeralds
            int emeraldCount = UnityEngine.Random.Range(2, 6);
            for (int i = 0; i < emeraldCount; i++)
            {
                SpawnItem(emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.45f, PickupType.Emerald, UnityEngine.Random.Range(1, 3));
            }

            // High-Value Bonus Drop Table (Jackpot 10%, Steak 35%, Magnet 27%, TNT 28%)
            float roll = UnityEngine.Random.value;
            if (roll < 0.10f)
            {
                Debug.Log("<color=#F59E0B>[LUCKY CHEST]</color> JACKPOT!");
                if (steakPrefab != null) SpawnItem(steakPrefab, position + Vector2.up * 0.4f, PickupType.Steak, 50);
                if (magnetPrefab != null) SpawnItem(magnetPrefab, position + Vector2.left * 0.4f, PickupType.Magnet, 1);
                if (tntPrefab != null) SpawnItem(tntPrefab, position + Vector2.right * 0.4f, PickupType.Tnt, 1);
                SpawnItem(emeraldPrefab, position + Vector2.down * 0.4f, PickupType.Emerald, 5);
                Combat.DamageNumber.SpawnText((Vector3)position + Vector3.up * 1.2f, "<color=#F59E0B>[JACKPOT!]</color>", Color.yellow);
            }
            else if (roll < 0.45f)
            {
                if (steakPrefab != null)
                {
                    SpawnItem(steakPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.4f, PickupType.Steak, 50);
                }
            }
            else if (roll < 0.72f)
            {
                if (magnetPrefab != null)
                {
                    SpawnItem(magnetPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.4f, PickupType.Magnet, 1);
                }
            }
            else
            {
                if (tntPrefab != null)
                {
                    SpawnItem(tntPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.4f, PickupType.Tnt, 1);
                }
            }
        }

        public void SpawnDrops(Vector2 position, string mobType = "Zombie")
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();

            // Drop logic
            if (mobType.Contains("Boss") || mobType.Contains("Dragon"))
            {
                SpawnItem(expOrbYellowPrefab, position, PickupType.ExpLarge, 250);
                SpawnItem(emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.5f, PickupType.Emerald, 20);
            }
            else if (mobType.Contains("Elite") || mobType.Contains("Golem"))
            {
                SpawnItem(expOrbYellowPrefab, position, PickupType.ExpMedium, 50);
                if (luckyBlockPrefab != null)
                {
                    SpawnItem(luckyBlockPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.4f, PickupType.ExpMedium, 50);
                }
                else
                {
                    SpawnItem(emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.4f, PickupType.Emerald, 5);
                }
            }
            else
            {
                // Regular Zombie / Mob: 100% small green EXP orb
                SpawnItem(expOrbGreenPrefab, position, PickupType.ExpSmall, 10);

                // Emerald drop rate (5% base for regular mobs, 15% for specialist mobs, x1.5 for Villager in v2.1)
                bool isSpecialist = mobType.Contains("Creeper") || mobType.Contains("Witch") ||
                                    mobType.Contains("Shovel") || mobType.Contains("Slime") ||
                                    mobType.Contains("Evoker") || mobType.Contains("Phantom") ||
                                    mobType.Contains("Ice");

                float emeraldChance = isSpecialist ? 0.15f : 0.05f;
                if (player != null && player.characterId == "Villager")
                {
                    emeraldChance *= 1.5f; // v2.1: Villager x1.5 Emerald drop rate
                }

                if (UnityEngine.Random.value <= emeraldChance)
                {
                    int dropValue = isSpecialist ? UnityEngine.Random.Range(2, 4) : UnityEngine.Random.Range(1, 3);
                    SpawnItem(emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.35f, PickupType.Emerald, dropValue);
                }
            }
        }

        public void SpawnEliteDrops(Vector2 position, Combat.GameDifficulty difficulty)
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();

            // 1. Guaranteed Huge EXP Gem (150 EXP)
            SpawnItem(expOrbYellowPrefab != null ? expOrbYellowPrefab : expOrbGreenPrefab, position, PickupType.ExpLarge, 150);

            // 2. Guaranteed Emerald Burst from Elite Mobs (Normal: 3, Hard: 5, Hardcore: 10)
            int eliteEmeralds = difficulty == Combat.GameDifficulty.Hardcore ? 10 : (difficulty == Combat.GameDifficulty.Hard ? 5 : 3);
            SpawnItem(emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.40f, PickupType.Emerald, eliteEmeralds);

            // 3. Special Drop Table based on GDD Section 4
            float roll = UnityEngine.Random.value;

            switch (difficulty)
            {
                case Combat.GameDifficulty.Normal:
                    // 70% Lucky Block, 30% Cooked Steak (50 HP)
                    if (roll < 0.70f)
                    {
                        SpawnItem(luckyBlockPrefab != null ? luckyBlockPrefab : emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.45f, PickupType.ExpMedium, 50);
                    }
                    else
                    {
                        SpawnItem(steakPrefab != null ? steakPrefab : emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.45f, PickupType.Steak, 50);
                    }
                    break;

                case Combat.GameDifficulty.Hard:
                    // 85% Lucky Block + Emeralds, 15% Super Magnet or TNT Nuke
                    if (roll < 0.85f)
                    {
                        SpawnItem(luckyBlockPrefab != null ? luckyBlockPrefab : emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.45f, PickupType.ExpMedium, 50);
                        SpawnItem(emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.55f, PickupType.Emerald, 5);
                    }
                    else
                    {
                        GameObject special = UnityEngine.Random.value < 0.5f ? magnetPrefab : tntPrefab;
                        PickupType specialType = special == magnetPrefab ? PickupType.Magnet : PickupType.Tnt;
                        SpawnItem(special != null ? special : emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.45f, specialType, 1);
                    }
                    break;

                case Combat.GameDifficulty.Hardcore:
                    // 100% Golden Lucky Block + 10 Emeralds + 50% bonus Emergency Support Item
                    SpawnItem(luckyBlockPrefab != null ? luckyBlockPrefab : emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.45f, PickupType.ExpLarge, 100);
                    SpawnItem(emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.55f, PickupType.Emerald, 10);

                    if (UnityEngine.Random.value < 0.50f)
                    {
                        float bonusRoll = UnityEngine.Random.value;
                        GameObject bonusItem = bonusRoll < 0.34f ? steakPrefab : (bonusRoll < 0.67f ? magnetPrefab : tntPrefab);
                        PickupType bonusType = bonusItem == steakPrefab ? PickupType.Steak : (bonusItem == magnetPrefab ? PickupType.Magnet : PickupType.Tnt);
                        SpawnItem(bonusItem != null ? bonusItem : emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.65f, bonusType, 50);
                    }
                    break;
            }

            Debug.Log($"<color=#F59E0B>[ELITE LOOT]</color> Dropped high-tier rewards at {position} for {difficulty} mode!");
        }

        private GameObject SpawnItem(GameObject prefab, Vector2 position, PickupType type, int val)
        {
            if (prefab == null) return null;
            GameObject itemObj = Instantiate(prefab, position, Quaternion.identity);
            itemObj.SetActive(true);
            PickupItem item = itemObj.GetComponent<PickupItem>();
            if (item != null)
            {
                item.Initialize(type, val);
            }
            return itemObj;
        }

        public void SetTemplates(GameObject expGreen, GameObject expYellow, GameObject emerald, GameObject luckyBlock, GameObject steak = null, GameObject magnet = null, GameObject tnt = null)
        {
            expOrbGreenPrefab = expGreen;
            expOrbYellowPrefab = expYellow;
            emeraldPrefab = emerald;
            luckyBlockPrefab = luckyBlock;
            if (steak != null) steakPrefab = steak;
            if (magnet != null) magnetPrefab = magnet;
            if (tnt != null) tntPrefab = tnt;
        }
    }
}
