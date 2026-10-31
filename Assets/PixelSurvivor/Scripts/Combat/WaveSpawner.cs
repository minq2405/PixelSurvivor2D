using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public enum GameDifficulty
    {
        Normal,
        Hard,
        Hardcore
    }

    /// <summary>
    /// Wave Spawner & Mob Progression Controller (v2.1 Architecture):
    /// - 15-Minute Timeline with 12 Mob Types.
    /// - Controlled Linear HP Formula with Final Surge:
    ///     HP(t, m) = HP_base * H0_m * (1 + s_m * t) * Surge(t)
    /// - Chaser Acceleration (+0.02 m/s/min for Zombie, Husk, ZombieV2, HuskShovel).
    /// - Contact Damage Scaling: DMG(t) = DMG_base * (1 + 0.05 * t) * DamMult_m.
    /// - Global Mob Cap (Normal 150, Hard 180, Hardcore 220) & Summon Sub-Cap (25).
    /// - Elite Budget Rolling every 2s with Guaranteed First Elite and 5 Affixes.
    /// - Golden Lucky Block Chest drops with Pity (+10%) and run caps (10/9/8).
    /// - Special Wave Events: Pincer (1:30 with 60° escape gap), Swarm Rush (4:00, 7:30), Siege (12:30).
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        public static WaveSpawner Instance { get; private set; }

        [Header("Difficulty Mode")]
        [SerializeField] private GameDifficulty difficulty = GameDifficulty.Normal;

        [Header("Elite Enemy System (v2.1 Budget)")]
        [SerializeField] private DifficultyEliteConfigSO normalEliteConfig;
        [SerializeField] private DifficultyEliteConfigSO hardEliteConfig;
        [SerializeField] private DifficultyEliteConfigSO hardcoreEliteConfig;
        [SerializeField] private int currentActiveElites = 0;
        [SerializeField] private GameObject luckyBlockPrefab;

        private float eliteRollTimer = 0f;
        private float nextEliteAvailableTime = 0f;
        private int totalElitesSpawned = 0;
        private float currentChestPity = 0f;
        private int chestsDroppedThisRun = 0;
        private float nextChestAllowedTime = 0f;

        [Header("Summon Exploits Guard")]
        public const int MaxSummonCap = 25;
        [SerializeField] private int currentSummonCount = 0;
        public bool CanSpawnSummon => currentSummonCount < MaxSummonCap;

        [Header("Run Duration (15 Minutes)")]
        [SerializeField] private float totalRunDuration = 900f; // 15 mins = 900s
        [SerializeField] private bool autoStart = true;

        [Header("Spawn Donut Geometry")]
        [SerializeField] private float minSpawnRadius = 12f;
        [SerializeField] private float maxSpawnRadius = 15f;
        [SerializeField] private float spawnInterval = 0.85f;

        [Header("Enemy Templates (12 Enemy Types)")]
        [SerializeField] private GameObject zombieTemplate;
        [SerializeField] private GameObject zombieV2Template;
        [SerializeField] private GameObject huskTemplate;
        [SerializeField] private GameObject spiderTemplate;
        [SerializeField] private GameObject creeperTemplate;
        [SerializeField] private GameObject slimeTemplate;
        [SerializeField] private GameObject witchTemplate;
        [SerializeField] private GameObject huskShovelTemplate;
        [SerializeField] private GameObject iceCreeperTemplate;
        [SerializeField] private GameObject vexTemplate;
        [SerializeField] private GameObject phantomTemplate;
        [SerializeField] private GameObject evokerTemplate;
        [SerializeField] private Sprite zombieWalkSprite;
        [SerializeField] private Sprite[] zombieWalkFrames;

        [Header("Live Run Statistics")]
        [SerializeField] private float timeElapsed = 0f;
        [SerializeField] private int currentMaxMobs = 18;
        [SerializeField] private int currentActiveMobs = 0;
        [SerializeField] private float currentHPMultiplier = 1.0f;

        // Special Events State
        private bool pincerTriggered = false;
        private bool swarmRush1Triggered = false;
        private bool swarmRush2Triggered = false;
        private bool siegeTriggered = false;
        private bool golem1Triggered = false;
        private bool golem2Triggered = false;
        private bool wardenTriggered = false;
        private float bossSpawningDampener = 1.0f;

        public void SetBossSpawningDampener(float dampener)
        {
            bossSpawningDampener = Mathf.Clamp(dampener, 0.1f, 1.0f);
            Debug.Log($"<color=#38BDF8>[WaveSpawner]</color> Boss mob spawn dampener set to {bossSpawningDampener:P0}");
        }

        private Transform playerTransform;
        private PlayerController playerController;
        private float spawnTimer = 0f;
        private readonly List<GameObject> activeEnemies = new List<GameObject>();
        private bool isRunActive = false;

        public float TimeElapsed => timeElapsed;
        public float TotalDuration => totalRunDuration;
        public float RemainingTime => Mathf.Max(0f, totalRunDuration - timeElapsed);
        public bool IsRunActive => isRunActive;
        public int ActiveMobCount => activeEnemies.Count;
        public int CurrentActiveElites => currentActiveElites;
        public int CurrentSummonCount => currentSummonCount;
        public GameDifficulty Difficulty { get => difficulty; set => difficulty = value; }
        public IReadOnlyList<GameObject> ActiveEnemies => activeEnemies;

        public GameObject ZombieTemplate => zombieTemplate;
        public GameObject ZombieV2Template => zombieV2Template;
        public GameObject HuskTemplate => huskTemplate;
        public GameObject SpiderTemplate => spiderTemplate;
        public GameObject CreeperTemplate => creeperTemplate;
        public GameObject SlimeTemplate => slimeTemplate;
        public GameObject WitchTemplate => witchTemplate;
        public GameObject HuskShovelTemplate => huskShovelTemplate;
        public GameObject IceCreeperTemplate => iceCreeperTemplate;
        public GameObject VexTemplate => vexTemplate;
        public GameObject PhantomTemplate => phantomTemplate;
        public GameObject EvokerTemplate => evokerTemplate;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            EnemyHealth.ResetKills();

            // Auto-attach SpatialSeparationSystem (10 Hz Boids)
            if (GetComponent<SpatialSeparationSystem>() == null)
            {
                gameObject.AddComponent<SpatialSeparationSystem>();
            }

