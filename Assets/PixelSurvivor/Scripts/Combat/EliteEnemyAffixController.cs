using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PixelSurvivor.Combat
{
    public class EliteEnemyAffixController : MonoBehaviour
    {
        [Header("Elite Status")]
        [SerializeField] private bool isElite = true;
        [SerializeField] private List<EliteAffix> activeAffixes = new List<EliteAffix>();

        [Header("References")]
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyAI ai;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Affix States")]
        // Shielded
        private int shieldCharges = 0;
        private GameObject shieldVfxObj;

        // Berserker
        private bool berserkerTriggered = false;
        private Color baseEliteColor;

        // Chilling Aura
        private GameObject chillingAuraVfxObj;
        private const float ChillingRadius = 4.0f;
        private const float ChillingSlowAmount = 0.20f; // 20% slow

        // Magnetic Pull
        private float magneticTimer = 0f;
        private const float MagneticInterval = 6.0f;
        private const float MagneticDuration = 1.5f;
        private bool isPulling = false;
        private float pullTimer = 0f;
        private GameObject magneticVfxObj;

        // Volatile
        private bool hasVolatile = false;

        // UI & Overhead Health Bar
        private GameObject overheadCanvasObj;
        private UnityEngine.UI.Slider healthSlider;
        private TMP_Text hpLabel;
        private TMP_Text titleLabel;

        private DifficultyEliteConfigSO activeConfig;
        private PlayerController player;
        private bool isDead = false;

        public bool IsElite => isElite;
        public List<EliteAffix> ActiveAffixes => activeAffixes;

        private void Awake()
        {
            if (health == null) health = GetComponent<EnemyHealth>();
            if (ai == null) ai = GetComponent<EnemyAI>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        public void InitializeElite(DifficultyEliteConfigSO config)
        {
            activeConfig = config;
            isElite = true;
            isDead = false;

            if (health == null) health = GetComponent<EnemyHealth>();
            if (ai == null) ai = GetComponent<EnemyAI>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // 1. Scale Up Transform (1.15x - 1.30x)
            transform.localScale = Vector3.Scale(transform.localScale, Vector3.one * config.scaleMultiplier);

            // 2. Multiply Max HP & Restore Full
            if (health != null)
            {
                float newHp = health.MaxHealth * config.hpMultiplier;
                health.SetMaxHealth(newHp);
                health.KnockbackResistance = Mathf.Min(0.70f, health.KnockbackResistance + 0.35f);
                health.OnDamageFilter += HandleDamageFilter;
            }

            // 3. Multiply Damage & Move Speed
            if (ai != null)
            {
                ai.ContactDamage *= config.damageMultiplier;
                ai.MoveSpeed *= config.speedMultiplier;
            }

            // 4. Elite Golden Aura / Color Tint
            baseEliteColor = new Color(1.0f, 0.90f, 0.35f, 1f); // Vibrant Gold
            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseEliteColor;
            }

            // 5. Select Random Affixes (0 to 3 based on difficulty)
            SelectAffixes(config);

            // 6. Spawn Overhead Elite Health Bar & Crown
            CreateOverheadHealthBar();

            // 7. Initialize VFX for chosen Affixes
            SetupAffixVFX();
        }

        private void SelectAffixes(DifficultyEliteConfigSO config)
        {
            activeAffixes.Clear();
            if (config.availableAffixes == null || config.availableAffixes.Count == 0) return;

            int count = Random.Range(config.minAffixes, config.maxAffixes + 1);
            List<EliteAffix> pool = new List<EliteAffix>(config.availableAffixes);

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                EliteAffix picked = pool[idx];
                pool.RemoveAt(idx);

                // GDD Rule 7: Prevent ChillingAura + Berserker combo in early hardcore
                if (picked == EliteAffix.ChillingAura && activeAffixes.Contains(EliteAffix.Berserker))
                {
                    continue;
                }

                if (picked != EliteAffix.None)
                {
                    activeAffixes.Add(picked);
                }
            }
        }

        private void SetupAffixVFX()
        {
            // Shielded: 5 hit blocks + Blue bubble
            if (activeAffixes.Contains(EliteAffix.Shielded))
            {
                shieldCharges = 5;
                CreateShieldVFX();
            }

            // Volatile: Explodes on death
            if (activeAffixes.Contains(EliteAffix.Volatile))
            {
                hasVolatile = true;
            }

            // Chilling Aura: Frost mist around feet
            if (activeAffixes.Contains(EliteAffix.ChillingAura))
            {
                CreateChillingVFX();
            }

            // Magnetic Pull
            if (activeAffixes.Contains(EliteAffix.MagneticPull))
            {
                magneticTimer = Random.Range(1.0f, 3.0f);
            }
        }

        private void Update()
        {
            if (isDead || health == null || !health.IsAlive) return;

            if (player == null) player = FindAnyObjectByType<PlayerController>();

            // Update Overhead Health Bar
            UpdateHealthBarUI();

            // 1. BERSERKER CHECK (HP < 50%)
            if (activeAffixes.Contains(EliteAffix.Berserker))
            {
                if (!berserkerTriggered && health.CurrentHealth <= (health.MaxHealth * 0.5f))
                {
                    TriggerBerserker();
                }
            }

            // 2. CHILLING AURA CHECK (4m radius around mob)
            if (activeAffixes.Contains(EliteAffix.ChillingAura) && player != null)
            {
                float dist = Vector2.Distance(transform.position, player.transform.position);
                if (dist <= ChillingRadius)
                {
                    player.ApplySlow(ChillingSlowAmount, 0.25f);
                }
            }

            // 3. MAGNETIC PULL (Every 6s, 1.5s pulse)
            if (activeAffixes.Contains(EliteAffix.MagneticPull) && player != null)
            {
                if (!isPulling)
                {
                    magneticTimer += Time.deltaTime;
                    if (magneticTimer >= MagneticInterval)
                    {
                        magneticTimer = 0f;
                        StartMagneticPulse();
                    }
                }
                else
                {
                    pullTimer += Time.deltaTime;
                    // Apply smooth suction towards mob center
                    Vector2 pullDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                    player.transform.position += (Vector3)(pullDir * 2.8f * Time.deltaTime);

                    if (pullTimer >= MagneticDuration)
                    {
                        EndMagneticPulse();
                    }
                }
            }
        }

        private void TriggerBerserker()
        {
            berserkerTriggered = true;
            if (spriteRenderer != null)
            {
                // Enraged Crimson Red
                spriteRenderer.color = new Color(1.0f, 0.25f, 0.25f, 1f);
            }
            if (ai != null)
            {
                ai.MoveSpeed *= 1.30f;
                ai.ContactDamage *= 1.30f;
            }
            DamageNumber.Spawn(transform.position + Vector3.up * 0.6f, 0f, "<color=#EF4444>BERSERK!</color>");
            Debug.Log($"<color=#EF4444>[BERSERKER]</color> {gameObject.name} entered Berserk mode (+30% Speed & Dmg)!");
        }

        private void StartMagneticPulse()
        {
            isPulling = true;
            pullTimer = 0f;
            if (magneticVfxObj == null)
            {
                magneticVfxObj = new GameObject("Magnetic_Vortex");
                magneticVfxObj.transform.SetParent(transform);
                magneticVfxObj.transform.localPosition = Vector3.zero;
                SpriteRenderer sr = magneticVfxObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 8;
                sr.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            }
            DamageNumber.Spawn(transform.position + Vector3.up * 0.5f, 0f, "<color=#38BDF8>VORTEX!</color>");
        }

        private void EndMagneticPulse()
        {
            isPulling = false;
            pullTimer = 0f;
            if (magneticVfxObj != null)
            {
                Destroy(magneticVfxObj);
                magneticVfxObj = null;
            }
        }

        private bool HandleDamageFilter(ref float damage, ref Vector2 knockbackDir, ref float knockbackForce)
        {
            // Shielded Affix intercepts and blocks 5 hits
            if (shieldCharges > 0)
            {
                shieldCharges--;
                damage = 0f;
                knockbackForce = 0f;
                DamageNumber.Spawn(transform.position + Vector3.up * 0.4f, 0f, "<color=#38BDF8>BLOCKED!</color>");

                if (shieldCharges <= 0)
                {
                    if (shieldVfxObj != null) Destroy(shieldVfxObj);
                    DamageNumber.Spawn(transform.position + Vector3.up * 0.6f, 0f, "<color=#FACC15>SHIELD BROKEN!</color>");
                }
                return false; // Damage blocked
            }

            return true; // Damage allowed
        }

        public void OnDealtDamageToPlayer(float contactDmg)
        {
            // Vampiric Affix: Heals 15% Max HP when touching player
            if (activeAffixes.Contains(EliteAffix.Vampiric) && health != null && health.IsAlive)
            {
                float healAmount = health.MaxHealth * 0.15f;
                health.Heal(healAmount);
                DamageNumber.Spawn(transform.position + Vector3.up * 0.5f, 0f, $"<color=#10B981>+{healAmount:F0} HP</color>");
                Debug.Log($"<color=#10B981>[Vampiric]</color> {gameObject.name} leeched {healAmount:F0} HP from player!");
            }
        }

        public void HandleDeath()
        {
            if (isDead) return;
            isDead = true;

            // 1. Notify WaveSpawner to decrement elite cap
            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.OnEliteDied();
            }

            // 2. Spawn Elite Special Drop Table (High Tier EXP, Chest, Specials)
            GameDifficulty diff = activeConfig != null ? activeConfig.difficulty : GameDifficulty.Normal;
            if (ExperienceManager.Instance != null)
            {
                ExperienceManager.Instance.SpawnEliteDrops(transform.position, diff);
            }

            // 3. Volatile Affix: Delayed AoE explosion
            if (hasVolatile)
            {
                StartCoroutine(DetonateVolatileRoutine(transform.position));
            }
        }

        private IEnumerator DetonateVolatileRoutine(Vector2 pos)
        {
            // Spawn warning telegraph circle
            GameObject warningObj = new GameObject("Volatile_Telegraph");
            warningObj.transform.position = pos;
            SpriteRenderer sr = warningObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            sr.color = new Color(1f, 0.2f, 0.2f, 0.45f);

            yield return new WaitForSeconds(0.8f);

            // Detonate AoE
            if (warningObj != null) Destroy(warningObj);

            DamageNumber.Spawn(pos, 0f, "<color=#EF4444>BOOM!</color>");

            // Hurt player (35 dmg)
            if (player != null)
            {
                float dist = Vector2.Distance(pos, player.transform.position);
                if (dist <= 3.0f)
                {
                    PlayerHealth ph = player.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        Vector2 kb = ((Vector2)player.transform.position - pos).normalized;
                        ph.TakeDamage(35f, kb, 8f);
                    }
                }
            }

            // Hurt nearby regular mobs (150 AoE damage)
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 3.5f);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyHealth eh = hits[i].GetComponent<EnemyHealth>();
                if (eh != null && eh.IsAlive && hits[i].gameObject != gameObject)
                {
                    Vector2 kb = ((Vector2)hits[i].transform.position - pos).normalized;
                    eh.TakeDamage(150f, kb, 6f);
                }
            }
        }

        private void CreateShieldVFX()
        {
            shieldVfxObj = new GameObject("Shield_Bubble");
            shieldVfxObj.transform.SetParent(transform);
            shieldVfxObj.transform.localPosition = Vector3.zero;
            shieldVfxObj.transform.localScale = Vector3.one * 1.35f;

            SpriteRenderer sr = shieldVfxObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 18;
            sr.color = new Color(0.2f, 0.75f, 1.0f, 0.55f); // Cyan energy shield
        }

        private void CreateChillingVFX()
        {
            chillingAuraVfxObj = new GameObject("Chilling_Aura");
            chillingAuraVfxObj.transform.SetParent(transform);
            chillingAuraVfxObj.transform.localPosition = new Vector3(0, -0.2f, 0);
            chillingAuraVfxObj.transform.localScale = new Vector3(ChillingRadius * 0.75f, ChillingRadius * 0.75f, 1f);

            SpriteRenderer sr = chillingAuraVfxObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            sr.color = new Color(0.4f, 0.85f, 1.0f, 0.25f); // Soft frost blue ring
        }

        private void CreateOverheadHealthBar()
        {
            overheadCanvasObj = new GameObject("Elite_Overhead_UI");
            overheadCanvasObj.transform.SetParent(transform);
            overheadCanvasObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            Canvas canvas = overheadCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;

            RectTransform rt = overheadCanvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1.6f, 0.45f);
            overheadCanvasObj.transform.localScale = Vector3.one * 0.85f;

            // Crown & Title Text
            GameObject titleObj = new GameObject("Elite_Title");
            titleObj.transform.SetParent(overheadCanvasObj.transform);
            titleObj.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            titleLabel = titleObj.AddComponent<TextMeshPro>();
            titleLabel.fontSize = 2.4f;
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.color = new Color(1.0f, 0.85f, 0.2f, 1f); // Gold

            string affixStr = "";
            if (activeAffixes.Count > 0)
            {
                affixStr = " [" + string.Join("·", activeAffixes) + "]";
            }
            titleLabel.text = $"★ ELITE ★{affixStr}";

            // Health Number
            GameObject hpNumObj = new GameObject("Elite_HP");
            hpNumObj.transform.SetParent(overheadCanvasObj.transform);
            hpNumObj.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            hpLabel = hpNumObj.AddComponent<TextMeshPro>();
            hpLabel.fontSize = 2.0f;
            hpLabel.alignment = TextAlignmentOptions.Center;
            hpLabel.color = Color.white;
            hpLabel.text = $"{health.CurrentHealth:F0}/{health.MaxHealth:F0}";
        }

        private void UpdateHealthBarUI()
        {
            if (hpLabel != null && health != null)
            {
                hpLabel.text = $"{Mathf.Max(0, health.CurrentHealth):F0}/{health.MaxHealth:F0}";
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamageFilter -= HandleDamageFilter;
            }

            if (!isDead && WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.OnEliteDied();
            }
        }
    }
}
