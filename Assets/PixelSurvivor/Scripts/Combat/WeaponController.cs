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

        private PlayerController player;
        private PlayerHealth playerHealth;
        private PlayerCollector playerCollector;

        public float DamageMultiplier => damageMultiplier;
        public float CooldownMultiplier => cooldownMultiplier;
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

        public void ApplyBuff(string buffType, float percentage)
        {
            switch (buffType.ToLower())
            {
                case "damage":
                    damageMultiplier += percentage;
                    Debug.Log($"<color=#10B981>[Buff]</color> Global Damage Multiplier: {damageMultiplier:F2}x");
                    break;
                case "cooldown":
                    cooldownMultiplier = Mathf.Max(0.2f, cooldownMultiplier - percentage);
                    Debug.Log($"<color=#10B981>[Buff]</color> Global Cooldown Multiplier: {cooldownMultiplier:F2}x");
                    break;
                case "area":
                    areaMultiplier += percentage;
                    Debug.Log($"<color=#10B981>[Buff]</color> Global Area Multiplier: {areaMultiplier:F2}x");
                    break;
                case "speed":
                    if (player != null) player.ApplySpeedBuff(percentage);
                    break;
                case "health":
                    if (playerHealth != null) playerHealth.IncreaseMaxHealth(percentage, true);
                    break;
                case "magnet":
                    if (playerCollector != null) playerCollector.ApplyRadiusBonus(percentage);
                    break;
            }
        }
    }
}