#if UNITY_EDITOR
            if (luckyBlockPrefab == null)
            {
                luckyBlockPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelSurvivor/Items/Prefabs/Drop_LuckyBlock.prefab");
            }
#endif
        }

        private void Start()
        {
            FindPlayer();

            if (zombieTemplate == null)
            {
                EnemyHealth dummy = FindAnyObjectByType<EnemyHealth>();
                if (dummy != null) zombieTemplate = dummy.gameObject;
            }

            if (autoStart)
            {
                StartRun();
            }
        }

        public void StartRun()
        {
            isRunActive = true;
            timeElapsed = 0f;
            currentSummonCount = 0;
            currentActiveElites = 0;
            totalElitesSpawned = 0;
            chestsDroppedThisRun = 0;
            currentChestPity = 0f;
            pincerTriggered = false;
            swarmRush1Triggered = false;
            swarmRush2Triggered = false;
            siegeTriggered = false;
            golem1Triggered = false;
            golem2Triggered = false;
            wardenTriggered = false;
            bossSpawningDampener = 1.0f;

            Debug.Log($"<color=#10B981>[WaveSpawner]</color> 15-Minute Roguelite Run Started! Difficulty: {difficulty}");
        }

        public void SetDifficulty(GameDifficulty newDiff)
        {
            difficulty = newDiff;
            Debug.Log($"<color=#38BDF8>[WaveSpawner]</color> Difficulty set to: {difficulty}");
        }

        public void RegisterEnemy(GameObject enemy)
        {
            if (enemy != null && !activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
            }
        }

        public void RegisterSummon()
        {
            currentSummonCount++;
        }

        public void OnSummonDied()
        {
            currentSummonCount = Mathf.Max(0, currentSummonCount - 1);
        }

        public void OnEliteSpawned() => currentActiveElites++;
        public void OnEliteDied() => currentActiveElites = Mathf.Max(0, currentActiveElites - 1);

        public DifficultyEliteConfigSO GetActiveEliteConfig()
        {
            switch (difficulty)
            {
                case GameDifficulty.Hardcore:
                    return hardcoreEliteConfig != null ? hardcoreEliteConfig : normalEliteConfig;
                case GameDifficulty.Hard:
                    return hardEliteConfig != null ? hardEliteConfig : normalEliteConfig;
                default:
                    return normalEliteConfig;
            }
        }

        public void SetEliteConfigs(DifficultyEliteConfigSO normal, DifficultyEliteConfigSO hard, DifficultyEliteConfigSO hardcore)
        {
            normalEliteConfig = normal;
            hardEliteConfig = hard;
            hardcoreEliteConfig = hardcore;
        }

        public int GetGlobalMobCap()
        {
            switch (difficulty)
            {
                case GameDifficulty.Hardcore: return 220;
                case GameDifficulty.Hard: return 180;
                default: return 150;
            }
        }

        public float GetSurgeMultiplier(float minute)
        {
            // Surge(t) = 1.0 + 0.35 * clamp((t - 12) / 3, 0, 1) (v2.1 Section 2.1)
            if (minute < 12f) return 1.0f;
            float surgeProgress = Mathf.Clamp01((minute - 12f) / 3f);
            return 1.0f + (0.35f * surgeProgress);
        }

        public float GetMobHPMultiplier(float minute)
        {
            // HP(t, m) = HP_base * H0_m * (1 + s_m * t) * Surge(t)
            float h0 = 1.00f;
            float s = 0.24f;

            switch (difficulty)
            {
                case GameDifficulty.Hardcore:
                    h0 = 1.22f;
                    s = 0.29f;
                    break;
                case GameDifficulty.Hard:
                    h0 = 1.10f;
                    s = 0.26f;
                    break;
                default:
                    h0 = 1.00f;
                    s = 0.24f;
                    break;
            }

            float surge = GetSurgeMultiplier(minute);
            return h0 * (1.0f + s * minute) * surge;
        }

        public float GetContactDamageMultiplier(float minute)
        {
            // DMG(t) = DMG_base * (1 + 0.05 * t) * DamMult_m
            float diffMult = 1.00f;
            switch (difficulty)
            {
                case GameDifficulty.Hardcore: diffMult = 1.30f; break;
                case GameDifficulty.Hard: diffMult = 1.15f; break;
                default: diffMult = 1.00f; break;
            }

            return (1.0f + 0.05f * minute) * diffMult;
        }

        public float GetSpeedMultiplier()
        {
            switch (difficulty)
            {
                case GameDifficulty.Hardcore: return 1.10f;
                case GameDifficulty.Hard: return 1.05f;
                default: return 1.00f;
            }
        }

        private int CalculateTargetDensity(float minute)
        {
            // Timeline: [[0,18],[3,38],[6,72],[10,110],[14,140],[15,150]]
            float baseDensity = 18f;
            if (minute <= 3f) baseDensity = Mathf.Lerp(18f, 38f, minute / 3f);
            else if (minute <= 6f) baseDensity = Mathf.Lerp(38f, 72f, (minute - 3f) / 3f);
            else if (minute <= 10f) baseDensity = Mathf.Lerp(72f, 110f, (minute - 6f) / 4f);
            else if (minute <= 14f) baseDensity = Mathf.Lerp(110f, 140f, (minute - 10f) / 4f);
            else baseDensity = Mathf.Lerp(140f, 150f, (minute - 14f) / 1f);

            float diffMult = difficulty == GameDifficulty.Hardcore ? 1.25f : (difficulty == GameDifficulty.Hard ? 1.12f : 1.0f);
            int target = Mathf.RoundToInt(baseDensity * diffMult);
            return Mathf.Min(target, GetGlobalMobCap());
        }

        private void Update()
        {
            if (!isRunActive) return;

            timeElapsed += Time.deltaTime;

            if (timeElapsed >= totalRunDuration)
            {
                OnRunTimeCompleted();
                return;
            }

            // Clean destroyed mobs
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    activeEnemies.RemoveAt(i);
                }
            }

            currentActiveMobs = activeEnemies.Count;
            float t = timeElapsed / 60f; // Minute float

            // Update Density Cap & HP Multipliers
            currentMaxMobs = CalculateTargetDensity(t);
            currentHPMultiplier = GetMobHPMultiplier(t);

            // Check Special Events (v2.1 Section 11)
            CheckSpecialEvents(timeElapsed, t);

            // Check Elite Spawn Roll every 2 seconds
            eliteRollTimer += Time.deltaTime;
            if (eliteRollTimer >= 2.0f)
            {
                eliteRollTimer = 0f;
                CheckEliteRoll(timeElapsed);
            }

            // Normal Donut Spawning
            float baseIntervalTarget = difficulty == GameDifficulty.Hardcore ? 0.22f : (difficulty == GameDifficulty.Hard ? 0.28f : 0.35f);
            float currentInterval = Mathf.Lerp(spawnInterval, baseIntervalTarget, Mathf.Clamp01(t / 14f));
            if (bossSpawningDampener > 0f) currentInterval /= bossSpawningDampener;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentInterval)
            {
                spawnTimer = 0f;
                if (activeEnemies.Count < currentMaxMobs && activeEnemies.Count < GetGlobalMobCap())
                {
                    SpawnNextEnemy(t);
                }
            }
        }

        private void CheckSpecialEvents(float seconds, float minute)
        {
            // 1. Pincer Event (at 1:30 = 90s)
            if (!pincerTriggered && seconds >= 90f)
            {
                pincerTriggered = true;
                StartCoroutine(ExecutePincerEvent());
            }

            // 2. Swarm Rush 1 (at 4:00 = 240s)
            if (!swarmRush1Triggered && seconds >= 240f)
            {
                swarmRush1Triggered = true;
                ExecuteSwarmRush("Spider", 15, minute);
            }

            // 3. Nether Golem I Mini-Boss (at 5:00 = 300s)
            if (!golem1Triggered && seconds >= 300f)
            {
                golem1Triggered = true;
                SpawnNetherGolem(1);
            }

            // 4. Swarm Rush 2 (at 7:30 = 450s)
            if (!swarmRush2Triggered && seconds >= 450f)
            {
                swarmRush2Triggered = true;
                ExecuteSwarmRush("Zombie", 18, minute);
            }

            // 5. Nether Golem II Enraged Mini-Boss (at 10:00 = 600s)
            if (!golem2Triggered && seconds >= 600f)
            {
                golem2Triggered = true;
                SpawnNetherGolem(2);
            }

            // 6. Siege Event (at 12:30 = 750s)
            if (!siegeTriggered && seconds >= 750f)
            {
                siegeTriggered = true;
                ExecuteSiegeWave(minute);
            }

            // 7. Final Boss: The Warden (at 15:00 = 900s)
            if (!wardenTriggered && seconds >= 900f)
            {
                wardenTriggered = true;
                SpawnTheWarden();
            }
        }

        private System.Collections.IEnumerator ExecutePincerEvent()
        {
            FindPlayer();
            if (playerTransform == null) yield break;

            Debug.Log("<color=#EF4444>[EVENT: PINCER]</color> ⚠️ PINCER AMBUSH DETECTED! 1.5s Telegraph!");
            DamageNumber.SpawnText(playerTransform.position + Vector3.up * 1.5f, "⚠️ PINCER AMBUSH (1.5s) ⚠️", Color.red);

            // 1.5s Telegraph delay
            yield return new WaitForSeconds(1.5f);
            if (playerTransform == null) yield break;

            int count = difficulty == GameDifficulty.Hardcore ? 8 : (difficulty == GameDifficulty.Hard ? 6 : 4);
            float radius = 4.5f;

            // Pick a random escape direction angle (60° escape gap)
            float escapeAngle = Random.Range(0f, 360f);
            float startAngle = escapeAngle + 30f;
            float arcSpan = 300f; // 360 - 60 = 300 degrees covered by mobs
            float step = arcSpan / Mathf.Max(1, count - 1);

            for (int i = 0; i < count; i++)
            {
                float ang = (startAngle + i * step) * Mathf.Deg2Rad;
                Vector3 spawnPos = playerTransform.position + new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f);
                GameObject mob = SpawnSingleMobDirect(zombieTemplate, "Zombie", spawnPos, 1.5f);
                if (mob != null) activeEnemies.Add(mob);
            }

            Debug.Log($"<color=#10B981>[EVENT: PINCER]</color> Spawned {count} Zombies in 4.5m ring with 60° escape route!");
        }

        private void ExecuteSwarmRush(string mobType, int count, float minute)
        {
            FindPlayer();
            if (playerTransform == null) return;

            Debug.Log($"<color=#F59E0B>[EVENT: SWARM RUSH]</color> ⚡ SWARM RUSH DETECTED: {count} {mobType}s! ⚡");
            DamageNumber.SpawnText(playerTransform.position + Vector3.up * 1.5f, $"⚡ SWARM RUSH ({mobType})! ⚡", Color.yellow);

            GameObject template = (mobType == "Spider" && spiderTemplate != null) ? spiderTemplate : zombieTemplate;

            Vector2 forwardDir = playerController != null ? playerController.FacingDirection : Vector2.right;

            for (int i = 0; i < count; i++)
            {
                float spreadAngle = Random.Range(-45f, 45f);
                Vector2 dir = Quaternion.Euler(0, 0, spreadAngle) * forwardDir;
                float dist = Random.Range(10f, 13f);
                Vector3 spawnPos = playerTransform.position + (Vector3)(dir * dist);

                GameObject mob = SpawnSingleMobDirect(template, mobType, spawnPos, minute);
                if (mob != null) activeEnemies.Add(mob);
            }
        }

        private void ExecuteSiegeWave(float minute)
        {
            FindPlayer();
            if (playerTransform == null) return;

            Debug.Log("<color=#EF4444>[EVENT: SIEGE]</color> 🛡️ ENEMY SIEGE WAVE! Vanguard deployed! 🛡️");
            DamageNumber.SpawnText(playerTransform.position + Vector3.up * 1.5f, "🛡️ ENEMY SIEGE WAVE! 🛡️", Color.red);

            GameObject[] heavyTemplates = new GameObject[] { huskShovelTemplate, evokerTemplate, witchTemplate, iceCreeperTemplate };

            for (int i = 0; i < heavyTemplates.Length; i++)
            {
                if (heavyTemplates[i] == null) continue;
                Vector2 dir = Random.insideUnitCircle.normalized;
                Vector3 spawnPos = playerTransform.position + (Vector3)(dir * 11.5f);
                GameObject mob = SpawnSingleMobDirect(heavyTemplates[i], heavyTemplates[i].name, spawnPos, minute);
                if (mob != null) activeEnemies.Add(mob);
            }
        }

        public GameObject SpawnNetherGolem(int tier)
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.up * 8f : Vector3.up * 8f;

            GameObject golem = new GameObject($"NetherGolem_Tier{tier}");
            golem.transform.position = spawnPos;

            var sr = golem.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 12;

            var rb = golem.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = golem.AddComponent<CircleCollider2D>();
            col.radius = 0.8f;

            var eh = golem.AddComponent<EnemyHealth>();
            eh.IsBoss = true;

            var boss = golem.AddComponent<NetherGolemBoss>();

