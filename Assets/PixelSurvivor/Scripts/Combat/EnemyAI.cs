using UnityEngine;

namespace PixelSurvivor.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float separationRadius = 0.55f;
        [SerializeField] private float separationForce = 1.2f;

        [Header("Direction Flipping")]
        [Tooltip("Ngưỡng di chuyển ngang tối thiểu để đổi hướng mặt (chống giật khi đi dọc trục Y)")]
        [SerializeField] private float flipThreshold = 0.25f;

        [Header("Touch Damage (1A Mechanic)")]
        [SerializeField] private float contactDamage = 15.0f;
        [SerializeField] private float knockbackForce = 2.2f;

        [Header("Off-Screen Repositioning")]
        [SerializeField] private float repositionDistance = 22.0f;
        [SerializeField] private float minSpawnDistance = 12.0f;
        [SerializeField] private float maxSpawnDistance = 14.0f;

        [Header("Walk Animation")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private float animationFps = 10f;
        [SerializeField] private string autoResourcePath = "Enemies/Zombie/zombie_walk";

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private EnemyHealth health;

        private Transform playerTransform;
        private PlayerController playerController;
        private PlayerHealth playerHealth;
        private float distanceCheckTimer = 0f;
        private const float DistanceCheckInterval = 0.4f;
        private float animTimer = 0f;
        private int currentFrame = 0;
        private float lastFlipTime = 0f;
        private const float MinFlipInterval = 0.12f;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (health == null) health = GetComponent<EnemyHealth>();

            // Setup 2D Top-Down Rigidbody
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Start()
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

        private void Update()
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

        private void FindPlayer()
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

        private void FixedUpdate()
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
        private void RepositionAheadOfPlayer()
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, separationRadius);

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, repositionDistance);
        }
    }
}
