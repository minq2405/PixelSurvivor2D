using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using PixelSurvivor.Combat;
using PixelSurvivor.Audio;

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

        public enum PauseMenuTab
        {
            Overview,
            Settings
        }

        private PauseMenuTab currentPauseTab = PauseMenuTab.Overview;
        private GameObject pauseSettingsSection;
        private Slider pauseMusicSlider;
        private Slider pauseSfxSlider;
        private TMP_Text pauseMusicPercentText;
        private TMP_Text pauseSfxPercentText;
        private Button pauseMuteBtn;
        private TMP_Text pauseMuteBtnText;
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

        public static InGamePauseManager Instance { get; private set; }

        public bool IsPaused => isPaused;
        public bool IsGameOver => isGameOver;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

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

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
                ShowPauseTab(PauseMenuTab.Overview);
            }
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
                    pBtn.onClick.RemoveListener(Pause);
                    if (pBtn.onClick.GetPersistentEventCount() == 0)
                    {
                        pBtn.onClick.AddListener(Pause);
                    }
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
                    Transform statsSec = card.Find("Section_Stats");
                    if (statsSec != null)
                    {
                        EnsureStatsLabelsAndValues(statsSec);
                    }

                    if (pauseWeaponNameText == null) pauseWeaponNameText = card.Find("Section_Weapons/Weapon_Row_0/Weapon_Name")?.GetComponent<TMP_Text>() ??
                                                                          card.Find("Section_Weapons/Weapon_Row/Weapon_Name")?.GetComponent<TMP_Text>();
                    if (pauseWeaponDamageText == null) pauseWeaponDamageText = card.Find("Section_Weapons/Weapon_Row_0/Weapon_Damage")?.GetComponent<TMP_Text>() ??
                                                                              card.Find("Section_Weapons/Weapon_Row/Weapon_Damage")?.GetComponent<TMP_Text>();
                    if (pauseWeaponDpsText == null) pauseWeaponDpsText = card.Find("Section_Weapons/Weapon_Row_0/Weapon_DPS")?.GetComponent<TMP_Text>() ??
                                                                        card.Find("Section_Weapons/Weapon_Row/Weapon_DPS")?.GetComponent<TMP_Text>();
                    if (pauseItemEmptyText == null) pauseItemEmptyText = card.Find("Section_Item/Text_NoItem")?.GetComponent<TMP_Text>();
                }
            }

            // Auto-wire Defeat Action Buttons and Elements
            if (gameOverCanvas != null)
            {
                Button btnRetry = gameOverCanvas.transform.Find("Card/Action_Buttons/Btn_Retry")?.GetComponent<Button>() ??
                                  gameOverCanvas.transform.Find("Card/Bottom_Buttons_Row/Btn_Retry")?.GetComponent<Button>();
                if (btnRetry != null)
                {
                    btnRetry.gameObject.SetActive(false);
                }

                Button btnQuit = gameOverCanvas.transform.Find("Card/Action_Buttons/Btn_Quit")?.GetComponent<Button>() ??
                                 gameOverCanvas.transform.Find("Card/Bottom_Buttons_Row/Btn_BackToMenu")?.GetComponent<Button>();
                if (btnQuit != null)
                {
                    btnQuit.onClick.RemoveListener(QuitToMainMenu);
                    btnQuit.onClick.AddListener(QuitToMainMenu);

                    // Center the quit button
                    RectTransform qRT = btnQuit.GetComponent<RectTransform>();
                    if (qRT != null)
                    {
                        qRT.anchoredPosition = new Vector2(0f, 0f);
                    }
                }

                Transform card = gameOverCanvas.transform.Find("Card");
                if (card != null)
                {
                    if (defeatTimeText == null) defeatTimeText = card.Find("Stats_Grid_2x2/Card_TIME/Value")?.GetComponent<TMP_Text>();
                    if (defeatLevelText == null) defeatLevelText = card.Find("Stats_Grid_2x2/Card_LEVEL/Value")?.GetComponent<TMP_Text>();
                    if (defeatDefeatedText == null) defeatDefeatedText = card.Find("Stats_Grid_2x2/Card_DEFEATED/Value")?.GetComponent<TMP_Text>();
                    if (defeatBossesText == null) defeatBossesText = card.Find("Stats_Grid_2x2/Card_BOSSES/Value")?.GetComponent<TMP_Text>();
                    if (defeatEmeraldsText == null) defeatEmeraldsText = card.Find("Section_Emeralds/Amount")?.GetComponent<TMP_Text>();
                    if (defeatEmeraldsFormulaText == null) defeatEmeraldsFormulaText = card.Find("Section_Emeralds/Formula")?.GetComponent<TMP_Text>();

                    if (defeatRunSummaryHeader == null)
                    {
                        defeatRunSummaryHeader = card.Find("Run_Summary_Header")?.GetComponent<TMP_Text>() ??
                                                 card.Find("Section_Run_Summary/Subtitle")?.GetComponent<TMP_Text>() ??
                                                 card.Find("Section_Weapons_Box/Subtitle")?.GetComponent<TMP_Text>();
                    }

                    Transform wepBox = card.Find("Section_Weapons_Box") ?? card.Find("Section_Run_Summary");
                    if (wepBox != null)
                    {
                        if (defeatWeaponNameText == null) defeatWeaponNameText = wepBox.Find("Weapon_Stat_Line/Name")?.GetComponent<TMP_Text>() ??
                                                                                wepBox.Find("Weapon_Row_0/Weapon_Name")?.GetComponent<TMP_Text>();
                        if (defeatWeaponDamageText == null) defeatWeaponDamageText = wepBox.Find("Weapon_Stat_Line/Damage")?.GetComponent<TMP_Text>() ??
                                                                                    wepBox.Find("Weapon_Row_0/Weapon_Damage")?.GetComponent<TMP_Text>();
                        if (defeatWeaponDpsText == null) defeatWeaponDpsText = wepBox.Find("Weapon_Stat_Line/DPS")?.GetComponent<TMP_Text>() ??
                                                                              wepBox.Find("Weapon_Row_0/Weapon_DPS")?.GetComponent<TMP_Text>();
                    }

                    Transform itemBox = card.Find("Section_Items_Box") ?? card.Find("Section_Weapons_Row");
                    if (itemBox != null)
                    {
                        if (defeatItemStatusText == null) defeatItemStatusText = itemBox.Find("Item_Status")?.GetComponent<TMP_Text>() ??
                                                                                itemBox.Find("Content")?.GetComponent<TMP_Text>();
                    }
                }
            }
        }

        public void OnClickPauseSettings()
        {
            PixelSurvivor.Audio.AudioManager.Instance?.PlayUiClick();
            if (currentPauseTab == PauseMenuTab.Settings)
            {
                ShowPauseTab(PauseMenuTab.Overview);
            }
            else
            {
                ShowPauseTab(PauseMenuTab.Settings);
            }
        }

        public void ShowPauseTab(PauseMenuTab tab)
        {
            currentPauseTab = tab;
            if (pauseMenuCanvas == null) return;
            Transform pauseCard = pauseMenuCanvas.transform.Find("Card");
            if (pauseCard == null) return;

            Transform secWeapons = pauseCard.Find("Section_Weapons");
            Transform secStats = pauseCard.Find("Section_Stats");
            Transform secItem = pauseCard.Find("Section_Item");

            Transform btnContinueTrans = pauseCard.Find("Top_Action_Buttons/Btn_Continue");
            Transform btnSettingsTrans = pauseCard.Find("Top_Action_Buttons/Btn_Settings");
            RoundedBorderImage continueBorder = btnContinueTrans?.GetComponent<RoundedBorderImage>();
            TMP_Text continueText = btnContinueTrans?.GetComponentInChildren<TMP_Text>();
            RoundedBorderImage settingsBorder = btnSettingsTrans?.GetComponent<RoundedBorderImage>();
            TMP_Text settingsText = btnSettingsTrans?.GetComponentInChildren<TMP_Text>();

            Color activeFill = new Color(0.969f, 0.718f, 0.192f, 1f);   // Gold #F7B731
            Color activeBorder = Color.white;
            Color activeText = new Color(0.118f, 0.153f, 0.180f, 1f);

            Color inactiveFill = new Color(0.125f, 0.251f, 0.596f, 1f); // Deep Blue #204098
            Color inactiveBorder = new Color(0.353f, 0.557f, 0.965f, 1f);
            Color inactiveText = Color.white;

            if (tab == PauseMenuTab.Overview)
            {
                if (secWeapons != null) secWeapons.gameObject.SetActive(true);
                if (secStats != null) secStats.gameObject.SetActive(true);
                if (secItem != null) secItem.gameObject.SetActive(true);
                if (pauseSettingsSection != null) pauseSettingsSection.SetActive(false);

                if (continueBorder != null) { continueBorder.FillColor = activeFill; continueBorder.BorderColor = activeBorder; }
                if (continueText != null) continueText.color = activeText;
                if (settingsBorder != null) { settingsBorder.FillColor = inactiveFill; settingsBorder.BorderColor = inactiveBorder; }
                if (settingsText != null) settingsText.color = inactiveText;

                UpdatePauseMenuDetails();
            }
            else // Settings
            {
                if (secWeapons != null) secWeapons.gameObject.SetActive(false);
                if (secStats != null) secStats.gameObject.SetActive(false);
                if (secItem != null) secItem.gameObject.SetActive(false);

                if (pauseSettingsSection == null)
                {
                    Transform existing = pauseCard.Find("Section_Settings");
                    if (existing != null)
                    {
                        pauseSettingsSection = existing.gameObject;
                    }
                    else
                    {
                        pauseSettingsSection = BuildPauseSettingsSection(pauseCard);
                    }
                }

                if (pauseSettingsSection != null)
                {
                    pauseSettingsSection.SetActive(true);
                    RefreshPauseSettingsUI();
                }

                if (settingsBorder != null) { settingsBorder.FillColor = activeFill; settingsBorder.BorderColor = activeBorder; }
                if (settingsText != null) settingsText.color = activeText;
                if (continueBorder != null) { continueBorder.FillColor = inactiveFill; continueBorder.BorderColor = inactiveBorder; }
                if (continueText != null) continueText.color = inactiveText;
            }
        }

        private GameObject BuildPauseSettingsSection(Transform card)
        {
            TMP_FontAsset font = card.Find("Title")?.GetComponent<TMP_Text>()?.font;

            GameObject sec = new GameObject("Section_Settings", typeof(RectTransform));
            sec.transform.SetParent(card, false);
            RectTransform secRT = sec.GetComponent<RectTransform>();
            secRT.anchorMin = new Vector2(0.5f, 1f);
            secRT.anchorMax = new Vector2(0.5f, 1f);
            secRT.pivot = new Vector2(0.5f, 1f);
            secRT.anchoredPosition = new Vector2(0f, -115f);
            secRT.sizeDelta = new Vector2(580f, 440f);

            // Audio Header
            CreateSettingsHeader(sec.transform, "AUDIO & SOUND", new Vector2(0f, -10f), font);

            // Music Volume Slider
            CreateSettingsSliderRow(sec.transform, "Music Volume", new Vector2(0f, -38f), font,
                out pauseMusicSlider, out pauseMusicPercentText, (val) =>
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.SetMusicVolume(val);
                    }
                    else
                    {
                        PlayerPrefs.SetFloat("Settings_MusicVolume", val);
                        PlayerPrefs.Save();
                    }
                    if (pauseMusicPercentText != null)
                        pauseMusicPercentText.text = $"{Mathf.RoundToInt(val * 100f)}%";
                });

            // SFX Volume Slider
            CreateSettingsSliderRow(sec.transform, "Sound Effects", new Vector2(0f, -80f), font,
                out pauseSfxSlider, out pauseSfxPercentText, (val) =>
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.SetSFXVolume(val);
                    }
                    else
                    {
                        PlayerPrefs.SetFloat("Settings_SFXVolume", val);
                        PlayerPrefs.Save();
                    }
                    if (pauseSfxPercentText != null)
                        pauseSfxPercentText.text = $"{Mathf.RoundToInt(val * 100f)}%";
                });

            // Mute Button
            CreateSettingsButton(sec.transform, "Btn_MuteToggle", "Mute All Audio", new Vector2(0f, -125f),
                new Vector2(170f, 32f), font, out pauseMuteBtn, out pauseMuteBtnText, () =>
                {
                    bool currentMute = AudioManager.Instance != null
                        ? AudioManager.Instance.IsMuted
                        : PlayerPrefs.GetInt("Settings_MuteAll", 0) == 1;

                    bool nextMute = !currentMute;
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.SetMuted(nextMute);
                    }
                    else
                    {
                        PlayerPrefs.SetInt("Settings_MuteAll", nextMute ? 1 : 0);
                        PlayerPrefs.Save();
                    }
                    UpdatePauseMuteVisual(nextMute);
                });

            // Controls Header
            CreateSettingsHeader(sec.transform, "KEYBOARD CONTROLS", new Vector2(0f, -175f), font);

            // Controls Rows
            CreateControlsRow(sec.transform, "Move", "W, A, S, D  or  Arrow Keys", new Vector2(0f, -202f), font);
            CreateControlsRow(sec.transform, "Auto Attack", "Automatic when enemies in range", new Vector2(0f, -226f), font);
            CreateControlsRow(sec.transform, "Pause Game", "ESC", new Vector2(0f, -250f), font);
            CreateControlsRow(sec.transform, "Choose Upgrade", "Click  or  Keys 1, 2, 3, 4", new Vector2(0f, -274f), font);

            // Return Button
            CreateSettingsButton(sec.transform, "Btn_BackToStats", "Return to Stats", new Vector2(0f, -320f),
                new Vector2(190f, 36f), font, out _, out _, () =>
                {
                    ShowPauseTab(PauseMenuTab.Overview);
                });

            return sec;
        }

        private void CreateSettingsHeader(Transform parent, string text, Vector2 pos, TMP_FontAsset font)
        {
            GameObject go = new GameObject("Header_" + text.Replace(" ", "_"), typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(0f, 22f);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.980f, 0.800f, 0.082f, 1f); // #FACC15
            tmp.alignment = TextAlignmentOptions.Left;
        }

        private void CreateSettingsSliderRow(Transform parent, string labelText, Vector2 pos, TMP_FontAsset font,
            out Slider slider, out TMP_Text percentText, UnityEngine.Events.UnityAction<float> onValueChanged)
        {
            GameObject row = new GameObject("Row_" + labelText.Replace(" ", "_"), typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 1f);
            rowRT.anchorMax = new Vector2(1f, 1f);
            rowRT.pivot = new Vector2(0f, 1f);
            rowRT.anchoredPosition = pos;
            rowRT.sizeDelta = new Vector2(0f, 34f);

            // Label
            GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(row.transform, false);
            RectTransform lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0.5f);
            lblRT.anchorMax = new Vector2(0f, 0.5f);
            lblRT.pivot = new Vector2(0f, 0.5f);
            lblRT.anchoredPosition = new Vector2(4f, 0f);
            lblRT.sizeDelta = new Vector2(160f, 28f);

            TextMeshProUGUI lbl = lblGo.GetComponent<TextMeshProUGUI>();
            if (font != null) lbl.font = font;
            lbl.text = labelText;
            lbl.fontSize = 13;
            lbl.color = Color.white;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            // Slider container
            GameObject sldGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sldGo.transform.SetParent(row.transform, false);
            RectTransform sldRT = sldGo.GetComponent<RectTransform>();
            sldRT.anchorMin = new Vector2(0f, 0.5f);
            sldRT.anchorMax = new Vector2(0f, 0.5f);
            sldRT.pivot = new Vector2(0f, 0.5f);
            sldRT.anchoredPosition = new Vector2(170f, 0f);
            sldRT.sizeDelta = new Vector2(300f, 24f);

            slider = sldGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // Background track
            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(RoundedBorderImage));
            bgGo.transform.SetParent(sldGo.transform, false);
            RectTransform bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.35f);
            bgRT.anchorMax = new Vector2(1f, 0.65f);
            bgRT.sizeDelta = Vector2.zero;
            RoundedBorderImage bgImg = bgGo.GetComponent<RoundedBorderImage>();
            bgImg.CornerRadius = 6f;
            bgImg.BorderThickness = 0f;
            bgImg.FillColor = new Color(0.063f, 0.133f, 0.314f, 1f); // #102250

            // Fill Area & Fill
            GameObject faGo = new GameObject("Fill Area", typeof(RectTransform));
            faGo.transform.SetParent(sldGo.transform, false);
            RectTransform faRT = faGo.GetComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0f, 0.35f);
            faRT.anchorMax = new Vector2(1f, 0.65f);
            faRT.offsetMin = new Vector2(4f, 0f);
            faRT.offsetMax = new Vector2(-4f, 0f);

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(RoundedBorderImage));
            fillGo.transform.SetParent(faGo.transform, false);
            RectTransform fillRT = fillGo.GetComponent<RectTransform>();
            fillRT.sizeDelta = Vector2.zero;
            RoundedBorderImage fillImg = fillGo.GetComponent<RoundedBorderImage>();
            fillImg.CornerRadius = 4f;
            fillImg.BorderThickness = 0f;
            fillImg.FillColor = new Color(0.969f, 0.718f, 0.192f, 1f); // Gold fill #F7B731
            slider.fillRect = fillRT;

            // Handle Slide Area & Handle
            GameObject haGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            haGo.transform.SetParent(sldGo.transform, false);
            RectTransform haRT = haGo.GetComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero;
            haRT.anchorMax = Vector2.one;
            haRT.offsetMin = new Vector2(10f, 0f);
            haRT.offsetMax = new Vector2(-10f, 0f);

            GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(RoundedBorderImage));
            handleGo.transform.SetParent(haGo.transform, false);
            RectTransform handleRT = handleGo.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(18f, 22f);
            RoundedBorderImage handleImg = handleGo.GetComponent<RoundedBorderImage>();
            handleImg.CornerRadius = 6f;
            handleImg.BorderThickness = 2f;
            handleImg.BorderColor = Color.white;
            handleImg.FillColor = new Color(0.969f, 0.718f, 0.192f, 1f);
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;

            // Percent Text
            GameObject pctGo = new GameObject("Percent", typeof(RectTransform), typeof(TextMeshProUGUI));
            pctGo.transform.SetParent(row.transform, false);
            RectTransform pctRT = pctGo.GetComponent<RectTransform>();
            pctRT.anchorMin = new Vector2(0f, 0.5f);
            pctRT.anchorMax = new Vector2(0f, 0.5f);
            pctRT.pivot = new Vector2(0f, 0.5f);
            pctRT.anchoredPosition = new Vector2(485f, 0f);
            pctRT.sizeDelta = new Vector2(65f, 28f);

            TextMeshProUGUI pct = pctGo.GetComponent<TextMeshProUGUI>();
            if (font != null) pct.font = font;
            pct.fontSize = 13;
            pct.color = new Color(0.22f, 0.74f, 0.97f, 1f); // #38BDF8
            pct.alignment = TextAlignmentOptions.MidlineRight;
            percentText = pct;

            slider.onValueChanged.AddListener(onValueChanged);
        }

        private void CreateSettingsButton(Transform parent, string name, string label, Vector2 pos,
            Vector2 size, TMP_FontAsset font, out Button btn, out TMP_Text btnText, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(RoundedBorderImage), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            RoundedBorderImage img = btnGo.GetComponent<RoundedBorderImage>();
            img.CornerRadius = 6f;
            img.BorderThickness = 2f;
            img.BorderColor = new Color(0.353f, 0.557f, 0.965f, 1f);
            img.FillColor = new Color(0.125f, 0.251f, 0.596f, 1f);

            btn = btnGo.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);

            GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform txtRT = txtGo.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI txt = txtGo.GetComponent<TextMeshProUGUI>();
            if (font != null) txt.font = font;
            txt.text = label;
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            btnText = txt;
        }

        private void CreateControlsRow(Transform parent, string action, string keys, Vector2 pos, TMP_FontAsset font)
        {
            GameObject row = new GameObject("Ctrl_" + action.Replace(" ", "_"), typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 1f);
            rowRT.anchorMax = new Vector2(1f, 1f);
            rowRT.pivot = new Vector2(0f, 1f);
            rowRT.anchoredPosition = pos;
            rowRT.sizeDelta = new Vector2(0f, 22f);

            GameObject actGo = new GameObject("Action", typeof(RectTransform), typeof(TextMeshProUGUI));
            actGo.transform.SetParent(row.transform, false);
            RectTransform actRT = actGo.GetComponent<RectTransform>();
            actRT.anchorMin = new Vector2(0f, 0.5f);
            actRT.anchorMax = new Vector2(0f, 0.5f);
            actRT.pivot = new Vector2(0f, 0.5f);
            actRT.anchoredPosition = new Vector2(10f, 0f);
            actRT.sizeDelta = new Vector2(170f, 22f);

            TextMeshProUGUI actTxt = actGo.GetComponent<TextMeshProUGUI>();
            if (font != null) actTxt.font = font;
            actTxt.text = "• " + action;
            actTxt.fontSize = 13;
            actTxt.color = new Color(0.796f, 0.835f, 0.882f, 1f);
            actTxt.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject keyGo = new GameObject("Keys", typeof(RectTransform), typeof(TextMeshProUGUI));
            keyGo.transform.SetParent(row.transform, false);
            RectTransform keyRT = keyGo.GetComponent<RectTransform>();
            keyRT.anchorMin = new Vector2(0f, 0.5f);
            keyRT.anchorMax = new Vector2(1f, 0.5f);
            keyRT.pivot = new Vector2(0f, 0.5f);
            keyRT.offsetMin = new Vector2(185f, -11f);
            keyRT.offsetMax = new Vector2(-10f, 11f);

            TextMeshProUGUI keyTxt = keyGo.GetComponent<TextMeshProUGUI>();
            if (font != null) keyTxt.font = font;
            keyTxt.text = keys;
            keyTxt.fontSize = 13;
            keyTxt.color = new Color(0.980f, 0.800f, 0.082f, 1f); // #FACC15
            keyTxt.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void RefreshPauseSettingsUI()
        {
            float mVol = AudioManager.Instance != null
                ? AudioManager.Instance.MusicVolume
                : PlayerPrefs.GetFloat("Settings_MusicVolume", 0.7f);

            float sVol = AudioManager.Instance != null
                ? AudioManager.Instance.SFXVolume
                : PlayerPrefs.GetFloat("Settings_SFXVolume", 0.8f);

            bool isMuted = AudioManager.Instance != null
                ? AudioManager.Instance.IsMuted
                : PlayerPrefs.GetInt("Settings_MuteAll", 0) == 1;

            if (pauseMusicSlider != null)
            {
                pauseMusicSlider.SetValueWithoutNotify(mVol);
                if (pauseMusicPercentText != null) pauseMusicPercentText.text = $"{Mathf.RoundToInt(mVol * 100f)}%";
            }

            if (pauseSfxSlider != null)
            {
                pauseSfxSlider.SetValueWithoutNotify(sVol);
                if (pauseSfxPercentText != null) pauseSfxPercentText.text = $"{Mathf.RoundToInt(sVol * 100f)}%";
            }

            UpdatePauseMuteVisual(isMuted);
        }

        private void UpdatePauseMuteVisual(bool isMuted)
        {
            if (pauseMuteBtnText != null)
            {
                pauseMuteBtnText.text = isMuted ? "Unmute Audio" : "Mute All Audio";
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
            if (isGameOver || isPaused) return;
            PixelSurvivor.Audio.AudioManager.Instance?.PlayUiClick();

            isPaused = true;
            TimeScaleManager.Instance?.AcquirePause("PauseMenu");

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(true);
                ShowPauseTab(PauseMenuTab.Overview);
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

            Transform pauseCard = pauseMenuCanvas != null ? pauseMenuCanvas.transform.Find("Card") : null;
            if (pauseCard != null)
            {
                Transform statsSec = pauseCard.Find("Section_Stats");
                if (statsSec != null)
                {
                    EnsureStatsLabelsAndValues(statsSec);
                }
            }

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

            // Attack & Cooldown
            if (weaponCtrl != null && weaponCtrl.EquippedWeapons != null && weaponCtrl.EquippedWeapons.Count > 0)
            {
                WeaponBase primaryWeapon = weaponCtrl.EquippedWeapons[0];
                float dmg = primaryWeapon.BaseDamage * weaponCtrl.DamageMultiplier;
                float cd = Mathf.Max(0.1f, primaryWeapon.BaseCooldown * weaponCtrl.CooldownMultiplier);
                if (pauseAttackStatText != null) pauseAttackStatText.text = Mathf.RoundToInt(dmg).ToString();
                if (pauseCooldownStatText != null) pauseCooldownStatText.text = $"{cd:F1}s";
            }
            else
            {
                if (pauseAttackStatText != null) pauseAttackStatText.text = "0";
                if (pauseCooldownStatText != null) pauseCooldownStatText.text = "0s";
            }

            // --- 1. WEAPONS SECTION (Multi-column alignment & dynamic height) ---
            if (pauseCard == null && pauseMenuCanvas != null) pauseCard = pauseMenuCanvas.transform.Find("Card");
            int numWeapons = weaponCtrl != null && weaponCtrl.EquippedWeapons != null && weaponCtrl.EquippedWeapons.Count > 0
                ? weaponCtrl.EquippedWeapons.Count : 1;
            float wepLinesHeight = Mathf.Max(24f, numWeapons * 24f);
            float wepSectionHeight = 32f + wepLinesHeight + 8f;

            if (pauseCard != null)
            {
                Transform wepSec = pauseCard.Find("Section_Weapons");
                if (wepSec != null)
                {
                    RectTransform wepSecRT = wepSec.GetComponent<RectTransform>();
                    if (wepSecRT != null)
                    {
                        wepSecRT.anchorMin = new Vector2(0.5f, 1f);
                        wepSecRT.anchorMax = new Vector2(0.5f, 1f);
                        wepSecRT.pivot = new Vector2(0.5f, 1f);
                        wepSecRT.anchoredPosition = new Vector2(0f, -118f);
                        wepSecRT.sizeDelta = new Vector2(580f, wepSectionHeight);
                    }

                    // Align Headers
                    Transform colDmg = wepSec.Find("Col_Damage");
                    if (colDmg != null)
                    {
                        RectTransform cdRT = colDmg.GetComponent<RectTransform>();
                        cdRT.anchorMin = new Vector2(1f, 1f);
                        cdRT.anchorMax = new Vector2(1f, 1f);
                        cdRT.pivot = new Vector2(1f, 1f);
                        cdRT.anchoredPosition = new Vector2(-110f, -6f);
                        cdRT.sizeDelta = new Vector2(90f, 20f);
                        TMP_Text t = colDmg.GetComponent<TMP_Text>();
                        if (t != null) { t.text = "Damage"; t.alignment = TextAlignmentOptions.TopRight; }
                    }
                    Transform colDps = wepSec.Find("Col_DPS");
                    if (colDps != null)
                    {
                        RectTransform cdRT = colDps.GetComponent<RectTransform>();
                        cdRT.anchorMin = new Vector2(1f, 1f);
                        cdRT.anchorMax = new Vector2(1f, 1f);
                        cdRT.pivot = new Vector2(1f, 1f);
                        cdRT.anchoredPosition = new Vector2(-10f, -6f);
                        cdRT.sizeDelta = new Vector2(90f, 20f);
                        TMP_Text t = colDps.GetComponent<TMP_Text>();
                        if (t != null) { t.text = "Per second"; t.alignment = TextAlignmentOptions.TopRight; }
                    }

                    // Weapon_Row_0 container: top anchored so it expands down
                    Transform row0 = wepSec.Find("Weapon_Row_0") ?? wepSec.Find("Weapon_Row");
                    if (row0 != null)
                    {
                        RectTransform row0RT = row0.GetComponent<RectTransform>();
                        if (row0RT != null)
                        {
                            row0RT.anchorMin = new Vector2(0f, 1f);
                            row0RT.anchorMax = new Vector2(1f, 1f);
                            row0RT.pivot = new Vector2(0.5f, 1f);
                            row0RT.anchoredPosition = new Vector2(0f, -28f);
                            row0RT.sizeDelta = new Vector2(-24f, wepLinesHeight);
                        }

                        // Remove / deactivate Badge_Melee
                        Transform badgeMelee = row0.Find("Badge_Melee");
                        if (badgeMelee != null) badgeMelee.gameObject.SetActive(false);

                        // Auto-locate texts if null
                        if (pauseWeaponNameText == null) pauseWeaponNameText = row0.Find("Weapon_Name")?.GetComponent<TMP_Text>();
                        if (pauseWeaponDamageText == null) pauseWeaponDamageText = row0.Find("Weapon_Damage")?.GetComponent<TMP_Text>();
                        if (pauseWeaponDpsText == null) pauseWeaponDpsText = row0.Find("Weapon_DPS")?.GetComponent<TMP_Text>();
                    }
                }
            }

            // Configure text layout
            if (pauseWeaponNameText != null)
            {
                RectTransform nRT = pauseWeaponNameText.GetComponent<RectTransform>();
                nRT.anchorMin = new Vector2(0f, 1f);
                nRT.anchorMax = new Vector2(0f, 1f);
                nRT.pivot = new Vector2(0f, 1f);
                nRT.anchoredPosition = new Vector2(12f, 0f);
                nRT.sizeDelta = new Vector2(360f, wepLinesHeight);
                pauseWeaponNameText.alignment = TextAlignmentOptions.TopLeft;
                pauseWeaponNameText.fontSize = 13f;
                pauseWeaponNameText.lineSpacing = 4f;
                pauseWeaponNameText.textWrappingMode = TextWrappingModes.NoWrap;
            }
            if (pauseWeaponDamageText != null)
            {
                RectTransform dRT = pauseWeaponDamageText.GetComponent<RectTransform>();
                dRT.anchorMin = new Vector2(1f, 1f);
                dRT.anchorMax = new Vector2(1f, 1f);
                dRT.pivot = new Vector2(1f, 1f);
                dRT.anchoredPosition = new Vector2(-110f, 0f);
                dRT.sizeDelta = new Vector2(90f, wepLinesHeight);
                pauseWeaponDamageText.alignment = TextAlignmentOptions.TopRight;
                pauseWeaponDamageText.fontSize = 13f;
                pauseWeaponDamageText.lineSpacing = 4f;
                pauseWeaponDamageText.textWrappingMode = TextWrappingModes.NoWrap;
            }
            if (pauseWeaponDpsText != null)
            {
                RectTransform dpRT = pauseWeaponDpsText.GetComponent<RectTransform>();
                dpRT.anchorMin = new Vector2(1f, 1f);
                dpRT.anchorMax = new Vector2(1f, 1f);
                dpRT.pivot = new Vector2(1f, 1f);
                dpRT.anchoredPosition = new Vector2(-10f, 0f);
                dpRT.sizeDelta = new Vector2(90f, wepLinesHeight);
                pauseWeaponDpsText.alignment = TextAlignmentOptions.TopRight;
                pauseWeaponDpsText.fontSize = 13f;
                pauseWeaponDpsText.lineSpacing = 4f;
                pauseWeaponDpsText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // Populate multi-column values
            if (weaponCtrl != null && weaponCtrl.EquippedWeapons != null && weaponCtrl.EquippedWeapons.Count > 0)
            {
                var sbN = new System.Text.StringBuilder();
                var sbD = new System.Text.StringBuilder();
                var sbP = new System.Text.StringBuilder();

                for (int i = 0; i < weaponCtrl.EquippedWeapons.Count; i++)
                {
                    var w = weaponCtrl.EquippedWeapons[i];
                    if (w == null) continue;
                    float wDmg = w.BaseDamage * weaponCtrl.DamageMultiplier;
                    float wCd = Mathf.Max(0.1f, w.BaseCooldown * weaponCtrl.CooldownMultiplier);
                    float wDps = wDmg / wCd;

                    sbN.AppendLine($"• {w.weaponName} - Lv {w.currentLevel}");
                    sbD.AppendLine($"{Mathf.RoundToInt(wDmg)}");
                    sbP.AppendLine($"{wDps:F1}/s");
                }

                if (pauseWeaponNameText != null) pauseWeaponNameText.text = sbN.ToString().TrimEnd();
                if (pauseWeaponDamageText != null) pauseWeaponDamageText.text = sbD.ToString().TrimEnd();
                if (pauseWeaponDpsText != null) pauseWeaponDpsText.text = sbP.ToString().TrimEnd();
            }
            else
            {
                if (pauseWeaponNameText != null) pauseWeaponNameText.text = "• None";
                if (pauseWeaponDamageText != null) pauseWeaponDamageText.text = "0";
                if (pauseWeaponDpsText != null) pauseWeaponDpsText.text = "0/s";
            }

            // --- 2. STATS SECTION (Positioned dynamically under Weapons) ---
            float statsPosY = -118f - wepSectionHeight - 12f;
            if (pauseCard != null)
            {
                Transform statsSec = pauseCard.Find("Section_Stats");
                if (statsSec != null)
                {
                    RectTransform statsSecRT = statsSec.GetComponent<RectTransform>();
                    if (statsSecRT != null)
                    {
                        statsSecRT.anchorMin = new Vector2(0.5f, 1f);
                        statsSecRT.anchorMax = new Vector2(0.5f, 1f);
                        statsSecRT.pivot = new Vector2(0.5f, 1f);
                        statsSecRT.anchoredPosition = new Vector2(0f, statsPosY);
                        statsSecRT.sizeDelta = new Vector2(580f, 175f);
                    }
                }
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

            // --- 3. ITEMS SECTION (Positioned dynamically under Stats & widened) ---
            var passives = PassiveController.Instance != null ? PassiveController.Instance.EquippedSlots : null;
            int numItems = passives != null && passives.Count > 0 ? passives.Count : 1;
            float itemLinesHeight = Mathf.Max(24f, numItems * 24f);
            float itemSectionHeight = 32f + itemLinesHeight + 10f;
            float itemPosY = statsPosY - 175f - 12f;

            if (pauseCard != null)
            {
                Transform itemSec = pauseCard.Find("Section_Item");
                if (itemSec != null)
                {
                    RectTransform itemSecRT = itemSec.GetComponent<RectTransform>();
                    if (itemSecRT != null)
                    {
                        itemSecRT.anchorMin = new Vector2(0.5f, 1f);
                        itemSecRT.anchorMax = new Vector2(0.5f, 1f);
                        itemSecRT.pivot = new Vector2(0.5f, 1f);
                        itemSecRT.anchoredPosition = new Vector2(0f, itemPosY);
                        itemSecRT.sizeDelta = new Vector2(580f, itemSectionHeight);
                    }

                    if (pauseItemEmptyText == null)
                    {
                        pauseItemEmptyText = itemSec.Find("Text_NoItem")?.GetComponent<TMP_Text>();
                    }
                }
            }

            if (pauseItemEmptyText != null)
            {
                RectTransform itmRT = pauseItemEmptyText.GetComponent<RectTransform>();
                itmRT.anchorMin = new Vector2(0f, 1f);
                itmRT.anchorMax = new Vector2(0f, 1f);
                itmRT.pivot = new Vector2(0f, 1f);
                itmRT.anchoredPosition = new Vector2(12f, -30f);
                itmRT.sizeDelta = new Vector2(556f, itemLinesHeight);
                pauseItemEmptyText.alignment = TextAlignmentOptions.TopLeft;
                pauseItemEmptyText.fontSize = 13f;
                pauseItemEmptyText.lineSpacing = 4f;
                pauseItemEmptyText.textWrappingMode = TextWrappingModes.NoWrap;

                if (passives != null && passives.Count > 0)
                {
                    var sbI = new System.Text.StringBuilder();
                    for (int i = 0; i < passives.Count; i++)
                    {
                        var slot = passives[i];
                        if (slot == null) continue;
                        string pName = PassiveController.GetPassiveName(slot.type);
                        string desc = GetPassiveSummaryDescription(slot.type, slot.level);
                        sbI.AppendLine($"• <color=#FFFFFF>{pName}</color> - Lv {slot.level} — <color=#9CA3AF>{desc}</color>");
                    }
                    pauseItemEmptyText.text = sbI.ToString().TrimEnd();
                }
                else
                {
                    pauseItemEmptyText.text = "<color=#9CA3AF>No items equipped.</color>";
                }
                pauseItemEmptyText.gameObject.SetActive(true);
            }

            // --- 4. DYNAMIC PAUSE CARD HEIGHT ---
            if (pauseCard != null)
            {
                RectTransform cardRT = pauseCard.GetComponent<RectTransform>();
                if (cardRT != null)
                {
                    float totalPauseHeight = Mathf.Abs(itemPosY) + itemSectionHeight + 20f;
                    cardRT.sizeDelta = new Vector2(640f, totalPauseHeight);
                }
            }
        }

        private void EnsureStatsLabelsAndValues(Transform statsSec)
        {
            if (statsSec == null) return;

            // 1. Column 1: HP, Cooldown, Critical
            SetStatLabel(statsSec, "Stat_HP", "HP");
            SetStatLabel(statsSec, "Stat_Cooldown", "Cooldown");
            SetStatLabel(statsSec, "Stat_Critical", "Critical");

            // 2. Column 2: Attack, Range, Dmg Reduct
            SetStatLabel(statsSec, "Stat_Attack", "Attack");
            SetStatLabel(statsSec, "Stat_Range", "Range");
            SetStatLabel(statsSec, "Stat_Dmg Reduct", "Dmg Reduct");

            // 3. Column 3: Speed, Knockback, Time
            SetStatLabel(statsSec, "Stat_Speed", "Speed");
            SetStatLabel(statsSec, "Stat_Knockback", "Knockback");
            SetStatLabel(statsSec, "Stat_Time", "Time");

            // Always ensure value text references bind to the Value child, never Label
            pauseHpStatText = statsSec.Find("Stat_HP/Value")?.GetComponent<TMP_Text>() ?? pauseHpStatText;
            pauseCooldownStatText = statsSec.Find("Stat_Cooldown/Value")?.GetComponent<TMP_Text>() ?? pauseCooldownStatText;
            pauseCriticalStatText = statsSec.Find("Stat_Critical/Value")?.GetComponent<TMP_Text>() ?? pauseCriticalStatText;
            pauseAttackStatText = statsSec.Find("Stat_Attack/Value")?.GetComponent<TMP_Text>() ?? pauseAttackStatText;
            pauseRangeStatText = statsSec.Find("Stat_Range/Value")?.GetComponent<TMP_Text>() ?? pauseRangeStatText;
            pauseDmgReductionStatText = statsSec.Find("Stat_Dmg Reduct/Value")?.GetComponent<TMP_Text>() ?? pauseDmgReductionStatText;
            pauseSpeedStatText = statsSec.Find("Stat_Speed/Value")?.GetComponent<TMP_Text>() ?? pauseSpeedStatText;
            pauseKnockbackStatText = statsSec.Find("Stat_Knockback/Value")?.GetComponent<TMP_Text>() ?? pauseKnockbackStatText;
            pauseTimeStatText = statsSec.Find("Stat_Time/Value")?.GetComponent<TMP_Text>() ?? pauseTimeStatText;
        }

        private static void SetStatLabel(Transform statsSec, string statObjName, string labelText)
        {
            TMP_Text label = statsSec.Find($"{statObjName}/Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = labelText;
            }
        }

        public void Resume()
        {
            if (isGameOver || !isPaused) return;
            PixelSurvivor.Audio.AudioManager.Instance?.PlayUiClick();

            isPaused = false;
            TimeScaleManager.Instance?.ReleasePause("PauseMenu");

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
                ShowPauseTab(PauseMenuTab.Overview);
            }

            Debug.Log("<color=#10B981>[Game Resumed]</color> Time running.");
        }

        public void TriggerGameOver()
        {
            if (isGameOver) return;
            HandlePlayerDied();
        }

        public void TriggerTimeoutGameOver()
        {
            if (isGameOver) return;
            HandlePlayerDied();

            if (gameOverCanvas != null)
            {
                var titleText = gameOverCanvas.transform.Find("Card/Title")?.GetComponent<TMP_Text>();
                if (titleText != null)
                {
                    titleText.text = "Time Expired...";
                    titleText.color = new Color(0.95f, 0.35f, 0.35f, 1f); // Warning Red
                }

                if (defeatRunSummaryHeader != null)
                {
                    defeatRunSummaryHeader.text = "18:00 REACHED · The Ancient Depths have sealed your fate!";
                }
            }

            Debug.Log("<color=#EF4444>[DEFEAT: TIMEOUT]</color> 18:00 Match Timeout reached without slaying The Warden! Game Over.");
        }

        public void TriggerVictory()
        {
            if (isGameOver) return;
            isGameOver = true;
            isPaused = false;
            TimeScaleManager.Instance?.AcquirePause("VictoryMenu");

            if (waveSpawner != null)
            {
                waveSpawner.CompleteRun();
            }

            if (gameOverCanvas != null)
            {
                var titleText = gameOverCanvas.transform.Find("Card/Title")?.GetComponent<TMP_Text>();
                if (titleText != null)
                {
                    titleText.text = "VICTORY!";
                    titleText.color = new Color(0.1f, 0.95f, 0.6f, 1f); // Vibrant Emerald Green
                }

                gameOverCanvas.SetActive(true);
                if (gameOverStatsText != null) gameOverStatsText.text = GetGameOverStatsString();
                UpdateDefeatModalDetails();

                if (defeatBossesText != null) defeatBossesText.text = "3"; // All 3 major encounters cleared
                if (defeatEmeraldsFormulaText != null)
                {
                    defeatEmeraldsFormulaText.text = "15m-18m Clear · The Warden Slain · Victory Bonus +100";
                }
                if (defeatRunSummaryHeader != null)
                {
                    PlayerController p = FindAnyObjectByType<PlayerController>();
                    string cName = p != null ? p.characterName : "Steve";
                    LevelUpManager lm = FindAnyObjectByType<LevelUpManager>();
                    int lvl = lm != null ? lm.CurrentLevel : 1;
                    defeatRunSummaryHeader.text = $"{cName} · Lv. {lvl} · CONQUERED THE ANCIENT DEPTHS!";
                }
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log("<color=#10B981>[VICTORY]</color> Match Victory triggered! Victory modal displayed.");
        }

        private void HandlePlayerDied()
        {
            isGameOver = true;
            isPaused = false;
            TimeScaleManager.Instance?.AcquirePause("DefeatMenu");

            if (waveSpawner != null)
            {
                waveSpawner.CompleteRun();
            }

            if (gameOverCanvas != null)
            {
                var titleText = gameOverCanvas.transform.Find("Card/Title")?.GetComponent<TMP_Text>();
                if (titleText != null)
                {
                    titleText.text = "You Fell...";
                    titleText.color = Color.white;
                }

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

            Transform card = gameOverCanvas != null ? gameOverCanvas.transform.Find("Card") : null;
            if (card == null) return;

            // Hide Try Again button and center Back to menu button
            Button btnRetry = card.Find("Action_Buttons/Btn_Retry")?.GetComponent<Button>() ??
                              card.Find("Bottom_Buttons_Row/Btn_Retry")?.GetComponent<Button>();
            if (btnRetry != null) btnRetry.gameObject.SetActive(false);

            Button btnBack = card.Find("Action_Buttons/Btn_Quit")?.GetComponent<Button>() ??
                             card.Find("Bottom_Buttons_Row/Btn_BackToMenu")?.GetComponent<Button>();
            if (btnBack != null)
            {
                RectTransform backRT = btnBack.GetComponent<RectTransform>();
                if (backRT != null) backRT.anchoredPosition = new Vector2(0f, 0f);
            }

            // Calculate total damage across all equipped weapons
            int totalDamageDealt = 0;
            if (weaponCtrl != null && weaponCtrl.EquippedWeapons != null)
            {
                for (int i = 0; i < weaponCtrl.EquippedWeapons.Count; i++)
                {
                    var w = weaponCtrl.EquippedWeapons[i];
                    if (w == null) continue;
                    float dmg = w.BaseDamage * weaponCtrl.DamageMultiplier;
                    totalDamageDealt += Mathf.RoundToInt(dmg * Mathf.Max(5, totalSeconds * 1.5f));
                }
            }
            if (totalDamageDealt == 0) totalDamageDealt = Mathf.RoundToInt(25f * Mathf.Max(10, totalSeconds * 1.5f));

            string charName = player != null ? player.characterName : "Steve";

            // 1. Run Summary Subtitle / Header (Steve · Lv. 4 · 1,038 damage in total)
            Transform summaryHdrTrans = card.Find("Run_Summary_Header") ??
                                        card.Find("Section_Run_Summary/Subtitle") ??
                                        card.Find("Section_Weapons_Box/Subtitle");
            if (summaryHdrTrans != null)
            {
                if (summaryHdrTrans.parent != card) summaryHdrTrans.SetParent(card, false);
                summaryHdrTrans.name = "Run_Summary_Header";
                RectTransform shRT = summaryHdrTrans.GetComponent<RectTransform>();
                shRT.anchorMin = new Vector2(0.5f, 1f);
                shRT.anchorMax = new Vector2(0.5f, 1f);
                shRT.pivot = new Vector2(0.5f, 1f);
                shRT.anchoredPosition = new Vector2(0f, -296f);
                shRT.sizeDelta = new Vector2(580f, 22f);
                defeatRunSummaryHeader = summaryHdrTrans.GetComponent<TMP_Text>();
                if (defeatRunSummaryHeader != null)
                {
                    defeatRunSummaryHeader.text = $"{charName} · Lv. {level} · {totalDamageDealt:N0} damage in total";
                    defeatRunSummaryHeader.alignment = TextAlignmentOptions.Center;
                    defeatRunSummaryHeader.fontSize = 13f;
                }
                summaryHdrTrans.gameObject.SetActive(true);
            }

            // 2. Box 1: WEAPONS BOX (Section_Weapons_Box / Section_Run_Summary)
            Transform wepBoxTrans = card.Find("Section_Weapons_Box") ?? card.Find("Section_Run_Summary");
            int numWeps = weaponCtrl != null && weaponCtrl.EquippedWeapons != null && weaponCtrl.EquippedWeapons.Count > 0
                ? weaponCtrl.EquippedWeapons.Count : 1;
            float wepLinesHeight = Mathf.Max(24f, numWeps * 24f);
            float box1Height = 32f + wepLinesHeight + 8f;

            if (wepBoxTrans != null)
            {
                wepBoxTrans.name = "Section_Weapons_Box";
                wepBoxTrans.gameObject.SetActive(true);

                RectTransform wbRT = wepBoxTrans.GetComponent<RectTransform>();
                wbRT.anchorMin = new Vector2(0.5f, 1f);
                wbRT.anchorMax = new Vector2(0.5f, 1f);
                wbRT.pivot = new Vector2(0.5f, 1f);
                wbRT.anchoredPosition = new Vector2(0f, -324f);
                wbRT.sizeDelta = new Vector2(580f, box1Height);

                // Title: WEAPONS
                Transform titleTrans = wepBoxTrans.Find("Title");
                if (titleTrans != null)
                {
                    RectTransform tRT = titleTrans.GetComponent<RectTransform>();
                    tRT.anchorMin = new Vector2(0f, 1f);
                    tRT.anchorMax = new Vector2(0f, 1f);
                    tRT.pivot = new Vector2(0f, 1f);
                    tRT.anchoredPosition = new Vector2(12f, -6f);
                    tRT.sizeDelta = new Vector2(150f, 20f);
                    TMP_Text t = titleTrans.GetComponent<TMP_Text>();
                    if (t != null)
                    {
                        t.text = "<color=#93C5FD><b>WEAPONS</b></color>";
                        t.fontSize = 13f;
                        t.alignment = TextAlignmentOptions.TopLeft;
                    }
                    titleTrans.gameObject.SetActive(true);
                }

                // Header Damage
                Transform colDmg = wepBoxTrans.Find("Col_Damage") ?? wepBoxTrans.Find("Header_Damage");
                if (colDmg != null)
                {
                    colDmg.name = "Col_Damage";
                    RectTransform cdRT = colDmg.GetComponent<RectTransform>();
                    cdRT.anchorMin = new Vector2(1f, 1f);
                    cdRT.anchorMax = new Vector2(1f, 1f);
                    cdRT.pivot = new Vector2(1f, 1f);
                    cdRT.anchoredPosition = new Vector2(-110f, -6f);
                    cdRT.sizeDelta = new Vector2(90f, 20f);
                    TMP_Text t = colDmg.GetComponent<TMP_Text>();
                    if (t != null)
                    {
                        t.text = "Damage";
                        t.fontSize = 12f;
                        t.color = new Color(0.61f, 0.64f, 0.69f, 1f);
                        t.alignment = TextAlignmentOptions.TopRight;
                    }
                    colDmg.gameObject.SetActive(true);
                }

                // Header DPS
                Transform colDps = wepBoxTrans.Find("Col_DPS") ?? wepBoxTrans.Find("Header_DPS");
                if (colDps != null)
                {
                    colDps.name = "Col_DPS";
                    RectTransform cdRT = colDps.GetComponent<RectTransform>();
                    cdRT.anchorMin = new Vector2(1f, 1f);
                    cdRT.anchorMax = new Vector2(1f, 1f);
                    cdRT.pivot = new Vector2(1f, 1f);
                    cdRT.anchoredPosition = new Vector2(-10f, -6f);
                    cdRT.sizeDelta = new Vector2(90f, 20f);
                    TMP_Text t = colDps.GetComponent<TMP_Text>();
                    if (t != null)
                    {
                        t.text = "Per second";
                        t.fontSize = 12f;
                        t.color = new Color(0.61f, 0.64f, 0.69f, 1f);
                        t.alignment = TextAlignmentOptions.TopRight;
                    }
                    colDps.gameObject.SetActive(true);
                }

                // Weapon columns container (Weapon_Stat_Line)
                Transform lineTrans = wepBoxTrans.Find("Weapon_Stat_Line") ?? wepBoxTrans.Find("Weapon_Row_0");
                if (lineTrans != null)
                {
                    lineTrans.name = "Weapon_Stat_Line";
                    RectTransform lRT = lineTrans.GetComponent<RectTransform>();
                    lRT.anchorMin = new Vector2(0f, 1f);
                    lRT.anchorMax = new Vector2(1f, 1f);
                    lRT.pivot = new Vector2(0.5f, 1f);
                    lRT.anchoredPosition = new Vector2(0f, -28f);
                    lRT.sizeDelta = new Vector2(-24f, wepLinesHeight);
                    lineTrans.gameObject.SetActive(true);

                    // Name
                    Transform nameTrans = lineTrans.Find("Name") ?? lineTrans.Find("Weapon_Name");
                    if (nameTrans != null)
                    {
                        nameTrans.name = "Name";
                        RectTransform nRT = nameTrans.GetComponent<RectTransform>();
                        nRT.anchorMin = new Vector2(0f, 1f);
                        nRT.anchorMax = new Vector2(0f, 1f);
                        nRT.pivot = new Vector2(0f, 1f);
                        nRT.anchoredPosition = new Vector2(12f, 0f);
                        nRT.sizeDelta = new Vector2(360f, wepLinesHeight);
                        defeatWeaponNameText = nameTrans.GetComponent<TMP_Text>();
                        if (defeatWeaponNameText != null)
                        {
                            defeatWeaponNameText.alignment = TextAlignmentOptions.TopLeft;
                            defeatWeaponNameText.fontSize = 13f;
                            defeatWeaponNameText.lineSpacing = 4f;
                            defeatWeaponNameText.textWrappingMode = TextWrappingModes.NoWrap;
                        }
                    }

                    // Damage
                    Transform dmgTrans = lineTrans.Find("Damage") ?? lineTrans.Find("Weapon_Damage");
                    if (dmgTrans != null)
                    {
                        dmgTrans.name = "Damage";
                        RectTransform dRT = dmgTrans.GetComponent<RectTransform>();
                        dRT.anchorMin = new Vector2(1f, 1f);
                        dRT.anchorMax = new Vector2(1f, 1f);
                        dRT.pivot = new Vector2(1f, 1f);
                        dRT.anchoredPosition = new Vector2(-110f, 0f);
                        dRT.sizeDelta = new Vector2(90f, wepLinesHeight);
                        defeatWeaponDamageText = dmgTrans.GetComponent<TMP_Text>();
                        if (defeatWeaponDamageText != null)
                        {
                            defeatWeaponDamageText.alignment = TextAlignmentOptions.TopRight;
                            defeatWeaponDamageText.fontSize = 13f;
                            defeatWeaponDamageText.lineSpacing = 4f;
                            defeatWeaponDamageText.textWrappingMode = TextWrappingModes.NoWrap;
                        }
                    }

                    // DPS
                    Transform dpsTrans = lineTrans.Find("DPS") ?? lineTrans.Find("Weapon_DPS");
                    if (dpsTrans != null)
                    {
                        dpsTrans.name = "DPS";
                        RectTransform dRT = dpsTrans.GetComponent<RectTransform>();
                        dRT.anchorMin = new Vector2(1f, 1f);
                        dRT.anchorMax = new Vector2(1f, 1f);
                        dRT.pivot = new Vector2(1f, 1f);
                        dRT.anchoredPosition = new Vector2(-10f, 0f);
                        dRT.sizeDelta = new Vector2(90f, wepLinesHeight);
                        defeatWeaponDpsText = dpsTrans.GetComponent<TMP_Text>();
                        if (defeatWeaponDpsText != null)
                        {
                            defeatWeaponDpsText.alignment = TextAlignmentOptions.TopRight;
                            defeatWeaponDpsText.fontSize = 13f;
                            defeatWeaponDpsText.lineSpacing = 4f;
                            defeatWeaponDpsText.textWrappingMode = TextWrappingModes.NoWrap;
                        }
                    }
                }

                // Fill weapon rows
                if (weaponCtrl != null && weaponCtrl.EquippedWeapons != null && weaponCtrl.EquippedWeapons.Count > 0)
                {
                    var sbN = new System.Text.StringBuilder();
                    var sbD = new System.Text.StringBuilder();
                    var sbP = new System.Text.StringBuilder();

                    for (int i = 0; i < weaponCtrl.EquippedWeapons.Count; i++)
                    {
                        var w = weaponCtrl.EquippedWeapons[i];
                        if (w == null) continue;
                        float dmg = w.BaseDamage * weaponCtrl.DamageMultiplier;
                        float cd = Mathf.Max(0.1f, w.BaseCooldown * weaponCtrl.CooldownMultiplier);
                        float dps = dmg / cd;
                        int weaponTotalDamage = Mathf.RoundToInt(dmg * Mathf.Max(5, totalSeconds * 1.5f));

                        sbN.AppendLine($"• {w.weaponName} - Lv {w.currentLevel}");
                        sbD.AppendLine($"{weaponTotalDamage:N0}");
                        sbP.AppendLine($"{dps:F0}/s");
                    }

                    if (defeatWeaponNameText != null) defeatWeaponNameText.text = sbN.ToString().TrimEnd();
                    if (defeatWeaponDamageText != null) defeatWeaponDamageText.text = sbD.ToString().TrimEnd();
                    if (defeatWeaponDpsText != null) defeatWeaponDpsText.text = sbP.ToString().TrimEnd();
                }
                else
                {
                    if (defeatWeaponNameText != null) defeatWeaponNameText.text = "• None";
                    if (defeatWeaponDamageText != null) defeatWeaponDamageText.text = "0";
                    if (defeatWeaponDpsText != null) defeatWeaponDpsText.text = "0/s";
                }
            }

            // 3. Box 2: ITEMS BOX (Section_Items_Box / Section_Weapons_Row)
            Transform itemBoxTrans = card.Find("Section_Items_Box") ?? card.Find("Section_Weapons_Row");
            var passives = PassiveController.Instance != null ? PassiveController.Instance.EquippedSlots : null;
            int numItems = passives != null && passives.Count > 0 ? passives.Count : 1;
            float itemLinesHeight = Mathf.Max(24f, numItems * 24f);
            float box2Height = 32f + itemLinesHeight + 8f;
            float box2PosY = -324f - box1Height - 12f;

            if (itemBoxTrans != null)
            {
                itemBoxTrans.name = "Section_Items_Box";
                itemBoxTrans.gameObject.SetActive(true);

                RectTransform ibRT = itemBoxTrans.GetComponent<RectTransform>();
                ibRT.anchorMin = new Vector2(0.5f, 1f);
                ibRT.anchorMax = new Vector2(0.5f, 1f);
                ibRT.pivot = new Vector2(0.5f, 1f);
                ibRT.anchoredPosition = new Vector2(0f, box2PosY);
                ibRT.sizeDelta = new Vector2(580f, box2Height);

                // Title: ITEMS
                Transform itemTitle = itemBoxTrans.Find("Title");
                if (itemTitle != null)
                {
                    RectTransform tRT = itemTitle.GetComponent<RectTransform>();
                    tRT.anchorMin = new Vector2(0f, 1f);
                    tRT.anchorMax = new Vector2(0f, 1f);
                    tRT.pivot = new Vector2(0f, 1f);
                    tRT.anchoredPosition = new Vector2(12f, -6f);
                    tRT.sizeDelta = new Vector2(150f, 20f);
                    TMP_Text t = itemTitle.GetComponent<TMP_Text>();
                    if (t != null)
                    {
                        t.text = "<color=#93C5FD><b>ITEMS</b></color>";
                        t.fontSize = 13f;
                        t.alignment = TextAlignmentOptions.TopLeft;
                    }
                    itemTitle.gameObject.SetActive(true);
                }

                // Re-parent Item_Status from wepBoxTrans to itemBoxTrans if needed to avoid overlap
                Transform contentTrans = itemBoxTrans.Find("Item_Status") ?? itemBoxTrans.Find("Content");
                if (contentTrans == null && wepBoxTrans != null)
                {
                    contentTrans = wepBoxTrans.Find("Item_Status");
                    if (contentTrans != null)
                    {
                        contentTrans.SetParent(itemBoxTrans, false);
                    }
                }

                if (contentTrans != null)
                {
                    contentTrans.name = "Item_Status";
                    RectTransform cRT = contentTrans.GetComponent<RectTransform>();
                    cRT.anchorMin = new Vector2(0f, 1f);
                    cRT.anchorMax = new Vector2(0f, 1f);
                    cRT.pivot = new Vector2(0f, 1f);
                    cRT.anchoredPosition = new Vector2(12f, -28f);
                    cRT.sizeDelta = new Vector2(556f, itemLinesHeight);

                    defeatItemStatusText = contentTrans.GetComponent<TMP_Text>();
                    if (defeatItemStatusText != null)
                    {
                        defeatItemStatusText.alignment = TextAlignmentOptions.TopLeft;
                        defeatItemStatusText.fontSize = 13f;
                        defeatItemStatusText.lineSpacing = 4f;
                        defeatItemStatusText.textWrappingMode = TextWrappingModes.NoWrap;

                        if (passives != null && passives.Count > 0)
                        {
                            var sbI = new System.Text.StringBuilder();
                            for (int i = 0; i < passives.Count; i++)
                            {
                                var slot = passives[i];
                                if (slot == null) continue;
                                string pName = PassiveController.GetPassiveName(slot.type);
                                string desc = GetPassiveSummaryDescription(slot.type, slot.level);
                                sbI.AppendLine($"• {pName} - Lv {slot.level} — <color=#9CA3AF>{desc}</color>");
                            }
                            defeatItemStatusText.text = sbI.ToString().TrimEnd();
                        }
                        else
                        {
                            defeatItemStatusText.text = "<color=#9CA3AF>No items equipped.</color>";
                        }
                        defeatItemStatusText.gameObject.SetActive(true);
                    }
                }
            }

            // 4. Bottom Buttons Row (Btn_BackToMenu)
            Transform btnRow = card.Find("Bottom_Buttons_Row") ?? card.Find("Action_Buttons");
            if (btnRow != null)
            {
                float btnPosY = box2PosY - box2Height - 16f;
                RectTransform brRT = btnRow.GetComponent<RectTransform>();
                brRT.anchorMin = new Vector2(0.5f, 1f);
                brRT.anchorMax = new Vector2(0.5f, 1f);
                brRT.pivot = new Vector2(0.5f, 1f);
                brRT.anchoredPosition = new Vector2(0f, btnPosY);

                // 5. Dynamic GameOver Card Height
                RectTransform cardRT = card.GetComponent<RectTransform>();
                if (cardRT != null)
                {
                    float totalCardHeight = Mathf.Abs(btnPosY) + 44f + 20f;
                    cardRT.sizeDelta = new Vector2(640f, totalCardHeight);
                }
            }
        }

        public static string BuildSummaryRows(WeaponController weaponCtrl, float totalSeconds)
        {
            var sb = new System.Text.StringBuilder();

            // Row 1: weapons
            sb.AppendLine("<color=#93C5FD><b>WEAPONS</b></color>");

            // Row 2-n: List equipped weapons
            if (weaponCtrl != null && weaponCtrl.EquippedWeapons != null && weaponCtrl.EquippedWeapons.Count > 0)
            {
                for (int i = 0; i < weaponCtrl.EquippedWeapons.Count; i++)
                {
                    var w = weaponCtrl.EquippedWeapons[i];
                    if (w == null) continue;
                    float dmg = w.BaseDamage * weaponCtrl.DamageMultiplier;
                    float cd = Mathf.Max(0.1f, w.BaseCooldown * weaponCtrl.CooldownMultiplier);
                    float dps = dmg / cd;
                    int weaponTotalDamage = Mathf.RoundToInt(dmg * Mathf.Max(5, totalSeconds * 1.5f));
                    sb.AppendLine($"  <color=#F9FAFB>• {w.weaponName} Lv.{w.currentLevel}</color>  <color=#9CA3AF>— {weaponTotalDamage:N0} dmg ({dps:F0}/s)</color>");
                }
            }
            else
            {
                sb.AppendLine("  <color=#9CA3AF>• None</color>");
            }

            sb.AppendLine();

            // Row n+1: items
            sb.AppendLine("<color=#93C5FD><b>ITEMS</b></color>");

            // Row n+2 - ...: List equipped items
            var passives = PassiveController.Instance != null ? PassiveController.Instance.EquippedSlots : null;
            if (passives != null && passives.Count > 0)
            {
                for (int i = 0; i < passives.Count; i++)
                {
                    var slot = passives[i];
                    if (slot == null) continue;
                    string pName = PassiveController.GetPassiveName(slot.type);
                    string desc = GetPassiveSummaryDescription(slot.type, slot.level);
                    sb.AppendLine($"  <color=#F9FAFB>• {pName} Lv.{slot.level}</color>  <color=#9CA3AF>— {desc}</color>");
                }
            }
            else
            {
                sb.AppendLine("  <color=#9CA3AF>• None</color>");
            }

            return sb.ToString();
        }

        public static string GetPassiveSummaryDescription(PassiveType type, int level)
        {
            switch (type)
            {
                case PassiveType.BookOfDamage: return $"+{level * 10}% Global Damage";
                case PassiveType.FeatherOfSpeed: return $"+{level * 8}% Move Speed";
                case PassiveType.HeartOfLife: return $"+{level * 25} Max Health";
                case PassiveType.IronShield: return $"+{level * 4}% Damage Reduction";
                case PassiveType.ChronosClock: return $"-{level * 6}% Attack Cooldown";
                case PassiveType.EmeraldOfWisdom: return $"+{level * 15}% Bonus EXP";
                case PassiveType.EnderEye: return $"+{(level * 1.0f):F1}m Pickup Radius";
                case PassiveType.PotionOfRegen: return $"+{(level * 0.4f):F1} HP/s Health Regen";
                case PassiveType.TotemOfUndying: return $"Revive with {Mathf.RoundToInt(PassiveController.GetTotemRevivePercent(level) * 100f)}% HP";
                default: return "Passive Item";
            }
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
            PixelSurvivor.Audio.AudioManager.Instance?.PlayUiClick();
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
            if (GUI.Button(new Rect(btnX, btnStartY, btnW, btnH), "RESUME (ESC)", btnEmeraldStyle))
            {
                Resume();
            }

            // 2. Secondary CTA: DAMAGE NUMBERS TOGGLE (Card Surface + Cyan border)
            string dmgText = showDamageNumbers ? "DAMAGE NUMBERS: [ON]" : "DAMAGE NUMBERS: [OFF]";
            if (GUI.Button(new Rect(btnX, btnStartY + 48, btnW, btnH), dmgText, btnCyanStyle))
            {
                showDamageNumbers = !showDamageNumbers;
            }

            // 3. Danger / Exit: BACK TO MENU (Slate Grey #334155)
            if (GUI.Button(new Rect(btnX, btnStartY + 96, btnW, btnH), "BACK TO MENU", btnSlateStyle))
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
            GUI.Label(new Rect(x, y + 20, modalW, 40), "DEFEAT", titleGameOverStyle);

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
                                 $"  EMERALDS EARNED:  <color=#10B981>+24</color>\n" +
                                 $"  WEAPON REACH:     <color=#F8FAFC>Magma Broadsword (Lv.1)</color>";

            GUI.Label(new Rect(cardX + 12, cardY + 12, cardW - 24, cardH - 24), defeatStats, statLabelStyle);

            // Interactive Buttons
            float btnX = x + 24;
            float btnW = modalW - 48;
            float btnH = 44f;
            float btnStartY = y + 198;

            // 1. Primary CTA: RETRY / TRY AGAIN (Emerald Green #10B981)
            if (GUI.Button(new Rect(btnX, btnStartY, btnW, btnH), "TRY AGAIN (RESTART)", btnEmeraldStyle))
            {
                RestartRun();
            }

            // 2. Secondary CTA: BACK TO MENU (Slate Grey #334155)
            if (GUI.Button(new Rect(btnX, btnStartY + 54, btnW, btnH), "BACK TO MENU", btnSlateStyle))
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
