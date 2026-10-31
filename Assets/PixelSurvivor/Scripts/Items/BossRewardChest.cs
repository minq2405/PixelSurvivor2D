using System.Collections;
using UnityEngine;
using PixelSurvivor.Combat;
using PixelSurvivor.UI;

namespace PixelSurvivor
{
    /// <summary>
    /// Boss Reward Chest dropped upon defeating Mini-Bosses (Nether Golem I & II) or Special Bosses.
    /// - Nether Golem I (5:00): Grants +1 Item Upgrade Choice (evolves: false).
    /// - Nether Golem II (10:00): Guarantees Weapon Evolution (evolves: true).
    /// Requires 3 hits to open, providing immediate tactile satisfaction.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossRewardChest : MonoBehaviour, IDamageable
    {
        [Header("Chest Properties")]
        [SerializeField] private bool guaranteesEvolution = false;
        [SerializeField] private int hitsToBreak = 3;
        [SerializeField] private float maxHealth = 60f;

        [Header("Visuals & Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private float punchScale = 1.30f;

        private int currentHits = 0;
        private Vector3 initialScale;
        private Color initialColor = Color.white;
        private Coroutine hitRoutine;
        private bool isBroken = false;

        public Transform Transform => transform;
        public bool IsAlive => !isBroken;
        public bool GuaranteesEvolution { get => guaranteesEvolution; set => guaranteesEvolution = value; }

        private void Awake()
        {
            initialScale = transform.localScale;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) initialColor = spriteRenderer.color;

            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        public void Initialize(bool evolve)
        {
            guaranteesEvolution = evolve;
            if (spriteRenderer != null)
            {
                // Golden chest tint for Golem I, Mythic Amethyst tint for Golem II
                spriteRenderer.color = evolve ? new Color(0.85f, 0.45f, 1f, 1f) : new Color(1f, 0.85f, 0.2f, 1f);
                initialColor = spriteRenderer.color;
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
            else
            {
                string crackText = guaranteesEvolution
                    ? $"<color=#C084FC>MYTHIC CHEST! ({currentHits}/{hitsToBreak})</color>"
                    : $"<color=#FACC15>BOSS CHEST! ({currentHits}/{hitsToBreak})</color>";
                DamageNumber.SpawnText(transform.position + Vector3.up * 0.5f, crackText, Color.yellow);
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
            if (spriteRenderer != null) spriteRenderer.color = initialColor;
            hitRoutine = null;
        }

        public void BreakOpen()
        {
            if (isBroken) return;
            isBroken = true;

            string openText = guaranteesEvolution
                ? "<color=#C084FC>👑 MYTHIC EVOLUTION UNLOCKED! 👑</color>"
                : "<color=#FACC15>📦 BOSS UPGRADE REWARD! 📦</color>";
            DamageNumber.SpawnText(transform.position + Vector3.up * 0.8f, openText, Color.yellow);

            // Grant Emerald shower
            int bonusEmeralds = guaranteesEvolution ? 15 : 8;
            if (ExperienceManager.Instance != null)
            {
                ExperienceManager.Instance.AddEmeralds(bonusEmeralds);
            }

            // Open Upgrade or Evolution Modal
            var lum = FindAnyObjectByType<LevelUpManager>();
            if (lum != null)
            {
                lum.OpenBossChest(guaranteesEvolution);
            }

            Destroy(gameObject);
        }
    }
}
