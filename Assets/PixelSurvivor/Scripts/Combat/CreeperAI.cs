using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Kamikaze AI for Creeper mobs:
    /// - Chases player at 3.2 m/s.
    /// - When within trigger distance (1.6m), freezes in place and starts fuse countdown (1.2s).
    /// - Plays attack swelling frames and rapid white blink.
    /// - Upon detonation: spawns fiery shockwave and smoke puff, dealing 45 AoE dmg to player
    ///   and 55 dmg to all surrounding mobs (chain reactions!).
    /// - If killed by player before fuse finishes, dies normally without exploding.
    /// </summary>
    public class CreeperAI : EnemyAI
    {
        public enum CreeperState
        {
            Chasing,
            Priming,
            Exploded
        }

        [Header("Creeper Kamikaze Settings")]
        [SerializeField] private float triggerDistance = 1.6f;
        [SerializeField] private float fuseDuration = 1.2f;
        [SerializeField] private float explosionRadius = 2.6f;
        [SerializeField] private float explosionDamage = 45f;
        [SerializeField] private float mobDamage = 55f;

        [Header("Creeper Animations & VFX")]
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] shockwaveFrames;
        [SerializeField] private Sprite[] smokeFrames;

        private CreeperState state = CreeperState.Chasing;
        private float fuseTimer = 0f;
        private Vector3 originalScale;
        private Color originalColor = Color.white;

        public CreeperState State => state;

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

            // Auto-load shockwave and smoke puff VFX if not assigned
            if (shockwaveFrames == null || shockwaveFrames.Length == 0)
            {
                shockwaveFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_shockwave_fire");
            }
            if (smokeFrames == null || smokeFrames.Length == 0)
            {
                smokeFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_smoke_puff");
            }
        }

        protected override void Update()
        {
            if (health != null && !health.IsAlive) return;

            if (state == CreeperState.Priming)
            {
                UpdatePrimingState();
                return;
            }

            base.Update();
        }

        protected override void FixedUpdate()
        {
            if (health != null && !health.IsAlive) return;

            if (state == CreeperState.Priming)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }

            base.FixedUpdate();

            // Check if player entered trigger radius
            if (state == CreeperState.Chasing && playerTransform != null)
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
            state = CreeperState.Priming;
            fuseTimer = 0f;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            Debug.Log($"<color=#EF4444>[Creeper]</color> Ssssss... Fuse primed! Exploding in {fuseDuration}s!");
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

            // 3. Rapid white blink tint
            float blinkFrequency = Mathf.Lerp(10f, 30f, progress);
            bool isWhite = (Mathf.Sin(fuseTimer * blinkFrequency) > 0f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isWhite ? Color.white : new Color(0.95f, 0.35f, 0.35f, 1f);
            }

            // 4. Detonate upon fuse completion
            if (fuseTimer >= fuseDuration)
            {
                Detonate();
            }
        }

        public void Detonate()
        {
            if (state == CreeperState.Exploded) return;
            state = CreeperState.Exploded;

            Vector3 explodePos = transform.position;
            Debug.Log($"<color=#EF4444>[BOOM!]</color> Creeper detonated at {explodePos}!");

            // 1. Spawn Shockwave Fire VFX
            if (shockwaveFrames != null && shockwaveFrames.Length > 0)
            {
                GameObject shockwaveObj = new GameObject("Creeper_Shockwave");
                shockwaveObj.transform.position = explodePos;
                shockwaveObj.transform.localScale = new Vector3(explosionRadius * 0.9f, explosionRadius * 0.9f, 1f);
                SpriteRenderer sr = shockwaveObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 18;
                AnimatedVFX vfx = shockwaveObj.AddComponent<AnimatedVFX>();
                vfx.Initialize(shockwaveFrames, 24f);
            }

            // 2. Spawn Smoke Puff VFX
            if (smokeFrames != null && smokeFrames.Length > 0)
            {
                GameObject smokeObj = new GameObject("Creeper_SmokePuff");
                smokeObj.transform.position = explodePos;
                smokeObj.transform.localScale = new Vector3(explosionRadius * 0.8f, explosionRadius * 0.8f, 1f);
                SpriteRenderer sr = smokeObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 19;
                AnimatedVFX vfx = smokeObj.AddComponent<AnimatedVFX>();
                vfx.Initialize(smokeFrames, 20f);
            }

            // 3. Damage Player in Radius
            if (playerTransform != null)
            {
                float distToPlayer = Vector2.Distance(explodePos, playerTransform.position);
                if (distToPlayer <= explosionRadius)
                {
                    var ph = playerTransform.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        Vector2 knockbackDir = ((Vector2)playerTransform.position - (Vector2)explodePos).normalized;
                        ph.TakeDamage(explosionDamage, knockbackDir, 10f);
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
                        mobHealth.TakeDamage(mobDamage, mobKnockback, 8f);
                    }
                }
            }

            // Destroy Creeper gameObject
            Destroy(gameObject);
        }

        public void ConfigureCreeper(Sprite[] walk, Sprite[] attack, Sprite[] shockwave, Sprite[] smoke)
        {
            walkFrames = walk;
            attackFrames = attack;
            shockwaveFrames = shockwave;
            smokeFrames = smoke;
        }
    }
}
