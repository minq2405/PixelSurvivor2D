using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Phantom AI (Airborne Hunter):
    /// - 35 HP, 4.0 m/s prowl speed.
    /// - Flying mob: ignores ground collisions.
    /// - Hovering / Wandering Phase: Hovers and prowls around the player (3.5m - 5.5m)
    ///   with fluid wandering curves and wing flaps (phantom_fly, 11 frames).
    /// - Dive Attack Phase: Every ~3.8s, locks onto player and executes a high-speed
    ///   dive strike (6.5 m/s) with phantom_attack (16 frames) dealing 14 dmg + knockback.
    /// - After the dive swoop, pulls up and returns to prowl altitude.
    /// </summary>
    public class PhantomAI : EnemyAI
    {
        public enum PhantomState
        {
            Prowling,
            Diving,
            PullingUp
        }

        [Header("Phantom Combat Settings")]
        [SerializeField] private float prowlDistanceMin = 3.5f;
        [SerializeField] private float prowlDistanceMax = 5.5f;
        [SerializeField] private float diveSpeed = 6.5f;
        [SerializeField] private float diveCooldown = 3.8f;
        [SerializeField] private float diveDuration = 1.0f;
        [SerializeField] private float pullUpDuration = 0.6f;
        [SerializeField] private float diveDamage = 14f;

        [Header("Phantom Animations")]
        [SerializeField] private Sprite[] flyFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float attackFps = 16f;

        private PhantomState state = PhantomState.Prowling;
        private float diveCooldownTimer = 2.0f; // Initial timer before first dive
        private float stateTimer = 0f;
        private Vector2 diveDirection;
        private float wanderAngle = 0f;
        private float hoverWobble = 0f;
        private int attackFrameIdx = 0;
        private float attackAnimTimer = 0f;
        private bool hasDealtDiveDamage = false;

        public PhantomState State => state;

        protected override void Start()
        {
            base.Start();

            if (flyFrames == null || flyFrames.Length == 0)
            {
                flyFrames = Resources.LoadAll<Sprite>("Enemies/Phantom/phantom_fly");
            }
            if (attackFrames == null || attackFrames.Length == 0)
            {
                attackFrames = Resources.LoadAll<Sprite>("Enemies/Phantom/phantom_attack");
            }

            if (flyFrames != null && flyFrames.Length > 0)
            {
                walkFrames = flyFrames;
            }

            // Random initial wander angle
            wanderAngle = Random.Range(0f, Mathf.PI * 2f);
        }

        protected override void Update()
        {
            if (health != null && !health.IsAlive) return;

            diveCooldownTimer -= Time.deltaTime;

            if (state == PhantomState.Diving)
            {
                UpdateDiveAnimation();
                return;
            }

            // Normal fly frames
            base.Update();

            // Prowling state check
            if (state == PhantomState.Prowling && diveCooldownTimer <= 0f && playerTransform != null)
            {
                StartDive();
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

            switch (state)
            {
                case PhantomState.Prowling:
                    FixedUpdateProwling();
                    break;
                case PhantomState.Diving:
                    FixedUpdateDiving();
                    break;
                case PhantomState.PullingUp:
                    FixedUpdatePullingUp();
                    break;
            }
        }

        private void FixedUpdateProwling()
        {
            Vector2 toPlayer = (playerTransform.position - transform.position);
            float dist = toPlayer.magnitude;

            // Reposition if too far
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

            // Prowl / Wander around player:
            // Drift around player with slow angular change and distance stabilization
            wanderAngle += Time.fixedDeltaTime * 0.8f;
            hoverWobble += Time.fixedDeltaTime * 4f;

            float targetRadius = (prowlDistanceMin + prowlDistanceMax) * 0.5f;
            Vector2 targetOffset = new Vector2(Mathf.Cos(wanderAngle), Mathf.Sin(wanderAngle)) * targetRadius;
            Vector2 targetPos = (Vector2)playerTransform.position + targetOffset;

            // Add subtle floating sway
            targetPos.y += Mathf.Sin(hoverWobble) * 0.35f;

            Vector2 moveDir = (targetPos - (Vector2)transform.position).normalized;
            rb.linearVelocity = moveDir * moveSpeed;

            // Direction flip
            FlipTowards(moveDir.x);
        }

        private void StartDive()
        {
            state = PhantomState.Diving;
            stateTimer = 0f;
            attackFrameIdx = 0;
            attackAnimTimer = 0f;
            hasDealtDiveDamage = false;

            if (playerTransform != null)
            {
                diveDirection = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            }
            else
            {
                diveDirection = Vector2.down;
            }
        }

        private void FixedUpdateDiving()
        {
            stateTimer += Time.fixedDeltaTime;
            rb.linearVelocity = diveDirection * diveSpeed;

            // Check contact damage during dive
            if (!hasDealtDiveDamage && playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= 0.95f)
                {
                    hasDealtDiveDamage = true;
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(diveDamage, diveDirection, 4.0f);
                    }
                }
            }

            FlipTowards(diveDirection.x);

            if (stateTimer >= diveDuration)
            {
                StartPullUp();
            }
        }

        private void StartPullUp()
        {
            state = PhantomState.PullingUp;
            stateTimer = 0f;
        }

        private void FixedUpdatePullingUp()
        {
            stateTimer += Time.fixedDeltaTime;

            // Swoop upwards and away
            Vector2 pullUpDir = (Vector2.up + Vector2.right * (spriteRenderer.flipX ? -0.5f : 0.5f)).normalized;
            rb.linearVelocity = pullUpDir * (moveSpeed * 1.2f);

            if (stateTimer >= pullUpDuration)
            {
                // Resume prowling
                state = PhantomState.Prowling;
                diveCooldownTimer = diveCooldown;
                wanderAngle = Random.Range(0f, Mathf.PI * 2f);
                if (flyFrames != null && flyFrames.Length > 0 && spriteRenderer != null)
                {
                    spriteRenderer.sprite = flyFrames[0];
                }
            }
        }

        private void UpdateDiveAnimation()
        {
            if (attackFrames == null || attackFrames.Length == 0) return;

            attackAnimTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(attackFps, 1f);

            if (attackAnimTimer >= frameDuration)
            {
                attackAnimTimer -= frameDuration;
                attackFrameIdx = (attackFrameIdx + 1) % attackFrames.Length;

                if (spriteRenderer != null && attackFrameIdx < attackFrames.Length)
                {
                    spriteRenderer.sprite = attackFrames[attackFrameIdx];
                }
            }
        }

        private void FlipTowards(float xDir)
        {
            if (spriteRenderer == null) return;
            if (Time.time - lastFlipTime < MinFlipInterval) return;

            if (spriteRenderer.flipX && xDir > flipThreshold)
            {
                spriteRenderer.flipX = false;
                lastFlipTime = Time.time;
            }
            else if (!spriteRenderer.flipX && xDir < -flipThreshold)
            {
                spriteRenderer.flipX = true;
                lastFlipTime = Time.time;
            }
        }

        public void ConfigurePhantom(Sprite[] fly, Sprite[] attack)
        {
            flyFrames = fly;
            walkFrames = fly;
            attackFrames = attack;
        }
    }
}
