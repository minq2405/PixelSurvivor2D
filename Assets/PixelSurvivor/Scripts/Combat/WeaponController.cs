using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    [RequireComponent(typeof(PlayerController))]
    public class WeaponController : MonoBehaviour
    {
        [Header("Global Modifiers")]
        [SerializeField] private float damageMultiplier = 1.0f;
        [SerializeField] private float cooldownMultiplier = 1.0f;
        [SerializeField] private float areaMultiplier = 1.0f;
        [SerializeField] private float projectileSpeedMultiplier = 1.0f;

        [Header("Active Weapons (Max 4)")]
        [SerializeField] private List<WeaponBase> equippedWeapons = new List<WeaponBase>();
        public const int MaxWeapons = 4;
        public const int MaxEvolvedWeapons = 2;

        [Header("Evolution State")]
        [SerializeField] private int evolvedWeaponCount = 0;
        public int EvolvedWeaponCount => evolvedWeaponCount;
        public bool CanEvolveMore => evolvedWeaponCount < MaxEvolvedWeapons;

        private PlayerController player;
        private PlayerHealth playerHealth;
        private PlayerCollector playerCollector;

        public float DamageMultiplier => PlayerStats.Instance != null ? PlayerStats.Instance.DamageMultiplier : damageMultiplier;
        public float CooldownMultiplier => PlayerStats.Instance != null ? PlayerStats.Instance.CooldownMultiplier : cooldownMultiplier;
        public float AreaMultiplier => areaMultiplier;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public IReadOnlyList<WeaponBase> EquippedWeapons => equippedWeapons;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            playerHealth = GetComponent<PlayerHealth>();
            playerCollector = GetComponent<PlayerCollector>();

            // Initialize any weapons already placed on player or as children
            WeaponBase[] existing = GetComponentsInChildren<WeaponBase>();
            for (int i = 0; i < existing.Length; i++)
            {
                if (!equippedWeapons.Contains(existing[i]))
                {
                    equippedWeapons.Add(existing[i]);
                }
                existing[i].Initialize(player, this);
            }
        }

        public void ClearAllWeapons()
        {
            for (int i = equippedWeapons.Count - 1; i >= 0; i--)
            {
                if (equippedWeapons[i] != null)
                {
                    Destroy(equippedWeapons[i].gameObject);
                }
            }
            equippedWeapons.Clear();
        }

        public WeaponBase EquipNewWeaponFromPrefab(GameObject weaponPrefab)
        {
            if (equippedWeapons.Count >= MaxWeapons)
            {
                Debug.LogWarning("[WeaponController] Weapon slots are full (Max 4)!");
                return null;
            }

            if (weaponPrefab == null)
            {
                Debug.LogWarning("[WeaponController] Cannot equip null weapon prefab!");
                return null;
            }

            // Check if already equipped
            WeaponBase prefabWb = weaponPrefab.GetComponent<WeaponBase>();
            if (prefabWb != null && HasWeapon(prefabWb.weaponId))
            {
                Debug.LogWarning($"[WeaponController] Already equipped {prefabWb.weaponName}!");
                return null;
            }

            GameObject weaponObj = Instantiate(weaponPrefab, transform);
            weaponObj.name = weaponPrefab.name;
            weaponObj.transform.localPosition = Vector3.zero;

            WeaponBase wb = weaponObj.GetComponent<WeaponBase>();
            if (wb != null)
            {
                equippedWeapons.Add(wb);
                wb.Initialize(player, this);
                Debug.Log($"<color=#10B981>[WeaponController]</color> Equipped new weapon: {wb.weaponName} (Slot {equippedWeapons.Count}/{MaxWeapons})!");
                return wb;
            }

            return null;
        }

        public void AddWeapon(WeaponBase weapon)
        {
            if (weapon == null) return;
            if (!equippedWeapons.Contains(weapon))
            {
                if (equippedWeapons.Count >= MaxWeapons)
                {
                    Debug.LogWarning("[WeaponController] Weapon slots are full (Max 4)!");
                    return;
                }
                equippedWeapons.Add(weapon);
                weapon.Initialize(player, this);
            }
        }

        public void SetStartingWeapon(GameObject weaponPrefab)
        {
            ClearAllWeapons();
            EquipNewWeaponFromPrefab(weaponPrefab);
        }

        public bool HasWeapon(string weaponId)
        {
            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                if (equippedWeapons[i] != null && equippedWeapons[i].weaponId == weaponId)
                {
                    return true;
                }
            }
            return false;
        }

        public WeaponBase GetWeapon(string weaponId)
        {
            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                if (equippedWeapons[i] != null && equippedWeapons[i].weaponId == weaponId)
                {
                    return equippedWeapons[i];
                }
            }
            return null;
        }

        public void UpgradeWeapon(string weaponId)
        {
            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                if (equippedWeapons[i] != null && equippedWeapons[i].weaponId == weaponId)
                {
                    equippedWeapons[i].LevelUp();
                    return;
                }
            }
        }

        public bool EvolveWeapon(string baseWeaponId)
        {
            if (evolvedWeaponCount >= MaxEvolvedWeapons)
            {
                Debug.LogWarning("[WeaponController] Cannot evolve: max evolved weapons limit (2) reached!");
                return false;
            }

            WeaponBase baseWb = GetWeapon(baseWeaponId);
            if (baseWb == null)
            {
                Debug.LogWarning($"[WeaponController] Cannot evolve {baseWeaponId}: weapon not equipped!");
                return false;
            }

            int slotIndex = equippedWeapons.IndexOf(baseWb);
            Sprite icon = baseWb.icon;
            GameObject weaponGo = baseWb.gameObject;

            WeaponBase evolvedWb = null;

            switch (baseWeaponId)
            {
                case "weapon_magma_broadsword":
                case "weapon_broadsword":
                {
                    var old = baseWb as MeleeSlashWeapon;
                    Sprite[] vfx = old != null ? old.SlashVfxFrames : null;
                    Destroy(baseWb);
                    var evolved = weaponGo.AddComponent<SolarFlareGreatsword>();
                    evolved.Configure(icon, vfx);
                    evolvedWb = evolved;
                    break;
                }
                case "weapon_battle_axe":
                {
                    var old = baseWb as MeleeSlashWeapon;
                    Sprite[] vfx = old != null ? old.SlashVfxFrames : null;
                    Destroy(baseWb);
                    var evolved = weaponGo.AddComponent<ExecutionerGreataxe>();
                    evolved.Configure(icon, vfx);
                    evolvedWb = evolved;
                    break;
                }
                case "weapon_twin_daggers":
                {
                    var old = baseWb as MeleeSlashWeapon;
                    Sprite[] vfx = old != null ? old.SlashVfxFrames : null;
                    Destroy(baseWb);
                    var evolved = weaponGo.AddComponent<ShadowPhantomDaggers>();
                    evolved.Configure(icon, vfx);
                    evolvedWb = evolved;
                    break;
                }
                case "weapon_dark_scythe":
                {
                    var old = baseWb as MeleeSlashWeapon;
                    Sprite[] vfx = old != null ? old.SlashVfxFrames : null;
                    Destroy(baseWb);
                    var evolved = weaponGo.AddComponent<ReapersHarvest>();
                    evolved.Configure(icon, vfx);
                    evolvedWb = evolved;
                    break;
                }
                case "weapon_crystal_laser":
                case "weapon_magic_staff":
                {
                    var old = baseWb as LaserBeamWeapon;
                    Sprite[] vfx = old != null ? old.LaserFrames : null;
                    Destroy(baseWb);
                    var evolved = weaponGo.AddComponent<OblivionDeathRay>();
                    evolved.Configure(icon, vfx);
                    evolvedWb = evolved;
                    break;
                }
                case "weapon_crossbow":
                case "weapon_repeating_crossbow":
                {
                    var old = baseWb as RangedProjectileWeapon;
                    Sprite[] vfx = old != null ? old.ProjectileFrames : null;
                    Destroy(baseWb);
                    var evolved = weaponGo.AddComponent<GatlingRepeater>();
                    evolved.Configure(icon, vfx);
                    evolvedWb = evolved;
                    break;
                }
                default:
                    Debug.LogWarning($"[WeaponController] Unknown weapon evolution for {baseWeaponId}!");
                    return false;
            }

            if (evolvedWb != null)
            {
                equippedWeapons[slotIndex] = evolvedWb;
                evolvedWb.Initialize(player, this);
                evolvedWeaponCount++;
                Debug.Log($"<color=#F59E0B>[EVOLUTION SUCCESS]</color> {baseWeaponId} evolved into {evolvedWb.weaponName}! ({evolvedWeaponCount}/{MaxEvolvedWeapons})");
                DamageNumber.SpawnText(player.transform.position + Vector3.up * 1.2f, $"✨ {evolvedWb.weaponName} EVOLVED! ✨", Color.yellow);
                return true;
            }

            return false;
        }

        public void ApplyBuff(string buffType, float percentage)
        {
            switch (buffType.ToLower())
            {
                case "damage":
                    damageMultiplier += percentage;
                    if (PlayerStats.Instance != null) PlayerStats.Instance.AddBonusDamage(percentage);
                    Debug.Log($"<color=#10B981>[Buff]</color> Global Damage Multiplier: {DamageMultiplier:F2}x");
                    break;
                case "cooldown":
                    cooldownMultiplier = Mathf.Max(0.2f, cooldownMultiplier - percentage);
                    if (PlayerStats.Instance != null) PlayerStats.Instance.AddBonusCDR(percentage);
                    Debug.Log($"<color=#10B981>[Buff]</color> Global Cooldown Multiplier: {CooldownMultiplier:F2}x");
                    break;
                case "area":
                    areaMultiplier += percentage;
                    Debug.Log($"<color=#10B981>[Buff]</color> Global Area Multiplier: {areaMultiplier:F2}x");
                    break;
                case "speed":
                    if (player != null) player.ApplySpeedBuff(percentage);
                    break;
                case "health":
                    if (PlayerStats.Instance != null) PlayerStats.Instance.AddMaxHealth(percentage, true);
                    else if (playerHealth != null) playerHealth.IncreaseMaxHealth(percentage, true);
                    break;
                case "armor":
                    if (PlayerStats.Instance != null) PlayerStats.Instance.AddBonusArmor(percentage);
                    break;
                case "crit":
                    if (PlayerStats.Instance != null) PlayerStats.Instance.AddBonusCritChance(percentage);
                    break;
                case "regen":
                    if (PlayerStats.Instance != null) PlayerStats.Instance.AddBonusRegen(percentage);
                    break;
                case "magnet":
                    if (playerCollector != null) playerCollector.ApplyRadiusBonus(percentage);
                    break;
            }
        }
    }
}
