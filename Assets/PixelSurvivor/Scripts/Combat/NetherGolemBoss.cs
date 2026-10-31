using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Mini-Boss Controller: Nether Golem I (05:00) & Nether Golem II (10:00).
    /// v2.1 Specifications:
    /// - Golem I (05:00): 3,300 HP, speed 2.2 m/s, contact dmg 30, Ground Slam (35 dmg, 3.0m), Lava Throw 3 balls.
    ///   Drops Chest: +1 item level (evolves: false).
    /// - Golem II (10:00): 9,600 HP, speed 2.6 m/s, contact dmg 40, Ground Slam (50 dmg, 3.6m).
    ///   Enrage <50% HP: +20% speed (3.12 m/s), leaves flame trail (12 dmg/s for 3s).
    ///   Drops Chest: Guarantees evolution (evolves: true).
    /// - Boss HP scales with Difficulty: Normal 1.00x, Hard 1.20x, Hardcore 1.40x.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetherGolemBoss : MonoBehaviour
    {
        [Header("Golem Tier Settings")]
        [SerializeField] private int golemTier = 1; // 1 = Golem I (5:00), 2 = Golem II (10:00)
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float contactDamage = 30f;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float animFps = 10f;
        [Tooltip("True if source sprite faces Left by default (standard for Pixel Survivor Boss pack).")]
        [SerializeField] private bool baseSpriteFacesLeft = true;
        [Tooltip("Scale adjustment factor for 64x64 vs 48x48 sprite proportion.")]
        [SerializeField] private float spriteScaleFactor = 0.75f;

        [Header("Prefabs & Rewards")]
        [SerializeField] private GameObject bossChestPrefab;
        [SerializeField] private GameObject expOrbYellowPrefab;
        [SerializeField] private GameObject emeraldPrefab;

        private EnemyHealth health;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Transform playerTransform;
        private PlayerController playerController;

        private float animTimer = 0f;
        private int currentFrame = 0;
        private bool isAttacking = false;
        private bool isEnraged = false;
        private float flameTrailTimer = 0f;

        // Skill Cooldowns
        private float slamCooldownTimer = 3.0f; // initial 3s breathing room
        private float lavaCooldownTimer = 5.0f;

        // Overhead Boss UI
        private GameObject bossCanvasObj;
        private UnityEngine.UI.Slider healthSlider;
        private TMP_Text titleText;
        private TMP_Text hpNumText;

        public int GolemTier => golemTier;
        public bool IsEnraged => isEnraged;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }
        }

        private void Start()
        {
            FindPlayer();
            ConfigureBossStats();
            BuildOverheadUI();
        }

        public void Initialize(int tier, Sprite[] walk, Sprite[] attack = null, GameObject chestPrefab = null)
        {
            golemTier = tier;
            walkFrames = walk;
            attackFrames = attack;
            bossChestPrefab = chestPrefab;
            ConfigureBossStats();
        }

        private void ConfigureBossStats()
        {
            float baseHp = golemTier == 1 ? 3300f : 9600f;
            moveSpeed = golemTier == 1 ? 2.2f : 2.6f;
            contactDamage = golemTier == 1 ? 30f : 40f;

            // Difficulty multiplier
            float hpMult = 1.00f;
            if (WaveSpawner.Instance != null)
            {
                switch (WaveSpawner.Instance.Difficulty)
                {
                    case GameDifficulty.Hardcore: hpMult = 1.40f; break;
                    case GameDifficulty.Hard: hpMult = 1.20f; break;
                    default: hpMult = 1.00f; break;
                }
            }

            float finalHp = baseHp * hpMult;
            if (health != null)
            {
                health.SetMaxHealth(finalHp);
                health.IsBoss = true;
                health.KnockbackResistance = 0.90f; // Boss massive knockback resistance
            }

            float baseScale = (golemTier == 1 ? 1.5f : 1.8f) * spriteScaleFactor;
            transform.localScale = Vector3.one * baseScale;
        }

        private void FindPlayer()
        {
            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
                if (playerController != null) playerTransform = playerController.transform;
            }
        }

        private void Update()
        {
            if (health == null || !health.IsAlive) return;
            if (playerTransform == null) { FindPlayer(); return; }

            // Update Overhead UI
            UpdateOverheadUI();

            // Check Tier 2 Enrage (<50% HP)
            if (golemTier == 2 && !isEnraged && health.CurrentHealth <= health.MaxHealth * 0.50f)
            {
                TriggerEnrage();
            }

            // Flame Trail when enraged
            if (isEnraged)
            {
                flameTrailTimer += Time.deltaTime;
                if (flameTrailTimer >= 0.35f)
                {
                    flameTrailTimer = 0f;
                    SpawnFlameTrailPatch();
                }
            }

            // Skill Cooldowns
            slamCooldownTimer -= Time.deltaTime;
            lavaCooldownTimer -= Time.deltaTime;

            float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            // Ground Slam Skill check
            if (!isAttacking && slamCooldownTimer <= 0f && distToPlayer <= 4.0f)
            {
                StartCoroutine(GroundSlamRoutine());
                return;
            }

            // Lava Throw Skill check
            if (!isAttacking && lavaCooldownTimer <= 0f && distToPlayer > 3.5f && distToPlayer <= 10.0f)
            {
                StartCoroutine(LavaThrowRoutine());
                return;
            }

            // Standard Follow Movement
            if (!isAttacking)
            {
                Vector2 moveDir = (playerTransform.position - transform.position).normalized;
                float currentSpeed = isEnraged ? moveSpeed * 1.20f : moveSpeed;
                rb.linearVelocity = moveDir * currentSpeed;

                // Sprite flip
                if (spriteRenderer != null && Mathf.Abs(moveDir.x) > 0.05f)
                {
                    spriteRenderer.flipX = baseSpriteFacesLeft ? (moveDir.x > 0) : (moveDir.x < 0);
                }

                // Walk Animation
                AnimateWalk();
            }
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.35f, 0.25f, 1f); // Fiery Crimson
            }
            DamageNumber.SpawnText(transform.position + Vector3.up * 1.5f, "<color=#EF4444>🔥 GOLEM ENRAGED! (+20% SPD) 🔥</color>", Color.red);
            Debug.Log("<color=#EF4444>[Nether Golem II]</color> Entered ENRAGE phase! Speed +20%, burning flame trail active!");
        }

        private void SpawnFlameTrailPatch()
        {
            GameObject flame = new GameObject("FlameTrail");
            flame.transform.position = transform.position;
            var sr = flame.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            sr.color = new Color(1f, 0.45f, 0.1f, 0.75f);

            var col = flame.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.65f;

            var hazard = flame.AddComponent<GroundHazardZone>();
            hazard.Initialize(dps: 12f, duration: 3.0f);
        }

        private void AnimateWalk()
        {
            if (walkFrames == null || walkFrames.Length == 0 || spriteRenderer == null) return;
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / animFps)
            {
                animTimer = 0f;
                currentFrame = (currentFrame + 1) % walkFrames.Length;
                spriteRenderer.sprite = walkFrames[currentFrame];
            }
        }

        private IEnumerator GroundSlamRoutine()
        {
            isAttacking = true;
            rb.linearVelocity = Vector2.zero;
            slamCooldownTimer = 6.0f;

            float slamWidth = golemTier == 1 ? 3.4f : 4.0f;
            float slamHeight = slamWidth / 2.32f;
            float slamDamage = golemTier == 1 ? 35f : 50f;

            // Attack animation windup (raising stone fists)
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(4, attackFrames.Length - 1)];
            }

            // Red Oval Telegraph warning 0.9s
            Vector3 impactPos = transform.position + Vector3.down * 0.2f;
            GroundShockwaveHelper.SpawnTelegraph(impactPos, slamWidth, slamHeight, 0.9f);
            yield return new WaitForSeconds(0.9f);

            // Slam fists down
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[attackFrames.Length - 1];
            }

            DamageNumber.SpawnText(transform.position, "<color=#EF4444>💥 TECTONIC EARTHQUAKE!</color>", Color.red);

            // Spawn Dia-Chan Earthquake VFX + Ellipse Damage Detection
            GroundShockwaveHelper.SpawnEarthquakeVFX(impactPos, slamWidth, slamHeight, slamDamage, 7f, null, playerTransform);

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private IEnumerator LavaThrowRoutine()
        {
            isAttacking = true;
            rb.linearVelocity = Vector2.zero;
            lavaCooldownTimer = 7.5f;

            yield return new WaitForSeconds(0.35f);

            if (playerTransform != null)
            {
                Vector2 targetDir = (playerTransform.position - transform.position).normalized;
                float[] angles = { -15f, 0f, 15f };

                for (int i = 0; i < angles.Length; i++)
                {
                    Vector2 dir = Quaternion.Euler(0f, 0f, angles[i]) * targetDir;
                    SpawnLavaProjectile(transform.position, dir, 20f, 5.0f);
                }
            }

            yield return new WaitForSeconds(0.3f);
            isAttacking = false;
        }

        private void SpawnLavaProjectile(Vector2 pos, Vector2 dir, float dmg, float spd)
        {
            GameObject lava = new GameObject("LavaOrb");
            lava.transform.position = pos;
            var sr = lava.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.4f, 0.05f, 1f);
            sr.sortingOrder = 14;

            var col = lava.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            var proj = lava.AddComponent<HostileProjectile>();
            proj.Initialize(dir, spd, dmg, 4.0f);
        }

        private GameObject CreateTelegraphCircle(Vector2 center, float radius, float duration)
        {
            GameObject circle = new GameObject("TelegraphCircle");
            circle.transform.position = center;
            var lr = circle.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 36;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.2f, 0.2f, 0.65f);
            lr.endColor = new Color(1f, 0.2f, 0.2f, 0.65f);

            for (int i = 0; i < 36; i++)
            {
                float rad = i * 10f * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f));
            }

            Destroy(circle, duration);
            return circle;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") && !isAttacking)
            {
                var ph = collision.gameObject.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector2 knockback = (collision.transform.position - transform.position).normalized;
                    ph.TakeDamage(contactDamage, knockback, 3f, DamageSourceType.Contact);
                }
            }
        }

        private void BuildOverheadUI()
        {
            bossCanvasObj = new GameObject("BossOverheadUI");
            bossCanvasObj.transform.SetParent(transform, false);
            bossCanvasObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);

            Canvas canvas = bossCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 35;

            RectTransform canvasRect = bossCanvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(160f, 26f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);

            // Background
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(bossCanvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = new Vector2(1f, 0.45f);
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.08f, 0.04f, 0.12f, 0.95f);

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(bossCanvasObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = new Vector2(1f, 0.45f);
            sliderRect.offsetMin = new Vector2(1f, 1f);
            sliderRect.offsetMax = new Vector2(-1f, -1f);

            healthSlider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            var fillImg = fillObj.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = golemTier == 1 ? new Color(0.95f, 0.45f, 0.1f, 1f) : new Color(0.90f, 0.15f, 0.2f, 1f);

            healthSlider.targetGraphic = fillImg;
            healthSlider.fillRect = fillRect;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bossCanvasObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.45f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 11f;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Overflow;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.text = golemTier == 1 ? "NETHER GOLEM I" : "NETHER GOLEM II";
            titleText.color = golemTier == 1 ? new Color(1f, 0.8f, 0.2f, 1f) : new Color(1f, 0.35f, 0.35f, 1f);
        }

        private void UpdateOverheadUI()
        {
            if (health == null || healthSlider == null) return;
            healthSlider.value = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
            if (isEnraged && titleText != null)
            {
                titleText.text = "<color=#EF4444>ENRAGED GOLEM II</color>";
            }
        }

        private void OnDestroy()
        {
            // Spawn Boss Rewards on Death
            if (health != null && health.CurrentHealth <= 0f)
            {
                SpawnBossLoot();
            }
        }

        private void SpawnBossLoot()
        {
            Vector3 pos = transform.position;

            // 1. Guaranteed EXP & Emeralds
            if (ExperienceManager.Instance != null)
            {
                ExperienceManager.Instance.AddExp(250);
                ExperienceManager.Instance.AddEmeralds(20);
            }

            // 2. Spawn Boss Reward Chest
            GameObject chestGo = new GameObject($"BossChest_Tier{golemTier}");
            chestGo.transform.position = pos;
            var sr = chestGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

#if UNITY_EDITOR
            var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_lucky_block.png");
            if (sp != null) sr.sprite = sp;
#endif
            var chest = chestGo.AddComponent<BossRewardChest>();
            chest.Initialize(evolve: golemTier == 2);

            Debug.Log($"<color=#10B981>[Boss Defeated]</color> Nether Golem {golemTier} slain! Boss Chest spawned (Evolves: {golemTier == 2})");
        }
    }

    /// <summary>
    /// Ground hazard that deals damage over time when player touches it.
    /// </summary>
    public class GroundHazardZone : MonoBehaviour
    {
        private float dps = 12f;
        private float duration = 3.0f;

        public void Initialize(float dps, float duration)
        {
            this.dps = dps;
            this.duration = duration;
            Destroy(gameObject, duration);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                var ph = collision.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(dps * Time.deltaTime, Vector2.zero, 0f, DamageSourceType.BossSkill);
                }
            }
        }
    }

    /// <summary>
    /// Simple projectile fired by bosses.
    /// </summary>
    public class HostileProjectile : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private float damage;

        public void Initialize(Vector2 dir, float spd, float dmg, float lifeTime)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                var ph = collision.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(damage, direction, 2f, DamageSourceType.Projectile);
                }
                Destroy(gameObject);
            }
        }
    }
}
