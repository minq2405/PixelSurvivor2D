using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class RangedProjectileWeapon : WeaponBase
    {
        [Header("Projectile Settings")]
        [SerializeField] private Sprite[] projectileFrames;
        [SerializeField] private float projectileSpeed = 9f;
        [SerializeField] private int pierceCount = 2;
        [SerializeField] private int projectileCount = 1;

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
            Vector2 baseDir = target != null 
                ? (Vector2)(target.position - player.transform.position).normalized 
                : (player != null ? player.FacingDirection : Vector2.right);

            for (int i = 0; i < projectileCount; i++)
            {
                Vector2 fireDir = baseDir;
                if (projectileCount > 1)
                {
                    float angle = (i - (projectileCount - 1) / 2f) * 12f;
                    fireDir = Quaternion.Euler(0, 0, angle) * baseDir;
                }

                SpawnProjectile(fireDir);
            }
        }

        private void SpawnProjectile(Vector2 dir)
        {
            GameObject projObj = new GameObject($"{weaponName}_Orb");
            projObj.transform.position = player.transform.position + (Vector3)(dir * 0.6f);
            
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            projObj.transform.rotation = Quaternion.Euler(0, 0, angle);

            EnergyOrbProjectile proj = projObj.AddComponent<EnergyOrbProjectile>();
            proj.Initialize(dir, GetTotalDamage(), pierceCount, enemyLayer, player.gameObject, projectileSpeed, projectileFrames);
        }

        public void ConfigureProjectile(Sprite[] frames, int pierce = 2, int count = 1)
        {
            projectileFrames = frames;
            pierceCount = pierce;
            projectileCount = count;
        }
    }
}
