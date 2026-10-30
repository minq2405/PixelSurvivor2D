using System.Collections;
using UnityEngine;
using PixelSurvivor.Combat;

namespace PixelSurvivor
{
    /// <summary>
    /// In-game Golden Lucky Block Chest.
    /// A stationary, breakable treasure prop that players/weapons attack to crack open.
    /// When broken, it bursts open and showers EXP gems, emeralds, and bonus powerups.
    /// Implements IDamageable so all player weapons automatically detect and strike it.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BreakableLuckyBlock : MonoBehaviour, IDamageable
    {
        [Header("Durability Settings")]
        [Tooltip("Number of weapon hits required to crack open the chest")]
        [SerializeField] private int hitsToBreak = 3;
        [SerializeField] private float maxHealth = 60f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.95f, 0.5f, 1f);
        [SerializeField] private float punchScaleFactor = 1.25f;

        private int currentHits = 0;
        private float currentHealth = 60f;
        private Vector3 initialScale;
        private Color initialColor = Color.white;
        private Coroutine hitFeedbackCoroutine;
        private bool isBroken = false;

        public Transform Transform => transform;
        public bool IsAlive => !isBroken;
        public int HitsRemaining => Mathf.Max(0, hitsToBreak - currentHits);

        private void Awake()
        {
            currentHealth = maxHealth;
            initialScale = transform.localScale;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) initialColor = spriteRenderer.color;

            // Ensure collider is set to trigger
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 0f)
        {
            if (isBroken) return;

            currentHits++;
            currentHealth -= damage;

            // Hit feedback animation
            if (Application.isPlaying)
            {
                if (hitFeedbackCoroutine != null) StopCoroutine(hitFeedbackCoroutine);
                hitFeedbackCoroutine = StartCoroutine(HitFeedbackRoutine());
            }

            if (currentHits >= hitsToBreak)
            {
                BreakOpen();
            }
            else
            {
                // Visual crack text
                string crackText = $"<color=#FACC15>CRACK! ({currentHits}/{hitsToBreak})</color>";
                DamageNumber.SpawnText(transform.position + Vector3.up * 0.4f, crackText, Color.yellow);
            }
        }

        private IEnumerator HitFeedbackRoutine()
        {
            if (spriteRenderer != null) spriteRenderer.color = hitFlashColor;

            float duration = 0.15f;
            float elapsed = 0f;
            float wobbleAngle = Random.Range(-10f, 10f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(punchScaleFactor, 1f, t);
                transform.localScale = initialScale * scale;
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(wobbleAngle, 0f, t));
                yield return null;
            }

            transform.localScale = initialScale;
            transform.rotation = Quaternion.identity;
            if (spriteRenderer != null) spriteRenderer.color = initialColor;
        }

        public void BreakOpen()
        {
            if (isBroken) return;
            isBroken = true;

            // Floating banner
            DamageNumber.SpawnText(transform.position + Vector3.up * 0.6f, "<color=#FACC15>★ LUCKY CHEST OPENED! ★</color>", new Color(1f, 0.85f, 0.1f));

            // Erupt loot items
            if (ExperienceManager.Instance != null)
            {
                ExperienceManager.Instance.SpawnLuckyChestLoot(transform.position);
            }

            // Clean destruction
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
