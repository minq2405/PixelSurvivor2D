using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class RangedProjectileWeapon : WeaponBase
    {
        [Header("Projectile Settings")]
        [SerializeField] private Sprite[] projectileFrames;
        public Sprite[] ProjectileFrames => projectileFrames;
        [SerializeField] private float projectileSpeed = 10.5f;
        [SerializeField] private int pierceCount = 2;
        [SerializeField] private int projectileCount = 1;
        [SerializeField] private float pierceDamageFalloff = 0.25f; // v2.1: -25% dmg per pierce

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

        protected override bool CanAttack()
        {
            if (requireTargetInRange)
            {
                // Crossbow prioritizes Boss > Elite > Closest (v2.1 Section 4.4)
                Transform target = FindTarget(attackRange, true);
                return target != null;
            }
            return true;
        }

        protected override void ExecuteAttack()
        {
            // Priority targeting: Boss > Elite > Closest
            Transform target = FindTarget(attackRange, true);
            Vector2 baseDir = target != null 
                ? (Vector2)(target.position - player.transform.position).normalized 
                : (player != null ? player.FacingDirection : Vector2.right);

            bool isBossOrElite = target != null && (
                target.name.Contains("Boss") || 
                target.name.Contains("Warden") || 
                target.name.Contains("Golem") || 
                target.GetComponent<EliteEnemyAffixController>() != null
            );

            var (calculatedDmg, isCrit) = GetDamageAndCrit(0f, isBossOrElite);

            for (int i = 0; i < projectileCount; i++)
            {
                Vector2 fireDir = baseDir;
                if (projectileCount > 1)
                {
                    float angle = (i - (projectileCount - 1) / 2f) * 12f;
                    fireDir = Quaternion.Euler(0, 0, angle) * baseDir;
                }

                SpawnProjectile(fireDir, calculatedDmg, isCrit);
            }
        }

        private void SpawnProjectile(Vector2 dir, float dmg, bool crit)
        {
            if (player == null) return;

            GameObject projObj = new GameObject($"{weaponName}_Bolt");
            projObj.transform.position = player.transform.position + (Vector3)(dir * 0.6f);
            
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            projObj.transform.rotation = Quaternion.Euler(0, 0, angle);

            EnergyOrbProjectile proj = projObj.AddComponent<EnergyOrbProjectile>();
            float speedMod = weaponController != null ? weaponController.ProjectileSpeedMultiplier : 1f;

            proj.Initialize(
                dir, 
                dmg, 
                pierceCount, 
                enemyLayer, 
                player.gameObject, 
                projectileSpeed * speedMod, 
                projectileFrames,
                pierceDamageFalloff,
                false, 1.0f, 0.4f,
                weaponId,
                crit
            );
        }

        protected override void OnLevelUpStats()
        {
            // Table 4.2: Lv1: 21 · CD 0.95 -> Lv4: 2x18 · CD 0.80 -> Lv8: 3x17.5 · CD 0.60 (DPS 87.5)
            switch (currentLevel)
            {
                case 2: baseDamage = 24f; baseCooldown = 0.90f; break;
                case 3: baseDamage = 28f; baseCooldown = 0.85f; break;
                case 4: baseDamage = 18f; baseCooldown = 0.80f; projectileCount = 2; pierceCount = 3; break;
                case 5: baseDamage = 20f; baseCooldown = 0.74f; break;
                case 6: baseDamage = 22f; baseCooldown = 0.68f; break;
                case 7: baseDamage = 16f; baseCooldown = 0.64f; projectileCount = 3; break;
                case 8: baseDamage = 17.5f; baseCooldown = 0.60f; projectileCount = 3; pierceCount = 3; projectileSpeed = 12f; break;
            }
        }

        public override string GetNextLevelDescription()
        {
            int next = currentLevel + 1;
            switch (next)
            {
                case 2: return "24 dmg · CD 0.90s · Pierce 2 (-25%/pierce)";
                case 3: return "28 dmg · CD 0.85s";
                case 4: return "+1 Arrow! (2x18 dmg) · CD 0.80s · Pierce 3";
                case 5: return "2x20 dmg · CD 0.74s";
                case 6: return "2x22 dmg · CD 0.68s";
                case 7: return "+1 Arrow! (3x16 dmg) · CD 0.64s";
                case 8: return "3x17.5 dmg · CD 0.60s (Max DPS 87.5)";
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
    }
}
