using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.Combat;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Manages the Level Up Modal, including:
    /// - Freezing game simulation (Time.timeScale = 0).
    /// - Offering 3 randomized upgrade cards (new weapons, weapon upgrades, stat boosts).
    /// - Enforcing 2-Click Selection:
    ///     * Click 1: Highlight / Select preview (Gold border, scale pop).
    ///     * Click 2: Confirm reward & resume game.
    /// - Hotkey support (1, 2, 3 for cards, R for reroll, Enter/Space to confirm selected).
    /// </summary>
    public class LevelUpManager : MonoBehaviour
    {
        [Header("UI Canvas Hierarchy")]
        [SerializeField] private GameObject levelUpModalPanel;
        [SerializeField] private TMP_Text levelTitleText;
        [SerializeField] private LevelUpCardUI[] cardUIs = new LevelUpCardUI[3];
        [SerializeField] private Button rerollButton;
        [SerializeField] private TMP_Text rerollButtonText;

        [Header("Settings & State")]
        [SerializeField] private int currentLevel = 1;

        [Header("In-Run Emerald Reroll Economy")]
        [SerializeField] private int baseRerollCost = 10;
        [SerializeField] private int rerollCostIncrease = 5;
        private int rerollsUsedThisRun = 0;

        public int CurrentRerollCost
        {
            get
            {
                if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
                int cost = baseRerollCost + (rerollsUsedThisRun * rerollCostIncrease);
                // Villager has 50% discount on reroll
                if (playerController != null && playerController.characterId == "Villager")
                {
                    return Mathf.Max(2, cost / 2);
                }
                return cost;
            }
        }

        private int selectedIndex = -1; // -1 = none selected
        private WeaponController weaponController;
        private PlayerController playerController;
        private PlayerHealth playerHealth;

        public bool IsOpen => levelUpModalPanel != null && levelUpModalPanel.activeSelf;
        public int CurrentLevel => currentLevel;

        private void Awake()
        {
            if (levelUpModalPanel != null)
            {
                levelUpModalPanel.SetActive(false);
            }
        }

        private void Start()
        {
            weaponController = FindAnyObjectByType<WeaponController>();
            playerController = FindAnyObjectByType<PlayerController>();
            playerHealth = FindAnyObjectByType<PlayerHealth>();

            if (rerollButton != null)
            {
                rerollButton.onClick.RemoveAllListeners();
                rerollButton.onClick.AddListener(OnRerollClicked);
            }

            UpdateRerollButtonText();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                // Cheat / Test shortcut in Editor: Press 'L' to trigger Level Up
#if UNITY_EDITOR
                if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
                {
                    TriggerLevelUp(currentLevel + 1);
                }
#endif
                return;
            }

            // Keyboard navigation while Level Up is open
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
                {
                    OnCardClicked(0);
                }
                else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
                {
                    OnCardClicked(1);
                }
                else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
                {
                    OnCardClicked(2);
                }
                else if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    OnRerollClicked();
                }
                else if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    if (selectedIndex >= 0 && selectedIndex < cardUIs.Length)
                    {
                        ConfirmSelection(selectedIndex);
                    }
                }
            }
        }

        [ContextMenu("Test Trigger Level Up")]
        public void TestTriggerLevelUp()
        {
            TriggerLevelUp(currentLevel + 1);
        }

        public void TriggerLevelUp(int newLevel)
        {
            currentLevel = newLevel;
            selectedIndex = -1;

            if (levelTitleText != null)
            {
                levelTitleText.text = $"Level {currentLevel}!";
            }

            PopulateUpgradeCards();
            UpdateRerollButtonText();

            if (levelUpModalPanel != null)
            {
                levelUpModalPanel.SetActive(true);
            }

            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log($"<color=#FACC15>[LEVEL UP]</color> Reached Level {currentLevel}! Choose a reward.");
        }

        public void OnCardClicked(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= cardUIs.Length) return;

            if (selectedIndex == cardIndex)
            {
                // Second click on the already selected card -> CONFIRM!
                ConfirmSelection(cardIndex);
            }
            else
            {
                // First click -> Select and preview highlight
                SelectCard(cardIndex);
            }
        }

        private void SelectCard(int cardIndex)
        {
            selectedIndex = cardIndex;

            for (int i = 0; i < cardUIs.Length; i++)
            {
                if (cardUIs[i] != null)
                {
                    cardUIs[i].SetSelected(i == selectedIndex);
                }
            }

            Debug.Log($"<color=#38BDF8>[Card Selected]</color> Card #{cardIndex + 1} highlighted. Click again to confirm!");
        }

        private void ConfirmSelection(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= cardUIs.Length) return;

            Debug.Log($"<color=#10B981>[Upgrade Confirmed]</color> Executing reward #{cardIndex + 1}!");

            if (cardUIs[cardIndex] != null)
            {
                cardUIs[cardIndex].ExecuteUpgrade();
            }

            // Close modal and resume game
            selectedIndex = -1;
            if (levelUpModalPanel != null)
            {
                levelUpModalPanel.SetActive(false);
            }

            Time.timeScale = 1f;
        }

        private void OnRerollClicked()
        {
            int cost = CurrentRerollCost;
            if (ExperienceManager.Instance == null || ExperienceManager.Instance.CurrentEmeralds < cost)
            {
                Debug.LogWarning($"[LevelUpManager] Not enough emeralds! Need {cost} 💎");
                return;
            }

            ExperienceManager.Instance.SpendEmeralds(cost);
            rerollsUsedThisRun++;
            selectedIndex = -1;
            PopulateUpgradeCards();
            UpdateRerollButtonText();
            Debug.Log($"<color=#10B981>[Reroll]</color> Refreshed cards for {cost} 💎! (Remaining: {ExperienceManager.Instance.CurrentEmeralds} 💎)");
        }

        private void UpdateRerollButtonText()
        {
            int cost = CurrentRerollCost;
            int curEmeralds = ExperienceManager.Instance != null ? ExperienceManager.Instance.CurrentEmeralds : 0;
            bool canAfford = curEmeralds >= cost;

            if (rerollButtonText != null)
            {
                rerollButtonText.text = $"Reroll ({cost} 💎) · R";
            }
            if (rerollButton != null)
            {
                rerollButton.interactable = canAfford;
            }
        }

        private void PopulateUpgradeCards()
        {
            if (weaponController == null) weaponController = FindAnyObjectByType<WeaponController>();
            if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
            if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();

            // Generate 3 contextual options matching Pixel Survivor
            List<UpgradeOptionData> pool = BuildOptionPool();

            // Shuffle / Pick 3 distinct options
            for (int i = 0; i < pool.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, pool.Count);
                var temp = pool[i];
                pool[i] = pool[r];
                pool[r] = temp;
            }

            for (int i = 0; i < cardUIs.Length; i++)
            {
                if (cardUIs[i] == null) continue;

                int idx = i;
                UpgradeOptionData opt = i < pool.Count ? pool[i] : GetFallbackOption();

                cardUIs[i].BindCard(
                    idx,
                    opt.title,
                    opt.description,
                    opt.synergy,
                    opt.badge1,
                    opt.badge2,
                    opt.badge2Color,
                    opt.icon,
                    () => OnCardClicked(idx),
                    opt.action
                );
            }
        }

        private List<UpgradeOptionData> BuildOptionPool()
        {
            List<UpgradeOptionData> pool = new List<UpgradeOptionData>();

            // 1. Weapon Upgrade (Iron Sword)
            pool.Add(new UpgradeOptionData
            {
                title = "Iron Sword",
                description = "+25% Slash Damage, -10% Cooldown.",
                synergy = "Primary Melee: Slashes all nearby enemies.",
                badge1 = "Upgrade",
                badge2 = "Lv.1 > 2",
                badge2Color = new Color(0.533f, 0.329f, 0.816f, 1f), // Purple
                action = () =>
                {
                    if (weaponController != null) weaponController.ApplyBuff("damage", 0.25f);
                }
            });

            // 2. New Weapon / Passive: Quick Dash / Wind Cloak
            pool.Add(new UpgradeOptionData
            {
                title = "Swift Boots",
                description = "Increases movement speed by +15%.",
                synergy = "Mobility: Dodge fast-moving enemy hordes.",
                badge1 = "New · Normal",
                badge2 = "Passive",
                badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f), // Emerald Green
                action = () =>
                {
                    // buff speed
                    Debug.Log("[Upgrade] Move speed boosted!");
                }
            });

            // 3. Heart of Obsidian (Vitality)
            pool.Add(new UpgradeOptionData
            {
                title = "Heart of Obsidian",
                description = "+25 Max Health and instantly restore 30 HP.",
                synergy = "Survivability: Survive longer against boss waves.",
                badge1 = "Stat Buff",
                badge2 = "Health",
                badge2Color = new Color(0.937f, 0.267f, 0.267f, 1f), // Crimson Red
                action = () =>
                {
                    if (playerHealth != null)
                    {
                        playerHealth.Heal(30f);
                    }
                }
            });

            // 4. Emerald Magnet (Area / Vacuum)
            pool.Add(new UpgradeOptionData
            {
                title = "Emerald Magnet",
                description = "+35% EXP & Gem pickup vacuum radius.",
                synergy = "Utility: Collect dropped gems without taking risks.",
                badge1 = "Passive",
                badge2 = "Utility",
                badge2Color = new Color(0.220f, 0.741f, 0.973f, 1f), // Cyan
                action = () =>
                {
                    if (weaponController != null) weaponController.ApplyBuff("area", 0.35f);
                }
            });

            // 5. Sharpened Blade (Critical / Damage)
            pool.Add(new UpgradeOptionData
            {
                title = "Sharpened Edge",
                description = "+20% Attack Damage to all equipped weapons.",
                synergy = "Offensive: Cut through tough armored zombies.",
                badge1 = "Stat Buff",
                badge2 = "Damage",
                badge2Color = new Color(0.969f, 0.718f, 0.192f, 1f), // Gold
                action = () =>
                {
                    if (weaponController != null) weaponController.ApplyBuff("damage", 0.20f);
                }
            });

            return pool;
        }

        private UpgradeOptionData GetFallbackOption()
        {
            return new UpgradeOptionData
            {
                title = "Vitality Surge",
                description = "Instantly restore 25 HP.",
                synergy = "",
                badge1 = "Heal",
                badge2 = "Recovery",
                badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f),
                action = () =>
                {
                    if (playerHealth != null) playerHealth.Heal(25f);
                }
            };
        }

        private struct UpgradeOptionData
        {
            public string title;
            public string description;
            public string synergy;
            public string badge1;
            public string badge2;
            public Color badge2Color;
            public Sprite icon;
            public Action action;
        }
    }
}
