using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class RangedProjectileWeapon : WeaponBase
    {
        [Header("Projectile Settings")]
        [SerializeField] private Sprite[] projectileFrames;
        [SerializeField] private float projectileSpeed = 10.5f;
        [SerializeField] private int pierceCount = 2;
        [SerializeField] private int projectileCount = 1;

        protected override void Awake()
        {
            base.Awake();
            if (enemyLayer.value == 0)
            {
                enemyLayer = ~LayerMask.GetMask("UI", "Ignore Raycast");
            }
        }

        private void Start()
        {
            if (enemyLayer.value == 0)
            {
                enemyLayer = ~LayerMask.GetMask("UI", "Ignore Raycast");
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
                    float angle = (i - (projectileCount - 1) / 2f) * 14f;
                    fireDir = Quaternion.Euler(0, 0, angle) * baseDir;
                }

                SpawnProjectile(fireDir);
            }
        }

        private void SpawnProjectile(Vector2 dir)
        {
            if (player == null) return;

            GameObject projObj = new GameObject($"{weaponName}_Bolt");
            projObj.transform.position = player.transform.position + (Vector3)(dir * 0.6f);
            
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            projObj.transform.rotation = Quaternion.Euler(0, 0, angle);
            projObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            EnergyOrbProjectile proj = projObj.AddComponent<EnergyOrbProjectile>();
            float speedMod = weaponController != null ? weaponController.ProjectileSpeedMultiplier : 1f;
            proj.Initialize(dir, GetTotalDamage(), pierceCount, enemyLayer, player.gameObject, projectileSpeed * speedMod, projectileFrames);
        }

        protected override void OnLevelUpStats()
        {
            switch (currentLevel)
            {
                case 2:
                    baseDamage *= 1.25f;
                    break;
                case 3:
                    baseCooldown *= 0.85f;
                    break;
                case 4:
                    projectileCount = 2; // +1 Arrow!
                    break;
                case 5:
                    baseDamage *= 1.25f;
                    pierceCount = 3;
                    break;
                case 6:
                    baseCooldown *= 0.85f;
                    break;
                case 7:
                    projectileCount = 3; // +1 Arrow (Fan of 3 arrows!)
                    break;
                case 8:
                    baseDamage *= 1.35f;
                    pierceCount = 5;
                    projectileSpeed *= 1.25f;
                    baseCooldown *= 0.85f;
                    break;
            }
        }

        public override string GetNextLevelDescription()
        {
            int next = currentLevel + 1;
            switch (next)
            {
                case 2: return $"+25% Arrow Damage ({baseDamage * 1.25f:F0} dmg)";
                case 3: return $"-15% Cooldown ({baseCooldown * 0.85f:F2}s)";
                case 4: return $"+1 Arrow! (Fires 2 arrows in cone)";
                case 5: return $"+25% Damage ({baseDamage * 1.25f:F0} dmg) & +1 Pierce";
                case 6: return $"-15% Cooldown ({baseCooldown * 0.85f:F2}s)";
                case 7: return $"+1 Arrow! (Fires 3-arrow fan spread!)";
                case 8: return $"+35% Damage, +2 Pierce (5 mobs) & +25% Arrow Speed!";
                default: return "Max Level reached!";
            }
        }

        public void ConfigureRangedWeapon(string id, string name, Sprite iconSpr, string desc, string syn, float dmg, float cd, float range, float speed, int pierce, int count, Sprite[] frames)
        {
            weaponId = id;
            weaponName = name;
            icon = iconSpr;
            weaponDescription = desc;
            weaponSynergy = syn;
            baseDamage = dmg;
            baseCooldown = cd;
            attackRange = range;
            projectileSpeed = speed;
            pierceCount = pierce;
            projectileCount = count;
            projectileFrames = frames;
        }

        public void ConfigureProjectile(Sprite[] frames, int pierce = 2, int count = 1)
        {
            projectileFrames = frames;
            pierceCount = pierce;
            projectileCount = count;
        }
    }
}
