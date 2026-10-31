using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Mutant Zombie Mini-Boss Controller:
    /// - Massive hulking bipedal brute.
    /// - Skill 1: Leap Slam -> Jumps into the air targeting Player position -> Red Oval Telegraph -> Land with Earthquake Shockwave (vfx_dia_chan).
    /// - Skill 2: Ground Smash (melee shockwave when close).
    /// - Enrage (< 40% HP): Berserk mutations (+35% speed, +25% damage).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class MutantZombieBoss : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float baseHp = 13000f;
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float contactDamage = 34f;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float animFps = 10f;
        [SerializeField] private bool baseSpriteFacesLeft = true;

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

        private float leapCooldown = 6.0f;
        private float poundCooldown = 4.0f;

        private GameObject bossCanvasObj;
        private UnityEngine.UI.Slider healthSlider;

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
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Mutant_Zombie/mutant_zombie_walk.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                walkFrames = list.ToArray();
            }
            if (attackFrames == null || attackFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Mutant_Zombie/mutant_zombie_attack.png");
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
                health.KnockbackResistance = 0.80f;
                health.IsBoss = true;
            }

            transform.localScale = Vector3.one * 1.45f;
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

            leapCooldown -= Time.deltaTime;
            poundCooldown -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Skill 1: Leap Slam (proactive initiation from up to 10.5m)
            if (leapCooldown <= 0f && dist <= 10.5f)
            {
                StartCoroutine(LeapSlamRoutine());
                return;
            }

            // Skill 2: Melee Ground Pound (dist <= 2.8m)
            if (poundCooldown <= 0f && dist <= 2.8f)
            {
                StartCoroutine(GroundPoundRoutine());
                return;
            }

            // Lumber towards player
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

        private IEnumerator LeapSlamRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            leapCooldown = isEnraged ? 4.5f : 6.5f;

            if (playerTransform == null) { isBusy = false; yield break; }

            // Target player's current ground spot
            Vector3 targetLandingPos = playerTransform.position;

            // Roar and chest beat
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(3, attackFrames.Length - 1)];
            }

            // Red Oval Telegraph appears at the landing target spot (Width 4.8m)
            float width = 4.8f;
            float height = width / 2.32f;
            GroundShockwaveHelper.SpawnTelegraph(targetLandingPos, width, height, 0.95f);

            // Leap Arc Animation: move up and lerp towards targetLandingPos
            Vector3 startPos = transform.position;
            float leapDuration = 0.95f;
            float elapsed = 0f;

            while (elapsed < leapDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / leapDuration);
                // Parabolic arc (jump height = 2.5m)
                float arcY = Mathf.Sin(t * Mathf.PI) * 2.5f;
                Vector3 currentGround = Vector3.Lerp(startPos, targetLandingPos, t);
                transform.position = currentGround + Vector3.up * arcY;
                yield return null;
            }

            transform.position = targetLandingPos;

            // Slam ground impact frame
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[attackFrames.Length - 1];
            }

            // Spawn Dia-Chan Earthquake VFX at landing spot
            GroundShockwaveHelper.SpawnEarthquakeVFX(targetLandingPos, width, height, 48f * dmgMultiplier, 8f, new Color(0.4f, 0.9f, 0.4f, 1f), playerTransform);

            yield return new WaitForSeconds(0.4f);
            isBusy = false;
        }

        private IEnumerator GroundPoundRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            poundCooldown = 4.0f;

            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(10, attackFrames.Length - 1)];
            }

            yield return new WaitForSeconds(0.35f);

            float width = 3.6f;
            float height = width / 2.32f;
            GroundShockwaveHelper.SpawnEarthquakeVFX(transform.position + Vector3.down * 0.2f, width, height, 32f * dmgMultiplier, 5f, null, playerTransform);

            yield return new WaitForSeconds(0.3f);
            isBusy = false;
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            dmgMultiplier = 1.25f;
            spdMultiplier = 1.35f;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.6f, 1.0f, 0.6f, 1f); // Toxic Green Glow
            }
        }

        private void BuildOverheadUI()
        {
            if (bossCanvasObj != null) return;

            bossCanvasObj = new GameObject("MutantZombieBossBarUI");
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
            bgImg.color = new Color(0.04f, 0.08f, 0.04f, 0.95f);

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
            fillImg.color = new Color(0.12f, 0.75f, 0.45f, 1f); // Toxic Emerald Green

            healthSlider.targetGraphic = fillImg;
            healthSlider.fillRect = fillRect;
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
                    ph.TakeDamage(contactDamage * dmgMultiplier, kb, 5f, DamageSourceType.Contact);
                }
            }
        }

        private void OnDestroy()
        {
            if (health != null && health.CurrentHealth <= 0f)
            {
                if (ExperienceManager.Instance != null)
                {
                    ExperienceManager.Instance.AddExp(300);
                    ExperienceManager.Instance.AddEmeralds(24);
                }
            }
        }
    }
}
