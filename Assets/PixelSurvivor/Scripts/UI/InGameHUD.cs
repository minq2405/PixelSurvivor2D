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
        [SerializeField] private Image hpFillImage;
        [SerializeField] private RectTransform hpFillRect;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image expFillImage;
        [SerializeField] private RectTransform expFillRect;

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

        private void Awake()
        {
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

            // Visual Alert: change time text color in final minute (14:00+) to heighten climax tension
            if (timeText != null)
            {
                if (seconds >= 840f) // 14:00+
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
    }
}
