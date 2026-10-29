using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Slime AI with Jump Physics Synchronization:
    /// - Frame 0..5 (Squash/Windup): Velocity = 0 (tích lực, co người chuẩn bị bung).
    /// - Frame 6..10 (Leap): Velocity = leapDirection * jumpSpeed (phóng vọt về hướng Player).
    /// - Frame 11..14 (Land & Rest): Velocity = 0 (tiếp đất dẹp người, hồi phục).
    /// - Split on Death: Khi Slime lớn bị tiêu diệt, tự động tách thành 2 Slime nhỏ (50% HP, 0.58x scale).
    /// </summary>
    public class SlimeAI : EnemyAI
    {
        [Header("Slime Jump Settings")]
        [SerializeField] private float jumpSpeed = 4.8f;
        [SerializeField] private bool isSmallSlime = false;
        [SerializeField] private float splitHealth = 25f;

        private Vector2 leapDirection = Vector2.right;

        public bool IsSmallSlime => isSmallSlime;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            if (health != null)
            {
                health.OnDeath += HandleDeath;
            }
        }

        protected override void Update()
        {
            if (health != null && !health.IsAlive) return;
            if (walkFrames == null || walkFrames.Length == 0) return;

            animTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(animationFps, 1f);

            if (animTimer >= frameDuration)
            {
                int steps = (int)(animTimer / frameDuration);
                animTimer -= steps * frameDuration;
                int previousFrame = currentFrame;
                currentFrame = (currentFrame + steps) % walkFrames.Length;

                // When transitioning into leap frames (Frame 6), lock direction toward Player
                if (previousFrame < 6 && currentFrame >= 6)
                {
                    if (playerTransform != null)
                    {
                        Vector2 toPlayer = (playerTransform.position - transform.position);
                        if (toPlayer.sqrMagnitude > 0.01f)
                        {
                            leapDirection = toPlayer.normalized;
                        }
                    }

                    // Flip sprite horizontally towards jump direction
                    if (spriteRenderer != null)
                    {
                        if (leapDirection.x < -0.1f) spriteRenderer.flipX = true;
                        else if (leapDirection.x > 0.1f) spriteRenderer.flipX = false;
                    }
                }

                if (spriteRenderer != null && currentFrame < walkFrames.Length)
                {
                    spriteRenderer.sprite = walkFrames[currentFrame];
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

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            // Synchronize velocity strictly with jump frames:
            // Frame 6..10: Airborne leap phase
            if (currentFrame >= 6 && currentFrame <= 10)
            {
                rb.linearVelocity = leapDirection * jumpSpeed;
            }
            else
            {
                // Frame 0..5 (windup) and 11..14 (land/rest)
                rb.linearVelocity = Vector2.zero;
            }

            // Periodic offscreen distance check
            distanceCheckTimer += Time.fixedDeltaTime;
            if (distanceCheckTimer >= DistanceCheckInterval)
            {
                distanceCheckTimer = 0f;
                float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
                if (distToPlayer > repositionDistance)
                {
                    RepositionAheadOfPlayer();
                }
            }
        }

        private void HandleDeath()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }

            // Only Big Slimes split into 2 smaller slimes
            if (!isSmallSlime)
            {
                SpawnSplitSlime(new Vector3(-0.35f, 0f, 0f));
                SpawnSplitSlime(new Vector3(0.35f, 0f, 0f));
            }
        }

        private void SpawnSplitSlime(Vector3 offset)
        {
            Vector3 spawnPos = transform.position + offset;
            GameObject small = Instantiate(gameObject, spawnPos, Quaternion.identity);
            small.name = $"Mob_Slime_Small_{Random.Range(100, 999)}";
            small.transform.localScale = transform.localScale * 0.58f;

            SlimeAI ai = small.GetComponent<SlimeAI>();
            if (ai != null)
            {
                ai.isSmallSlime = true;
                ai.jumpSpeed = jumpSpeed * 1.12f; // Small slimes are slightly nippier
                ai.contactDamage = contactDamage * 0.7f;
            }

            CircleCollider2D col = small.GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.radius = 0.28f;
                col.isTrigger = true;
            }

            EnemyHealth eh = small.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                eh.SetMaxHealth(splitHealth);
                eh.SetInvincible(false);
            }

            small.SetActive(true);
        }

        public void ConfigureSlime(bool small, float speed, float hp, float dmg)
        {
            isSmallSlime = small;
            jumpSpeed = speed;
            splitHealth = hp;
            contactDamage = dmg;
        }
    }
}
