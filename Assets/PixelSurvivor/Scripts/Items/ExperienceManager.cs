using System;
using UnityEngine;
using PixelSurvivor.UI;

namespace PixelSurvivor
{
    /// <summary>
    /// Central manager for:
    /// - Level & EXP Progression (calculating level thresholds, dispatching events to HUD)
    /// - In-Run Emerald Economy (tracking session emeralds used for rerolls, zero DB)
    /// - Item Drop spawning upon enemy death
    /// - Special item effects (Super Magnet, TNT Nuke)
    /// </summary>
    public class ExperienceManager : MonoBehaviour
    {
        public static ExperienceManager Instance { get; private set; }

        [Header("Level & EXP Progression")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentExp = 0;
        [SerializeField] private int expToNextLevel = 50;

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
        }

        public int CalculateExpRequirement(int level)
        {
            // Smooth Roguelite EXP curve:
            // Lv 1: 50 EXP (5 small orbs)
            // Lv 2: 90 EXP
            // Lv 3: 140 EXP
            // Lv 4: 200 EXP
            return Mathf.RoundToInt(50 + 35 * (level - 1) + 5 * (level - 1) * (level - 1));
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            currentExp += amount;
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

        public void DetonateTnt(Vector2 position)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, 8f);
            int count = 0;
            for (int i = 0; i < hits.Length; i++)
            {
                var damageable = hits[i].GetComponent<Combat.IDamageable>();
                if (damageable != null && damageable.IsAlive && hits[i].gameObject.name.Contains("Mob"))
                {
                    damageable.TakeDamage(500f, (hits[i].transform.position - (Vector3)position).normalized, 12f);
                    count++;
                }
            }
            Debug.Log($"<color=#EF4444>[TNT NUKE]</color> Detonated at {position}, hit {count} enemies for 500 damage!");
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

                // Emerald drop rate (2% base, 6% Villager)
                float emeraldChance = (player != null && player.characterId == "Villager") ? 0.06f : 0.02f;
                if (UnityEngine.Random.value <= emeraldChance)
                {
                    SpawnItem(emeraldPrefab, position + (Vector2)UnityEngine.Random.insideUnitCircle * 0.35f, PickupType.Emerald, 1);
                }
            }
        }

        public void SpawnEliteDrops(Vector2 position, Combat.GameDifficulty difficulty)
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();

            // 1. Guaranteed Huge EXP Gem (150 EXP)
            SpawnItem(expOrbYellowPrefab != null ? expOrbYellowPrefab : expOrbGreenPrefab, position, PickupType.ExpLarge, 150);

            // 2. Special Drop Table based on GDD Section 4
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
