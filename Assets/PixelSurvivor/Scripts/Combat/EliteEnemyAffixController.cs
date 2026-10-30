using System.Collections.Generic;
using UnityEngine;
using PixelSurvivor;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Unified Elite Enemy Controller:
    /// - Exactly ONE type of Elite: Mob + vfx_elite_aura_ring at feet + Stat Buff (HP x2.5, Damage x1.25, Scale 1.2x).
    /// - Normal mob sprite tint is retained (NOT yellow-tinted).
    /// - Animated vfx_elite_aura_ring (12 frames, PPU=32) plays smoothly under feet.
    /// - Displays a sleek, small overhead health bar in Elite Amethyst Purple (#A855F7).
    /// - Drops 150 EXP Yellow Gem + High-tier drops on defeat.
    /// </summary>
    public class EliteEnemyAffixController : MonoBehaviour
    {
        [Header("Elite Status")]
        [SerializeField] private bool isElite = true;

        [Header("References")]
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyAI ai;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("VFX & Overhead UI")]
        [SerializeField] private GameObject auraRingObj;
        [SerializeField] private SpriteRenderer auraRenderer;
        [SerializeField] private Sprite[] auraFrames;
        [SerializeField] private float auraFps = 12f;

        private float auraAnimTimer = 0f;
        private int currentAuraFrame = 0;

        private GameObject overheadCanvasObj;
        private UnityEngine.UI.Slider healthSlider;
        private UnityEngine.UI.Image fillImage;

        private DifficultyEliteConfigSO activeConfig;
        private bool isDead = false;

        public bool IsElite => isElite;
        public List<EliteAffix> ActiveAffixes { get; } = new List<EliteAffix>();

        private void Awake()
        {
            if (health == null) health = GetComponent<EnemyHealth>();
            if (ai == null) ai = GetComponent<EnemyAI>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void InitializeElite(DifficultyEliteConfigSO config)
        {
            activeConfig = config;
            isElite = true;
            isDead = false;

            if (health == null) health = GetComponent<EnemyHealth>();
            if (ai == null) ai = GetComponent<EnemyAI>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // 1. Scale Up Mob (1.20x)
            float scaleMul = config != null ? config.scaleMultiplier : 1.20f;
            transform.localScale = Vector3.Scale(transform.localScale, Vector3.one * scaleMul);

            // 2. Buff HP (2.5x)
            if (health != null)
            {
                float hpMul = config != null ? config.hpMultiplier : 2.5f;
                float newHp = health.MaxHealth * hpMul;
                health.SetMaxHealth(newHp);
                health.KnockbackResistance = Mathf.Min(0.75f, health.KnockbackResistance + 0.25f);
            }

            // 3. Buff Damage & Speed
            if (ai != null)
            {
                float dmgMul = config != null ? config.damageMultiplier : 1.25f;
                ai.ContactDamage *= dmgMul;

                if (config != null && config.speedMultiplier > 1.0f)
                {
                    ai.MoveSpeed *= config.speedMultiplier;
                }
            }

            // Note: Retain mob's original sprite color! Do NOT tint entire body yellow.

            // 4. Attach & Play Animated Aura Ring VFX under feet
            CreateAuraRing();

            // 5. Small Sleek Purple Health Bar above head
            CreateSmallPurpleHealthBar();
        }

        private void CreateAuraRing()
        {
            if (auraRingObj != null) return;

            // Load 12 frames of vfx_elite_aura_ring
            if (auraFrames == null || auraFrames.Length == 0)
            {
                auraFrames = Resources.LoadAll<Sprite>("VFX/vfx_elite_aura_ring");
#if UNITY_EDITOR
                if (auraFrames == null || auraFrames.Length == 0)
                {
                    var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Particles/VFX/vfx_elite_aura_ring.png");
                    List<Sprite> sList = new List<Sprite>();
                    foreach (var o in loaded)
                    {
                        if (o is Sprite s) sList.Add(s);
                    }
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
            // vfx_elite_aura_ring sprite pivot is already set at y = 0.714, placing ring exactly under feet
            auraRingObj.transform.localPosition = Vector3.zero;
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

        private void CreateSmallPurpleHealthBar()
        {
            if (overheadCanvasObj != null) return;

            overheadCanvasObj = new GameObject("Elite_HealthBar_UI");
            overheadCanvasObj.transform.SetParent(transform, false);
            overheadCanvasObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);

            Canvas canvas = overheadCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;

            RectTransform canvasRect = overheadCanvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(0.85f, 0.10f); // Sleek 0.85m x 0.10m
            canvasRect.localScale = Vector3.one;

            // Background Bar
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(overheadCanvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.08f, 0.05f, 0.14f, 0.90f); // Dark Obsidian-Purple

            // Slider Container
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(overheadCanvasObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(0.02f, 0.02f);
            sliderRect.offsetMax = new Vector2(-0.02f, -0.02f);

            healthSlider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
            healthSlider.interactable = false;

            // Fill Bar
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
            // Elite Amethyst Purple (#A855F7)
            fillImage.color = new Color(0.66f, 0.33f, 0.97f, 1.0f);

            healthSlider.targetGraphic = fillImage;
            healthSlider.fillRect = fillRect;
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

            // 2. Update Small Purple Health Bar
            if (healthSlider != null && health != null)
            {
                float ratio = Mathf.Clamp01(health.CurrentHealth / Mathf.Max(1f, health.MaxHealth));
                healthSlider.value = ratio;
                if (ratio <= 0f && overheadCanvasObj != null)
                {
                    overheadCanvasObj.SetActive(false);
                }
            }

            // 3. Keep Overhead UI upright (independent of enemy sprite X-flip)
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

            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.OnEliteDied();
            }

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
            // Unified Elite has standard combat behavior
        }

        public float HandleDamageFilter(float incomingDmg)
        {
            return incomingDmg;
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
