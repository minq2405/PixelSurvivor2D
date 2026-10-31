using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public enum DamageSourceType
    {
        Contact,
        Explosion,
        Poison,
        Projectile,
        BossSkill
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 120f;
        [SerializeField] private float iFramesDuration = 0.35f; // v2.1: Global contact I-frame 0.35s
        [SerializeField] private bool isInvincible = false;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hurtFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

        private float currentHealth;
        private bool isInvulnerable = false;
        private Color originalColor = Color.white;
        private Coroutine invulnerabilityCoroutine;
        private Rigidbody2D rb;
        private CircleCollider2D circleCol;
        private PlayerStats playerStats;

        // v2.1 Timing & Rules
        private float lastContactDamageTime = -999f;
        private float lastDamageTakenTime = -999f;
        private const float REGEN_PAUSE_DURATION = 1.0f; // 1.0s regen pause after hit
        private readonly Dictionary<int, float> nonContactLocks = new Dictionary<int, float>();
        private static readonly Collider2D[] touchingCollidersBuffer = new Collider2D[16];
        private ContactFilter2D enemyContactFilter;

        public event Action<float, float> OnHealthChanged;
        public event Action OnPlayerDied;

        public Transform Transform => transform;
        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsInvulnerable => isInvulnerable;
        public bool IsInvincible { get => isInvincible; set => isInvincible = value; }
        public bool IsRegenPaused => (Time.time - lastDamageTakenTime) < REGEN_PAUSE_DURATION;
        public float TimeSinceLastDamage => Time.time - lastDamageTakenTime;

        private void Awake()
        {
            currentHealth = maxHealth;
            rb = GetComponent<Rigidbody2D>();
            circleCol = GetComponent<CircleCollider2D>();
            playerStats = GetComponent<PlayerStats>();
            if (playerStats == null)
            {
                playerStats = gameObject.AddComponent<PlayerStats>();
            }
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            enemyContactFilter = new ContactFilter2D();
            enemyContactFilter.useTriggers = true;
            enemyContactFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
        }

        private void Start()
        {
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Update()
        {
            if (!IsAlive) return;

            // v2.1 Passive Health Regeneration (Steve 0.5 HP/s, Villager 0.2 HP/s)
            // Strictly paused for 1.0s after any damage taken
            if (!IsRegenPaused && playerStats != null && playerStats.EffectiveRegen > 0f)
            {
                float regenThisFrame = playerStats.EffectiveRegen * Time.deltaTime;
                if (currentHealth < maxHealth)
                {
                    currentHealth = Mathf.Min(maxHealth, currentHealth + regenThisFrame);
                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                }
            }
        }

        public void SetMaxHealthFromStats(float newMaxHealth)
        {
            float diff = newMaxHealth - maxHealth;
            maxHealth = newMaxHealth;
            if (diff > 0f) currentHealth = Mathf.Min(maxHealth, currentHealth + diff);
            else currentHealth = Mathf.Min(maxHealth, currentHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 0f)
        {
            TakeDamage(damage, knockbackDir, knockbackForce, DamageSourceType.Contact, -1);
        }

        public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce, DamageSourceType sourceType, int sourceId = -1)
        {
            if (!IsAlive || isInvincible) return;

            // 1. Contact damage check: Global 0.35s I-Frame
            if (sourceType == DamageSourceType.Contact)
            {
                if (isInvulnerable || (Time.time - lastContactDamageTime < iFramesDuration))
                {
                    return;
                }

                // Surround Damage Multiplier: DMG * (1 + 0.25 * min(N_extra, 4))
                int extraTouchers = CountExtraTouchingEnemies();
                float surroundMult = 1f + 0.25f * Mathf.Min(extraTouchers, 4);
                damage *= surroundMult;
                lastContactDamageTime = Time.time;
            }
            else
            {
                // 2. Non-contact damage check (Explosions, poison, projectiles, boss skills):
                // Separate 0.25s lock per source instance
                if (sourceId != -1)
                {
                    if (nonContactLocks.TryGetValue(sourceId, out float unlockTime) && Time.time < unlockTime)
                    {
                        return;
                    }
                    nonContactLocks[sourceId] = Time.time + 0.25f;
                }
            }

            // 3. Armor Damage Reduction: max(1, DMG * (1 - min(Armor, 0.35)))
            if (playerStats != null)
            {
                damage = playerStats.CalculateDamageTaken(damage);
            }

            // 4. Record hit timestamp to pause regeneration for 1.0s
            lastDamageTakenTime = Time.time;

            // 5. Apply Damage
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            Debug.Log($"<color=#EF4444>[Player Hurt]</color> Player took {damage:F1} ({sourceType}) damage! HP: {currentHealth:F1}/{maxHealth:F0}");

            // 6. Spawn Red Floating Damage Number
            DamageNumber.Spawn(transform.position + Vector3.up * 0.3f, damage, false);

            // 7. Apply Knockback
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
                // Trigger I-Frame blink
                if (invulnerabilityCoroutine != null) StopCoroutine(invulnerabilityCoroutine);
                invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine());
            }
        }

        private int CountExtraTouchingEnemies()
        {
            float checkRadius = (circleCol != null ? circleCol.radius : 0.35f) + 0.25f;
            int hitCount = Physics2D.OverlapCircle(transform.position, checkRadius, enemyContactFilter, touchingCollidersBuffer);
            int livingEnemies = 0;

            for (int i = 0; i < hitCount; i++)
            {
                var col = touchingCollidersBuffer[i];
                if (col == null || col.gameObject == gameObject) continue;

                var eh = col.GetComponent<EnemyHealth>();
                if (eh == null && col.transform.parent != null)
                {
                    eh = col.transform.parent.GetComponent<EnemyHealth>();
                }
                if (eh != null && eh.IsAlive)
                {
                    livingEnemies++;
                }
            }

            return Mathf.Max(0, livingEnemies - 1);
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
            // Check Totem of Undying Revive
            PassiveController passives = GetComponent<PassiveController>();
            if (passives != null && passives.TryUseTotemRevive())
            {
                return;
            }

            currentHealth = 0f;
            Debug.Log("<color=#DC2626>[GAME OVER]</color> Player has fallen in battle!");

            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.SetMovementLocked(true);
            }

            WeaponController wc = GetComponent<WeaponController>();
            if (wc != null)
            {
                wc.enabled = false;
            }

            OnPlayerDied?.Invoke();

            // Centralized TimeScaleManager pause lock
            TimeScaleManager.Instance?.AcquirePause("PlayerDeath");

            PixelSurvivor.UI.InGamePauseManager pauseMgr = FindAnyObjectByType<PixelSurvivor.UI.InGamePauseManager>();
            if (pauseMgr != null)
            {
                pauseMgr.TriggerGameOver();
            }
        }
    }
}
