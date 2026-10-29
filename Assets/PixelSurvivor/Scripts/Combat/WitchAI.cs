using System.Collections;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Witch AI (Tier Cao - Tầm xa):
    /// - Không ném bình độc, thay vào đó triệu hồi Gai Thạch Anh Tím (Amethyst Spikes - vfx_evoker_skill tinted purple).
    /// - Giữ khoảng cách thả diều tầm trung (4.5m - 6.5m). Nếu Player quá gần sẽ lùi lại.
    /// - Có animation niệm chú (witch_attack).
    /// </summary>
    public class WitchAI : EnemyAI
    {
        [Header("Witch Combat Settings")]
        [SerializeField] private float preferredMinDistance = 4.2f;
        [SerializeField] private float preferredMaxDistance = 6.5f;
        [SerializeField] private float attackCooldown = 3.5f;
        [SerializeField] private float spikeDamage = 20f;
        [SerializeField] private float spikeRadius = 1.15f;

        [Header("Witch Animations & VFX")]
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] spikeVfxFrames;
        [SerializeField] private Color amethystColor = new Color(0.85f, 0.45f, 1.0f, 1f); // Thạch anh tím sáng

        private float cooldownTimer = 1.5f;
        private bool isAttacking = false;
        private float attackTimer = 0f;
        private int attackFrameIdx = 0;
        private bool hasSpawnedSpikeThisAttack = false;

        public bool IsAttacking => isAttacking;

        protected override void Start()
        {
            base.Start();

            // Auto load Amethyst spike frames if not assigned
            if (spikeVfxFrames == null || spikeVfxFrames.Length == 0)
            {
                spikeVfxFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_evoker_skill");
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
                // Within sweet spot: slight strafe / stand ground
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
            hasSpawnedSpikeThisAttack = false;
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
            float frameDuration = 1f / 14f; // ~14 fps attack casting speed

            int currentStep = Mathf.FloorToInt(attackTimer / frameDuration);
            if (currentStep != attackFrameIdx && currentStep < attackFrames.Length)
            {
                attackFrameIdx = currentStep;
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = attackFrames[attackFrameIdx];
                }

                // Mid-cast point (frame 7): spawn Amethyst Spikes!
                if (attackFrameIdx == 7 && !hasSpawnedSpikeThisAttack)
                {
                    hasSpawnedSpikeThisAttack = true;
                    CastAmethystSpike();
                }
            }

            if (attackTimer >= attackFrames.Length * frameDuration)
            {
                FinishAttack();
            }
        }

        private void CastAmethystSpike()
        {
            if (playerTransform == null) return;

            Vector3 targetPos = playerTransform.position;
            StartCoroutine(EruptSpikeRoutine(targetPos));
        }

        private IEnumerator EruptSpikeRoutine(Vector3 targetPos)
        {
            // 0.2s telegraph delay so attentive players can dash/dodge
            yield return new WaitForSeconds(0.2f);

            // Spawn Amethyst Spike VFX
            if (spikeVfxFrames != null && spikeVfxFrames.Length > 0)
            {
                GameObject spikeObj = new GameObject("Amethyst_Spike");
                spikeObj.transform.position = targetPos;
                spikeObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

                SpriteRenderer sr = spikeObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 17;
                sr.color = amethystColor;

                Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit") ?? Shader.Find("Sprites/Default");
                if (unlit != null)
                {
                    sr.material = new Material(unlit);
                }

                AnimatedVFX anim = spikeObj.AddComponent<AnimatedVFX>();
                anim.Initialize(spikeVfxFrames, 20f);
            }

            // Damage Check
            if (playerTransform != null)
            {
                float dist = Vector2.Distance(targetPos, playerTransform.position);
                if (dist <= spikeRadius)
                {
                    PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
                    if (ph != null && ph.IsAlive)
                    {
                        Vector2 knockDir = ((Vector2)playerTransform.position - (Vector2)targetPos).normalized;
                        ph.TakeDamage(spikeDamage, knockDir, 6.0f);
                        Debug.Log($"<color=#C084FC>[Witch]</color> Amethyst Spike impaled player for {spikeDamage:F0} DMG!");
                    }
                }
            }
        }

        private void FinishAttack()
        {
            isAttacking = false;
            cooldownTimer = attackCooldown;
        }

        public void ConfigureWitch(Sprite[] walk, Sprite[] attack, Sprite[] spikes)
        {
            walkFrames = walk;
            attackFrames = attack;
            spikeVfxFrames = spikes;
        }
    }
}
