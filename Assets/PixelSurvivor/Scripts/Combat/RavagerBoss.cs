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
        private TMP_Text titleText;

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

            // Skill 2: Ground Tremor Stomp (dist <= 3.0m)
            if (stompCooldown <= 0f && dist <= 3.0f)
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

            DamageNumber.SpawnText(transform.position + Vector3.up * 1.5f, "<color=#DC2626>🐂 RAVAGER CHARGE!</color>", Color.red);

            // Telegraph
            GameObject line = CreateTelegraphLine(transform.position, chargeDir, 9.0f, 0.75f);
            yield return new WaitForSeconds(0.75f);
            if (line != null) Destroy(line);

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

            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.35f);
            isBusy = false;
        }

        private IEnumerator TremorStompRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            stompCooldown = 5.0f;

            DamageNumber.SpawnText(transform.position + Vector3.up * 1.2f, "<color=#DC2626>💥 HOOF STOMP!</color>", Color.red);

            // Red Oval Telegraph warning
            float width = 3.0f;
            float height = width / 2.32f;
            Vector3 impactPos = transform.position + Vector3.down * 0.2f;
            GroundShockwaveHelper.SpawnTelegraph(impactPos, width, height, 0.8f);

            yield return new WaitForSeconds(0.8f);

            // Spawn Dia-Chan Earthquake VFX
            GroundShockwaveHelper.SpawnEarthquakeVFX(impactPos, width, height, 42f * dmgMultiplier, 7f, new Color(0.9f, 0.35f, 0.25f, 1f), playerTransform);

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
            lr.startColor = new Color(1f, 0.2f, 0.2f, 0.75f);
            lr.endColor = new Color(1f, 0.2f, 0.2f, 0.75f);
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

            DamageNumber.SpawnText(transform.position + Vector3.up * 2f, "<color=#DC2626>🩸 BLOOD RAGE RAVAGER! (+25% DMG, +35% SPD) 🩸</color>", Color.red);
            if (titleText != null)
            {
                titleText.text = "<color=#DC2626>BLOOD-RAGED RAVAGER</color>";
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
            canvasRect.sizeDelta = new Vector2(180f, 28f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);

            // BG
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(bossCanvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = new Vector2(1f, 0.45f);
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.06f, 0.04f, 0.04f, 0.95f);

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
            fillImg.color = new Color(0.85f, 0.15f, 0.15f, 1f); // Dark Crimson

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
            titleText.text = "RAVAGER - BEAST OF SIEGE";
            titleText.color = new Color(1f, 0.4f, 0.4f, 1f);
        }

        private void UpdateOverheadUI()
        {
            if (health == null || healthSlider == null) return;
            healthSlider.value = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") && !isBusy)
            {
                var ph = collision.gameObject.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Vector2 kb = (collision.transform.position - transform.position).normalized;
                    ph.TakeDamage(contactDamage * dmgMultiplier, kb, 6f, DamageSourceType.Contact);
                }
            }
        }

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
