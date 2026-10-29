using System.Collections;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private bool isInvincible = false;
        private float currentHealth;

        [Header("Effects")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
        [SerializeField] private Sprite[] hitSparkFrames;
        [SerializeField] private Sprite[] deathSmokeFrames;

        [Header("Physics")]
        [SerializeField] private Rigidbody2D rb;

        public Transform Transform => transform;
        public bool IsAlive => currentHealth > 0;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            currentHealth = maxHealth;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        public void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = newMaxHealth;
            currentHealth = newMaxHealth;
        }

        public void SetInvincible(bool invincible)
        {
            isInvincible = invincible;
        }

        public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 0f)
        {
            if (!IsAlive) return;

            currentHealth -= damage;

            // Spawn Floating Damage Text
            DamageNumber.Spawn(transform.position, damage);

            // Spawn Hit Spark VFX
            SpawnHitSpark();

            // Apply Knockback
            if (rb != null && knockbackForce > 0f)
            {
                rb.linearVelocity = knockbackDir.normalized * knockbackForce;
            }

            // Flash effect
            if (spriteRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashRoutine());
            }

            Debug.Log($"<color=#EF4444>[Hit]</color> {gameObject.name} took {damage} damage! HP: {currentHealth}/{maxHealth}");

            if (isInvincible)
            {
                currentHealth = maxHealth; // Immortal dummy for combo testing
            }
            else if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private IEnumerator FlashRoutine()
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            flashCoroutine = null;
        }

        private void SpawnHitSpark()
        {
            if (hitSparkFrames != null && hitSparkFrames.Length > 0)
            {
                GameObject sparkObj = new GameObject("HitSpark");
                sparkObj.transform.position = transform.position + (Vector3)Random.insideUnitCircle * 0.2f;
                SpriteRenderer sr = sparkObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 15;
                AnimatedVFX vfx = sparkObj.AddComponent<AnimatedVFX>();
                vfx.Initialize(hitSparkFrames, 24f);
            }
        }

        private void Die()
        {
            Debug.Log($"<color=#10B981>[Death]</color> {gameObject.name} defeated!");

            // Spawn death smoke if available
            if (deathSmokeFrames != null && deathSmokeFrames.Length > 0)
            {
                GameObject smokeObj = new GameObject("DeathSmoke");
                smokeObj.transform.position = transform.position;
                SpriteRenderer sr = smokeObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 12;
                AnimatedVFX vfx = smokeObj.AddComponent<AnimatedVFX>();
                vfx.Initialize(deathSmokeFrames, 18f);
            }

            // Spawn EXP & Emerald Drops
            if (ExperienceManager.Instance != null)
            {
                ExperienceManager.Instance.SpawnDrops(transform.position, gameObject.name);
            }

            Destroy(gameObject);
        }

        public void SetFrames(Sprite[] sparks, Sprite[] deathSmoke)
        {
            hitSparkFrames = sparks;
            deathSmokeFrames = deathSmoke;
        }
    }
}
