using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class MeleeSlashWeapon : WeaponBase
    {
        [Header("Melee VFX Animation & Scaling")]
        [SerializeField] private Sprite[] slashVfxFrames;
        [SerializeField] private float vfxFrameRate = 20f;
        [SerializeField] private float vfxScale = 1.4f;

        [Header("Offset & Alignment Adjustments")]
        [Tooltip("Khoảng cách từ tâm Player đến nhát chém (mặc định 0.25)")]
        [SerializeField] private float spawnOffset = 0.25f;

        [Tooltip("Góc bù xoay để căn hướng nhát chém về phía trước (mặc định 0 cho sprite quay phải)")]
        [SerializeField] private float vfxAngleOffset = 0f;

        [Tooltip("Độ lệch bù vị trí X/Y nếu sprite bị vẽ lệch tâm")]
        [SerializeField] private Vector2 vfxLocalOffset = Vector2.zero;

        [Tooltip("Nhát chém có bám theo người chơi khi đang di chuyển không")]
        [SerializeField] private bool followPlayer = true;

        [Header("Hitbox Settings")]
        [SerializeField] private float hitRadius = 1.8f;

        [Header("Multi-Strike")]
        [SerializeField] private int strikesCount = 1;
        [SerializeField] private float strikeDelay = 0.12f;

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
            // 1. Aim directly towards closest enemy if present in attackRange, otherwise facing direction
            Vector2 aimDirection = player != null ? player.FacingDirection : Vector2.right;

            Transform target = FindClosestEnemy(attackRange);
            if (target != null && player != null)
            {
                Vector2 dirToTarget = ((Vector2)target.position - (Vector2)player.transform.position).normalized;
                if (dirToTarget.sqrMagnitude > 0.001f)
                {
                    aimDirection = dirToTarget;
                }
            }

            if (strikesCount > 1)
            {
                StartCoroutine(PerformMultiStrikes(aimDirection));
            }
            else
            {
                PerformSingleStrike(aimDirection);
            }
        }

        private System.Collections.IEnumerator PerformMultiStrikes(Vector2 aimDirection)
        {
            // Duration of one animation so sequential strikes never overlap
            float animDuration = (slashVfxFrames != null && slashVfxFrames.Length > 0 && vfxFrameRate > 0)
                ? (slashVfxFrames.Length / vfxFrameRate)
                : 0.18f;
            float delay = Mathf.Max(0.06f, animDuration);

            for (int i = 0; i < strikesCount; i++)
            {
                Vector2 strikeDir = aimDirection;
                if (i > 0)
                {
                    float angleOffset = (i % 2 == 1) ? 8f : -8f;
                    strikeDir = Quaternion.Euler(0, 0, angleOffset) * aimDirection;
                }

                // Alternate flip on odd strikes (Strike 1 normal right-to-left, Strike 2 flipped left-to-right)
                bool flipStrike = (i % 2 == 1);
                PerformSingleStrike(strikeDir, flipStrike);
                yield return new WaitForSeconds(delay);
            }
        }

        private void PerformSingleStrike(Vector2 direction, bool flipVisual = false)
        {
            if (player == null) return;

            Vector3 spawnPos = player.transform.position + (Vector3)(direction * spawnOffset) + (Vector3)vfxLocalOffset;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 1. Spawn VFX GameObject
            if (slashVfxFrames != null && slashVfxFrames.Length > 0)
            {
                GameObject slashObj = new GameObject($"{weaponName}_Slash");

                if (followPlayer)
                {
                    slashObj.transform.SetParent(player.transform);
                    slashObj.transform.localPosition = (Vector3)(direction * spawnOffset) + (Vector3)vfxLocalOffset;
                }
                else
                {
                    slashObj.transform.position = spawnPos;
                }

                float areaMod = weaponController != null ? weaponController.AreaMultiplier : 1f;
                float finalScale = vfxScale * areaMod;

                // Rotate slash arc to accurately face target direction across 360 degrees
                slashObj.transform.rotation = Quaternion.Euler(0f, 0f, angle + vfxAngleOffset);
                slashObj.transform.localScale = new Vector3(finalScale, finalScale, 1f);

                SpriteRenderer sr = slashObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 25;
                if (flipVisual)
                {
                    sr.flipY = true;
                }

                AnimatedVFX anim = slashObj.AddComponent<AnimatedVFX>();
                anim.Initialize(slashVfxFrames, vfxFrameRate);
            }

            // 2. Perform Hitbox Damage Check
            Vector3 hitCenter = player.transform.position + (Vector3)(direction * (spawnOffset + 0.5f));
            float area = hitRadius * (weaponController != null ? weaponController.AreaMultiplier : 1f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, area, enemyLayer);

            float totalDmg = GetTotalDamage();

            for (int i = 0; i < hits.Length; i++)
            {
                // Don't damage player
                if (hits[i].gameObject == player.gameObject) continue;

                IDamageable victim = hits[i].GetComponent<IDamageable>();
                if (victim != null && victim.IsAlive)
                {
                    victim.TakeDamage(totalDmg, direction, knockbackForce);
                }
            }
        }

        public void ConfigureVFX(Sprite[] frames, float scale = 1.2f, float fps = 20f, int strikes = 1)
        {
            slashVfxFrames = frames;
            vfxScale = scale;
            vfxFrameRate = fps;
            strikesCount = strikes;
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
                    hitRadius *= 1.25f;
                    vfxScale *= 1.20f;
                    attackRange *= 1.15f;
                    baseDamage *= 1.15f;
                    break;
                case 5:
                    if (weaponId == "weapon_twin_daggers")
                    {
                        strikesCount = 3;
                        vfxFrameRate = 42f; // Snappy triple combo
                    }
                    else baseDamage *= 1.25f;
                    break;
                case 6:
                    baseCooldown *= 0.85f;
                    break;
                case 7:
                    hitRadius *= 1.20f;
                    vfxScale *= 1.15f;
                    attackRange *= 1.15f;
                    break;
                case 8:
                    if (weaponId == "weapon_twin_daggers")
                    {
                        strikesCount = 4;
                        vfxFrameRate = 50f; // Lightning fast quad combo
                    }
                    baseDamage *= 1.35f;
                    baseCooldown *= 0.80f;
                    knockbackForce *= 1.4f;
                    break;
            }
        }

        public override string GetNextLevelDescription()
        {
            int next = currentLevel + 1;
            switch (next)
            {
                case 2: return $"+25% Damage ({baseDamage * 1.25f:F0} dmg)";
                case 3: return $"-15% Cooldown ({baseCooldown * 0.85f:F2}s)";
                case 4: return $"+25% Slash Area & +15% Damage ({baseDamage * 1.15f:F0} dmg)";
                case 5: return weaponId == "weapon_twin_daggers" ? "+1 Strike! (Triple Combo)" : $"+25% Damage ({baseDamage * 1.25f:F0} dmg)";
                case 6: return $"-15% Cooldown ({baseCooldown * 0.85f:F2}s)";
                case 7: return $"+20% Slash Reach & Hitbox";
                case 8: return weaponId == "weapon_twin_daggers" 
                    ? "+1 Strike! (Quadruple Combo!) & +35% Damage" 
                    : $"+35% Damage, -20% Cooldown & +40% Knockback!";
                default: return "Max Level reached!";
            }
        }

        public void ConfigureMeleeWeapon(string id, string name, Sprite iconSpr, string desc, string syn, float dmg, float cd, float range, float radius, float kb, float scale, int strikes, Sprite[] frames, float fps = 20f, float delay = 0.12f)
        {
            weaponId = id;
            weaponName = name;
            icon = iconSpr;
            weaponDescription = desc;
            weaponSynergy = syn;
            baseDamage = dmg;
            baseCooldown = cd;
            attackRange = range;
            hitRadius = radius;
            knockbackForce = kb;
            vfxScale = scale;
            strikesCount = strikes;
            slashVfxFrames = frames;
            vfxFrameRate = fps;
            strikeDelay = delay;
        }

        [ContextMenu("Sync Range & Hitbox with VFX Scale")]
        public void SyncRangeWithVFX()
        {
            // Calculate physical reach based on 96px sprite (arc radius ~36px / 32 PPU = 1.125m)
            float arcRadius = 1.125f * vfxScale;
            attackRange = Mathf.Round((spawnOffset + arcRadius) * 10f) / 10f;
            hitRadius = Mathf.Round((arcRadius * 0.75f) * 10f) / 10f;
            Debug.Log($"<color=#10B981>[Sync]</color> Synced {weaponName}: AttackRange = {attackRange}m, HitRadius = {hitRadius}m (Visual Reach: {spawnOffset + arcRadius:F2}m)");
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            if (player != null) origin = player.transform.position;

            Vector2 dir = player != null ? player.FacingDirection : Vector2.right;

            // 1. Attack Range (Vòng tròn VÀNG: Tầm phát hiện quái để kích hoạt chém)
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(origin, attackRange);

            // 2. Damage Hitbox (Vòng tròn ĐỎ CAM: Vùng thực tế quái bị nhận sát thương)
            Vector3 hitCenter = origin + (Vector3)(dir * (spawnOffset + 0.5f));
            float area = hitRadius * (weaponController != null ? weaponController.AreaMultiplier : 1f);
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(hitCenter, area);

            // 3. VFX Spawn Center & Line (Đường CYAN: Hướng xuất chiêu)
            Vector3 vfxCenter = origin + (Vector3)(dir * spawnOffset) + (Vector3)vfxLocalOffset;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, hitCenter);
            Gizmos.DrawWireSphere(vfxCenter, 0.12f);

            // 4. Visual Reach Tip (Điểm XANH LÁ: Đầu mút xa nhất của nhát chém)
            float visualReach = spawnOffset + (1.125f * vfxScale);
            Vector3 visualTip = origin + (Vector3)(dir * visualReach);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(visualTip, 0.1f);
        }
    }
}
