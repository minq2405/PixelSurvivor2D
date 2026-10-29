using System.Collections;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Witch AI (Tier Cao - Tầm xa):
    /// - Không ném bình độc, thay vào đó triệu hồi Gai Thạch Anh Tím (Amethyst Spikes - vfx_evoker_skill tinted purple).
    /// - Giữ khoảng cách thả diều tầm trung (4.2m - 6.5m). Nếu Player quá gần sẽ lùi lại.
    /// - Kéo dài hoạt ảnh niệm phép (animation windup ~1.0s) và hiện Vòng tròn cảnh báo tím dưới chân (0.85s)
    ///   để người chơi có thời gian phản ứng bước né ra ngoài.
    /// - Hạ trục Y của gai xuống (y -= 0.55m) để cắm đúng mặt đất, tạo cảm giác gai trồi thẳng từ lòng đất lên.
    /// </summary>
    public class WitchAI : EnemyAI
    {
        [Header("Witch Combat Settings")]
        [SerializeField] private float preferredMinDistance = 4.2f;
        [SerializeField] private float preferredMaxDistance = 6.5f;
        [SerializeField] private float attackCooldown = 3.8f;
        [SerializeField] private float spikeDamage = 20f;
        [SerializeField] private float spikeRadius = 1.15f;
        [SerializeField] private float castFps = 8f; // Kéo dài animation niệm chú (~1.75s) để dễ né
        [SerializeField] private float telegraphDelay = 0.85f; // Thời gian báo hiệu dưới đất trước khi gai trồi lên

        [Header("Witch Animations & VFX")]
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] spikeVfxFrames;
        [SerializeField] private Sprite[] magicCircleFrames;
        [SerializeField] private Color amethystColor = new Color(0.85f, 0.45f, 1.0f, 1f); // Thạch anh tím sáng

        private float cooldownTimer = 1.5f;
        private bool isAttacking = false;
        private float attackTimer = 0f;
        private int attackFrameIdx = 0;
        private bool hasInitiatedSpikeRoutine = false;

        public bool IsAttacking => isAttacking;

        protected override void Start()
        {
            base.Start();

            if (spikeVfxFrames == null || spikeVfxFrames.Length == 0)
            {
                spikeVfxFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_evoker_skill");
            }
            if (magicCircleFrames == null || magicCircleFrames.Length == 0)
            {
                magicCircleFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_evoker_magic_circle");
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

            // Normal walk animation
            base.Update();

            // Check if ready to cast Amethyst Spike
            if (cooldownTimer <= 0f && playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= preferredMaxDistance + 0.8f)
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

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            Vector2 toPlayer = (playerTransform.position - transform.position);
            float dist = toPlayer.magnitude;
            Vector2 dir = toPlayer.normalized;

            // Kiting / Repositioning logic:
            if (dist < preferredMinDistance)
            {
                // Player is too close: back away / kite
                rb.linearVelocity = -dir * (moveSpeed * 0.85f);
            }
            else if (dist > preferredMaxDistance)
            {
                // Player is too far: advance forward
                rb.linearVelocity = dir * moveSpeed;
            }
            else
            {
                // Sweet spot: stand ground
                rb.linearVelocity = Vector2.zero;
            }

            // Direction flip
            if (dir.x < -0.1f && spriteRenderer != null) spriteRenderer.flipX = true;
            else if (dir.x > 0.1f && spriteRenderer != null) spriteRenderer.flipX = false;
        }

        private void StartAttack()
        {
            isAttacking = true;
            attackTimer = 0f;
            attackFrameIdx = 0;
            hasInitiatedSpikeRoutine = false;
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
            float frameDuration = 1f / Mathf.Max(1f, castFps);

            int currentStep = Mathf.FloorToInt(attackTimer / frameDuration);
            if (currentStep != attackFrameIdx && currentStep < attackFrames.Length)
            {
                attackFrameIdx = currentStep;
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = attackFrames[attackFrameIdx];
                }

                // Trigger telegraph at frame 3 (early in windup) so player has 0.85s to react!
                if (attackFrameIdx == 3 && !hasInitiatedSpikeRoutine)
                {
                    hasInitiatedSpikeRoutine = true;
                    if (playerTransform != null)
                    {
                        // Lower Y by 0.55m so it plants firmly on the ground floor!
                        Vector3 groundTargetPos = playerTransform.position;
                        groundTargetPos.y -= 0.55f;
                        StartCoroutine(AmethystEruptionSequence(groundTargetPos));
                    }
                }
            }

            if (attackTimer >= attackFrames.Length * frameDuration)
            {
                FinishAttack();
            }
        }

        private IEnumerator AmethystEruptionSequence(Vector3 groundPos)
        {
            // 1. Spawn Purple Magic Circle Telegraph on the ground
            GameObject telegraphObj = new GameObject("Amethyst_Telegraph");
            telegraphObj.transform.position = groundPos;
            telegraphObj.transform.localScale = new Vector3(1.4f, 0.7f, 1f); // 2D top-down flat perspective

            SpriteRenderer srCircle = telegraphObj.AddComponent<SpriteRenderer>();
            srCircle.sortingOrder = 4; // Below mobs, directly on the floor
            srCircle.color = new Color(0.75f, 0.35f, 1f, 0.8f);

            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit") ?? Shader.Find("Sprites/Default");
            if (unlit != null)
            {
                srCircle.material = new Material(unlit);
            }

            if (magicCircleFrames != null && magicCircleFrames.Length > 0)
            {
                AnimatedVFX vfxCircle = telegraphObj.AddComponent<AnimatedVFX>();
                vfxCircle.Initialize(magicCircleFrames, 24f);
            }

            // 2. Wait for telegraph duration (0.85s window for player to dodge)
            float elapsed = 0f;
            while (elapsed < telegraphDelay)
            {
                elapsed += Time.deltaTime;
                // Subtle pulse effect on telegraph
                if (srCircle != null)
                {
                    float alpha = 0.5f + Mathf.PingPong(elapsed * 4f, 0.4f);
                    srCircle.color = new Color(0.75f, 0.35f, 1f, alpha);
                }
                yield return null;
            }

            // Remove telegraph
            if (telegraphObj != null)
            {
                Destroy(telegraphObj);
            }

            // 3. Erupt Amethyst Spikes out of the ground!
            if (spikeVfxFrames != null && spikeVfxFrames.Length > 0)
            {
                GameObject spikeObj = new GameObject("Amethyst_Spike");
                spikeObj.transform.position = groundPos;
                spikeObj.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

                SpriteRenderer srSpike = spikeObj.AddComponent<SpriteRenderer>();
                srSpike.sortingOrder = 17;
                srSpike.color = amethystColor;

                if (unlit != null)
                {
                    srSpike.material = new Material(unlit);
                }

                AnimatedVFX anim = spikeObj.AddComponent<AnimatedVFX>();
                anim.Initialize(spikeVfxFrames, 22f);
            }

            // 4. Hitbox Damage Check (fair: only hits if player failed to step out!)
            if (playerTransform != null)
            {
                // Measure distance from groundPos to player ground point
                Vector3 playerGroundPos = playerTransform.position;
                playerGroundPos.y -= 0.55f;

                float dist = Vector2.Distance(groundPos, playerGroundPos);
                if (dist <= spikeRadius)
                {
                    PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
                    if (ph != null && ph.IsAlive)
                    {
                        Vector2 knockDir = ((Vector2)playerTransform.position - (Vector2)groundPos).normalized;
                        ph.TakeDamage(spikeDamage, knockDir, 6.0f);
                        Debug.Log($"<color=#C084FC>[Witch]</color> Amethyst Spike hit player for {spikeDamage:F0} DMG!");
                    }
                }
            }
        }

        private void FinishAttack()
        {
            isAttacking = false;
            cooldownTimer = attackCooldown;
        }

        public void ConfigureWitch(Sprite[] walk, Sprite[] attack, Sprite[] spikes, Sprite[] circles)
        {
            walkFrames = walk;
            attackFrames = attack;
            spikeVfxFrames = spikes;
            magicCircleFrames = circles;
        }
    }
}
