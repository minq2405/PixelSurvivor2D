using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class LaserBeamWeapon : WeaponBase
    {
        [Header("Laser Beam Visuals")]
        [SerializeField] private Sprite[] laserFrames;
        [SerializeField] private float beamLength = 8f;
        [SerializeField] private float beamThickness = 0.8f;
        [SerializeField] private float vfxFps = 20f;

        private void Start()
        {
            if (enemyLayer == 0)
            {
                enemyLayer = LayerMask.GetMask("Default");
            }
        }

        protected override void ExecuteAttack()
        {
            Transform target = FindClosestEnemy(attackRange);
            Vector2 beamDir = target != null 
                ? (Vector2)(target.position - player.transform.position).normalized 
                : (player != null ? player.FacingDirection : Vector2.right);

            Vector3 startPos = player.transform.position;
            Vector3 centerPos = startPos + (Vector3)(beamDir * (beamLength / 2f));
            float angle = Mathf.Atan2(beamDir.y, beamDir.x) * Mathf.Rad2Deg;

            // 1. Spawn Laser VFX
            if (laserFrames != null && laserFrames.Length > 0)
            {
                GameObject laserObj = new GameObject($"{weaponName}_Beam");
                laserObj.transform.position = centerPos;
                laserObj.transform.rotation = Quaternion.Euler(0, 0, angle);
                laserObj.transform.localScale = new Vector3(beamLength / 2.5f, beamThickness, 1f);

                SpriteRenderer sr = laserObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 11;

                AnimatedVFX anim = laserObj.AddComponent<AnimatedVFX>();
                anim.Initialize(laserFrames, vfxFps);
            }

            // 2. Perform Line Cast for instant damage
            RaycastHit2D[] hits = Physics2D.CircleCastAll(startPos, beamThickness * 0.5f, beamDir, beamLength, enemyLayer);
            float dmg = GetTotalDamage();

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.gameObject == player.gameObject) continue;

                IDamageable victim = hits[i].collider.GetComponent<IDamageable>();
                if (victim != null && victim.IsAlive)
                {
                    victim.TakeDamage(dmg, beamDir, knockbackForce);
                }
            }
        }

        public void ConfigureLaser(Sprite[] frames, float length = 8f, float thickness = 0.8f)
        {
            laserFrames = frames;
            beamLength = length;
            beamThickness = thickness;
        }
    }
}
