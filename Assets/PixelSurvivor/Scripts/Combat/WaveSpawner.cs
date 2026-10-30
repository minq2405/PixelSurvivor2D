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

    public class WaveSpawner : MonoBehaviour
    {
        public static WaveSpawner Instance { get; private set; }

        [Header("Difficulty Mode")]
        [SerializeField] private GameDifficulty difficulty = GameDifficulty.Normal;

        [Header("Elite Enemy System (GDD Spec)")]
        [SerializeField] private DifficultyEliteConfigSO normalEliteConfig;
        [SerializeField] private DifficultyEliteConfigSO hardEliteConfig;
        [SerializeField] private DifficultyEliteConfigSO hardcoreEliteConfig;
        [SerializeField] private int currentActiveElites = 0;
        private float nextEliteAvailableTime = 0f;

        public int CurrentActiveElites => currentActiveElites;
        public GameDifficulty Difficulty { get => difficulty; set => difficulty = value; }

        [Header("Run Duration (15 Minutes)")]
        [SerializeField] private float totalRunDuration = 900f; // 15 mins = 900 seconds
        [SerializeField] private bool autoStart = true;

        [Header("Spawn Donut Geometry")]
        [SerializeField] private float minSpawnRadius = 12f;
        [SerializeField] private float maxSpawnRadius = 15f;
        [SerializeField] private float spawnInterval = 0.85f;

        [Header("Enemy Templates & Animations")]
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
        [SerializeField] private int currentMaxMobs = 15;
        [SerializeField] private int currentActiveMobs = 0;
        [SerializeField] private float currentHPMultiplier = 1.0f;

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

        public void SetTemplates(GameObject zombie, GameObject spider, GameObject creeper, GameObject slime = null, GameObject witch = null, GameObject huskShovel = null, GameObject iceCreeper = null, GameObject vex = null, GameObject phantom = null, GameObject evoker = null, GameObject zombieV2 = null, GameObject husk = null)
        {
            zombieTemplate = zombie;
            spiderTemplate = spider;
            creeperTemplate = creeper;
            if (slime != null) slimeTemplate = slime;
            if (witch != null) witchTemplate = witch;
            if (huskShovel != null) huskShovelTemplate = huskShovel;
            if (iceCreeper != null) iceCreeperTemplate = iceCreeper;
            if (vex != null) vexTemplate = vex;
            if (phantom != null) phantomTemplate = phantom;
            if (evoker != null) evokerTemplate = evoker;
            if (zombieV2 != null) zombieV2Template = zombieV2;
            if (husk != null) huskTemplate = husk;
        }

        public void RegisterEnemy(GameObject enemy)
        {
            if (enemy != null && !activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
            }
        }

        // ==========================================
        // DEBUG & RAPID TESTING API
        // ==========================================
        public void JumpToTime(float minute)
        {
            timeElapsed = Mathf.Clamp(minute * 60f, 0f, totalRunDuration);
            Debug.Log($"<color=#38BDF8>[WaveSpawner]</color> Jumped run time to {minute:F1} minutes ({timeElapsed:F0}s)!");
        }

        public GameObject ForceSpawnEnemy(GameObject template, Vector3? customPos = null)
        {
            if (template == null)
            {
                Debug.LogWarning("[WaveSpawner] ForceSpawnEnemy: template is null!");
                return null;
            }

            Vector3 spawnPos = customPos.HasValue 
                ? customPos.Value 
                : (playerTransform != null ? playerTransform.position + Vector3.right * 2.5f : transform.position + Vector3.right * 2.5f);

            GameObject newEnemy = Instantiate(template, spawnPos, Quaternion.identity);
            newEnemy.name = $"Debug_{template.name}_{activeEnemies.Count + 1}";
            newEnemy.SetActive(true);

            EnemyHealth eh = newEnemy.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                eh.SetInvincible(false);
            }

            activeEnemies.Add(newEnemy);
            Debug.Log($"<color=#10B981>[WaveSpawner]</color> Force spawned {newEnemy.name} at {spawnPos}");
            return newEnemy;
        }

        public void ClearAllEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                {
                    Destroy(activeEnemies[i]);
                }
            }
            activeEnemies.Clear();
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
            Debug.Log($"<color=#10B981>[WaveSpawner]</color> Executed kill command on {killed} mobs (tested drop burst)!");
        }

        [ContextMenu("Debug: Jump to Min 2 (Spider Phase)")]
        public void DebugJumpToMin2() => JumpToTime(2f);

        [ContextMenu("Debug: Jump to Min 5 (Creeper Phase)")]
        public void DebugJumpToMin5() => JumpToTime(5f);

        [ContextMenu("Debug: Jump to Min 10 (Horde Phase)")]
        public void DebugJumpToMin10() => JumpToTime(10f);

        [ContextMenu("Debug: Spawn Zombie (2.5m right)")]
        public void DebugSpawnZombie() => ForceSpawnEnemy(zombieTemplate);

        [ContextMenu("Debug: Spawn Zombie V2 (2.5m right)")]
        public void DebugSpawnZombieV2() => ForceSpawnEnemy(zombieV2Template);

        [ContextMenu("Debug: Spawn Husk (2.5m right)")]
        public void DebugSpawnHusk() => ForceSpawnEnemy(huskTemplate);

        [ContextMenu("Debug: Spawn Spider (2.5m right)")]
        public void DebugSpawnSpider() => ForceSpawnEnemy(spiderTemplate);

        [ContextMenu("Debug: Spawn Creeper (2.5m right)")]
        public void DebugSpawnCreeper() => ForceSpawnEnemy(creeperTemplate);

        [ContextMenu("Debug: Spawn Slime (2.5m right)")]
        public void DebugSpawnSlime() => ForceSpawnEnemy(slimeTemplate);

        [ContextMenu("Debug: Spawn Witch (2.5m right)")]
        public void DebugSpawnWitch() => ForceSpawnEnemy(witchTemplate);

        [ContextMenu("Debug: Spawn Husk Shovel (2.5m right)")]
        public void DebugSpawnHuskShovel() => ForceSpawnEnemy(huskShovelTemplate);

        [ContextMenu("Debug: Spawn Ice Creeper (2.5m right)")]
        public void DebugSpawnIceCreeper() => ForceSpawnEnemy(iceCreeperTemplate);

        [ContextMenu("Debug: Spawn Vex (2.5m right)")]
        public void DebugSpawnVex() => ForceSpawnEnemy(vexTemplate);

        [ContextMenu("Debug: Spawn Phantom (2.5m right)")]
        public void DebugSpawnPhantom() => ForceSpawnEnemy(phantomTemplate);

        [ContextMenu("Debug: Spawn Evoker (2.5m right)")]
        public void DebugSpawnEvoker() => ForceSpawnEnemy(evokerTemplate);

        [ContextMenu("Debug: Force Spawn Elite Mob (2.5m right)")]
        public void DebugForceSpawnElite() => ForceSpawnEliteEnemy();

        [ContextMenu("Debug: Jump to Min 7 (Elite Unlock Normal)")]
        public void DebugJumpToMin7() => JumpToTime(7.05f);

        [ContextMenu("Debug: Jump to Min 4 (Elite Unlock Hard)")]
        public void DebugJumpToMin4() => JumpToTime(4.05f);

        [ContextMenu("Debug: Jump to Min 2 (Elite Unlock Hardcore)")]
        public void DebugJumpToMin2Hardcore() => JumpToTime(2.05f);

        [ContextMenu("Debug: Clear All Mobs")]
        public void DebugClearAll() => ClearAllEnemies();

        [ContextMenu("Debug: Kill All Mobs (Test Drops)")]
        public void DebugKillAll() => KillAllActiveEnemies();

        private void Awake()
        {
            Time.timeScale = 1f;
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            EnemyHealth.ResetKills();
        }

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

        public bool ShouldSpawnAsElite(bool isMiniboss, float gameTime)
        {
            var config = GetActiveEliteConfig();
            if (config == null) return false;

            // 1. Check Unlock Time
            if (gameTime < config.unlockTimeInSeconds) return false;

            // 2. Check On-Screen Cap
            if (currentActiveElites >= config.maxElitesOnScreen) return false;

            // 3. Check Cooldown
            if (Time.time < nextEliteAvailableTime) return false;

            // 4. Miniboss Rules
            if (isMiniboss)
            {
                if (!config.canEliteMiniboss) return false;
                if (Random.value < config.minibossEliteChance)
                {
                    nextEliteAvailableTime = Time.time + config.eliteCooldown;
                    return true;
                }
                return false;
            }

            // 5. Regular Mob Chance
            if (Random.value < config.eliteChance)
            {
                nextEliteAvailableTime = Time.time + config.eliteCooldown;
                return true;
            }

            return false;
        }

        public void OnEliteSpawned() => currentActiveElites++;
        public void OnEliteDied() => currentActiveElites = Mathf.Max(0, currentActiveElites - 1);

        public void ForceSpawnEliteEnemy(GameObject template = null)
        {
            if (template == null) template = zombieTemplate;
            if (template == null) return;

            Vector3 spawnPos = playerTransform != null ? playerTransform.position + Vector3.right * 2.5f : Vector3.right * 2.5f;
            GameObject mob = Instantiate(template, spawnPos, Quaternion.identity);
            mob.name = $"Elite_{template.name}_{activeEnemies.Count + 1}";
            mob.SetActive(true);

            var eliteCtrl = mob.AddComponent<EliteEnemyAffixController>();
            eliteCtrl.InitializeElite(GetActiveEliteConfig());
            OnEliteSpawned();
            activeEnemies.Add(mob);
            Debug.Log($"<color=#F59E0B>[Debug Elite Spawn]</color> Spawned {mob.name} 2.5m right of player!");
        }

        private void Start()
        {
            FindPlayer();

            // Try to find a template in scene if not assigned
            if (zombieTemplate == null)
            {
                EnemyHealth dummy = FindAnyObjectByType<EnemyHealth>();
                if (dummy != null)
                {
                    zombieTemplate = dummy.gameObject;
                }
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
            Debug.Log("<color=#10B981>[WaveSpawner]</color> 15-Minute Roguelite Run Started!");
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

        private void Update()
        {
            if (!isRunActive) return;

            timeElapsed += Time.deltaTime;

            if (timeElapsed >= totalRunDuration)
            {
                OnRunTimeCompleted();
                return;
            }

            // Clean up dead/destroyed mobs from tracking list
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    activeEnemies.RemoveAt(i);
                }
            }

            currentActiveMobs = activeEnemies.Count;

            // Compute Minute Float t (0.0 to 15.0)
            float t = timeElapsed / 60f;

            // Difficulty Multipliers
            float diffHpMod = difficulty == GameDifficulty.Hard ? 1.25f : (difficulty == GameDifficulty.Hardcore ? 1.50f : 1.0f);
            float diffMaxMobMod = difficulty == GameDifficulty.Hard ? 1.33f : (difficulty == GameDifficulty.Hardcore ? 1.66f : 1.0f);

            // 1. Max Mob Density Scaling (15 to 150 mobs, scaled by difficulty)
            currentMaxMobs = Mathf.RoundToInt(Mathf.Lerp(15f, 150f * diffMaxMobMod, Mathf.Clamp01(t / 14f)));

            // 2. Controlled Linear HP Formula: HP(t) = HP_base * (1 + 0.25t) * diffHpMod
            // Ensures regular mobs cap strictly in the hundreds (e.g. 50 * (1 + 3.75) = ~237 HP on Normal, ~356 HP on Hardcore)
            currentHPMultiplier = (1.0f + 0.25f * t) * diffHpMod;

            // Spawn interval accelerates slightly as match progresses
            float baseIntervalTarget = difficulty == GameDifficulty.Hardcore ? 0.22f : (difficulty == GameDifficulty.Hard ? 0.28f : 0.35f);
            float currentInterval = Mathf.Lerp(spawnInterval, baseIntervalTarget, Mathf.Clamp01(t / 14f));

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentInterval)
            {
                spawnTimer = 0f;
                if (activeEnemies.Count < currentMaxMobs)
                {
                    SpawnNextEnemy(t);
                }
            }
        }

        private void GetMobStats(string mobType, float minute, out float baseHp, out float baseSpeed, out float baseDamage)
        {
            switch (mobType)
            {
                case "Spider":
                    baseHp = 30f;
                    baseSpeed = 4.5f + 0.025f * minute;
                    baseDamage = 8f + 0.5f * minute;
                    break;
                case "Vex":
                    baseHp = 25f;
                    baseSpeed = 4.8f + 0.025f * minute;
                    baseDamage = 10f + 0.5f * minute;
                    break;
                case "Phantom":
                    baseHp = 35f;
                    baseSpeed = 4.4f + 0.025f * minute;
                    baseDamage = 12f + 0.6f * minute;
                    break;
                case "Creeper":
                    baseHp = 60f;
                    baseSpeed = 3.4f + 0.02f * minute;
                    baseDamage = 10f + 0.6f * minute;
                    break;
                case "IceCreeper":
                    baseHp = 65f;
                    baseSpeed = 3.4f + 0.02f * minute;
                    baseDamage = 10f + 0.6f * minute;
                    break;
                case "Slime":
                    baseHp = 60f;
                    baseSpeed = 3.2f + 0.02f * minute;
                    baseDamage = 8f + 0.5f * minute;
                    break;
                case "Witch":
                    baseHp = 40f;
                    baseSpeed = 3.3f + 0.02f * minute;
                    baseDamage = 10f + 0.5f * minute;
                    break;
                case "HuskShovel":
                    baseHp = 85f;
                    baseSpeed = 3.6f + 0.03f * minute;
                    baseDamage = 14f + 0.7f * minute;
                    break;
                case "Evoker":
                    baseHp = 150f;
                    baseSpeed = 3.2f + 0.02f * minute;
                    baseDamage = 12f + 0.6f * minute;
                    break;
                case "ZombieV2":
                    baseHp = 65f;
                    baseSpeed = 3.6f + 0.03f * minute;
                    baseDamage = 8f + 0.8f * minute;
                    break;
                case "Husk":
                    baseHp = 75f;
                    baseSpeed = 3.5f + 0.03f * minute;
                    baseDamage = 9f + 0.8f * minute;
                    break;
                default: // Zombie
                    baseHp = 50f;
                    baseSpeed = 3.5f + 0.03f * minute;
                    baseDamage = 6f + 0.8f * minute;
                    break;
            }
        }

        private void PickZombieVariant(float minute, out GameObject template, out string prefix, out float hp, out float speed, out float dmg)
        {
            // Tỉ lệ sinh quái từ level đầu (Zombie thường không còn là 100% nữa):
            // 50% Zombie thường (3.5 m/s), 25% Zombie v2 (3.6 m/s), 25% Husk thường (3.5 m/s)
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
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            // Pick random point in donut ring around player
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 spawnPos = playerTransform.position + (Vector3)(randomDir * randomDist);

            GameObject templateToSpawn = zombieTemplate;
            string mobPrefix = "Zombie";
            float baseHp = 50f;
            float baseSpeed = 3.5f + 0.03f * minute;
            float baseDamage = 6f + 0.8f * minute;

            if (minute < 2f)
            {
                // Minute 0-2: Đa dạng Zombie Pool ngay từ đầu (50% Zombie 3.5 m/s, 25% ZombieV2 3.6 m/s, 25% Husk 3.5 m/s)
                PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out baseSpeed, out baseDamage);
            }
            else if (minute < 4f)
            {
                // Minute 2-4: Nhện Spider tốc độ cao bọc sườn (35% Spider 4.5 m/s, 65% Zombie Pool)
                float roll = Random.value;
                if (roll < 0.35f && spiderTemplate != null)
                {
                    templateToSpawn = spiderTemplate;
                    mobPrefix = "Spider";
                    GetMobStats(mobPrefix, minute, out baseHp, out baseSpeed, out baseDamage);
                }
                else
                {
                    PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out baseSpeed, out baseDamage);
                }
            }
            else if (minute < 7f)
            {
                // Minute 4-7: Creeper nổ & Slime nhảy (20% Creeper 3.4 m/s, 15% Slime 3.2 m/s, 25% Spider 4.5 m/s, 40% Zombie Pool)
                float roll = Random.value;
                if (roll < 0.20f && creeperTemplate != null)
                {
                    templateToSpawn = creeperTemplate;
                    mobPrefix = "Creeper";
                }
                else if (roll < 0.35f && slimeTemplate != null)
                {
                    templateToSpawn = slimeTemplate;
                    mobPrefix = "Slime";
                }
                else if (roll < 0.60f && spiderTemplate != null)
                {
                    templateToSpawn = spiderTemplate;
                    mobPrefix = "Spider";
                }
                else
                {
                    PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out baseSpeed, out baseDamage);
                }
                if (mobPrefix != "Zombie" && mobPrefix != "ZombieV2" && mobPrefix != "Husk")
                {
                    GetMobStats(mobPrefix, minute, out baseHp, out baseSpeed, out baseDamage);
                }
            }
            else if (minute < 10f)
            {
                // Minute 7-10: Witch tầm xa, Husk Shovel tank & Ice Creeper
                float roll = Random.value;
                if (roll < 0.18f && witchTemplate != null)
                {
                    templateToSpawn = witchTemplate;
                    mobPrefix = "Witch";
                }
                else if (roll < 0.36f && iceCreeperTemplate != null)
                {
                    templateToSpawn = iceCreeperTemplate;
                    mobPrefix = "IceCreeper";
                }
                else if (roll < 0.54f && huskShovelTemplate != null)
                {
                    templateToSpawn = huskShovelTemplate;
                    mobPrefix = "HuskShovel";
                }
                else if (roll < 0.72f && spiderTemplate != null)
                {
                    templateToSpawn = spiderTemplate;
                    mobPrefix = "Spider";
                }
                else
                {
                    PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out baseSpeed, out baseDamage);
                }
                if (mobPrefix != "Zombie" && mobPrefix != "ZombieV2" && mobPrefix != "Husk")
                {
                    GetMobStats(mobPrefix, minute, out baseHp, out baseSpeed, out baseDamage);
                }
            }
            else
            {
                // Minute 10-15: Tiểu quỷ Vex bay nhanh (4.8 m/s), Phantom lượn (4.4 m/s), Evoker triệu hồi (3.2 m/s)
                float roll = Random.value;
                if (roll < 0.18f && vexTemplate != null)
                {
                    templateToSpawn = vexTemplate;
                    mobPrefix = "Vex";
                }
                else if (roll < 0.34f && phantomTemplate != null)
                {
                    templateToSpawn = phantomTemplate;
                    mobPrefix = "Phantom";
                }
                else if (roll < 0.44f && evokerTemplate != null)
                {
                    templateToSpawn = evokerTemplate;
                    mobPrefix = "Evoker";
                }
                else if (roll < 0.60f && iceCreeperTemplate != null)
                {
                    templateToSpawn = iceCreeperTemplate;
                    mobPrefix = "IceCreeper";
                }
                else if (roll < 0.75f && spiderTemplate != null)
                {
                    templateToSpawn = spiderTemplate;
                    mobPrefix = "Spider";
                }
                else
                {
                    PickZombieVariant(minute, out templateToSpawn, out mobPrefix, out baseHp, out baseSpeed, out baseDamage);
                }
                if (mobPrefix != "Zombie" && mobPrefix != "ZombieV2" && mobPrefix != "Husk")
                {
                    GetMobStats(mobPrefix, minute, out baseHp, out baseSpeed, out baseDamage);
                }
            }

            GameObject newEnemy = null;

            if (templateToSpawn != null)
            {
                newEnemy = Instantiate(templateToSpawn, spawnPos, Quaternion.identity);
                newEnemy.name = $"Mob_{mobPrefix}_{activeEnemies.Count + 1}";
                newEnemy.SetActive(true);
            }
            else if (zombieTemplate != null)
            {
                newEnemy = Instantiate(zombieTemplate, spawnPos, Quaternion.identity);
                newEnemy.name = $"Mob_Zombie_{activeEnemies.Count + 1}";
                newEnemy.SetActive(true);
            }
            else
            {
                newEnemy = CreateProceduralEnemy(spawnPos);
            }

            if (newEnemy != null)
            {
                Transform overheadLabel = newEnemy.transform.Find("Label");
                if (overheadLabel != null)
                {
                    Destroy(overheadLabel.gameObject);
                }

                EnemyHealth eh = newEnemy.GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    eh.SetMaxHealth(baseHp * currentHPMultiplier);
                    eh.SetInvincible(false);
                }

                CircleCollider2D col = newEnemy.GetComponent<CircleCollider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                }

                float diffSpeedMod = difficulty == GameDifficulty.Hard ? 1.15f : (difficulty == GameDifficulty.Hardcore ? 1.30f : 1.0f);
                float diffDmgMod = difficulty == GameDifficulty.Hard ? 1.25f : (difficulty == GameDifficulty.Hardcore ? 1.50f : 1.0f);

                EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
                if (ai != null)
                {
                    float finalSpeed = baseSpeed * diffSpeedMod;
                    float finalDmg = baseDamage * diffDmgMod;
                    ai.InitializeStats(baseHp * currentHPMultiplier, finalSpeed, finalDmg);
                }

                activeEnemies.Add(newEnemy);

                // Elite Enemy Transformation Check (GDD Spec)
                bool isMiniboss = (mobPrefix == "Evoker" || mobPrefix == "HuskShovel" || mobPrefix == "Slime");
                if (ShouldSpawnAsElite(isMiniboss, timeElapsed))
                {
                    var eliteCtrl = newEnemy.AddComponent<EliteEnemyAffixController>();
                    var eliteCfg = GetActiveEliteConfig();
                    if (eliteCfg != null)
                    {
                        eliteCtrl.InitializeElite(eliteCfg);
                        OnEliteSpawned();
                        Debug.Log($"<color=#F59E0B>[ELITE MOB SPAWNED]</color> {newEnemy.name} transformed into Elite! Active Elites: {currentActiveElites}/{eliteCfg.maxElitesOnScreen}");
                    }
                }
            }
        }

        private GameObject CreateProceduralEnemy(Vector3 pos)
        {
            GameObject mob = new GameObject("Procedural_Zombie");
            mob.transform.position = pos;

            SpriteRenderer sr = mob.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            if (zombieWalkFrames != null && zombieWalkFrames.Length > 0)
            {
                EnemyAnimation anim = mob.AddComponent<EnemyAnimation>();
                anim.SetWalkFrames(zombieWalkFrames, 10f);
            }
            else if (zombieWalkSprite != null)
            {
                sr.sprite = zombieWalkSprite;
            }

            CircleCollider2D col = mob.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = true;

            Rigidbody2D rb = mob.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            mob.AddComponent<EnemyHealth>();
            mob.AddComponent<EnemyAI>();

            return mob;
        }

        private void OnRunTimeCompleted()
        {
            isRunActive = false;
            Debug.Log("<color=#FACC15>[WaveSpawner]</color> 15 Minutes reached! Spawning Final Boss The Warden!");
            // Boss spawn triggers here in Phase 5
        }

        private void OnDrawGizmosSelected()
        {
            Transform center = playerTransform != null ? playerTransform : transform;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.25f);
            Gizmos.DrawWireSphere(center.position, minSpawnRadius);
            Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(center.position, maxSpawnRadius);
        }
    }
}
