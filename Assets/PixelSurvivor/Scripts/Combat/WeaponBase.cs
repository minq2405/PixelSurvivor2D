using UnityEngine;

namespace PixelSurvivor.Combat
{
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Weapon Identity")]
        public string weaponId = "weapon_id";
        public string weaponName = "Weapon Name";
        public Sprite icon;
        public int currentLevel = 1;
        public int maxLevel = 8;
        public bool isEvolved = false;

        [Header("Description & Synergy")]
        [TextArea(2, 3)]
        [SerializeField] protected string weaponDescription = "";
        [SerializeField] protected string weaponSynergy = "";

        [Header("Base Stats")]
        [SerializeField] protected float baseDamage = 25f;
        [SerializeField] protected float baseCooldown = 1.3f;
        [SerializeField] protected float attackRange = 3.5f;
        [SerializeField] protected float knockbackForce = 4f;

        [Header("Targeting & Layers")]
        [SerializeField] protected LayerMask enemyLayer;

        [Tooltip("Nếu bật: Chỉ tấn công khi có quái trong tầm đánh (attackRange). Nếu tắt: Tự động chém liên tục.")]
        [SerializeField] protected bool requireTargetInRange = true;

        protected float currentCooldownTimer = 0f;
        protected PlayerController player;
        protected WeaponController weaponController;

        public float BaseDamage => baseDamage;
        public float BaseCooldown => baseCooldown;
        public float AttackRange => attackRange;
        public float KnockbackForce => knockbackForce;
        public string WeaponDescription => weaponDescription;
        public string WeaponSynergy => weaponSynergy;

        protected virtual void Awake()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (weaponController == null) weaponController = GetComponentInParent<WeaponController>();
            if (currentCooldownTimer <= 0f) currentCooldownTimer = Random.Range(0.1f, 0.3f);
        }

        public virtual void Initialize(PlayerController playerRef, WeaponController controllerRef)
        {
            player = playerRef;
            weaponController = controllerRef;
            currentCooldownTimer = Random.Range(0.1f, 0.4f); // slight random start stagger
        }

        protected virtual void Update()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (weaponController == null) weaponController = GetComponentInParent<WeaponController>();
            if (player == null) return;

            currentCooldownTimer -= Time.deltaTime;
            if (currentCooldownTimer <= 0f)
            {
                if (CanAttack())
                {
                    ExecuteAttack();
                    float cooldownMod = weaponController != null ? weaponController.CooldownMultiplier : 1f;
                    float finalCd = baseCooldown * cooldownMod;
                    // v2.1: Absolute cooldown floor for Twin Daggers is 0.25s
                    if (weaponId == "weapon_twin_daggers") finalCd = Mathf.Max(0.25f, finalCd);
                    currentCooldownTimer = Mathf.Max(0.1f, finalCd);
                }
                else
                {
                    currentCooldownTimer = 0f; // Ready to strike instantly as soon as enemy enters range
                }
            }
        }

        protected virtual bool CanAttack()
        {
            if (requireTargetInRange)
            {
                Transform target = FindClosestEnemy(attackRange);
                return target != null;
            }
            return true;
        }

        protected abstract void ExecuteAttack();

        public virtual void LevelUp()
        {
            if (currentLevel < maxLevel)
            {
                currentLevel++;
                OnLevelUpStats();
                Debug.Log($"<color=#FACC15>[Level Up]</color> {weaponName} upgraded to Level {currentLevel}!");
            }
        }

        protected abstract void OnLevelUpStats();

        public virtual string GetBaseDescription()
        {
            return string.IsNullOrEmpty(weaponDescription) 
                ? $"Deals {baseDamage:F0} damage every {baseCooldown:F1}s." 
                : weaponDescription;
        }

        public abstract string GetNextLevelDescription();

        protected Transform FindClosestEnemy(float radius)
        {
            return FindTarget(radius, false);
        }

        protected Transform FindTarget(float radius, bool prioritizeBossAndElite = false)
        {
            Vector3 center = player != null ? player.transform.position : transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayer);
            Transform bestBoss = null;
            float minBossDistSqr = float.MaxValue;
            Transform bestElite = null;
            float minEliteDistSqr = float.MaxValue;
            Transform closest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                // Exclude player and any child of player
                if (player != null && (hits[i].gameObject == player.gameObject || hits[i].transform.IsChildOf(player.transform)))
                {
                    continue;
                }

                // Dedicated EnemyHealth check
                EnemyHealth eh = hits[i].GetComponent<EnemyHealth>();
                if (eh == null && hits[i].transform.parent != null)
                {
                    eh = hits[i].transform.parent.GetComponent<EnemyHealth>();
                }

                if (eh != null && eh.IsAlive)
                {
                    float distSqr = (hits[i].transform.position - center).sqrMagnitude;
                    if (distSqr < minDistSqr)
                    {
                        minDistSqr = distSqr;
                        closest = hits[i].transform;
                    }

                    if (prioritizeBossAndElite)
                    {
                        string mobName = hits[i].gameObject.name;
                        bool isBoss = mobName.Contains("Boss") || mobName.Contains("Warden") || mobName.Contains("Golem");
                        if (isBoss && distSqr < minBossDistSqr)
                        {
                            minBossDistSqr = distSqr;
                            bestBoss = hits[i].transform;
                        }

                        bool isElite = hits[i].GetComponent<EliteEnemyAffixController>() != null || mobName.Contains("Elite");
                        if (isElite && distSqr < minEliteDistSqr)
                        {
                            minEliteDistSqr = distSqr;
                            bestElite = hits[i].transform;
                        }
                    }
                    continue;
                }

                // Fallback IDamageable check
                IDamageable d = hits[i].GetComponent<IDamageable>();
                if (d != null && d.IsAlive && !(d is PlayerHealth))
                {
                    float distSqr = (hits[i].transform.position - center).sqrMagnitude;
                    if (distSqr < minDistSqr)
                    {
                        minDistSqr = distSqr;
                        closest = hits[i].transform;
                    }
                }
            }

            if (prioritizeBossAndElite)
            {
                if (bestBoss != null) return bestBoss;
                if (bestElite != null) return bestElite;
            }

            return closest;
        }

        public (float damage, bool isCrit) GetDamageAndCrit(float weaponCritBonus = 0f, bool isBossOrElite = false)
        {
            if (PlayerStats.Instance != null)
            {
                return PlayerStats.Instance.CalculateWeaponDamage(baseDamage, weaponCritBonus, isBossOrElite);
            }
            float mod = weaponController != null ? weaponController.DamageMultiplier : 1f;
            return (baseDamage * mod, false);
        }

        public float GetTotalDamage()
        {
            float mod = weaponController != null ? weaponController.DamageMultiplier : 1f;
            return baseDamage * mod;
        }
    }
}
