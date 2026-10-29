using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PixelSurvivor.Editor
{
    public static class SceneSetupHelper
    {
        [MenuItem("PixelSurvivor/Layout: Split Scene & Game View")]
        public static void SetSplitSceneAndGameLayout()
        {
            EditorApplication.ExecuteMenuItem("Window/Layouts/2 by 3");
            Debug.Log("[PixelSurvivor] Layout switched to 2 by 3 (Scene & Game side-by-side)!");
        }

        [MenuItem("PixelSurvivor/Layout: Reset to Default")]
        public static void SetDefaultLayout()
        {
            EditorApplication.ExecuteMenuItem("Window/Layouts/Default");
            Debug.Log("[PixelSurvivor] Layout reset to Default!");
        }

        [MenuItem("PixelSurvivor/Graphics: Optimize Crisp Pixel Quality")]
        public static void OptimizeCrispPixelQuality()
        {
            // 1. Ensure all textures in PixelSurvivor use Point filter and Uncompressed
            var guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { "Assets/PixelSurvivor" });
            int fixedCount = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null)
                {
                    bool modified = false;
                    if (ti.filterMode != FilterMode.Point)
                    {
                        ti.filterMode = FilterMode.Point;
                        modified = true;
                    }
                    if (ti.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        ti.textureCompression = TextureImporterCompression.Uncompressed;
                        modified = true;
                    }
                    if (modified)
                    {
                        ti.SaveAndReimport();
                        fixedCount++;
                    }
                }
            }

            // 2. Set Font Material to Point
            string[] fontPaths = new string[] {
                "Assets/PixelSurvivor/UI/Fonts/VCR_OSD_MONO_1.001.ttf",
                "Assets/PixelSurvivor/UI/Fonts/Minecraftia-Regular.ttf"
            };
            foreach (var fp in fontPaths)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(fp);
                if (font != null && font.material != null && font.material.mainTexture != null)
                {
                    font.material.mainTexture.filterMode = FilterMode.Point;
                }
            }

            // 3. Disable Low Resolution in GameView
            var gvType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gvType != null)
            {
                var gv = EditorWindow.GetWindow(gvType);
                var lowResProp = gvType.GetProperty("lowResolutionForAspectRatios", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                lowResProp?.SetValue(gv, false, null);
                gv?.Repaint();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelSurvivor] Graphics optimized: Fixed {fixedCount} textures to Point/Uncompressed, Font texture to Point, and GameView Low-Res disabled!");
        }

        [MenuItem("PixelSurvivor/Edit View/Show & Focus: Character Select (Pedestals)")]
        public static void ShowCharacterSelectEditView()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var mm = canvas.transform.Find("MainMenuPanel")?.gameObject;
            var cs = canvas.transform.Find("CharacterSelectPanel")?.gameObject;
            var st = canvas.transform.Find("SettingsPanel")?.gameObject;
            var hud = canvas.transform.Find("GameHUDPanel")?.gameObject;

            if (mm != null) mm.SetActive(false);
            if (st != null) st.SetActive(false);
            if (hud != null) hud.SetActive(false);
            if (cs != null)
            {
                cs.SetActive(true);
                Selection.activeGameObject = cs.transform.Find("Pedestal_Steve")?.gameObject ?? cs;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }
            Debug.Log("[PixelSurvivor] Switched to Character Select edit view!");
        }

        [MenuItem("PixelSurvivor/Edit View/Show & Focus: Main Menu")]
        public static void ShowMainMenuEditView()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var mm = canvas.transform.Find("MainMenuPanel")?.gameObject;
            var cs = canvas.transform.Find("CharacterSelectPanel")?.gameObject;
            var st = canvas.transform.Find("SettingsPanel")?.gameObject;
            var hud = canvas.transform.Find("GameHUDPanel")?.gameObject;

            if (cs != null) cs.SetActive(false);
            if (st != null) st.SetActive(false);
            if (hud != null) hud.SetActive(false);
            if (mm != null)
            {
                mm.SetActive(true);
                Selection.activeGameObject = mm;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }
            Debug.Log("[PixelSurvivor] Switched to Main Menu edit view!");
        }

        [MenuItem("PixelSurvivor/Build Main Game Scene")]
        public static void BuildMainGameScene()
        {
            // Create a fresh scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Load Assets
            Font mcFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/PixelSurvivor/UI/Fonts/VCR_OSD_MONO_1.001.ttf");
            Sprite bgMainMenu = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/UI/Backgrounds/bg_main_menu.png");
            Sprite bgCharSelect = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/UI/Backgrounds/bg_character_select.png");
            Sprite titleBanner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/UI/Banners/title_banner_pixel_survivor.png");
            Sprite btnBlank = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/UI/Buttons/button_minecraft_blank.png");

            // Load Realm Map Sprites (16 sliced tiles each)
            Sprite[] overworldSprites = LoadSpritesSorted("Assets/PixelSurvivor/Maps/map_overworld.png");
            Sprite[] netherSprites = LoadSpritesSorted("Assets/PixelSurvivor/Maps/map_nether.png");
            Sprite[] theEndSprites = LoadSpritesSorted("Assets/PixelSurvivor/Maps/map_the_end.png");

            // Load Character Sprites with curated seamless loop sequences (removes stutter/pause frames)
            Sprite[] steveIdleRaw = LoadSpritesSorted("Assets/PixelSurvivor/Characters/Steve/steve_idle.png");
            Sprite[] steveRunRaw = LoadSpritesSorted("Assets/PixelSurvivor/Characters/Steve/steve_walk.png");
            Sprite[] steveIdle = steveIdleRaw;
            Sprite[] steveRun = SelectSpriteSequence(steveRunRaw, new int[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 0 });
            UpdateAnimationClip("Assets/PixelSurvivor/Animations/Characters/Steve/steve_walk.anim", steveRun, 10f);
            UpdateAnimationClip("Assets/PixelSurvivor/Animations/Characters/Steve/steve_idle.anim", steveIdle, 8f);
            RuntimeAnimatorController steveCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/PixelSurvivor/Animations/Characters/Steve/steve_animator.controller");

            Sprite[] alexIdleRaw = LoadSpritesSorted("Assets/PixelSurvivor/Characters/Alex/alex_idle.png");
            Sprite[] alexRunRaw = LoadSpritesSorted("Assets/PixelSurvivor/Characters/Alex/alex_walk.png");
            Sprite[] alexIdle = SelectSpriteSequence(alexIdleRaw, new int[] { 0, 1, 2, 3, 4, 6, 7, 8 });
            Sprite[] alexRun = SelectSpriteSequence(alexRunRaw, new int[] { 2, 4, 5, 6, 7, 8, 10, 11, 13, 14 });
            UpdateAnimationClip("Assets/PixelSurvivor/Animations/Characters/Alex/alex_walk.anim", alexRun, 10f);
            UpdateAnimationClip("Assets/PixelSurvivor/Animations/Characters/Alex/alex_idle.anim", alexIdle, 8f);
            RuntimeAnimatorController alexCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/PixelSurvivor/Animations/Characters/Alex/alex_animator.controller");

            Sprite[] villagerIdleRaw = LoadSpritesSorted("Assets/PixelSurvivor/Characters/Villager/villager_idle.png");
            Sprite[] villagerRunRaw = LoadSpritesSorted("Assets/PixelSurvivor/Characters/Villager/villager_walk.png");
            Sprite[] villagerIdle = villagerIdleRaw;
            Sprite[] villagerRun = SelectSpriteSequence(villagerRunRaw, new int[] { 0, 2, 4, 6, 8, 10, 12, 14 });
            UpdateAnimationClip("Assets/PixelSurvivor/Animations/Characters/Villager/villager_walk.anim", villagerRun, 10f);
            UpdateAnimationClip("Assets/PixelSurvivor/Animations/Characters/Villager/villager_idle.anim", villagerIdle, 8f);
            RuntimeAnimatorController villagerCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/PixelSurvivor/Animations/Characters/Villager/villager_animator.controller");

            // 2. Camera Setup
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.1f, 0.15f, 1f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();
            CameraFollow2D camFollow = camGo.AddComponent<CameraFollow2D>();

            // 3. Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            Light dirLight = lightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 1.0f;
            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 4. Player Setup
            GameObject playerGo = new GameObject("Player");
            playerGo.transform.position = Vector3.zero;
            SpriteRenderer playerSr = playerGo.AddComponent<SpriteRenderer>();
            playerSr.sprite = steveIdle != null && steveIdle.Length > 0 ? steveIdle[0] : null;
            playerSr.sortingOrder = 5;

            Rigidbody2D playerRb = playerGo.AddComponent<Rigidbody2D>();
            playerRb.gravityScale = 0f;
            playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            playerRb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D playerCol = playerGo.AddComponent<CircleCollider2D>();
            playerCol.radius = 0.35f;
            playerCol.offset = new Vector2(0f, 0f);

            Animator playerAnim = playerGo.AddComponent<Animator>();
            playerAnim.runtimeAnimatorController = steveCtrl;

            SimpleSpriteAnimator playerSpriteAnim = playerGo.AddComponent<SimpleSpriteAnimator>();
            playerSpriteAnim.SetFrames(steveIdle, steveRun, 8f);

            PlayerController playerCtrl = playerGo.AddComponent<PlayerController>();

            // 5. Infinite Map Generator
            GameObject mapGenGo = new GameObject("InfiniteMapGenerator");
            InfiniteMapGenerator mapGen = mapGenGo.AddComponent<InfiniteMapGenerator>();
            // Use SerializedObject to assign private serialized fields safely
            SerializedObject mapGenSo = new SerializedObject(mapGen);
            mapGenSo.FindProperty("playerTransform").objectReferenceValue = playerGo.transform;
            mapGenSo.FindProperty("chunkSize").intValue = 16;
            mapGenSo.FindProperty("viewDistance").intValue = 1; // 3x3 chunks (48x48 units)
            mapGenSo.FindProperty("noiseScale").floatValue = 0.08f;
            mapGenSo.FindProperty("noiseThreshold").floatValue = 0.50f;
            
            // Assign Overworld tiles
            SerializedProperty owProp = mapGenSo.FindProperty("overworldTiles");
            owProp.arraySize = overworldSprites.Length;
            for (int i = 0; i < overworldSprites.Length; i++) owProp.GetArrayElementAtIndex(i).objectReferenceValue = overworldSprites[i];

            // Assign Nether tiles
            SerializedProperty netherProp = mapGenSo.FindProperty("netherTiles");
            netherProp.arraySize = netherSprites.Length;
            for (int i = 0; i < netherSprites.Length; i++) netherProp.GetArrayElementAtIndex(i).objectReferenceValue = netherSprites[i];

            // Assign The End tiles
            SerializedProperty endProp = mapGenSo.FindProperty("theEndTiles");
            endProp.arraySize = theEndSprites.Length;
            for (int i = 0; i < theEndSprites.Length; i++) endProp.GetArrayElementAtIndex(i).objectReferenceValue = theEndSprites[i];

            mapGenSo.ApplyModifiedProperties();

            // 6. Event System
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            // 7. UI Canvas Setup
            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Manager GameObject
            GameObject managerGo = new GameObject("MainMenuManager");
            MainMenuManager menuManager = managerGo.AddComponent<MainMenuManager>();

            // --- Panel 1: Main Menu Panel ---
            GameObject menuPanel = CreateUIPanel("MainMenuPanel", canvasGo.transform, bgMainMenu);
            
            // Title Banner (Centered in the upper-middle of screen)
            GameObject bannerGo = new GameObject("TitleBanner");
            bannerGo.transform.SetParent(menuPanel.transform, false);
            RectTransform bannerRt = bannerGo.AddComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRt.pivot = new Vector2(0.5f, 0.5f);
            bannerRt.anchoredPosition = new Vector2(0f, 150f);
            bannerRt.sizeDelta = new Vector2(920f, 145f);
            Image bannerImg = bannerGo.AddComponent<Image>();
            bannerImg.sprite = titleBanner;
            bannerImg.type = Image.Type.Simple;
            bannerImg.preserveAspect = true;

            // Menu Button Container (Centered in the lower-middle of screen below banner)
            GameObject btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(menuPanel.transform, false);
            RectTransform btnContainerRt = btnContainer.AddComponent<RectTransform>();
            btnContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnContainerRt.pivot = new Vector2(0.5f, 0.5f);
            btnContainerRt.anchoredPosition = new Vector2(0f, -90f);
            btnContainerRt.sizeDelta = new Vector2(400f, 260f);

            VerticalLayoutGroup vlg = btnContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            Button btnNewGame = CreateMinecraftButton("Btn_NewGame", btnContainer.transform, btnBlank, mcFont, "NEW GAME", 32, new Vector2(380f, 62f));
            Button btnSettings = CreateMinecraftButton("Btn_Settings", btnContainer.transform, btnBlank, mcFont, "SETTINGS", 32, new Vector2(380f, 62f));
            Button btnQuit = CreateMinecraftButton("Btn_Quit", btnContainer.transform, btnBlank, mcFont, "QUIT", 32, new Vector2(380f, 62f));

            UnityEventTools.AddPersistentListener(btnNewGame.onClick, menuManager.OnClickNewGame);
            UnityEventTools.AddPersistentListener(btnSettings.onClick, menuManager.OnClickSettings);
            UnityEventTools.AddPersistentListener(btnQuit.onClick, menuManager.OnClickQuit);

            // --- Panel 2: Character Select Panel ---
            GameObject charSelectPanel = CreateUIPanel("CharacterSelectPanel", canvasGo.transform, bgCharSelect);
            charSelectPanel.SetActive(false);

            // Header Title
            GameObject titleGo = CreateTextObject("SelectTitle", charSelectPanel.transform, mcFont, "CHOOSE YOUR HERO", 54, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.4f));
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -40f);
            titleRt.sizeDelta = new Vector2(800f, 60f);

            GameObject subTitleGo = CreateTextObject("SelectSubtitle", charSelectPanel.transform, mcFont, "Hover hero to preview animation  •  Click to enter battle", 32, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f));
            RectTransform subTitleRt = subTitleGo.GetComponent<RectTransform>();
            subTitleRt.anchorMin = new Vector2(0.5f, 1f);
            subTitleRt.anchorMax = new Vector2(0.5f, 1f);
            subTitleRt.pivot = new Vector2(0.5f, 1f);
            subTitleRt.anchoredPosition = new Vector2(0f, -100f);
            subTitleRt.sizeDelta = new Vector2(800f, 35f);

            // 3 Pedestals (Coordinates matching bg_character_select pedestals)
            // Left: Steve (-480, -60), Center: Alex (0, -60), Right: Villager (+480, -60)
            CharacterPedestalUI stevePed = CreatePedestal(
                "Pedestal_Steve", charSelectPanel.transform, new Vector2(-480f, -60f),
                "Steve", "STEVE", "Balanced Fighter - Standard Speed & Sturdy Defense",
                steveIdle, steveRun, steveCtrl, mcFont, btnBlank, menuManager
            );

            CharacterPedestalUI alexPed = CreatePedestal(
                "Pedestal_Alex", charSelectPanel.transform, new Vector2(0f, -60f),
                "Alex", "ALEX", "Swift Scout - High Movement Speed & Agile Reflexes",
                alexIdle, alexRun, alexCtrl, mcFont, btnBlank, menuManager
            );

            CharacterPedestalUI villagerPed = CreatePedestal(
                "Pedestal_Villager", charSelectPanel.transform, new Vector2(480f, -60f),
                "Villager", "VILLAGER", "Resource Master - Extra Experience & Lucky Item Pickups",
                villagerIdle, villagerRun, villagerCtrl, mcFont, btnBlank, menuManager
            );

            // Character Description Box at bottom
            GameObject infoBox = new GameObject("HeroInfoBox");
            infoBox.transform.SetParent(charSelectPanel.transform, false);
            RectTransform infoBoxRt = infoBox.AddComponent<RectTransform>();
            infoBoxRt.anchorMin = new Vector2(0.5f, 0f);
            infoBoxRt.anchorMax = new Vector2(0.5f, 0f);
            infoBoxRt.pivot = new Vector2(0.5f, 0f);
            infoBoxRt.anchoredPosition = new Vector2(0f, 50f);
            infoBoxRt.sizeDelta = new Vector2(850f, 95f);

            GameObject charNameObj = CreateTextObject("CharNameText", infoBox.transform, mcFont, "STEVE", 34, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.3f));
            RectTransform charNameRt = charNameObj.GetComponent<RectTransform>();
            charNameRt.anchorMin = new Vector2(0.5f, 1f);
            charNameRt.anchorMax = new Vector2(0.5f, 1f);
            charNameRt.pivot = new Vector2(0.5f, 1f);
            charNameRt.anchoredPosition = new Vector2(0f, 0f);
            charNameRt.sizeDelta = new Vector2(800f, 35f);

            GameObject charDescObj = CreateTextObject("CharDescText", infoBox.transform, mcFont, "Balanced Fighter - Standard Speed & Sturdy Defense", 25, TextAnchor.MiddleCenter, Color.white);
            RectTransform charDescRt = charDescObj.GetComponent<RectTransform>();
            charDescRt.anchorMin = new Vector2(0.5f, 0f);
            charDescRt.anchorMax = new Vector2(0.5f, 0f);
            charDescRt.pivot = new Vector2(0.5f, 0f);
            charDescRt.anchoredPosition = new Vector2(0f, 10f);
            charDescRt.sizeDelta = new Vector2(800f, 45f);

            // Back Button
            Button btnBackToMenu = CreateMinecraftButton("Btn_BackToMenu", charSelectPanel.transform, btnBlank, mcFont, "BACK", 28, new Vector2(170f, 52f));
            RectTransform btnBackRt = btnBackToMenu.GetComponent<RectTransform>();
            btnBackRt.anchorMin = new Vector2(0f, 0f);
            btnBackRt.anchorMax = new Vector2(0f, 0f);
            btnBackRt.pivot = new Vector2(0f, 0f);
            btnBackRt.anchoredPosition = new Vector2(60f, 45f);
            UnityEventTools.AddPersistentListener(btnBackToMenu.onClick, menuManager.OnClickBackToMainMenu);

            // --- Panel 3: Settings Panel ---
            GameObject settingsPanel = CreateUIPanel("SettingsPanel", canvasGo.transform, null);
            Image settingsOverlay = settingsPanel.GetComponent<Image>();
            settingsOverlay.color = new Color(0f, 0f, 0f, 0.75f);
            settingsPanel.SetActive(false);

            GameObject settingsBox = new GameObject("SettingsBox");
            settingsBox.transform.SetParent(settingsPanel.transform, false);
            RectTransform sBoxRt = settingsBox.AddComponent<RectTransform>();
            sBoxRt.anchorMin = new Vector2(0.5f, 0.5f);
            sBoxRt.anchorMax = new Vector2(0.5f, 0.5f);
            sBoxRt.pivot = new Vector2(0.5f, 0.5f);
            sBoxRt.sizeDelta = new Vector2(560f, 460f);
            Image sBoxImg = settingsBox.AddComponent<Image>();
            sBoxImg.sprite = btnBlank;
            sBoxImg.type = Image.Type.Filled;
            sBoxImg.fillAmount = 1f;
            sBoxImg.color = new Color(0.2f, 0.2f, 0.25f, 0.98f);

            GameObject sTitle = CreateTextObject("SettingsTitle", settingsBox.transform, mcFont, "SETTINGS", 40, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f));
            RectTransform sTitleRt = sTitle.GetComponent<RectTransform>();
            sTitleRt.anchorMin = new Vector2(0.5f, 1f);
            sTitleRt.anchorMax = new Vector2(0.5f, 1f);
            sTitleRt.pivot = new Vector2(0.5f, 1f);
            sTitleRt.anchoredPosition = new Vector2(0f, -25f);
            sTitleRt.sizeDelta = new Vector2(500f, 45f);

            string[] settingLabels = new string[] {
                "MASTER VOLUME: 100%",
                "MUSIC VOLUME: 80%",
                "SOUND FX: 100%",
                "DISPLAY: FULLSCREEN"
            };

            for (int i = 0; i < settingLabels.Length; i++)
            {
                Button sItem = CreateMinecraftButton("Setting_" + i, settingsBox.transform, btnBlank, mcFont, settingLabels[i], 24, new Vector2(440f, 46f));
                RectTransform sItemRt = sItem.GetComponent<RectTransform>();
                sItemRt.anchorMin = new Vector2(0.5f, 1f);
                sItemRt.anchorMax = new Vector2(0.5f, 1f);
                sItemRt.pivot = new Vector2(0.5f, 1f);
                sItemRt.anchoredPosition = new Vector2(0f, -95f - (i * 58f));
            }

            Button btnCloseSettings = CreateMinecraftButton("Btn_CloseSettings", settingsBox.transform, btnBlank, mcFont, "BACK", 28, new Vector2(240f, 50f));
            RectTransform bcsRt = btnCloseSettings.GetComponent<RectTransform>();
            bcsRt.anchorMin = new Vector2(0.5f, 0f);
            bcsRt.anchorMax = new Vector2(0.5f, 0f);
            bcsRt.pivot = new Vector2(0.5f, 0f);
            bcsRt.anchoredPosition = new Vector2(0f, 25f);
            UnityEventTools.AddPersistentListener(btnCloseSettings.onClick, menuManager.OnClickCloseSettings);

            // --- Panel 4: In-Game HUD Panel ---
            GameObject hudPanel = new GameObject("GameHUDPanel");
            hudPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform hudRt = hudPanel.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.offsetMin = Vector2.zero;
            hudRt.offsetMax = Vector2.zero;
            hudPanel.SetActive(false);

            // Top-left HUD info
            GameObject hudInfo = CreateTextObject("HUD_Controls", hudPanel.transform, mcFont, "CONTROLS: [W][A][S][D] MOVE  |  [M] SWITCH REALM  |  [ESC] MENU", 24, TextAnchor.MiddleLeft, Color.white);
            RectTransform hudInfoRt = hudInfo.GetComponent<RectTransform>();
            hudInfoRt.anchorMin = new Vector2(0f, 1f);
            hudInfoRt.anchorMax = new Vector2(0f, 1f);
            hudInfoRt.pivot = new Vector2(0f, 1f);
            hudInfoRt.anchoredPosition = new Vector2(25f, -25f);
            hudInfoRt.sizeDelta = new Vector2(800f, 35f);

            GameObject hudMapInfo = CreateTextObject("HUD_Map", hudPanel.transform, mcFont, "REALM: <color=#55FF55>OVERWORLD</color>  [PRESS M TO SWITCH]", 24, TextAnchor.MiddleRight, new Color(0.6f, 1f, 0.6f));
            RectTransform hudMapRt = hudMapInfo.GetComponent<RectTransform>();
            hudMapRt.anchorMin = new Vector2(1f, 1f);
            hudMapRt.anchorMax = new Vector2(1f, 1f);
            hudMapRt.pivot = new Vector2(1f, 1f);
            hudMapRt.anchoredPosition = new Vector2(-25f, -25f);
            hudMapRt.sizeDelta = new Vector2(600f, 35f);

            SerializedObject mapGenSo2 = new SerializedObject(mapGen);
            mapGenSo2.FindProperty("hudMapText").objectReferenceValue = hudMapInfo.GetComponent<Text>();
            mapGenSo2.ApplyModifiedProperties();

            // 8. Wire up MainMenuManager Serialized Fields
            SerializedObject mmSo = new SerializedObject(menuManager);
            mmSo.FindProperty("mainMenuPanel").objectReferenceValue = menuPanel;
            mmSo.FindProperty("characterSelectPanel").objectReferenceValue = charSelectPanel;
            mmSo.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            mmSo.FindProperty("gameHudPanel").objectReferenceValue = hudPanel;

            mmSo.FindProperty("characterNameText").objectReferenceValue = charNameObj.GetComponent<Text>();
            mmSo.FindProperty("characterDescText").objectReferenceValue = charDescObj.GetComponent<Text>();

            mmSo.FindProperty("player").objectReferenceValue = playerCtrl;
            mmSo.FindProperty("cameraFollow").objectReferenceValue = camFollow;
            mmSo.FindProperty("mapGenerator").objectReferenceValue = mapGen;

            mmSo.FindProperty("stevePedestal").objectReferenceValue = stevePed;
            mmSo.FindProperty("alexPedestal").objectReferenceValue = alexPed;
            mmSo.FindProperty("villagerPedestal").objectReferenceValue = villagerPed;

            AssignSpriteArray(mmSo.FindProperty("steveIdleFrames"), steveIdle);
            AssignSpriteArray(mmSo.FindProperty("steveRunFrames"), steveRun);
            mmSo.FindProperty("steveController").objectReferenceValue = steveCtrl;

            AssignSpriteArray(mmSo.FindProperty("alexIdleFrames"), alexIdle);
            AssignSpriteArray(mmSo.FindProperty("alexRunFrames"), alexRun);
            mmSo.FindProperty("alexController").objectReferenceValue = alexCtrl;

            AssignSpriteArray(mmSo.FindProperty("villagerIdleFrames"), villagerIdle);
            AssignSpriteArray(mmSo.FindProperty("villagerRunFrames"), villagerRun);
            mmSo.FindProperty("villagerController").objectReferenceValue = villagerCtrl;

            mmSo.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainGame.unity");
            Debug.Log("[PixelSurvivor] MainGame scene built and saved successfully!");
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Sprite bgSprite)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = panel.AddComponent<Image>();
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
            }
            return panel;
        }

        private static CharacterPedestalUI CreatePedestal(
            string name, Transform parent, Vector2 pos,
            string id, string displayName, string desc,
            Sprite[] idleFrames, Sprite[] runFrames, RuntimeAnimatorController ctrl,
            Font font, Sprite btnBlank, MainMenuManager manager)
        {
            GameObject pedGo = new GameObject(name);
            pedGo.transform.SetParent(parent, false);

            RectTransform pedRt = pedGo.AddComponent<RectTransform>();
            pedRt.anchorMin = new Vector2(0.5f, 0.5f);
            pedRt.anchorMax = new Vector2(0.5f, 0.5f);
            pedRt.pivot = new Vector2(0.5f, 0.5f);
            pedRt.anchoredPosition = pos;
            pedRt.sizeDelta = new Vector2(280f, 360f);

            // Transparent hit raycaster image covering entire pedestal area
            Image hitImg = pedGo.AddComponent<Image>();
            hitImg.color = new Color(0f, 0f, 0f, 0.005f); // Invisible raycast target

            // Scalable Character Container (scales up smoothly on hover)
            GameObject charContainer = new GameObject("CharacterVisual");
            charContainer.transform.SetParent(pedGo.transform, false);
            RectTransform charRt = charContainer.AddComponent<RectTransform>();
            charRt.anchorMin = new Vector2(0.5f, 0.5f);
            charRt.anchorMax = new Vector2(0.5f, 0.5f);
            charRt.pivot = new Vector2(0.5f, 0.5f);
            charRt.anchoredPosition = new Vector2(0f, 30f);
            charRt.sizeDelta = new Vector2(160f, 210f);

            Image charImg = charContainer.AddComponent<Image>();
            charImg.sprite = idleFrames != null && idleFrames.Length > 0 ? idleFrames[0] : null;
            charImg.preserveAspect = true;
            charImg.raycastTarget = false;

            SimpleSpriteAnimator spriteAnim = charContainer.AddComponent<SimpleSpriteAnimator>();
            spriteAnim.SetFrames(idleFrames, runFrames, 10f);

            // Name Banner
            GameObject nameBanner = new GameObject("NameBanner");
            nameBanner.transform.SetParent(pedGo.transform, false);
            RectTransform nbRt = nameBanner.AddComponent<RectTransform>();
            nbRt.anchorMin = new Vector2(0.5f, 0f);
            nbRt.anchorMax = new Vector2(0.5f, 0f);
            nbRt.pivot = new Vector2(0.5f, 0f);
            nbRt.anchoredPosition = new Vector2(0f, 10f);
            nbRt.sizeDelta = new Vector2(210f, 45f);

            Image nbImg = nameBanner.AddComponent<Image>();
            nbImg.sprite = btnBlank;
            nbImg.type = Image.Type.Filled;
            nbImg.fillAmount = 1f;
            nbImg.raycastTarget = false;

            GameObject nbTextObj = CreateTextObject("Text", nameBanner.transform, font, displayName, 28, TextAnchor.MiddleCenter, Color.white);
            RectTransform nbtRt = nbTextObj.GetComponent<RectTransform>();
            nbtRt.anchorMin = Vector2.zero;
            nbtRt.anchorMax = Vector2.one;
            nbtRt.offsetMin = Vector2.zero;
            nbtRt.offsetMax = Vector2.zero;

            CharacterPedestalUI pedUI = pedGo.AddComponent<CharacterPedestalUI>();
            pedUI.characterId = id;
            pedUI.characterDisplayName = displayName;
            pedUI.characterDescription = desc;

            SerializedObject pedSo = new SerializedObject(pedUI);
            pedSo.FindProperty("characterTransform").objectReferenceValue = charRt;
            pedSo.FindProperty("spriteAnimator").objectReferenceValue = spriteAnim;
            pedSo.FindProperty("normalScale").floatValue = 1.0f;
            pedSo.FindProperty("hoverScale").floatValue = 1.18f;
            pedSo.FindProperty("scaleSpeed").floatValue = 12f;
            pedSo.ApplyModifiedProperties();

            pedUI.Initialize(manager);
            return pedUI;
        }

        private static Button CreateMinecraftButton(string name, Transform parent, Sprite btnSprite, Font font, string label, int fontSize, Vector2 size)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.sizeDelta = size;

            LayoutElement le = btnGo.AddComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            le.minWidth = size.x;
            le.minHeight = size.y;

            Image img = btnGo.AddComponent<Image>();
            img.sprite = btnSprite;
            img.type = Image.Type.Filled;
            img.fillAmount = 1f;
            img.color = Color.white;

            Button btn = btnGo.AddComponent<Button>();
            btn.transition = Selectable.Transition.None; // MinecraftButtonFeedback handles smooth animations
            btnGo.AddComponent<MinecraftButtonFeedback>();

            GameObject textGo = CreateTextObject("Text", btnGo.transform, font, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return btn;
        }

        private static GameObject CreateTextObject(string name, Transform parent, Font font, string content, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textGo = new GameObject(name);
            textGo.transform.SetParent(parent, false);

            Text t = textGo.AddComponent<Text>();
            t.font = font;
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
            t.raycastTarget = false;

            Shadow shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);

            return textGo;
        }

        private static Sprite[] LoadSpritesSorted(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var list = new List<Sprite>();
            foreach (var a in assets)
            {
                if (a is Sprite s) list.Add(s);
            }
            return list.OrderBy(s => s.name).ToArray();
        }

        private static void AssignSpriteArray(SerializedProperty prop, Sprite[] sprites)
        {
            if (sprites == null)
            {
                prop.arraySize = 0;
                return;
            }
            prop.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }
        private static Sprite[] SelectSpriteSequence(Sprite[] all, int[] indices)
        {
            if (all == null || all.Length == 0 || indices == null || indices.Length == 0) return all;
            var list = new List<Sprite>();
            foreach (int idx in indices)
            {
                if (idx >= 0 && idx < all.Length)
                {
                    list.Add(all[idx]);
                }
            }
            return list.ToArray();
        }

        private static void UpdateAnimationClip(string assetPath, Sprite[] sprites, float frameRate)
        {
            if (sprites == null || sprites.Length == 0) return;
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, assetPath);
            }

            clip.frameRate = frameRate;
            EditorCurveBinding binding = new EditorCurveBinding();
            binding.type = typeof(SpriteRenderer);
            binding.path = "";
            binding.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe();
                keyframes[i].time = (float)i / frameRate;
                keyframes[i].value = sprites[i];
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.stopTime = (float)sprites.Length / frameRate;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }
    }
}
