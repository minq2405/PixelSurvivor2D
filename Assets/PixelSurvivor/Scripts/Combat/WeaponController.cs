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

        public float DamageMultiplier => damageMultiplier;
        public float CooldownMultiplier => cooldownMultiplier;
        public float AreaMultiplier => areaMultiplier;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public IReadOnlyList<WeaponBase> EquippedWeapons => equippedWeapons;

        private void Awake()
        {
            player = GetComponent<PlayerController>();

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

        public bool AddWeapon(WeaponBase weapon)
        {
            if (equippedWeapons.Count >= MaxWeapons)
            {
                Debug.LogWarning("[WeaponController] Weapon slots are full (Max 4)!");
                return false;
            }

            if (!equippedWeapons.Contains(weapon))
            {
                equippedWeapons.Add(weapon);
                weapon.transform.SetParent(transform);
                weapon.transform.localPosition = Vector3.zero;
                weapon.Initialize(player, this);
                Debug.Log($"<color=#10B981>[Weapon Equipped]</color> {weapon.weaponName} equipped to slot {equippedWeapons.Count}!");
                return true;
            }

            return false;
        }

        public void UpgradeWeapon(string weaponId)
        {
            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                if (equippedWeapons[i].weaponId == weaponId)
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
                    Debug.Log($"<color=#10B981>[Buff]</color> Damage Multiplier: {damageMultiplier:F2}x");
                    break;
                case "cooldown":
                    cooldownMultiplier = Mathf.Max(0.2f, cooldownMultiplier - percentage);
                    Debug.Log($"<color=#10B981>[Buff]</color> Cooldown Multiplier: {cooldownMultiplier:F2}x");
                    break;
                case "area":
                    areaMultiplier += percentage;
                    Debug.Log($"<color=#10B981>[Buff]</color> Area Multiplier: {areaMultiplier:F2}x");
                    break;
                case "speed":
                    projectileSpeedMultiplier += percentage;
                    break;
            }
        }
    }
}
