using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PixelSurvivor;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Pillager Captain Boss Controller:
    /// - Equipped with Crossbow (weapon_crossbow_0) & Projectile Arrow (projectile_arrow_0).
    /// - Tactical Ranged Commander:
    ///     * Skill 1: Crossbow Heavy Snipe (0.5s telegraph laser, high speed armor-piercing bolt).
    ///     * Skill 2: Triple Fan Spread-Shot (3 arrows in 30° cone, 5 arrows when enraged).
    ///     * Skill 3: Raid Horn Rally (calls reinforcements, buffs speed).
    ///     * Enrage (< 40% HP): Reload speed +40%, Crimson aura.
    /// - Single Sleek Overhead Boss Bar (#EF4444 Crimson).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PillagerCaptainBoss : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float baseHp = 12000f;
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float contactDamage = 30f;
        [SerializeField] private float preferredDistance = 5.5f;

        [Header("Animation & Sprites")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite crossbowSprite;
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private float animFps = 10f;
        [SerializeField] private bool baseSpriteFacesLeft = true;

        [Header("Crossbow Attachment")]
        [SerializeField] private GameObject crossbowObj;
        [SerializeField] private SpriteRenderer crossbowRenderer;

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

        // Skill Cooldowns
        private float snipeCooldown = 3.0f;
        private float spreadCooldown = 7.0f;
        private float raidHornCooldown = 15.0f;

        // Overhead UI
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
            ConfigureStats();
            SetupCrossbowVisual();
            BuildOverheadUI();
        }

        public void Initialize(Sprite[] walk, Sprite[] attack = null, Sprite crossbow = null, Sprite arrow = null)
        {
            walkFrames = walk;
            attackFrames = attack;
            crossbowSprite = crossbow;
            arrowSprite = arrow;

            EnsureSprites();
            ConfigureStats();
            SetupCrossbowVisual();
            BuildOverheadUI();
        }

        private void EnsureSprites()
        {
#if UNITY_EDITOR
            if (crossbowSprite == null)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Weapons/weapon_crossbow.png");
                foreach (var o in loaded) if (o is Sprite s) { crossbowSprite = s; break; }
            }
            if (arrowSprite == null)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Particles/Projectiles/projectile_arrow.png");
                foreach (var o in loaded) if (o is Sprite s) { arrowSprite = s; break; }
            }
            if (walkFrames == null || walkFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Pillager_Captain/pillager_captain_walk.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                walkFrames = list.ToArray();
            }
            if (attackFrames == null || attackFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Bosses/Pillager_Captain/pillager_captain_attack.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                attackFrames = list.ToArray();
            }
