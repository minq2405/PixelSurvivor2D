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

        private string selectedCharacterId = "Steve";

        private void Awake()
        {
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
        }

        private void Start()
        {
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

            if (player != null)
            {
                player.SetMovementLocked(true);
                player.gameObject.SetActive(false);
            }
        }

        public void OnClickNewGame()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Default display info
            if (stevePedestal != null)
            {
                OnCharacterHovered(stevePedestal);
            }
        }

        public void OnClickSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void OnClickCloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void OnClickQuit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        public void OnClickBackToMainMenu()
        {
            ShowMainMenu();
        }

        public void OnCharacterHovered(CharacterPedestalUI pedestal)
        {
            if (pedestal == null) return;
            if (characterNameText != null) characterNameText.text = pedestal.characterDisplayName;
            if (characterDescText != null) characterDescText.text = pedestal.characterDescription;
        }

        public void OnCharacterUnhovered(CharacterPedestalUI pedestal)
        {
            // Keep selected or show general instruction
            if (characterNameText != null && string.IsNullOrEmpty(characterNameText.text))
            {
                characterNameText.text = "SELECT YOUR HERO";
            }
        }

        public void OnCharacterSelected(string characterId)
        {
            selectedCharacterId = characterId;
            StartGameWithCharacter(characterId);
        }

        public void StartGameWithCharacter(string characterId)
        {
            // Close UI panels
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (gameHudPanel != null)
            {
                if (gameHudPanel.transform.parent != null && !gameHudPanel.transform.parent.gameObject.activeSelf)
                {
                    gameHudPanel.transform.parent.gameObject.SetActive(true);
                    gameHudPanel.transform.parent.localScale = Vector3.one;
                }
                gameHudPanel.SetActive(true);
            }

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
                    player.ConfigureCharacter("Villager", villagerController, villagerIdleFrames, villagerRunFrames, 3.5f);
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
