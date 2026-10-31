using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Wither King Mini-Boss Controller:
    /// - Hovering floating undead titan with 3 skulls.
    /// - Skill 1: Wither Quake -> Red Oval Telegraph -> Necrotic Earthquake Shockwave (vfx_dia_chan).
    /// - Skill 2: Triple Wither Skull Volley (homing dark energy projectiles).
    /// - Enrage (< 40% HP): Wither Armor (+25% armor, +35% move speed).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class WitherKingBoss : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float baseHp = 13500f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float contactDamage = 32f;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float animFps = 10f;
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

        private float quakeCooldown = 6.0f;
        private float skullCooldown = 4.5f;

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
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Wither_King/wither_king_walk.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                walkFrames = list.ToArray();
            }
            if (attackFrames == null || attackFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/MiniBosses/Wither_King/wither_king_attack.png");
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
                health.KnockbackResistance = 0.75f;
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

            if (isBusy) return;

            quakeCooldown -= Time.deltaTime;
            skullCooldown -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Skill 1: Wither Quake (proactive initiation from up to 9.5m)
            if (quakeCooldown <= 0f && dist <= 9.5f)
            {
                StartCoroutine(WitherQuakeRoutine());
                return;
            }

            // Skill 2: Skull Volley
            if (skullCooldown <= 0f && dist <= 10.0f)
            {
                StartCoroutine(SkullVolleyRoutine());
                return;
            }

            // Floating Pursuit
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

        private IEnumerator WitherQuakeRoutine()
        {
            isBusy = true;
            quakeCooldown = isEnraged ? 4.0f : 6.5f;

            if (playerTransform == null) { isBusy = false; yield break; }

            // Proactive Gap-Closing Shadow Dash: Glide swiftly towards player before unleashing quake
            if (Vector2.Distance(transform.position, playerTransform.position) > 2.2f)
            {
                Vector2 lungeDir = (playerTransform.position - transform.position).normalized;
                rb.linearVelocity = lungeDir * (6.0f * spdMultiplier);
                yield return new WaitForSeconds(0.40f);
            }
            rb.linearVelocity = Vector2.zero;

            Vector2 dir = (playerTransform.position - transform.position).normalized;

            // Attack windup: heads spin
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(4, attackFrames.Length - 1)];
            }

            // Red Oval Telegraph directly beneath/around the impact point (Width 4.8m)
            float width = 4.8f;
            float height = width / 2.32f;
            Vector3 impactPos = transform.position + (Vector3)(dir * 1.5f) + Vector3.down * 0.3f;
            GroundShockwaveHelper.SpawnTelegraph(impactPos, width, height, 0.85f);

            yield return new WaitForSeconds(0.85f);

            // Release dark wave
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[attackFrames.Length - 1];
            }

            DamageNumber.SpawnText(impactPos, "<color=#A855F7>💀 WITHER QUAKE!</color>", new Color(0.7f, 0.35f, 0.95f, 1f));

            // Spawn Dia-Chan Earthquake VFX with Necrotic Purple tint
            Color witherTint = new Color(0.60f, 0.30f, 0.90f, 1f);
            GroundShockwaveHelper.SpawnEarthquakeVFX(impactPos, width, height, 46f * dmgMultiplier, 6f, witherTint, playerTransform);

            yield return new WaitForSeconds(0.4f);
            isBusy = false;
        }

        private IEnumerator SkullVolleyRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            skullCooldown = 5.0f;

            if (playerTransform == null) { isBusy = false; yield break; }
            Vector2 targetDir = (playerTransform.position - transform.position).normalized;

            int count = isEnraged ? 5 : 3;
            float[] angles = isEnraged ? new float[] { -24f, -12f, 0f, 12f, 24f } : new float[] { -15f, 0f, 15f };

            DamageNumber.SpawnText(transform.position + Vector3.up * 1f, "<color=#A855F7>☠️ SKULL VOLLEY! ☠️</color>", new Color(0.7f, 0.35f, 0.95f, 1f));

            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Quaternion.Euler(0f, 0f, angles[i]) * targetDir;
                SpawnWitherSkull(transform.position, dir, 22f * dmgMultiplier, 7.5f);
            }

            yield return new WaitForSeconds(0.4f);
            isBusy = false;
        }

        private void SpawnWitherSkull(Vector2 pos, Vector2 dir, float dmg, float spd)
        {
            GameObject skull = new GameObject("WitherSkull");
            skull.transform.position = pos;

            var sr = skull.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.12f, 0.08f, 0.18f, 1f);
            sr.sortingOrder = 17;
            skull.transform.localScale = Vector3.one * 0.55f;

            var col = skull.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            var proj = skull.AddComponent<HostileProjectile>();
            proj.Initialize(dir, spd, dmg, 4.0f);
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            dmgMultiplier = 1.25f;
            spdMultiplier = 1.30f;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.75f, 0.50f, 1.0f, 1f); // Dark Necrotic Glow
            }

            DamageNumber.SpawnText(transform.position + Vector3.up * 2f, "<color=#A855F7>⚡ WITHER ARMOR ACTIVE! (+25% DMG, +30% SPD) ⚡</color>", Color.magenta);
        }

        private void BuildOverheadUI()
        {
            if (bossCanvasObj != null) return;

            bossCanvasObj = new GameObject("WitherBossBarUI");
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
            bgImg.color = new Color(0.06f, 0.04f, 0.08f, 0.95f);

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
            fillImg.color = new Color(0.66f, 0.33f, 0.97f, 1.0f); // Necrotic Purple

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
                    ExperienceManager.Instance.AddEmeralds(25);
                }
            }
        }
    }
}
