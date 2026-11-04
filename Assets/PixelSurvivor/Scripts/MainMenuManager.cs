using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PixelSurvivor
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject gameHudPanel;

        [Header("Character Information Display")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text characterDescText;

        [Header("Starting Weapon Prefabs")]
        [SerializeField] private GameObject steveStartingWeaponPrefab;
        [SerializeField] private GameObject alexStartingWeaponPrefab;
        [SerializeField] private GameObject villagerStartingWeaponPrefab;

        [Header("Gameplay Systems")]
        [SerializeField] private PlayerController player;
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private InfiniteMapGenerator mapGenerator;

        [Header("Character Pedestals")]
        [SerializeField] private CharacterPedestalUI stevePedestal;
        [SerializeField] private CharacterPedestalUI alexPedestal;
        [SerializeField] private CharacterPedestalUI villagerPedestal;

        [Header("Character Animation Resources")]
        [SerializeField] private Sprite[] steveIdleFrames;
        [SerializeField] private Sprite[] steveRunFrames;
        [SerializeField] private RuntimeAnimatorController steveController;

        [SerializeField] private Sprite[] alexIdleFrames;
        [SerializeField] private Sprite[] alexRunFrames;
        [SerializeField] private RuntimeAnimatorController alexController;

        [SerializeField] private Sprite[] villagerIdleFrames;
        [SerializeField] private Sprite[] villagerRunFrames;
        [SerializeField] private RuntimeAnimatorController villagerController;

        [Header("Difficulty Modal")]
        [SerializeField] private UI.DifficultySelectionUI difficultyModal;

        private string selectedCharacterId = "Steve";
        private CharacterPedestalUI currentSelectedPedestal;

        private void Awake()
        {
            // Defensive Canvas scale check
            Canvas mainCanvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            if (mainCanvas != null && mainCanvas.transform.localScale != Vector3.one)
            {
                mainCanvas.transform.localScale = Vector3.one;
            }

            // Configure mobile-friendly CanvasScaler: 1600x900 Match Height (+20% scale boost on mobile)
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

            // Ensure full-screen panels have safe overscan to eliminate sub-pixel rasterization seams at screen edges
            if (mainMenuPanel != null)
            {
                RectTransform rt = mainMenuPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(20f, 20f);
                }
            }
            if (characterSelectPanel != null)
            {
                RectTransform rt = characterSelectPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(20f, 20f);
                }
            }

            // Auto-wire text displays if not wired in Inspector
            if (characterNameText == null && characterSelectPanel != null)
            {
                characterNameText = characterSelectPanel.transform.Find("HeroInfoBox/CharNameText")?.GetComponent<TMP_Text>();
            }
            if (characterDescText == null && characterSelectPanel != null)
            {
                characterDescText = characterSelectPanel.transform.Find("HeroInfoBox/CharDescText")?.GetComponent<TMP_Text>();
            }

            // Auto-locate pedestals if null
            if (stevePedestal == null && characterSelectPanel != null)
            {
                stevePedestal = characterSelectPanel.transform.Find("Pedestal_Steve")?.GetComponent<CharacterPedestalUI>();
            }
            if (alexPedestal == null && characterSelectPanel != null)
            {
                alexPedestal = characterSelectPanel.transform.Find("Pedestal_Alex")?.GetComponent<CharacterPedestalUI>();
            }
            if (villagerPedestal == null && characterSelectPanel != null)
            {
                villagerPedestal = characterSelectPanel.transform.Find("Pedestal_Villager")?.GetComponent<CharacterPedestalUI>();
            }

            // Auto-locate difficultyModal if not wired
            if (difficultyModal == null)
            {
                Transform diffPanel = transform.parent != null ? transform.parent.Find("DifficultySelectPanel") : null;
                if (diffPanel == null && mainCanvas != null)
                {
                    diffPanel = mainCanvas.transform.Find("DifficultySelectPanel");
                }
                if (diffPanel != null)
                {
                    difficultyModal = diffPanel.GetComponent<UI.DifficultySelectionUI>();
                }
                if (difficultyModal == null)
                {
                    difficultyModal = FindAnyObjectByType<UI.DifficultySelectionUI>(FindObjectsInactive.Include);
                }
            }

            // Initialize pedestals
            if (stevePedestal != null) stevePedestal.Initialize(this);
            if (alexPedestal != null) alexPedestal.Initialize(this);
            if (villagerPedestal != null) villagerPedestal.Initialize(this);

            // Auto-locate gameHudPanel if not assigned
            if (gameHudPanel == null)
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
                if (inGameCanvas != null)
                {
                    if (!inGameCanvas.activeSelf) inGameCanvas.SetActive(true);
                    if (inGameCanvas.transform.localScale != Vector3.one) inGameCanvas.transform.localScale = Vector3.one;

                    Transform hr = inGameCanvas.transform.Find("HUD_Root");
                    gameHudPanel = hr != null ? hr.gameObject : inGameCanvas;
                }
            }

            // Auto-wire main menu buttons dynamically
            if (mainMenuPanel != null)
            {
                Button btnNew = mainMenuPanel.transform.Find("ButtonContainer/Btn_NewGame")?.GetComponent<Button>();
                if (btnNew != null)
                {
                    btnNew.onClick.RemoveListener(OnClickNewGame);
                    btnNew.onClick.AddListener(OnClickNewGame);
                }

                Button btnSet = mainMenuPanel.transform.Find("ButtonContainer/Btn_Settings")?.GetComponent<Button>();
                if (btnSet != null)
                {
                    btnSet.onClick.RemoveListener(OnClickSettings);
                    btnSet.onClick.AddListener(OnClickSettings);
                }

                Button btnQuit = mainMenuPanel.transform.Find("ButtonContainer/Btn_Quit")?.GetComponent<Button>();
                if (btnQuit != null)
                {
                    btnQuit.onClick.RemoveListener(OnClickQuit);
                    btnQuit.onClick.AddListener(OnClickQuit);
                }
            }

            // Audio: Start Menu BGM
            Audio.AudioManager.Instance?.PlayMenuMusic();
        }

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            ShowMainMenu();

            if (player != null)
            {
                player.SetMovementLocked(true);
                player.gameObject.SetActive(false);
            }
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (gameHudPanel != null) gameHudPanel.SetActive(false);
            if (difficultyModal != null) difficultyModal.Hide();

            GameObject diffPanel = GameObject.Find("DifficultySelectPanel");
            if (diffPanel != null) diffPanel.SetActive(false);

            if (player != null)
            {
                player.SetMovementLocked(true);
                player.gameObject.SetActive(false);
            }

            // Ensure WaveSpawner is stopped and reset to 00:00 while in Main Menu
            if (Combat.WaveSpawner.Instance != null)
            {
                Combat.WaveSpawner.Instance.StopRun();
            }
            Combat.BossArenaManager.Instance?.ResetArenaState();
            Combat.BossArenaManager.Instance?.ResetArenaState();
        }

        public void OnClickNewGame()
        {
            Audio.AudioManager.Instance?.PlayUiClick();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (difficultyModal != null) difficultyModal.Hide();

            // Default focus on Steve with active walk animation and description
            if (stevePedestal != null)
            {
                SelectPedestal(stevePedestal);
            }
        }

        public void OnClickSettings()
        {
            Audio.AudioManager.Instance?.PlayUiClick();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void OnClickCloseSettings()
        {
            Audio.AudioManager.Instance?.PlayUiClick();
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void OnClickQuit()
        {
            Audio.AudioManager.Instance?.PlayUiClick();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        public void OnClickBackToMainMenu()
        {
            Audio.AudioManager.Instance?.PlayUiClick();
            ShowMainMenu();
        }

        /// <summary>
        /// Handles 2-click hero selection:
        /// 1st click: selects pedestal, runs walk animation, zooms up, lights up ring, updates info text.
        /// 2nd click on the same selected hero: confirms and transitions to Difficulty Selection.
        /// </summary>
        public void OnPedestalClicked(CharacterPedestalUI pedestal)
        {
            if (pedestal == null) return;

            if (pedestal == currentSelectedPedestal && pedestal.IsSelected)
            {
                // Second click on the same selected hero: CONFIRM!
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlayUiClick();
                }
                OnCharacterSelected(pedestal.characterId);
            }
            else
            {
                // First click (or switch hero): FOCUS, WALK ANIMATION & DESCRIPTION UPDATE!
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlayUiClick();
                }
                SelectPedestal(pedestal);
            }
        }

        public void SelectPedestal(CharacterPedestalUI pedestal)
        {
            currentSelectedPedestal = pedestal;

            if (stevePedestal != null) stevePedestal.SetSelected(pedestal == stevePedestal);
            if (alexPedestal != null) alexPedestal.SetSelected(pedestal == alexPedestal);
            if (villagerPedestal != null) villagerPedestal.SetSelected(pedestal == villagerPedestal);

            if (pedestal != null)
            {
                selectedCharacterId = pedestal.characterId;
                if (characterNameText != null) characterNameText.text = pedestal.characterDisplayName;
                if (characterDescText != null) characterDescText.text = pedestal.characterDescription;
            }
        }

        public void OnCharacterHovered(CharacterPedestalUI pedestal)
        {
            if (pedestal == null) return;
            if (characterNameText != null) characterNameText.text = pedestal.characterDisplayName;
            if (characterDescText != null) characterDescText.text = pedestal.characterDescription;
        }

        public void OnCharacterUnhovered(CharacterPedestalUI pedestal)
        {
            // Restore current selected hero's info so it never gets frozen or empty
            if (currentSelectedPedestal != null)
            {
                if (characterNameText != null) characterNameText.text = currentSelectedPedestal.characterDisplayName;
                if (characterDescText != null) characterDescText.text = currentSelectedPedestal.characterDescription;
            }
        }

        public void OnCharacterSelected(string characterId)
        {
            selectedCharacterId = characterId;

            // Hide Character Selection so Difficulty screen is Fullscreen
            if (characterSelectPanel != null) characterSelectPanel.SetActive(false);

            if (difficultyModal == null) difficultyModal = FindAnyObjectByType<UI.DifficultySelectionUI>(FindObjectsInactive.Include);
            if (difficultyModal == null)
            {
                difficultyModal = gameObject.AddComponent<UI.DifficultySelectionUI>();
            }

            difficultyModal.Show((chosenDiff) => {
                StartGameWithCharacterAndDifficulty(characterId, chosenDiff);
            });
        }

        public void StartGameWithCharacterAndDifficulty(string characterId, Combat.GameDifficulty diff)
        {
            if (Combat.WaveSpawner.Instance != null)
            {
                Combat.WaveSpawner.Instance.SetDifficulty(diff);
                Combat.WaveSpawner.Instance.StartRun();
            }
            StartGameWithCharacter(characterId);
        }

        public void StartGameWithCharacter(string characterId)
        {
            // Close UI panels
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (difficultyModal != null) difficultyModal.Hide();

            GameObject diffPanel = GameObject.Find("DifficultySelectPanel");
            if (diffPanel != null) diffPanel.SetActive(false);

            if (gameHudPanel != null)
            {
                if (gameHudPanel.transform.parent != null && !gameHudPanel.transform.parent.gameObject.activeSelf)
                {
                    gameHudPanel.transform.parent.gameObject.SetActive(true);
                    gameHudPanel.transform.parent.localScale = Vector3.one;
                }
                gameHudPanel.SetActive(true);
            }

            // Start wave spawner run cleanly if not already running
            if (Combat.WaveSpawner.Instance != null && !Combat.WaveSpawner.Instance.IsRunActive)
            {
                Combat.WaveSpawner.Instance.StartRun();
            }

            // Audio: Transition to Battle BGM
            Audio.AudioManager.Instance?.PlayBattleMusic();

            // Configure Player
            if (player != null)
            {
                player.gameObject.SetActive(true);
                player.transform.position = Vector3.zero;
                player.SetMovementLocked(false);

                // Configure starting weapon per character (Phương án A)
                var wc = player.GetComponent<Combat.WeaponController>();
                if (wc != null)
                {
                    if (characterId == "Steve" && steveStartingWeaponPrefab != null)
                    {
                        wc.SetStartingWeapon(steveStartingWeaponPrefab);
                    }
                    else if (characterId == "Alex" && alexStartingWeaponPrefab != null)
                    {
                        wc.SetStartingWeapon(alexStartingWeaponPrefab);
                    }
                    else if (characterId == "Villager" && villagerStartingWeaponPrefab != null)
                    {
                        wc.SetStartingWeapon(villagerStartingWeaponPrefab);
                    }
                }

                if (characterId == "Steve")
                {
                    player.ConfigureCharacter("Steve", steveController, steveIdleFrames, steveRunFrames, 4.0f);
                }
                else if (characterId == "Alex")
                {
                    player.ConfigureCharacter("Alex", alexController, alexIdleFrames, alexRunFrames, 4.8f);
                }
                else if (characterId == "Villager")
                {
                    player.ConfigureCharacter("Villager", villagerController, villagerIdleFrames, villagerRunFrames, 4.2f);
                }
            }

            // Track with camera
            if (cameraFollow != null && player != null)
            {
                cameraFollow.SetTarget(player.transform);
            }

            // Start infinite map generator
            if (mapGenerator != null && player != null)
            {
                mapGenerator.SetPlayer(player.transform);
            }
        }

        private void Update()
        {
            // Return from submenus with ESC key
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (characterSelectPanel != null && characterSelectPanel.activeSelf)
                {
                    ShowMainMenu();
                }
                else if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    OnClickCloseSettings();
                }
                // When in-game (gameHudPanel active), InGamePauseManager handles ESC pause
            }
        }
    }
}
