using System.Collections;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private bool isInvincible = false;
        [SerializeField] private bool isSummoned = false;
        private float currentHealth;

        public bool IsSummoned { get => isSummoned; set => isSummoned = value; }

        [Header("Effects")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
        [SerializeField] private Sprite[] hitSparkFrames;
        [SerializeField] private Sprite[] deathSmokeFrames;

        [Header("Physics")]
        [SerializeField] private Rigidbody2D rb;

        [Header("Defense & Knockback")]
        [Range(0f, 1f)]
        [SerializeField] private float knockbackResistance = 0f;
        [SerializeField] private bool isBoss = false;
        public bool IsBoss { get => isBoss; set => isBoss = value; }

        public static int TotalKills { get; private set; } = 0;
        public static event System.Action<int> OnKillCountChanged;
        public static event System.Action<EnemyHealth, string> OnEnemyKilled;

        public static void ResetKills()
        {
            TotalKills = 0;
            OnKillCountChanged?.Invoke(0);
        }

        public event System.Action OnDeath;
        public delegate bool DamageFilterDelegate(ref float damage, ref Vector2 knockbackDir, ref float knockbackForce);
        public event DamageFilterDelegate OnDamageFilter;

        public Transform Transform => transform;
        public bool IsAlive => currentHealth > 0;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float KnockbackResistance { get => knockbackResistance; set => knockbackResistance = Mathf.Clamp01(value); }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }

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

        private Coroutine burnCoroutine;

        public void ApplyBurn(float dps, float duration)
        {
            if (!IsAlive) return;
            if (burnCoroutine != null) StopCoroutine(burnCoroutine);
            burnCoroutine = StartCoroutine(BurnRoutine(dps, duration));
        }

        private IEnumerator BurnRoutine(float dps, float duration)
        {
            float elapsed = 0f;
            float tick = 1.0f; // 1s tick
            while (elapsed < duration && IsAlive)
            {
                yield return new WaitForSeconds(tick);
                elapsed += tick;
                if (IsAlive)
                {
                    TakeDamage(dps, Vector2.zero, 0f, false, "burn");
                }
            }
            burnCoroutine = null;
        }

        public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 0f)
        {
            TakeDamage(damage, knockbackDir, knockbackForce, false, null);
        }

        public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce, bool isCrit, string attackerWeaponId = null)
        {
            if (!IsAlive) return;

            if (OnDamageFilter != null)
            {
                if (!OnDamageFilter.Invoke(ref damage, ref knockbackDir, ref knockbackForce))
                {
                    return; // Damage blocked by Shielded or Invincible
                }
            }

            currentHealth -= damage;

            // Spawn Floating Damage Text
            DamageNumber.Spawn(transform.position, damage, isCrit);

            // Spawn Hit Spark VFX
            SpawnHitSpark();

            // Apply Knockback (scaled by resistance)
            if (rb != null && knockbackForce > 0f)
            {
                float effectiveKnockback = knockbackForce * (1.0f - knockbackResistance);
                rb.linearVelocity = knockbackDir.normalized * effectiveKnockback;
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
                Die(attackerWeaponId);
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

        private void Die(string killerWeaponId = null)
        {
            if (burnCoroutine != null)
            {
                StopCoroutine(burnCoroutine);
                burnCoroutine = null;
            }

            Debug.Log($"<color=#10B981>[Death]</color> {gameObject.name} defeated!");

            // Anti-exploit: Summoned mobs (Vex, Slimes, Boss minions) strictly DO NOT trigger on-kill heal, drop 0 EXP, 0 Emeralds (v2.1 Section 7.1)
            if (!isSummoned)
            {
                OnEnemyKilled?.Invoke(this, killerWeaponId);
            }
            else
            {
                WaveSpawner.Instance?.OnSummonDied();
            }

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

            // Spawn EXP & Emerald Drops (Elite vs Regular vs Summoned)
            if (!isSummoned)
            {
                var eliteCtrl = GetComponent<EliteEnemyAffixController>();
                if (eliteCtrl != null && eliteCtrl.IsElite)
                {
                    eliteCtrl.HandleDeath();
                }
                else if (ExperienceManager.Instance != null)
                {
                    ExperienceManager.Instance.SpawnDrops(transform.position, gameObject.name);
                }
            }

            TotalKills++;
            OnKillCountChanged?.Invoke(TotalKills);
            OnDeath?.Invoke();

            Destroy(gameObject);
        }

        public void SetFrames(Sprite[] sparks, Sprite[] deathSmoke)
        {
            hitSparkFrames = sparks;
            deathSmokeFrames = deathSmoke;
        }
    }
}
