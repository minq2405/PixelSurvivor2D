using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using TMPro;

namespace PixelSurvivor
{
    public enum MapRealm
    {
        Overworld = 0,
        Nether = 1,
        TheEnd = 2
    }

    public class InfiniteMapGenerator : MonoBehaviour
    {
        [Header("Realm Selection")]
        [SerializeField] private MapRealm currentRealm = MapRealm.Overworld;

        [Header("Realm Tilesets (16 Dual-Grid Tiles Each)")]
        [SerializeField] private Sprite[] overworldTiles;
        [SerializeField] private Sprite[] netherTiles;
        [SerializeField] private Sprite[] theEndTiles;
        [SerializeField] private Sprite[] tilesetSprites; // Backward compatibility

        [Header("UI Feedback")]
        [SerializeField] private TMP_Text hudMapText;

        [Header("Player Tracking")]
        [SerializeField] private Transform playerTransform;

        [Header("Chunk Settings")]
        [SerializeField] private int chunkSize = 16;
        [Tooltip("Chunk radius around player. 1 = 3x3 chunks (48x48 units), 2 = 5x5 chunks (80x80 units)")]
        [SerializeField] private int viewDistance = 1;

        [Header("Procedural Generation")]
        [SerializeField] private float noiseScale = 0.08f;
        [SerializeField] private float noiseThreshold = 0.50f;
        [SerializeField] private float seed = 1234.56f;

        // Mathematical Dual-Grid bitmask mapping to 4x4 tileset index (0..15)
        // bitmask: (TL << 3) | (TR << 2) | (BR << 1) | BL
        private static readonly int[] MaskToTileIndex = new int[16]
        {
            12, // 0:  0000 -> All secondary (dirt / soul sand / void)
            0,  // 1:  0001 -> BL primary
            13, // 2:  0010 -> BR primary
            3,  // 3:  0011 -> Bottom primary
            8,  // 4:  0100 -> TR primary
            14, // 5:  0101 -> TR + BL primary
            1,  // 6:  0110 -> Right primary
            5,  // 7:  0111 -> TR + BR + BL
            15, // 8:  1000 -> TL primary
            11, // 9:  1001 -> Left primary (TL + BL)
            4,  // 10: 1010 -> TL + BR primary
            2,  // 11: 1011 -> TL + BR + BL
            9,  // 12: 1100 -> Top primary (TL + TR)
            7,  // 13: 1101 -> TL + TR + BL
            10, // 14: 1110 -> TL + TR + BR
            6   // 15: 1111 -> All primary (grass / netherrack / end stone)
        };

        private Tile[] runtimeTiles;
        private readonly Dictionary<Vector2Int, MapChunk> activeChunks = new Dictionary<Vector2Int, MapChunk>();
        private readonly Queue<MapChunk> chunkPool = new Queue<MapChunk>();
        private Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
        private Transform chunkContainer;
        private MapRealm lastValidatedRealm = MapRealm.Overworld;

        public MapRealm CurrentRealm => currentRealm;
        public string CurrentRealmDisplayName => GetRealmDisplayName(currentRealm);
        public string CurrentRealmColorHex => GetRealmColorHex(currentRealm);
        public int ViewDistance { get => viewDistance; set => viewDistance = Mathf.Max(1, value); }

        private void Awake()
        {
            if (chunkContainer == null)
            {
                chunkContainer = new GameObject("ChunkContainer").transform;
                chunkContainer.SetParent(transform);
            }

            ApplyCurrentRealm();
        }

        private void Start()
        {
            if (playerTransform != null)
            {
                UpdateChunks(true);
            }
        }

        public void SetPlayer(Transform player)
        {
            playerTransform = player;
            UpdateChunks(true);
        }

        public void SetRealm(MapRealm realm)
        {
            currentRealm = realm;
            ApplyCurrentRealm();
        }

        public void CycleNextRealm()
        {
            int next = ((int)currentRealm + 1) % 3;
            SetRealm((MapRealm)next);
        }

