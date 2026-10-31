using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using PixelSurvivor.Combat;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Manages In-Game Pause (ESC) and Game Over (Player Death) states.
    /// Strictly adheres to Tone 1: Dark Obsidian & Gemstone design tokens.
    /// Uses custom textures & TextMeshPro font for high-contrast crisp pixel aesthetics.
    /// </summary>
    public class InGamePauseManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool allowPause = true;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("UI Canvas (Optional Override)")]
        [SerializeField] private GameObject pauseMenuCanvas;
        [SerializeField] private GameObject gameOverCanvas;
        [SerializeField] private TMP_Text pauseStatsText;
        [SerializeField] private TMP_Text gameOverStatsText;

        [Header("Detailed Pause Menu Elements")]
        [SerializeField] private TMP_Text pauseHpStatText;
        [SerializeField] private TMP_Text pauseAttackStatText;
        [SerializeField] private TMP_Text pauseSpeedStatText;
        [SerializeField] private TMP_Text pauseCooldownStatText;
        [SerializeField] private TMP_Text pauseRangeStatText;
        [SerializeField] private TMP_Text pauseKnockbackStatText;
        [SerializeField] private TMP_Text pauseCriticalStatText;
        [SerializeField] private TMP_Text pauseDmgReductionStatText;
        [SerializeField] private TMP_Text pauseTimeStatText;
        [SerializeField] private TMP_Text pauseWeaponNameText;
        [SerializeField] private TMP_Text pauseWeaponDamageText;
        [SerializeField] private TMP_Text pauseWeaponDpsText;
        [SerializeField] private TMP_Text pauseItemEmptyText;
        [SerializeField] private Transform pauseItemContainer;

        [Header("Detailed Defeat Modal Elements")]
        [SerializeField] private TMP_Text defeatTimeText;
        [SerializeField] private TMP_Text defeatLevelText;
        [SerializeField] private TMP_Text defeatDefeatedText;
        [SerializeField] private TMP_Text defeatBossesText;
        [SerializeField] private TMP_Text defeatEmeraldsText;
        [SerializeField] private TMP_Text defeatEmeraldsFormulaText;
        [SerializeField] private TMP_Text defeatWeaponsRowText;
        [SerializeField] private TMP_Text defeatRunSummaryHeader;
        [SerializeField] private TMP_Text defeatWeaponNameText;
        [SerializeField] private TMP_Text defeatWeaponDamageText;
        [SerializeField] private TMP_Text defeatWeaponDpsText;
        [SerializeField] private TMP_Text defeatItemStatusText;

        private bool isPaused = false;
        private bool isGameOver = false;
        private WaveSpawner waveSpawner;
        private PlayerHealth playerHealth;
        private bool showDamageNumbers = true;
        private Font pixelFont;

        // Cached solid textures matching Tone 1 tokens
        private Texture2D texDimBg;
        private Texture2D texObsidian;
        private Texture2D texCardSurface;
        private Texture2D texEmerald;
        private Texture2D texEmeraldHover;
        private Texture2D texSlate;
        private Texture2D texSlateHover;
        private Texture2D texCrimson;
        private Texture2D texCrimsonHover;
        private Texture2D texGoldBorder;
        private Texture2D texSlateBorder;
        private Texture2D texCardBorder;

        // Custom GUIStyles
        private GUIStyle titlePauseStyle;
        private GUIStyle titleGameOverStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle statValueStyle;
        private GUIStyle btnEmeraldStyle;
        private GUIStyle btnSlateStyle;
        private GUIStyle btnCyanStyle;

        public bool IsPaused => isPaused;
        public bool IsGameOver => isGameOver;

        private void Awake()
        {
            isPaused = false;
            isGameOver = false;

            pixelFont = Resources.Load<Font>("Fonts/VCR_OSD_MONO");
            BuildTone1Textures();
        }

        private void Start()
        {
            isPaused = false;
            isGameOver = false;

            waveSpawner = FindAnyObjectByType<WaveSpawner>();
            playerHealth = FindAnyObjectByType<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.OnPlayerDied += HandlePlayerDied;
            }

            AutoLocatePauseAndDefeatPanels();

            if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
            if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
        }

        private void AutoLocatePauseAndDefeatPanels()
        {
            GameObject inGameCanvas = GameObject.Find("InGame_Canvas");
            if (inGameCanvas == null)
            {
                var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (var c in canvases)
                {
                    if (c.gameObject.name == "InGame_Canvas")
                    {
                        inGameCanvas = c.gameObject;
                        break;
                    }
                }
            }

            if (inGameCanvas == null) return;
            if (!inGameCanvas.activeSelf) inGameCanvas.SetActive(true);
            if (inGameCanvas.transform.localScale != Vector3.one) inGameCanvas.transform.localScale = Vector3.one;

            if (pauseMenuCanvas == null)
            {
                Transform p = inGameCanvas.transform.Find("Pause_Modal_Panel");
                if (p != null) pauseMenuCanvas = p.gameObject;
            }

            if (gameOverCanvas == null)
            {
                Transform g = inGameCanvas.transform.Find("GameOver_Modal_Panel");
                if (g != null)
                {
                    gameOverCanvas = g.gameObject;
                    gameOverCanvas.transform.localScale = Vector3.one;
                }
            }

            // Auto-wire InGame HUD Pause Button (Top Right)
            Transform pauseBtnTrans = inGameCanvas.transform.Find("HUD_Root/Button_Pause");
            if (pauseBtnTrans != null)
            {
                Button pBtn = pauseBtnTrans.GetComponent<Button>();
                if (pBtn != null)
                {
                    pBtn.onClick.RemoveListener(TogglePause);
                    pBtn.onClick.AddListener(TogglePause);
                }
            }

            // Auto-wire Pause Top Action Buttons (Continue, Settings, Quit)
            if (pauseMenuCanvas != null)
            {
                Button btnContinue = pauseMenuCanvas.transform.Find("Card/Top_Action_Buttons/Btn_Continue")?.GetComponent<Button>();
                if (btnContinue != null)
                {
                    btnContinue.onClick.RemoveListener(Resume);
                    btnContinue.onClick.AddListener(Resume);
                }

                Button btnSettings = pauseMenuCanvas.transform.Find("Card/Top_Action_Buttons/Btn_Settings")?.GetComponent<Button>();
                if (btnSettings != null)
                {
                    btnSettings.onClick.RemoveListener(OnClickPauseSettings);
                    btnSettings.onClick.AddListener(OnClickPauseSettings);
                }

                Button btnQuit = pauseMenuCanvas.transform.Find("Card/Top_Action_Buttons/Btn_Quit")?.GetComponent<Button>();
                if (btnQuit != null)
                {
                    btnQuit.onClick.RemoveListener(QuitToMainMenu);
                    btnQuit.onClick.AddListener(QuitToMainMenu);
                }

                // Auto-link known pause text fields if missing
                Transform card = pauseMenuCanvas.transform.Find("Card");
                if (card != null)
                {
                    if (pauseHpStatText == null) pauseHpStatText = card.Find("Section_Stats/Stat_HP/Value")?.GetComponent<TMP_Text>();
                    if (pauseAttackStatText == null) pauseAttackStatText = card.Find("Section_Stats/Stat_Attack/Value")?.GetComponent<TMP_Text>();
                    if (pauseSpeedStatText == null) pauseSpeedStatText = card.Find("Section_Stats/Stat_Speed/Value")?.GetComponent<TMP_Text>();
                    if (pauseCooldownStatText == null) pauseCooldownStatText = card.Find("Section_Stats/Stat_Cooldown/Value")?.GetComponent<TMP_Text>();
                    if (pauseRangeStatText == null) pauseRangeStatText = card.Find("Section_Stats/Stat_Range/Value")?.GetComponent<TMP_Text>();
                    if (pauseKnockbackStatText == null) pauseKnockbackStatText = card.Find("Section_Stats/Stat_Knockback/Value")?.GetComponent<TMP_Text>();
                    if (pauseCriticalStatText == null) pauseCriticalStatText = card.Find("Section_Stats/Stat_Critical/Value")?.GetComponent<TMP_Text>();
                    if (pauseDmgReductionStatText == null) pauseDmgReductionStatText = card.Find("Section_Stats/Stat_Dmg Reduct/Value")?.GetComponent<TMP_Text>();
                    if (pauseTimeStatText == null) pauseTimeStatText = card.Find("Section_Stats/Stat_Time/Value")?.GetComponent<TMP_Text>();
                    if (pauseWeaponNameText == null) pauseWeaponNameText = card.Find("Section_Weapons/Weapon_Row/Weapon_Name")?.GetComponent<TMP_Text>();
                    if (pauseWeaponDamageText == null) pauseWeaponDamageText = card.Find("Section_Weapons/Weapon_Row/Weapon_Damage")?.GetComponent<TMP_Text>();
                    if (pauseWeaponDpsText == null) pauseWeaponDpsText = card.Find("Section_Weapons/Weapon_Row/Weapon_DPS")?.GetComponent<TMP_Text>();
                }
            }

            // Auto-wire Defeat Action Buttons (Retry, Quit)
            if (gameOverCanvas != null)
            {
                Button btnRetry = gameOverCanvas.transform.Find("Card/Action_Buttons/Btn_Retry")?.GetComponent<Button>();
                if (btnRetry != null)
                {
                    btnRetry.onClick.RemoveListener(RestartRun);
                    btnRetry.onClick.AddListener(RestartRun);
                }

                Button btnQuit = gameOverCanvas.transform.Find("Card/Action_Buttons/Btn_Quit")?.GetComponent<Button>();
                if (btnQuit != null)
                {
                    btnQuit.onClick.RemoveListener(QuitToMainMenu);
                    btnQuit.onClick.AddListener(QuitToMainMenu);
                }
            }
        }

        public void OnClickPauseSettings()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                Transform sp = canvasObj.transform.Find("SettingsPanel");
                if (sp != null)
                {
                    sp.gameObject.SetActive(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDied -= HandlePlayerDied;
            }
        }

        private void Update()
        {
            if (playerHealth == null)
            {
                playerHealth = FindAnyObjectByType<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.OnPlayerDied -= HandlePlayerDied;
                    playerHealth.OnPlayerDied += HandlePlayerDied;
                }
            }

            if (isGameOver || !allowPause) return;

            // Only respond to New Input System
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (isGameOver) return;

            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (isGameOver) return;

            isPaused = true;
            TimeScaleManager.Instance?.AcquirePause("PauseMenu");

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(true);
                if (pauseStatsText != null) pauseStatsText.text = GetPauseStatsString();
                UpdatePauseMenuDetails();
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log("<color=#FACC15>[Game Paused]</color> Time frozen.");
        }

        public void UpdatePauseMenuDetails()
        {
            if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
            PlayerController player = FindAnyObjectByType<PlayerController>();
            WeaponController weaponCtrl = FindAnyObjectByType<WeaponController>();
            if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();

            // HP
            if (pauseHpStatText != null)
            {
                float curHp = playerHealth != null ? playerHealth.CurrentHealth : 100f;
                float maxHp = playerHealth != null ? playerHealth.MaxHealth : 100f;
                pauseHpStatText.text = $"{Mathf.CeilToInt(curHp)} / {Mathf.CeilToInt(maxHp)}";
            }

            // Time
            if (pauseTimeStatText != null)
            {
                string timeStr = "00:00";
                if (waveSpawner != null)
                {
                    int m = Mathf.FloorToInt(waveSpawner.TimeElapsed / 60f);
                    int s = Mathf.FloorToInt(waveSpawner.TimeElapsed % 60f);
                    timeStr = $"{m:00}:{s:00}";
                }
                pauseTimeStatText.text = timeStr;
            }

            // Move Speed
            if (pauseSpeedStatText != null)
            {
                float speed = player != null ? player.MoveSpeed : 4f;
                pauseSpeedStatText.text = speed.ToString("F1");
            }

            // Weapon & Attack Stats
            if (weaponCtrl != null && weaponCtrl.EquippedWeapons != null && weaponCtrl.EquippedWeapons.Count > 0)
            {
                WeaponBase primaryWeapon = weaponCtrl.EquippedWeapons[0];
                float dmg = primaryWeapon.BaseDamage * weaponCtrl.DamageMultiplier;
                float cd = Mathf.Max(0.1f, primaryWeapon.BaseCooldown * weaponCtrl.CooldownMultiplier);
                float dps = dmg / cd;

                if (pauseWeaponNameText != null) pauseWeaponNameText.text = $"{primaryWeapon.weaponName} Lv.{primaryWeapon.currentLevel}";
                if (pauseWeaponDamageText != null) pauseWeaponDamageText.text = Mathf.RoundToInt(dmg).ToString();
                if (pauseWeaponDpsText != null) pauseWeaponDpsText.text = $"{dps:F1}/s";

                if (pauseAttackStatText != null) pauseAttackStatText.text = Mathf.RoundToInt(dmg).ToString();
                if (pauseCooldownStatText != null) pauseCooldownStatText.text = $"{cd:F1}s";
            }
            else
            {
                if (pauseWeaponNameText != null) pauseWeaponNameText.text = "Iron Sword Lv.1";
                if (pauseWeaponDamageText != null) pauseWeaponDamageText.text = "25";
                if (pauseWeaponDpsText != null) pauseWeaponDpsText.text = "19.2/s";
                if (pauseAttackStatText != null) pauseAttackStatText.text = "25";
                if (pauseCooldownStatText != null) pauseCooldownStatText.text = "1.3s";
            }

            PlayerStats pStats = PlayerStats.Instance;
            if (pStats != null)
            {
                if (pauseSpeedStatText != null) pauseSpeedStatText.text = $"{pStats.EffectiveMoveSpeed:F1} m/s";
                if (pauseCriticalStatText != null) pauseCriticalStatText.text = $"{Mathf.RoundToInt(pStats.EffectiveCritChance * 100f)} %";
                if (pauseDmgReductionStatText != null) pauseDmgReductionStatText.text = $"{Mathf.RoundToInt(pStats.EffectiveArmor * 100f)} %";
            }
            else
            {
                if (pauseRangeStatText != null) pauseRangeStatText.text = "3.5 m";
                if (pauseKnockbackStatText != null) pauseKnockbackStatText.text = "4.0";
                if (pauseCriticalStatText != null) pauseCriticalStatText.text = "5 %";
                if (pauseDmgReductionStatText != null) pauseDmgReductionStatText.text = "0 %";
            }

            if (pauseItemEmptyText != null)
            {
                bool hasItems = pauseItemContainer != null && pauseItemContainer.childCount > 0;
                pauseItemEmptyText.gameObject.SetActive(!hasItems);
            }
        }

        public void Resume()
        {
            if (isGameOver) return;

            isPaused = false;
            TimeScaleManager.Instance?.ReleasePause("PauseMenu");

            if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);

            Debug.Log("<color=#10B981>[Game Resumed]</color> Time running.");
        }

        public void TriggerGameOver()
        {
            if (isGameOver) return;
            HandlePlayerDied();
        }

        private void HandlePlayerDied()
        {
            isGameOver = true;
            isPaused = false;
            TimeScaleManager.Instance?.AcquirePause("DefeatMenu");

            if (gameOverCanvas != null)
            {
                gameOverCanvas.SetActive(true);
                if (gameOverStatsText != null) gameOverStatsText.text = GetGameOverStatsString();
                UpdateDefeatModalDetails();
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log("<color=#EF4444>[DEFEAT]</color> Player has fallen. Game Over screen displayed.");
        }

        public void UpdateDefeatModalDetails()
        {
            if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();
            PlayerController player = FindAnyObjectByType<PlayerController>();
            WeaponController weaponCtrl = FindAnyObjectByType<WeaponController>();
            LevelUpManager lvlMgr = FindAnyObjectByType<LevelUpManager>();

            // Time
            string timeStr = "0:00";
            int totalSeconds = 0;
            if (waveSpawner != null)
            {
                totalSeconds = Mathf.FloorToInt(waveSpawner.TimeElapsed);
                int m = totalSeconds / 60;
                int s = totalSeconds % 60;
                timeStr = $"{m}:{s:00}";
            }
            if (defeatTimeText != null) defeatTimeText.text = timeStr;

            // Level
            int level = lvlMgr != null ? lvlMgr.CurrentLevel : 1;
            if (defeatLevelText != null) defeatLevelText.text = level.ToString();

            // Defeated & Bosses
            int defeatedCount = PixelSurvivor.Combat.EnemyHealth.TotalKills;
            if (defeatDefeatedText != null) defeatDefeatedText.text = defeatedCount.ToString();
            if (defeatBossesText != null) defeatBossesText.text = "0";

            // Emeralds Earned
            int timeBonus = totalSeconds / 5;
            int killBonus = defeatedCount / 2;
            int totalEmeralds = Mathf.Max(10, timeBonus + killBonus);
            if (defeatEmeraldsText != null) defeatEmeraldsText.text = $"+{totalEmeralds}";
            if (defeatEmeraldsFormulaText != null) defeatEmeraldsFormulaText.text = $"Time {timeBonus} · Defeated {killBonus} · Bonus x1.25 Fortune";

            // Weapon info
            string wepName = "Iron Sword Lv.1";
            float dmg = 25f;
            float cd = 1.3f;
            float dps = 19.2f;

            if (weaponCtrl != null && weaponCtrl.EquippedWeapons != null && weaponCtrl.EquippedWeapons.Count > 0)
            {
                WeaponBase primaryWeapon = weaponCtrl.EquippedWeapons[0];
                wepName = $"{primaryWeapon.weaponName} Lv.{primaryWeapon.currentLevel}";
                dmg = primaryWeapon.BaseDamage * weaponCtrl.DamageMultiplier;
                cd = Mathf.Max(0.1f, primaryWeapon.BaseCooldown * weaponCtrl.CooldownMultiplier);
                dps = dmg / cd;
            }

            int totalDamageDealt = Mathf.RoundToInt(dmg * Mathf.Max(10, totalSeconds * 1.5f));

            if (defeatWeaponsRowText != null) defeatWeaponsRowText.text = wepName;

            string charName = player != null ? player.characterName : "Steve";
            if (defeatRunSummaryHeader != null) defeatRunSummaryHeader.text = $"{charName} · Lv. {level} · {totalDamageDealt:N0} damage in total";

            if (defeatWeaponNameText != null) defeatWeaponNameText.text = wepName;
            if (defeatWeaponDamageText != null) defeatWeaponDamageText.text = $"{totalDamageDealt:N0}";
            if (defeatWeaponDpsText != null) defeatWeaponDpsText.text = $"{Mathf.RoundToInt(dps)}/s";
            if (defeatItemStatusText != null) defeatItemStatusText.text = "No item.";
        }

        public void RestartRun()
        {
            TimeScaleManager.Instance?.ResetAllPauses();
            Debug.Log("<color=#10B981>[Retry]</color> Restarting run...");
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        public void RetryRun() => RestartRun();

        public void QuitToMainMenu()
        {
            TimeScaleManager.Instance?.ResetAllPauses();
            Debug.Log("<color=#EF4444>[Quit]</color> Returning to Main Menu...");
            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Scene activeScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(activeScene.buildIndex);
            }
        }

        public string GetPauseStatsString()
        {
            string timeStr = "00:00";
            if (waveSpawner != null)
            {
                int m = Mathf.FloorToInt(waveSpawner.TimeElapsed / 60f);
                int s = Mathf.FloorToInt(waveSpawner.TimeElapsed % 60f);
                timeStr = $"{m:00}:{s:00}";
            }
            float hp = playerHealth != null ? playerHealth.CurrentHealth : 100f;
            float maxHp = playerHealth != null ? playerHealth.MaxHealth : 100f;
            return $"  SURVIVAL TIME:    {timeStr}\n" +
                   $"  HEALTH:           {Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(maxHp)}\n" +
                   $"  DIFFICULTY:       1.0x (Standard)\n" +
                   $"  EQUIPPED WEAPON:  Magma Broadsword (Lv.1)";
        }

        public string GetGameOverStatsString()
        {
            string survivedTime = "00:00";
            if (waveSpawner != null)
            {
                int m = Mathf.FloorToInt(waveSpawner.TimeElapsed / 60f);
                int s = Mathf.FloorToInt(waveSpawner.TimeElapsed % 60f);
                survivedTime = $"{m:00}:{s:00}";
            }
            return $"  TIME SURVIVED:    {survivedTime}\n" +
                   $"  ENEMIES DEFEATED: Zombie Horde\n" +
                   $"  EMERALDS EARNED:  +24 Gems\n" +
                   $"  WEAPON REACH:     Magma Broadsword (Lv.1)";
        }

        private void OnGUI()
        {
            // Fully disabled: Canvas UI (Pause_Modal_Panel & GameOver_Modal_Panel) handles all rendering.
            return;
        }

        // =========================================================================
        // MODAL 1: TONE 1 PAUSE MENU
        // =========================================================================
        private void DrawPauseModal(float screenW, float screenH)
        {
            float modalW = 440f;
            float modalH = 360f;
            float x = (screenW - modalW) * 0.5f;
            float y = (screenH - modalH) * 0.5f;

            // Base Modal Panel (#0B0F19 - Deep Obsidian)
            GUI.DrawTexture(new Rect(x, y, modalW, modalH), texObsidian);

            // Border (#FACC15 - Aurum Gold, 2px)
            DrawBorder(new Rect(x, y, modalW, modalH), texGoldBorder, 2);

            // Title: GAME PAUSED
            GUI.Label(new Rect(x, y + 18, modalW, 36), "GAME PAUSED", titlePauseStyle);

            // Inner Stat Card (#1E293B - Slate Card Surface)
            float cardX = x + 24;
            float cardY = y + 62;
            float cardW = modalW - 48;
            float cardH = 100f;
            GUI.DrawTexture(new Rect(cardX, cardY, cardW, cardH), texCardSurface);
            DrawBorder(new Rect(cardX, cardY, cardW, cardH), texCardBorder, 1);

            // Stats info
            string timeStr = "00:00";
            int mobCount = 0;
            if (waveSpawner != null)
            {
                int m = Mathf.FloorToInt(waveSpawner.TimeElapsed / 60f);
                int s = Mathf.FloorToInt(waveSpawner.TimeElapsed % 60f);
                timeStr = $"{m:00}:{s:00} / 15:00";
                mobCount = waveSpawner.ActiveMobCount;
            }

            float hp = playerHealth != null ? playerHealth.CurrentHealth : 100f;
            float maxHp = playerHealth != null ? playerHealth.MaxHealth : 100f;

            string stats = $"  SURVIVAL TIME:  <color=#38BDF8>{timeStr}</color>\n" +
                           $"  ACTIVE MOBS:    <color=#FACC15>{mobCount}</color>\n" +
                           $"  PLAYER HEALTH:  <color=#EF4444>{hp:F0}/{maxHp:F0} HP</color>\n" +
                           $"  CURRENT WEAPON: <color=#10B981>Magma Broadsword</color>";

            GUI.Label(new Rect(cardX + 12, cardY + 12, cardW - 24, cardH - 24), stats, statLabelStyle);

            // Interactive Buttons
            float btnX = x + 24;
            float btnW = modalW - 48;
            float btnH = 40f;
            float btnStartY = y + 178;

            // 1. Primary CTA: RESUME (Emerald Green #10B981)
            if (GUI.Button(new Rect(btnX, btnStartY, btnW, btnH), "▶  RESUME (ESC)", btnEmeraldStyle))
            {
                Resume();
            }

            // 2. Secondary CTA: DAMAGE NUMBERS TOGGLE (Card Surface + Cyan border)
            string dmgText = showDamageNumbers ? "⚙  DAMAGE NUMBERS: [ON]" : "⚙  DAMAGE NUMBERS: [OFF]";
            if (GUI.Button(new Rect(btnX, btnStartY + 48, btnW, btnH), dmgText, btnCyanStyle))
            {
                showDamageNumbers = !showDamageNumbers;
            }

            // 3. Danger / Exit: BACK TO MENU (Slate Grey #334155)
            if (GUI.Button(new Rect(btnX, btnStartY + 96, btnW, btnH), "🏠  BACK TO MENU", btnSlateStyle))
            {
                QuitToMainMenu();
            }
        }

        // =========================================================================
        // MODAL 2: TONE 1 DEFEAT / GAME OVER SCREEN
        // =========================================================================
        private void DrawGameOverModal(float screenW, float screenH)
        {
            float modalW = 460f;
            float modalH = 370f;
            float x = (screenW - modalW) * 0.5f;
            float y = (screenH - modalH) * 0.5f;

            // Base Modal Panel (#0B0F19 - Deep Obsidian)
            GUI.DrawTexture(new Rect(x, y, modalW, modalH), texObsidian);

            // Border (#EF4444 - Crimson Redstone, 2px)
            DrawBorder(new Rect(x, y, modalW, modalH), texCrimson, 2);

            // Title: DEFEAT
            GUI.Label(new Rect(x, y + 20, modalW, 40), "☠  DEFEAT", titleGameOverStyle);

            // Inner Stat Card (#1E293B)
            float cardX = x + 24;
            float cardY = y + 68;
            float cardW = modalW - 48;
            float cardH = 110f;
            GUI.DrawTexture(new Rect(cardX, cardY, cardW, cardH), texCardSurface);
            DrawBorder(new Rect(cardX, cardY, cardW, cardH), texCardBorder, 1);

            string survivedTime = "00:00";
            if (waveSpawner != null)
            {
                int m = Mathf.FloorToInt(waveSpawner.TimeElapsed / 60f);
                int s = Mathf.FloorToInt(waveSpawner.TimeElapsed % 60f);
                survivedTime = $"{m:00}:{s:00}";
            }

            string defeatStats = $"  TIME SURVIVED:    <color=#FACC15>{survivedTime}</color>\n" +
                                 $"  ENEMIES DEFEATED: <color=#38BDF8>Zombie Horde</color>\n" +
                                 $"  EMERALDS EARNED:  <color=#10B981>+24 💎</color>\n" +
                                 $"  WEAPON REACH:     <color=#F8FAFC>Magma Broadsword (Lv.1)</color>";

            GUI.Label(new Rect(cardX + 12, cardY + 12, cardW - 24, cardH - 24), defeatStats, statLabelStyle);

            // Interactive Buttons
            float btnX = x + 24;
            float btnW = modalW - 48;
            float btnH = 44f;
            float btnStartY = y + 198;

            // 1. Primary CTA: RETRY / TRY AGAIN (Emerald Green #10B981)
            if (GUI.Button(new Rect(btnX, btnStartY, btnW, btnH), "🔄  TRY AGAIN (RESTART)", btnEmeraldStyle))
            {
                RestartRun();
            }

            // 2. Secondary CTA: BACK TO MENU (Slate Grey #334155)
            if (GUI.Button(new Rect(btnX, btnStartY + 54, btnW, btnH), "🏠  BACK TO MENU", btnSlateStyle))
            {
                QuitToMainMenu();
            }
        }

        private void DrawBorder(Rect rect, Texture2D borderTex, int thickness)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), borderTex); // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), borderTex); // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), borderTex); // Left
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), borderTex); // Right
        }

        // =========================================================================
        // TONE 1 COLOR & TEXTURE BUILDER
        // =========================================================================
        private void BuildTone1Textures()
        {
            texDimBg = MakeSolidTex(new Color(0.043f, 0.059f, 0.098f, 0.86f));
            texObsidian = MakeSolidTex(new Color(0.043f, 0.059f, 0.098f, 0.96f));
            texCardSurface = MakeSolidTex(new Color(0.118f, 0.161f, 0.231f, 0.94f));

            texEmerald = MakeSolidTex(new Color(0.063f, 0.725f, 0.506f, 1f)); // #10B981
            texEmeraldHover = MakeSolidTex(new Color(0.020f, 0.588f, 0.412f, 1f)); // #059669

            texSlate = MakeSolidTex(new Color(0.200f, 0.255f, 0.333f, 1f)); // #334155
            texSlateHover = MakeSolidTex(new Color(0.278f, 0.333f, 0.412f, 1f)); // #475569

            texCrimson = MakeSolidTex(new Color(0.937f, 0.267f, 0.267f, 1f)); // #EF4444
            texCrimsonHover = MakeSolidTex(new Color(0.863f, 0.149f, 0.149f, 1f)); // #DC2626

            texGoldBorder = MakeSolidTex(new Color(0.980f, 0.800f, 0.082f, 1f)); // #FACC15
            texSlateBorder = MakeSolidTex(new Color(0.200f, 0.255f, 0.333f, 1f)); // #334155
            texCardBorder = MakeSolidTex(new Color(0.278f, 0.333f, 0.412f, 1f)); // #475569
        }

        private void InitStyles()
        {
            if (titlePauseStyle != null) return;

            titlePauseStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            titlePauseStyle.normal.textColor = new Color(0.980f, 0.800f, 0.082f, 1f); // Aurum Gold
            if (pixelFont != null) titlePauseStyle.font = pixelFont;

            titleGameOverStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            titleGameOverStyle.normal.textColor = new Color(0.937f, 0.267f, 0.267f, 1f); // Crimson Red
            if (pixelFont != null) titleGameOverStyle.font = pixelFont;

            statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                richText = true
            };
            statLabelStyle.normal.textColor = new Color(0.973f, 0.980f, 0.988f, 1f); // TextMain
            if (pixelFont != null) statLabelStyle.font = pixelFont;

            // Emerald Button (Resume / Try Again)
            btnEmeraldStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            btnEmeraldStyle.normal.background = texEmerald;
            btnEmeraldStyle.normal.textColor = new Color(0.973f, 0.980f, 0.988f, 1f);
            btnEmeraldStyle.hover.background = texEmeraldHover;
            btnEmeraldStyle.hover.textColor = Color.white;
            btnEmeraldStyle.active.background = texEmeraldHover;
            if (pixelFont != null) btnEmeraldStyle.font = pixelFont;

            // Slate Button (Back to Menu)
            btnSlateStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            btnSlateStyle.normal.background = texSlate;
            btnSlateStyle.normal.textColor = new Color(0.973f, 0.980f, 0.988f, 1f);
            btnSlateStyle.hover.background = texSlateHover;
            btnSlateStyle.hover.textColor = Color.white;
            btnSlateStyle.active.background = texSlateHover;
            if (pixelFont != null) btnSlateStyle.font = pixelFont;

            // Cyan Card Button (Settings)
            btnCyanStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            btnCyanStyle.normal.background = texCardSurface;
            btnCyanStyle.normal.textColor = new Color(0.220f, 0.741f, 0.973f, 1f); // Diamond Cyan
            btnCyanStyle.hover.background = texSlate;
            btnCyanStyle.hover.textColor = Color.white;
            btnCyanStyle.active.background = texSlate;
            if (pixelFont != null) btnCyanStyle.font = pixelFont;
        }

        private static Texture2D MakeSolidTex(Color col)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }
    }
}
