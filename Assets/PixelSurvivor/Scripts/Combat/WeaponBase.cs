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
                    currentCooldownTimer = Mathf.Max(0.1f, baseCooldown * cooldownMod);
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

        protected virtual void OnLevelUpStats()
        {
            baseDamage *= 1.25f; // default +25% dmg per level
        }

        protected Transform FindClosestEnemy(float radius)
        {
            Vector3 center = player != null ? player.transform.position : transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayer);
            Transform closest = null;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                // CRUCIAL: Exclude player and any child of player
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
                    continue;
                }

                // Fallback IDamageable check (excluding player health)
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

            return closest;
        }

        public float GetTotalDamage()
        {
            float mod = weaponController != null ? weaponController.DamageMultiplier : 1f;
            return baseDamage * mod;
        }
    }
}
