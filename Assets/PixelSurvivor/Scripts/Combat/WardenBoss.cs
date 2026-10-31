using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Final Boss: The Warden (15:00 Encounter).
    /// v2.1 Specifications:
    /// - Máu: 42,000 HP (Normal 1.0x, Hard 1.20x: 50,400 HP, Hardcore 1.40x: 58,800 HP).
    /// - Giảm 60% tốc độ spawn quái thường của WaveSpawner khi đang sống.
    /// - Kỹ năng:
    ///   1. Melee Smash (60 dmg, 1.8m range).
    ///   2. Sonic Boom (45 dmg, range 8m, 1.2s telegraph beam).
    ///   3. Charge (55 dmg, dash 5.5 m/s, 0.8s roar telegraph).
    ///   4. Anti-kiting Sculk Tendril Pull (khi người chơi cách > 5.5m, gây 25 dmg và kéo về cự ly 3.0m).
    /// - Đồng hồ Enrage: 180s (Normal), 150s (Hard), 130s (Hardcore) -> +50% dmg, +35% move speed, dark sculk aura.
    /// - Knockback Resistance: 100% (1.0f).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class WardenBoss : MonoBehaviour
    {
        [Header("Warden Base Stats")]
        [SerializeField] private float baseHp = 42000f;
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float contactDamage = 55f;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float animFps = 9f;
        [Tooltip("True if source sprite faces Left by default (standard for Pixel Survivor Boss pack).")]
        [SerializeField] private bool baseSpriteFacesLeft = true;
        [Tooltip("Scale adjustment factor for 64x64 vs 48x48 sprite proportion.")]
        [SerializeField] private float spriteScaleFactor = 0.75f;

        private EnemyHealth health;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Transform playerTransform;
        private PlayerController playerController;

        private float animTimer = 0f;
        private int currentFrame = 0;
        private bool isBusy = false;

        // Enrage Timer
        private float enrageTotalTime = 180f;
        private float enrageTimer = 0f;
        private bool isEnraged = false;
        private float dmgMultiplier = 1.0f;
        private float spdMultiplier = 1.0f;

        // Skill Timers
        private float smashCooldown = 2.0f;
        private float sonicBoomCooldown = 9.0f;
        private float chargeCooldown = 12.0f;
        private float tendrilPullTimer = 6.0f;

        // Overhead Boss UI
        private GameObject bossCanvasObj;
        private UnityEngine.UI.Slider healthSlider;
        private TMP_Text titleText;
        private TMP_Text enrageTimerText;

        public bool IsEnraged => isEnraged;
        public float RemainingEnrageTime => Mathf.Max(0f, enrageTotalTime - enrageTimer);

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
            ConfigureWardenStats();
            BuildOverheadUI();

            // Dampen WaveSpawner mobs by 60%
            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.SetBossSpawningDampener(0.40f);
            }
        }

        public void Initialize(Sprite[] walk, Sprite[] attack = null)
        {
            walkFrames = walk;
            attackFrames = attack;
            ConfigureWardenStats();
        }

        private void ConfigureWardenStats()
        {
            float hpMult = 1.00f;
            enrageTotalTime = 180f;

            if (WaveSpawner.Instance != null)
            {
                switch (WaveSpawner.Instance.Difficulty)
                {
                    case GameDifficulty.Hardcore:
                        hpMult = 1.40f;
                        enrageTotalTime = 130f;
                        break;
                    case GameDifficulty.Hard:
                        hpMult = 1.20f;
                        enrageTotalTime = 150f;
                        break;
                    default:
                        hpMult = 1.00f;
                        enrageTotalTime = 180f;
                        break;
                }
            }

            float finalHp = baseHp * hpMult;
            if (health != null)
            {
                health.SetMaxHealth(finalHp);
                health.IsBoss = true;
                health.KnockbackResistance = 1.0f; // 100% knockback immune
            }

            transform.localScale = Vector3.one * (2.2f * spriteScaleFactor);
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

            // Enrage Timer check
            enrageTimer += Time.deltaTime;
            if (!isEnraged && enrageTimer >= enrageTotalTime)
            {
                TriggerCataclysmEnrage();
            }

            // Update UI
            UpdateOverheadUI();

            // Skill Cooldowns
            smashCooldown -= Time.deltaTime;
            sonicBoomCooldown -= Time.deltaTime;
            chargeCooldown -= Time.deltaTime;
            tendrilPullTimer -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Skill 4: Anti-kiting Sculk Tendril Pull (distance > 5.5m, cycle 18s)
            if (!isBusy && tendrilPullTimer <= 0f && dist > 5.5f)
            {
                StartCoroutine(SculkTendrilPullRoutine());
                return;
            }

            // Skill 1: Melee Smash (distance <= 1.8m)
            if (!isBusy && smashCooldown <= 0f && dist <= 1.8f)
            {
                StartCoroutine(MeleeSmashRoutine());
                return;
            }

            // Skill 3: Charge (dist between 4m and 12m)
            if (!isBusy && chargeCooldown <= 0f && dist >= 4.0f && dist <= 12.0f)
            {
                StartCoroutine(ChargeRoutine());
                return;
            }

            // Skill 2: Sonic Boom (dist between 3m and 9m)
            if (!isBusy && sonicBoomCooldown <= 0f && dist >= 3.0f && dist <= 9.0f)
            {
                StartCoroutine(SonicBoomRoutine());
                return;
            }

            // Standard Pursuit
            if (!isBusy)
            {
                Vector2 moveDir = (playerTransform.position - transform.position).normalized;
                rb.linearVelocity = moveDir * (moveSpeed * spdMultiplier);

                if (spriteRenderer != null && Mathf.Abs(moveDir.x) > 0.05f)
                {
                    spriteRenderer.flipX = baseSpriteFacesLeft ? (moveDir.x > 0) : (moveDir.x < 0);
                }

                AnimateWalk();
            }
        }

        private void TriggerCataclysmEnrage()
        {
            isEnraged = true;
            dmgMultiplier = 1.50f; // +50% dmg
            spdMultiplier = 1.35f; // +35% move speed

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.2f, 0.95f, 0.85f, 1f); // Menacing Dark Sculk Cyan
            }

            DamageNumber.SpawnText(transform.position + Vector3.up * 2f, "<color=#06B6D4>⚡ CATACLYSM ENRAGED! (+50% DMG, +35% SPD) ⚡</color>", Color.cyan);
            Debug.Log("<color=#06B6D4>[THE WARDEN]</color> Timer expired! CATACLYSM ENRAGE activated!");
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

        private IEnumerator MeleeSmashRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            smashCooldown = 2.0f;

            yield return new WaitForSeconds(0.4f);

            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.2f)
            {
                var ph = playerTransform.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector2 knockback = (playerTransform.position - transform.position).normalized;
                    ph.TakeDamage(60f * dmgMultiplier, knockback, 8f, DamageSourceType.BossSkill);
                }
                DamageNumber.SpawnText(transform.position, "<color=#EF4444>💥 WARDEN SMASH!</color>", Color.red);
            }

            yield return new WaitForSeconds(0.3f);
            isBusy = false;
        }

        private IEnumerator SonicBoomRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            sonicBoomCooldown = 9.0f;

            Vector2 targetDir = (playerTransform.position - transform.position).normalized;

            // 1.2s Telegraph beam line
            GameObject telegraphLine = CreateTelegraphBeam(transform.position, targetDir, 8.0f, 1.2f);
            yield return new WaitForSeconds(1.2f);

            if (telegraphLine != null) Destroy(telegraphLine);

            // Fire Sonic Boom projectile
            SpawnSonicShockwave(transform.position, targetDir, 45f * dmgMultiplier, 14f);
            DamageNumber.SpawnText(transform.position + Vector3.up * 1f, "<color=#06B6D4>🔊 SONIC BOOM!</color>", Color.cyan);

            yield return new WaitForSeconds(0.4f);
            isBusy = false;
        }

        private IEnumerator ChargeRoutine()
        {
            isBusy = true;
            chargeCooldown = 12.0f;
            rb.linearVelocity = Vector2.zero;

            // Roar telegraph 0.8s
            DamageNumber.SpawnText(transform.position + Vector3.up * 1.5f, "<color=#EF4444>⚠ WARDEN CHARGE!</color>", Color.red);
            yield return new WaitForSeconds(0.8f);

            if (playerTransform == null) { isBusy = false; yield break; }
            Vector2 chargeDir = (playerTransform.position - transform.position).normalized;

            float chargeDuration = 1.8f;
            float elapsed = 0f;
            float chargeSpeed = 5.5f * spdMultiplier;

            while (elapsed < chargeDuration)
            {
                elapsed += Time.deltaTime;
                rb.linearVelocity = chargeDir * chargeSpeed;

                if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.0f)
                {
                    var ph = playerTransform.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        ph.TakeDamage(55f * dmgMultiplier, chargeDir, 6f, DamageSourceType.Contact);
                    }
                    break;
                }
                yield return null;
            }

            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.5f);
            isBusy = false;
        }

        private IEnumerator SculkTendrilPullRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            tendrilPullTimer = 18.0f; // 18s cycle per GDD

            // 1.0s Telegraph at player feet
            if (playerTransform != null)
            {
                DamageNumber.SpawnText(playerTransform.position + Vector3.up * 0.8f, "<color=#06B6D4>🕸 SCULK TENDRIL PULL! 🕸</color>", Color.cyan);
            }

            yield return new WaitForSeconds(1.0f);

            if (playerTransform != null)
            {
                var ph = playerTransform.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(25f * dmgMultiplier, Vector2.zero, 0f, DamageSourceType.BossSkill);
                }

                // Violent Pull to 3.0m in front of Warden
                Vector2 pullTarget = (Vector2)transform.position + ((Vector2)playerTransform.position - (Vector2)transform.position).normalized * 3.0f;
                playerTransform.position = pullTarget;
            }

            yield return new WaitForSeconds(0.4f);
            isBusy = false;
        }

        private GameObject CreateTelegraphBeam(Vector2 start, Vector2 dir, float length, float duration)
        {
            GameObject line = new GameObject("SonicTelegraph");
            var lr = line.AddComponent<LineRenderer>();
            lr.startWidth = 0.25f;
            lr.endWidth = 0.25f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.1f, 0.9f, 0.95f, 0.65f);
            lr.endColor = new Color(0.1f, 0.9f, 0.95f, 0.65f);
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, start + dir * length);

            Destroy(line, duration);
            return line;
        }

        private void SpawnSonicShockwave(Vector2 pos, Vector2 dir, float dmg, float spd)
        {
            GameObject wave = new GameObject("SonicWave");
            wave.transform.position = pos;
            var sr = wave.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.1f, 0.95f, 0.9f, 0.9f);
            sr.sortingOrder = 16;
            wave.transform.localScale = new Vector3(1.5f, 0.6f, 1f);

            var col = wave.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var proj = wave.AddComponent<HostileProjectile>();
            proj.Initialize(dir, spd, dmg, 1.0f);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") && !isBusy)
            {
                var ph = collision.gameObject.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector2 knockback = (collision.transform.position - transform.position).normalized;
                    ph.TakeDamage(contactDamage * dmgMultiplier, knockback, 4f, DamageSourceType.Contact);
                }
            }
        }

        private void BuildOverheadUI()
        {
            bossCanvasObj = new GameObject("WardenBossBarUI");
            bossCanvasObj.transform.SetParent(transform, false);
            bossCanvasObj.transform.localPosition = new Vector3(0f, 2.4f, 0f);

            Canvas canvas = bossCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 35;

            RectTransform canvasRect = bossCanvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(3.2f, 0.60f);
            canvasRect.localScale = Vector3.one;

            // BG
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(bossCanvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = new Vector2(1f, 0.40f);
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.04f, 0.08f, 0.12f, 0.95f);

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(bossCanvasObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = new Vector2(1f, 0.40f);
            sliderRect.offsetMin = new Vector2(0.04f, 0.04f);
            sliderRect.offsetMax = new Vector2(-0.04f, -0.04f);

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
            fillImg.color = new Color(0.06f, 0.75f, 0.85f, 1f); // Sculk Cyan

            healthSlider.targetGraphic = fillImg;
            healthSlider.fillRect = fillRect;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bossCanvasObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.40f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 2.6f;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.text = "THE WARDEN - ANCIENT TERROR";
            titleText.color = new Color(0.2f, 0.95f, 0.90f, 1f);
        }

        private void UpdateOverheadUI()
        {
            if (health == null || healthSlider == null) return;
            healthSlider.value = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);

            if (isEnraged && titleText != null)
            {
                titleText.text = "<color=#06B6D4>⚡ CATACLYSM WARDEN (ENRAGED) ⚡</color>";
            }
        }

        private void OnDestroy()
        {
            // Restore WaveSpawner mob spawning rate
            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.SetBossSpawningDampener(1.0f);
            }

            // Victory triggered!
            if (health != null && health.CurrentHealth <= 0f)
            {
                Debug.Log("<color=#10B981>[VICTORY]</color> The Warden defeated! Pixel Survivor 15-minute Roguelite Run Cleared!");
                DamageNumber.SpawnText(transform.position + Vector3.up * 1.5f, "<color=#10B981>👑 VICTORY ACHIEVED! 👑</color>", Color.green);
            }
        }
    }
}
