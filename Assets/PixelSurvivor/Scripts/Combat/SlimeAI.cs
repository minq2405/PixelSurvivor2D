using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Slime AI with Parabolic Jump Physics:
    /// - Nhảy định hướng dọc trục X về phía Player (cho khớp với animation quay ngang).
    /// - Frame 0..5 (Squash / Lấy đà): Velocity = 0, sprite ép dẹp trên mặt đất.
    /// - Frame 6..10 (Leap / Bay lên không trung): 
    ///     + Vận tốc trục X bứt phá đẩy mạnh về phía Player theo đường cong Parabol.
    ///     + Visual Sprite nhấc bổng lên theo trục Y (jumpArcHeight = 0.55m) giả lập độ cao 3D rồi rơi xuống.
    /// - Frame 11..14 (Land & Rest / Tiếp đất): Velocity = 0, sprite chạm đất dẹp người hấp thụ xung lực rồi nghỉ 1 nhịp.
    /// - Split on Death: Khi Slime lớn bị diệt, tự động tách thành 2 Slime nhỏ.
    /// </summary>
    public class SlimeAI : EnemyAI
    {
        [Header("Slime Jump & Visual Hop Settings")]
        [SerializeField] private float jumpSpeedX = 5.4f;
        [SerializeField] private float jumpSpeedY = 2.6f;
        [SerializeField] private float jumpArcHeight = 0.55f;
        [SerializeField] private Transform visualTransform;

        [Header("Slime Split Settings")]
        [SerializeField] private bool isSmallSlime = false;
        [SerializeField] private float splitHealth = 25f;

        private Vector2 currentJumpVelocity = Vector2.zero;

        public bool IsSmallSlime => isSmallSlime;

        protected override void Awake()
        {
            base.Awake();

            EnsureVisualTransform();
        }

        private void EnsureVisualTransform()
        {
            if (visualTransform == null)
            {
                Transform foundVisual = transform.Find("Visual");
                if (foundVisual != null)
                {
                    visualTransform = foundVisual;
                }
                else
                {
                    // Create visual child to isolate hop height from Collider/Rigidbody
                    GameObject vObj = new GameObject("Visual");
                    vObj.transform.SetParent(transform, false);
                    vObj.transform.localPosition = Vector3.zero;

                    SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
                    if (rootSr != null)
                    {
                        SpriteRenderer newSr = vObj.AddComponent<SpriteRenderer>();
                        newSr.sprite = rootSr.sprite;
                        newSr.sortingOrder = rootSr.sortingOrder;
                        newSr.color = rootSr.color;
                        newSr.material = rootSr.material;
                        Destroy(rootSr);
                        spriteRenderer = newSr;
                    }
                    visualTransform = vObj.transform;
                }
            }

            if (spriteRenderer == null && visualTransform != null)
            {
                spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();
            }
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

                // Frame 0..5 (Squash / Windup): Face player on X axis and ensure grounded
                if (currentFrame <= 5)
                {
                    if (visualTransform != null) visualTransform.localPosition = Vector3.zero;

                    if (playerTransform != null && spriteRenderer != null)
                    {
                        spriteRenderer.flipX = (playerTransform.position.x < transform.position.x);
                    }
                }
                // Transition into Frame 6 (Takeoff point): Lock explosive jump velocity
                else if (previousFrame <= 5 && currentFrame >= 6)
                {
                    if (playerTransform != null)
                    {
                        float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
                        float diffY = playerTransform.position.y - transform.position.y;
                        float dirY = Mathf.Clamp(diffY / 2.0f, -1.0f, 1.0f);

                        currentJumpVelocity = new Vector2(dirX * jumpSpeedX, dirY * jumpSpeedY);

                        if (spriteRenderer != null)
                        {
                            spriteRenderer.flipX = (dirX < 0);
                        }
                    }
                }
                // Frame 6..10 (Airborne Leap): Apply Parabolic Visual Elevation Arc
                if (currentFrame >= 6 && currentFrame <= 10)
                {
                    float t = (float)(currentFrame - 6) / 4.0f; // 0.0 at frame 6, 1.0 at frame 10
                    float hopY = Mathf.Sin(t * Mathf.PI) * jumpArcHeight;
                    if (visualTransform != null)
                    {
                        visualTransform.localPosition = new Vector3(0f, hopY, 0f);
                    }
                }
                // Frame 11..14 (Touchdown & Rest): Reset visual height to zero
                else if (currentFrame >= 11)
                {
                    if (visualTransform != null) visualTransform.localPosition = Vector3.zero;
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

            // Frame 6..10: Propel along X and Y with jump curve
            if (currentFrame >= 6 && currentFrame <= 10)
            {
                float t = (float)(currentFrame - 6) / 4.0f;
                // Parabolic speed curve (accelerate into leap, coast at apex, decelerate into landing)
                float speedMultiplier = Mathf.Sin(t * Mathf.PI) * 0.6f + 0.5f;
                rb.linearVelocity = currentJumpVelocity * speedMultiplier;
            }
            else
            {
                // Frame 0..5 (windup) and 11..14 (land/rest): firmly stopped
                rb.linearVelocity = Vector2.zero;
            }

            // Distance check
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
                ai.jumpSpeedX = jumpSpeedX * 1.15f;
                ai.jumpArcHeight = jumpArcHeight * 0.65f;
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

        public void ConfigureSlime(bool small, float speedX, float arcHeight, float hp, float dmg)
        {
            isSmallSlime = small;
            jumpSpeedX = speedX;
            jumpArcHeight = arcHeight;
            splitHealth = hp;
            contactDamage = dmg;
        }
    }
}
