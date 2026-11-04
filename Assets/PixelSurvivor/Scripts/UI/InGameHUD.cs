using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.Combat;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Minimalist In-Game HUD.
    /// Pure 2-line typography (Line 1: Survival Time counting up 00:00 -> 15:00, Line 2: Biome/Realm).
    /// Zero outlines, zero background boxes, zero containers.
    /// </summary>
    public class InGameHUD : MonoBehaviour
    {
        [Header("Display Mode (Count Up 00:00 -> 15:00)")]
        [Tooltip("If true, counts down from 15:00 to 00:00. If false, counts up from 00:00 to 15:00 (Standard Survival).")]
        [SerializeField] private bool isCountdown = false;

        [Header("Canvas UI References (Optional)")]
        [SerializeField] private GameObject hudRootObject;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text biomeText;
        [Tooltip("Optional reference to Game Mode text in HUD. Auto-located if null.")]
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private Image hpFillImage;
        [SerializeField] private RectTransform hpFillRect;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image expFillImage;
        [SerializeField] private RectTransform expFillRect;

        [Header("Kill & Emerald Counters")]
        [SerializeField] private TMP_Text killCountText;
        [SerializeField] private TMP_Text emeraldCountText;

        [Header("Performance & FPS")]
        [Tooltip("Optional reference to FPS display text in HUD. Auto-located if null.")]
        [SerializeField] private TMP_Text fpsText;
        [Tooltip("Interval in seconds between FPS display refreshes.")]
        [SerializeField] private float fpsUpdateInterval = 0.4f;

        [Header("Equipment Slots UI (Top-Down Center)")]
        [SerializeField] private GameObject equipmentSlotsContainer;
        [SerializeField] private Image[] weaponSlotIcons = new Image[4];
        [SerializeField] private TMP_Text[] weaponSlotLevels = new TMP_Text[4];
        [SerializeField] private Image[] passiveSlotIcons = new Image[4];
        [SerializeField] private TMP_Text[] passiveSlotLevels = new TMP_Text[4];
        [SerializeField] private Vector2 equipmentBarOffset = new Vector2(0f, -144f);

        [Header("Player HP Bar Customization")]
        [SerializeField] private Vector2 hpBarPosition = new Vector2(20f, 16f);
        [SerializeField] private Vector2 hpBarSize = new Vector2(180f, 16f);
        [SerializeField] private Color hpFillColor = new Color(0.937f, 0.267f, 0.267f, 1f); // Crimson Red
        [SerializeField] private Color hpBgColor = new Color(0.043f, 0.059f, 0.098f, 0.92f); // Dark Obsidian
        [SerializeField] private Color hpBorderColor = new Color(0.200f, 0.255f, 0.333f, 1f); // Subtle Slate

        [Header("Font & Styling")]
        [SerializeField] private Font customFont;
        [SerializeField] private int timeFontSize = 28;
        [SerializeField] private int biomeFontSize = 14;
        [SerializeField] private Color timeColor = new Color(0.97f, 0.98f, 0.99f, 1f); // Crisp White (#F8FAFC)
        [SerializeField] private Color biomeColor = new Color(0.22f, 0.74f, 0.97f, 1f); // Diamond Cyan (#38BDF8)

        public static InGameHUD Instance { get; private set; }

        private WaveSpawner waveSpawner;
        private InfiniteMapGenerator mapGenerator;
        private PlayerHealth playerHealth;
        private GUIStyle timeGuiStyle;
        private GUIStyle biomeGuiStyle;
        private GUIStyle shadowStyle;
        private GUIStyle hpTextStyle;
        private Texture2D texHpBg;
        private Texture2D texHpFill;
        private Texture2D texHpBorder;

        private Color? timerColorOverride = null;
        private Coroutine warningPulseCoroutine;

        private float fpsTimeLeft = 0.4f;
        private float fpsAccumulator = 0f;
        private int fpsFrames = 0;
        private int currentFps = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            // Ensure optimal mobile UI scaling (1600x900 Match Height)
            CanvasScaler[] scalers = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            foreach (var s in scalers)
            {
                s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1600f, 900f);
                s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                s.matchWidthOrHeight = 1.0f;
            }

            // FPS: Unlimited in Editor, min 60 max 90 on Android APK
            QualitySettings.vSyncCount = 0;