        public void ApplyCurrentRealm()
        {
            Sprite[] selected = GetTilesForRealm(currentRealm);
            Color moodColor = GetMoodColorForRealm(currentRealm);
            string realmName = GetRealmDisplayName(currentRealm);
            string realmColor = GetRealmColorHex(currentRealm);

            if (selected != null && selected.Length > 0)
            {
                InitializeTiles(selected);
                // Refresh existing chunks with the new tileset and noise offset
                foreach (var kvp in activeChunks)
                {
                    GenerateChunkTiles(kvp.Value, kvp.Key);
                }
            }

            if (Camera.main != null)
            {
                Camera.main.backgroundColor = moodColor;
            }

            if (hudMapText != null)
            {
                hudMapText.text = $"REALM: <color={realmColor}>{realmName}</color>  [PRESS M TO SWITCH]";
            }
        }

        public void SetTileset(Sprite[] sprites)
        {
            tilesetSprites = sprites;
            InitializeTiles(sprites);
            foreach (var kvp in activeChunks)
            {
                GenerateChunkTiles(kvp.Value, kvp.Key);
            }
        }

        private Sprite[] GetTilesForRealm(MapRealm realm)
        {
            switch (realm)
            {
                case MapRealm.Nether:
                    if (netherTiles != null && netherTiles.Length == 16) return netherTiles;
                    break;
                case MapRealm.TheEnd:
                    if (theEndTiles != null && theEndTiles.Length == 16) return theEndTiles;
                    break;
                case MapRealm.Overworld:
                default:
                    if (overworldTiles != null && overworldTiles.Length == 16) return overworldTiles;
                    break;
            }

            // Fallback to legacy tilesetSprites or overworldTiles
            if (tilesetSprites != null && tilesetSprites.Length == 16) return tilesetSprites;
            return overworldTiles;
        }

        private Color GetMoodColorForRealm(MapRealm realm)
        {
            switch (realm)
            {
                case MapRealm.Nether:
                    return new Color(0.18f, 0.05f, 0.05f, 1f); // Deep Crimson Nether
                case MapRealm.TheEnd:
                    return new Color(0.08f, 0.04f, 0.13f, 1f); // Abyssal End Void Purple
                case MapRealm.Overworld:
                default:
                    return new Color(0.09f, 0.10f, 0.15f, 1f); // Twilight Forest Blue
            }
        }

        private string GetRealmDisplayName(MapRealm realm)
        {
            switch (realm)
            {
                case MapRealm.Nether: return "THE NETHER";
                case MapRealm.TheEnd: return "THE END";
                case MapRealm.Overworld:
                default: return "OVERWORLD";
            }
        }

        private string GetRealmColorHex(MapRealm realm)
        {
            switch (realm)
            {
                case MapRealm.Nether: return "#FF5555";
                case MapRealm.TheEnd: return "#DDA0DD";
                case MapRealm.Overworld:
                default: return "#55FF55";
            }
        }

        private void InitializeTiles(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0) return;

            runtimeTiles = new Tile[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                runtimeTiles[i] = ScriptableObject.CreateInstance<Tile>();
                runtimeTiles[i].sprite = sprites[i];
            }
        }

        private void Update()
        {
            // Switch realm with [M] key
            bool mPressed = Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;

            if (mPressed)
            {
                CycleNextRealm();
            }

            if (playerTransform == null) return;

            Vector2Int currentChunk = new Vector2Int(
                Mathf.FloorToInt(playerTransform.position.x / chunkSize),
                Mathf.FloorToInt(playerTransform.position.y / chunkSize)
            );

            if (currentChunk != lastPlayerChunk)
            {
                lastPlayerChunk = currentChunk;
                UpdateChunks(false);
            }
        }

        public void UpdateChunks(bool force)
        {
            if (playerTransform == null || runtimeTiles == null || runtimeTiles.Length == 0) return;

            Vector2Int center = new Vector2Int(
                Mathf.FloorToInt(playerTransform.position.x / chunkSize),
                Mathf.FloorToInt(playerTransform.position.y / chunkSize)
            );

            HashSet<Vector2Int> requiredCoords = new HashSet<Vector2Int>();
            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                for (int y = -viewDistance; y <= viewDistance; y++)
                {
                    requiredCoords.Add(new Vector2Int(center.x + x, center.y + y));
                }
            }

