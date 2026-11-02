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

        [Header("Run Duration (18 Minutes Total, Final Boss at 15m)")]
        [SerializeField] private float totalRunDuration = 1080f; // 18 mins = 1080s (Final Boss at 15m = 900s)
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
        private bool golem1Warned = false;
        private bool golem1Triggered = false;
        private bool golem2Warned = false;
        private bool golem2Triggered = false;
        private bool wardenWarned = false;
        private bool wardenTriggered = false;
        private float bossSpawningDampener = 1.0f;
        private bool isSpawningPaused = false;

        [Header("Mini-Boss & Final Boss Randomizer")]
        private int firstMiniBossIndex = -1;
        private int secondMiniBossIndex = -1;
        private int finalBossIndex = -1;

        public enum MiniBossType
        {
            NetherGolem = 0,
            MutantZombie = 1,
            CorruptedIronGolem = 2,
            PhantomLord = 3,
            PiglinBrute = 4,
            Ravager = 5,
            WitherKing = 6,
            PillagerCaptain = 7
        }

        public enum FinalBossType
        {
            TheWarden = 0,
            GrandWitherKing = 1,
            GrandPillagerCaptain = 2,
            MutantColossus = 3,
            CorruptedTitanGolem = 4
        }

        public bool IsSpawningPaused => isSpawningPaused;

        public void SetSpawningPaused(bool paused)
        {
            isSpawningPaused = paused;
            Debug.Log($"<color=#38BDF8>[WaveSpawner]</color> Mob spawning paused: {paused}");
        }

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
        public float CurrentMinute => timeElapsed / 60f;
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

            if (totalRunDuration < 1080f)
            {
                totalRunDuration = 1080f;
            }

            // Auto-attach SpatialSeparationSystem (10 Hz Boids)
            if (GetComponent<SpatialSeparationSystem>() == null)
            {
                gameObject.AddComponent<SpatialSeparationSystem>();
            }

            if (luckyBlockPrefab == null)
            {
                luckyBlockPrefab = Resources.Load<GameObject>("Prefabs/Drop_LuckyBlock");
#if UNITY_EDITOR
                if (luckyBlockPrefab == null)
                {
                    luckyBlockPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelSurvivor/Items/Prefabs/Drop_LuckyBlock.prefab");
                }
#endif
            }
        }

        private void Start()
        {
            FindPlayer();

            if (zombieTemplate == null)
            {
                EnemyHealth dummy = FindAnyObjectByType<EnemyHealth>();
                if (dummy != null) zombieTemplate = dummy.gameObject;
            }

            // Only auto-start if there is NO MainMenuManager in the scene (e.g. standalone combat testbed).
            // When MainMenuManager is present, the match will begin cleanly when the player clicks New Game & confirms selection.
            var menu = FindAnyObjectByType<MainMenuManager>();
            if (autoStart && menu == null)
            {
                StartRun();
            }
        }

        public void StartRun()
        {
            if (totalRunDuration < 1080f)
            {
                totalRunDuration = 1080f;
            }
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
            golem1Warned = false;
            golem1Triggered = false;
            golem2Warned = false;
            golem2Triggered = false;
            wardenWarned = false;
            wardenTriggered = false;
            firstMiniBossIndex = -1;
            secondMiniBossIndex = -1;
            finalBossIndex = -1;
            isSpawningPaused = false;
            bossSpawningDampener = 1.0f;

            Debug.Log($"<color=#10B981>[WaveSpawner]</color> 15-Minute Roguelite Run Started! Difficulty: {difficulty}");
        }

        public void StopRun()
        {
            isRunActive = false;
            timeElapsed = 0f;
            ClearAllEnemies();
            Debug.Log("<color=#EF4444>[WaveSpawner]</color> Run stopped and timer reset to 00:00.");
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

            // Clean destroyed mobs
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    activeEnemies.RemoveAt(i);
                }
            }

            currentActiveMobs = activeEnemies.Count;

            // Minute float for scaling formulas (capped at 15.0m so mob stats remain balanced during Warden fight)
            float t = Mathf.Min(timeElapsed, 900f) / 60f;

            // Update Density Cap & HP Multipliers
            currentMaxMobs = CalculateTargetDensity(t);
            currentHPMultiplier = GetMobHPMultiplier(t);

            // Check Special Events (v2.1 Section 11)
            CheckSpecialEvents(timeElapsed, t);

            // Check 18-minute match timeout (1080s): Immediate defeat if Final Boss not defeated within 3 minutes!
            if (timeElapsed >= Mathf.Max(1080f, totalRunDuration))
            {
                OnRunTimeCompleted();
                return;
            }

            // If Boss Arena Lockdown is active, skip regular mob spawning and elite rolls
            if (isSpawningPaused) return;

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

            // 3a. Mini-Boss 1 Warning (at 4:55 = 295s)
            if (!golem1Warned && seconds >= 295f)
            {
                golem1Warned = true;
                BossArenaManager.Instance?.TriggerWarning(isGrandBoss: false, duration: 5f);
            }

            // 3b. Mini-Boss 1 Random Encounter (at 5:00 = 300s)
            if (!golem1Triggered && seconds >= 300f)
            {
                golem1Triggered = true;
                SpawnRandomMiniBoss(1);
            }

            // 4. Swarm Rush 2 (at 7:30 = 450s)
            if (!swarmRush2Triggered && seconds >= 450f)
            {
                swarmRush2Triggered = true;
                ExecuteSwarmRush("Zombie", 18, minute);
            }

            // 5a. Mini-Boss 2 Warning (at 9:55 = 595s)
            if (!golem2Warned && seconds >= 595f)
            {
                golem2Warned = true;
                BossArenaManager.Instance?.TriggerWarning(isGrandBoss: false, duration: 5f);
            }

            // 5b. Mini-Boss 2 Enraged Encounter (at 10:00 = 600s)
            if (!golem2Triggered && seconds >= 600f)
            {
                golem2Triggered = true;
                SpawnRandomMiniBoss(2);
            }

            // 6. Siege Event (at 12:30 = 750s)
            if (!siegeTriggered && seconds >= 750f)
            {
                siegeTriggered = true;
                ExecuteSiegeWave(minute);
            }

            // 7a. Final Boss Warning (at 14:55 = 895s)
            if (!wardenWarned && seconds >= 895f)
            {
                wardenWarned = true;
                BossArenaManager.Instance?.TriggerWarning(isGrandBoss: true, duration: 5f);
            }

            // 7b. Grand Final Boss Random Encounter (at 15:00 = 900s)
            if (!wardenTriggered && seconds >= 900f)
            {
                wardenTriggered = true;
                SpawnRandomFinalBoss();
            }
        }

        private System.Collections.IEnumerator ExecutePincerEvent()
        {
            FindPlayer();
            if (playerTransform == null) yield break;

            Debug.Log("<color=#EF4444>[EVENT: PINCER]</color> ⚠️ PINCER AMBUSH DETECTED! 1.5s Telegraph!");

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

                #region Mini-Boss & Grand Boss Spawning

        public GameObject SpawnRandomMiniBoss(int tier)
        {
            List<int> pool = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
            // Tier 2 (10:00) avoids repeating the same mini-boss as Tier 1 (5:00)
            if (tier >= 2 && firstMiniBossIndex >= 0)
            {
                pool.Remove(firstMiniBossIndex);
            }

            int roll = pool[Random.Range(0, pool.Count)];
            if (tier == 1) firstMiniBossIndex = roll;
            else secondMiniBossIndex = roll;

            return SpawnMiniBoss((MiniBossType)roll, tier);
        }

        public GameObject SpawnMiniBoss(MiniBossType type, int tier)
        {
            GameObject bossGo = null;
            switch (type)
            {
                case MiniBossType.NetherGolem:
                    bossGo = SpawnNetherGolem(tier);
                    break;
                case MiniBossType.MutantZombie:
                    bossGo = SpawnMutantZombie();
                    break;
                case MiniBossType.CorruptedIronGolem:
                    bossGo = SpawnCorruptedIronGolem();
                    break;
                case MiniBossType.PhantomLord:
                    bossGo = SpawnPhantomLord();
                    break;
                case MiniBossType.PiglinBrute:
                    bossGo = SpawnPiglinBrute();
                    break;
                case MiniBossType.Ravager:
                    bossGo = SpawnRavager();
                    break;
                case MiniBossType.WitherKing:
                    bossGo = SpawnWitherKing();
                    break;
                case MiniBossType.PillagerCaptain:
                    bossGo = SpawnPillagerCaptain(isGrandBoss: false);
                    break;
            }

            // Tier 2 (10:00) mid-game progression stat scaling
            if (tier >= 2 && type != MiniBossType.NetherGolem && bossGo != null)
            {
                var eh = bossGo.GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    eh.SetMaxHealth(eh.MaxHealth * 1.30f);
                }
            }

            Debug.Log($"<color=#FACC15>[RANDOM MINI-BOSS]</color> Tier {tier} Mini-Boss Emerged: {type} (HP: {bossGo?.GetComponent<EnemyHealth>()?.MaxHealth:N0})");
            return bossGo;
        }

        public GameObject SpawnRandomFinalBoss()
        {
            int roll = Random.Range(0, 5);
            finalBossIndex = roll;
            return SpawnFinalBoss((FinalBossType)roll);
        }

        public GameObject SpawnFinalBoss(FinalBossType type)
        {
            GameObject bossGo = null;
            switch (type)
            {
                case FinalBossType.TheWarden:
                    bossGo = SpawnTheWarden();
                    break;
                case FinalBossType.GrandWitherKing:
                    bossGo = SpawnGrandWitherKing();
                    break;
                case FinalBossType.GrandPillagerCaptain:
                    bossGo = SpawnGrandPillagerCaptain();
                    break;
                case FinalBossType.MutantColossus:
                    bossGo = SpawnGrandMutantZombie();
                    break;
                case FinalBossType.CorruptedTitanGolem:
                    bossGo = SpawnGrandCorruptedIronGolem();
                    break;
            }

            Debug.Log($"<color=#EF4444>[RANDOM FINAL BOSS]</color> Grand Final Boss Emerged: {type}! (HP: {bossGo?.GetComponent<EnemyHealth>()?.MaxHealth:N0})");
            return bossGo;
        }

        private float GetBossDifficultyHpMultiplier()
        {
            switch (difficulty)
            {
                case GameDifficulty.Hard: return 1.25f;
                case GameDifficulty.Hardcore: return 1.50f;
                default: return 1.0f;
            }
        }

        public GameObject SpawnNetherGolem(int tier)
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.up * 3.3f : Vector3.up * 3.3f;

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

            Sprite[] walkList = Resources.LoadAll<Sprite>("MiniBosses/Redstone_Golem/redstone_golem_walk");
            Sprite[] atkList = Resources.LoadAll<Sprite>("MiniBosses/Redstone_Golem/redstone_golem_attack");
