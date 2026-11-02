using System.Collections;
using UnityEngine;
using PixelSurvivor.Combat;
using PixelSurvivor.UI;

namespace PixelSurvivor
{
    /// <summary>
    /// Boss Reward Chest dropped upon defeating Mini-Bosses (Nether Golem I & II) or Special Bosses.
    /// - Appears as the iconic Golden Lucky Block chest with radiant golden pulse.
    /// - Takes 3 hits to crack open (via weapon attack or player contact).
    /// - Nether Golem I (5:00): Erupts +800 EXP direct, 4x Large EXP Orbs, 30 Emeralds, Cooked Steak (50 HP), Super Magnet + 1 Upgrade Card choice.
    /// - Nether Golem II (10:00): Erupts +1800 EXP direct, 6x Large EXP Orbs, 45 Emeralds, Cooked Steak (50 HP), Super Magnet + Guaranteed Weapon Evolution choice.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BossRewardChest : MonoBehaviour, IDamageable
    {
        [Header("Chest Properties")]
        [SerializeField] private bool guaranteesEvolution = false;
        [SerializeField] private int hitsToBreak = 3;

        [Header("Visuals & Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitFlashColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private float punchScale = 1.35f;

        private int currentHits = 0;
        private Vector3 initialScale;
        private Color baseChestColor = new Color(1f, 0.90f, 0.20f, 1f); // Radiant Golden Lucky Chest
        private Coroutine hitRoutine;
        private bool isBroken = false;
        private float lastPlayerContactTime = 0f;

        public Transform Transform => transform;
        public bool IsAlive => !isBroken;
        public bool GuaranteesEvolution { get => guaranteesEvolution; set => guaranteesEvolution = value; }

        private void Awake()
        {
            if (transform.localScale == Vector3.one)
            {
                transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            }
            initialScale = transform.localScale;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            // Ensure sprite is present
            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                var sp = Resources.Load<Sprite>("Drops/drop_lucky_block");
#if UNITY_EDITOR
                if (sp == null)
                {
                    sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_lucky_block.png");
                }
#endif
                if (sp != null) spriteRenderer.sprite = sp;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseChestColor;
                spriteRenderer.sortingOrder = 10;
            }

            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.size = new Vector2(0.9f, 0.9f);
            }
        }

        private void Update()
        {
            if (isBroken) return;

            // Gentle pulsating gold gleam
            if (spriteRenderer != null && hitRoutine == null)
            {
                float pulse = 0.90f + 0.10f * Mathf.Sin(Time.time * 5f);
                spriteRenderer.color = new Color(baseChestColor.r, baseChestColor.g * pulse, baseChestColor.b * pulse, 1f);
            }
        }

        public void Initialize(bool evolve)
        {
            guaranteesEvolution = evolve;
            baseChestColor = new Color(1f, 0.90f, 0.20f, 1f); // Always golden lucky block as requested
            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseChestColor;
            }
        }

        public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 0f)
        {
            if (isBroken) return;

            currentHits++;
            if (hitRoutine != null) StopCoroutine(hitRoutine);
            hitRoutine = StartCoroutine(HitFeedbackRoutine());

            if (currentHits >= hitsToBreak)
            {
                BreakOpen();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (isBroken) return;

            // Walking into the chest also strikes it (0.4s cooldown per hit)
            if (other.CompareTag("Player") && Time.time - lastPlayerContactTime > 0.4f)
            {
                lastPlayerContactTime = Time.time;
                TakeDamage(20f, Vector2.zero, 0f);
            }
        }

        private IEnumerator HitFeedbackRoutine()
        {
            if (spriteRenderer != null) spriteRenderer.color = hitFlashColor;

            float elapsed = 0f;
            float duration = 0.15f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scale = Mathf.Lerp(punchScale, 1f, progress);
                transform.localScale = initialScale * scale;
                yield return null;
            }

            transform.localScale = initialScale;
            if (spriteRenderer != null) spriteRenderer.color = baseChestColor;
            hitRoutine = null;
        }

        public void BreakOpen()
        {
            if (isBroken) return;
            isBroken = true;

            // 1. Erupt Super Rich Mini-Boss Loot (Huge EXP, Emerald shower, Cooked Steak, Super Magnet)
            if (ExperienceManager.Instance != null)
            {
                ExperienceManager.Instance.SpawnBossLuckyChestLoot(transform.position, guaranteesEvolution);
            }

            // 2. Open Upgrade or Evolution Modal (queued sequentially in LevelUpManager)
            var lum = FindAnyObjectByType<LevelUpManager>();
            if (lum != null)
            {
                lum.OpenBossChest(guaranteesEvolution);
            }

            Destroy(gameObject);
        }
    }
}
