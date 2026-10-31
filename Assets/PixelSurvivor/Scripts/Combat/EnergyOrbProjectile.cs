using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class EnergyOrbProjectile : MonoBehaviour
    {
        [Header("Projectile Stats")]
        [SerializeField] private float speed = 10.5f;
        [SerializeField] private float baseDamage = 21f;
        [SerializeField] private float knockbackForce = 3f;
        [SerializeField] private int pierceCount = 2;
        [SerializeField] private float lifetime = 3.5f;
        [SerializeField] private float visualScale = 0.5f;
        [SerializeField] private float hitRadius = 0.25f;

        [Header("Falloff & Evolution Mechanics")]
        [Tooltip("Damage reduction applied per pierce (v2.1 Crossbow: 0.25 = -25%/pierce, Gatling: 0.15 = -15%/pierce)")]
        [SerializeField] private float damageReductionPerPierce = 0.25f;
        [SerializeField] private bool isExplosive = false;
        [SerializeField] private float explosiveRadius = 1.0f;
        [SerializeField] private float explosiveDamagePct = 0.40f;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] orbFrames;
        [SerializeField] private float frameRate = 16f;

        private Vector2 moveDirection;
        private LayerMask enemyLayer;
        private GameObject owner;
        private int piercesDone = 0;
        private string weaponId = "weapon_crossbow";
        private bool isCrit = false;
        private readonly HashSet<int> hitVictims = new HashSet<int>();

        public void Initialize(
            Vector2 dir, 
            float dmg, 
            int pierce, 
            LayerMask targetLayer, 
            GameObject caster, 
            float moveSpeed = 10.5f, 
            Sprite[] animFrames = null, 
            float falloffPerPierce = 0.25f, 
            bool explosive = false, 
            float aoeRadius = 1.0f, 
            float aoeDmgPct = 0.40f,
            string sourceWeaponId = "weapon_crossbow",
            bool crit = false)
        {
            moveDirection = dir.normalized;
            baseDamage = dmg;
            pierceCount = pierce;
            enemyLayer = targetLayer;
            owner = caster;
            speed = moveSpeed;
            damageReductionPerPierce = falloffPerPierce;
            isExplosive = explosive;
            explosiveRadius = aoeRadius;
            explosiveDamagePct = aoeDmgPct;
            weaponId = sourceWeaponId;
            isCrit = crit;

            transform.localScale = new Vector3(visualScale, visualScale, 1f);

            if (animFrames != null && animFrames.Length > 0)
            {
                orbFrames = animFrames;
            }

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

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius, enemyLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == owner) continue;

                int victimId = hits[i].gameObject.GetInstanceID();
                if (hitVictims.Contains(victimId)) continue; // Never hit the same enemy multiple times per projectile

                EnemyHealth eh = hits[i].GetComponent<EnemyHealth>();
                if (eh == null && hits[i].transform.parent != null)
                {
                    eh = hits[i].transform.parent.GetComponent<EnemyHealth>();
                }

                if (eh != null && eh.IsAlive)
                {
                    hitVictims.Add(victimId);

                    // v2.1 Mathematical Pierce Falloff: -25% base dmg per pierce (e.g. 100% -> 75% -> 50%)
                    float falloff = Mathf.Max(0.20f, 1.0f - (damageReductionPerPierce * piercesDone));
                    float finalDmg = baseDamage * falloff;

                    eh.TakeDamage(finalDmg, moveDirection, knockbackForce, isCrit, weaponId);

                    // Gatling Repeater Evolution: 1.0m AoE explosion (40% damage)
                    if (isExplosive)
                    {
                        TriggerAreaExplosion(transform.position, finalDmg * explosiveDamagePct);
                    }

                    piercesDone++;
                    pierceCount--;

                    if (pierceCount <= 0)
                    {
                        Destroy(gameObject);
                        break;
                    }
                    continue;
                }

                IDamageable victim = hits[i].GetComponent<IDamageable>();
                if (victim != null && victim.IsAlive)
                {
                    hitVictims.Add(victimId);
                    float falloff = Mathf.Max(0.20f, 1.0f - (damageReductionPerPierce * piercesDone));
                    victim.TakeDamage(baseDamage * falloff, moveDirection, knockbackForce);

                    piercesDone++;
                    pierceCount--;
                    if (pierceCount <= 0)
                    {
                        Destroy(gameObject);
                        break;
                    }
                }
            }
        }

        private void TriggerAreaExplosion(Vector2 pos, float aoeDmg)
        {
            Collider2D[] aoeHits = Physics2D.OverlapCircleAll(pos, explosiveRadius, enemyLayer);
            for (int i = 0; i < aoeHits.Length; i++)
            {
                if (aoeHits[i].gameObject == owner) continue;
                EnemyHealth eh = aoeHits[i].GetComponent<EnemyHealth>();
                if (eh != null && eh.IsAlive)
                {
                    eh.TakeDamage(aoeDmg, Vector2.zero, 1.0f, false, weaponId);
                }
            }
        }
    }
}