#if UNITY_EDITOR
            if (walkList == null || walkList.Length == 0)
            {
                Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Redstone_Golem/redstone_golem_walk.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var a in allAssets) if (a is Sprite s) list.Add(s);
                walkList = list.ToArray();
            }
            if (atkList == null || atkList.Length == 0)
            {
                Object[] atkAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Redstone_Golem/redstone_golem_attack.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var a in atkAssets) if (a is Sprite s) list.Add(s);
                atkList = list.ToArray();
            }
#endif
            boss.Initialize(tier, walkList, atkList);
            if (walkList != null && walkList.Length > 0) sr.sprite = walkList[0];
            activeEnemies.Add(golem);

            string title = tier == 1 ? "NETHER GOLEM I" : "NETHER GOLEM II";
            Debug.Log($"<color=#EF4444>[BOSS SPAWN]</color> {title} has emerged onto the battlefield!");
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: false, golem);
            return golem;
        }

        public GameObject SpawnTheWarden()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.up * 3.3f : Vector3.up * 3.3f;

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

            Sprite[] walkList = Resources.LoadAll<Sprite>("Bosses/Warden/warden_walk");
            Sprite[] atkList = Resources.LoadAll<Sprite>("Bosses/Warden/warden_attack");
#if UNITY_EDITOR
            if (walkList == null || walkList.Length == 0)
            {
                Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Warden/warden_walk.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var a in allAssets) if (a is Sprite s && s.rect.width <= 75f && s.rect.height <= 75f) list.Add(s);
                walkList = list.ToArray();
            }
            if (atkList == null || atkList.Length == 0)
            {
                Object[] atkAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Warden/warden_attack.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var a in atkAssets) if (a is Sprite s && s.rect.width <= 75f && s.rect.height <= 75f) list.Add(s);
                atkList = list.ToArray();
            }
