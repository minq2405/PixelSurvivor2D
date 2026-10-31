using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Corrupted Iron Golem Mini-Boss Controller:
    /// - Heavy mechanical rusted titan.
    /// - Skill 1: Tectonic Slam -> Red Oval Telegraph -> Earth-shattering Shockwave (vfx_dia_chan).
    /// - Skill 2: Rising Uppercut (catapults player upward).
    /// - Enrage (< 40% HP): Overclocked steam engines (+30% move speed, +25% damage).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class CorruptedIronGolemBoss : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float baseHp = 15000f;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float contactDamage = 38f;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float animFps = 10f;
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

        private float slamCooldown = 6.0f;
        private float uppercutCooldown = 4.0f;

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

        public void Initialize(Sprite[] walk = null, Sprite[] attack = null)
        {
            if (walk != null && walk.Length > 0) walkFrames = walk;
            if (attack != null && attack.Length > 0) attackFrames = attack;
            EnsureSprites();
            ConfigureStats();
            BuildOverheadUI();
        }

        private void EnsureSprites()
        {
#if UNITY_EDITOR
            if (walkFrames == null || walkFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Corrupted_Iron_Golem/corrupted_iron_golem_walk.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded)
                {
                    if (o is Sprite s && s.rect.width <= 75f && s.rect.height <= 75f) list.Add(s);
                }
                walkFrames = list.ToArray();
            }
            if (attackFrames == null || attackFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Corrupted_Iron_Golem/corrupted_iron_golem_attack.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded)
                {
                    if (o is Sprite s && s.rect.width <= 75f && s.rect.height <= 75f) list.Add(s);
                }
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
                health.KnockbackResistance = 0.85f;
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
            float touchRadius = col != null ? (col.radius * transform.localScale.x + 0.35f) : 1.3f;
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= touchRadius)
            {
                CheckPlayerContactDamage(playerTransform.gameObject);
            }

            if (isBusy) return;

            slamCooldown -= Time.deltaTime;
            uppercutCooldown -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Skill 1: Tectonic Slam (proactive initiation from up to 8.5m)
            if (slamCooldown <= 0f && dist <= 8.5f)
            {
                StartCoroutine(TectonicSlamRoutine());
                return;
            }

            // Skill 2: Uppercut (dist <= 2.0m)
            if (uppercutCooldown <= 0f && dist <= 2.0f)
            {
                StartCoroutine(UppercutRoutine());
                return;
            }

            // Stomp towards player
            Vector2 toPlayer = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = toPlayer * (moveSpeed * spdMultiplier);

            if (spriteRenderer != null && Mathf.Abs(toPlayer.x) > 0.05f)
            {
                spriteRenderer.flipX = baseSpriteFacesLeft ? (toPlayer.x > 0) : (toPlayer.x < 0);
            }

            AnimateWalk();
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

        private IEnumerator TectonicSlamRoutine()
        {
            isBusy = true;
            slamCooldown = isEnraged ? 4.5f : 6.5f;

            if (playerTransform == null) { isBusy = false; yield break; }

            // Proactive Gap-Closing Lunge: Sprint towards player before slamming mechanical fists
            if (Vector2.Distance(transform.position, playerTransform.position) > 2.2f)
            {
                if (col != null) col.isTrigger = true; // Phase through obstacles during lunge
                Vector2 lungeDir = (playerTransform.position - transform.position).normalized;
                rb.linearVelocity = lungeDir * (5.5f * spdMultiplier);
                yield return new WaitForSeconds(0.40f);
                if (col != null) col.isTrigger = false;
            }
            rb.linearVelocity = Vector2.zero;

            Vector2 dir = (playerTransform.position - transform.position).normalized;

            // Attack windup: raising mechanical fists
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(3, attackFrames.Length - 1)];
            }

            // Red Oval Telegraph directly beneath/around the impact point (Width 5.0m)
            float width = 5.0f;
            float height = width / 2.32f;
            Vector3 impactPos = transform.position + (Vector3)(dir * 1.4f) + Vector3.down * 0.2f;
            GroundShockwaveHelper.SpawnTelegraph(impactPos, width, height, 0.60f);

            yield return new WaitForSeconds(0.60f);

            // Double slam down
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(12, attackFrames.Length - 1)];
            }

            // Spawn Dia-Chan Earthquake VFX
            GroundShockwaveHelper.SpawnEarthquakeVFX(impactPos, width, height, 48f * dmgMultiplier, 8f, new Color(1f, 0.25f, 0.25f, 1f), playerTransform);

            yield return new WaitForSeconds(0.4f);
            isBusy = false;
        }

        private IEnumerator UppercutRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            uppercutCooldown = 4.0f;

            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(8, attackFrames.Length - 1)];
            }

            yield return new WaitForSeconds(0.3f);

            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.2f)
            {
                var ph = playerTransform.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector2 kb = Vector2.up * 8f; // Launch upward
                    ph.TakeDamage(38f * dmgMultiplier, kb, 7f, DamageSourceType.BossSkill);
                }
            }

            yield return new WaitForSeconds(0.3f);
            isBusy = false;
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            dmgMultiplier = 1.25f;
            spdMultiplier = 1.30f;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1.0f, 0.40f, 0.40f, 1f); // Red Overclock Glow
            }
        }

        private void BuildOverheadUI()
        {
            if (bossCanvasObj != null) return;

            bossCanvasObj = new GameObject("CorruptedGolemBossBarUI");
            bossCanvasObj.transform.SetParent(transform, false);
            bossCanvasObj.transform.localPosition = new Vector3(0f, 1.9f, 0f);

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
            bgImg.color = new Color(0.08f, 0.04f, 0.04f, 0.95f);

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
                    ExperienceManager.Instance.AddExp(320);
                    ExperienceManager.Instance.AddEmeralds(26);
                }
            }
        }
    }
}
