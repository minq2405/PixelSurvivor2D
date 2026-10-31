using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelSurvivor.UI;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Boss & Mini-Boss Arena Lockdown Controller:
    /// - 5-Second Flashing Countdown Warning on HUD Timer (Yellow for Mini-Boss, Red for Boss).
    /// - Viewport-aligned Arena Boundary: Camera locks tightly to current arena center.
    /// - 0.35s Punch Zoom impact (5.0 -> 4.4 -> 5.0) preserving 100% tactical 16:9 dodging space.
    /// - Outer Chunk Darkness Mask: 75% dark tint outside the locked camera viewport.
    /// - Glowing Energy Barrier Border: Amber Yellow (Mini-boss) or Crimson Red (Grand Boss).
    /// - Player Movement Confinement: Physical 2D colliders + LateUpdate clamp.
    /// - Mob Spawning Paused & regular mobs cleared for an uninterrupted duel.
    /// - Smooth arena unlock and camera resume upon boss defeat.
    /// </summary>
    public class BossArenaManager : MonoBehaviour
    {
        private static BossArenaManager _instance;
        public static BossArenaManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<BossArenaManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("BossArenaManager");
                        _instance = go.AddComponent<BossArenaManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("State")]
        [SerializeField] private bool isLockdownActive = false;
        [SerializeField] private bool isGrandBossArena = false;
        [SerializeField] private Vector2 arenaCenter;
        [SerializeField] private float arenaHalfWidth = 8.89f;
        [SerializeField] private float arenaHalfHeight = 5.0f;

        [Header("Colors")]
        public static readonly Color MiniBossYellow = new Color(0.98f, 0.80f, 0.08f, 1f); // #FACC15
        public static readonly Color BossCrimson = new Color(0.94f, 0.27f, 0.27f, 1f);    // #EF4444

        private readonly List<GameObject> activeBosses = new List<GameObject>();
        private Transform playerTransform;
        private const float PlayerRadius = 0.5f;

        private GameObject visualsContainer;
        private GameObject boundaryCollidersContainer;
        private LineRenderer barrierLine;
        private readonly List<SpriteRenderer> darknessRenderers = new List<SpriteRenderer>();
        private Coroutine warningCoroutine;
        private Coroutine unlockCoroutine;

        public bool IsLockdownActive => isLockdownActive;
        public Vector2 ArenaCenter => arenaCenter;
        public float ArenaHalfWidth => arenaHalfWidth;
        public float ArenaHalfHeight => arenaHalfHeight;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) playerTransform = player.transform;
            }
        }

        /// <summary>
        /// Trigger 5-Second Flashing HUD Timer Warning phase.
        /// </summary>
        public void TriggerWarning(bool isGrandBoss, float duration = 5f, Action onComplete = null)
        {
            if (warningCoroutine != null) StopCoroutine(warningCoroutine);
            warningCoroutine = StartCoroutine(WarningRoutine(isGrandBoss, duration, onComplete));
        }

        private IEnumerator WarningRoutine(bool isGrandBoss, float duration, Action onComplete)
        {
            Color warningColor = isGrandBoss ? BossCrimson : MiniBossYellow;
            string bossTypeStr = isGrandBoss ? "GRAND BOSS" : "MINI-BOSS";

            Debug.Log($"<color={(isGrandBoss ? "#EF4444" : "#FACC15")}>[ARENA WARNING]</color> ⚠️ {bossTypeStr} approaching in {duration:F0}s! Timer pulsing alert initiated.");

            if (InGameHUD.Instance != null)
            {
                InGameHUD.Instance.StartTimerWarning(warningColor, duration);
            }

            yield return new WaitForSeconds(duration);

            warningCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Locks down the arena around the player/camera viewport.
        /// </summary>
        public void StartArenaLockdown(bool isGrandBoss, GameObject bossInstance)
        {
            if (bossInstance != null && !activeBosses.Contains(bossInstance))
            {
                activeBosses.Add(bossInstance);
                var eh = bossInstance.GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    eh.OnDeath += () => OnBossDied(bossInstance);
                }
            }

            if (isLockdownActive)
            {
                Debug.Log($"<color=#38BDF8>[BossArenaManager]</color> Additional boss registered to active lockdown: {bossInstance?.name}");
                return;
            }

            if (unlockCoroutine != null)
            {
                StopCoroutine(unlockCoroutine);
                unlockCoroutine = null;
            }

            isLockdownActive = true;
            isGrandBossArena = isGrandBoss;
            FindPlayer();

            // 1. Establish Arena Center & Viewport Dimensions
            Camera cam = Camera.main;
            if (cam != null)
            {
                arenaCenter = playerTransform != null ? (Vector2)playerTransform.position : (Vector2)cam.transform.position;
                arenaHalfHeight = cam.orthographicSize;
                arenaHalfWidth = cam.orthographicSize * cam.aspect;
            }
            else
            {
                arenaCenter = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
                arenaHalfHeight = 5.0f;
                arenaHalfWidth = 8.89f;
            }

            Debug.Log($"<color={(isGrandBoss ? "#EF4444" : "#FACC15")}>[ARENA LOCKDOWN]</color> Arena locked at {arenaCenter} (Size: {arenaHalfWidth * 2f:F1}m x {arenaHalfHeight * 2f:F1}m)");

            // 2. Lock Camera & Perform Punch Zoom
            if (CameraFollow2D.Instance != null)
            {
                CameraFollow2D.Instance.Lock(arenaCenter);
                CameraFollow2D.Instance.DoPunchZoom(4.4f, 0.35f);
            }

            // 3. HUD Timer lockdown color
            Color themeColor = isGrandBoss ? BossCrimson : MiniBossYellow;
            if (InGameHUD.Instance != null)
            {
                InGameHUD.Instance.SetTimerLockdown(themeColor);
            }

            // 4. Pause regular mob spawning & purge lingering regular mobs
            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.SetSpawningPaused(true);
            }
            ClearRegularMobs();

            // 5. Build Visuals: 75% Dark Mask + Glowing Energy Barrier
            BuildArenaVisuals(themeColor);

            // 6. Build Physical 2D Colliders to confine player
            BuildBoundaryColliders();
        }

        private void BuildArenaVisuals(Color barrierColor)
        {
            if (visualsContainer != null) Destroy(visualsContainer);
            visualsContainer = new GameObject("BossArena_Visuals");
            visualsContainer.transform.position = (Vector3)arenaCenter;

            darknessRenderers.Clear();

            // Procedural 1x1 white sprite for coloring
            Texture2D tex = Texture2D.whiteTexture;
            Sprite maskSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            float w = arenaHalfWidth;
            float h = arenaHalfHeight;
            float pad = 200f; // Large enough to cover infinite terrain chunks
            Color darkColor = new Color(0.02f, 0.03f, 0.05f, 0.75f); // 75% obsidian darkness

            // 1. Top Darkness Mask
            CreateMaskQuad("Mask_Top", new Vector3(0f, h + pad * 0.5f, 0f), new Vector3(w * 2f + pad * 2f, pad, 1f), maskSprite, darkColor);

            // 2. Bottom Darkness Mask
            CreateMaskQuad("Mask_Bottom", new Vector3(0f, -h - pad * 0.5f, 0f), new Vector3(w * 2f + pad * 2f, pad, 1f), maskSprite, darkColor);

            // 3. Left Darkness Mask
            CreateMaskQuad("Mask_Left", new Vector3(-w - pad * 0.5f, 0f, 0f), new Vector3(pad, h * 2f, 1f), maskSprite, darkColor);

            // 4. Right Darkness Mask
            CreateMaskQuad("Mask_Right", new Vector3(w + pad * 0.5f, 0f, 0f), new Vector3(pad, h * 2f, 1f), maskSprite, darkColor);

            // 5. Energy Barrier Perimeter Line
            GameObject lineObj = new GameObject("Energy_Barrier_Border");
            lineObj.transform.SetParent(visualsContainer.transform, false);

            barrierLine = lineObj.AddComponent<LineRenderer>();
            barrierLine.useWorldSpace = false;
            barrierLine.positionCount = 5;

            barrierLine.SetPosition(0, new Vector3(-w, -h, 0f));
            barrierLine.SetPosition(1, new Vector3(w, -h, 0f));
            barrierLine.SetPosition(2, new Vector3(w, h, 0f));
            barrierLine.SetPosition(3, new Vector3(-w, h, 0f));
            barrierLine.SetPosition(4, new Vector3(-w, -h, 0f));

            barrierLine.startWidth = 0.12f;
            barrierLine.endWidth = 0.12f;

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (spriteShader == null) spriteShader = Shader.Find("UI/Default");

            Material lineMat = new Material(spriteShader);
            lineMat.color = barrierColor;
            barrierLine.material = lineMat;
            barrierLine.startColor = barrierColor;
            barrierLine.endColor = barrierColor;
            barrierLine.sortingOrder = 55;

            // Fade in the darkness mask smoothly
            StartCoroutine(FadeInVisualsRoutine());
        }

        private void CreateMaskQuad(string quadName, Vector3 localPos, Vector3 localScale, Sprite sprite, Color color)
        {
            GameObject q = new GameObject(quadName);
            q.transform.SetParent(visualsContainer.transform, false);
            q.transform.localPosition = localPos;
            q.transform.localScale = localScale;

            var sr = q.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(color.r, color.g, color.b, 0f); // Start transparent for fade-in
            sr.sortingOrder = 50; // Above background tiles and props
            darknessRenderers.Add(sr);
        }

        private IEnumerator FadeInVisualsRoutine()
        {
            float elapsed = 0f;
            float duration = 0.45f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 0.75f, elapsed / duration);
                foreach (var sr in darknessRenderers)
                {
                    if (sr != null)
                    {
                        sr.color = new Color(0.02f, 0.03f, 0.05f, alpha);
                    }
                }
                yield return null;
            }
        }

        private void BuildBoundaryColliders()
        {
            if (boundaryCollidersContainer != null) Destroy(boundaryCollidersContainer);
            boundaryCollidersContainer = new GameObject("BossArena_Colliders");
            boundaryCollidersContainer.transform.position = (Vector3)arenaCenter;

            float w = arenaHalfWidth;
            float h = arenaHalfHeight;
            float thickness = 1.0f;

            CreateEdgeCollider("Wall_Top", new Vector2(0f, h + thickness * 0.5f), new Vector2(w * 2f + thickness * 2f, thickness));
            CreateEdgeCollider("Wall_Bottom", new Vector2(0f, -h - thickness * 0.5f), new Vector2(w * 2f + thickness * 2f, thickness));
            CreateEdgeCollider("Wall_Left", new Vector2(-w - thickness * 0.5f, 0f), new Vector2(thickness, h * 2f + thickness * 2f));
            CreateEdgeCollider("Wall_Right", new Vector2(w + thickness * 0.5f, 0f), new Vector2(thickness, h * 2f + thickness * 2f));
        }

        private void CreateEdgeCollider(string colName, Vector2 localPos, Vector2 size)
        {
            GameObject wall = new GameObject(colName);
            wall.transform.SetParent(boundaryCollidersContainer.transform, false);
            wall.transform.localPosition = localPos;

            var box = wall.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = false;
        }

        public void ClearRegularMobs()
        {
            EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            int cleared = 0;
            foreach (var eh in allEnemies)
            {
                if (eh != null && !eh.IsBoss)
                {
                    Destroy(eh.gameObject);
                    cleared++;
                }
            }
            if (cleared > 0)
            {
                Debug.Log($"<color=#38BDF8>[BossArenaManager]</color> Cleared {cleared} regular mobs for duel isolation.");
            }
        }

        private void Update()
        {
            if (!isLockdownActive) return;

            // 1. Shimmer / Pulse Energy Barrier Border
            if (barrierLine != null)
            {
                float shimmer = 0.85f + 0.15f * Mathf.Sin(Time.time * 8f);
                Color baseCol = isGrandBossArena ? BossCrimson : MiniBossYellow;
                Color shimmerCol = new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * shimmer);
                barrierLine.startColor = shimmerCol;
                barrierLine.endColor = shimmerCol;
            }

            // 2. Clean dead / destroyed bosses from list
            for (int i = activeBosses.Count - 1; i >= 0; i--)
            {
                if (activeBosses[i] == null)
                {
                    activeBosses.RemoveAt(i);
                }
            }

            // 3. If all active bosses are dead, unlock arena!
            if (activeBosses.Count == 0)
            {
                UnlockArena();
            }
        }

        private void LateUpdate()
        {
            if (!isLockdownActive) return;

            // Failsafe Player Clamp: Prevents player from dashing, flashing, or glitching outside boundary
            if (playerTransform != null)
            {
                Vector3 pos = playerTransform.position;
                float minX = arenaCenter.x - arenaHalfWidth + PlayerRadius;
                float maxX = arenaCenter.x + arenaHalfWidth - PlayerRadius;
                float minY = arenaCenter.y - arenaHalfHeight + PlayerRadius;
                float maxY = arenaCenter.y + arenaHalfHeight - PlayerRadius;

                pos.x = Mathf.Clamp(pos.x, minX, maxX);
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
                playerTransform.position = pos;
            }
        }

        private void OnBossDied(GameObject boss)
        {
            if (boss != null && activeBosses.Contains(boss))
            {
                activeBosses.Remove(boss);
                Debug.Log($"<color=#10B981>[BossArenaManager]</color> Boss {boss.name} defeated! Remaining bosses: {activeBosses.Count}");
            }

            if (activeBosses.Count == 0 && isLockdownActive)
            {
                UnlockArena();
            }
        }

        /// <summary>
        /// Unlocks the arena, fades darkness & barrier, restores camera follow, and resumes mob spawning.
        /// </summary>
        public void UnlockArena()
        {
            if (!isLockdownActive) return;
            isLockdownActive = false;

            if (unlockCoroutine != null) StopCoroutine(unlockCoroutine);
            unlockCoroutine = StartCoroutine(UnlockRoutine());
        }

        private IEnumerator UnlockRoutine()
        {
            Debug.Log("<color=#10B981>[ARENA UNLOCKED]</color> 🎉 Victory! Energy barrier collapsing, camera follow restored!");

            // 1. Immediately destroy boundary colliders so player can walk freely
            if (boundaryCollidersContainer != null)
            {
                Destroy(boundaryCollidersContainer);
                boundaryCollidersContainer = null;
            }

            // 2. Unlock camera follow
            if (CameraFollow2D.Instance != null)
            {
                CameraFollow2D.Instance.Unlock();
            }

            // 3. Reset HUD timer color
            if (InGameHUD.Instance != null)
            {
                InGameHUD.Instance.ResetTimerColor();
            }

            // 4. Resume regular mob spawning
            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.SetSpawningPaused(false);
            }

            // 5. Flash barrier white and fade out visuals over 0.7s
            if (barrierLine != null)
            {
                barrierLine.startColor = Color.white;
                barrierLine.endColor = Color.white;
            }

            float elapsed = 0f;
            float duration = 0.7f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Fade barrier line
                if (barrierLine != null)
                {
                    Color c = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), t);
                    barrierLine.startColor = c;
                    barrierLine.endColor = c;
                }

                // Fade darkness mask
                float alpha = Mathf.Lerp(0.75f, 0f, t);
                foreach (var sr in darknessRenderers)
                {
                    if (sr != null)
                    {
                        sr.color = new Color(0.02f, 0.03f, 0.05f, alpha);
                    }
                }

                yield return null;
            }

            if (visualsContainer != null)
            {
                Destroy(visualsContainer);
                visualsContainer = null;
            }

            activeBosses.Clear();
            unlockCoroutine = null;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
