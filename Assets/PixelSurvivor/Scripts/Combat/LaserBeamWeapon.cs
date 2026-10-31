using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class LaserBeamWeapon : WeaponBase
    {
        [Header("Laser Beam Visuals")]
        [SerializeField] private Sprite[] laserFrames;
        public Sprite[] LaserFrames => laserFrames;
        [SerializeField] private float beamLength = 7.0f;
        [SerializeField] private float beamThickness = 1.0f;
        [SerializeField] private float vfxFps = 18f;

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
            Vector2 beamDir = target != null 
                ? (Vector2)(target.position - player.transform.position).normalized 
                : (player != null ? player.FacingDirection : Vector2.right);

            Vector3 startPos = player != null ? player.transform.position : transform.position;
            Vector3 emitterPos = startPos + (Vector3)(beamDir * 0.45f);
            float angle = Mathf.Atan2(beamDir.y, beamDir.x) * Mathf.Rad2Deg;

            // 1. Spawn Laser VFX
            if (laserFrames != null && laserFrames.Length > 0)
            {
                GameObject laserObj = new GameObject($"{weaponName}_Beam");
                laserObj.transform.position = emitterPos;
                laserObj.transform.rotation = Quaternion.Euler(0, 0, angle);
                float areaMod = weaponController != null ? weaponController.AreaMultiplier : 1f;
                laserObj.transform.localScale = new Vector3(beamLength / 3.75f, beamThickness * areaMod, 1f);

                SpriteRenderer sr = laserObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 24;

                AnimatedVFX anim = laserObj.AddComponent<AnimatedVFX>();
                anim.Initialize(laserFrames, vfxFps);
            }

            // 2. Perform Line Cast for instant damage
            float effectiveThickness = beamThickness * (weaponController != null ? weaponController.AreaMultiplier : 1f);
            RaycastHit2D[] hits = Physics2D.CircleCastAll(emitterPos, effectiveThickness * 0.5f, beamDir, beamLength, enemyLayer);

            for (int i = 0; i < hits.Length; i++)
            {
                if (player != null && hits[i].collider.gameObject == player.gameObject) continue;

                EnemyHealth eh = hits[i].collider.GetComponent<EnemyHealth>();
                if (eh == null && hits[i].collider.transform.parent != null)
                {
                    eh = hits[i].collider.transform.parent.GetComponent<EnemyHealth>();
                }

                if (eh != null && eh.IsAlive)
                {
                    bool isBossOrElite = eh.gameObject.name.Contains("Boss") || 
                                         eh.gameObject.name.Contains("Warden") || 
                                         eh.gameObject.name.Contains("Golem") || 
                                         eh.GetComponent<EliteEnemyAffixController>() != null;

                    var (finalDmg, isCrit) = GetDamageAndCrit(0f, isBossOrElite);
                    eh.TakeDamage(finalDmg, beamDir, knockbackForce, isCrit, weaponId);
                    continue;
                }

                IDamageable victim = hits[i].collider.GetComponent<IDamageable>();
                if (victim != null && victim.IsAlive)
                {
                    var (finalDmg, isCrit) = GetDamageAndCrit(0f, false);
                    victim.TakeDamage(finalDmg, beamDir, knockbackForce);
                }
            }
        }

        protected override void OnLevelUpStats()
        {
            // Table 4.2: Lv1: 37 · CD 1.75 -> Lv4: 61 · CD 1.45 -> Lv8: 92 · CD 1.10 (DPS 83.6)
            switch (currentLevel)
            {
                case 2: baseDamage = 44f; baseCooldown = 1.65f; break;
                case 3: baseDamage = 52f; baseCooldown = 1.55f; break;
                case 4: baseDamage = 61f; baseCooldown = 1.45f; beamThickness = 1.1f; attackRange = 7.2f; break;
                case 5: baseDamage = 68f; baseCooldown = 1.35f; break;
                case 6: baseDamage = 76f; baseCooldown = 1.25f; break;
                case 7: baseDamage = 84f; baseCooldown = 1.18f; break;
                case 8: baseDamage = 92f; baseCooldown = 1.10f; beamLength = 7.5f; beamThickness = 1.2f; attackRange = 7.5f; break;
            }
        }

        public override string GetNextLevelDescription()
        {
            int next = currentLevel + 1;
            switch (next)
            {
                case 2: return "44 dmg · CD 1.65s (Piercing Beam)";
                case 3: return "52 dmg · CD 1.55s";
                case 4: return "61 dmg · CD 1.45s · Reach 7.2m";
                case 5: return "68 dmg · CD 1.35s";
                case 6: return "76 dmg · CD 1.25s";
                case 7: return "84 dmg · CD 1.18s";
                case 8: return "92 dmg · CD 1.10s · Reach 7.5m (Max DPS 83.6)";
                default: return "Max Level reached!";
            }
        }

        public void ConfigureLaserWeapon(string id, string name, Sprite iconSpr, string desc, string syn, float dmg, float cd, float range, float length, float thickness, Sprite[] frames)
        {
            weaponId = id;
            weaponName = name;
            icon = iconSpr;
            weaponDescription = desc;
            weaponSynergy = syn;
            baseDamage = dmg;
            baseCooldown = cd;
            attackRange = range;
            beamLength = length;
            beamThickness = thickness;
            laserFrames = frames;
        }
    }
}
