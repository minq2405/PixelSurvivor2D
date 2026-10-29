using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.UI;

namespace PixelSurvivor.Editor
{
    public static class CanvasUISetupHelper
    {
        private const string FONT_PATH = "Fonts/VCR_OSD_MONO SDF";

        [MenuItem("PixelSurvivor/UI: Build Complete Canvas UI (HUD, Pause, Defeat)")]
        public static void BuildCompleteCanvasUI()
        {
            TMP_FontAsset pixelFont = Resources.Load<TMP_FontAsset>(FONT_PATH);

            // 1. Locate or create EventSystem
            UnityEngine.EventSystems.EventSystem es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // 2. Locate or create Canvas
            string canvasName = "InGame_Canvas";
            GameObject canvasObj = GameObject.Find(canvasName);
            if (canvasObj != null)
            {
                Undo.DestroyObjectImmediate(canvasObj);
            }

            canvasObj = new GameObject(canvasName);
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // =========================================================
            // PART 1: HUD ROOT (HP Bar & Top Center Timer/Biome)
            // =========================================================
            GameObject hudRoot = CreateUIElement("HUD_Root", canvasObj.transform);
            StretchFull(hudRoot.GetComponent<RectTransform>());

            // A. HP Bar (Top Left)
            GameObject hpContainer = CreateUIElement("HP_Bar_Container", hudRoot.transform);
            RectTransform hpRect = hpContainer.GetComponent<RectTransform>();
            SetAnchor(hpRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            hpRect.anchoredPosition = new Vector2(28f, -28f);
            hpRect.sizeDelta = new Vector2(320f, 32f);

            GameObject hpBorder = CreateImageElement("HP_Border", hpContainer.transform, new Color(0.200f, 0.255f, 0.333f, 1f));
            StretchFull(hpBorder.GetComponent<RectTransform>());

            GameObject hpBg = CreateImageElement("HP_Background", hpBorder.transform, new Color(0.043f, 0.059f, 0.098f, 0.95f));
            Inset(hpBg.GetComponent<RectTransform>(), 3f);

            GameObject hpFill = CreateImageElement("HP_Fill", hpBg.transform, new Color(0.063f, 0.725f, 0.506f, 1f));
            Image fillImg = hpFill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;
            StretchFull(hpFill.GetComponent<RectTransform>());

            GameObject hpTextObj = CreateTextElement("HP_Text", hpContainer.transform, "100 / 100", pixelFont, 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchFull(hpTextObj.GetComponent<RectTransform>());

            // B. Timer & Biome HUD (Top Center)
            GameObject timerContainer = CreateUIElement("Timer_Container", hudRoot.transform);
            RectTransform timerRect = timerContainer.GetComponent<RectTransform>();
            SetAnchor(timerRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            timerRect.anchoredPosition = new Vector2(0f, -24f);
            timerRect.sizeDelta = new Vector2(240f, 62f);

            GameObject timerBg = CreateImageElement("Timer_Bg", timerContainer.transform, new Color(0.043f, 0.059f, 0.098f, 0.88f));
            StretchFull(timerBg.GetComponent<RectTransform>());

            GameObject timerTextObj = CreateTextElement("Timer_Text", timerContainer.transform, "00:00", pixelFont, 24, FontStyles.Bold, new Color(0.980f, 0.800f, 0.082f, 1f), TextAlignmentOptions.Center);
            RectTransform timeTR = timerTextObj.GetComponent<RectTransform>();
            timeTR.anchorMin = new Vector2(0f, 0.45f);
            timeTR.anchorMax = new Vector2(1f, 1f);
            timeTR.offsetMin = Vector2.zero;
            timeTR.offsetMax = Vector2.zero;

            GameObject biomeTextObj = CreateTextElement("Biome_Text", timerContainer.transform, "OVERWORLD", pixelFont, 14, FontStyles.Normal, new Color(0.220f, 0.741f, 0.973f, 1f), TextAlignmentOptions.Center);
            RectTransform biomeTR = biomeTextObj.GetComponent<RectTransform>();
            biomeTR.anchorMin = new Vector2(0f, 0f);
            biomeTR.anchorMax = new Vector2(1f, 0.45f);
            biomeTR.offsetMin = Vector2.zero;
            biomeTR.offsetMax = Vector2.zero;

            // Locate Game Manager for wiring
            GameObject gmObj = GameObject.Find("[Game_Manager]");
            InGamePauseManager pauseMgr = gmObj != null ? gmObj.GetComponent<InGamePauseManager>() : Object.FindAnyObjectByType<InGamePauseManager>();

            // =========================================================
            // PART 2: PAUSE MODAL PANEL (Modernized to match JSON & Image)
            // =========================================================
            GameObject pausePanel = BuildPauseModal(canvasObj.transform, pixelFont, pauseMgr);

            // =========================================================
            // PART 3: LEVEL UP MODAL PANEL (2-Click Selection)
            // =========================================================
            LevelUpManager lvlMgr = gmObj != null ? gmObj.GetComponent<LevelUpManager>() : Object.FindAnyObjectByType<LevelUpManager>();
            if (lvlMgr == null && gmObj != null)
            {
                lvlMgr = gmObj.AddComponent<LevelUpManager>();
            }
            GameObject levelUpPanel = BuildLevelUpModalContent(canvasObj.transform, pixelFont, lvlMgr);

            // =========================================================
            // PART 4: GAME OVER / DEFEAT MODAL PANEL (Modernized to match You Fell... reference)
            // =========================================================
            GameObject gameOverPanel = BuildDefeatModal(canvasObj.transform, pixelFont, pauseMgr);

            // Wire HUD to Game Manager
            if (gmObj != null)
            {
                InGameHUD hud = gmObj.GetComponent<InGameHUD>();
                if (hud != null)
                {
                    SetPrivateField(hud, "timeText", timerTextObj.GetComponent<TMP_Text>());
                    SetPrivateField(hud, "biomeText", biomeTextObj.GetComponent<TMP_Text>());
                    SetPrivateField(hud, "hpFillImage", fillImg);
                    SetPrivateField(hud, "hpText", hpTextObj.GetComponent<TMP_Text>());
                    EditorUtility.SetDirty(hud);
                }
            }

            pausePanel.SetActive(false);
            levelUpPanel.SetActive(false);
            gameOverPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#10B981>[CanvasUISetupHelper] SUCCESS!</color> Built InGame_Canvas with HUD, Updated Pause Modal, Level Up Modal, and Defeat Modal connected to [Game_Manager]!");
        }

        [MenuItem("PixelSurvivor/UI: Update Pause Menu Only")]
        public static void UpdatePauseMenuOnly()
        {
            TMP_FontAsset pixelFont = Resources.Load<TMP_FontAsset>(FONT_PATH);

            string canvasName = "InGame_Canvas";
            GameObject canvasObj = GameObject.Find(canvasName);
            if (canvasObj == null)
            {
                Debug.LogWarning("[CanvasUISetupHelper] InGame_Canvas not found. Creating complete canvas instead.");
                BuildCompleteCanvasUI();
                return;
            }

            // Remove existing Pause_Modal_Panel if any
            Transform oldPause = canvasObj.transform.Find("Pause_Modal_Panel");
            if (oldPause != null)
            {
                Undo.DestroyObjectImmediate(oldPause.gameObject);
            }

            GameObject gmObj = GameObject.Find("[Game_Manager]");
            InGamePauseManager pauseMgr = gmObj != null ? gmObj.GetComponent<InGamePauseManager>() : Object.FindAnyObjectByType<InGamePauseManager>();

            GameObject newPausePanel = BuildPauseModal(canvasObj.transform, pixelFont, pauseMgr);
            newPausePanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#10B981>[CanvasUISetupHelper] SUCCESS!</color> Updated Pause_Modal_Panel without touching HUD or Defeat Modal!");
        }

        [MenuItem("PixelSurvivor/UI: Build or Update Level Up Modal")]
        public static void BuildOrUpdateLevelUpModal()
        {
            TMP_FontAsset pixelFont = Resources.Load<TMP_FontAsset>(FONT_PATH);

            string canvasName = "InGame_Canvas";
            GameObject canvasObj = GameObject.Find(canvasName);
            if (canvasObj == null)
            {
                Debug.LogWarning("[CanvasUISetupHelper] InGame_Canvas not found. Creating complete canvas first.");
                BuildCompleteCanvasUI();
                return;
            }

            Transform oldPanel = canvasObj.transform.Find("LevelUp_Modal_Panel");
            if (oldPanel != null)
            {
                Undo.DestroyObjectImmediate(oldPanel.gameObject);
            }

            GameObject gmObj = GameObject.Find("[Game_Manager]");
            LevelUpManager lvlMgr = gmObj != null ? gmObj.GetComponent<LevelUpManager>() : Object.FindAnyObjectByType<LevelUpManager>();
            if (lvlMgr == null && gmObj != null)
            {
                lvlMgr = gmObj.AddComponent<LevelUpManager>();
                Undo.RegisterCreatedObjectUndo(lvlMgr, "Add LevelUpManager");
            }

            GameObject newLevelPanel = BuildLevelUpModalContent(canvasObj.transform, pixelFont, lvlMgr);
            newLevelPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#10B981>[CanvasUISetupHelper] SUCCESS!</color> Built LevelUp_Modal_Panel with 2-Click Card selection!");
        }

        [MenuItem("PixelSurvivor/UI: Build or Update Defeat Modal")]
        public static void BuildOrUpdateDefeatModal()
        {
            TMP_FontAsset pixelFont = Resources.Load<TMP_FontAsset>(FONT_PATH);

            string canvasName = "InGame_Canvas";
            GameObject canvasObj = GameObject.Find(canvasName);
            if (canvasObj == null)
            {
                Debug.LogWarning("[CanvasUISetupHelper] InGame_Canvas not found. Creating complete canvas first.");
                BuildCompleteCanvasUI();
                return;
            }

            Transform oldPanel = canvasObj.transform.Find("GameOver_Modal_Panel");
            if (oldPanel != null)
            {
                Undo.DestroyObjectImmediate(oldPanel.gameObject);
            }

            GameObject gmObj = GameObject.Find("[Game_Manager]");
            InGamePauseManager pauseMgr = gmObj != null ? gmObj.GetComponent<InGamePauseManager>() : Object.FindAnyObjectByType<InGamePauseManager>();

            GameObject newDefeatPanel = BuildDefeatModal(canvasObj.transform, pixelFont, pauseMgr);
            newDefeatPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#10B981>[CanvasUISetupHelper] SUCCESS!</color> Built GameOver_Modal_Panel (Defeat) matching 'You Fell...' layout!");
        }

        private static GameObject BuildLevelUpModalContent(Transform canvasTransform, TMP_FontAsset pixelFont, LevelUpManager lvlMgr)
        {
            GameObject levelPanel = CreateUIElement("LevelUp_Modal_Panel", canvasTransform);
            StretchFull(levelPanel.GetComponent<RectTransform>());
            Image levelDim = levelPanel.AddComponent<Image>();
            levelDim.color = new Color(0.043f, 0.059f, 0.098f, 0.85f); // Dim Obsidian

            // Outer Card (Width 680, Height 560)
            GameObject levelCard = CreateRoundedCard("Card", levelPanel.transform, 16f, 4f,
                new Color(0.106f, 0.220f, 0.510f, 0.98f), // #1B3882 Deep Blue
                new Color(0.353f, 0.557f, 0.965f, 1f));   // #5A8EF6 Bright Cyan/Blue Border
            RectTransform cardTR = levelCard.GetComponent<RectTransform>();
            SetAnchor(cardTR, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            cardTR.sizeDelta = new Vector2(680f, 560f);

            // Title "Level 4!"
            GameObject titleObj = CreateTextElement("Title", levelCard.transform, "Level 4!", pixelFont, 28, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            RectTransform titleTR = titleObj.GetComponent<RectTransform>();
            SetAnchor(titleTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            titleTR.anchoredPosition = new Vector2(0f, -24f);
            titleTR.sizeDelta = new Vector2(300f, 36f);

            // Subtitle "Choose a reward · 1 2 3"
            GameObject subObj = CreateTextElement("Subtitle", levelCard.transform, "Choose a reward ·  1   2   3 ", pixelFont, 14, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            RectTransform subTR = subObj.GetComponent<RectTransform>();
            SetAnchor(subTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            subTR.anchoredPosition = new Vector2(0f, -62f);
            subTR.sizeDelta = new Vector2(400f, 24f);

            // 3 Cards Container
            GameObject cardsContainer = CreateUIElement("Cards_Container", levelCard.transform);
            RectTransform cardsContTR = cardsContainer.GetComponent<RectTransform>();
            SetAnchor(cardsContTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            cardsContTR.anchoredPosition = new Vector2(0f, -94f);
            cardsContTR.sizeDelta = new Vector2(620f, 380f);

            LevelUpCardUI[] cardArray = new LevelUpCardUI[3];
            float cardSpacing = 118f;

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                GameObject cardObj = CreateRoundedCard($"Card_{idx}", cardsContainer.transform, 10f, 2f,
                    new Color(0.082f, 0.165f, 0.396f, 1f), // #152A65
                    new Color(0.220f, 0.357f, 0.686f, 1f)); // #385BAF
                RectTransform cTR = cardObj.GetComponent<RectTransform>();
                SetAnchor(cTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                cTR.anchoredPosition = new Vector2(0f, -(idx * cardSpacing));
                cTR.sizeDelta = new Vector2(620f, 106f);

                LevelUpCardUI cardUI = cardObj.AddComponent<LevelUpCardUI>();
                cardArray[idx] = cardUI;

                // Left: Icon Frame (72x72)
                GameObject iconFrame = CreateRoundedCard("Icon_Frame", cardObj.transform, 8f, 0f, new Color(0.059f, 0.114f, 0.271f, 1f), Color.clear);
                RectTransform ifTR = iconFrame.GetComponent<RectTransform>();
                SetAnchor(ifTR, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
                ifTR.anchoredPosition = new Vector2(16f, 0f);
                ifTR.sizeDelta = new Vector2(72f, 72f);

                GameObject iconObj = CreateImageElement("Icon_Image", iconFrame.transform, Color.white);
                RectTransform iconTR = iconObj.GetComponent<RectTransform>();
                Inset(iconTR, 10f);

                // Center Content Container
                GameObject contentObj = CreateUIElement("Content", cardObj.transform);
                RectTransform contTR = contentObj.GetComponent<RectTransform>();
                SetAnchor(contTR, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
                contTR.offsetMin = new Vector2(102f, 8f);
                contTR.offsetMax = new Vector2(-70f, -8f);

                // Row 1: Title & Badges
                GameObject titleRow = CreateUIElement("Title_Row", contentObj.transform);
                RectTransform trTR = titleRow.GetComponent<RectTransform>();
                SetAnchor(trTR, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
                trTR.anchoredPosition = new Vector2(0f, 0f);
                trTR.sizeDelta = new Vector2(0f, 26f);

                GameObject cardTitle = CreateTextElement("Title", titleRow.transform, idx == 0 ? "Quick Attack" : (idx == 1 ? "Iron Sword" : "Swift Boots"), pixelFont, 16, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
                RectTransform ctTR = cardTitle.GetComponent<RectTransform>();
                SetAnchor(ctTR, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
                ctTR.anchoredPosition = Vector2.zero;
                ctTR.sizeDelta = new Vector2(160f, 0f);

                // Badge 1 (New / Upgrade)
                GameObject b1Obj = CreateRoundedCard("Badge_1", titleRow.transform, 6f, 0f, new Color(0.820f, 0.800f, 0.753f, 1f), Color.clear);
                RectTransform b1TR = b1Obj.GetComponent<RectTransform>();
                SetAnchor(b1TR, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
                b1TR.anchoredPosition = new Vector2(170f, 0f);
                b1TR.sizeDelta = new Vector2(95f, 20f);
                GameObject b1Txt = CreateTextElement("Label", b1Obj.transform, idx == 1 ? "Upgrade" : "New · Normal", pixelFont, 10, FontStyles.Bold, new Color(0.184f, 0.208f, 0.259f, 1f), TextAlignmentOptions.Center);
                StretchFull(b1Txt.GetComponent<RectTransform>());

                // Badge 2 (Physical / Passive / Lv.1>2)
                GameObject b2Obj = CreateRoundedCard("Badge_2", titleRow.transform, 6f, 0f, idx == 1 ? new Color(0.533f, 0.329f, 0.816f, 1f) : new Color(0.118f, 0.216f, 0.600f, 1f), Color.clear);
                RectTransform b2TR = b2Obj.GetComponent<RectTransform>();
                SetAnchor(b2TR, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
                b2TR.anchoredPosition = new Vector2(272f, 0f);
                b2TR.sizeDelta = new Vector2(75f, 20f);
                GameObject b2Txt = CreateTextElement("Label", b2Obj.transform, idx == 1 ? "Lv.1 > 2" : "Physical", pixelFont, 10, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                StretchFull(b2Txt.GetComponent<RectTransform>());

                // Row 2: Description
                string defaultDesc = idx == 0 ? "You dash at the enemy like lightning and hit everything on the way." : (idx == 1 ? "+25% Slash Damage, -10% Cooldown." : "Increases base movement speed by +15%.");
                GameObject descObj = CreateTextElement("Description", contentObj.transform, defaultDesc, pixelFont, 12, FontStyles.Normal, new Color(0.95f, 0.95f, 0.95f, 1f), TextAlignmentOptions.TopLeft);
                RectTransform descTR = descObj.GetComponent<RectTransform>();
                SetAnchor(descTR, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
                descTR.anchoredPosition = new Vector2(0f, -28f);
                descTR.sizeDelta = new Vector2(0f, 32f);

                // Row 3: Synergy / Extra note
                string defaultSyn = idx == 0 ? "With Steve it deals +25% bonus melee damage." : (idx == 1 ? "Primary Melee: Slashes all nearby enemies." : "Mobility: Dodge fast-moving enemy hordes.");
                GameObject synObj = CreateTextElement("Synergy", contentObj.transform, defaultSyn, pixelFont, 11, FontStyles.Normal, new Color(0.953f, 0.612f, 0.071f, 1f), TextAlignmentOptions.Left);
                RectTransform synTR = synObj.GetComponent<RectTransform>();
                SetAnchor(synTR, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
                synTR.anchoredPosition = new Vector2(0f, 2f);
                synTR.sizeDelta = new Vector2(0f, 20f);

                // Right: Hotkey pill [ 1 ], [ 2 ], [ 3 ]
                GameObject hkPill = CreateRoundedCard("Hotkey_Pill", cardObj.transform, 6f, 2f, new Color(0.106f, 0.220f, 0.510f, 1f), new Color(0.220f, 0.357f, 0.686f, 1f));
                RectTransform hkTR = hkPill.GetComponent<RectTransform>();
                SetAnchor(hkTR, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                hkTR.anchoredPosition = new Vector2(-16f, 0f);
                hkTR.sizeDelta = new Vector2(36f, 36f);
                GameObject hkTxt = CreateTextElement("Label", hkPill.transform, (idx + 1).ToString(), pixelFont, 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                StretchFull(hkTxt.GetComponent<RectTransform>());

                // Confirm Hint Text (e.g. "Click again to confirm!")
                GameObject hintObj = CreateTextElement("Confirm_Hint", cardObj.transform, "Click again to confirm!", pixelFont, 10, FontStyles.Bold, new Color(0.980f, 0.800f, 0.082f, 1f), TextAlignmentOptions.Right);
                RectTransform hintTR = hintObj.GetComponent<RectTransform>();
                SetAnchor(hintTR, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
                hintTR.anchoredPosition = new Vector2(-16f, 6f);
                hintTR.sizeDelta = new Vector2(180f, 16f);
                hintObj.SetActive(false);

                // Wire Card UI fields
                SetPrivateField(cardUI, "cardBackground", cardObj.GetComponent<RoundedBorderImage>());
                SetPrivateField(cardUI, "iconImage", iconObj.GetComponent<Image>());
                SetPrivateField(cardUI, "titleText", cardTitle.GetComponent<TMP_Text>());
                SetPrivateField(cardUI, "badge1Text", b1Txt.GetComponent<TMP_Text>());
                SetPrivateField(cardUI, "badge1Container", b1Obj);
                SetPrivateField(cardUI, "badge2Text", b2Txt.GetComponent<TMP_Text>());
                SetPrivateField(cardUI, "badge2Background", b2Obj.GetComponent<RoundedBorderImage>());
                SetPrivateField(cardUI, "badge2Container", b2Obj);
                SetPrivateField(cardUI, "descriptionText", descObj.GetComponent<TMP_Text>());
                SetPrivateField(cardUI, "synergyText", synObj.GetComponent<TMP_Text>());
                SetPrivateField(cardUI, "hotkeyText", hkTxt.GetComponent<TMP_Text>());
                SetPrivateField(cardUI, "confirmHintText", hintObj.GetComponent<TMP_Text>());
            }

            // Bottom Action: Reroll Button
            GameObject btnReroll = CreateRoundedButtonElement("Btn_Reroll", levelCard.transform, "Reroll (3) · R", pixelFont, 13, 8f, 2f,
                new Color(0.106f, 0.220f, 0.510f, 1f), // #1B3882
                new Color(0.353f, 0.557f, 0.965f, 1f), // #5A8EF6
                Color.white,
                new Vector2(0f, -508f),
                new Vector2(190f, 38f));
            RectTransform rerollTR = btnReroll.GetComponent<RectTransform>();
            SetAnchor(rerollTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            rerollTR.anchoredPosition = new Vector2(0f, -508f);

            // Wire to LevelUpManager
            if (lvlMgr != null)
            {
                SetPrivateField(lvlMgr, "levelUpModalPanel", levelPanel);
                SetPrivateField(lvlMgr, "levelTitleText", titleObj.GetComponent<TMP_Text>());
                SetPrivateField(lvlMgr, "cardUIs", cardArray);
                SetPrivateField(lvlMgr, "rerollButton", btnReroll.GetComponent<Button>());
                SetPrivateField(lvlMgr, "rerollButtonText", btnReroll.GetComponentInChildren<TMP_Text>());
                EditorUtility.SetDirty(lvlMgr);
            }

            return levelPanel;
        }

        private static GameObject BuildPauseModal(Transform canvasTransform, TMP_FontAsset pixelFont, InGamePauseManager pauseMgr)
        {
            GameObject pausePanel = CreateUIElement("Pause_Modal_Panel", canvasTransform);
            StretchFull(pausePanel.GetComponent<RectTransform>());
            Image pauseDim = pausePanel.AddComponent<Image>();
            pauseDim.color = new Color(0.043f, 0.059f, 0.098f, 0.85f); // Tone 1 Obsidian Dim

            // Card using RoundedBorderImage
            GameObject pauseCard = CreateRoundedCard("Card", pausePanel.transform, 16f, 4f,
                new Color(0.106f, 0.220f, 0.510f, 0.98f), // #1B3882 Deep Blue
                new Color(0.353f, 0.557f, 0.965f, 1f));   // #5A8EF6 Bright Cyan/Blue Border
            RectTransform pCardTR = pauseCard.GetComponent<RectTransform>();
            SetAnchor(pCardTR, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            pCardTR.sizeDelta = new Vector2(640f, 580f);

            // Title "Pause"
            GameObject pTitle = CreateTextElement("Title", pauseCard.transform, "Pause", pixelFont, 26, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            RectTransform pTitleTR = pTitle.GetComponent<RectTransform>();
            SetAnchor(pTitleTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            pTitleTR.anchoredPosition = new Vector2(0f, -22f);
            pTitleTR.sizeDelta = new Vector2(300f, 32f);

            // Top Buttons Container (Row of 3 buttons: Continue, Settings, Quit run)
            GameObject topBtnRow = CreateUIElement("Top_Action_Buttons", pauseCard.transform);
            RectTransform topBtnTR = topBtnRow.GetComponent<RectTransform>();
            SetAnchor(topBtnTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            topBtnTR.anchoredPosition = new Vector2(0f, -68f);
            topBtnTR.sizeDelta = new Vector2(580f, 38f);

            GameObject btnContinue = CreateRoundedButtonElement("Btn_Continue", topBtnRow.transform, "Continue", pixelFont, 14, 8f, 2f, new Color(0.969f, 0.718f, 0.192f, 1f), Color.white, new Color(0.118f, 0.153f, 0.180f, 1f), new Vector2(-190f, 0f), new Vector2(170f, 38f));
            GameObject btnSettings = CreateRoundedButtonElement("Btn_Settings", topBtnRow.transform, "Settings", pixelFont, 14, 8f, 2f, new Color(0.125f, 0.251f, 0.596f, 1f), new Color(0.353f, 0.557f, 0.965f, 1f), Color.white, new Vector2(0f, 0f), new Vector2(170f, 38f));
            GameObject btnQuit = CreateRoundedButtonElement("Btn_Quit", topBtnRow.transform, "Quit run", pixelFont, 14, 8f, 2f, new Color(0.125f, 0.251f, 0.596f, 1f), new Color(0.353f, 0.557f, 0.965f, 1f), Color.white, new Vector2(190f, 0f), new Vector2(170f, 38f));

            // Section 1: "Your weapons"
            GameObject secWeapons = CreateUIElement("Section_Weapons", pauseCard.transform);
            RectTransform secWepTR = secWeapons.GetComponent<RectTransform>();
            SetAnchor(secWepTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            secWepTR.anchoredPosition = new Vector2(0f, -120f);
            secWepTR.sizeDelta = new Vector2(580f, 85f);

            GameObject lblWeapons = CreateTextElement("Title_Weapons", secWeapons.transform, "Your weapons", pixelFont, 15, FontStyles.Bold, new Color(0.969f, 0.718f, 0.192f, 1f), TextAlignmentOptions.Left);
            RectTransform lblWepTR = lblWeapons.GetComponent<RectTransform>();
            SetAnchor(lblWepTR, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            lblWepTR.anchoredPosition = new Vector2(10f, 0f);
            lblWepTR.sizeDelta = new Vector2(200f, 24f);

            GameObject lblColDamage = CreateTextElement("Col_Damage", secWeapons.transform, "Damage", pixelFont, 11, FontStyles.Normal, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Right);
            RectTransform colDmgTR = lblColDamage.GetComponent<RectTransform>();
            SetAnchor(colDmgTR, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            colDmgTR.anchoredPosition = new Vector2(-110f, 0f);
            colDmgTR.sizeDelta = new Vector2(90f, 24f);

            GameObject lblColDPS = CreateTextElement("Col_DPS", secWeapons.transform, "Per second", pixelFont, 11, FontStyles.Normal, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Right);
            RectTransform colDpsTR = lblColDPS.GetComponent<RectTransform>();
            SetAnchor(colDpsTR, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            colDpsTR.anchoredPosition = new Vector2(-10f, 0f);
            colDpsTR.sizeDelta = new Vector2(90f, 24f);

            // Weapon Item Row
            GameObject wepRow = CreateUIElement("Weapon_Row_0", secWeapons.transform);
            RectTransform wepRowTR = wepRow.GetComponent<RectTransform>();
            SetAnchor(wepRowTR, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            wepRowTR.anchoredPosition = new Vector2(0f, 15f);
            wepRowTR.sizeDelta = new Vector2(-20f, 32f);

            GameObject wepNameObj = CreateTextElement("Weapon_Name", wepRow.transform, "Iron Sword Lv.1", pixelFont, 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            RectTransform wepNameTR = wepNameObj.GetComponent<RectTransform>();
            SetAnchor(wepNameTR, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            wepNameTR.anchoredPosition = new Vector2(10f, 0f);
            wepNameTR.sizeDelta = new Vector2(180f, 30f);

            // Badge "Melee"
            GameObject badge = CreateRoundedCard("Badge_Melee", wepRow.transform, 6f, 0f, new Color(0.220f, 0.404f, 0.839f, 1f), Color.clear);
            RectTransform badgeTR = badge.GetComponent<RectTransform>();
            SetAnchor(badgeTR, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            badgeTR.anchoredPosition = new Vector2(200f, 0f);
            badgeTR.sizeDelta = new Vector2(65f, 22f);
            GameObject badgeTxt = CreateTextElement("Badge_Text", badge.transform, "Melee", pixelFont, 10, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchFull(badgeTxt.GetComponent<RectTransform>());

            GameObject wepDmgObj = CreateTextElement("Weapon_Damage", wepRow.transform, "25", pixelFont, 13, FontStyles.Normal, Color.white, TextAlignmentOptions.Right);
            RectTransform wepDmgTR = wepDmgObj.GetComponent<RectTransform>();
            SetAnchor(wepDmgTR, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            wepDmgTR.anchoredPosition = new Vector2(-100f, 0f);
            wepDmgTR.sizeDelta = new Vector2(80f, 30f);

            GameObject wepDpsObj = CreateTextElement("Weapon_DPS", wepRow.transform, "19.2/s", pixelFont, 13, FontStyles.Normal, Color.white, TextAlignmentOptions.Right);
            RectTransform wepDpsTR = wepDpsObj.GetComponent<RectTransform>();
            SetAnchor(wepDpsTR, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            wepDpsTR.anchoredPosition = new Vector2(0f, 0f);
            wepDpsTR.sizeDelta = new Vector2(80f, 30f);

            // Section 2: "Your stats"
            GameObject secStats = CreateUIElement("Section_Stats", pauseCard.transform);
            RectTransform secStatsTR = secStats.GetComponent<RectTransform>();
            SetAnchor(secStatsTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            secStatsTR.anchoredPosition = new Vector2(0f, -220f);
            secStatsTR.sizeDelta = new Vector2(580f, 175f);

            GameObject lblStats = CreateTextElement("Title_Stats", secStats.transform, "Your stats", pixelFont, 15, FontStyles.Bold, new Color(0.969f, 0.718f, 0.192f, 1f), TextAlignmentOptions.Left);
            RectTransform lblStatsTR = lblStats.GetComponent<RectTransform>();
            SetAnchor(lblStatsTR, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            lblStatsTR.anchoredPosition = new Vector2(10f, 0f);
            lblStatsTR.sizeDelta = new Vector2(200f, 24f);

            // 3-Column Stats Grid
            // Col 1
            CreateStatLine(secStats.transform, pixelFont, "HP", "100 / 100", new Vector2(10f, -32f), out TMP_Text txtHp);
            CreateStatLine(secStats.transform, pixelFont, "Cooldown", "1.3s", new Vector2(10f, -68f), out TMP_Text txtCooldown);
            CreateStatLine(secStats.transform, pixelFont, "Critical", "5 %", new Vector2(10f, -104f), out TMP_Text txtCritical);

            // Col 2
            CreateStatLine(secStats.transform, pixelFont, "Attack", "25", new Vector2(205f, -32f), out TMP_Text txtAttack);
            CreateStatLine(secStats.transform, pixelFont, "Range", "3.5 m", new Vector2(205f, -68f), out TMP_Text txtRange);
            CreateStatLine(secStats.transform, pixelFont, "Dmg Reduct", "0 %", new Vector2(205f, -104f), out TMP_Text txtDmgRed);

            // Col 3
            CreateStatLine(secStats.transform, pixelFont, "Speed", "4.0", new Vector2(400f, -32f), out TMP_Text txtSpeed);
            CreateStatLine(secStats.transform, pixelFont, "Knockback", "4.0", new Vector2(400f, -68f), out TMP_Text txtKnockback);
            CreateStatLine(secStats.transform, pixelFont, "Time", "00:00", new Vector2(400f, -104f), out TMP_Text txtTime);

            // Section 3: "Item"
            GameObject secItem = CreateUIElement("Section_Item", pauseCard.transform);
            RectTransform secItemTR = secItem.GetComponent<RectTransform>();
            SetAnchor(secItemTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            secItemTR.anchoredPosition = new Vector2(0f, -400f);
            secItemTR.sizeDelta = new Vector2(580f, 130f);

            GameObject lblItem = CreateTextElement("Title_Item", secItem.transform, "Item", pixelFont, 15, FontStyles.Bold, new Color(0.969f, 0.718f, 0.192f, 1f), TextAlignmentOptions.Left);
            RectTransform lblItemTR = lblItem.GetComponent<RectTransform>();
            SetAnchor(lblItemTR, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            lblItemTR.anchoredPosition = new Vector2(10f, 0f);
            lblItemTR.sizeDelta = new Vector2(200f, 24f);

            GameObject txtNoItem = CreateTextElement("Text_NoItem", secItem.transform, "No item.", pixelFont, 13, FontStyles.Normal, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Left);
            RectTransform noItemTR = txtNoItem.GetComponent<RectTransform>();
            SetAnchor(noItemTR, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            noItemTR.anchoredPosition = new Vector2(10f, -32f);
            noItemTR.sizeDelta = new Vector2(300f, 24f);

            GameObject itemContainer = CreateUIElement("Item_Icons_Container", secItem.transform);
            RectTransform itemContTR = itemContainer.GetComponent<RectTransform>();
            SetAnchor(itemContTR, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            itemContTR.anchoredPosition = new Vector2(10f, -30f);
            itemContTR.sizeDelta = new Vector2(-20f, 48f);
            HorizontalLayoutGroup hlg = itemContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Wire everything to pauseMgr
            if (pauseMgr != null)
            {
                SetPrivateField(pauseMgr, "pauseMenuCanvas", pausePanel);
                SetPrivateField(pauseMgr, "pauseHpStatText", txtHp);
                SetPrivateField(pauseMgr, "pauseAttackStatText", txtAttack);
                SetPrivateField(pauseMgr, "pauseSpeedStatText", txtSpeed);
                SetPrivateField(pauseMgr, "pauseCooldownStatText", txtCooldown);
                SetPrivateField(pauseMgr, "pauseRangeStatText", txtRange);
                SetPrivateField(pauseMgr, "pauseKnockbackStatText", txtKnockback);
                SetPrivateField(pauseMgr, "pauseCriticalStatText", txtCritical);
                SetPrivateField(pauseMgr, "pauseDmgReductionStatText", txtDmgRed);
                SetPrivateField(pauseMgr, "pauseTimeStatText", txtTime);

                SetPrivateField(pauseMgr, "pauseWeaponNameText", wepNameObj.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "pauseWeaponDamageText", wepDmgObj.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "pauseWeaponDpsText", wepDpsObj.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "pauseItemEmptyText", txtNoItem.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "pauseItemContainer", itemContainer.transform);

                Button bResume = btnContinue.GetComponent<Button>();
                bResume.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(bResume.onClick, new UnityAction(pauseMgr.Resume));

                Button bQuit = btnQuit.GetComponent<Button>();
                bQuit.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(bQuit.onClick, new UnityAction(pauseMgr.QuitToMainMenu));

                EditorUtility.SetDirty(pauseMgr);
            }

            return pausePanel;
        }

        private static GameObject BuildDefeatModal(Transform canvasTransform, TMP_FontAsset pixelFont, InGamePauseManager pauseMgr)
        {
            GameObject gameOverPanel = CreateUIElement("GameOver_Modal_Panel", canvasTransform);
            StretchFull(gameOverPanel.GetComponent<RectTransform>());
            Image goDim = gameOverPanel.AddComponent<Image>();
            goDim.color = new Color(0.043f, 0.059f, 0.098f, 0.88f); // Dark Dim

            // Card using RoundedBorderImage
            GameObject defeatCard = CreateRoundedCard("Card", gameOverPanel.transform, 16f, 4f,
                new Color(0.106f, 0.220f, 0.510f, 0.98f), // #1B3882 Deep Blue
                new Color(0.353f, 0.557f, 0.965f, 1f));   // #5A8EF6 Bright Cyan/Blue Border
            RectTransform cardTR = defeatCard.GetComponent<RectTransform>();
            SetAnchor(cardTR, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            cardTR.sizeDelta = new Vector2(640f, 710f);

            // Title "You Fell..."
            GameObject titleObj = CreateTextElement("Title", defeatCard.transform, "You Fell...", pixelFont, 28, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            RectTransform titleTR = titleObj.GetComponent<RectTransform>();
            SetAnchor(titleTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            titleTR.anchoredPosition = new Vector2(0f, -22f);
            titleTR.sizeDelta = new Vector2(300f, 36f);

            // =========================================================
            // SECTION 1: 2x2 Grid of Key Stats (TIME, LEVEL, DEFEATED, BOSSES)
            // =========================================================
            GameObject gridContainer = CreateUIElement("Stats_Grid_2x2", defeatCard.transform);
            RectTransform gridTR = gridContainer.GetComponent<RectTransform>();
            SetAnchor(gridTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            gridTR.anchoredPosition = new Vector2(0f, -66f);
            gridTR.sizeDelta = new Vector2(580f, 134f);

            // Row 1: TIME & LEVEL
            CreateStatCard(gridContainer.transform, pixelFont, "TIME", "0:45", new Vector2(-148f, 0f), new Vector2(280f, 62f), out TMP_Text txtTime);
            CreateStatCard(gridContainer.transform, pixelFont, "LEVEL", "4", new Vector2(148f, 0f), new Vector2(280f, 62f), out TMP_Text txtLevel);

            // Row 2: DEFEATED & BOSSES
            CreateStatCard(gridContainer.transform, pixelFont, "DEFEATED", "23", new Vector2(-148f, -70f), new Vector2(280f, 62f), out TMP_Text txtDefeated);
            CreateStatCard(gridContainer.transform, pixelFont, "BOSSES", "0", new Vector2(148f, -70f), new Vector2(280f, 62f), out TMP_Text txtBosses);

            // =========================================================
            // SECTION 2: EMERALDS EARNED BOX
            // =========================================================
            GameObject emBox = CreateRoundedCard("Section_Emeralds", defeatCard.transform, 8f, 2f,
                new Color(0.075f, 0.149f, 0.361f, 1f), // #13265C
                new Color(0.173f, 0.298f, 0.608f, 1f)); // #2C4C9B
            RectTransform emTR = emBox.GetComponent<RectTransform>();
            SetAnchor(emTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            emTR.anchoredPosition = new Vector2(0f, -212f);
            emTR.sizeDelta = new Vector2(580f, 76f);

            GameObject emTitle = CreateTextElement("Title", emBox.transform, "EMERALDS EARNED", pixelFont, 11, FontStyles.Bold, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Center);
            RectTransform emTitleTR = emTitle.GetComponent<RectTransform>();
            SetAnchor(emTitleTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            emTitleTR.anchoredPosition = new Vector2(0f, -8f);
            emTitleTR.sizeDelta = new Vector2(300f, 18f);

            GameObject emAmount = CreateTextElement("Amount", emBox.transform, "+30", pixelFont, 22, FontStyles.Bold, new Color(0.980f, 0.800f, 0.082f, 1f), TextAlignmentOptions.Center);
            RectTransform emAmountTR = emAmount.GetComponent<RectTransform>();
            SetAnchor(emAmountTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            emAmountTR.anchoredPosition = new Vector2(0f, -28f);
            emAmountTR.sizeDelta = new Vector2(200f, 26f);

            GameObject emFormula = CreateTextElement("Formula", emBox.transform, "Time 9 · Defeated 14 · Bonus x1.25 Fortune", pixelFont, 10, FontStyles.Normal, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Center);
            RectTransform emFormulaTR = emFormula.GetComponent<RectTransform>();
            SetAnchor(emFormulaTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            emFormulaTR.anchoredPosition = new Vector2(0f, -54f);
            emFormulaTR.sizeDelta = new Vector2(500f, 16f);

            // =========================================================
            // SECTION 3: WEAPONS ROW
            // =========================================================
            GameObject wepRowBox = CreateRoundedCard("Section_Weapons_Row", defeatCard.transform, 8f, 2f,
                new Color(0.075f, 0.149f, 0.361f, 1f),
                new Color(0.173f, 0.298f, 0.608f, 1f));
            RectTransform wepRowTR = wepRowBox.GetComponent<RectTransform>();
            SetAnchor(wepRowTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            wepRowTR.anchoredPosition = new Vector2(0f, -298f);
            wepRowTR.sizeDelta = new Vector2(580f, 54f);

            GameObject wepRowTitle = CreateTextElement("Title", wepRowBox.transform, "WEAPONS", pixelFont, 10, FontStyles.Bold, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Center);
            RectTransform wepRowTitleTR = wepRowTitle.GetComponent<RectTransform>();
            SetAnchor(wepRowTitleTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            wepRowTitleTR.anchoredPosition = new Vector2(0f, -6f);
            wepRowTitleTR.sizeDelta = new Vector2(200f, 16f);

            GameObject wepRowContent = CreateTextElement("Content", wepRowBox.transform, "Iron Sword Lv.2", pixelFont, 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            RectTransform wepRowContentTR = wepRowContent.GetComponent<RectTransform>();
            SetAnchor(wepRowContentTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            wepRowContentTR.anchoredPosition = new Vector2(0f, -26f);
            wepRowContentTR.sizeDelta = new Vector2(500f, 22f);

            // =========================================================
            // SECTION 4: RUN SUMMARY BOX
            // =========================================================
            GameObject runSummaryBox = CreateRoundedCard("Section_Run_Summary", defeatCard.transform, 8f, 2f,
                new Color(0.075f, 0.149f, 0.361f, 1f),
                new Color(0.173f, 0.298f, 0.608f, 1f));
            RectTransform rsTR = runSummaryBox.GetComponent<RectTransform>();
            SetAnchor(rsTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            rsTR.anchoredPosition = new Vector2(0f, -362f);
            rsTR.sizeDelta = new Vector2(580f, 260f);

            GameObject rsTitle = CreateTextElement("Title", runSummaryBox.transform, "RUN SUMMARY", pixelFont, 11, FontStyles.Bold, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Center);
            RectTransform rsTitleTR = rsTitle.GetComponent<RectTransform>();
            SetAnchor(rsTitleTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            rsTitleTR.anchoredPosition = new Vector2(0f, -10f);
            rsTitleTR.sizeDelta = new Vector2(300f, 18f);

            GameObject rsSubtitle = CreateTextElement("Subtitle", runSummaryBox.transform, "Steve · Lv. 4 · 1,038 damage in total", pixelFont, 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            RectTransform rsSubTR = rsSubtitle.GetComponent<RectTransform>();
            SetAnchor(rsSubTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            rsSubTR.anchoredPosition = new Vector2(0f, -32f);
            rsSubTR.sizeDelta = new Vector2(500f, 20f);

            // Columns Header
            GameObject colDmg = CreateTextElement("Col_Damage", runSummaryBox.transform, "Damage", pixelFont, 11, FontStyles.Normal, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Right);
            RectTransform colDmgTR = colDmg.GetComponent<RectTransform>();
            SetAnchor(colDmgTR, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            colDmgTR.anchoredPosition = new Vector2(-120f, -60f);
            colDmgTR.sizeDelta = new Vector2(90f, 18f);

            GameObject colDps = CreateTextElement("Col_DPS", runSummaryBox.transform, "Per second", pixelFont, 11, FontStyles.Normal, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Right);
            RectTransform colDpsTR = colDps.GetComponent<RectTransform>();
            SetAnchor(colDpsTR, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            colDpsTR.anchoredPosition = new Vector2(-20f, -60f);
            colDpsTR.sizeDelta = new Vector2(90f, 18f);

            // Weapon Stat Line
            GameObject wepStatLine = CreateUIElement("Weapon_Stat_Line", runSummaryBox.transform);
            RectTransform wslTR = wepStatLine.GetComponent<RectTransform>();
            SetAnchor(wslTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            wslTR.anchoredPosition = new Vector2(0f, -88f);
            wslTR.sizeDelta = new Vector2(540f, 28f);

            GameObject wepNameTxt = CreateTextElement("Name", wepStatLine.transform, "Iron Sword Lv.2", pixelFont, 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            RectTransform wnTR = wepNameTxt.GetComponent<RectTransform>();
            SetAnchor(wnTR, new Vector2(0f, 0f), new Vector2(0.6f, 1f), new Vector2(0f, 0.5f));
            wnTR.offsetMin = Vector2.zero;
            wnTR.offsetMax = Vector2.zero;

            GameObject wepDmgTxt = CreateTextElement("Damage", wepStatLine.transform, "1,038", pixelFont, 13, FontStyles.Normal, Color.white, TextAlignmentOptions.Right);
            RectTransform wdTR = wepDmgTxt.GetComponent<RectTransform>();
            SetAnchor(wdTR, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f));
            wdTR.anchoredPosition = new Vector2(-110f, 0f);
            wdTR.sizeDelta = new Vector2(90f, 0f);

            GameObject wepDpsTxt = CreateTextElement("DPS", wepStatLine.transform, "23/s", pixelFont, 13, FontStyles.Normal, Color.white, TextAlignmentOptions.Right);
            RectTransform wdpTR = wepDpsTxt.GetComponent<RectTransform>();
            SetAnchor(wdpTR, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f));
            wdpTR.anchoredPosition = new Vector2(-10f, 0f);
            wdpTR.sizeDelta = new Vector2(90f, 0f);

            // Item status text
            GameObject itemStatusObj = CreateTextElement("Item_Status", runSummaryBox.transform, "No item.", pixelFont, 13, FontStyles.Normal, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Center);
            RectTransform isTR = itemStatusObj.GetComponent<RectTransform>();
            SetAnchor(isTR, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            isTR.anchoredPosition = new Vector2(0f, 20f);
            isTR.sizeDelta = new Vector2(300f, 24f);

            // =========================================================
            // SECTION 5: BOTTOM BUTTONS (Try Again & Back to menu)
            // =========================================================
            GameObject btnRow = CreateUIElement("Bottom_Buttons_Row", defeatCard.transform);
            RectTransform btnRowTR = btnRow.GetComponent<RectTransform>();
            SetAnchor(btnRowTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            btnRowTR.anchoredPosition = new Vector2(0f, -638f);
            btnRowTR.sizeDelta = new Vector2(580f, 44f);

            GameObject btnRetry = CreateRoundedButtonElement("Btn_Retry", btnRow.transform, "Try Again", pixelFont, 14, 8f, 2f,
                new Color(0.063f, 0.725f, 0.506f, 1f), // Green #10B981
                Color.white,
                Color.white,
                new Vector2(-105f, 0f),
                new Vector2(195f, 42f));

            GameObject btnQuit = CreateRoundedButtonElement("Btn_BackToMenu", btnRow.transform, "Back to menu", pixelFont, 14, 8f, 2f,
                new Color(0.969f, 0.718f, 0.192f, 1f), // Gold #F7B731
                Color.white,
                new Color(0.118f, 0.153f, 0.180f, 1f), // Dark text #1E272E
                new Vector2(105f, 0f),
                new Vector2(195f, 42f));

            // Wire to InGamePauseManager
            if (pauseMgr != null)
            {
                SetPrivateField(pauseMgr, "gameOverCanvas", gameOverPanel);
                SetPrivateField(pauseMgr, "defeatTimeText", txtTime);
                SetPrivateField(pauseMgr, "defeatLevelText", txtLevel);
                SetPrivateField(pauseMgr, "defeatDefeatedText", txtDefeated);
                SetPrivateField(pauseMgr, "defeatBossesText", txtBosses);
                SetPrivateField(pauseMgr, "defeatEmeraldsText", emAmount.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "defeatEmeraldsFormulaText", emFormula.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "defeatWeaponsRowText", wepRowContent.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "defeatRunSummaryHeader", rsSubtitle.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "defeatWeaponNameText", wepNameTxt.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "defeatWeaponDamageText", wepDmgTxt.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "defeatWeaponDpsText", wepDpsTxt.GetComponent<TMP_Text>());
                SetPrivateField(pauseMgr, "defeatItemStatusText", itemStatusObj.GetComponent<TMP_Text>());

                Button bRetry = btnRetry.GetComponent<Button>();
                bRetry.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(bRetry.onClick, new UnityAction(pauseMgr.RestartRun));

                Button bQuit = btnQuit.GetComponent<Button>();
                bQuit.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(bQuit.onClick, new UnityAction(pauseMgr.QuitToMainMenu));

                EditorUtility.SetDirty(pauseMgr);
            }

            return gameOverPanel;
        }

        private static void CreateStatCard(Transform parent, TMP_FontAsset font, string label, string defaultValue, Vector2 pos, Vector2 size, out TMP_Text valText)
        {
            GameObject cardObj = CreateRoundedCard($"Card_{label}", parent, 8f, 2f,
                new Color(0.075f, 0.149f, 0.361f, 1f), // #13265C
                new Color(0.173f, 0.298f, 0.608f, 1f)); // #2C4C9B
            RectTransform tr = cardObj.GetComponent<RectTransform>();
            SetAnchor(tr, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            tr.anchoredPosition = pos;
            tr.sizeDelta = size;

            GameObject lblObj = CreateTextElement("Label", cardObj.transform, label, font, 11, FontStyles.Bold, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Center);
            RectTransform lblTR = lblObj.GetComponent<RectTransform>();
            SetAnchor(lblTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            lblTR.anchoredPosition = new Vector2(0f, -8f);
            lblTR.sizeDelta = new Vector2(size.x - 10f, 18f);

            GameObject valObj = CreateTextElement("Value", cardObj.transform, defaultValue, font, 22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            RectTransform valTR = valObj.GetComponent<RectTransform>();
            SetAnchor(valTR, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            valTR.anchoredPosition = new Vector2(0f, -28f);
            valTR.sizeDelta = new Vector2(size.x - 10f, 28f);

            valText = valObj.GetComponent<TMP_Text>();
        }

        private static void CreateStatLine(Transform parent, TMP_FontAsset font, string label, string defaultValue, Vector2 pos, out TMP_Text valText)
        {
            GameObject container = CreateUIElement($"Stat_{label}", parent);
            RectTransform tr = container.GetComponent<RectTransform>();
            SetAnchor(tr, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            tr.anchoredPosition = pos;
            tr.sizeDelta = new Vector2(180f, 28f);

            GameObject lblObj = CreateTextElement("Label", container.transform, label, font, 12, FontStyles.Normal, new Color(0.58f, 0.65f, 0.75f, 1f), TextAlignmentOptions.Left);
            RectTransform lblTR = lblObj.GetComponent<RectTransform>();
            SetAnchor(lblTR, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
            lblTR.anchoredPosition = Vector2.zero;
            lblTR.sizeDelta = new Vector2(90f, 0f);

            GameObject valObj = CreateTextElement("Value", container.transform, defaultValue, font, 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Right);
            RectTransform valTR = valObj.GetComponent<RectTransform>();
            SetAnchor(valTR, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f));
            valTR.anchoredPosition = Vector2.zero;
            valTR.sizeDelta = new Vector2(85f, 0f);

            valText = valObj.GetComponent<TMP_Text>();
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static GameObject CreateImageElement(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            obj.GetComponent<Image>().color = color;
            return obj;
        }

        private static GameObject CreateRoundedCard(string name, Transform parent, float radius, float thickness, Color fill, Color border)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBorderImage));
            obj.transform.SetParent(parent, false);
            RoundedBorderImage rbi = obj.GetComponent<RoundedBorderImage>();
            rbi.CornerRadius = radius;
            rbi.BorderThickness = thickness;
            rbi.FillColor = fill;
            rbi.BorderColor = border;
            return obj;
        }

        private static GameObject CreateTextElement(string name, Transform parent, string text, TMP_FontAsset font, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI t = obj.GetComponent<TextMeshProUGUI>();
            t.text = text;
            if (font != null) t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = alignment;
            t.raycastTarget = false;
            t.enableWordWrapping = true;
            return obj;
        }

        private static GameObject CreateButtonElement(string name, Transform parent, string label, TMP_FontAsset font, int fontSize, Color bgColor, Color textColor, Vector2 anchoredPos, Vector2 size)
        {
            GameObject btnObj = CreateImageElement(name, parent, bgColor);
            RectTransform tr = btnObj.GetComponent<RectTransform>();
            SetAnchor(tr, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            tr.anchoredPosition = anchoredPos;
            tr.sizeDelta = size;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.15f;
            cb.pressedColor = bgColor * 0.85f;
            btn.colors = cb;

            GameObject textObj = CreateTextElement("Label", btnObj.transform, label, font, fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            StretchFull(textObj.GetComponent<RectTransform>());

            return btnObj;
        }

        private static GameObject CreateRoundedButtonElement(string name, Transform parent, string label, TMP_FontAsset font, int fontSize, float radius, float borderThickness, Color fill, Color border, Color textColor, Vector2 anchoredPos, Vector2 size)
        {
            GameObject btnObj = CreateRoundedCard(name, parent, radius, borderThickness, fill, border);
            RectTransform tr = btnObj.GetComponent<RectTransform>();
            SetAnchor(tr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            tr.anchoredPosition = anchoredPos;
            tr.sizeDelta = size;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;

            GameObject textObj = CreateTextElement("Label", btnObj.transform, label, font, fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            StretchFull(textObj.GetComponent<RectTransform>());

            return btnObj;
        }

        private static void StretchFull(RectTransform tr)
        {
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
        }

        private static void Inset(RectTransform tr, float inset)
        {
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(inset, inset);
            tr.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetAnchor(RectTransform tr, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            tr.anchorMin = anchorMin;
            tr.anchorMax = anchorMax;
            tr.pivot = pivot;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
