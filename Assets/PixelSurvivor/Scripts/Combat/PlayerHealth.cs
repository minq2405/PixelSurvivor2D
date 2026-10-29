using System;
using System.Collections;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float iFramesDuration = 0.3f;
        [SerializeField] private bool isInvincible = false;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hurtFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

        private float currentHealth;
        private bool isInvulnerable = false;
        private Color originalColor = Color.white;
        private Coroutine invulnerabilityCoroutine;
        private Rigidbody2D rb;

        public event Action<float, float> OnHealthChanged;
        public event Action OnPlayerDied;

        public Transform Transform => transform;
        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsInvulnerable => isInvulnerable;
        public bool IsInvincible { get => isInvincible; set => isInvincible = value; }

        private void Awake()
        {
            currentHealth = maxHealth;
            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 0f)
        {
            if (!IsAlive || isInvulnerable || isInvincible) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            Debug.Log($"<color=#EF4444>[Player Hurt]</color> Player took {damage:F0} touch damage! HP: {currentHealth:F0}/{maxHealth:F0}");

            // Spawn Red Damage Number on Player
            DamageNumber.Spawn(transform.position + Vector3.up * 0.3f, damage, false);

            // Apply Knockback
            if (rb != null && knockbackForce > 0f)
            {
                rb.linearVelocity = knockbackDir.normalized * knockbackForce;
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
            else
            {
                // Trigger I-Frames (0.3s)
                if (invulnerabilityCoroutine != null) StopCoroutine(invulnerabilityCoroutine);
                invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine());
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Debug.Log($"<color=#10B981>[Player Heal]</color> Restored {amount:F0} HP! Current: {currentHealth:F0}/{maxHealth:F0}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void IncreaseMaxHealth(float amount, bool healEquivalent = true)
        {
            maxHealth += amount;
            if (healEquivalent)
            {
                currentHealth += amount;
            }
            Debug.Log($"<color=#10B981>[Buff]</color> Player Max Health increased to: {maxHealth:F0}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            isInvulnerable = true;
            float elapsed = 0f;
            float blinkInterval = 0.05f;

            while (elapsed < iFramesDuration)
            {
                if (spriteRenderer != null)
                {
                    // Alternate alpha and tint
                    Color c = hurtFlashColor;
                    c.a = 0.35f;
                    spriteRenderer.color = c;
                }

                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }

                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            isInvulnerable = false;
            invulnerabilityCoroutine = null;
        }

        private void Die()
        {
            currentHealth = 0f;
            Debug.Log("<color=#DC2626>[GAME OVER]</color> Player has fallen in battle!");

            // Lock movement immediately
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.SetMovementLocked(true);
            }

            // Disable player weapons
            WeaponController wc = GetComponent<WeaponController>();
            if (wc != null)
            {
                wc.enabled = false;
            }

            OnPlayerDied?.Invoke();

            // Freeze game immediately and guarantee Game Over modal
            Time.timeScale = 0f;
            PixelSurvivor.UI.InGamePauseManager pauseMgr = FindAnyObjectByType<PixelSurvivor.UI.InGamePauseManager>();
            if (pauseMgr != null)
            {
                pauseMgr.TriggerGameOver();
            }
        }
    }
}