#endif
            boss.Initialize(walkList, atkList);
            if (walkList != null && walkList.Length > 0) sr.sprite = walkList[0];
            activeEnemies.Add(warden);

            Debug.Log("<color=#06B6D4>[FINAL BOSS]</color> THE WARDEN has descended from the ancient depths!");
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: true, warden);
            return warden;
        }

        public GameObject SpawnPillagerCaptain(bool isGrandBoss = false)
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.right * 5.5f : Vector3.right * 5.5f;

            GameObject captain = new GameObject(isGrandBoss ? "GrandPillagerCaptain_Boss" : "PillagerCaptain_Boss");
            captain.transform.position = spawnPos;

            var sr = captain.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 14;

            var rb = captain.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = captain.AddComponent<CircleCollider2D>();
            col.radius = isGrandBoss ? 0.9f : 0.75f;

            var eh = captain.AddComponent<EnemyHealth>();
            eh.IsBoss = true;

            var boss = captain.AddComponent<PillagerCaptainBoss>();

            Sprite[] walkList = Resources.LoadAll<Sprite>("Bosses/Pillager_Captain/pillager_captain_walk");
            Sprite[] atkList = Resources.LoadAll<Sprite>("Bosses/Pillager_Captain/pillager_captain_attack");
#if UNITY_EDITOR
            if (walkList == null || walkList.Length == 0)
            {
                Object[] walkAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Pillager_Captain/pillager_captain_walk.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var a in walkAssets) if (a is Sprite s) list.Add(s);
                walkList = list.ToArray();
            }
            if (atkList == null || atkList.Length == 0)
            {
                Object[] atkAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Pillager_Captain/pillager_captain_attack.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var a in atkAssets) if (a is Sprite s) list.Add(s);
                atkList = list.ToArray();
            }
