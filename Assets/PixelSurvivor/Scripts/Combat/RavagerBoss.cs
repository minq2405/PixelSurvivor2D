using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Ravager Boss Controller:
    /// - Heavy armored quad siege beast.
    /// - Skill 1: Battering Ram Charge (unstoppable high-speed iron horn charge).
    /// - Skill 2: Ground Tremor Stomp -> Red Oval Telegraph -> Earthquake Shockwave (vfx_dia_chan).
    /// - Enrage (< 40% HP): Blood Rage (+35% speed, +25% damage).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class RavagerBoss : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float baseHp = 16000f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float contactDamage = 40f;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private float animFps = 9f;
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

        private float chargeCooldown = 7.0f;
        private float stompCooldown = 5.0f;

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

        public void Initialize(Sprite[] walk = null)
        {
            if (walk != null && walk.Length > 0) walkFrames = walk;
            EnsureSprites();
            ConfigureStats();
            BuildOverheadUI();
        }

        private void EnsureSprites()
        {
#if UNITY_EDITOR
            if (walkFrames == null || walkFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Ravager/ravager_walk.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                walkFrames = list.ToArray();
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
                health.KnockbackResistance = 0.90f;
                health.IsBoss = true;
            }

            transform.localScale = Vector3.one * (1.7f * spriteScaleFactor);
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

            chargeCooldown -= Time.deltaTime;
            stompCooldown -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Skill 1: Ramming Charge (dist between 3.5m and 10.0m)
            if (chargeCooldown <= 0f && dist >= 3.5f && dist <= 10.0f)
            {
                StartCoroutine(RammingChargeRoutine());
                return;
            }

            // Skill 2: Ground Tremor Stomp (proactive leap-stomp initiation from up to 10.0m)
            if (stompCooldown <= 0f && dist <= 10.0f)
            {
                StartCoroutine(TremorStompRoutine());
                return;
            }

            // Trot towards player
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

        private IEnumerator RammingChargeRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            chargeCooldown = isEnraged ? 4.5f : 7.0f;

            if (playerTransform == null) { isBusy = false; yield break; }
            Vector2 chargeDir = (playerTransform.position - transform.position).normalized;

            // Telegraph 0.50s
            GameObject line = CreateTelegraphLine(transform.position, chargeDir, 9.0f, 0.50f);
            yield return new WaitForSeconds(0.50f);
            if (line != null) Destroy(line);

            if (col != null) col.isTrigger = true; // Phase through obstacles during ramming charge

            // Ram forward
            float duration = 1.2f;
            float elapsed = 0f;
            float chargeSpeed = 9.5f * spdMultiplier;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rb.linearVelocity = chargeDir * chargeSpeed;

                if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.0f)
                {
                    var ph = playerTransform.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        ph.TakeDamage(52f * dmgMultiplier, chargeDir, 9f, DamageSourceType.Contact);
                    }
                    break;
                }
                yield return null;
            }

            if (col != null) col.isTrigger = false;
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.35f);
            isBusy = false;
        }

        private IEnumerator TremorStompRoutine()
        {
            isBusy = true;
            stompCooldown = 5.0f;
            rb.linearVelocity = Vector2.zero;

            if (playerTransform == null) { isBusy = false; yield break; }

            // Target Player's current position at skill start
            Vector3 targetLandingPos = playerTransform.position;

            // Flip towards target
            if (spriteRenderer != null && Mathf.Abs(targetLandingPos.x - transform.position.x) > 0.05f)
            {
                spriteRenderer.flipX = baseSpriteFacesLeft ? (targetLandingPos.x > transform.position.x) : (targetLandingPos.x < transform.position.x);
            }

            float width = 4.8f;
            float height = width / 2.32f;
            float leapDuration = 0.55f;

            // 1. Red Oval Telegraph appears centered directly at Player's position!
            GroundShockwaveHelper.SpawnTelegraph(targetLandingPos, width, height, leapDuration);

            // 2. Ravager leaps/lunges dynamically through the air towards targetLandingPos
            if (col != null) col.isTrigger = true; // Phase through obstacles during leap stomp
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < leapDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / leapDuration);
                // Heavy beast leaping arc (arc height = 1.7m)
                float arcY = Mathf.Sin(t * Mathf.PI) * 1.7f;
                transform.position = Vector3.Lerp(startPos, targetLandingPos, t) + Vector3.up * arcY;
                yield return null;
            }

            if (col != null) col.isTrigger = false;
            transform.position = targetLandingPos;

            // 3. Spawn Dia-Chan Earthquake VFX + Damage at landing spot
            GroundShockwaveHelper.SpawnEarthquakeVFX(targetLandingPos, width, height, 42f * dmgMultiplier, 7f, new Color(1f, 0.25f, 0.25f, 1f), playerTransform);

            yield return new WaitForSeconds(0.35f);
            isBusy = false;
        }

        private GameObject CreateTelegraphLine(Vector2 start, Vector2 dir, float length, float duration)
        {
            GameObject lineObj = new GameObject("ChargeTelegraph");
            var lr = lineObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.20f;
            lr.endWidth = 0.20f;
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
                spriteRenderer.color = new Color(1f, 0.45f, 0.45f, 1f); // Crimson Blood Rage
            }
        }

        private void BuildOverheadUI()
        {
            if (bossCanvasObj != null) return;

            bossCanvasObj = new GameObject("RavagerBossBarUI");
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
            bgImg.color = new Color(0.06f, 0.04f, 0.04f, 0.95f);

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
                    ph.TakeDamage(contactDamage * dmgMultiplier, kb, 6f, DamageSourceType.Contact);
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
