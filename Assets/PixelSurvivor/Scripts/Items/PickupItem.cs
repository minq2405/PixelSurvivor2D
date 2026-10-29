using System.Collections;
using UnityEngine;

namespace PixelSurvivor
{
    public enum PickupType
    {
        ExpSmall,   // 10 EXP
        ExpMedium,  // 50 EXP
        ExpLarge,   // 250 EXP
        Emerald,    // In-run Reroll currency
        Steak,      // Restores HP
        Magnet,     // Attracts all items on screen
        Tnt         // AoE explosion wiping enemies
    }

    /// <summary>
    /// Handles collectible in-game drops (EXP orbs, emeralds, steaks, magnets, TNT).
    /// Features:
    /// - Spawn pop physics arc
    /// - Vacuum magnet acceleration towards player
    /// - Instant pickup detection and dispatch to ExperienceManager
    /// </summary>
    public class PickupItem : MonoBehaviour
    {
        [Header("Item Properties")]
        [SerializeField] private PickupType itemType = PickupType.ExpSmall;
        [SerializeField] private int value = 10;
        [SerializeField] private float collectDistance = 0.5f;

        [Header("Magnet Acceleration Physics")]
        [SerializeField] private float initialSpeed = 3f;
        [SerializeField] private float acceleration = 24f;
        [SerializeField] private float maxSpeed = 18f;

        private Transform targetTransform;
        private bool isMagnetized = false;
        private float currentSpeed;
        private float spawnTime;
        private bool isCollected = false;

        public bool CanBeMagnetized => !isCollected;
        public PickupType ItemType => itemType;
        public int Value => value;

        private void Awake()
        {
            spawnTime = Time.time;
            currentSpeed = initialSpeed;
        }

        private void Start()
        {
            StartCoroutine(SpawnPopRoutine());
        }

        private IEnumerator SpawnPopRoutine()
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float popDist = Random.Range(0.35f, 0.85f);
            Vector3 targetPos = transform.position + (Vector3)(randomDir * popDist);
            float elapsed = 0f;
            float duration = 0.22f;

            Vector3 origin = transform.position;
            while (elapsed < duration && !isMagnetized)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float arc = Mathf.Sin(progress * Mathf.PI) * 0.25f;
                transform.position = Vector3.Lerp(origin, targetPos, progress) + Vector3.up * arc;
                yield return null;
            }
        }

        private void Update()
        {
            if (isCollected) return;

            if (isMagnetized && targetTransform != null)
            {
                currentSpeed = Mathf.Min(maxSpeed, currentSpeed + acceleration * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, targetTransform.position, currentSpeed * Time.deltaTime);

                float distSqr = (transform.position - targetTransform.position).sqrMagnitude;
                if (distSqr <= collectDistance * collectDistance)
                {
                    Collect();
                }
            }
            else
            {
                // Subtle gentle bobbing for visual flair
                float bobOffset = Mathf.Sin((Time.time - spawnTime) * 5f) * 0.002f;
                transform.position += new Vector3(0f, bobOffset, 0f);
            }
        }

        public void AttractTo(Transform playerTransform)
        {
            if (isCollected) return;
            isMagnetized = true;
            targetTransform = playerTransform;
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            switch (itemType)
            {
                case PickupType.ExpSmall:
                case PickupType.ExpMedium:
                case PickupType.ExpLarge:
                    if (ExperienceManager.Instance != null)
                    {
                        ExperienceManager.Instance.AddExp(value);
                    }
                    break;

                case PickupType.Emerald:
                    if (ExperienceManager.Instance != null)
                    {
                        ExperienceManager.Instance.AddEmeralds(value);
                    }
                    break;

                case PickupType.Steak:
                    var ph = FindAnyObjectByType<Combat.PlayerHealth>();
                    if (ph != null) ph.Heal(value);
                    break;

                case PickupType.Magnet:
                    if (ExperienceManager.Instance != null)
                    {
                        ExperienceManager.Instance.AttractAllPickups();
                    }
                    break;

                case PickupType.Tnt:
                    if (ExperienceManager.Instance != null)
                    {
                        ExperienceManager.Instance.DetonateTnt(transform.position);
                    }
                    break;
            }

            Destroy(gameObject);
        }

        public void Initialize(PickupType type, int val)
        {
            itemType = type;
            value = val;
        }
    }
}
