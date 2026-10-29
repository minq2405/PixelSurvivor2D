using UnityEngine;
using UnityEngine.InputSystem;
using PixelSurvivor.Combat;
using PixelSurvivor.UI;

namespace PixelSurvivor.Debugging
{
    /// <summary>
    /// Interactive In-Game Debug & Sandbox Controller:
    /// - Allows instant jumping to any minute of the run (Min 0, 2, 5, 10, 14) without waiting.
    /// - Allows 1-click force spawning of Zombie, Spider, Creeper directly next to Player.
    /// - Features God Mode, Game Speed multiplier (1x/2x/5x), Mass Kill (for drop testing), and Mass Clear.
    /// - Toggleable with F1, Backquote (~), or top-left [🛠️ DEV] button.
    /// </summary>
    public class CombatDebugOverlay : MonoBehaviour
    {
        [Header("Overlay Settings")]
        [SerializeField] private bool isOpen = true;
        [SerializeField] private bool showMiniToggle = true;

        private WaveSpawner spawner;
        private PlayerController player;
        private PlayerHealth playerHealth;
        private Rect windowRect = new Rect(20, 40, 345, 560);
        private int selectedSpeedIdx = 0;
        private readonly float[] speedMultipliers = { 1.0f, 2.0f, 4.0f, 8.0f };
        private readonly string[] speedLabels = { "1x", "2x", "4x", "8x" };

        private void Start()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            if (spawner == null) spawner = Object.FindAnyObjectByType<WaveSpawner>();
            if (player == null) player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null && playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            // Toggle overlay via F1 or Backquote
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f1Key.wasPressedThisFrame || Keyboard.current.backquoteKey.wasPressedThisFrame)
                {
                    isOpen = !isOpen;
                }

