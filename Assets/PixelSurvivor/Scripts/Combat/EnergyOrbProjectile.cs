using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class EnergyOrbProjectile : MonoBehaviour
    {
        [Header("Projectile Stats")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float knockbackForce = 3f;
        [SerializeField] private int pierceCount = 2;
        [SerializeField] private float lifetime = 3.5f;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] orbFrames;
        [SerializeField] private float frameRate = 16f;

        private Vector2 moveDirection;
        private LayerMask enemyLayer;
        private GameObject owner;

        public void Initialize(Vector2 dir, float dmg, int pierce, LayerMask targetLayer, GameObject caster, float moveSpeed = 9f, Sprite[] animFrames = null)
        {
            moveDirection = dir.normalized;
            damage = dmg;
            pierceCount = pierce;
            enemyLayer = targetLayer;
            owner = caster;
            speed = moveSpeed;

            if (animFrames != null && animFrames.Length > 0)
            {
                orbFrames = animFrames;
            }

            // Setup AnimatedVFX
            if (orbFrames != null && orbFrames.Length > 0)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 10;

                AnimatedVFX anim = gameObject.AddComponent<AnimatedVFX>();
                anim.Initialize(orbFrames, frameRate, null);
            }

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

            // Check collision with enemies
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.4f, enemyLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == owner) continue;

                IDamageable victim = hits[i].GetComponent<IDamageable>();
                if (victim != null && victim.IsAlive)
                {
                    victim.TakeDamage(damage, moveDirection, knockbackForce);
                    pierceCount--;

                    if (pierceCount <= 0)
                    {
                        Destroy(gameObject);
                        break;
                    }
                }
            }
        }
    }
}
