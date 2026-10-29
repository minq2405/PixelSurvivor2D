using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Husk Shovel AI (Tier Cao - Cận chiến Thiết giáp):
    /// - Quái thiết giáp trâu bò (85 HP), kháng 50% lực đẩy lùi.
    /// - Khi tiếp cận cự ly 1.4m, dừng lại và vung xẻng bổ mạnh xuống đất (husk_shovel_attack).
    /// - Đòn bổ xẻng gây 18 sát thương trong bán kính quạt trước mặt, hất văng người chơi.
    /// </summary>
    public class HuskShovelAI : EnemyAI
    {
        [Header("Husk Shovel Combat Settings")]
        [SerializeField] private float attackRange = 1.35f;
        [SerializeField] private float attackCooldown = 1.8f;
        [SerializeField] private float swingDamage = 18f;
        [SerializeField] private float swingRadius = 1.1f;
        [SerializeField] private float swingKnockback = 8.0f;

        [Header("Husk Shovel Animations")]
        [SerializeField] private Sprite[] attackFrames;

        private float cooldownTimer = 0.5f;
        private bool isAttacking = false;
        private float attackTimer = 0f;
        private int attackFrameIdx = 0;
        private bool hasDealtDamageThisAttack = false;

        public bool IsAttacking => isAttacking;

        protected override void Awake()
        {
            base.Awake();

            // Set 50% Knockback resistance on EnemyHealth
            if (health != null)
            {
                health.KnockbackResistance = 0.5f;
            }
        }

        protected override void Update()
        {
            if (health != null && !health.IsAlive) return;

            cooldownTimer -= Time.deltaTime;

            if (isAttacking)
            {
                UpdateAttackAnimation();
                return;
            }

            base.Update();

            // Check if player entered shovel swing range
            if (cooldownTimer <= 0f && playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= attackRange)
                {
                    StartAttack();
                }
            }
        }

        protected override void FixedUpdate()
        {
            if (health != null && !health.IsAlive)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (isAttacking)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            base.FixedUpdate();
        }

        private void StartAttack()
        {
            isAttacking = true;
            attackTimer = 0f;
            attackFrameIdx = 0;
            hasDealtDamageThisAttack = false;
            rb.linearVelocity = Vector2.zero;

            // Face player
            if (playerTransform != null && spriteRenderer != null)
            {
                spriteRenderer.flipX = (playerTransform.position.x < transform.position.x);
            }
        }

        private void UpdateAttackAnimation()
        {
            if (attackFrames == null || attackFrames.Length == 0)
            {
                FinishAttack();
                return;
            }

            attackTimer += Time.deltaTime;
            float frameDuration = 1f / 12f; // ~12 fps shovel swing speed

            int currentStep = Mathf.FloorToInt(attackTimer / frameDuration);
            if (currentStep != attackFrameIdx && currentStep < attackFrames.Length)
            {
                attackFrameIdx = currentStep;
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = attackFrames[attackFrameIdx];
                }

                // Shovel slam impact at frame 6 (roughly 50% into animation)
                if (attackFrameIdx == 6 && !hasDealtDamageThisAttack)
                {
                    hasDealtDamageThisAttack = true;
                    PerformShovelSlam();
                }
            }

            if (attackTimer >= attackFrames.Length * frameDuration)
            {
                FinishAttack();
            }
        }

        private void PerformShovelSlam()
        {
            if (playerTransform == null) return;

            // Offset attack center slightly forward in facing direction
            Vector2 forwardDir = (spriteRenderer != null && spriteRenderer.flipX) ? Vector2.left : Vector2.right;
            Vector2 hitCenter = (Vector2)transform.position + (forwardDir * 0.7f);

            float dist = Vector2.Distance(hitCenter, playerTransform.position);
            if (dist <= swingRadius)
            {
                PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
                if (ph != null && ph.IsAlive)
                {
                    Vector2 knockDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                    ph.TakeDamage(swingDamage, knockDir, swingKnockback);
                    Debug.Log($"<color=#F59E0B>[Husk Shovel]</color> Slammed player with shovel for {swingDamage:F0} DMG!");
                }
            }
        }

        private void FinishAttack()
        {
            isAttacking = false;
            cooldownTimer = attackCooldown;
        }

        public void ConfigureHusk(Sprite[] walk, Sprite[] attack)
        {
            walkFrames = walk;
            attackFrames = attack;
        }
    }
}