#endif
        }

        private void SetupCrossbowVisual()
        {
            EnsureSprites();

            if (crossbowObj == null)
            {
                crossbowObj = new GameObject("Equipped_Crossbow");
                crossbowObj.transform.SetParent(transform, false);
                crossbowObj.transform.localPosition = new Vector3(0.35f, 0f, 0f);
                crossbowObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

                crossbowRenderer = crossbowObj.AddComponent<SpriteRenderer>();
                crossbowRenderer.sortingOrder = 14;
                if (crossbowSprite != null) crossbowRenderer.sprite = crossbowSprite;
            }
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

            transform.localScale = Vector3.one * 1.30f; // Imposing commander size
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

            // Enrage check (< 40% HP)
            if (!isEnraged && health.CurrentHealth <= health.MaxHealth * 0.40f)
            {
                TriggerEnrage();
            }

            // Aim Crossbow at Player
            UpdateCrossbowAim();

            if (isBusy) return;

            // Skill Timers
            snipeCooldown -= Time.deltaTime;
            spreadCooldown -= Time.deltaTime;
            raidHornCooldown -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Skill 3: Raid Horn Rally
            if (raidHornCooldown <= 0f)
            {
                StartCoroutine(RaidHornRoutine());
                return;
            }

            // Skill 2: Crossbow Spread-Shot (3 to 5 arrows)
            if (spreadCooldown <= 0f && dist <= 9.0f)
            {
                StartCoroutine(SpreadShotRoutine());
                return;
            }

            // Skill 1: Crossbow Heavy Snipe
            if (snipeCooldown <= 0f && dist <= 11.0f)
            {
                StartCoroutine(HeavySnipeRoutine());
                return;
            }

            // Tactical Movement: Kites or advances to maintain preferredDistance (5.5m)
            Vector2 toPlayer = (playerTransform.position - transform.position);
            Vector2 moveDir = Vector2.zero;

            if (dist < preferredDistance - 1.0f)
            {
                // Too close: back off
                moveDir = -toPlayer.normalized;
            }
            else if (dist > preferredDistance + 1.5f)
            {
                // Too far: advance
                moveDir = toPlayer.normalized;
            }
            else
            {
                // Strafe sideways around player
                Vector2 perp = new Vector2(-toPlayer.y, toPlayer.x).normalized;
                moveDir = perp * 0.7f;
            }

            rb.linearVelocity = moveDir * (moveSpeed * spdMultiplier);

            // Facing flip
            Vector2 aimDir = (playerTransform.position - transform.position).normalized;
            if (spriteRenderer != null && Mathf.Abs(aimDir.x) > 0.05f)
            {
                spriteRenderer.flipX = baseSpriteFacesLeft ? (aimDir.x > 0) : (aimDir.x < 0);
            }

            AnimateWalk();
        }

        private void UpdateCrossbowAim()
        {
            if (crossbowObj == null || playerTransform == null) return;

            Vector2 dir = (playerTransform.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            bool isFacingLeft = spriteRenderer != null && spriteRenderer.flipX;
            if (baseSpriteFacesLeft) isFacingLeft = !isFacingLeft;

            float offsetX = isFacingLeft ? -0.40f : 0.40f;
            crossbowObj.transform.localPosition = new Vector3(offsetX, -0.05f, 0f);
            crossbowObj.transform.rotation = Quaternion.Euler(0, 0, angle);

            if (crossbowRenderer != null)
            {
                crossbowRenderer.flipY = (dir.x < 0);
            }
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

        private IEnumerator HeavySnipeRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            snipeCooldown = isEnraged ? 2.5f : 4.2f;

            if (playerTransform == null) { isBusy = false; yield break; }
            Vector2 targetDir = (playerTransform.position - transform.position).normalized;

            // 0.5s Telegraph Red Laser Sight
            GameObject sight = CreateTelegraphLaser(transform.position, targetDir, 12f, 0.5f);
            yield return new WaitForSeconds(0.5f);
            if (sight != null) Destroy(sight);

            // Fire Heavy Arrow
            FireSingleArrow(targetDir, 36f * dmgMultiplier, 13f);
            DamageNumber.SpawnText(transform.position + Vector3.up * 1f, "<color=#EF4444>🏹 SNIPER SHOT!</color>", Color.red);

            yield return new WaitForSeconds(0.35f);
            isBusy = false;
        }

        private IEnumerator SpreadShotRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            spreadCooldown = isEnraged ? 4.5f : 7.5f;

            if (playerTransform == null) { isBusy = false; yield break; }
            Vector2 baseDir = (playerTransform.position - transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            // Telegraph
            yield return new WaitForSeconds(0.4f);

            int count = isEnraged ? 5 : 3;
            float spreadAngle = isEnraged ? 40f : 24f;
            float startAngle = baseAngle - (spreadAngle * 0.5f);
            float step = spreadAngle / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float ang = (startAngle + i * step) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                FireSingleArrow(dir, 22f * dmgMultiplier, 11f);
            }

            DamageNumber.SpawnText(transform.position + Vector3.up * 1.2f, $"<color=#F59E0B>🏹 CROSSBOW BARRAGE x{count}! 🏹</color>", Color.yellow);

            yield return new WaitForSeconds(0.4f);
            isBusy = false;
        }

        private IEnumerator RaidHornRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            raidHornCooldown = 18.0f;

            DamageNumber.SpawnText(transform.position + Vector3.up * 1.5f, "📯 RAID HORN! REINFORCEMENTS! 📯", new Color(1f, 0.85f, 0.2f, 1f));

            // Speed buff
            spdMultiplier = 1.35f;

            // Spawn 2 escort raiders
            if (WaveSpawner.Instance != null && playerTransform != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 spawnPos = transform.position + (Vector3)Random.insideUnitCircle.normalized * 2.5f;
                    WaveSpawner.Instance.ForceSpawnEnemy(null, spawnPos);
                }
            }

            yield return new WaitForSeconds(0.8f);
            spdMultiplier = 1.0f;
            isBusy = false;
        }

        private void FireSingleArrow(Vector2 dir, float dmg, float spd)
        {
            EnsureSprites();

            Vector3 spawnPos = transform.position + (Vector3)(dir * 0.5f);
            GameObject arrowObj = new GameObject("Pillager_Arrow");
            arrowObj.transform.position = spawnPos;

            float rotAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowObj.transform.rotation = Quaternion.Euler(0, 0, rotAngle);
            arrowObj.transform.localScale = Vector3.one * 1.1f;

            var sr = arrowObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;
            if (arrowSprite != null) sr.sprite = arrowSprite;

            var arrow = arrowObj.AddComponent<BossCrossbowArrow>();
            arrow.Initialize(dir, spd, dmg, 4.0f);
        }

        private GameObject CreateTelegraphLaser(Vector2 start, Vector2 dir, float length, float duration)
        {
            GameObject line = new GameObject("CrossbowTelegraph");
            var lr = line.AddComponent<LineRenderer>();
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.2f, 0.2f, 0.7f);
            lr.endColor = new Color(1f, 0.2f, 0.2f, 0.7f);
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, start + dir * length);

            Destroy(line, duration);
            return line;
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            dmgMultiplier = 1.35f;
            spdMultiplier = 1.25f;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.55f, 0.55f, 1f); // Menacing Crimson Tint
            }

            DamageNumber.SpawnText(transform.position + Vector3.up * 2f, "<color=#EF4444>🔥 PILLAGER ENRAGED! (+35% DMG, RAPID FIRE) 🔥</color>", Color.red);
            if (titleText != null)
            {
                titleText.text = "<color=#EF4444>ENRAGED PILLAGER CAPTAIN</color>";
            }
        }

        private void BuildOverheadUI()
        {
            if (bossCanvasObj != null) return;

            bossCanvasObj = new GameObject("PillagerBossBarUI");
            bossCanvasObj.transform.SetParent(transform, false);
            bossCanvasObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);

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
            bgImg.color = new Color(0.06f, 0.04f, 0.08f, 0.95f);

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
            fillImg.color = new Color(0.92f, 0.25f, 0.20f, 1f); // Pillager Crimson

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
            titleText.text = "PILLAGER CAPTAIN - RAID COMMANDER";
            titleText.color = new Color(1f, 0.8f, 0.25f, 1f); // Gold
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
                    Vector2 knockback = (collision.transform.position - transform.position).normalized;
                    ph.TakeDamage(contactDamage * dmgMultiplier, knockback, 5f, DamageSourceType.Contact);
                }
            }
        }

        private void OnDestroy()
        {
            if (health != null && health.CurrentHealth <= 0f)
            {
                // Guaranteed Boss Loot
                if (ExperienceManager.Instance != null)
                {
                    ExperienceManager.Instance.AddExp(300);
                    ExperienceManager.Instance.AddEmeralds(25);
                }
            }
        }
    }

    /// <summary>
    /// Hostile Arrow Projectile fired by Pillager Captain:
    /// Travels forward, checks collision with Player, deals Projectile damage.
    /// </summary>
    public class BossCrossbowArrow : MonoBehaviour
    {
        private Vector2 moveDir;
        private float moveSpeed;
        private float damage;
        private float lifetime;

        public void Initialize(Vector2 dir, float spd, float dmg, float maxLifetime)
        {
            moveDir = dir.normalized;
            moveSpeed = spd;
            damage = dmg;
            lifetime = maxLifetime;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.position += (Vector3)(moveDir * moveSpeed * Time.deltaTime);

            // Raycast check against Player
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.25f, moveDir, 0.1f);
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                var ph = hit.collider.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(damage, moveDir, 4f, DamageSourceType.Projectile);
                }
                Destroy(gameObject);
            }
        }
    }
}
