using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.UI;

namespace PixelSurvivor.Editor
{
    public static class SettingsModalSetupHelper
    {
        private const string FONT_PATH = "Fonts/VCR_OSD_MONO SDF";

        [MenuItem("PixelSurvivor/UI: Fix Settings Click & GameOver Scale")]
        public static void FixUIWiringAndScale()
        {
            // 1. Fix GameOver_Modal_Panel scale (Scale must be (1, 1, 1), not (0, 0, 0))
            GameObject inGameCanvas = GameObject.Find("InGame_Canvas");
            if (inGameCanvas != null)
            {
                Transform goPanelTrans = inGameCanvas.transform.Find("GameOver_Modal_Panel");
                if (goPanelTrans != null)
                {
                    Undo.RecordObject(goPanelTrans, "Fix GameOver Scale");
                    goPanelTrans.localScale = Vector3.one;
                    Debug.Log("<color=#10B981>[FixUI] Fixed GameOver_Modal_Panel localScale to Vector3.one (1, 1, 1)!</color>");
                }
            }

            // 2. Wire Main Menu Buttons persistent listeners (NewGame, Settings, Quit)
            GameObject canvasObj = GameObject.Find("Canvas");
            MainMenuManager mainMgr = Object.FindAnyObjectByType<MainMenuManager>();
            if (canvasObj != null && mainMgr != null)
            {
                Transform btnContainer = canvasObj.transform.Find("MainMenuPanel/ButtonContainer");
                if (btnContainer != null)
                {
                    Button btnNew = btnContainer.Find("Btn_NewGame")?.GetComponent<Button>();
                    if (btnNew != null)
                    {
                        UnityEventTools.RemovePersistentListener(btnNew.onClick, mainMgr.OnClickNewGame);
                        UnityEventTools.AddPersistentListener(btnNew.onClick, mainMgr.OnClickNewGame);
                    }

                    Button btnSet = btnContainer.Find("Btn_Settings")?.GetComponent<Button>();
                    if (btnSet != null)
                    {
                        UnityEventTools.RemovePersistentListener(btnSet.onClick, mainMgr.OnClickSettings);
                        UnityEventTools.AddPersistentListener(btnSet.onClick, mainMgr.OnClickSettings);
                    }

                    Button btnQuit = btnContainer.Find("Btn_Quit")?.GetComponent<Button>();
                    if (btnQuit != null)
                    {
                        UnityEventTools.RemovePersistentListener(btnQuit.onClick, mainMgr.OnClickQuit);
                        UnityEventTools.AddPersistentListener(btnQuit.onClick, mainMgr.OnClickQuit);
                    }
                    Debug.Log("<color=#10B981>[FixUI] Wired persistent onClick listeners on MainMenu buttons!</color>");
                }

                // Ensure SettingsPanel reference on MainMenuManager
                Transform setPanelTrans = canvasObj.transform.Find("SettingsPanel");
                if (setPanelTrans != null)
                {
                    SerializedObject so = new SerializedObject(mainMgr);
                    so.FindProperty("settingsPanel").objectReferenceValue = setPanelTrans.gameObject;
                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=#10B981>[FixUI] Successfully applied fixes and saved scene!</color>");
        }

        [MenuItem("PixelSurvivor/UI: Build Settings Modal (From JSON)")]
        public static void BuildSettingsModal()
        {
            TMP_FontAsset pixelFont = Resources.Load<TMP_FontAsset>(FONT_PATH);
            if (pixelFont == null)
            {
                Debug.LogError($"[SettingsModalSetupHelper] Could not find font asset at Resources/{FONT_PATH}!");
                return;
            }

            // 1. Locate Canvas (main menu canvas)
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[SettingsModalSetupHelper] Canvas 'Canvas' not found in active scene!");
                return;
            }

            // 2. Locate or create SettingsPanel
            Transform settingsPanelTrans = canvasObj.transform.Find("SettingsPanel");
            GameObject settingsPanelObj;
            if (settingsPanelTrans == null)
            {
                settingsPanelObj = new GameObject("SettingsPanel");
                settingsPanelObj.transform.SetParent(canvasObj.transform, false);
                Undo.RegisterCreatedObjectUndo(settingsPanelObj, "Create SettingsPanel");
            }
            else
            {
                settingsPanelObj = settingsPanelTrans.gameObject;
            }

            // Ensure full-stretch on SettingsPanel background overlay
            RectTransform panelRT = settingsPanelObj.GetComponent<RectTransform>();
            if (panelRT == null) panelRT = settingsPanelObj.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            // Semi-transparent dark backdrop
            Image backdrop = settingsPanelObj.GetComponent<Image>();
            if (backdrop == null) backdrop = settingsPanelObj.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.75f);
            backdrop.raycastTarget = true;

            // Clear old children of SettingsPanel
            while (settingsPanelObj.transform.childCount > 0)
            {
                Undo.DestroyObjectImmediate(settingsPanelObj.transform.GetChild(0).gameObject);
            }

            // =========================================================
            // 3. MAIN MODAL BOX (Width 680, Height 520, Rounded Border)
            // =========================================================
            GameObject boxObj = new GameObject("SettingsBox");
            boxObj.transform.SetParent(settingsPanelObj.transform, false);
            RectTransform boxRT = boxObj.AddComponent<RectTransform>();
            boxRT.sizeDelta = new Vector2(680f, 520f);
            boxRT.anchoredPosition = Vector2.zero;

            RoundedBorderImage boxBorder = boxObj.AddComponent<RoundedBorderImage>();
            boxBorder.CornerRadius = 16f;
            boxBorder.BorderThickness = 4f;
            boxBorder.FillColor = new Color(0.106f, 0.220f, 0.510f, 1f); // #1B3882
            boxBorder.BorderColor = new Color(0.353f, 0.557f, 0.965f, 1f); // #5A8EF6

            // =========================================================
            // 4. HEADER: Title & Close Button
            // =========================================================
            // Title
            GameObject titleObj = CreateText("Title", boxObj.transform, "Settings", pixelFont, 28, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(0f, 1f);
            titleRT.anchoredPosition = new Vector2(28f, -38f);
            titleRT.sizeDelta = new Vector2(240f, 40f);

            // Close button (Top-Right)
            GameObject closeBtnObj = CreateButton("Btn_Close", boxObj.transform, "Close", pixelFont, 14,
                new Vector2(680f - 28f - 85f, -22f), new Vector2(85f, 36f),
                new Color(0.125f, 0.251f, 0.596f, 1f), Color.white, 8f, 2f, Color.white,
                out Button closeBtn, out RoundedBorderImage closeBorder, out TMP_Text closeText);
            RectTransform closeRT = closeBtnObj.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(0f, 1f);
            closeRT.anchorMax = new Vector2(0f, 1f);

            // =========================================================
            // 5. TAB NAVIGATION BAR (Sound, Controls, About)
            // =========================================================
            GameObject tabBarObj = new GameObject("Tab_Bar");
            tabBarObj.transform.SetParent(boxObj.transform, false);
            RectTransform tabBarRT = tabBarObj.AddComponent<RectTransform>();
            tabBarRT.anchorMin = new Vector2(0f, 1f);
            tabBarRT.anchorMax = new Vector2(0f, 1f);
            tabBarRT.anchoredPosition = new Vector2(28f, -80f);
            tabBarRT.sizeDelta = new Vector2(624f, 40f);

            // Sound Tab
            GameObject soundTabObj = CreateButton("Tab_Sound", tabBarObj.transform, "Sound", pixelFont, 14,
                new Vector2(0f, 0f), new Vector2(115f, 36f),
                new Color(0.094f, 0.204f, 0.471f, 1f), new Color(0.980f, 0.800f, 0.082f, 1f), 8f, 3.5f, Color.white,
                out Button soundTabBtn, out RoundedBorderImage soundTabBorder, out TMP_Text soundTabText);
            SetTopLeftAnchor(soundTabObj.GetComponent<RectTransform>());

            // Controls Tab
            GameObject controlsTabObj = CreateButton("Tab_Controls", tabBarObj.transform, "Controls", pixelFont, 14,
                new Vector2(125f, 0f), new Vector2(125f, 36f),
                new Color(0.075f, 0.153f, 0.361f, 1f), new Color(0.169f, 0.290f, 0.573f, 1f), 8f, 2f, new Color(0.82f, 0.85f, 0.88f, 1f),
                out Button controlsTabBtn, out RoundedBorderImage controlsTabBorder, out TMP_Text controlsTabText);
            SetTopLeftAnchor(controlsTabObj.GetComponent<RectTransform>());

            // About Tab
            GameObject aboutTabObj = CreateButton("Tab_About", tabBarObj.transform, "About", pixelFont, 14,
                new Vector2(260f, 0f), new Vector2(115f, 36f),
                new Color(0.075f, 0.153f, 0.361f, 1f), new Color(0.169f, 0.290f, 0.573f, 1f), 8f, 2f, new Color(0.82f, 0.85f, 0.88f, 1f),
                out Button aboutTabBtn, out RoundedBorderImage aboutTabBorder, out TMP_Text aboutTabText);
            SetTopLeftAnchor(aboutTabObj.GetComponent<RectTransform>());

            // =========================================================
            // 6. CONTENT CONTAINER (Width 624, Height 360)
            // =========================================================
            GameObject contentArea = new GameObject("Content_Area");
            contentArea.transform.SetParent(boxObj.transform, false);
            RectTransform contentRT = contentArea.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 0f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.offsetMin = new Vector2(28f, 20f);
            contentRT.offsetMax = new Vector2(-28f, -130f);

            // ---------------------------------------------------------
            // TAB 1 CONTENT: SOUND (Active)
            // ---------------------------------------------------------
            GameObject soundPanel = new GameObject("Panel_Sound");
            soundPanel.transform.SetParent(contentArea.transform, false);
            StretchFull(soundPanel.AddComponent<RectTransform>());

            GameObject soundTitle = CreateText("Section_Title", soundPanel.transform, "Sound", pixelFont, 20, FontStyles.Bold, new Color(0.980f, 0.800f, 0.082f, 1f), TextAlignmentOptions.Left);
            RectTransform stRT = soundTitle.GetComponent<RectTransform>();
            SetTopLeftAnchor(stRT);
            stRT.anchoredPosition = new Vector2(0f, -10f);
            stRT.sizeDelta = new Vector2(200f, 30f);

            // Music Slider
            CreateVolumeSlider("Slider_Music", soundPanel.transform, pixelFont, "Music", new Vector2(0f, -65f), out Slider musicSlider);

            // Effects Slider
            CreateVolumeSlider("Slider_Effects", soundPanel.transform, pixelFont, "Effects", new Vector2(0f, -125f), out Slider effectsSlider);

            // Mute / Unmute Button
            GameObject muteBtnObj = CreateButton("Btn_ToggleMute", soundPanel.transform, "Unmute", pixelFont, 15,
                new Vector2(0f, -185f), new Vector2(140f, 44f),
                new Color(0.961f, 0.620f, 0.043f, 1f), Color.white, 8f, 2f, new Color(0.118f, 0.153f, 0.180f, 1f),
                out Button muteBtn, out RoundedBorderImage muteBorder, out TMP_Text muteText);
            SetTopLeftAnchor(muteBtnObj.GetComponent<RectTransform>());

            // ---------------------------------------------------------
            // TAB 2 CONTENT: CONTROLS (Inactive)
            // ---------------------------------------------------------
            GameObject controlsPanel = new GameObject("Panel_Controls");
            controlsPanel.transform.SetParent(contentArea.transform, false);
            StretchFull(controlsPanel.AddComponent<RectTransform>());
            controlsPanel.SetActive(false);

            GameObject controlsTitle = CreateText("Section_Title", controlsPanel.transform, "Controls", pixelFont, 20, FontStyles.Bold, new Color(0.980f, 0.800f, 0.082f, 1f), TextAlignmentOptions.Left);
            RectTransform ctRT = controlsTitle.GetComponent<RectTransform>();
            SetTopLeftAnchor(ctRT);
            ctRT.anchoredPosition = new Vector2(0f, -10f);
            ctRT.sizeDelta = new Vector2(200f, 30f);

            // Keybindings list container
            float rowY = -55f;
            CreateKeybindRow(controlsPanel.transform, pixelFont, rowY, new string[] { "W", "A", "S", "D" }, "or arrow keys", "Move character");
            rowY -= 45f;
            CreateKeybindRow(controlsPanel.transform, pixelFont, rowY, new string[] { "Auto" }, "in attack range", "Auto slash closest enemy");
            rowY -= 45f;
            CreateKeybindRow(controlsPanel.transform, pixelFont, rowY, new string[] { "Esc", "P" }, "or", "Pause game");
            rowY -= 45f;
            CreateKeybindRow(controlsPanel.transform, pixelFont, rowY, new string[] { "1", "2", "3" }, "or click", "Choose upgrade card (2 clicks to confirm)");
            rowY -= 45f;
            CreateKeybindRow(controlsPanel.transform, pixelFont, rowY, new string[] { "R" }, "", "Reroll upgrade cards");
            rowY -= 45f;
            CreateKeybindRow(controlsPanel.transform, pixelFont, rowY, new string[] { "Space", "Enter" }, "or", "Confirm selected card");

            // ---------------------------------------------------------
            // TAB 3 CONTENT: ABOUT (Inactive)
            // ---------------------------------------------------------
            GameObject aboutPanel = new GameObject("Panel_About");
            aboutPanel.transform.SetParent(contentArea.transform, false);
            StretchFull(aboutPanel.AddComponent<RectTransform>());
            aboutPanel.SetActive(false);

            // Title
            GameObject gameTitle = CreateText("Game_Title", aboutPanel.transform, "Pixel Survivor", pixelFont, 28, FontStyles.Bold, new Color(0.980f, 0.800f, 0.082f, 1f), TextAlignmentOptions.Center);
            RectTransform gtRT = gameTitle.GetComponent<RectTransform>();
            gtRT.anchorMin = new Vector2(0f, 1f);
            gtRT.anchorMax = new Vector2(1f, 1f);
            gtRT.anchoredPosition = new Vector2(0f, -18f);
            gtRT.sizeDelta = new Vector2(0f, 35f);

            // Version
            GameObject verText = CreateText("Version_Text", aboutPanel.transform, "Version 1.0 · 2026-10-29", pixelFont, 13, FontStyles.Normal, new Color(0.796f, 0.835f, 0.882f, 1f), TextAlignmentOptions.Center);
            RectTransform vtRT = verText.GetComponent<RectTransform>();
            vtRT.anchorMin = new Vector2(0f, 1f);
            vtRT.anchorMax = new Vector2(1f, 1f);
            vtRT.anchoredPosition = new Vector2(0f, -50f);
            vtRT.sizeDelta = new Vector2(0f, 22f);

            // Disclaimer
            GameObject discText = CreateText("Disclaimer", aboutPanel.transform, "Fan game Free and non-profit: no ads or purchases. Fast-Paced Roguelite Action Survivor (Minecraft Aesthetic).", pixelFont, 12, FontStyles.Normal, new Color(0.58f, 0.64f, 0.72f, 1f), TextAlignmentOptions.Center);
            RectTransform dtRT = discText.GetComponent<RectTransform>();
            dtRT.anchorMin = new Vector2(0f, 1f);
            dtRT.anchorMax = new Vector2(1f, 1f);
            dtRT.anchoredPosition = new Vector2(0f, -76f);
            dtRT.sizeDelta = new Vector2(0f, 22f);

            // Credits Header
            GameObject creditsTitle = CreateText("Credits_Title", aboutPanel.transform, "Credits", pixelFont, 16, FontStyles.Bold, new Color(0.980f, 0.800f, 0.082f, 1f), TextAlignmentOptions.Left);
            RectTransform crtRT = creditsTitle.GetComponent<RectTransform>();
            SetTopLeftAnchor(crtRT);
            crtRT.anchoredPosition = new Vector2(20f, -115f);
            crtRT.sizeDelta = new Vector2(200f, 25f);

            // Credits Lines
            float cY = -145f;
            CreateCreditLine(aboutPanel.transform, pixelFont, "• Sprites", "Minecraft style assets & custom pixel art spritesheets", cY);
            cY -= 30f;
            CreateCreditLine(aboutPanel.transform, pixelFont, "• Engine", "Unity 6 LTS (6000.4.10f1, URP 2D)", cY);
            cY -= 30f;
            CreateCreditLine(aboutPanel.transform, pixelFont, "• Music and sounds", "Retro 8-bit chiptune sound effects & tunes", cY);
            cY -= 30f;
            CreateCreditLine(aboutPanel.transform, pixelFont, "• Font", "VCR OSD Mono SDF (TextMeshPro OFL)", cY);

            // =========================================================
            // 7. ATTACH AND WIRE SettingsModalUI
            // =========================================================
            SettingsModalUI modalUI = settingsPanelObj.GetComponent<SettingsModalUI>();
            if (modalUI == null) modalUI = settingsPanelObj.AddComponent<SettingsModalUI>();

            SerializedObject so = new SerializedObject(modalUI);
            so.FindProperty("btnClose").objectReferenceValue = closeBtn;

            so.FindProperty("tabSoundBtn").objectReferenceValue = soundTabBtn;
            so.FindProperty("tabControlsBtn").objectReferenceValue = controlsTabBtn;
            so.FindProperty("tabAboutBtn").objectReferenceValue = aboutTabBtn;

            so.FindProperty("tabSoundBorder").objectReferenceValue = soundTabBorder;
            so.FindProperty("tabControlsBorder").objectReferenceValue = controlsTabBorder;
            so.FindProperty("tabAboutBorder").objectReferenceValue = aboutTabBorder;

            so.FindProperty("tabSoundText").objectReferenceValue = soundTabText;
            so.FindProperty("tabControlsText").objectReferenceValue = controlsTabText;
            so.FindProperty("tabAboutText").objectReferenceValue = aboutTabText;

            so.FindProperty("soundContentPanel").objectReferenceValue = soundPanel;
            so.FindProperty("controlsContentPanel").objectReferenceValue = controlsPanel;
            so.FindProperty("aboutContentPanel").objectReferenceValue = aboutPanel;

            so.FindProperty("musicSlider").objectReferenceValue = musicSlider;
            so.FindProperty("effectsSlider").objectReferenceValue = effectsSlider;
            so.FindProperty("btnToggleMute").objectReferenceValue = muteBtn;
            so.FindProperty("muteButtonText").objectReferenceValue = muteText;

            so.ApplyModifiedProperties();

            // 8. Re-wire MainMenuManager settingsPanel reference
            MainMenuManager mainMgr = Object.FindAnyObjectByType<MainMenuManager>();
            if (mainMgr != null)
            {
                SerializedObject mmSO = new SerializedObject(mainMgr);
                SerializedProperty sp = mmSO.FindProperty("settingsPanel");
                if (sp != null)
                {
                    sp.objectReferenceValue = settingsPanelObj;
                    mmSO.ApplyModifiedProperties();
                }
            }

            // Keep settingsPanel inactive initially by default
            settingsPanelObj.SetActive(false);

            // Wire all buttons and ensure GameOver scale is 1
            FixUIWiringAndScale();

            Debug.Log("<color=#10B981>[SettingsModalSetupHelper] SUCCESS!</color> Settings Modal UI built completely from JSON specifications and wired to MainMenuManager!");
        }

        private static GameObject CreateText(string name, Transform parent, string content, TMP_FontAsset font, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.font = font;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, TMP_FontAsset font, float textSize,
            Vector2 pos, Vector2 size, Color fill, Color border, float radius, float thickness, Color textColor,
            out Button btn, out RoundedBorderImage rbi, out TMP_Text txt)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            rbi = go.AddComponent<RoundedBorderImage>();
            rbi.CornerRadius = radius;
            rbi.BorderThickness = thickness;
            rbi.FillColor = fill;
            rbi.BorderColor = border;

            btn = go.AddComponent<Button>();
            btn.targetGraphic = rbi;
            btn.transition = Selectable.Transition.None;

            go.AddComponent<MinecraftButtonFeedback>();

            GameObject textObj = CreateText("Label", go.transform, label, font, textSize, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            StretchFull(textObj.GetComponent<RectTransform>());
            txt = textObj.GetComponent<TMP_Text>();

            return go;
        }

        private static void CreateVolumeSlider(string name, Transform parent, TMP_FontAsset font, string label, Vector2 pos, out Slider slider)
        {
            GameObject rowObj = new GameObject(name + "_Row");
            rowObj.transform.SetParent(parent, false);
            RectTransform rowRT = rowObj.AddComponent<RectTransform>();
            SetTopLeftAnchor(rowRT);
            rowRT.anchoredPosition = pos;
            rowRT.sizeDelta = new Vector2(624f, 40f);

            // Icon & Label
            GameObject lblObj = CreateText("Label", rowObj.transform, "🔊  " + label, font, 16, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);
            RectTransform lblRT = lblObj.GetComponent<RectTransform>();
            SetTopLeftAnchor(lblRT);
            lblRT.anchoredPosition = new Vector2(0f, 0f);
            lblRT.sizeDelta = new Vector2(160f, 36f);

            // Slider Container
            GameObject sldObj = new GameObject(name);
            sldObj.transform.SetParent(rowObj.transform, false);
            RectTransform sldRT = sldObj.AddComponent<RectTransform>();
            SetTopLeftAnchor(sldRT);
            sldRT.anchoredPosition = new Vector2(170f, -4f);
            sldRT.sizeDelta = new Vector2(320f, 28f);

            slider = sldObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.75f;

            // Background Track
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sldObj.transform, false);
            RectTransform bgRT = bgObj.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.35f);
            bgRT.anchorMax = new Vector2(1f, 0.65f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            RoundedBorderImage bgImg = bgObj.AddComponent<RoundedBorderImage>();
            bgImg.CornerRadius = 6f;
            bgImg.BorderThickness = 0f;
            bgImg.FillColor = new Color(0.063f, 0.133f, 0.314f, 1f); // #102250

            // Fill Area & Fill
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sldObj.transform, false);
            RectTransform faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0f, 0.35f);
            faRT.anchorMax = new Vector2(1f, 0.65f);
            faRT.offsetMin = new Vector2(4f, 0f);
            faRT.offsetMax = new Vector2(-4f, 0f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRT = fillObj.AddComponent<RectTransform>();
            fillRT.sizeDelta = Vector2.zero;
            RoundedBorderImage fillImg = fillObj.AddComponent<RoundedBorderImage>();
            fillImg.CornerRadius = 4f;
            fillImg.BorderThickness = 0f;
            fillImg.FillColor = new Color(0.220f, 0.357f, 0.686f, 1f); // #385BAF
            slider.fillRect = fillRT;

            // Handle Slide Area & Handle
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sldObj.transform, false);
            RectTransform haRT = handleArea.AddComponent<RectTransform>();
            haRT.anchorMin = new Vector2(0f, 0f);
            haRT.anchorMax = new Vector2(1f, 1f);
            haRT.offsetMin = new Vector2(12f, 0f);
            haRT.offsetMax = new Vector2(-12f, 0f);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform, false);
            RectTransform handleRT = handleObj.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(24f, 24f);
            RoundedBorderImage handleImg = handleObj.AddComponent<RoundedBorderImage>();
            handleImg.CornerRadius = 6f;
            handleImg.BorderThickness = 2f;
            handleImg.FillColor = new Color(0.980f, 0.800f, 0.082f, 1f); // #FACC15
            handleImg.BorderColor = Color.white;
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;
        }

        private static void CreateKeybindRow(Transform parent, TMP_FontAsset font, float y, string[] keys, string extra, string desc)
        {
            GameObject rowObj = new GameObject("Row_" + desc.Replace(" ", "_"));
            rowObj.transform.SetParent(parent, false);
            RectTransform rowRT = rowObj.AddComponent<RectTransform>();
            SetTopLeftAnchor(rowRT);
            rowRT.anchoredPosition = new Vector2(10f, y);
            rowRT.sizeDelta = new Vector2(600f, 36f);

            // Container for keys
            float curX = 0f;
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                float w = Mathf.Max(32f, k.Length * 12f + 16f);

                GameObject pillObj = new GameObject("Pill_" + k);
                pillObj.transform.SetParent(rowObj.transform, false);
                RectTransform pillRT = pillObj.AddComponent<RectTransform>();
                SetTopLeftAnchor(pillRT);
                pillRT.anchoredPosition = new Vector2(curX, 0f);
                pillRT.sizeDelta = new Vector2(w, 28f);

                RoundedBorderImage pillImg = pillObj.AddComponent<RoundedBorderImage>();
                pillImg.CornerRadius = 6f;
                pillImg.BorderThickness = 1.5f;
                pillImg.FillColor = new Color(0.125f, 0.251f, 0.596f, 1f); // #204098
                pillImg.BorderColor = Color.white;

                GameObject pTxt = CreateText("Key", pillObj.transform, k, font, 12, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                StretchFull(pTxt.GetComponent<RectTransform>());

                curX += w + 6f;
            }

            if (!string.IsNullOrEmpty(extra))
            {
                GameObject extraTxt = CreateText("Extra", rowObj.transform, extra, font, 13, FontStyles.Normal, new Color(0.796f, 0.835f, 0.882f, 1f), TextAlignmentOptions.Left);
                RectTransform etRT = extraTxt.GetComponent<RectTransform>();
                SetTopLeftAnchor(etRT);
                etRT.anchoredPosition = new Vector2(curX + 4f, -4f);
                etRT.sizeDelta = new Vector2(140f, 26f);
                curX += 145f;
            }
            else
            {
                curX = Mathf.Max(curX, 160f);
            }

            // Action Description
            GameObject descTxt = CreateText("Desc", rowObj.transform, desc, font, 14, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);
            RectTransform descRT = descTxt.GetComponent<RectTransform>();
            SetTopLeftAnchor(descRT);
            descRT.anchoredPosition = new Vector2(Mathf.Max(260f, curX + 15f), -4f);
            descRT.sizeDelta = new Vector2(340f, 26f);
        }

        private static void CreateCreditLine(Transform parent, TMP_FontAsset font, string category, string detail, float y)
        {
            GameObject lineObj = new GameObject("Line_" + category.Replace("• ", ""));
            lineObj.transform.SetParent(parent, false);
            RectTransform lineRT = lineObj.AddComponent<RectTransform>();
            SetTopLeftAnchor(lineRT);
            lineRT.anchoredPosition = new Vector2(20f, y);
            lineRT.sizeDelta = new Vector2(580f, 26f);

            GameObject catTxt = CreateText("Category", lineObj.transform, category + ":", font, 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            RectTransform cRT = catTxt.GetComponent<RectTransform>();
            SetTopLeftAnchor(cRT);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta = new Vector2(150f, 26f);

            GameObject detTxt = CreateText("Detail", lineObj.transform, detail, font, 13, FontStyles.Normal, new Color(0.85f, 0.88f, 0.92f, 1f), TextAlignmentOptions.Left);
            RectTransform dRT = detTxt.GetComponent<RectTransform>();
            SetTopLeftAnchor(dRT);
            dRT.anchoredPosition = new Vector2(155f, 0f);
            dRT.sizeDelta = new Vector2(420f, 26f);
        }

        private static void SetTopLeftAnchor(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
