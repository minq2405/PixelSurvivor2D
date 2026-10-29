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
        [Header("Difficulty Mode")]
        [SerializeField] private GameDifficulty difficulty = GameDifficulty.Normal;

        [Header("Run Duration (15 Minutes)")]
        [SerializeField] private float totalRunDuration = 900f; // 15 mins = 900 seconds
        [SerializeField] private bool autoStart = true;

        [Header("Spawn Donut Geometry")]
        [SerializeField] private float minSpawnRadius = 12f;
        [SerializeField] private float maxSpawnRadius = 15f;
        [SerializeField] private float spawnInterval = 0.85f;

        [Header("Enemy Templates & Animations")]
        [SerializeField] private GameObject zombieTemplate;
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

            GameObject newEnemy = null;

            if (zombieTemplate != null)
            {
                // Instantiate clone of template
                newEnemy = Instantiate(zombieTemplate, spawnPos, Quaternion.identity);
                newEnemy.name = $"Mob_Zombie_{activeEnemies.Count + 1}";
                newEnemy.SetActive(true);
            }
            else
            {
                // Fallback procedural enemy instantiation
                newEnemy = CreateProceduralEnemy(spawnPos);
            }

            if (newEnemy != null)
            {
                // Remove overhead label if inherited from dummy template
                Transform overheadLabel = newEnemy.transform.Find("Label");
                if (overheadLabel != null)
                {
                    Destroy(overheadLabel.gameObject);
                }

                // Ensure EnemyHealth is NOT invincible for spawned wave mobs
                EnemyHealth eh = newEnemy.GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    // Scale Base HP: 50 * HPMultiplier
                    float scaledHp = 50f * currentHPMultiplier;
                    eh.SetMaxHealth(scaledHp);
                    eh.SetInvincible(false);
                }

                // Ensure collider is a trigger so mobs don't physically wedge/trap player
                CircleCollider2D col = newEnemy.GetComponent<CircleCollider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                }

                // Ensure EnemyAI is present
                EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
                if (ai == null)
                {
                    ai = newEnemy.AddComponent<EnemyAI>();
                }

                // Speed & Damage formulas scaled by minute and difficulty
                float diffSpeedMod = difficulty == GameDifficulty.Hard ? 1.15f : (difficulty == GameDifficulty.Hardcore ? 1.30f : 1.0f);
                float diffDmgMod = difficulty == GameDifficulty.Hard ? 1.25f : (difficulty == GameDifficulty.Hardcore ? 1.50f : 1.0f);

                float scaledSpeed = Mathf.Min(3.4f, (1.8f + 0.05f * minute) * diffSpeedMod);
                float scaledDamage = (6f + 0.8f * minute) * diffDmgMod;
                ai.InitializeStats(50f * currentHPMultiplier, scaledSpeed, scaledDamage);

                // Ensure EnemyAnimation is present with walk frames
                EnemyAnimation anim = newEnemy.GetComponent<EnemyAnimation>();
                if (anim == null && zombieWalkFrames != null && zombieWalkFrames.Length > 0)
                {
                    anim = newEnemy.AddComponent<EnemyAnimation>();
                    anim.SetWalkFrames(zombieWalkFrames, 10f);
                }

                activeEnemies.Add(newEnemy);
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