#if UNITY_EDITOR
            Application.targetFrameRate = -1; // Unlimited in Editor
#else
            Application.targetFrameRate = 90; // Target 60 - 90 FPS on Android APK
#endif
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (customFont == null)
            {
                customFont = Resources.Load<Font>("Fonts/VCR_OSD_MONO");
            }
        }

        private void Start()
        {
            waveSpawner = FindAnyObjectByType<WaveSpawner>();
            mapGenerator = FindAnyObjectByType<InfiniteMapGenerator>();
            playerHealth = FindAnyObjectByType<PlayerHealth>();

            AutoLocateCustomHUD();

            // If running directly without MainMenuManager or if MainMenuManager is inactive, ensure HUD is visible
            var mmm = FindAnyObjectByType<MainMenuManager>();
            if (mmm == null || !mmm.gameObject.activeInHierarchy)
            {
                if (hudRootObject != null) hudRootObject.SetActive(true);
            }

            UpdateModeText();
        }

        private void AutoLocateCustomHUD()
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

            Transform hudRoot = inGameCanvas.transform.Find("HUD_Root");
            if (hudRoot == null) return;
            if (hudRootObject == null) hudRootObject = hudRoot.gameObject;

            // Defensive alignment: flush HUD_Root to Canvas edges with 0 offset
            RectTransform hrRT = hudRoot.GetComponent<RectTransform>();
            if (hrRT != null)
            {
                hrRT.anchorMin = Vector2.zero;
                hrRT.anchorMax = Vector2.one;
                hrRT.anchoredPosition = Vector2.zero;
                hrRT.sizeDelta = Vector2.zero;
            }

            // Defensive alignment: flush EXP_Bar_Container to top edge with slight bleed to eliminate 1px gap
            Transform expBarContainer = hudRoot.Find("EXP_Bar_Container");
            if (expBarContainer != null)
            {
                RectTransform expRT = expBarContainer.GetComponent<RectTransform>();
                if (expRT != null)
                {
                    expRT.anchorMin = new Vector2(0f, 1f);
                    expRT.anchorMax = new Vector2(1f, 1f);
                    expRT.pivot = new Vector2(0.5f, 1f);
                    expRT.anchoredPosition = new Vector2(0f, 2f);
                    expRT.sizeDelta = new Vector2(10f, 20f);
                }
            }

            if (timeText == null)
            {
                timeText = hudRoot.Find("TopCenter_Container/Time_Text")?.GetComponent<TMP_Text>() ??
                           hudRoot.Find("Timer_Container/Timer_Text")?.GetComponent<TMP_Text>();
            }

            if (biomeText == null)
            {
                biomeText = hudRoot.Find("TopCenter_Container/Biome_Text")?.GetComponent<TMP_Text>() ??
                            hudRoot.Find("Timer_Container/Biome_Text")?.GetComponent<TMP_Text>();
            }

            if (modeText == null)
            {
                modeText = hudRoot.Find("TopCenter_Container/Mode_Text")?.GetComponent<TMP_Text>() ??
                           hudRoot.Find("Mode_Text")?.GetComponent<TMP_Text>();
                if (modeText == null)
                {
                    var allTmps = hudRoot.GetComponentsInChildren<TMP_Text>(true);
                    foreach (var t in allTmps)
                    {
                        if (t.gameObject.name == "Mode_Text")
                        {
                            modeText = t;
                            break;
                        }
                    }
                }
            }

            if (hpText == null)
            {
                hpText = hudRoot.Find("Player_Stats/HP_Bar_Container/HP_Text")?.GetComponent<TMP_Text>() ??
                         hudRoot.Find("HP_Bar_Container/HP_Text")?.GetComponent<TMP_Text>();
            }

            if (characterNameText == null)
            {
                characterNameText = hudRoot.Find("Player_Stats/Character_Name")?.GetComponent<TMP_Text>();
            }

            if (levelText == null)
            {
                levelText = hudRoot.Find("Player_Stats/Level_Text")?.GetComponent<TMP_Text>();
            }

            if (expFillRect == null && expFillImage == null)
            {
                Transform expFillTrans = hudRoot.Find("EXP_Bar_Container/Background/Fill");
                if (expFillTrans != null)
                {
                    expFillRect = expFillTrans.GetComponent<RectTransform>();
                    expFillImage = expFillTrans.GetComponent<Image>();
                }
            }

            if (hpFillRect == null && hpFillImage == null)
            {
                Transform fillTrans = hudRoot.Find("Player_Stats/HP_Bar_Container/Border/Background/Fill") ??
                                      hudRoot.Find("HP_Bar_Container/HP_Background/HP_Fill");
                if (fillTrans != null)
                {
                    hpFillRect = fillTrans.GetComponent<RectTransform>();
                    hpFillImage = fillTrans.GetComponent<Image>();
                }
            }

            if (killCountText == null)
            {
                killCountText = hudRoot.Find("Player_Stats/Counters_Row/Kill_Group/Kill_Text")?.GetComponent<TMP_Text>() ??
                                hudRoot.Find("Player_Stats/Counters_Row/Kill_Text")?.GetComponent<TMP_Text>();
            }

            if (emeraldCountText == null)
            {
                emeraldCountText = hudRoot.Find("Player_Stats/Counters_Row/Emerald_Group/Emerald_Text")?.GetComponent<TMP_Text>() ??
                                   hudRoot.Find("Player_Stats/Counters_Row/Emerald_Text")?.GetComponent<TMP_Text>();
            }

            if (fpsText == null)
            {
                fpsText = hudRoot.Find("FPS_Text")?.GetComponent<TMP_Text>();
            }

            // Ensure 2-row Equipment Slots Bar (Weapons Top, Passives Bottom) is active
            EnsureEquipmentSlotsBuilt(hudRoot);
        }

        private void Update()
        {
            if (playerHealth == null)
            {
                playerHealth = FindAnyObjectByType<PlayerHealth>();
            }

            // Update Canvas UI components if present
            if (timeText != null) timeText.text = GetFormattedTime();
            if (biomeText != null) biomeText.text = GetCurrentBiome();
            UpdateModeText();

            if (characterNameText != null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    string cName = !string.IsNullOrEmpty(player.characterName) ? player.characterName : player.characterId;
                    if (!string.IsNullOrEmpty(cName) && characterNameText.text != cName)
                    {
                        characterNameText.text = cName;
                    }
                }
            }

            if (levelText != null)
            {
                int curLvl = ExperienceManager.Instance != null ? ExperienceManager.Instance.CurrentLevel : 1;
                string lvlStr = $"Lv.{curLvl}";
                if (levelText.text != lvlStr)
                {
                    levelText.text = lvlStr;
                }
            }

            if (ExperienceManager.Instance != null)
            {
                float expPct = Mathf.Clamp01((float)ExperienceManager.Instance.CurrentExp / Mathf.Max(1, ExperienceManager.Instance.ExpToNextLevel));

                if (expFillRect != null)
                {
                    expFillRect.anchorMin = Vector2.zero;
                    expFillRect.anchorMax = new Vector2(expPct, 1f);
                    expFillRect.offsetMin = Vector2.zero;
                    expFillRect.offsetMax = Vector2.zero;
                }
                else if (expFillImage != null && expFillImage.type == Image.Type.Filled && expFillImage.sprite != null)
                {
                    expFillImage.fillAmount = expPct;
                }
            }

            if (playerHealth != null)
            {
                float curHp = Mathf.Max(0f, playerHealth.CurrentHealth);
                float maxHp = Mathf.Max(1f, playerHealth.MaxHealth);
                float pct = Mathf.Clamp01(curHp / maxHp);

                if (hpFillImage != null && hpFillImage.type == Image.Type.Filled)
                {
                    hpFillImage.fillAmount = pct;
                }
                else if (hpFillRect != null)
                {
                    hpFillRect.anchorMin = Vector2.zero;
                    hpFillRect.anchorMax = new Vector2(pct, 1f);
                    hpFillRect.offsetMin = Vector2.zero;
                    hpFillRect.offsetMax = Vector2.zero;
                }

                if (hpText != null)
                {
                    hpText.text = $"{Mathf.CeilToInt(curHp)} / {Mathf.CeilToInt(maxHp)}";
                }
            }

            if (killCountText != null)
            {
                string kStr = PixelSurvivor.Combat.EnemyHealth.TotalKills.ToString();
                if (killCountText.text != kStr) killCountText.text = kStr;
            }

            if (emeraldCountText != null)
            {
                int em = ExperienceManager.Instance != null ? ExperienceManager.Instance.CurrentEmeralds : 0;
                string eStr = em.ToString();
                if (emeraldCountText.text != eStr) emeraldCountText.text = eStr;
            }

            // Real-time equipment slots update
            UpdateEquipmentSlots();

            // Real-time FPS update
            UpdateFPS();
        }

        private void UpdateFPS()
        {
            if (fpsText == null) return;
            if (fpsText.GetComponent<FPSCounter>() != null) return; // Allow dedicated FPSCounter component to take precedence

            fpsTimeLeft -= Time.unscaledDeltaTime;
            fpsAccumulator += Time.unscaledDeltaTime;
            fpsFrames++;

            if (fpsTimeLeft <= 0f)
            {
                currentFps = fpsAccumulator > 0f ? Mathf.RoundToInt(fpsFrames / fpsAccumulator) : 0;
                fpsText.text = $"FPS {currentFps}";
                fpsTimeLeft = fpsUpdateInterval;
                fpsAccumulator = 0f;
                fpsFrames = 0;
            }
        }

        /// <summary>
        /// Updates the in-game HUD mode text and theme color based on the active run difficulty.
        /// Preserves the exact RectTransform position and anchors configured in the editor.
        /// </summary>
        private void UpdateModeText()
        {
            if (modeText == null) return;

            if (waveSpawner == null)
            {
                waveSpawner = WaveSpawner.Instance ?? FindAnyObjectByType<WaveSpawner>();
            }

            GameDifficulty diff = waveSpawner != null ? waveSpawner.Difficulty : GameDifficulty.Normal;

            string textStr;
            Color textColor;

            switch (diff)
            {
                case GameDifficulty.Hardcore:
                    textStr = "Hardcore";
                    textColor = new Color(0.937f, 0.267f, 0.267f, 1f); // #EF4444 Crimson Red
                    break;
                case GameDifficulty.Hard:
                    textStr = "Hard";
                    textColor = new Color(0.961f, 0.620f, 0.047f, 1f); // #F59E0B Amber Gold
                    break;
                case GameDifficulty.Normal:
                default:
                    textStr = "Normal";
                    textColor = new Color(0.063f, 0.725f, 0.506f, 1f); // #10B981 Emerald Green
                    break;
            }

            if (modeText.text != textStr)
            {
                modeText.text = textStr;
            }
            if (modeText.color != textColor)
            {
                modeText.color = textColor;
            }
        }

        /// <summary>
        /// Explicitly update the HUD mode display text and theme color.
        /// Does NOT modify the RectTransform position or styling set in the editor.
        /// </summary>
        public void SetModeText(GameDifficulty diff)
        {
            if (modeText == null) return;

            string textStr;
            Color textColor;

            switch (diff)
            {
                case GameDifficulty.Hardcore:
                    textStr = "Hardcore";
                    textColor = new Color(0.937f, 0.267f, 0.267f, 1f);
                    break;
                case GameDifficulty.Hard:
                    textStr = "Hard";
                    textColor = new Color(0.961f, 0.620f, 0.047f, 1f);
                    break;
                case GameDifficulty.Normal:
                default:
                    textStr = "Normal";
                    textColor = new Color(0.063f, 0.725f, 0.506f, 1f);
                    break;
            }

            modeText.text = textStr;
            modeText.color = textColor;
        }

        public string GetFormattedTime()
        {
            if (waveSpawner == null)
            {
                waveSpawner = FindAnyObjectByType<WaveSpawner>();
            }

            float seconds = 0f;
            if (waveSpawner != null)
            {
                seconds = isCountdown ? waveSpawner.RemainingTime : waveSpawner.TimeElapsed;
            }

            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);

            // Visual Alert: Boss Warning / Lockdown override takes precedence, then final minute 14:00+ crimson
            if (timeText != null)
            {
                if (timerColorOverride.HasValue)
                {
                    timeText.color = timerColorOverride.Value;
                }
                else if (seconds >= 840f) // 14:00+
                {
                    timeText.color = new Color(0.98f, 0.38f, 0.38f, 1f); // Warning Crimson Red
                }
                else
                {
                    timeText.color = timeColor; // Crisp White
                }
            }

            return $"{m:00}:{s:00}";
        }

        /// <summary>
        /// Start 5-second flashing alert on HUD timer (Yellow for Mini-boss, Red for Grand Boss).
        /// </summary>
        public void StartTimerWarning(Color color, float duration = 5f)
        {
            if (warningPulseCoroutine != null) StopCoroutine(warningPulseCoroutine);
            warningPulseCoroutine = StartCoroutine(TimerWarningRoutine(color, duration));
        }

        private System.Collections.IEnumerator TimerWarningRoutine(Color warningColor, float duration)
        {
            float elapsed = 0f;
            Vector3 originalScale = (timeText != null) ? timeText.transform.localScale : Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 2.5 Hz Pulse (flash color + scale bounce 1.0 -> 1.18)
                float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI * 5f);
                Color curCol = Color.Lerp(timeColor, warningColor, pulse);
                timerColorOverride = curCol;

                if (timeText != null)
                {
                    timeText.color = curCol;
                    float s = Mathf.Lerp(1.0f, 1.18f, pulse);
                    timeText.transform.localScale = originalScale * s;
                }

                yield return null;
            }

            if (timeText != null)
            {
                timeText.transform.localScale = originalScale;
            }
            warningPulseCoroutine = null;
        }

        /// <summary>
        /// Sets a solid theme color on timer throughout the boss arena lockdown.
        /// </summary>
        public void SetTimerLockdown(Color color)
        {
            if (warningPulseCoroutine != null)
            {
                StopCoroutine(warningPulseCoroutine);
                warningPulseCoroutine = null;
            }

            if (timeText != null)
            {
                timeText.transform.localScale = Vector3.one;
            }

            timerColorOverride = color;
            if (timeText != null) timeText.color = color;
        }

        /// <summary>
        /// Resets the timer color back to normal (white / 14:00 red).
        /// </summary>
        public void ResetTimerColor()
        {
            if (warningPulseCoroutine != null)
            {
                StopCoroutine(warningPulseCoroutine);
                warningPulseCoroutine = null;
            }

            if (timeText != null)
            {
                timeText.transform.localScale = Vector3.one;
            }

            timerColorOverride = null;
            if (timeText != null) timeText.color = timeColor;
        }

        public string GetCurrentBiome()
        {
            if (mapGenerator != null)
            {
                return mapGenerator.CurrentRealmDisplayName;
            }
            return "OVERWORLD";
        }

        private void OnGUI()
        {
            // Fully disabled: Canvas UI (HUD_Root) handles all rendering.
            return;
        }

        private void DrawPlayerHPBar()
        {
            if (playerHealth == null)
            {
                playerHealth = FindAnyObjectByType<PlayerHealth>();
                if (playerHealth == null) return;
            }

            float currentHp = Mathf.Max(0f, playerHealth.CurrentHealth);
            float maxHp = Mathf.Max(1f, playerHealth.MaxHealth);
            float pct = Mathf.Clamp01(currentHp / maxHp);

            float barX = hpBarPosition.x;
            float barY = hpBarPosition.y;
            float barW = hpBarSize.x;
            float barH = hpBarSize.y;

            // Border
            GUI.DrawTexture(new Rect(barX - 2, barY - 2, barW + 4, barH + 4), texHpBorder);
            // Background
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), texHpBg);
            // Fill
            if (pct > 0f)
            {
                GUI.DrawTexture(new Rect(barX, barY, barW * pct, barH), texHpFill);
            }

            // Text
            string hpString = $"HP {Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
            Rect hpTextRect = new Rect(barX, barY - 1, barW, barH);
            DrawTextWithShadow(hpTextRect, hpString, hpTextStyle, Color.white);
        }

        private void DrawTextWithShadow(Rect rect, string text, GUIStyle style, Color textColor)
        {
            // Subtle 1px shadow for readability over any tile background
            Rect shadowRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height);
            shadowStyle.fontSize = style.fontSize;
            shadowStyle.font = style.font;
            shadowStyle.alignment = style.alignment;
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            GUI.Label(shadowRect, text, shadowStyle);

            // Crisp front text
            style.normal.textColor = textColor;
            GUI.Label(rect, text, style);
        }

        private void InitStyles()
        {
            if (timeGuiStyle == null)
            {
                timeGuiStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = timeFontSize,
                    fontStyle = FontStyle.Bold
                };
                if (customFont != null) timeGuiStyle.font = customFont;
            }

            if (biomeGuiStyle == null)
            {
                biomeGuiStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = biomeFontSize,
                    fontStyle = FontStyle.Normal
                };
                if (customFont != null) biomeGuiStyle.font = customFont;
            }

            if (shadowStyle == null)
            {
                shadowStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter
                };
            }

            if (hpTextStyle == null)
            {
                hpTextStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                if (customFont != null) hpTextStyle.font = customFont;
            }

            if (texHpBg == null) texHpBg = MakeSolidTexture(hpBgColor);
            if (texHpFill == null) texHpFill = MakeSolidTexture(hpFillColor);
            if (texHpBorder == null) texHpBorder = MakeSolidTexture(hpBorderColor);
        }

        private Texture2D MakeSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        #region Equipment Slots UI (2-Row Top-Down Center)

        public void EnsureEquipmentSlotsBuilt(Transform hudRoot)
        {
            if (hudRoot == null) return;

            // 1. If already referenced in Inspector, reuse without modifying Transform
            if (equipmentSlotsContainer != null)
            {
                WireExistingEquipmentSlots(equipmentSlotsContainer.transform);
                return;
            }

            // 2. Check under hudRoot
            Transform existing = hudRoot.Find("Equipment_Slots_Container");

            // 3. Fallback: check under parent (InGame_Canvas)
            if (existing == null && hudRoot.parent != null)
            {
                existing = hudRoot.parent.Find("Equipment_Slots_Container");
            }

            // 4. Fallback: search anywhere in scene
            if (existing == null)
            {
                GameObject found = GameObject.Find("Equipment_Slots_Container");
                if (found != null) existing = found.transform;
            }

            if (existing != null)
            {
                equipmentSlotsContainer = existing.gameObject;
                WireExistingEquipmentSlots(existing);
                return;
            }

            // Calculate dynamic position right below TopCenter_Container
            Transform topCenter = hudRoot.Find("TopCenter_Container") ?? hudRoot.Find("Timer_Container");
            if (topCenter != null)
            {
                RectTransform tcRT = topCenter.GetComponent<RectTransform>();
                if (tcRT != null)
                {
                    float tcBottom = tcRT.anchoredPosition.y - tcRT.sizeDelta.y;
                    equipmentBarOffset = new Vector2(0f, tcBottom - 6f);
                }
            }

            TMP_FontAsset pixelFont = Resources.Load<TMP_FontAsset>("Fonts/VCR_OSD_MONO SDF");

            GameObject container = new GameObject("Equipment_Slots_Container");
            container.transform.SetParent(hudRoot, false);

            RectTransform crt = container.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 1f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = equipmentBarOffset;
            crt.sizeDelta = new Vector2(152f, 74f);

            // Row 1: Weapons Row (Top)
            GameObject wepRow = new GameObject("Weapons_Row");
            wepRow.transform.SetParent(container.transform, false);
            RectTransform wepRowRT = wepRow.AddComponent<RectTransform>();
            wepRowRT.anchorMin = new Vector2(0.5f, 1f);
            wepRowRT.anchorMax = new Vector2(0.5f, 1f);
            wepRowRT.pivot = new Vector2(0.5f, 1f);
            wepRowRT.anchoredPosition = Vector2.zero;
            wepRowRT.sizeDelta = new Vector2(152f, 34f);

            // Row 2: Items / Passives Row (Bottom - no border)
            GameObject itemRow = new GameObject("Items_Row");
            itemRow.transform.SetParent(container.transform, false);
            RectTransform itemRowRT = itemRow.AddComponent<RectTransform>();
            itemRowRT.anchorMin = new Vector2(0.5f, 1f);
            itemRowRT.anchorMax = new Vector2(0.5f, 1f);
            itemRowRT.pivot = new Vector2(0.5f, 1f);
            itemRowRT.anchoredPosition = new Vector2(0f, -38f);
            itemRowRT.sizeDelta = new Vector2(152f, 34f);

            float slotSize = 34f;
            float slotGap = 4f;
            float totalW = (slotSize * 4f) + (slotGap * 3f);
            float startX = -(totalW * 0.5f) + (slotSize * 0.5f);

            weaponSlotIcons = new Image[4];
            weaponSlotLevels = new TMP_Text[4];
            passiveSlotIcons = new Image[4];
            passiveSlotLevels = new TMP_Text[4];

            for (int i = 0; i < 4; i++)
            {
                float posX = startX + i * (slotSize + slotGap);

                // Row 1: Weapon slot
                CreateSingleSlotElement($"Wep_Slot_{i}", wepRow.transform, new Vector2(posX, 0f), slotSize, pixelFont, out weaponSlotIcons[i], out weaponSlotLevels[i]);

                // Row 2: Item slot (no border)
                CreateSingleSlotElement($"Item_Slot_{i}", itemRow.transform, new Vector2(posX, 0f), slotSize, pixelFont, out passiveSlotIcons[i], out passiveSlotLevels[i]);
            }

            equipmentSlotsContainer = container;
        }

        private void WireExistingEquipmentSlots(Transform container)
        {
            weaponSlotIcons = new Image[4];
            weaponSlotLevels = new TMP_Text[4];
            passiveSlotIcons = new Image[4];
            passiveSlotLevels = new TMP_Text[4];

            Transform wepRow = container.Find("Weapons_Row");
            Transform itemRow = container.Find("Items_Row");

            for (int i = 0; i < 4; i++)
            {
                if (wepRow != null)
                {
                    Transform ws = wepRow.Find($"Wep_Slot_{i}");
                    if (ws != null)
                    {
                        weaponSlotIcons[i] = ws.Find("Icon")?.GetComponent<Image>();
                        weaponSlotLevels[i] = ws.Find("Level_Text")?.GetComponent<TMP_Text>();
                    }
                }

                if (itemRow != null)
                {
                    Transform ps = itemRow.Find($"Item_Slot_{i}");
                    if (ps != null)
                    {
                        passiveSlotIcons[i] = ps.Find("Icon")?.GetComponent<Image>();
                        passiveSlotLevels[i] = ps.Find("Level_Text")?.GetComponent<TMP_Text>();
                    }
                }
            }
        }

        private void CreateSingleSlotElement(string name, Transform parent, Vector2 pos, float size, TMP_FontAsset font, out Image iconImg, out TMP_Text levelTxt)
        {
            GameObject slotObj = new GameObject(name);
            slotObj.transform.SetParent(parent, false);

            RectTransform rt = slotObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);

            // Style: Rounded square, black with reduced opacity, NO border
            RoundedBorderImage slotBg = slotObj.AddComponent<RoundedBorderImage>();
            slotBg.CornerRadius = 6f;
            slotBg.BorderThickness = 0f; // ko border
            slotBg.FillColor = new Color(0.02f, 0.03f, 0.05f, 0.45f); // black with reduced opacity
            slotBg.raycastTarget = false;

            // Icon Image (size 22x22, centered)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRT = iconObj.AddComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.offsetMin = new Vector2(5f, 5f);
            iconRT.offsetMax = new Vector2(-5f, -5f);

            iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.enabled = false;

            // Level Text (bottom right)
            GameObject lvlObj = new GameObject("Level_Text");
            lvlObj.transform.SetParent(slotObj.transform, false);
            RectTransform lvlRT = lvlObj.AddComponent<RectTransform>();
            lvlRT.anchorMin = new Vector2(0.2f, 0f);
            lvlRT.anchorMax = new Vector2(1f, 0.5f);
            lvlRT.pivot = new Vector2(1f, 0f);
            lvlRT.offsetMin = Vector2.zero;
            lvlRT.offsetMax = new Vector2(-2f, 1f);

            TextMeshProUGUI tmp = lvlObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = 9.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.BottomRight;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.text = "";
            levelTxt = tmp;
        }

        private void UpdateEquipmentSlots()
        {
            if (equipmentSlotsContainer == null) return;

            // 1. Update Weapon Slots (Top row)
            PlayerController player = FindAnyObjectByType<PlayerController>();
            WeaponController wepCtrl = player != null ? player.GetComponent<WeaponController>() : null;
            var equippedWeapons = wepCtrl != null ? wepCtrl.EquippedWeapons : null;

            for (int i = 0; i < 4; i++)
            {
                if (weaponSlotIcons == null || i >= weaponSlotIcons.Length || weaponSlotIcons[i] == null) continue;
                if (weaponSlotLevels == null || i >= weaponSlotLevels.Length || weaponSlotLevels[i] == null) continue;

                if (equippedWeapons != null && i < equippedWeapons.Count && equippedWeapons[i] != null)
                {
                    var wep = equippedWeapons[i];
                    weaponSlotIcons[i].enabled = wep.icon != null;
                    weaponSlotIcons[i].sprite = wep.icon;

                    if (wep.currentLevel >= wep.maxLevel)
                    {
                        weaponSlotLevels[i].text = "MAX";
                        weaponSlotLevels[i].color = new Color(0.98f, 0.80f, 0.08f, 1f); // Vibrant Gold
                    }
                    else
                    {
                        weaponSlotLevels[i].text = $"{wep.currentLevel}";
                        weaponSlotLevels[i].color = new Color(0.97f, 0.98f, 0.99f, 1f); // Crisp White
                    }
                }
                else
                {
                    weaponSlotIcons[i].enabled = false;
                    weaponSlotLevels[i].text = "";
                }
            }

            // 2. Update Passive Item Slots (Bottom row)
            var passives = PassiveController.Instance != null ? PassiveController.Instance.EquippedSlots : null;
            LevelUpManager lvlMgr = LevelUpManager.Instance != null ? LevelUpManager.Instance : FindAnyObjectByType<LevelUpManager>();

            for (int i = 0; i < 4; i++)
            {
                if (passiveSlotIcons == null || i >= passiveSlotIcons.Length || passiveSlotIcons[i] == null) continue;
                if (passiveSlotLevels == null || i >= passiveSlotLevels.Length || passiveSlotLevels[i] == null) continue;

                if (passives != null && i < passives.Count && passives[i] != null)
                {
                    var pSlot = passives[i];
                    Sprite pIcon = lvlMgr != null ? lvlMgr.GetPassiveIcon(pSlot.type) : null;

                    passiveSlotIcons[i].enabled = pIcon != null;
                    passiveSlotIcons[i].sprite = pIcon;

                    if (pSlot.IsMaxLevel)
                    {
                        passiveSlotLevels[i].text = "MAX";
                        passiveSlotLevels[i].color = new Color(0.22f, 0.74f, 0.97f, 1f); // Diamond Cyan
                    }
                    else
                    {
                        passiveSlotLevels[i].text = $"{pSlot.level}";
                        passiveSlotLevels[i].color = new Color(0.97f, 0.98f, 0.99f, 1f); // Crisp White
                    }
                }
                else
                {
                    passiveSlotIcons[i].enabled = false;
                    passiveSlotLevels[i].text = "";
                }
            }
        }

        #endregion
    }
}
