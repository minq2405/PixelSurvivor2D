using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Piglin Brute Boss Controller:
    /// - Aggressive melee boss wielding a giant Golden Battleaxe.
    /// - Skill 1: Execution Chop -> Red Oval Telegraph -> Earthquake Shockwave (vfx_dia_chan).
    /// - Skill 2: 360° Whirlwind Spin (cleaves melee perimeter).
    /// - Enrage (< 50% HP): +35% move speed, +30% attack speed.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PiglinBruteBoss : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float baseHp = 14000f;
        [SerializeField] private float moveSpeed = 2.7f;
        [SerializeField] private float contactDamage = 35f;

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

        private float chopCooldown = 5.0f;
        private float spinCooldown = 4.0f;

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
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Piglin_Brute/piglin_brute_walk.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                walkFrames = list.ToArray();
            }
            if (attackFrames == null || attackFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Piglin_Brute/piglin_brute_attack.png");
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

            transform.localScale = Vector3.one * (1.5f * spriteScaleFactor);
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

            // Enrage check (< 50% HP)
            if (!isEnraged && health.CurrentHealth <= health.MaxHealth * 0.50f)
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

            chopCooldown -= Time.deltaTime;
            spinCooldown -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Skill 1: Execution Chop Ground Slam (proactive leap-chop initiation from up to 9.5m)
            if (chopCooldown <= 0f && dist <= 9.5f)
            {
                StartCoroutine(ExecutionChopRoutine());
                return;
            }

            // Skill 2: Whirlwind Cleave (dist <= 2.0m)
            if (spinCooldown <= 0f && dist <= 2.0f)
            {
                StartCoroutine(WhirlwindRoutine());
                return;
            }

            // Move towards Player
            Vector2 moveDir = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = moveDir * (moveSpeed * spdMultiplier);

            if (spriteRenderer != null && Mathf.Abs(moveDir.x) > 0.05f)
            {
                spriteRenderer.flipX = baseSpriteFacesLeft ? (moveDir.x > 0) : (moveDir.x < 0);
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

        private IEnumerator ExecutionChopRoutine()
        {
            isBusy = true;
            chopCooldown = isEnraged ? 3.5f : 5.5f;
            rb.linearVelocity = Vector2.zero;

            if (playerTransform == null) { isBusy = false; yield break; }

            // Target Player's current position at skill start
            Vector3 targetLandingPos = playerTransform.position;

            // Flip towards target
            if (spriteRenderer != null && Mathf.Abs(targetLandingPos.x - transform.position.x) > 0.05f)
            {
                spriteRenderer.flipX = baseSpriteFacesLeft ? (targetLandingPos.x > transform.position.x) : (targetLandingPos.x < transform.position.x);
            }

            // Attack windup: raising golden battleaxe
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(4, attackFrames.Length - 1)];
            }

            float width = 4.4f;
            float height = width / 2.32f;
            float leapDuration = 0.50f;

            // 1. Red Oval Telegraph appears centered directly at Player's position!
            GroundShockwaveHelper.SpawnTelegraph(targetLandingPos, width, height, leapDuration);

            // 2. Piglin Brute leaps/lunges dynamically through the air towards targetLandingPos
            if (col != null) col.isTrigger = true; // Phase through obstacles during leap
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < leapDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / leapDuration);
                // Aggressive leaping chop arc (arc height = 1.6f)
                float arcY = Mathf.Sin(t * Mathf.PI) * 1.6f;
                transform.position = Vector3.Lerp(startPos, targetLandingPos, t) + Vector3.up * arcY;
                yield return null;
            }

            if (col != null) col.isTrigger = false;
            transform.position = targetLandingPos;

            // 3. Battleaxe slams down onto ground on landing
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(9, attackFrames.Length - 1)];
            }

            // 4. Spawn Dia-Chan Earthquake VFX + Damage at landing spot
            GroundShockwaveHelper.SpawnEarthquakeVFX(targetLandingPos, width, height, 42f * dmgMultiplier, 6f, new Color(1f, 0.25f, 0.25f, 1f), playerTransform);

            yield return new WaitForSeconds(0.4f);
            isBusy = false;
        }

        private IEnumerator WhirlwindRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            spinCooldown = 4.0f;

            // Spin animation
            for (int i = 10; i < (attackFrames != null ? attackFrames.Length : 0); i++)
            {
                if (spriteRenderer != null && attackFrames != null && i < attackFrames.Length)
                {
                    spriteRenderer.sprite = attackFrames[i];
                }
                yield return new WaitForSeconds(0.06f);
            }

            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.2f)
            {
                var ph = playerTransform.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector2 kb = (playerTransform.position - transform.position).normalized;
                    ph.TakeDamage(30f * dmgMultiplier, kb, 5f, DamageSourceType.BossSkill);
                }
            }

            yield return new WaitForSeconds(0.2f);
            isBusy = false;
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            dmgMultiplier = 1.30f;
            spdMultiplier = 1.35f;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.65f, 0.35f, 1f); // Golden Berserk Glow
            }
        }

        private void BuildOverheadUI()
        {
            if (bossCanvasObj != null) return;

            bossCanvasObj = new GameObject("PiglinBossBarUI");
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
            bgImg.color = new Color(0.08f, 0.06f, 0.04f, 0.95f);

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
                    ExperienceManager.Instance.AddExp(280);
                    ExperienceManager.Instance.AddEmeralds(22);
                }
            }
        }
    }
}