#if UNITY_EDITOR
            Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Redstone_Golem/redstone_golem_walk.png");
            List<Sprite> walkList = new List<Sprite>();
            foreach (var a in allAssets) if (a is Sprite s) walkList.Add(s);

            Object[] atkAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Redstone_Golem/redstone_golem_attack.png");
            List<Sprite> atkList = new List<Sprite>();
            foreach (var a in atkAssets) if (a is Sprite s) atkList.Add(s);

            boss.Initialize(tier, walkList.ToArray(), atkList.ToArray());
            if (walkList.Count > 0) sr.sprite = walkList[0];
#endif
            activeEnemies.Add(golem);

            string title = tier == 1 ? "NETHER GOLEM I" : "NETHER GOLEM II";
            Debug.Log($"<color=#EF4444>[BOSS SPAWN]</color> {title} has emerged onto the battlefield!");
            DamageNumber.SpawnText(spawnPos + Vector3.up * 2f, $"👑 {title} HAS AWAKENED! 👑", Color.red);
            return golem;
        }

        public GameObject SpawnTheWarden()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.up * 9f : Vector3.up * 9f;

            GameObject warden = new GameObject("TheWarden_Boss");
            warden.transform.position = spawnPos;

            var sr = warden.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 15;

            var rb = warden.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = warden.AddComponent<CircleCollider2D>();
            col.radius = 1.1f;

            var eh = warden.AddComponent<EnemyHealth>();
            eh.IsBoss = true;

            var boss = warden.AddComponent<WardenBoss>();

