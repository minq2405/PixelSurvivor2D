using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    // ==========================================
    // 1. SOLAR FLARE GREATSWORD (Broadsword + Book of Damage)
    // ==========================================
    public class SolarFlareGreatsword : WeaponBase
    {
        [Header("Solar Flare Attributes")]
        [SerializeField] private float novaRadius = 3.0f;
        [SerializeField] private float auraRadius = 6.0f;
        [SerializeField] private float auraDps = 12.0f;
        [SerializeField] private int maxAuraTargets = 12;
        [SerializeField] private Sprite[] slashVfxFrames;

        private float auraTickTimer = 0f;

        protected override void Awake()
        {
            weaponId = "weapon_solar_flare_greatsword";
            weaponName = "Solar Flare Greatsword";
            baseDamage = 95f;
            baseCooldown = 0.80f;
            attackRange = 3.5f;
            knockbackForce = 4.5f;
            isEvolved = true;
            currentLevel = 8;
            maxLevel = 8;
            base.Awake();
        }

        public void Configure(Sprite iconSpr, Sprite[] frames)
        {
            icon = iconSpr;
            slashVfxFrames = frames;
        }

        protected override void Update()
        {
            base.Update();
            if (player == null) return;

            // Fire Aura: 12 dmg/s on up to 12 closest enemies within 6.0m
            auraTickTimer += Time.deltaTime;
            if (auraTickTimer >= 0.5f)
            {
                auraTickTimer = 0f;
                ApplyFireAura();
            }
        }

        private void ApplyFireAura()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, auraRadius, enemyLayer);
            int damaged = 0;
            for (int i = 0; i < hits.Length && damaged < maxAuraTargets; i++)
            {
                if (hits[i].gameObject == player.gameObject) continue;
                EnemyHealth eh = hits[i].GetComponent<EnemyHealth>();
                if (eh != null && eh.IsAlive)
                {
                    eh.TakeDamage(auraDps * 0.5f, Vector2.zero, 0f, false, weaponId);
                    damaged++;
                }
            }
        }

        protected override void ExecuteAttack()
        {
            if (player == null) return;

            // Nova 360° Blast: 95 dmg, radius 3.0m
            Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, novaRadius, enemyLayer);
            var (finalDmg, isCrit) = GetDamageAndCrit(0f, false);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == player.gameObject) continue;
                EnemyHealth eh = hits[i].GetComponent<EnemyHealth>();
                if (eh != null && eh.IsAlive)
                {
                    Vector2 kbDir = ((Vector2)eh.transform.position - (Vector2)player.transform.position).normalized;
                    eh.TakeDamage(finalDmg, kbDir, knockbackForce, isCrit, weaponId);
                    eh.ApplyBurn(10f, 3.0f);
                }
            }

            // Spawn Visual Nova
            if (slashVfxFrames != null && slashVfxFrames.Length > 0)
            {
                GameObject novaObj = new GameObject("SolarNova");
                novaObj.transform.position = player.transform.position;
                novaObj.transform.localScale = Vector3.one * (novaRadius * 0.8f);
                SpriteRenderer sr = novaObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 26;
                sr.color = new Color(1f, 0.75f, 0.2f, 0.9f);
                AnimatedVFX anim = novaObj.AddComponent<AnimatedVFX>();
                anim.Initialize(slashVfxFrames, 28f);
            }
        }

        protected override void OnLevelUpStats() { }
        public override string GetNextLevelDescription() => "Mythic Evolved Weapon (MAX)!";
    }

    // ==========================================
    // 2. EXECUTIONER'S GREATAXE (Battle Axe + Heart of Life)
    // ==========================================
    public class ExecutionerGreataxe : WeaponBase
    {
        [Header("Executioner Attributes")]
        [SerializeField] private float coneRadius = 3.6f;
        [SerializeField] private float executeHpThreshold = 0.10f; // Execute regular mobs < 10% HP
        [SerializeField] private Sprite[] slashVfxFrames;

        protected override void Awake()
        {
            weaponId = "weapon_executioners_greataxe";
            weaponName = "Executioner's Greataxe";
            baseDamage = 150f;
            baseCooldown = 1.25f;
            attackRange = 3.6f;
            knockbackForce = 6.0f;
            isEvolved = true;
            currentLevel = 8;
            maxLevel = 8;
            base.Awake();
        }

        public void Configure(Sprite iconSpr, Sprite[] frames)
        {
            icon = iconSpr;
            slashVfxFrames = frames;
        }

        protected override void ExecuteAttack()
        {
            if (player == null) return;

            Vector2 dir = player.FacingDirection;
            Transform target = FindClosestEnemy(attackRange);
            if (target != null) dir = ((Vector2)target.position - (Vector2)player.transform.position).normalized;

            Vector3 hitCenter = player.transform.position + (Vector3)(dir * 1.0f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, coneRadius * 0.6f, enemyLayer);

            var (finalDmg, isCrit) = GetDamageAndCrit(0f, false);
            finalDmg *= 1.15f; // Extra Crit Multiplier factor

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == player.gameObject) continue;
                EnemyHealth eh = hits[i].GetComponent<EnemyHealth>();
                if (eh != null && eh.IsAlive)
                {
                    bool isBossOrElite = eh.name.Contains("Boss") || eh.name.Contains("Warden") || eh.name.Contains("Golem") || eh.GetComponent<EliteEnemyAffixController>() != null;

                    // Execute regular mobs under 10% HP
                    if (!isBossOrElite && (eh.CurrentHealth / eh.MaxHealth <= executeHpThreshold))
                    {
                        DamageNumber.SpawnText(eh.transform.position + Vector3.up * 0.4f, "💀 EXECUTED!", Color.red);
                        eh.TakeDamage(99999f, dir, knockbackForce, true, weaponId);
                        continue;
                    }

                    eh.TakeDamage(finalDmg, dir, knockbackForce, isCrit, weaponId);

                    EnemyAI ai = eh.GetComponent<EnemyAI>();
                    if (ai != null) ai.ApplyStun(0.4f);
                }
            }

            // Spawn VFX
            if (slashVfxFrames != null && slashVfxFrames.Length > 0)
            {
                GameObject vfxObj = new GameObject("ExecutionerCleave");
                vfxObj.transform.position = hitCenter;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                vfxObj.transform.rotation = Quaternion.Euler(0, 0, angle);
                vfxObj.transform.localScale = Vector3.one * 2.2f;
                SpriteRenderer sr = vfxObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 26;
                sr.color = new Color(1f, 0.3f, 0.1f, 1f);
                AnimatedVFX anim = vfxObj.AddComponent<AnimatedVFX>();
                anim.Initialize(slashVfxFrames, 24f);
            }
        }

        protected override void OnLevelUpStats() { }
        public override string GetNextLevelDescription() => "Mythic Evolved Weapon (MAX)!";
    }

    // ==========================================
    // 3. SHADOW PHANTOM DAGGERS (Twin Daggers + Feather of Speed)
    // ==========================================
    public class ShadowPhantomDaggers : WeaponBase
    {
        [Header("Flurry Settings")]
        [SerializeField] private int flurryStrikes = 8;
        [SerializeField] private float strikeDamage = 16f;
        [SerializeField] private float dashDistance = 1.5f;
        [SerializeField] private Sprite[] slashVfxFrames;

        private Rigidbody2D playerRb;

        protected override void Awake()
        {
            weaponId = "weapon_shadow_phantom_daggers";
            weaponName = "Shadow Phantom Daggers";
            baseDamage = 16f;
            baseCooldown = 0.50f;
            attackRange = 2.4f;
            knockbackForce = 1.5f;
            isEvolved = true;
            currentLevel = 8;
            maxLevel = 8;
            base.Awake();
        }

        public void Configure(Sprite iconSpr, Sprite[] frames)
        {
            icon = iconSpr;
            slashVfxFrames = frames;
        }

        protected override void ExecuteAttack()
        {
            StartCoroutine(FlurryRoutine());
        }

        private IEnumerator FlurryRoutine()
        {
            if (player == null) yield break;
            if (playerRb == null) playerRb = player.GetComponent<Rigidbody2D>();

            Transform target = FindTarget(attackRange, true);
            Vector2 dashDir = target != null 
                ? ((Vector2)target.position - (Vector2)player.transform.position).normalized 
                : player.FacingDirection;

            // Micro-dash 1.5m
            if (playerRb != null)
            {
                playerRb.linearVelocity = dashDir * (dashDistance / 0.15f);
            }

            for (int i = 0; i < flurryStrikes; i++)
            {
                if (player == null) yield break;

                Vector2 strikeDir = Quaternion.Euler(0, 0, Random.Range(-15f, 15f)) * dashDir;
                Vector3 hitPos = player.transform.position + (Vector3)(strikeDir * 0.8f);

                Collider2D[] hits = Physics2D.OverlapCircleAll(hitPos, 1.2f, enemyLayer);
                var (finalDmg, isCrit) = GetDamageAndCrit(0.12f, false); // Extra +12% crit

                for (int h = 0; h < hits.Length; h++)
                {
                    if (hits[h].gameObject == player.gameObject) continue;
                    EnemyHealth eh = hits[h].GetComponent<EnemyHealth>();
                    if (eh != null && eh.IsAlive)
                    {
                        eh.TakeDamage(strikeDamage * (isCrit ? 1.9f : 1.0f), strikeDir, knockbackForce, isCrit, weaponId);
                    }
                }

                // VFX flash
                if (slashVfxFrames != null && slashVfxFrames.Length > 0)
                {
                    GameObject slashObj = new GameObject("PhantomSlash");
                    slashObj.transform.position = hitPos;
                    float angle = Mathf.Atan2(strikeDir.y, strikeDir.x) * Mathf.Rad2Deg;
                    slashObj.transform.rotation = Quaternion.Euler(0, 0, angle);
                    slashObj.transform.localScale = Vector3.one * 1.3f;
                    SpriteRenderer sr = slashObj.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 26;
                    sr.color = new Color(0.7f, 0.4f, 1f, 0.9f); // Purple phantom glow
                    AnimatedVFX anim = slashObj.AddComponent<AnimatedVFX>();
                    anim.Initialize(slashVfxFrames, 50f);
                }

                yield return new WaitForSeconds(0.04f);
            }
        }

        protected override void OnLevelUpStats() { }
        public override string GetNextLevelDescription() => "Mythic Evolved Weapon (MAX)!";
    }

    // ==========================================
    // 4. REAPER'S HARVEST (Dark Scythe + Iron Shield)
    // ==========================================
    public class ReapersHarvest : WeaponBase
    {
        [Header("Harvest Stats")]
        [SerializeField] private float lifestealPct = 0.04f; // 4% lifesteal
        [SerializeField] private float maxLifestealPerSec = 8.0f; // Max 8 HP/s
        [SerializeField] private float soulShieldMax = 30f;
        [SerializeField] private float soulShieldCooldown = 12f;
        [SerializeField] private Sprite[] slashVfxFrames;

        private float lifestealWindowTimer = 0f;
        private float lifestealThisSec = 0f;
        private float shieldCurrent = 30f;
        private float shieldTimer = 0f;

        protected override void Awake()
        {
            weaponId = "weapon_reapers_harvest";
            weaponName = "Reaper's Harvest";
            baseDamage = 90f;
            baseCooldown = 0.85f;
            attackRange = 3.5f;
            knockbackForce = 4.5f;
            isEvolved = true;
            currentLevel = 8;
            maxLevel = 8;
            base.Awake();
        }

        public void Configure(Sprite iconSpr, Sprite[] frames)
        {
            icon = iconSpr;
            slashVfxFrames = frames;
        }

        protected override void Update()
        {
            base.Update();

            // Lifesteal second window
            lifestealWindowTimer += Time.deltaTime;
            if (lifestealWindowTimer >= 1.0f)
            {
                lifestealWindowTimer = 0f;
                lifestealThisSec = 0f;
            }

            // Soul Shield recharge
            if (shieldCurrent < soulShieldMax)
            {
                shieldTimer += Time.deltaTime;
                if (shieldTimer >= soulShieldCooldown)
                {
                    shieldCurrent = soulShieldMax;
                    shieldTimer = 0f;
                    DamageNumber.SpawnText(transform.position + Vector3.up * 0.5f, "🛡️ SOUL SHIELD (30)", Color.cyan);
                }
            }
        }

        protected override void ExecuteAttack()
        {
            if (player == null) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, attackRange, enemyLayer);
            var (finalDmg, isCrit) = GetDamageAndCrit(0f, false);
            float totalDamageDealt = 0f;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == player.gameObject) continue;
                EnemyHealth eh = hits[i].GetComponent<EnemyHealth>();
                if (eh != null && eh.IsAlive)
                {
                    Vector2 kbDir = ((Vector2)eh.transform.position - (Vector2)player.transform.position).normalized;
                    eh.TakeDamage(finalDmg, kbDir, knockbackForce, isCrit, weaponId);
                    totalDamageDealt += finalDmg;
                }
            }

            // Lifesteal 4%, max 8 HP/s
            if (totalDamageDealt > 0f)
            {
                float potentialHeal = totalDamageDealt * lifestealPct;
                float allowedHeal = Mathf.Min(potentialHeal, maxLifestealPerSec - lifestealThisSec);
                if (allowedHeal > 0f)
                {
                    PlayerHealth ph = player.GetComponent<PlayerHealth>();
                    if (ph != null) ph.Heal(allowedHeal);
                    lifestealThisSec += allowedHeal;
                }
            }

            // VFX
            if (slashVfxFrames != null && slashVfxFrames.Length > 0)
            {
                GameObject vfxObj = new GameObject("ReaperSweep");
                vfxObj.transform.position = player.transform.position;
                vfxObj.transform.localScale = Vector3.one * 2.4f;
                SpriteRenderer sr = vfxObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 26;
                sr.color = new Color(0.2f, 1f, 0.4f, 0.85f); // Toxic ghost green
                AnimatedVFX anim = vfxObj.AddComponent<AnimatedVFX>();
                anim.Initialize(slashVfxFrames, 28f);
            }
        }

        protected override void OnLevelUpStats() { }
        public override string GetNextLevelDescription() => "Mythic Evolved Weapon (MAX)!";
    }

    // ==========================================
    // 5. OBLIVION DEATH RAY (Crystal Laser + Chronos Clock)
    // ==========================================
    public class OblivionDeathRay : WeaponBase
    {
        [Header("Death Ray Stats")]
        [SerializeField] private float beamLength = 7.5f;
        [SerializeField] private float beamThickness = 0.8f;
        [SerializeField] private float baseRotationSpeed = 90f; // 90°/s
        [SerializeField] private float bossRotationSpeed = 30f; // Slows to 30°/s on Boss
        [SerializeField] private Sprite[] laserFrames;

        private float currentAngle = 0f;
        private float bossSlowTimer = 0f;
        private float tickTimer = 0f;
        private const float TICK_INTERVAL = 0.20f;
        private const float DAMAGE_PER_TICK = 22f; // 110 DPS
        private const int MAX_TARGETS_PER_TICK = 15;

        protected override void Awake()
        {
            weaponId = "weapon_oblivion_death_ray";
            weaponName = "Oblivion Death Ray";
            baseDamage = DAMAGE_PER_TICK;
            baseCooldown = 0.1f;
            attackRange = 7.5f;
            requireTargetInRange = false;
            isEvolved = true;
            currentLevel = 8;
            maxLevel = 8;
            base.Awake();
        }

        public void Configure(Sprite iconSpr, Sprite[] frames)
        {
            icon = iconSpr;
            laserFrames = frames;
        }

        protected override void Update()
        {
            base.Update();
            if (player == null) return;

            // Rotate beams continuously
            float currentRotSpeed = (bossSlowTimer > 0f) ? bossRotationSpeed : baseRotationSpeed;
            if (bossSlowTimer > 0f) bossSlowTimer -= Time.deltaTime;

            currentAngle = (currentAngle + currentRotSpeed * Time.deltaTime) % 360f;

            tickTimer += Time.deltaTime;
            if (tickTimer >= TICK_INTERVAL)
            {
                tickTimer = 0f;
                ExecuteRayTick();
            }
        }

        private void ExecuteRayTick()
        {
            // Two dual opposite beams (0° and 180°)
            Vector2 dir1 = Quaternion.Euler(0, 0, currentAngle) * Vector2.right;
            Vector2 dir2 = -dir1;

            SweepBeam(dir1);
            SweepBeam(dir2);
        }

        private void SweepBeam(Vector2 dir)
        {
            Vector3 startPos = player.transform.position + (Vector3)(dir * 0.4f);
            RaycastHit2D[] hits = Physics2D.CircleCastAll(startPos, beamThickness * 0.5f, dir, beamLength, enemyLayer);
            int targetsCount = 0;

            for (int i = 0; i < hits.Length && targetsCount < MAX_TARGETS_PER_TICK; i++)
            {
                if (hits[i].collider.gameObject == player.gameObject) continue;
                EnemyHealth eh = hits[i].collider.GetComponent<EnemyHealth>();
                if (eh != null && eh.IsAlive)
                {
                    bool isBoss = eh.name.Contains("Boss") || eh.name.Contains("Warden") || eh.name.Contains("Golem");
                    if (isBoss)
                    {
                        bossSlowTimer = 0.6f; // Hãm xoay còn 30°/s trong 0.6s
                    }

                    eh.TakeDamage(DAMAGE_PER_TICK, dir, 0.5f, false, weaponId);
                    targetsCount++;
                }
            }
        }

        protected override void ExecuteAttack() { }
        protected override void OnLevelUpStats() { }
        public override string GetNextLevelDescription() => "Mythic Evolved Weapon (MAX)!";
    }

    // ==========================================
    // 6. GATLING REPEATER (Crossbow + Emerald of Wisdom)
    // ==========================================
    public class GatlingRepeater : WeaponBase
    {
        [Header("Gatling Settings")]
        [SerializeField] private int boltCount = 5;
        [SerializeField] private float boltDamage = 13f;
        [SerializeField] private Sprite[] boltFrames;

        protected override void Awake()
        {
            weaponId = "weapon_gatling_repeater";
            weaponName = "Gatling Repeater";
            baseDamage = boltDamage;
            baseCooldown = 0.50f;
            attackRange = 8.5f;
            knockbackForce = 2.0f;
            isEvolved = true;
            currentLevel = 8;
            maxLevel = 8;
            base.Awake();
        }

        public void Configure(Sprite iconSpr, Sprite[] frames)
        {
            icon = iconSpr;
            boltFrames = frames;
        }

        protected override void ExecuteAttack()
        {
            Transform target = FindTarget(attackRange, true);
            Vector2 baseDir = target != null 
                ? ((Vector2)target.position - (Vector2)player.transform.position).normalized 
                : player.FacingDirection;

            // Fan of 5 bolts across 20° total spread
            for (int i = 0; i < boltCount; i++)
            {
                float angle = -10f + (i * 5f);
                Vector2 fireDir = Quaternion.Euler(0, 0, angle) * baseDir;
                SpawnGatlingBolt(fireDir);
            }
        }

        private void SpawnGatlingBolt(Vector2 dir)
        {
            if (player == null) return;

            GameObject boltObj = new GameObject("GatlingBolt");
            boltObj.transform.position = player.transform.position + (Vector3)(dir * 0.5f);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            boltObj.transform.rotation = Quaternion.Euler(0, 0, angle);

            EnergyOrbProjectile proj = boltObj.AddComponent<EnergyOrbProjectile>();
            // Pierce 4 with -15% falloff per pierce, 1.0m AoE explosion (40% dmg)
            proj.Initialize(
                dir,
                boltDamage,
                4,
                enemyLayer,
                player.gameObject,
                14.0f,
                boltFrames,
                0.15f,
                true, 1.0f, 0.40f,
                weaponId,
                false
            );
        }

        protected override void OnLevelUpStats() { }
        public override string GetNextLevelDescription() => "Mythic Evolved Weapon (MAX)!";
    }
}
