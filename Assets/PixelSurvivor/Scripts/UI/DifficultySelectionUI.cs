using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.Combat;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Interactive Difficulty Selection Modal (v2.1 Master Specification).
    /// Displays 3 distinct difficulty cards:
    /// 1. Normal (1.0x stats, 150 cap, 2:00 Elite, 180s Enrage, 1.0x EXP)
    /// 2. Hard (1.1x HP, 1.15x DMG, 180 cap, 1:30 Elite, Pincer 6 mobs, 150s Enrage, +10% EXP)
    /// 3. Hardcore (1.22x HP, 1.30x DMG, 220 cap, 1:00 Elite, Pincer 8 mobs, 130s Enrage, +25% EXP)
    /// Features Tone 1 Dark Obsidian styling and instant 1-click play confirmation.
    /// </summary>
    public class DifficultySelectionUI : MonoBehaviour
    {
        [Header("Modal Panels")]
        [SerializeField] private GameObject modalPanel;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Button backButton;

        private Action<GameDifficulty> onDifficultyChosen;

        private void Awake()
        {
            if (modalPanel == null)
            {
                BuildModalUI();
            }
        }

        public void Show(Action<GameDifficulty> callback)
        {
            onDifficultyChosen = callback;
            if (modalPanel != null)
            {
                modalPanel.SetActive(true);
            }
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void Hide()
        {
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
        }

        public void SelectDifficulty(GameDifficulty diff)
        {
            Debug.Log($"<color=#10B981>[Difficulty Selected]</color> Mode set to: <b>{diff}</b>");
            Hide();
            onDifficultyChosen?.Invoke(diff);
        }

        private void BuildModalUI()
        {
            modalPanel = new GameObject("DifficultySelectionModal");
            modalPanel.transform.SetParent(transform, false);

            Canvas canvas = modalPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95;

            var scaler = modalPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            modalPanel.AddComponent<GraphicRaycaster>();

            // Fullscreen Dark Backdrop
            GameObject backdropObj = new GameObject("Backdrop");
            backdropObj.transform.SetParent(modalPanel.transform, false);
            RectTransform bdRect = backdropObj.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;
            var bdImg = backdropObj.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.03f, 0.08f, 0.94f); // Tone 1 Dark Obsidian

            // Modal Header Title
            GameObject titleObj = new GameObject("Title_Header");
            titleObj.transform.SetParent(modalPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.85f);
            titleRect.anchorMax = new Vector2(0.5f, 0.95f);
            titleRect.sizeDelta = new Vector2(900f, 80f);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "SELECT MISSION DIFFICULTY";
            titleTxt.fontSize = 42f;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(0.95f, 0.95f, 1f, 1f);

            // Subtitle Description
            GameObject subObj = new GameObject("Subtitle_Header");
            subObj.transform.SetParent(modalPanel.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.80f);
            subRect.anchorMax = new Vector2(0.5f, 0.85f);
            subRect.sizeDelta = new Vector2(900f, 40f);
            var subTxt = subObj.AddComponent<TextMeshProUGUI>();
            subTxt.text = "Choose your risk and survival reward tier for this 15-minute roguelite run";
            subTxt.fontSize = 20f;
            subTxt.alignment = TextAlignmentOptions.Center;
            subTxt.color = new Color(0.65f, 0.70f, 0.85f, 0.85f);

            // Card Container (Horizontal Layout)
            GameObject containerObj = new GameObject("CardContainer");
            containerObj.transform.SetParent(modalPanel.transform, false);
            RectTransform contRect = containerObj.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 0.20f);
            contRect.anchorMax = new Vector2(0.5f, 0.78f);
            contRect.sizeDelta = new Vector2(1450f, 580f);

            var hlg = containerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 35f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Build 3 Cards: Normal, Hard, Hardcore
            CreateDifficultyCard(containerObj.transform, GameDifficulty.Normal,
                "NORMAL", "Balanced Survival", new Color(0.06f, 0.75f, 0.50f, 1f),
                "• Mob HP: 1.0x (Surge +35%)\n• Sát thương: 1.0x\n• Mob Cap: 150 con\n• Elite đầu tiên: 02:00\n• Rương rớt: 20% (Pity +10%)\n• Warden Enrage: 180s\n• Thưởng EXP: 1.00x",
                "STANDARD RUN");

            CreateDifficultyCard(containerObj.transform, GameDifficulty.Hard,
                "HARD", "Relentless Swarm", new Color(0.96f, 0.62f, 0.08f, 1f),
                "• Mob HP: 1.10x (Boss 1.20x)\n• Sát thương: 1.15x\n• Mob Cap: 180 con\n• Elite đầu tiên: 01:30\n• Pincer Ambush lúc 01:30 (6 Mobs)\n• Warden Enrage: 150s\n• Thưởng EXP: +10% (1.10x)",
                "RECOMMENDED");

            CreateDifficultyCard(containerObj.transform, GameDifficulty.Hardcore,
                "HARDCORE", "Apocalyptic Nightmare", new Color(0.94f, 0.27f, 0.27f, 1f),
                "• Mob HP: 1.22x (Boss 1.40x)\n• Sát thương: 1.30x\n• Mob Cap: 220 con\n• Elite đầu tiên: 01:00\n• Pincer Ambush lúc 01:30 (8 Mobs)\n• Warden Enrage: 130s\n• Thưởng EXP: +25% (1.25x)",
                "HIGH RISK");

            modalPanel.SetActive(false);
        }

        private void CreateDifficultyCard(Transform parent, GameDifficulty diff, string title, string subtitle, Color themeColor, string details, string badge)
        {
            GameObject cardObj = new GameObject($"Card_{diff}");
            cardObj.transform.SetParent(parent, false);

            var cardImg = cardObj.AddComponent<Image>();
            cardImg.color = new Color(0.08f, 0.07f, 0.14f, 0.98f); // Obsidian Card BG

            var btn = cardObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.25f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => SelectDifficulty(diff));

            // Outline / Border Accent
            var outline = cardObj.AddComponent<Outline>();
            outline.effectColor = themeColor;
            outline.effectDistance = new Vector2(2f, 2f);

            // Card Layout
            var vlg = cardObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 28, 24);
            vlg.spacing = 14f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Badge
            GameObject badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(cardObj.transform, false);
            var badgeTxt = badgeObj.AddComponent<TextMeshProUGUI>();
            badgeTxt.text = $"[{badge}]";
            badgeTxt.fontSize = 15f;
            badgeTxt.alignment = TextAlignmentOptions.Center;
            badgeTxt.color = themeColor;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = title;
            titleTxt.fontSize = 32f;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = themeColor;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(cardObj.transform, false);
            var subTxt = subObj.AddComponent<TextMeshProUGUI>();
            subTxt.text = subtitle;
            subTxt.fontSize = 16f;
            subTxt.alignment = TextAlignmentOptions.Center;
            subTxt.color = new Color(0.7f, 0.75f, 0.85f, 0.8f);

            // Separator Line
            GameObject sepObj = new GameObject("Sep");
            sepObj.transform.SetParent(cardObj.transform, false);
            var sepImg = sepObj.AddComponent<Image>();
            sepImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.35f);
            var sepRect = sepObj.GetComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(0f, 2f);

            // Details List
            GameObject detObj = new GameObject("Details");
            detObj.transform.SetParent(cardObj.transform, false);
            var detTxt = detObj.AddComponent<TextMeshProUGUI>();
            detTxt.text = details;
            detTxt.fontSize = 16.5f;
            detTxt.lineSpacing = 16f;
            detTxt.color = new Color(0.85f, 0.90f, 0.95f, 0.95f);
            detTxt.alignment = TextAlignmentOptions.TopLeft;

            // Select Button Prompt at bottom
            GameObject btnPromptObj = new GameObject("SelectBtn");
            btnPromptObj.transform.SetParent(cardObj.transform, false);
            var bpImg = btnPromptObj.AddComponent<Image>();
            bpImg.color = themeColor;
            var bpRect = btnPromptObj.GetComponent<RectTransform>();
            bpRect.sizeDelta = new Vector2(0f, 44f);

            GameObject bpTextObj = new GameObject("BtnText");
            bpTextObj.transform.SetParent(btnPromptObj.transform, false);
            RectTransform bpTextRect = bpTextObj.AddComponent<RectTransform>();
            bpTextRect.anchorMin = Vector2.zero;
            bpTextRect.anchorMax = Vector2.one;
            bpTextRect.sizeDelta = Vector2.zero;
            var bpText = bpTextObj.AddComponent<TextMeshProUGUI>();
            bpText.text = $"DEPLOY ({title})";
            bpText.fontSize = 18f;
            bpText.alignment = TextAlignmentOptions.Center;
            bpText.color = new Color(0.04f, 0.03f, 0.08f, 1f);
        }
    }
}
