using UnityEngine;

namespace PixelSurvivor.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] protected float moveSpeed = 2.0f;
        [SerializeField] protected float separationRadius = 0.55f;
        [SerializeField] protected float separationForce = 1.2f;

        [Header("Direction Flipping")]
        [Tooltip("Ngưỡng di chuyển ngang tối thiểu để đổi hướng mặt (chống giật khi đi dọc trục Y)")]
        [SerializeField] protected float flipThreshold = 0.25f;

        [Header("Touch Damage (1A Mechanic)")]
        [SerializeField] protected float contactDamage = 15.0f;
        [SerializeField] protected float knockbackForce = 2.2f;

        [Header("Off-Screen Repositioning")]
        [SerializeField] protected float repositionDistance = 22.0f;
        [SerializeField] protected float minSpawnDistance = 12.0f;
        [SerializeField] protected float maxSpawnDistance = 14.0f;

        [Header("Walk Animation")]
        [SerializeField] protected Sprite[] walkFrames;
        [SerializeField] protected float animationFps = 10f;
        [SerializeField] protected string autoResourcePath = "Enemies/Zombie/zombie_walk";

        [Header("References")]
        [SerializeField] protected Rigidbody2D rb;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected EnemyHealth health;

        protected Transform playerTransform;
        protected PlayerController playerController;
        protected PlayerHealth playerHealth;
        protected float distanceCheckTimer = 0f;
        protected const float DistanceCheckInterval = 0.4f;
        protected float animTimer = 0f;
        protected int currentFrame = 0;
        protected float lastFlipTime = 0f;
        protected const float MinFlipInterval = 0.12f;

        protected virtual void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (health == null) health = GetComponent<EnemyHealth>();

            // Setup 2D Top-Down Rigidbody
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        protected virtual void Start()
        {
            FindPlayer();

            // Auto-load walk frames from Resources if not assigned
            if ((walkFrames == null || walkFrames.Length == 0) && !string.IsNullOrEmpty(autoResourcePath))
            {
                var loaded = Resources.LoadAll<Sprite>(autoResourcePath);
                if (loaded != null && loaded.Length > 0)
                {
                    System.Array.Sort(loaded, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
                    walkFrames = loaded;
                }
            }

            if (walkFrames != null && walkFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = walkFrames[0];
            }
        }

        protected virtual void Update()
        {
            if (health != null && !health.IsAlive) return;
            if (walkFrames == null || walkFrames.Length == 0) return;

            // Animate walk frames continuously when active
            animTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(animationFps, 1f);

            if (animTimer >= frameDuration)
            {
                int steps = (int)(animTimer / frameDuration);
                animTimer -= steps * frameDuration;
                currentFrame = (currentFrame + steps) % walkFrames.Length;

                if (spriteRenderer != null && currentFrame < walkFrames.Length)
                {
                    spriteRenderer.sprite = walkFrames[currentFrame];
                }
            }
        }

        protected virtual void FindPlayer()
        {
            if (playerTransform != null) return;

            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                playerController = pc;
                playerTransform = pc.transform;
                playerHealth = pc.GetComponent<PlayerHealth>();
            }
        }

        protected virtual void FixedUpdate()
        {
            // Do not move if dead
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

            Vector2 toPlayer = playerTransform.position - transform.position;
            float distToPlayer = toPlayer.magnitude;

            // Periodic off-screen distance check
            distanceCheckTimer += Time.fixedDeltaTime;
            if (distanceCheckTimer >= DistanceCheckInterval)
            {
                distanceCheckTimer = 0f;
                if (distToPlayer > repositionDistance)
                {
                    RepositionAheadOfPlayer();
                    return;
                }
            }

            // Direction to player
            Vector2 chaseDir = distToPlayer > 0.05f ? toPlayer / distToPlayer : Vector2.zero;

            // Soft Separation (avoid stacking)
            Vector2 separation = Vector2.zero;
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, separationRadius);
            int neighborCount = 0;

            for (int i = 0; i < nearby.Length; i++)
            {
                if (nearby[i].gameObject != gameObject && nearby[i].GetComponent<EnemyAI>() != null)
                {
                    Vector2 diff = (Vector2)transform.position - (Vector2)nearby[i].transform.position;
                    float d = diff.magnitude;
                    if (d > 0.01f)
                    {
                        separation += diff.normalized / d;
                        neighborCount++;
                    }
                }
            }

            if (neighborCount > 0)
            {
                separation /= neighborCount;
            }

            Vector2 finalMoveDir = (chaseDir + separation * separationForce).normalized;
            rb.linearVelocity = finalMoveDir * moveSpeed;

            // Flip sprite horizontally with hysteresis and deadzone (prevents rapid jitter when moving vertically)
            if (spriteRenderer != null && (Time.time - lastFlipTime >= MinFlipInterval))
            {
                if (spriteRenderer.flipX && finalMoveDir.x > flipThreshold)
                {
                    spriteRenderer.flipX = false;
                    lastFlipTime = Time.time;
                }
                else if (!spriteRenderer.flipX && finalMoveDir.x < -flipThreshold)
                {
                    spriteRenderer.flipX = true;
                    lastFlipTime = Time.time;
                }
            }
        }

        /// <summary>
        /// Instantly teleports this mob ahead of the player's movement direction
        /// when it falls too far behind off-screen.
        /// </summary>
        protected virtual void RepositionAheadOfPlayer()
        {
            if (playerTransform == null) return;

            Vector2 forwardDir = Vector2.right;
            if (playerController != null && playerController.MoveInput.sqrMagnitude > 0.01f)
            {
                forwardDir = playerController.MoveInput.normalized;
            }
            else
            {
                forwardDir = Random.insideUnitCircle.normalized;
            }

            // Pick a cone of -55 to +55 degrees ahead of player forward
            float angleOffset = Random.Range(-55f, 55f);
            Vector2 spawnDir = Quaternion.Euler(0, 0, angleOffset) * forwardDir;
            float spawnDist = Random.Range(minSpawnDistance, maxSpawnDistance);

            Vector3 newPosition = playerTransform.position + (Vector3)(spawnDir * spawnDist);
            transform.position = newPosition;
            rb.linearVelocity = Vector2.zero;

            Debug.Log($"<color=#38BDF8>[Reposition]</color> {gameObject.name} repositioned ahead of player at {newPosition}");
        }

        // ==========================
        // 1A TOUCH DAMAGE MECHANIC
        // ==========================
        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDealTouchDamage(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDealTouchDamage(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDealTouchDamage(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDealTouchDamage(other.gameObject);
        }

        private void TryDealTouchDamage(GameObject hitObject)
        {
            if (health != null && !health.IsAlive) return;

            PlayerHealth pHealth = hitObject.GetComponent<PlayerHealth>();
            if (pHealth == null && hitObject.transform.parent != null)
            {
                pHealth = hitObject.transform.parent.GetComponent<PlayerHealth>();
            }

            if (pHealth != null && pHealth.IsAlive && !pHealth.IsInvulnerable)
            {
                Vector2 knockbackDir = (pHealth.transform.position - transform.position).normalized;
                pHealth.TakeDamage(contactDamage, knockbackDir, knockbackForce);
            }
        }

        /// <summary>
        /// Dynamic wave stat initializer called by WaveSpawner.
        /// </summary>
        public void InitializeStats(float hp, float speed, float damage)
        {
            if (health != null)
            {
                health.SetMaxHealth(hp);
            }
            moveSpeed = speed;
            contactDamage = damage;
        }

        public void ApplyDifficulty(float speedMultiplier, float damageMultiplier)
        {
            moveSpeed *= speedMultiplier;
            contactDamage *= damageMultiplier;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, separationRadius);

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, repositionDistance);
        }
    }
}