                // Quick number hotkeys when overlay is open
                if (isOpen)
                {
                    if (Keyboard.current.digit1Key.wasPressedThisFrame) SpawnZombie();
                    if (Keyboard.current.digit2Key.wasPressedThisFrame) SpawnSpider();
                    if (Keyboard.current.digit3Key.wasPressedThisFrame) SpawnCreeper();
                    if (Keyboard.current.digit4Key.wasPressedThisFrame) SpawnSlime();
                    if (Keyboard.current.digit5Key.wasPressedThisFrame) SpawnWitch();
                    if (Keyboard.current.digit6Key.wasPressedThisFrame) SpawnHuskShovel();
                    if (Keyboard.current.gKey.wasPressedThisFrame) ToggleGodMode();
                    if (Keyboard.current.kKey.wasPressedThisFrame) KillAllMobs();
                    if (Keyboard.current.cKey.wasPressedThisFrame) ClearAllMobs();
                }
            }
        }

        private void OnGUI()
        {
            if (showMiniToggle)
            {
                if (GUI.Button(new Rect(10, 10, 90, 26), isOpen ? "✕ Hide Dev" : "🛠️ DEV PANEL"))
                {
                    isOpen = !isOpen;
                }
            }

            if (!isOpen) return;

            GUI.backgroundColor = new Color(0.08f, 0.12f, 0.18f, 0.95f);
            windowRect = GUI.Window(999123, windowRect, DrawWindowContent, "⚔️ PIXEL SURVIVOR - COMBAT TESTBED");
        }

        private void DrawWindowContent(int windowID)
        {
            if (spawner == null || player == null)
            {
                FindReferences();
            }

            GUILayout.Space(6);

            // 1. RUN STATISTICS
            GUILayout.BeginVertical("box");
            float t = spawner != null ? spawner.TimeElapsed : 0f;
            int mins = Mathf.FloorToInt(t / 60f);
            int secs = Mathf.FloorToInt(t % 60f);
            int mobCount = spawner != null ? spawner.ActiveMobCount : 0;
            float playerHp = playerHealth != null ? playerHealth.CurrentHealth : 0f;
            float playerMaxHp = playerHealth != null ? playerHealth.MaxHealth : 100f;
            bool isGod = playerHealth != null && playerHealth.IsInvincible;

            GUILayout.Label($"<b>Run Time:</b> <color=#38BDF8>{mins:00}:{secs:00}</color> ({t:F0}s / 900s)");
            GUILayout.Label($"<b>Active Mobs:</b> <color=#FBBF24>{mobCount}</color> | <b>Player HP:</b> <color={(isGod ? "#34D399" : "#F87171")}>{playerHp:F0}/{playerMaxHp:F0} {(isGod ? "(GOD)" : "")}</color>");
            GUILayout.EndVertical();

            GUILayout.Space(6);

            // 2. FAST-FORWARD TIME (No waiting required!)
            GUILayout.Label("<b>⏩ FAST-FORWARD TIME (PHASES)</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("00:00\nZombie", GUILayout.Height(38))) JumpTo(0f);
            if (GUILayout.Button("02:00\n+Spider", GUILayout.Height(38))) JumpTo(2f);
            if (GUILayout.Button("05:00\n+Creeper", GUILayout.Height(38))) JumpTo(5f);
            if (GUILayout.Button("10:00\nHorde", GUILayout.Height(38))) JumpTo(10f);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // 3. INSTANT SPAWN MOB DIRECTLY AT PLAYER
            GUILayout.Label("<b>👾 INSTANT SPAWN (2.5m Near Player)</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🧟 Zombie [1]", GUILayout.Height(30))) SpawnZombie();
            if (GUILayout.Button("🕷️ Spider [2]", GUILayout.Height(30))) SpawnSpider();
            if (GUILayout.Button("💣 Creeper [3]", GUILayout.Height(30))) SpawnCreeper();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🟢 Slime [4]", GUILayout.Height(30))) SpawnSlime();
            if (GUILayout.Button("🧙 Witch [5]", GUILayout.Height(30))) SpawnWitch();
            if (GUILayout.Button("⛏️ Husk [6]", GUILayout.Height(30))) SpawnHuskShovel();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💥 Pack (All 6 Mobs)", GUILayout.Height(28))) SpawnPack();
            if (GUILayout.Button("🎲 5x Wave Spawns", GUILayout.Height(28))) SpawnWaveBurst(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // 4. GAME SPEED MULTIPLIER
            GUILayout.Label("<b>⚡ GAME SIMULATION SPEED</b>");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < speedMultipliers.Length; i++)
            {
                bool isSelected = selectedSpeedIdx == i;
                if (GUILayout.Toggle(isSelected, speedLabels[i], "Button", GUILayout.Height(26)) != isSelected)
                {
                    selectedSpeedIdx = i;
                    Time.timeScale = speedMultipliers[i];
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // 5. UTILITY & CHEATS
            GUILayout.Label("<b>🛠️ CHEATS & DROP TESTING</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(isGod ? "🛡️ God Mode: ON" : "🛡️ God Mode: OFF", GUILayout.Height(28))) ToggleGodMode();
            if (GUILayout.Button("💚 Full Heal", GUILayout.Height(28))) HealPlayer();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💎 Kill All [K]\n(Test Drops)", GUILayout.Height(34))) KillAllMobs();
            if (GUILayout.Button("🧹 Clear All [C]\n(Clean Screen)", GUILayout.Height(34))) ClearAllMobs();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("⭐ Trigger Level Up Modal Now", GUILayout.Height(26)))
            {
                var lvlMgr = Object.FindAnyObjectByType<LevelUpManager>();
                if (lvlMgr != null) lvlMgr.TestTriggerLevelUp();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 24));
        }

        private void JumpTo(float minute)
        {
            if (spawner != null) spawner.JumpToTime(minute);
        }

        private void SpawnZombie()
        {
            if (spawner != null) spawner.ForceSpawnEnemy(spawner.ZombieTemplate);
        }

        private void SpawnSpider()
        {
            if (spawner != null) spawner.ForceSpawnEnemy(spawner.SpiderTemplate);
        }

        private void SpawnCreeper()
        {
            if (spawner != null) spawner.ForceSpawnEnemy(spawner.CreeperTemplate);
        }

        private void SpawnSlime()
        {
            if (spawner != null) spawner.ForceSpawnEnemy(spawner.SlimeTemplate);
        }

        private void SpawnWitch()
        {
            if (spawner != null) spawner.ForceSpawnEnemy(spawner.WitchTemplate);
        }

        private void SpawnHuskShovel()
        {
            if (spawner != null) spawner.ForceSpawnEnemy(spawner.HuskShovelTemplate);
        }

        private void SpawnPack()
        {
            if (spawner == null || player == null) return;
            Vector3 p = player.transform.position;
            spawner.ForceSpawnEnemy(spawner.ZombieTemplate, p + new Vector3(2.5f, 0f, 0f));
            spawner.ForceSpawnEnemy(spawner.SpiderTemplate, p + new Vector3(0f, 2.5f, 0f));
            spawner.ForceSpawnEnemy(spawner.CreeperTemplate, p + new Vector3(-2.5f, 0f, 0f));
            spawner.ForceSpawnEnemy(spawner.SlimeTemplate, p + new Vector3(0f, -2.5f, 0f));
            spawner.ForceSpawnEnemy(spawner.WitchTemplate, p + new Vector3(3.5f, 2.5f, 0f));
            spawner.ForceSpawnEnemy(spawner.HuskShovelTemplate, p + new Vector3(-2f, 2f, 0f));
        }

        private void SpawnWaveBurst(int count)
        {
            if (spawner == null) return;
            var spawnMethod = typeof(WaveSpawner).GetMethod("SpawnNextEnemy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (spawnMethod != null)
            {
                float t = spawner.TimeElapsed / 60f;
                for (int i = 0; i < count; i++)
                {
                    spawnMethod.Invoke(spawner, new object[] { t });
                }
            }
        }

        private void ToggleGodMode()
        {
            if (playerHealth != null)
            {
                playerHealth.IsInvincible = !playerHealth.IsInvincible;
                Debug.Log($"<color=#38BDF8>[Dev]</color> God Mode: {(playerHealth.IsInvincible ? "ENABLED" : "DISABLED")}");
            }
        }

        private void HealPlayer()
        {
            if (playerHealth != null)
            {
                playerHealth.Heal(playerHealth.MaxHealth);
            }
        }

        private void KillAllMobs()
        {
            if (spawner != null) spawner.KillAllActiveEnemies();
        }

        private void ClearAllMobs()
        {
            if (spawner != null) spawner.ClearAllEnemies();
        }

        private void OnDisable()
        {
            // Restore normal game speed on disable
            Time.timeScale = 1.0f;
        }
    }
}
