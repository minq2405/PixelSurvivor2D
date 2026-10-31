using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Phantom Lord Boss Controller:
    /// - High-speed flying apex predator.
    /// - Skill 1: Dive-Bomb Swoop (charges at supersonic speed, raking claws).
    /// - Skill 2: Spectral Screech (sonic pushback + summons 2 Phantom adds).
    /// - Enrage (< 40% HP): Ethereal Phasing (+35% speed, +25% damage).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PhantomLordBoss : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float baseHp = 13500f;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float contactDamage = 35f;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] flyFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float animFps = 11f;
        [SerializeField] private bool baseSpriteFacesLeft = true;
        [SerializeField] private float spriteScaleFactor = 0.75f; // 64x64 normalization

        private EnemyHealth health;
        private Rigidbody2D rb;
        private CircleCollider2D col;
        private SpriteRenderer spriteRenderer;
        private Transform playerTransform;

        private float animTimer = 0f;
        private int currentFrame = 0;
        private bool isBusy = false;
        private bool isEnraged = false;
        private float dmgMultiplier = 1.0f;
        private float spdMultiplier = 1.0f;

        private float diveCooldown = 5.5f;
        private float screechCooldown = 12.0f;

        private GameObject bossCanvasObj;
        private UnityEngine.UI.Slider healthSlider;

        public bool IsEnraged => isEnraged;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<CircleCollider2D>();
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
            EnsureSprites();
            ConfigureStats();
            BuildOverheadUI();
        }

        public void Initialize(Sprite[] fly = null, Sprite[] attack = null)
        {
            if (fly != null && fly.Length > 0) flyFrames = fly;
            if (attack != null && attack.Length > 0) attackFrames = attack;
            EnsureSprites();
            ConfigureStats();
            BuildOverheadUI();
        }

        private void EnsureSprites()
        {
#if UNITY_EDITOR
            if (flyFrames == null || flyFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Phantom_Lord/phantom_lord_fly.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                flyFrames = list.ToArray();
            }
            if (attackFrames == null || attackFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Phantom_Lord/phantom_lord_attack.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                attackFrames = list.ToArray();
            }
#endif
        }

        private void ConfigureStats()
        {
            float hpMult = 1.0f;
            if (WaveSpawner.Instance != null)
            {
                switch (WaveSpawner.Instance.Difficulty)
                {
                    case GameDifficulty.Hard: hpMult = 1.25f; break;
                    case GameDifficulty.Hardcore: hpMult = 1.50f; break;
                }
            }

            if (health != null)
            {
                health.SetMaxHealth(baseHp * hpMult);
                health.KnockbackResistance = 0.70f;
                health.IsBoss = true;
            }

            transform.localScale = Vector3.one * (1.6f * spriteScaleFactor);
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) playerTransform = pc.transform;
        }

        private void Update()
        {
            if (health == null || !health.IsAlive) return;
            if (playerTransform == null) { FindPlayer(); return; }

            UpdateOverheadUI();

            if (!isEnraged && health.CurrentHealth <= health.MaxHealth * 0.40f)
            {
                TriggerEnrage();
            }

            // Continuous contact damage check (regardless of casting state)
            float touchRadius = col != null ? (col.radius * transform.localScale.x + 0.35f) : 1.2f;
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= touchRadius)
            {
                CheckPlayerContactDamage(playerTransform.gameObject);
            }

            if (isBusy) return;

            diveCooldown -= Time.deltaTime;
            screechCooldown -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Skill 1: Dive-Bomb Swoop
            if (diveCooldown <= 0f && dist <= 9.0f)
            {
                StartCoroutine(DiveBombRoutine());
                return;
            }

            // Skill 2: Spectral Screech
            if (screechCooldown <= 0f)
            {
                StartCoroutine(SpectralScreechRoutine());
                return;
            }

            // Aerial Pursuit
            Vector2 toPlayer = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = toPlayer * (moveSpeed * spdMultiplier);

            if (spriteRenderer != null && Mathf.Abs(toPlayer.x) > 0.05f)
            {
                spriteRenderer.flipX = baseSpriteFacesLeft ? (toPlayer.x > 0) : (toPlayer.x < 0);
            }

            AnimateFly();
        }

        private void AnimateFly()
        {
            if (flyFrames == null || flyFrames.Length == 0 || spriteRenderer == null) return;
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / animFps)
            {
                animTimer = 0f;
                currentFrame = (currentFrame + 1) % flyFrames.Length;
                spriteRenderer.sprite = flyFrames[currentFrame];
            }
        }

        private IEnumerator DiveBombRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            diveCooldown = isEnraged ? 3.5f : 5.5f;

            if (playerTransform == null) { isBusy = false; yield break; }
            Vector2 diveDir = (playerTransform.position - transform.position).normalized;

            // Telegraph line
            GameObject line = CreateTelegraphLine(transform.position, diveDir, 8.0f, 0.40f);
            yield return new WaitForSeconds(0.40f);
            if (line != null) Destroy(line);

            if (col != null) col.isTrigger = true; // Phase through obstacles during swoop

            // Supersonic swoop
            float duration = 0.5f;
            float elapsed = 0f;
            float swoopSpeed = 13.0f * spdMultiplier;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rb.linearVelocity = diveDir * swoopSpeed;

                if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 1.8f)
                {
                    var ph = playerTransform.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        ph.TakeDamage(42f * dmgMultiplier, diveDir, 7f, DamageSourceType.BossSkill);
                    }
                    break;
                }
                yield return null;
            }

            if (col != null) col.isTrigger = false;
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.3f);
            isBusy = false;
        }

        private IEnumerator SpectralScreechRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            screechCooldown = 15.0f;

            // Pushback player
            if (playerTransform != null)
            {
                var ph = playerTransform.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector2 pushDir = (playerTransform.position - transform.position).normalized;
                    ph.TakeDamage(20f * dmgMultiplier, pushDir, 9f, DamageSourceType.BossSkill);
                }
            }

            // Spawn 2 Phantom adds
            if (WaveSpawner.Instance != null && playerTransform != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 pos = transform.position + (Vector3)Random.insideUnitCircle.normalized * 2.5f;
                    WaveSpawner.Instance.ForceSpawnEnemy(WaveSpawner.Instance.PhantomTemplate, pos);
                }
            }

            yield return new WaitForSeconds(0.6f);
            isBusy = false;
        }

        private GameObject CreateTelegraphLine(Vector2 start, Vector2 dir, float length, float duration)
        {
            GameObject lineObj = new GameObject("DiveTelegraph");
            var lr = lineObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.15f;
            lr.endWidth = 0.15f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.15f, 0.15f, 0.85f);
            lr.endColor = new Color(1f, 0.15f, 0.15f, 0.85f);
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, start + dir * length);

            Destroy(lineObj, duration);
            return lineObj;
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            dmgMultiplier = 1.25f;
            spdMultiplier = 1.35f;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.4f, 0.95f, 1.0f, 1f); // Spectral Cyan
            }
        }

        private void BuildOverheadUI()
        {
            if (bossCanvasObj != null) return;

            bossCanvasObj = new GameObject("PhantomBossBarUI");
            bossCanvasObj.transform.SetParent(transform, false);
            bossCanvasObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);

            Canvas canvas = bossCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 35;

            RectTransform canvasRect = bossCanvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(180f, 16f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);

            // BG
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(bossCanvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.04f, 0.08f, 0.12f, 0.95f);

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(bossCanvasObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
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
            fillImg.color = new Color(0.92f, 0.20f, 0.20f, 1f); // Unified Crimson Red

            healthSlider.targetGraphic = fillImg;
            healthSlider.fillRect = fillRect;
        }

        private void UpdateOverheadUI()
        {
            if (health == null || healthSlider == null) return;
            healthSlider.value = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
        }

        private void CheckPlayerContactDamage(GameObject other)
        {
            if (other != null && other.CompareTag("Player"))
            {
                var ph = other.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector2 kb = (other.transform.position - transform.position).normalized;
                    ph.TakeDamage(contactDamage * dmgMultiplier, kb, 5f, DamageSourceType.Contact);
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision) => CheckPlayerContactDamage(collision.gameObject);
        private void OnCollisionStay2D(Collision2D collision) => CheckPlayerContactDamage(collision.gameObject);
        private void OnTriggerEnter2D(Collider2D other) => CheckPlayerContactDamage(other.gameObject);
        private void OnTriggerStay2D(Collider2D other) => CheckPlayerContactDamage(other.gameObject);

        private void OnDestroy()
        {
            if (health != null && health.CurrentHealth <= 0f)
            {
                if (ExperienceManager.Instance != null)
                {
                    ExperienceManager.Instance.AddExp(300);
                    ExperienceManager.Instance.AddEmeralds(25);
                }
            }
        }
    }
}