#if UNITY_EDITOR
            Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Warden/warden_walk.png");
            List<Sprite> walkList = new List<Sprite>();
            foreach (var a in allAssets) if (a is Sprite s) walkList.Add(s);

            Object[] atkAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Warden/warden_attack.png");
            List<Sprite> atkList = new List<Sprite>();
            foreach (var a in atkAssets) if (a is Sprite s) atkList.Add(s);

            boss.Initialize(walkList.ToArray(), atkList.ToArray());
            if (walkList.Count > 0) sr.sprite = walkList[0];
#endif
            activeEnemies.Add(warden);

            Debug.Log("<color=#06B6D4>[FINAL BOSS]</color> THE WARDEN has descended from the ancient depths!");
            DamageNumber.SpawnText(spawnPos + Vector3.up * 2.5f, "💀 THE WARDEN HAS AWAKENED! 💀", Color.cyan);
            return warden;
        }

        private void CheckEliteRoll(float seconds)
        {
            var config = GetActiveEliteConfig();
            if (config == null) return;

            // Check unlock time
            if (seconds < config.unlockTimeInSeconds) return;

            // Check on-screen cap
            if (currentActiveElites >= config.maxElitesOnScreen) return;

            // Check cooldown
            if (Time.time < nextEliteAvailableTime) return;

            // Guaranteed first Elite
            bool isFirstElite = (totalElitesSpawned == 0);
            bool rollSuccess = isFirstElite || (Random.value < config.eliteChance);

            if (rollSuccess)
            {
                nextEliteAvailableTime = Time.time + config.eliteCooldown;
                totalElitesSpawned++;
                TransformRandomEnemyToElite(config);
            }
        }

        private void TransformRandomEnemyToElite(DifficultyEliteConfigSO config)
        {
            // Pick a suitable enemy near player to transform
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                var mob = activeEnemies[i];
                if (mob == null || !mob.activeInHierarchy) continue;
                if (mob.GetComponent<EliteEnemyAffixController>() != null) continue;

                var eliteCtrl = mob.AddComponent<EliteEnemyAffixController>();
                eliteCtrl.InitializeElite(config);
                OnEliteSpawned();
                Debug.Log($"<color=#A855F7>[ELITE SPAWNED]</color> {mob.name} transformed into Elite! ({currentActiveElites}/{config.maxElitesOnScreen})");
                return;
            }

            // Fallback: spawn a new elite directly
            ForceSpawnEliteEnemy(zombieTemplate);
        }

        public bool TryRollEliteChest(Vector3 position)
        {
            var config = GetActiveEliteConfig();
            if (config == null) return false;

            if (chestsDroppedThisRun >= config.maxChestsPerRun)
            {
                Debug.Log($"[WaveSpawner] Elite chest cap reached ({chestsDroppedThisRun}/{config.maxChestsPerRun})!");
                return false;
            }

            if (Time.time < nextChestAllowedTime)
            {
                return false;
            }

            float finalChance = config.chestBaseChance + currentChestPity;
            if (Random.value < finalChance)
            {
                // Chest SUCCESS!
                chestsDroppedThisRun++;
                currentChestPity = 0f;
                nextChestAllowedTime = Time.time + config.chestMinGap;

                if (luckyBlockPrefab != null)
                {
                    Instantiate(luckyBlockPrefab, position, Quaternion.identity);
                    Debug.Log($"<color=#F59E0B>[CHEST DROP SUCCESS]</color> Golden Lucky Block dropped! (Total: {chestsDroppedThisRun}/{config.maxChestsPerRun})");
                }
                return true;
            }
            else
            {
                // Chest PITY increment (+10%)
                currentChestPity += config.chestPityStep;
                Debug.Log($"<color=#38BDF8>[CHEST PITY]</color> Chest roll missed. Pity increased to +{currentChestPity * 100:F0}%");
                return false;
            }
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                playerController = pc;
                playerTransform = pc.transform;
            }
        }

        private void GetMobStats(string mobType, float minute, out float baseHp, out float speed, out float dmg)
        {
            float spdMult = GetSpeedMultiplier();
            float dmgMult = GetContactDamageMultiplier(minute);

            bool isChaser = (mobType == "Zombie" || mobType == "Husk" || mobType == "ZombieV2" || mobType == "HuskShovel");

            switch (mobType)
            {
                case "Spider":
                    baseHp = 30f;
                    speed = 4.5f * spdMult;
                    dmg = 8f * dmgMult;
                    break;
                case "Vex":
                    baseHp = 25f;
                    speed = 4.8f * spdMult;
                    dmg = 10f * dmgMult;
                    break;
                case "Phantom":
                    baseHp = 35f;
                    speed = 4.4f * spdMult;
                    dmg = 12f * dmgMult;
                    break;
                case "Creeper":
                    baseHp = 60f;
                    speed = 3.4f * spdMult;
                    dmg = 10f * dmgMult;
                    break;
                case "IceCreeper":
                    baseHp = 65f;
                    speed = 3.4f * spdMult;
                    dmg = 10f * dmgMult;
                    break;
                case "Slime":
                    baseHp = 60f;
                    speed = 3.2f * spdMult;
                    dmg = 8f * dmgMult;
                    break;
                case "Witch":
                    baseHp = 40f;
                    speed = 3.3f * spdMult;
                    dmg = 10f * dmgMult;
                    break;
                case "HuskShovel":
                    baseHp = 85f;
                    speed = (3.6f + 0.02f * minute) * spdMult;
                    dmg = 14f * dmgMult;
                    break;
                case "Evoker":
                    baseHp = 150f;
                    speed = 3.2f * spdMult;
                    dmg = 12f * dmgMult;
                    break;
                case "ZombieV2":
                    baseHp = 65f;
                    speed = (3.6f + 0.02f * minute) * spdMult;
                    dmg = 8f * dmgMult;
                    break;
                case "Husk":
                    baseHp = 75f;
                    speed = (3.5f + 0.02f * minute) * spdMult;
                    dmg = 9f * dmgMult;
                    break;
                default: // Zombie
                    baseHp = 50f;
                    speed = (3.5f + 0.02f * minute) * spdMult;
                    dmg = 6f * dmgMult;
                    break;
            }
        }

        private void PickZombieVariant(float minute, out GameObject template, out string prefix, out float hp, out float speed, out float dmg)
        {
            float roll = Random.value;
            if (roll < 0.25f && zombieV2Template != null)
            {
                template = zombieV2Template;
                prefix = "ZombieV2";
                GetMobStats(prefix, minute, out hp, out speed, out dmg);
            }
            else if (roll < 0.50f && huskTemplate != null)
            {
                template = huskTemplate;
                prefix = "Husk";
                GetMobStats(prefix, minute, out hp, out speed, out dmg);
            }
            else
            {
                template = zombieTemplate;
                prefix = "Zombie";
                GetMobStats(prefix, minute, out hp, out speed, out dmg);
            }
        }

        private void SpawnNextEnemy(float minute)
        {
            FindPlayer();
            if (playerTransform == null) return;

            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 spawnPos = playerTransform.position + (Vector3)(randomDir * randomDist);

            GameObject templateToSpawn = zombieTemplate;
            string mobPrefix = "Zombie";
            float baseHp = 50f, speed = 3.5f, dmg = 6f;

            if (minute < 2f)
            {
                PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out speed, out dmg);
            }
            else if (minute < 4f)
            {
                float roll = Random.value;
                if (roll < 0.35f && spiderTemplate != null)
                {
                    templateToSpawn = spiderTemplate;
                    mobPrefix = "Spider";
                    GetMobStats(mobPrefix, minute, out baseHp, out speed, out dmg);
                }
                else
                {
                    PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out speed, out dmg);
                }
            }
            else if (minute < 7f)
            {
                float roll = Random.value;
                if (roll < 0.20f && creeperTemplate != null) { templateToSpawn = creeperTemplate; mobPrefix = "Creeper"; }
                else if (roll < 0.35f && slimeTemplate != null) { templateToSpawn = slimeTemplate; mobPrefix = "Slime"; }
                else if (roll < 0.60f && spiderTemplate != null) { templateToSpawn = spiderTemplate; mobPrefix = "Spider"; }
                else PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out speed, out dmg);

                if (mobPrefix != "Zombie" && mobPrefix != "ZombieV2" && mobPrefix != "Husk")
                {
                    GetMobStats(mobPrefix, minute, out baseHp, out speed, out dmg);
                }
            }
            else if (minute < 10f)
            {
                float roll = Random.value;
                if (roll < 0.18f && witchTemplate != null) { templateToSpawn = witchTemplate; mobPrefix = "Witch"; }
                else if (roll < 0.36f && iceCreeperTemplate != null) { templateToSpawn = iceCreeperTemplate; mobPrefix = "IceCreeper"; }
                else if (roll < 0.54f && huskShovelTemplate != null) { templateToSpawn = huskShovelTemplate; mobPrefix = "HuskShovel"; }
                else if (roll < 0.72f && spiderTemplate != null) { templateToSpawn = spiderTemplate; mobPrefix = "Spider"; }
                else PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out speed, out dmg);

                if (mobPrefix != "Zombie" && mobPrefix != "ZombieV2" && mobPrefix != "Husk")
                {
                    GetMobStats(mobPrefix, minute, out baseHp, out speed, out dmg);
                }
            }
            else
            {
                float roll = Random.value;
                if (roll < 0.18f && vexTemplate != null) { templateToSpawn = vexTemplate; mobPrefix = "Vex"; }
                else if (roll < 0.34f && phantomTemplate != null) { templateToSpawn = phantomTemplate; mobPrefix = "Phantom"; }
                else if (roll < 0.44f && evokerTemplate != null) { templateToSpawn = evokerTemplate; mobPrefix = "Evoker"; }
                else if (roll < 0.60f && iceCreeperTemplate != null) { templateToSpawn = iceCreeperTemplate; mobPrefix = "IceCreeper"; }
                else if (roll < 0.75f && spiderTemplate != null) { templateToSpawn = spiderTemplate; mobPrefix = "Spider"; }
                else PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out speed, out dmg);

                if (mobPrefix != "Zombie" && mobPrefix != "ZombieV2" && mobPrefix != "Husk")
                {
                    GetMobStats(mobPrefix, minute, out baseHp, out speed, out dmg);
                }
            }

            GameObject newEnemy = SpawnSingleMobDirect(templateToSpawn, mobPrefix, spawnPos, minute);
            if (newEnemy != null)
            {
                activeEnemies.Add(newEnemy);
            }
        }

        private GameObject SpawnSingleMobDirect(GameObject template, string mobPrefix, Vector3 spawnPos, float minute)
        {
            if (template == null) return null;

            GameObject newEnemy = Instantiate(template, spawnPos, Quaternion.identity);
            newEnemy.name = $"Mob_{mobPrefix}_{activeEnemies.Count + 1}";
            newEnemy.SetActive(true);

            GetMobStats(mobPrefix, minute, out float baseHp, out float speed, out float dmg);
            float finalHp = baseHp * GetMobHPMultiplier(minute);

            EnemyHealth eh = newEnemy.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                eh.SetMaxHealth(finalHp);
                eh.SetInvincible(false);
            }

            CircleCollider2D col = newEnemy.GetComponent<CircleCollider2D>();
            if (col != null) col.isTrigger = true;

            EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.MoveSpeed = speed;
                ai.ContactDamage = dmg;
            }

            return newEnemy;
        }

        public GameObject ForceSpawnEnemy(GameObject template, Vector3? customPos = null)
        {
            if (template == null)
            {
                Debug.LogWarning("[WaveSpawner] ForceSpawnEnemy: template is null!");
                return null;
            }

            FindPlayer();
            Vector3 spawnPos = customPos.HasValue 
                ? customPos.Value 
                : (playerTransform != null ? playerTransform.position + Vector3.right * 2.5f : transform.position + Vector3.right * 2.5f);

            GameObject newEnemy = SpawnSingleMobDirect(template, template.name, spawnPos, timeElapsed / 60f);
            if (newEnemy != null)
            {
                newEnemy.name = $"Debug_{template.name}_{activeEnemies.Count + 1}";
                activeEnemies.Add(newEnemy);
                Debug.Log($"<color=#10B981>[WaveSpawner]</color> Force spawned {newEnemy.name} at {spawnPos}");
            }
            return newEnemy;
        }

        public void ForceSpawnEliteEnemy(GameObject template = null)
        {
            if (template == null) template = zombieTemplate;
            if (template == null) return;

            FindPlayer();
            Vector3 spawnPos = playerTransform != null ? playerTransform.position + Vector3.right * 2.5f : Vector3.right * 2.5f;
            GameObject mob = Instantiate(template, spawnPos, Quaternion.identity);
            mob.name = $"Elite_{template.name}_{activeEnemies.Count + 1}";
            mob.SetActive(true);

            float t = timeElapsed / 60f;
            GetMobStats("Zombie", t, out float baseHp, out float speed, out float dmg);
            float finalHp = baseHp * GetMobHPMultiplier(t);

            EnemyHealth eh = mob.GetComponent<EnemyHealth>();
            if (eh != null) eh.SetMaxHealth(finalHp);

            EnemyAI ai = mob.GetComponent<EnemyAI>();
            if (ai != null) { ai.MoveSpeed = speed; ai.ContactDamage = dmg; }

            var eliteCtrl = mob.AddComponent<EliteEnemyAffixController>();
            eliteCtrl.InitializeElite(GetActiveEliteConfig());
            OnEliteSpawned();
            activeEnemies.Add(mob);
        }

        public void JumpToTime(float minute)
        {
            timeElapsed = Mathf.Clamp(minute * 60f, 0f, totalRunDuration);
            Debug.Log($"<color=#38BDF8>[WaveSpawner]</color> Jumped run time to {minute:F1} minutes ({timeElapsed:F0}s)!");
        }

        public void ClearAllEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null) Destroy(activeEnemies[i]);
            }
            activeEnemies.Clear();
            currentSummonCount = 0;
            currentActiveElites = 0;
            Debug.Log("<color=#F59E0B>[WaveSpawner]</color> Cleared all active enemies!");
        }

        public void KillAllActiveEnemies()
        {
            int killed = 0;
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                {
                    var eh = activeEnemies[i].GetComponent<EnemyHealth>();
                    if (eh != null && eh.IsAlive)
                    {
                        eh.TakeDamage(99999f, Vector2.zero, 0f);
                        killed++;
                    }
                }
            }
            Debug.Log($"<color=#10B981>[WaveSpawner]</color> Executed kill command on {killed} mobs!");
        }

        private void OnRunTimeCompleted()
        {
            isRunActive = false;
            Debug.Log("<color=#FACC15>[WaveSpawner]</color> 15 Minutes reached! Spawning Final Boss The Warden!");
        }
    }
}
