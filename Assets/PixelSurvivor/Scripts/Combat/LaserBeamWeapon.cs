using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class LaserBeamWeapon : WeaponBase
    {
        [Header("Laser Beam Visuals")]
        [SerializeField] private Sprite[] laserFrames;
        [SerializeField] private float beamLength = 8.5f;
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
            // Emitter starts cleanly 0.45m in front of player (zero segment behind character)
            Vector3 emitterPos = startPos + (Vector3)(beamDir * 0.45f);
            float angle = Mathf.Atan2(beamDir.y, beamDir.x) * Mathf.Rad2Deg;

            // 1. Spawn Laser VFX
            if (laserFrames != null && laserFrames.Length > 0)
            {
                GameObject laserObj = new GameObject($"{weaponName}_Beam");
                laserObj.transform.position = emitterPos;
                laserObj.transform.rotation = Quaternion.Euler(0, 0, angle);
                float areaMod = weaponController != null ? weaponController.AreaMultiplier : 1f;
                // Beam sprite has its pivot set directly at the emitter origin (12px / 144px).
                // Effective forward reach is scaled to beamLength.
                laserObj.transform.localScale = new Vector3(beamLength / 3.75f, beamThickness * areaMod, 1f);

                SpriteRenderer sr = laserObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 24;

                AnimatedVFX anim = laserObj.AddComponent<AnimatedVFX>();
                anim.Initialize(laserFrames, vfxFps);
            }

            // 2. Perform Line Cast for instant damage
            float effectiveThickness = beamThickness * (weaponController != null ? weaponController.AreaMultiplier : 1f);
            RaycastHit2D[] hits = Physics2D.CircleCastAll(emitterPos, effectiveThickness * 0.5f, beamDir, beamLength, enemyLayer);
            float dmg = GetTotalDamage();

            for (int i = 0; i < hits.Length; i++)
            {
                if (player != null && hits[i].collider.gameObject == player.gameObject) continue;

                IDamageable victim = hits[i].collider.GetComponent<IDamageable>();
                if (victim != null && victim.IsAlive)
                {
                    victim.TakeDamage(dmg, beamDir, knockbackForce);
                }
            }
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
                    beamThickness *= 1.35f;
                    beamLength *= 1.15f;
                    attackRange *= 1.15f;
                    break;
                case 5:
                    baseDamage *= 1.25f;
                    break;
                case 6:
                    baseCooldown *= 0.85f;
                    break;
                case 7:
                    beamThickness *= 1.30f;
                    break;
                case 8:
                    baseDamage *= 1.40f;
                    baseCooldown *= 0.80f;
                    beamLength *= 1.25f;
                    attackRange *= 1.25f;
                    break;
            }
        }

        public override string GetNextLevelDescription()
        {
            int next = currentLevel + 1;
            switch (next)
            {
                case 2: return $"+25% Beam Damage ({baseDamage * 1.25f:F0} dmg)";
                case 3: return $"-15% Cooldown ({baseCooldown * 0.85f:F2}s)";
                case 4: return $"+35% Laser Thickness & +15% Length";
                case 5: return $"+25% Beam Damage ({baseDamage * 1.25f:F0} dmg)";
                case 6: return $"-15% Cooldown ({baseCooldown * 0.85f:F2}s)";
                case 7: return $"+30% Massive Beam Width";
                case 8: return $"+40% Damage, -20% Cooldown & +25% Reach!";
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

        public void ConfigureLaser(Sprite[] frames, float length = 8.5f, float thickness = 1.0f)
        {
            laserFrames = frames;
            beamLength = length;
            beamThickness = thickness;
        }
    }
}
