using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelSurvivor;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Pillager Captain Boss Controller:
    /// - Tactical Ranged Commander wielding Arcane Energy Orbs.
    /// - Signature Attack: Triple Energy Orb Barrage
    ///     * Displays 3 red telegraph targeting lines (3 tia) in a fan spread pointing at Player.
    ///     * Launches 3 animated Energy Orbs (vfx_energy_orb) along the 3 lines!
    /// - Skill 2: Raid Horn Rally (calls reinforcements, buffs movement speed).
    /// - Enrage (< 40% HP): Attacks faster, Crimson battle rage.
    /// - Single Sleek Overhead Boss Bar (#EF4444 Crimson), no text clutter.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PillagerCaptainBoss : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float baseHp = 12000f;
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float contactDamage = 30f;
        [SerializeField] private float preferredDistance = 6.0f;

        [Header("Animation & Sprites")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float animFps = 10f;
        [SerializeField] private bool baseSpriteFacesLeft = true;

        private static Sprite[] energyOrbFrames;

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
        private float orbBarrageCooldown = 2.5f; // Initial 2.5s breather
        private float raidHornCooldown = 14.0f;

        // Overhead UI
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
            ConfigureStats();
            BuildOverheadUI();
        }

        public void Initialize(Sprite[] walk, Sprite[] attack = null, Sprite crossbow = null, Sprite arrow = null)
        {
            walkFrames = walk;
            attackFrames = attack;

            EnsureSprites();
            ConfigureStats();
            BuildOverheadUI();
        }

        private void EnsureSprites()
        {
#if UNITY_EDITOR
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
            GetEnergyOrbFrames();
        }

        public static Sprite[] GetEnergyOrbFrames()
        {
            if (energyOrbFrames != null && energyOrbFrames.Length > 0) return energyOrbFrames;

            energyOrbFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_energy_orb");
#if UNITY_EDITOR
            if (energyOrbFrames == null || energyOrbFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Particles/VFX/vfx_energy_orb.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                list.Sort((a, b) => a.name.CompareTo(b.name));
                energyOrbFrames = list.ToArray();
            }
#endif
            return energyOrbFrames;
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
                health.IsBoss = true;
                health.KnockbackResistance = 0.85f;
            }

            transform.localScale = Vector3.one * 1.30f; // Imposing commander size
        }

        private void FindPlayer()
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) playerTransform = pc.transform;
        }

        private void Update()
        {
            if (health == null || !health.IsAlive) return;
            if (playerTransform == null) { FindPlayer(); return; }

            UpdateOverheadUI();

            // Enrage check (< 40% HP)
            if (!isEnraged && (health.CurrentHealth / health.MaxHealth) <= 0.40f)
            {
                TriggerEnrage();
            }

            // Cooldowns
            if (orbBarrageCooldown > 0f) orbBarrageCooldown -= Time.deltaTime;
            if (raidHornCooldown > 0f) raidHornCooldown -= Time.deltaTime;

            if (isBusy) return;

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // AI Decision Priority:
            // 1. Raid Horn Rally (calls backup & speeds up)
            if (raidHornCooldown <= 0f && dist <= 12f)
            {
                StartCoroutine(RaidHornRoutine());
                return;
            }

            // 2. Proactive 3-Orb Barrage (telegraphs 3 red laser lines, then fires 3 energy orbs)
            if (orbBarrageCooldown <= 0f && dist <= 11f)
            {
                StartCoroutine(TripleEnergyOrbRoutine());
                return;
            }

            // Movement: Tactical spacing (maintains preferred distance ~6.0m)
            Vector2 toPlayer = (playerTransform.position - transform.position).normalized;
            if (dist < preferredDistance - 0.8f)
            {
                // Back pedal
                rb.linearVelocity = -toPlayer * (moveSpeed * 0.75f * spdMultiplier);
            }
            else if (dist > preferredDistance + 0.8f)
            {
                // Advance
                rb.linearVelocity = toPlayer * (moveSpeed * spdMultiplier);
            }
            else
            {
                // Strafe slowly
                Vector2 strafe = new Vector2(-toPlayer.y, toPlayer.x);
                rb.linearVelocity = strafe * (moveSpeed * 0.5f * spdMultiplier);
            }

            // Sprite facing
            UpdateFacing(toPlayer.x);

            // Walk animation
            AnimateWalk();
        }

        private void UpdateFacing(float moveX)
        {
            if (spriteRenderer == null) return;
            if (Mathf.Abs(moveX) > 0.05f)
            {
                bool movingLeft = moveX < 0f;
                spriteRenderer.flipX = baseSpriteFacesLeft ? !movingLeft : movingLeft;
            }
        }

        private void AnimateWalk()
        {
            if (walkFrames == null || walkFrames.Length == 0 || spriteRenderer == null) return;

            animTimer += Time.deltaTime;
            if (animTimer >= 1f / animFps)
            {
                animTimer -= 1f / animFps;
                currentFrame = (currentFrame + 1) % walkFrames.Length;
                spriteRenderer.sprite = walkFrames[currentFrame];
            }
        }

        /// <summary>
        /// Signature Skill: 3 Red Telegraph Lines -> 3 Energy Orbs fired in fan spread
        /// </summary>
        private IEnumerator TripleEnergyOrbRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            orbBarrageCooldown = isEnraged ? 3.0f : 5.0f;

            if (playerTransform == null) { isBusy = false; yield break; }
            Vector2 centerDir = (playerTransform.position - transform.position).normalized;
            float baseAngle = Mathf.Atan2(centerDir.y, centerDir.x) * Mathf.Rad2Deg;

            // Attack frame
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = attackFrames[Mathf.Min(1, attackFrames.Length - 1)];
            }

            // 3 angles: -18°, 0°, +18°
            float[] spreadAngles = { -18f, 0f, 18f };
            Vector2[] shotDirs = new Vector2[3];
            GameObject[] telegraphs = new GameObject[3];

            float telegraphDuration = 0.6f;
            float telegraphLength = 12.0f;

            for (int i = 0; i < 3; i++)
            {
                float ang = (baseAngle + spreadAngles[i]) * Mathf.Deg2Rad;
                shotDirs[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                telegraphs[i] = CreateTelegraphLaser(transform.position, shotDirs[i], telegraphLength, telegraphDuration);
            }

            // Wait 0.6s telegraph
            yield return new WaitForSeconds(telegraphDuration);

            // Launch 3 Energy Orbs synchronized with full telegraph range (12m)
            for (int i = 0; i < 3; i++)
            {
                FireEnergyOrb(shotDirs[i], 32f * dmgMultiplier, 9.5f, telegraphLength);
            }

            yield return new WaitForSeconds(0.35f);
            isBusy = false;
        }

        private IEnumerator RaidHornRoutine()
        {
            isBusy = true;
            rb.linearVelocity = Vector2.zero;
            raidHornCooldown = 16.0f;

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

        private void FireEnergyOrb(Vector2 dir, float dmg, float spd, float range)
        {
            Sprite[] frames = GetEnergyOrbFrames();

            Vector3 spawnPos = transform.position + (Vector3)(dir * 0.8f);
            GameObject orbObj = new GameObject("Pillager_EnergyOrb");
            orbObj.transform.position = spawnPos;
            orbObj.transform.localScale = Vector3.one * 1.25f;

            var sr = orbObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 22;

            if (frames != null && frames.Length > 0)
            {
                var anim = orbObj.AddComponent<AnimatedVFX>();
                anim.Initialize(frames, 18f, null, loopAnim: true);
            }

            var orb = orbObj.AddComponent<BossEnergyOrbProjectile>();
            orb.Initialize(dir, spd, dmg, range);
        }

        private GameObject CreateTelegraphLaser(Vector2 start, Vector2 dir, float length, float duration)
        {
            GameObject line = new GameObject("TelegraphLaserLine");
            var lr = line.AddComponent<LineRenderer>();
            lr.startWidth = 0.10f;
            lr.endWidth = 0.10f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.15f, 0.15f, 0.85f);
            lr.endColor = new Color(1f, 0.15f, 0.15f, 0.85f);
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
            fillImg.color = new Color(0.92f, 0.25f, 0.20f, 1f); // Pillager Crimson

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
    /// Arcane Energy Orb Projectile fired by Pillager Captain:
    /// Travels forward, checks collision with Player, deals Projectile damage.
    /// Trajectory distance is synchronized with the targeting telegraph range!
    /// </summary>
    public class BossEnergyOrbProjectile : MonoBehaviour
    {
        private Vector2 moveDir;
        private float moveSpeed;
        private float damage;
        private float maxDistance;
        private float distanceTraveled = 0f;

        public void Initialize(Vector2 dir, float spd, float dmg, float maxRange = 12.0f)
        {
            moveDir = dir.normalized;
            moveSpeed = spd;
            damage = dmg;
            maxDistance = maxRange;
            distanceTraveled = 0f;
            // Failsafe timeout in case speed is very low
            Destroy(gameObject, (maxRange / Mathf.Max(1f, spd)) + 0.5f);
        }

        private void Update()
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position += (Vector3)(moveDir * step);
            distanceTraveled += step;

            // Raycast / circle cast check against Player along this frame's step
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.38f, moveDir, step);
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                var ph = hit.collider.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(damage, moveDir, 5f, DamageSourceType.Projectile);
                }
                Destroy(gameObject);
                return;
            }

            // Synchronize with telegraph range: despawn when reached maximum range
            if (distanceTraveled >= maxDistance)
            {
                Destroy(gameObject);
            }
        }
    }
}