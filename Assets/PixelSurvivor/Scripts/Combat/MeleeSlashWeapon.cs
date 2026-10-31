using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class MeleeSlashWeapon : WeaponBase
    {
        [Header("Melee & VFX Alignment")]
        [SerializeField] private float hitRadius = 1.3f;
        [SerializeField] private float spawnOffset = 0.6f;
        [SerializeField] private Vector2 vfxLocalOffset = Vector2.zero;
        [SerializeField] private float vfxScale = 1.4f;
        [SerializeField] private bool followPlayer = false;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] slashVfxFrames;
        public Sprite[] SlashVfxFrames => slashVfxFrames;
        [SerializeField] private float vfxFrameRate = 20f;

        [Header("Multi-Strike Combo Settings")]
        [SerializeField] private int strikesCount = 1;
        [SerializeField] private float strikeDelay = 0.12f;

        // Scythe lifesteal window (capped at 5 HP/s)
        private float scytheHealWindowStart = 0f;
        private float scytheHealedInWindow = 0f;
        private const float MAX_SCYTHE_HEAL_PER_SEC = 5.0f;

        protected override void Awake()
        {
            base.Awake();
            if (enemyLayer.value == 0)
            {
                enemyLayer = ~LayerMask.GetMask("UI", "Ignore Raycast");
            }
        }

        private void OnEnable()
        {
            EnemyHealth.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            EnemyHealth.OnEnemyKilled -= HandleEnemyKilled;
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
                bool isPriority = (weaponId == "weapon_twin_daggers");
                Transform target = FindTarget(attackRange, isPriority);
                return target != null;
            }
            return true;
        }

        protected override void ExecuteAttack()
        {
            // 1. Aim directly towards priority enemy if present in attackRange, otherwise facing direction
            Vector2 aimDirection = player != null ? player.FacingDirection : Vector2.right;

            bool isPriority = (weaponId == "weapon_twin_daggers");
            Transform target = FindTarget(attackRange, isPriority);
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

        private IEnumerator PerformMultiStrikes(Vector2 aimDirection)
        {
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
                slashObj.transform.localScale = new Vector3(vfxScale * areaMod, (flipVisual ? -vfxScale : vfxScale) * areaMod, 1f);
                slashObj.transform.rotation = Quaternion.Euler(0, 0, angle);

                SpriteRenderer sr = slashObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 25;

                AnimatedVFX anim = slashObj.AddComponent<AnimatedVFX>();
                anim.Initialize(slashVfxFrames, vfxFrameRate);
            }

            // 2. Perform Hitbox Damage Check
            Vector3 hitCenter = player.transform.position + (Vector3)(direction * (spawnOffset + 0.5f));
            float area = hitRadius * (weaponController != null ? weaponController.AreaMultiplier : 1f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, area, enemyLayer);

            float weaponCritBonus = (weaponId == "weapon_twin_daggers") ? 0.08f : 0f;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == player.gameObject) continue;

                EnemyHealth eh = hits[i].GetComponent<EnemyHealth>();
                if (eh == null && hits[i].transform.parent != null)
                {
                    eh = hits[i].transform.parent.GetComponent<EnemyHealth>();
                }

                if (eh != null && eh.IsAlive)
                {
                    bool isBossOrElite = eh.gameObject.name.Contains("Boss") || 
                                         eh.gameObject.name.Contains("Warden") || 
                                         eh.gameObject.name.Contains("Golem") || 
                                         eh.GetComponent<EliteEnemyAffixController>() != null;

                    var (finalDmg, isCrit) = GetDamageAndCrit(weaponCritBonus, isBossOrElite);
                    eh.TakeDamage(finalDmg, direction, knockbackForce, isCrit, weaponId);

                    // Battle Axe Stagger: 0.4s
                    if (weaponId == "weapon_battle_axe")
                    {
                        EnemyAI ai = eh.GetComponent<EnemyAI>();
                        if (ai != null) ai.ApplyStun(0.4f);
                    }

                    // Magma Broadsword Burn: 4 / 7 / 10 dmg/s for 3s
                    if (weaponId == "weapon_magma_broadsword")
                    {
                        float burnDps = (currentLevel >= 8) ? 10f : ((currentLevel >= 4) ? 7f : 4f);
                        eh.ApplyBurn(burnDps, 3.0f);
                    }
                    continue;
                }

                IDamageable victim = hits[i].GetComponent<IDamageable>();
                if (victim != null && victim.IsAlive)
                {
                    var (finalDmg, isCrit) = GetDamageAndCrit(weaponCritBonus, false);
                    victim.TakeDamage(finalDmg, direction, knockbackForce);
                }
            }
        }

        private void HandleEnemyKilled(EnemyHealth eh, string killerWeaponId)
        {
            if (killerWeaponId == "weapon_dark_scythe" && player != null)
            {
                if (Time.time - scytheHealWindowStart >= 1.0f)
                {
                    scytheHealWindowStart = Time.time;
                    scytheHealedInWindow = 0f;
                }

                float healAmount = (currentLevel >= 5) ? 1.5f : 1.0f;
                float allowedHeal = Mathf.Min(healAmount, MAX_SCYTHE_HEAL_PER_SEC - scytheHealedInWindow);
                if (allowedHeal > 0f)
                {
                    PlayerHealth ph = player.GetComponent<PlayerHealth>();
                    if (ph != null) ph.Heal(allowedHeal);
                    scytheHealedInWindow += allowedHeal;
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
            // Cân bằng theo Bảng 4.2 (Pixel Survivor v2.1)
            switch (weaponId)
            {
                case "weapon_magma_broadsword":
                    // Lv1: 20 · CD 1.10 -> Lv4: 36 · CD 0.95 -> Lv8: 62 · CD 0.80
                    switch (currentLevel)
                    {
                        case 2: baseDamage = 24f; baseCooldown = 1.05f; break;
                        case 3: baseDamage = 30f; baseCooldown = 1.00f; break;
                        case 4: baseDamage = 36f; baseCooldown = 0.95f; hitRadius = 1.4f; break;
                        case 5: baseDamage = 42f; baseCooldown = 0.90f; break;
                        case 6: baseDamage = 48f; baseCooldown = 0.85f; break;
                        case 7: baseDamage = 55f; baseCooldown = 0.82f; hitRadius = 1.5f; break;
                        case 8: baseDamage = 62f; baseCooldown = 0.80f; attackRange = 3.0f; hitRadius = 1.6f; break;
                    }
                    break;

                case "weapon_battle_axe":
                    // Lv1: 34 · CD 1.70 -> Lv4: 60 · CD 1.50 -> Lv8: 100 · CD 1.25 (Stagger 0.4s, KB 5.5)
                    switch (currentLevel)
                    {
                        case 2: baseDamage = 42f; baseCooldown = 1.62f; break;
                        case 3: baseDamage = 50f; baseCooldown = 1.55f; break;
                        case 4: baseDamage = 60f; baseCooldown = 1.50f; attackRange = 2.9f; break;
                        case 5: baseDamage = 70f; baseCooldown = 1.42f; break;
                        case 6: baseDamage = 80f; baseCooldown = 1.35f; break;
                        case 7: baseDamage = 90f; baseCooldown = 1.30f; attackRange = 3.0f; break;
                        case 8: baseDamage = 100f; baseCooldown = 1.25f; attackRange = 3.2f; knockbackForce = 5.5f; break;
                    }
                    break;

                case "weapon_twin_daggers":
                    // Lv1: 12x2 · CD 0.70 -> Lv4: 16x3 · CD 0.55 -> Lv8: 18x4 · CD 0.42 (Crit +8%)
                    switch (currentLevel)
                    {
                        case 2: baseDamage = 13f; baseCooldown = 0.65f; break;
                        case 3: baseDamage = 15f; baseCooldown = 0.60f; break;
                        case 4: baseDamage = 16f; baseCooldown = 0.55f; strikesCount = 3; vfxFrameRate = 35f; break;
                        case 5: baseDamage = 16.5f; baseCooldown = 0.50f; break;
                        case 6: baseDamage = 17f; baseCooldown = 0.46f; break;
                        case 7: baseDamage = 17.5f; baseCooldown = 0.44f; strikesCount = 4; vfxFrameRate = 42f; break;
                        case 8: baseDamage = 18f; baseCooldown = 0.42f; strikesCount = 4; vfxFrameRate = 48f; break;
                    }
                    break;

                case "weapon_dark_scythe":
                    // Lv1: 24 · CD 1.35 -> Lv4: 41 · CD 1.15 -> Lv8: 67 · CD 0.95 (Heal 1.0->1.5 HP/kill)
                    switch (currentLevel)
                    {
                        case 2: baseDamage = 29f; baseCooldown = 1.28f; break;
                        case 3: baseDamage = 35f; baseCooldown = 1.20f; break;
                        case 4: baseDamage = 41f; baseCooldown = 1.15f; attackRange = 3.0f; break;
                        case 5: baseDamage = 47f; baseCooldown = 1.10f; break;
                        case 6: baseDamage = 53f; baseCooldown = 1.05f; break;
                        case 7: baseDamage = 60f; baseCooldown = 1.00f; break;
                        case 8: baseDamage = 67f; baseCooldown = 0.95f; attackRange = 3.2f; break;
                    }
                    break;
            }
        }

        public override string GetNextLevelDescription()
        {
            int next = currentLevel + 1;
            switch (weaponId)
            {
                case "weapon_magma_broadsword":
                    switch (next)
                    {
                        case 2: return "24 dmg · CD 1.05s · Burn 4 dmg/s";
                        case 3: return "30 dmg · CD 1.00s · Burn 5 dmg/s";
                        case 4: return "36 dmg · CD 0.95s · Burn 7 dmg/s (+Area)";
                        case 5: return "42 dmg · CD 0.90s · Burn 8 dmg/s";
                        case 6: return "48 dmg · CD 0.85s · Burn 8 dmg/s";
                        case 7: return "55 dmg · CD 0.82s · Burn 9 dmg/s (+Reach)";
                        case 8: return "62 dmg · CD 0.80s · Burn 10 dmg/s (Max DPS 87.5)";
                        default: return "Max Level!";
                    }

                case "weapon_battle_axe":
                    switch (next)
                    {
                        case 2: return "42 dmg · CD 1.62s · Stagger 0.4s";
                        case 3: return "50 dmg · CD 1.55s · Stagger 0.4s";
                        case 4: return "60 dmg · CD 1.50s · Reach 2.9m";
                        case 5: return "70 dmg · CD 1.42s · Stagger 0.4s";
                        case 6: return "80 dmg · CD 1.35s · Stagger 0.4s";
                        case 7: return "90 dmg · CD 1.30s · Reach 3.0m";
                        case 8: return "100 dmg · CD 1.25s · Stagger 0.4s, KB 5.5 (Max DPS 80.0)";
                        default: return "Max Level!";
                    }

                case "weapon_twin_daggers":
                    switch (next)
                    {
                        case 2: return "13x2 dmg · CD 0.65s · Crit +8%";
                        case 3: return "15x2 dmg · CD 0.60s · Crit +8%";
                        case 4: return "+1 Strike! (16x3 dmg) · CD 0.55s";
                        case 5: return "16.5x3 dmg · CD 0.50s · Crit +8%";
                        case 6: return "17x3 dmg · CD 0.46s · Crit +8%";
                        case 7: return "+1 Strike! (17.5x4 dmg) · CD 0.44s";
                        case 8: return "18x4 dmg · CD 0.42s · Crit +8% (Max DPS 171.4)";
                        default: return "Max Level!";
                    }

                case "weapon_dark_scythe":
                    switch (next)
                    {
                        case 2: return "29 dmg · CD 1.28s · Heal 1.0 HP/kill";
                        case 3: return "35 dmg · CD 1.20s · Heal 1.0 HP/kill";
                        case 4: return "41 dmg · CD 1.15s · Sweep 3.0m";
                        case 5: return "47 dmg · CD 1.10s · Heal 1.5 HP/kill!";
                        case 6: return "53 dmg · CD 1.05s · Heal 1.5 HP/kill";
                        case 7: return "60 dmg · CD 1.00s · Heal 1.5 HP/kill";
                        case 8: return "67 dmg · CD 0.95s · Heal 1.5 HP/kill (Max DPS 70.5)";
                        default: return "Max Level!";
                    }

                default:
                    return $"Upgrade to Level {next}!";
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
    }
}
