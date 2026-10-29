using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Specialized Evoker AI (Elite Summoner):
    /// - 150 HP, 1.8 m/s move speed, 10 contact damage.
    /// - NO FANGS (as confirmed by user - Witch already casts spikes/fangs).
    /// - Advances like a normal mob chasing the player.
    /// - Periodically (every ~6s) stops, activates attack animation (evoker_attack, 19 frames)
    ///   with a golden summon circle (vfx_evoker_magic_circle), and summons 2-3 Vex minions
    ///   to swarm the player.
    /// </summary>
    public class EvokerAI : EnemyAI
    {
        [Header("Evoker Summoning Settings")]
        [SerializeField] private float summonCooldown = 6.0f;
        [SerializeField] private int minVexToSummon = 2;
        [SerializeField] private int maxVexToSummon = 3;
        [SerializeField] private GameObject vexPrefab;
        [SerializeField] private float summonAnimationFps = 10f; // 19 frames @ 10 fps = 1.9s
        [SerializeField] private Color summonCircleColor = new Color(0.98f, 0.75f, 0.14f, 1.0f); // Golden yellow

        [Header("Evoker Animations & VFX")]
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] magicCircleFrames;
        [SerializeField] private Sprite[] summonSmokeFrames;

        private float cooldownTimer = 3.0f; // Initial delay before first summon
        private bool isSummoning = false;
        private float summonAnimTimer = 0f;
        private int summonFrameIdx = 0;
        private bool hasSpawnedVexThisCast = false;
        private WaveSpawner waveSpawner;

        public bool IsSummoning => isSummoning;

        protected override void Start()
        {
            base.Start();

            waveSpawner = Object.FindAnyObjectByType<WaveSpawner>();

            if (attackFrames == null || attackFrames.Length == 0)
            {
                attackFrames = Resources.LoadAll<Sprite>("Enemies/Evoker/evoker_attack");
            }
            if (magicCircleFrames == null || magicCircleFrames.Length == 0)
            {
                magicCircleFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_evoker_magic_circle");
            }
            if (summonSmokeFrames == null || summonSmokeFrames.Length == 0)
            {
                summonSmokeFrames = Resources.LoadAll<Sprite>("Particles/VFX/vfx_smoke_puff");
            }

            // Auto-load Vex prefab if not assigned
            if (vexPrefab == null)
            {
                vexPrefab = Resources.Load<GameObject>("Enemies/Prefabs/Enemy_Vex");
                #if UNITY_EDITOR
                if (vexPrefab == null)
                {
                    vexPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelSurvivor/Enemies/Prefabs/Enemy_Vex.prefab");
                }
                #endif
            }
        }

        protected override void Update()
        {
            if (health != null && !health.IsAlive) return;

            cooldownTimer -= Time.deltaTime;

            if (isSummoning)
            {
                UpdateSummonAnimation();
                return;
            }

            // Normal walk animation
            base.Update();

            // Check if ready to summon Vex
            if (cooldownTimer <= 0f && playerTransform != null)
            {
                StartSummoningRitual();
            }
        }

        protected override void FixedUpdate()
        {
            if (health != null && !health.IsAlive)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (isSummoning)
            {
                // Stand ground during ritual
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Standard mob chase movement
            base.FixedUpdate();
        }

        private void StartSummoningRitual()
        {
            isSummoning = true;
            summonAnimTimer = 0f;
            summonFrameIdx = 0;
            hasSpawnedVexThisCast = false;
            rb.linearVelocity = Vector2.zero;

            Debug.Log("<color=#F59E0B>[Evoker]</color> Channeling summon ritual... Vex incoming!");

            // Spawn Golden Magic Circle under Evoker's feet
            if (magicCircleFrames != null && magicCircleFrames.Length > 0)
            {
                GameObject circleObj = new GameObject("Evoker_SummonCircle");
                circleObj.transform.position = transform.position + new Vector3(0f, -0.2f, 0f);
                circleObj.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

                SpriteRenderer sr = circleObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 3; // On ground beneath mob
                sr.color = summonCircleColor;

                AnimatedVFX vfx = circleObj.AddComponent<AnimatedVFX>();
                vfx.Initialize(magicCircleFrames, 14f);
            }
        }

        private void UpdateSummonAnimation()
        {
            if (attackFrames == null || attackFrames.Length == 0)
            {
                EndSummoning();
                return;
            }

            summonAnimTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(summonAnimationFps, 1f);

            if (summonAnimTimer >= frameDuration)
            {
                summonAnimTimer -= frameDuration;
                summonFrameIdx++;

                // Trigger Vex spawn mid-animation (frame 10 / halfway)
                if (!hasSpawnedVexThisCast && summonFrameIdx >= attackFrames.Length / 2)
                {
                    hasSpawnedVexThisCast = true;
                    ExecuteVexSpawn();
                }

                if (summonFrameIdx >= attackFrames.Length)
                {
                    EndSummoning();
                    return;
                }

                if (spriteRenderer != null && summonFrameIdx < attackFrames.Length)
                {
                    spriteRenderer.sprite = attackFrames[summonFrameIdx];
                }
            }
        }

        private void ExecuteVexSpawn()
        {
            if (vexPrefab == null)
            {
                #if UNITY_EDITOR
                vexPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelSurvivor/Enemies/Prefabs/Enemy_Vex.prefab");
                #endif
                if (vexPrefab == null)
                {
                    Debug.LogWarning("[Evoker] Cannot summon Vex: vexPrefab is null!");
                    return;
                }
            }

            int count = Random.Range(minVexToSummon, maxVexToSummon + 1);
            Debug.Log($"<color=#F59E0B>[Evoker]</color> Summoning {count} Vex minions!");

            for (int i = 0; i < count; i++)
            {
                float angle = (i * (360f / count) + Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
                float dist = Random.Range(1.1f, 1.7f);
                Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);

                GameObject vex = Instantiate(vexPrefab, spawnPos, Quaternion.identity);
                vex.name = $"Summoned_Vex_{Time.time:F1}_{i}";
                vex.SetActive(true);

                // Spawn smoke puff at vex spawn position
                if (summonSmokeFrames != null && summonSmokeFrames.Length > 0)
                {
                    GameObject puff = new GameObject("Vex_SpawnPuff");
                    puff.transform.position = spawnPos;
                    puff.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
                    SpriteRenderer sr = puff.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 16;
                    AnimatedVFX vfx = puff.AddComponent<AnimatedVFX>();
                    vfx.Initialize(summonSmokeFrames, 24f);
                }

                // Register with WaveSpawner if available
                if (waveSpawner != null)
                {
                    waveSpawner.RegisterEnemy(vex);
                }
            }
        }

        private void EndSummoning()
        {
            isSummoning = false;
            summonAnimTimer = 0f;
            summonFrameIdx = 0;
            cooldownTimer = summonCooldown;

            if (walkFrames != null && walkFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = walkFrames[0];
            }
        }

        public void ConfigureEvoker(Sprite[] walk, Sprite[] attack, GameObject vexTemplateRef, Sprite[] circleFrames, Sprite[] smoke)
        {
            walkFrames = walk;
            attackFrames = attack;
            vexPrefab = vexTemplateRef;
            magicCircleFrames = circleFrames;
            summonSmokeFrames = smoke;
        }
    }
}
