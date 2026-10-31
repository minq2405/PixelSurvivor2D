using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Attaches to Player to continuously detect nearby PickupItems within magnetic radius
    /// and pull them smoothly toward the Player.
    /// In v2.1: Driven by PlayerStats.EffectiveMagnetRadius (Steve 1.7m, Alex 1.7m, Villager 2.2m).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCollector : MonoBehaviour
    {
        [Header("Magnet Radius Settings")]
        [SerializeField] private float baseRadius = 1.7f;
        [SerializeField] private float radiusMultiplier = 1.0f;
        [SerializeField] private LayerMask pickupLayer;

        private PlayerController player;

        public float BaseRadius => baseRadius;
        public float RadiusMultiplier => radiusMultiplier;

        public float EffectiveRadius
        {
            get
            {
                if (Combat.PlayerStats.Instance != null)
                {
                    return Combat.PlayerStats.Instance.EffectiveMagnetRadius;
                }
                float characterBonus = (player != null && player.characterId == "Villager") ? 1.3f : 1.0f;
                return baseRadius * radiusMultiplier * characterBonus;
            }
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            if (pickupLayer == 0)
            {
                pickupLayer = ~LayerMask.GetMask("UI");
            }
        }

        private void Update()
        {
            float radius = EffectiveRadius;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius, pickupLayer);
            for (int i = 0; i < colliders.Length; i++)
            {
                PickupItem item = colliders[i].GetComponent<PickupItem>();
                if (item != null && item.CanBeMagnetized)
                {
                    item.AttractTo(transform);
                }
            }
        }

        public void ApplyRadiusBonus(float bonus)
        {
            radiusMultiplier += bonus;
            if (Combat.PlayerStats.Instance != null)
            {
                Combat.PlayerStats.Instance.AddBonusMagnet(bonus);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, EffectiveRadius);
        }
    }
}