#endif
            boss.Initialize(walkList, atkList);
            if (walkList != null && walkList.Length > 0) sr.sprite = walkList[0];
            if (isGrandBoss)
            {
                float hpMult = GetBossDifficultyHpMultiplier();
                eh.SetMaxHealth(38000f * hpMult);
                eh.KnockbackResistance = 0.90f;
                captain.transform.localScale = Vector3.one * 1.6f;
            }

            activeEnemies.Add(captain);

            Debug.Log($"<color=#EF4444>[BOSS SPAWN]</color> {(isGrandBoss ? "GRAND PILLAGER CAPTAIN" : "PILLAGER CAPTAIN")} has arrived with his war crossbow!");
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss, captain);
            return captain;
        }

        public GameObject SpawnGrandPillagerCaptain()
        {
            return SpawnPillagerCaptain(isGrandBoss: true);
        }

        public GameObject SpawnGrandWitherKing()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.up * 3.5f : Vector3.up * 3.5f;
            GameObject go = new GameObject("GrandWitherKing_Boss");
            go.transform.position = spawnPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 15;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 1.0f;
            var eh = go.AddComponent<EnemyHealth>();
            eh.IsBoss = true;
            var boss = go.AddComponent<WitherKingBoss>();
            boss.Initialize();
            if (sr.sprite == null) {
                var sArr = Resources.LoadAll<Sprite>("MiniBosses/Wither_King/wither_king_walk");
                if (sArr != null && sArr.Length > 0) sr.sprite = sArr[0];
            }

            float hpMult = GetBossDifficultyHpMultiplier();
            eh.SetMaxHealth(40000f * hpMult);
            eh.KnockbackResistance = 0.95f;
            go.transform.localScale = Vector3.one * 1.9f;

            activeEnemies.Add(go);
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: true, go);
            Debug.Log("<color=#9333EA>[FINAL BOSS]</color> GRAND WITHER KING has ascended from the Nether Void!");
            return go;
        }

        public GameObject SpawnGrandMutantZombie()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.down * 3.5f : Vector3.down * 3.5f;
            GameObject go = new GameObject("MutantColossus_Boss");
            go.transform.position = spawnPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 14;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 1.0f;
            var eh = go.AddComponent<EnemyHealth>();
            eh.IsBoss = true;
            var boss = go.AddComponent<MutantZombieBoss>();
            boss.Initialize();
            if (sr.sprite == null) {
                var sArr = Resources.LoadAll<Sprite>("MiniBosses/Mutant_Zombie/mutant_zombie_walk");
                if (sArr != null && sArr.Length > 0) sr.sprite = sArr[0];
            }

            float hpMult = GetBossDifficultyHpMultiplier();
            eh.SetMaxHealth(42000f * hpMult);
            eh.KnockbackResistance = 0.95f;
            go.transform.localScale = Vector3.one * 1.8f;

            activeEnemies.Add(go);
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: true, go);
            Debug.Log("<color=#EF4444>[FINAL BOSS]</color> MUTANT COLOSSUS has awakened to crush all mortals!");
            return go;
        }

        public GameObject SpawnGrandCorruptedIronGolem()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.left * 5.5f : Vector3.left * 5.5f;
            GameObject go = new GameObject("CorruptedTitanGolem_Boss");
            go.transform.position = spawnPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 14;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 1.1f;
            var eh = go.AddComponent<EnemyHealth>();
            eh.IsBoss = true;
            var boss = go.AddComponent<CorruptedIronGolemBoss>();
            boss.Initialize();
            if (sr.sprite == null) {
                var sArr = Resources.LoadAll<Sprite>("MiniBosses/Corrupted_Iron_Golem/corrupted_iron_golem_walk");
                if (sArr != null && sArr.Length > 0) sr.sprite = sArr[0];
            }

            float hpMult = GetBossDifficultyHpMultiplier();
            eh.SetMaxHealth(44000f * hpMult);
            eh.KnockbackResistance = 1.0f;
            go.transform.localScale = Vector3.one * 1.8f;

            activeEnemies.Add(go);
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: true, go);
            Debug.Log("<color=#EF4444>[FINAL BOSS]</color> CORRUPTED TITAN GOLEM shakes the very foundation of the realm!");
            return go;
        }

        public GameObject SpawnPiglinBrute()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.right * 5.5f : Vector3.right * 5.5f;
            GameObject go = new GameObject("PiglinBrute_Boss");
            go.transform.position = spawnPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 14;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.8f;
            var eh = go.AddComponent<EnemyHealth>();
            eh.IsBoss = true;
            var boss = go.AddComponent<PiglinBruteBoss>();
            boss.Initialize();
            if (sr.sprite == null) {
                var sArr = Resources.LoadAll<Sprite>("Bosses/Piglin_Brute/piglin_brute_walk");
                if (sArr != null && sArr.Length > 0) sr.sprite = sArr[0];
            }
            activeEnemies.Add(go);
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: false, go);
            return go;
        }

        public GameObject SpawnWitherKing()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.up * 3.3f : Vector3.up * 3.3f;
            GameObject go = new GameObject("WitherKing_Boss");
            go.transform.position = spawnPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 15;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.85f;
            var eh = go.AddComponent<EnemyHealth>();
            eh.IsBoss = true;
            var boss = go.AddComponent<WitherKingBoss>();
            boss.Initialize();
            if (sr.sprite == null) {
                var sArr = Resources.LoadAll<Sprite>("MiniBosses/Wither_King/wither_king_walk");
                if (sArr != null && sArr.Length > 0) sr.sprite = sArr[0];
            }
            activeEnemies.Add(go);
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: false, go);
            return go;
        }

        public GameObject SpawnCorruptedIronGolem()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.left * 5.5f : Vector3.left * 5.5f;
            GameObject go = new GameObject("CorruptedIronGolem_Boss");
            go.transform.position = spawnPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 14;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.85f;
            var eh = go.AddComponent<EnemyHealth>();
            eh.IsBoss = true;
            var boss = go.AddComponent<CorruptedIronGolemBoss>();
            boss.Initialize();
            if (sr.sprite == null) {
                var sArr = Resources.LoadAll<Sprite>("MiniBosses/Corrupted_Iron_Golem/corrupted_iron_golem_walk");
                if (sArr != null && sArr.Length > 0) sr.sprite = sArr[0];
            }
            activeEnemies.Add(go);
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: false, go);
            return go;
        }

        public GameObject SpawnMutantZombie()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.down * 3.0f : Vector3.down * 3.0f;
            GameObject go = new GameObject("MutantZombie_Boss");
            go.transform.position = spawnPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 14;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.8f;
            var eh = go.AddComponent<EnemyHealth>();
            eh.IsBoss = true;
            var boss = go.AddComponent<MutantZombieBoss>();
            boss.Initialize();
            if (sr.sprite == null) {
                var sArr = Resources.LoadAll<Sprite>("MiniBosses/Mutant_Zombie/mutant_zombie_walk");
                if (sArr != null && sArr.Length > 0) sr.sprite = sArr[0];
            }
            activeEnemies.Add(go);
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: false, go);
            return go;
        }

        public GameObject SpawnPhantomLord()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.up * 3.3f : Vector3.up * 3.3f;
            GameObject go = new GameObject("PhantomLord_Boss");
            go.transform.position = spawnPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 16;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.75f;
            var eh = go.AddComponent<EnemyHealth>();
            eh.IsBoss = true;
            var boss = go.AddComponent<PhantomLordBoss>();
            boss.Initialize();
            if (sr.sprite == null) {
                var sArr = Resources.LoadAll<Sprite>("Bosses/Phantom_Lord/phantom_lord_fly");
                if (sArr != null && sArr.Length > 0) sr.sprite = sArr[0];
            }
            activeEnemies.Add(go);
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: false, go);
            return go;
        }

        public GameObject SpawnRavager()
        {
            FindPlayer();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position + Vector3.right * 5.5f : Vector3.right * 5.5f;
            GameObject go = new GameObject("Ravager_Boss");
            go.transform.position = spawnPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 14;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.9f;
            var eh = go.AddComponent<EnemyHealth>();
            eh.IsBoss = true;
            var boss = go.AddComponent<RavagerBoss>();
            boss.Initialize();
            if (sr.sprite == null) {
                var sArr = Resources.LoadAll<Sprite>("Bosses/Ravager/ravager_walk");
                if (sArr != null && sArr.Length > 0) sr.sprite = sArr[0];
            }
            activeEnemies.Add(go);
            BossArenaManager.Instance?.StartArenaLockdown(isGrandBoss: false, go);
            return go;
        }

        #endregion

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
                // Spawn a new Elite directly at the perimeter. Active mobs mid-fight are NEVER transformed!
                SpawnEliteEnemy(config);
            }
        }

        private void SpawnEliteEnemy(DifficultyEliteConfigSO config)
        {
            FindPlayer();
            if (playerTransform == null) return;

            // Pick a donut spawn position outside immediate camera view
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 spawnPos = playerTransform.position + (Vector3)(randomDir * randomDist);

            float minute = timeElapsed / 60f;
            PickZombieVariant(minute, out GameObject template, out string mobPrefix, out float baseHp, out float speed, out float dmg);
            if (template == null) template = zombieTemplate;
            if (template == null) return;

            GameObject mob = Instantiate(template, spawnPos, Quaternion.identity);
            mob.name = $"Elite_{mobPrefix}_{activeEnemies.Count + 1}";
            mob.SetActive(true);

            float finalHp = baseHp * GetMobHPMultiplier(minute);

            EnemyHealth eh = mob.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                eh.SetMaxHealth(finalHp);
                eh.SetInvincible(false);
                eh.IsBoss = false; // Strictly regular elite mob, NEVER a boss
            }

            CircleCollider2D col = mob.GetComponent<CircleCollider2D>();
            if (col != null) col.isTrigger = true;

            EnemyAI ai = mob.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.MoveSpeed = speed;
                ai.ContactDamage = dmg;
            }

            // Elite status is determined AT SPAWN TIME ONLY
            var eliteCtrl = mob.AddComponent<EliteEnemyAffixController>();
            eliteCtrl.InitializeElite(config);
            OnEliteSpawned();

            activeEnemies.Add(mob);
            Debug.Log($"<color=#A855F7>[ELITE SPAWN]</color> {mob.name} spawned as Elite at {spawnPos}! ({currentActiveElites}/{config.maxElitesOnScreen})");
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
            if (eh != null)
            {
                eh.SetMaxHealth(finalHp);
                eh.IsBoss = false;
            }

            EnemyAI ai = mob.GetComponent<EnemyAI>();
            if (ai != null) { ai.MoveSpeed = speed; ai.ContactDamage = dmg; }

            var eliteCtrl = mob.AddComponent<EliteEnemyAffixController>();
            eliteCtrl.InitializeElite(GetActiveEliteConfig());
            OnEliteSpawned();
            activeEnemies.Add(mob);
        }

        public void JumpToTime(float minute)
        {
            isRunActive = true;
            timeElapsed = Mathf.Clamp(minute * 60f, 0f, totalRunDuration);

            // Flag earlier special events so skipping forward doesn't backlog trigger them all simultaneously
            if (timeElapsed >= 90f) pincerTriggered = true;
            if (timeElapsed >= 240f) swarmRush1Triggered = true;
            if (timeElapsed >= 295f) golem1Warned = true;
            if (timeElapsed >= 300f) golem1Triggered = true;
            if (timeElapsed >= 450f) swarmRush2Triggered = true;
            if (timeElapsed >= 595f) golem2Warned = true;
            if (timeElapsed >= 600f) golem2Triggered = true;
            if (timeElapsed >= 750f) siegeTriggered = true;
            if (timeElapsed >= 895f && minute < 14.9f) wardenWarned = false; // Allow warning if jumping right before 15m
            if (timeElapsed >= 900f && minute < 15.0f) wardenTriggered = false;

            if (timeElapsed < 295f) { golem1Warned = false; golem1Triggered = false; firstMiniBossIndex = -1; }
            if (timeElapsed < 595f) { golem2Warned = false; golem2Triggered = false; secondMiniBossIndex = -1; }
            if (timeElapsed < 895f) { wardenWarned = false; wardenTriggered = false; finalBossIndex = -1; }

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

        /// <summary>
        /// Freezes run timer cleanly without wiping elapsed time (for Victory or Defeat screens).
        /// </summary>
        public void CompleteRun()
        {
            isRunActive = false;
        }

        private void OnRunTimeCompleted()
        {
            isRunActive = false;
            Debug.Log("<color=#EF4444>[WaveSpawner]</color> 18:00 Match Timeout reached! Player failed to defeat Final Boss in time.");

            // Trigger defeat if game is still active
            if (UI.InGamePauseManager.Instance != null && !UI.InGamePauseManager.Instance.IsGameOver)
            {
                UI.InGamePauseManager.Instance.TriggerTimeoutGameOver();
            }
        }
    }
}
