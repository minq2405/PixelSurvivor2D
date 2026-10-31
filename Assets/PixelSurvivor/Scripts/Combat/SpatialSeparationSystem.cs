using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Spatial Grid Boids Separation System (v2.1 Architecture):
    /// - Operates at 10 Hz (every 0.1s tick) to eliminate Box2D physics solver overhead.
    /// - Disables Enemy-Enemy physics layer collisions in Awake().
    /// - Efficient spatial hash grid partitions enemies into 1.5m cells.
    /// - Calculates smooth boid separation vectors without calling Physics2D.OverlapCircleAll.
    /// </summary>
    public class SpatialSeparationSystem : MonoBehaviour
    {
        public static SpatialSeparationSystem Instance { get; private set; }

        private const float CELL_SIZE = 1.5f;
        private const float TICK_INTERVAL = 0.10f; // 10 Hz
        private float tickTimer = 0f;

        private readonly Dictionary<int, List<EnemyAI>> grid = new Dictionary<int, List<EnemyAI>>();
        private readonly List<EnemyAI> activeEnemies = new List<EnemyAI>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(this); return; }

            // Eliminate Box2D physics pair checks between enemies
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            }
        }

        public void RegisterEnemy(EnemyAI enemy)
        {
            if (enemy != null && !activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
            }
        }

        public void UnregisterEnemy(EnemyAI enemy)
        {
            activeEnemies.Remove(enemy);
        }

        private static int GetCellHash(int cx, int cy)
        {
            return unchecked((cx * 73856093) ^ (cy * 19349663));
        }

        private void Update()
        {
            tickTimer += Time.deltaTime;
            if (tickTimer < TICK_INTERVAL) return;
            tickTimer = 0f;

            // 1. Clear existing cell lists
            foreach (var list in grid.Values)
            {
                list.Clear();
            }

            // 2. Populate spatial grid with active enemies
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                var e = activeEnemies[i];
                if (e == null || !e.gameObject.activeInHierarchy)
                {
                    activeEnemies.RemoveAt(i);
                    continue;
                }

                Vector2 pos = e.transform.position;
                int cx = Mathf.FloorToInt(pos.x / CELL_SIZE);
                int cy = Mathf.FloorToInt(pos.y / CELL_SIZE);
                int key = GetCellHash(cx, cy);

                if (!grid.TryGetValue(key, out var cellList))
                {
                    cellList = new List<EnemyAI>();
                    grid[key] = cellList;
                }
                cellList.Add(e);
            }

            // 3. Compute soft separation for each enemy across cell and 8 adjacent cells
            float radiusSq = 0.55f * 0.55f;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                var e = activeEnemies[i];
                if (e == null) continue;

                Vector2 pos = e.transform.position;
                int cx = Mathf.FloorToInt(pos.x / CELL_SIZE);
                int cy = Mathf.FloorToInt(pos.y / CELL_SIZE);

                Vector2 separation = Vector2.zero;
                int neighborCount = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int key = GetCellHash(cx + dx, cy + dy);
                        if (grid.TryGetValue(key, out var cellList))
                        {
                            for (int n = 0; n < cellList.Count; n++)
                            {
                                var neighbor = cellList[n];
                                if (neighbor != e && neighbor != null)
                                {
                                    Vector2 diff = pos - (Vector2)neighbor.transform.position;
                                    float sqDist = diff.sqrMagnitude;
                                    if (sqDist < radiusSq && sqDist > 0.0001f)
                                    {
                                        float d = Mathf.Sqrt(sqDist);
                                        separation += diff / d;
                                        neighborCount++;
                                    }
                                }
                            }
                        }
                    }
                }

                if (neighborCount > 0)
                {
                    separation /= neighborCount;
                }

                e.CachedSeparation = separation;
            }
        }
    }
}