            // Recycle out-of-range chunks
            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (var kvp in activeChunks)
            {
                if (!requiredCoords.Contains(kvp.Key) || force)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var coord in toRemove)
            {
                MapChunk chunk = activeChunks[coord];
                activeChunks.Remove(coord);
                chunk.gameObject.SetActive(false);
                chunkPool.Enqueue(chunk);
            }

            // Spawn or reuse chunks for required coordinates
            foreach (var coord in requiredCoords)
            {
                if (!activeChunks.ContainsKey(coord))
                {
                    MapChunk chunk = GetOrCreateChunk();
                    chunk.transform.position = new Vector3(coord.x * chunkSize, coord.y * chunkSize, 0f);
                    chunk.gameObject.SetActive(true);
                    chunk.Coord = coord;
                    GenerateChunkTiles(chunk, coord);
                    activeChunks[coord] = chunk;
                }
            }
        }

        private MapChunk GetOrCreateChunk()
        {
            if (chunkPool.Count > 0)
            {
                return chunkPool.Dequeue();
            }

            GameObject chunkGo = new GameObject("Chunk");
            chunkGo.transform.SetParent(chunkContainer);

            Grid grid = chunkGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            GameObject tilemapGo = new GameObject("Tilemap");
            tilemapGo.transform.SetParent(chunkGo.transform, false);

            Tilemap tilemap = tilemapGo.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapGo.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = -10; // Under player and enemies

            MapChunk chunk = chunkGo.AddComponent<MapChunk>();
            chunk.Tilemap = tilemap;

            return chunk;
        }

        private void GenerateChunkTiles(MapChunk chunk, Vector2Int chunkCoord)
        {
            Tilemap tilemap = chunk.Tilemap;
            tilemap.ClearAllTiles();

            if (runtimeTiles == null || runtimeTiles.Length == 0) return;

            Vector3Int[] positions = new Vector3Int[chunkSize * chunkSize];
            TileBase[] tiles = new TileBase[chunkSize * chunkSize];

            int idx = 0;
            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int worldX = chunkCoord.x * chunkSize + x;
                    int worldY = chunkCoord.y * chunkSize + y;

                    // Dual-grid sample: check 4 corners
                    int tl = IsPrimaryTile(worldX, worldY + 1) ? 1 : 0;
                    int tr = IsPrimaryTile(worldX + 1, worldY + 1) ? 1 : 0;
                    int br = IsPrimaryTile(worldX + 1, worldY) ? 1 : 0;
                    int bl = IsPrimaryTile(worldX, worldY) ? 1 : 0;

                    int mask = (tl << 3) | (tr << 2) | (br << 1) | bl;
                    int tileIdx = MaskToTileIndex[mask];

                    if (tileIdx >= 0 && tileIdx < runtimeTiles.Length)
                    {
                        positions[idx] = new Vector3Int(x, y, 0);
                        tiles[idx] = runtimeTiles[tileIdx];
                    }
                    idx++;
                }
            }

            tilemap.SetTiles(positions, tiles);
        }

        private bool IsPrimaryTile(int worldX, int worldY)
        {
            // Deterministic 2D Perlin noise with realm seed offset
            float realmSeed = seed + ((int)currentRealm * 1000f);
            float sampleX = (worldX + realmSeed) * noiseScale;
            float sampleY = (worldY + realmSeed) * noiseScale;
            float noise = Mathf.PerlinNoise(sampleX, sampleY);
            return noise > noiseThreshold;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (viewDistance < 1) viewDistance = 1;
            if (Application.isPlaying && currentRealm != lastValidatedRealm)
            {
                lastValidatedRealm = currentRealm;
                ApplyCurrentRealm();
            }
        }
        #endif
    }

    public class MapChunk : MonoBehaviour
    {
        public Tilemap Tilemap { get; set; }
        public Vector2Int Coord { get; set; }
    }
}
