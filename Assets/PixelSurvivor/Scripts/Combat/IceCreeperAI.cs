using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Ice Creeper AI:
    /// - 65 HP, 3.0 m/s chase speed.
    /// - Triggers fuse (1.2s) when within 1.6m of player.
    /// - Plays attack swelling frames and rapid cyan/white blinking.
    /// - Upon detonation: spawns icy shockwave (vfx_shockwave_ice) dealing 35 AoE dmg
    ///   and applying 45% Slow to player for 1.8s (confirmed 1.5 - 2s window).
    ///   Also deals 55 dmg to surrounding mobs (friendly fire).
    /// - Dies normally without exploding if killed before fuse completes.
    /// </summary>
    public class IceCreeperAI : EnemyAI
    {
        public enum IceCreeperState
        {
            Chasing,
            Priming,
            Exploded
        }

        [Header("Ice Creeper Settings")]
        [SerializeField] private float triggerDistance = 1.6f;
        [SerializeField] private float fuseDuration = 1.2f;
        [SerializeField] private float explosionRadius = 2.6f;
        [SerializeField] private float explosionDamage = 35f;
        [SerializeField] private float mobDamage = 55f;
        [SerializeField] private float slowMultiplier = 0.55f;
        [SerializeField] private float slowDuration = 1.8f;

        [Header("Ice Creeper Animations & VFX")]
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] shockwaveFrames;
        [SerializeField] private Sprite[] smokeFrames;

        private IceCreeperState state = IceCreeperState.Chasing;
        private float fuseTimer = 0f;
        private Vector3 originalScale;
        private Color originalColor = Color.white;

        public IceCreeperState State => state;

        protected override void Awake()
        {
            base.Awake();
            originalScale = transform.localScale;
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        protected override void Start()
        {
            base.Start();

            if (shockwaveFrames == null || shockwaveFrames.Length == 0)
            {
                shockwaveFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_shockwave_ice");
            }
            if (smokeFrames == null || smokeFrames.Length == 0)
            {
                smokeFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_smoke_puff");
            }
        }

        protected override void Update()
        {
            if (health != null && !health.IsAlive) return;

            if (state == IceCreeperState.Priming)
            {
                UpdatePrimingState();
                return;
            }

            base.Update();
        }

        protected override void FixedUpdate()
        {
            if (health != null && !health.IsAlive) return;

            if (state == IceCreeperState.Priming)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }

            base.FixedUpdate();

            // Check if player entered trigger radius
            if (state == IceCreeperState.Chasing && playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= triggerDistance)
                {
                    StartPriming();
                }
            }
        }

        private void StartPriming()
        {
            state = IceCreeperState.Priming;
            fuseTimer = 0f;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            Debug.Log($"<color=#38BDF8>[Ice Creeper]</color> Frost charging! Exploding in {fuseDuration}s!");
        }

        private void UpdatePrimingState()
        {
            fuseTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(fuseTimer / fuseDuration);

            // 1. Swell transform scale (1.0 -> 1.35x)
            float scaleMultiplier = Mathf.Lerp(1.0f, 1.35f, progress);
            transform.localScale = originalScale * scaleMultiplier;

            // 2. Play attack swelling frames
            if (attackFrames != null && attackFrames.Length > 0 && spriteRenderer != null)
            {
                int frameIdx = Mathf.Clamp(Mathf.FloorToInt(progress * attackFrames.Length), 0, attackFrames.Length - 1);
                spriteRenderer.sprite = attackFrames[frameIdx];
            }

            // 3. Rapid cyan/white blink tint
            float blinkFrequency = Mathf.Lerp(10f, 30f, progress);
            bool isWhite = (Mathf.Sin(fuseTimer * blinkFrequency) > 0f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isWhite ? Color.white : new Color(0.35f, 0.85f, 1f, 1f);
            }

            // 4. Detonate upon fuse completion
            if (fuseTimer >= fuseDuration)
            {
                Detonate();
            }
        }

        public void Detonate()
        {
            if (state == IceCreeperState.Exploded) return;
            state = IceCreeperState.Exploded;

            Vector3 explodePos = transform.position;
            Debug.Log($"<color=#38BDF8>[FREEZE BOOM!]</color> Ice Creeper detonated at {explodePos}!");

            // 1. Spawn Shockwave Ice VFX
            if (shockwaveFrames != null && shockwaveFrames.Length > 0)
            {
                GameObject shockwaveObj = new GameObject("IceCreeper_Shockwave");
                shockwaveObj.transform.position = explodePos;
                shockwaveObj.transform.localScale = new Vector3(explosionRadius * 0.95f, explosionRadius * 0.95f, 1f);
                SpriteRenderer sr = shockwaveObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 18;
                AnimatedVFX vfx = shockwaveObj.AddComponent<AnimatedVFX>();
                vfx.Initialize(shockwaveFrames, 20f);
            }

            // 2. Spawn Smoke Puff VFX
            if (smokeFrames != null && smokeFrames.Length > 0)
            {
                GameObject smokeObj = new GameObject("IceCreeper_SmokePuff");
                smokeObj.transform.position = explodePos;
                smokeObj.transform.localScale = new Vector3(explosionRadius * 0.8f, explosionRadius * 0.8f, 1f);
                SpriteRenderer sr = smokeObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 19;
                AnimatedVFX vfx = smokeObj.AddComponent<AnimatedVFX>();
                vfx.Initialize(smokeFrames, 20f);
            }

            // 3. Damage Player in Radius and Apply Slow
            if (playerTransform != null)
            {
                float distToPlayer = Vector2.Distance(explodePos, playerTransform.position);
                if (distToPlayer <= explosionRadius)
                {
                    var ph = playerTransform.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        Vector2 knockbackDir = ((Vector2)playerTransform.position - (Vector2)explodePos).normalized;
                        ph.TakeDamage(explosionDamage, knockbackDir, 8f);
                    }

                    if (playerController != null)
                    {
                        playerController.ApplySlow(slowMultiplier, slowDuration);
                    }
                }
            }

            // 4. Damage Surrounding Mobs in Radius (Friendly fire / chain reactions!)
            Collider2D[] colliders = Physics2D.OverlapCircleAll(explodePos, explosionRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != gameObject)
                {
                    var mobHealth = colliders[i].GetComponent<EnemyHealth>();
                    if (mobHealth != null && mobHealth.IsAlive)
                    {
                        Vector2 mobKnockback = ((Vector2)colliders[i].transform.position - (Vector2)explodePos).normalized;
                        mobHealth.TakeDamage(mobDamage, mobKnockback, 7f);
                    }
                }
            }

            // Destroy Ice Creeper gameObject
            Destroy(gameObject);
        }

        public void ConfigureIceCreeper(Sprite[] walk, Sprite[] attack, Sprite[] shockwave, Sprite[] smoke)
        {
            walkFrames = walk;
            attackFrames = attack;
            shockwaveFrames = shockwave;
            smokeFrames = smoke;
        }
    }
}
