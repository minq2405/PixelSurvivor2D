using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PixelSurvivor;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Unified Elite Enemy Controller (v2.1 Architecture):
    /// - Exactly ONE unified Elite visual standard:
    ///     * Amethyst Aura Ring (12 frames, PPU=32) animated smoothly under feet.
    ///     * Small sleek overhead purple health bar (#A855F7) + Affix tag.
    ///     * Scale 1.20x, HP 2.5x, Damage 1.30x, Knockback Resistance +0.25.
    /// - 5 Affixes per GDD:
    ///     1. Swift (+12% speed; excludes Spider, Vex, Phantom; mutex with Berserk).
    ///     2. Armored (25% damage reduction).
    ///     3. Berserk (+35% contact damage; mutex with Swift).
    ///     4. Regenerating (Regenerates 1% Max HP/s).
    ///     5. Volatile (Death explosion 2.0m dealing 30 * (1 + 0.025t) dmg to player).
    /// - Drops 150 EXP Yellow Gem + Roll for Golden Lucky Block (20%/17%/15% with +10% pity step).
    /// </summary>
    public class EliteEnemyAffixController : MonoBehaviour
    {
        [Header("Elite Status & Affix")]
        [SerializeField] private bool isElite = true;
        [SerializeField] private EliteAffix activeAffix = EliteAffix.None;

        [Header("References")]
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyAI ai;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("VFX & Overhead UI")]
        [SerializeField] private GameObject auraRingObj;
        [SerializeField] private SpriteRenderer auraRenderer;
        [SerializeField] private Sprite[] auraFrames;
        [SerializeField] private float auraFps = 12f;
        [SerializeField] private float auraVerticalOffset = 0f;

        private float auraAnimTimer = 0f;
        private int currentAuraFrame = 0;
        private float regenTimer = 0f;

        private GameObject overheadCanvasObj;
        private UnityEngine.UI.Slider healthSlider;
        private UnityEngine.UI.Image fillImage;

        private DifficultyEliteConfigSO activeConfig;
        private bool isDead = false;

        public bool IsElite => isElite;
        public EliteAffix ActiveAffix => activeAffix;

        private void Awake()
        {
            if (health == null) health = GetComponent<EnemyHealth>();
            if (ai == null) ai = GetComponent<EnemyAI>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Strictly guard: Bosses and Mini-bosses can NEVER be Elite
            if (health != null && health.IsBoss)
            {
                Destroy(this);
                return;
            }
        }

        public void InitializeElite(DifficultyEliteConfigSO config)
        {
            if (health == null) health = GetComponent<EnemyHealth>();
            if (health != null && health.IsBoss)
            {
                Destroy(this);
                return;
            }

            activeConfig = config;
            isElite = true;
            isDead = false;

            if (ai == null) ai = GetComponent<EnemyAI>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // 1. Scale Up Mob (1.20x per v2.1)
            float scaleMul = config != null ? config.scaleMultiplier : 1.20f;
            transform.localScale = Vector3.Scale(transform.localScale, Vector3.one * scaleMul);

            // 2. Buff HP (2.5x per v2.1) & Knockback Resistance (+0.25)
            if (health != null)
            {
                float hpMul = config != null ? config.hpMultiplier : 2.5f;
                float newHp = health.MaxHealth * hpMul;
                health.SetMaxHealth(newHp);
                health.KnockbackResistance = Mathf.Min(0.75f, health.KnockbackResistance + 0.25f);
                health.OnDamageFilter += HandleIncomingDamageFilter;
            }

            // 3. Buff Base Contact Damage (1.30x per v2.1)
            if (ai != null)
            {
                float dmgMul = config != null ? config.damageMultiplier : 1.30f;
                ai.ContactDamage *= dmgMul;
            }

            // 4. Roll exactly 1 of 5 Affixes (v2.1 Section 8.3)
            RollAndApplyAffix();

            // 5. Attach 12-Frame Amethyst Aura Ring VFX under feet
            CreateAuraRing();

            // 6. Overhead Purple Health Bar + Affix Tag
            CreateOverheadUI();
        }

        private void RollAndApplyAffix()
        {
            List<EliteAffix> candidates = new List<EliteAffix>
            {
                EliteAffix.Armored,
                EliteAffix.Regenerating,
                EliteAffix.Volatile
            };

            string mobName = gameObject.name;
            bool isFastMob = mobName.Contains("Spider") || mobName.Contains("Vex") || mobName.Contains("Phantom");

            // Swift is excluded on inherently fast mobs (Spider, Vex, Phantom)
            if (!isFastMob)
            {
                candidates.Add(EliteAffix.Swift);
            }

            candidates.Add(EliteAffix.Berserk);

            activeAffix = candidates[Random.Range(0, candidates.Count)];

            // Apply Affix Modifications
            switch (activeAffix)
            {
                case EliteAffix.Swift:
                    if (ai != null) ai.MoveSpeed *= 1.12f; // +12% Speed
                    break;
                case EliteAffix.Berserk:
                    if (ai != null) ai.ContactDamage *= 1.35f; // +35% DMG
                    break;
                case EliteAffix.Armored:
                    // Handled in HandleIncomingDamageFilter (-25% incoming damage)
                    break;
                case EliteAffix.Regenerating:
                    // Handled in Update() (1% Max HP/s)
                    break;
                case EliteAffix.Volatile:
                    // Handled in HandleDeath() (Explosion on death)
                    break;
            }

            Debug.Log($"<color=#A855F7>[Elite Affix]</color> {gameObject.name} assigned Affix: <b>{activeAffix}</b>");
        }

        private bool HandleIncomingDamageFilter(ref float damage, ref Vector2 knockbackDir, ref float knockbackForce)
        {
            if (activeAffix == EliteAffix.Armored)
            {
                damage *= 0.75f; // 25% damage reduction
            }
            return true;
        }

        private void CreateAuraRing()
        {
            if (auraRingObj != null) return;

            if (auraFrames == null || auraFrames.Length == 0)
            {
                auraFrames = Resources.LoadAll<Sprite>("VFX/vfx_elite_aura_ring");
#if UNITY_EDITOR
                if (auraFrames == null || auraFrames.Length == 0)
                {
                    var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Particles/VFX/vfx_elite_aura_ring.png");
                    List<Sprite> sList = new List<Sprite>();
                    foreach (var o in loaded) if (o is Sprite s) sList.Add(s);
                    auraFrames = sList.ToArray();
                }
#endif
                if (auraFrames != null && auraFrames.Length > 0)
                {
                    System.Array.Sort(auraFrames, (a, b) => ExtractFrameIndex(a.name).CompareTo(ExtractFrameIndex(b.name)));
                }
            }

            auraRingObj = new GameObject("Elite_Aura_Ring");
            auraRingObj.transform.SetParent(transform, false);

            float feetLocalY = -0.47f;
            SpriteRenderer mobSr = GetComponentInChildren<SpriteRenderer>();
            if (mobSr != null)
            {
                Vector3 worldBottom = new Vector3(transform.position.x, mobSr.bounds.min.y, transform.position.z);
                feetLocalY = transform.InverseTransformPoint(worldBottom).y;
            }

            auraRingObj.transform.localPosition = new Vector3(0f, feetLocalY + auraVerticalOffset, 0f);
            auraRingObj.transform.localScale = Vector3.one;

            auraRenderer = auraRingObj.AddComponent<SpriteRenderer>();
            auraRenderer.sortingOrder = 4; // Render beneath mob sprite (5)

            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit") ?? Shader.Find("Sprites/Default");
            if (unlit != null)
            {
                auraRenderer.material = new Material(unlit);
            }

            if (auraFrames != null && auraFrames.Length > 0)
            {
                auraRenderer.sprite = auraFrames[0];
            }
        }

        private int ExtractFrameIndex(string name)
        {
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int val))
            {
                return val;
            }
            return 0;
        }

        private void CreateOverheadUI()
        {
            if (overheadCanvasObj != null) return;
            if (health != null && health.IsBoss) return; // Bosses have their own Boss Bar

            overheadCanvasObj = new GameObject("Elite_Overhead_UI");
            overheadCanvasObj.transform.SetParent(transform, false);

            float headLocalY = 0.65f;
            SpriteRenderer mobSr = GetComponentInChildren<SpriteRenderer>();
            if (mobSr != null)
            {
                Vector3 worldTop = new Vector3(transform.position.x, mobSr.bounds.max.y, transform.position.z);
                headLocalY = transform.InverseTransformPoint(worldTop).y + 0.15f;
            }
            overheadCanvasObj.transform.localPosition = new Vector3(0f, headLocalY, 0f);

            Canvas canvas = overheadCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;

            RectTransform canvasRect = overheadCanvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100f, 12f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);

            // Background Bar
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(overheadCanvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.08f, 0.05f, 0.14f, 0.90f);

            // Slider Container
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(overheadCanvasObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(1f, 1f);
            sliderRect.offsetMax = new Vector2(-1f, -1f);

            healthSlider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
            healthSlider.interactable = false;

            // Fill Bar (Amethyst Purple #A855F7)
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.66f, 0.33f, 0.97f, 1.0f);

            healthSlider.targetGraphic = fillImage;
            healthSlider.fillRect = fillRect;

            // NOTE: Affix text label is omitted per user specification:
            // "và phần tên elite hãy bỏ đi, chỉ cần có vfx là được không cần tên nữa"
        }

        private void Update()
        {
            if (isDead) return;

            // 1. Loop 12-frame Aura Ring animation
            if (auraFrames != null && auraFrames.Length > 0 && auraRenderer != null)
            {
                auraAnimTimer += Time.deltaTime;
                if (auraAnimTimer >= 1f / auraFps)
                {
                    auraAnimTimer -= 1f / auraFps;
                    currentAuraFrame = (currentAuraFrame + 1) % auraFrames.Length;
                    auraRenderer.sprite = auraFrames[currentAuraFrame];
                }
            }

            // 2. Regenerating Affix (1% Max HP/s)
            if (activeAffix == EliteAffix.Regenerating && health != null && health.IsAlive)
            {
                regenTimer += Time.deltaTime;
                if (regenTimer >= 1.0f)
                {
                    regenTimer = 0f;
                    health.Heal(health.MaxHealth * 0.01f);
                }
            }

            // 3. Update Overhead Health Bar
            if (healthSlider != null && health != null)
            {
                float ratio = Mathf.Clamp01(health.CurrentHealth / Mathf.Max(1f, health.MaxHealth));
                healthSlider.value = ratio;
                if (ratio <= 0f && overheadCanvasObj != null)
                {
                    overheadCanvasObj.SetActive(false);
                }
            }

            // 4. Keep Overhead UI upright
            if (overheadCanvasObj != null)
            {
                overheadCanvasObj.transform.rotation = Quaternion.identity;
                float headY = 0.65f * (transform.localScale.y / 1.20f);
                overheadCanvasObj.transform.position = transform.position + Vector3.up * headY;
            }
        }

        public void HandleDeath()
        {
            if (isDead) return;
            isDead = true;

            if (health != null)
            {
                health.OnDamageFilter -= HandleIncomingDamageFilter;
            }

            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.OnEliteDied();
            }

            // Volatile Affix Death Explosion
            if (activeAffix == EliteAffix.Volatile)
            {
                TriggerVolatileExplosion();
            }

            // Roll for Golden Lucky Block Chest (Pity & Cap system v2.1)
            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.TryRollEliteChest(transform.position);
            }

            // Spawn 150 EXP Yellow Gem & Drops
            GameDifficulty diff = activeConfig != null ? activeConfig.difficulty : GameDifficulty.Normal;
            if (ExperienceManager.Instance != null)
            {
                ExperienceManager.Instance.SpawnEliteDrops(transform.position, diff);
            }

            if (overheadCanvasObj != null) Destroy(overheadCanvasObj);
            if (auraRingObj != null) Destroy(auraRingObj);
        }

        public void OnDealtDamageToPlayer(float dmg)
        {
            // Optional affix combat reactions
        }

        private void TriggerVolatileExplosion()
        {
            float t = WaveSpawner.Instance != null ? (WaveSpawner.Instance.TimeElapsed / 60f) : 0f;
            float explosionDmg = 30f * (1.0f + 0.025f * t);
            float radius = 2.0f;

            Debug.Log($"<color=#EF4444>[VOLATILE EXPLOSION]</color> {gameObject.name} detonated in {radius}m radius ({explosionDmg:F1} dmg)!");
            DamageNumber.SpawnText(transform.position + Vector3.up * 0.5f, "💥 BOOM!", Color.red);

            PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
            if (ph != null)
            {
                float dist = Vector2.Distance(transform.position, ph.transform.position);
                if (dist <= radius)
                {
                    Vector2 kbDir = ((Vector2)ph.transform.position - (Vector2)transform.position).normalized;
                    ph.TakeDamage(explosionDmg, kbDir, 4.0f, DamageSourceType.Explosion, -1);
                }
            }
        }

        private void OnDestroy()
        {
            if (overheadCanvasObj != null) Destroy(overheadCanvasObj);
            if (auraRingObj != null) Destroy(auraRingObj);

            if (!isDead && WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.OnEliteDied();
            }
        }
    }
}
