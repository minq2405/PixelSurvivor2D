using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Vex AI:
    /// - Small airborne minion (scale ~0.65x, 25 HP, 4.2 m/s, 8 dmg).
    /// - Flying movement: ignores ground collisions, swoops directly toward player.
    /// - Sinusoidal wing flap oscillation for ghostly floating motion.
    /// - Plays vex_fly (12 frames) while pursuing.
    /// - Plays vex_attack (16 frames) when closing in on the player (< 1.2m) with an aggressive swoop.
    /// </summary>
    public class VexAI : EnemyAI
    {
        [Header("Vex Attack Settings")]
        [SerializeField] private float attackTriggerDistance = 1.2f;
        [SerializeField] private float attackSwoopSpeed = 5.5f;
        [SerializeField] private float attackCooldown = 2.0f;
        [SerializeField] private Sprite[] flyFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float attackFps = 16f;

        private bool isAttacking = false;
        private float attackTimer = 0f;
        private float attackCooldownTimer = 0.5f;
        private int attackFrameIdx = 0;
        private Vector2 attackTargetDir;
        private float hoverTime = 0f;
        private Transform visualChild;

        public bool IsAttacking => isAttacking;

        protected override void Awake()
        {
            base.Awake();
            visualChild = transform.Find("Visual");
            if (visualChild == null)
            {
                visualChild = transform;
            }
        }

        protected override void Start()
        {
            base.Start();

            if (flyFrames == null || flyFrames.Length == 0)
            {
                flyFrames = Resources.LoadAll<Sprite>("Enemies/Vex/vex_fly");
            }
            if (attackFrames == null || attackFrames.Length == 0)
            {
                attackFrames = Resources.LoadAll<Sprite>("Enemies/Vex/vex_attack");
            }

            if (flyFrames != null && flyFrames.Length > 0)
            {
                walkFrames = flyFrames;
            }
        }

        protected override void Update()
        {
            if (health != null && !health.IsAlive) return;

            attackCooldownTimer -= Time.deltaTime;

            if (isAttacking)
            {
                UpdateAttackAnimation();
                return;
            }

            // Normal flying animation via base Update
            base.Update();

            // Wing hover bobbing effect
            hoverTime += Time.deltaTime * 9f;
            if (visualChild != null && visualChild != transform)
            {
                visualChild.localPosition = new Vector3(0f, Mathf.Sin(hoverTime) * 0.12f, 0f);
            }

            // Check if within attack swoop range
            if (attackCooldownTimer <= 0f && playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= attackTriggerDistance)
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
                // Swoop towards attack direction
                rb.linearVelocity = attackTargetDir * attackSwoopSpeed;
                return;
            }

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            // Direct pursuit ignoring ground collisions
            Vector2 toPlayer = (playerTransform.position - transform.position);
            float dist = toPlayer.magnitude;

            // Reposition if too far offscreen
            distanceCheckTimer += Time.fixedDeltaTime;
            if (distanceCheckTimer >= DistanceCheckInterval)
            {
                distanceCheckTimer = 0f;
                if (dist > repositionDistance)
                {
                    RepositionAheadOfPlayer();
                    return;
                }
            }

            Vector2 dir = dist > 0.05f ? toPlayer / dist : Vector2.zero;
            rb.linearVelocity = dir * moveSpeed;

            // Flip sprite
            if (spriteRenderer != null && (Time.time - lastFlipTime >= MinFlipInterval))
            {
                if (spriteRenderer.flipX && dir.x > flipThreshold)
                {
                    spriteRenderer.flipX = false;
                    lastFlipTime = Time.time;
                }
                else if (!spriteRenderer.flipX && dir.x < -flipThreshold)
                {
                    spriteRenderer.flipX = true;
                    lastFlipTime = Time.time;
                }
            }
        }

        private void StartAttack()
        {
            isAttacking = true;
            attackTimer = 0f;
            attackFrameIdx = 0;
            attackCooldownTimer = attackCooldown;

            if (playerTransform != null)
            {
                attackTargetDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            }
            else
            {
                attackTargetDir = Vector2.right;
            }
        }

        private void UpdateAttackAnimation()
        {
            if (attackFrames == null || attackFrames.Length == 0)
            {
                EndAttack();
                return;
            }

            attackTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(attackFps, 1f);

            if (attackTimer >= frameDuration)
            {
                attackTimer -= frameDuration;
                attackFrameIdx++;

                if (attackFrameIdx >= attackFrames.Length)
                {
                    EndAttack();
                    return;
                }

                if (spriteRenderer != null && attackFrameIdx < attackFrames.Length)
                {
                    spriteRenderer.sprite = attackFrames[attackFrameIdx];
                }
            }
        }

        private void EndAttack()
        {
            isAttacking = false;
            attackTimer = 0f;
            attackFrameIdx = 0;
            if (flyFrames != null && flyFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = flyFrames[0];
            }
        }

        public void ConfigureVex(Sprite[] fly, Sprite[] attack)
        {
            flyFrames = fly;
            walkFrames = fly;
            attackFrames = attack;
        }
    }
}
