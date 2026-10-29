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

        [Header("All Available Weapon Prefabs (6 Weapons)")]
        [SerializeField] private GameObject[] allWeaponPrefabs = new GameObject[0];

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
            if (weaponController == null) weaponController = FindAnyObjectByType<WeaponController>();
            if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
            if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();

            List<UpgradeOptionData> pool = new List<UpgradeOptionData>();

            // 1. Upgrades for currently equipped weapons (Lv 1 -> Lv 8)
            if (weaponController != null)
            {
                var equipped = weaponController.EquippedWeapons;
                for (int i = 0; i < equipped.Count; i++)
                {
                    var w = equipped[i];
                    if (w != null && w.currentLevel < w.maxLevel)
                    {
                        string wId = w.weaponId;
                        pool.Add(new UpgradeOptionData
                        {
                            title = w.weaponName,
                            description = w.GetNextLevelDescription(),
                            synergy = string.IsNullOrEmpty(w.WeaponSynergy) ? "Upgrade equipped weapon." : w.WeaponSynergy,
                            badge1 = "Upgrade",
                            badge2 = $"Lv.{w.currentLevel} > {w.currentLevel + 1}",
                            badge2Color = new Color(0.969f, 0.718f, 0.192f, 1f), // Gold
                            icon = w.icon,
                            action = () =>
                            {
                                if (weaponController != null) weaponController.UpgradeWeapon(wId);
                            }
                        });
                    }
                }

                // 2. New Weapons (if player has less than MaxWeapons slots)
                if (equipped.Count < Combat.WeaponController.MaxWeapons && allWeaponPrefabs != null)
                {
                    for (int i = 0; i < allWeaponPrefabs.Length; i++)
                    {
                        var prefab = allWeaponPrefabs[i];
                        if (prefab == null) continue;

                        var wb = prefab.GetComponent<Combat.WeaponBase>();
                        if (wb != null && !weaponController.HasWeapon(wb.weaponId))
                        {
                            GameObject capturedPrefab = prefab;
                            pool.Add(new UpgradeOptionData
                            {
                                title = wb.weaponName,
                                description = wb.GetBaseDescription(),
                                synergy = string.IsNullOrEmpty(wb.WeaponSynergy) ? "New Weapon: Adds an extra attack source." : wb.WeaponSynergy,
                                badge1 = "New Weapon",
                                badge2 = "Lv.1",
                                badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f), // Emerald Green
                                icon = wb.icon,
                                action = () =>
                                {
                                    if (weaponController != null) weaponController.EquipNewWeaponFromPrefab(capturedPrefab);
                                }
                            });
                        }
                    }
                }
            }

            // 3. Passive Buffs
            pool.Add(new UpgradeOptionData
            {
                title = "Swift Boots",
                description = "+15% Movement Speed bonus.",
                synergy = "Mobility: Dodge fast-moving enemy hordes.",
                badge1 = "Passive",
                badge2 = "Speed",
                badge2Color = new Color(0.220f, 0.741f, 0.973f, 1f), // Cyan
                action = () =>
                {
                    if (playerController != null) playerController.ApplySpeedBuff(0.15f);
                }
            });

            pool.Add(new UpgradeOptionData
            {
                title = "Heart of Obsidian",
                description = "+25 Max Health and instantly restore 35 HP.",
                synergy = "Survivability: Endure high-damage elite waves.",
                badge1 = "Stat Buff",
                badge2 = "Health",
                badge2Color = new Color(0.937f, 0.267f, 0.267f, 1f), // Crimson Red
                action = () =>
                {
                    if (playerHealth != null)
                    {
                        playerHealth.IncreaseMaxHealth(25f, false);
                        playerHealth.Heal(35f);
                    }
                }
            });

            pool.Add(new UpgradeOptionData
            {
                title = "Emerald Magnet",
                description = "+35% EXP & Gem pickup vacuum radius.",
                synergy = "Utility: Collect dropped gems without taking risks.",
                badge1 = "Passive",
                badge2 = "Utility",
                badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f), // Emerald
                action = () =>
                {
                    var collector = playerController != null ? playerController.GetComponent<PlayerCollector>() : null;
                    if (collector != null) collector.ApplyRadiusBonus(0.35f);
                }
            });

            pool.Add(new UpgradeOptionData
            {
                title = "Sharpened Edge",
                description = "+20% Attack Damage to all equipped weapons.",
                synergy = "Offensive: Cut through tough armored hordes.",
                badge1 = "Stat Buff",
                badge2 = "Damage",
                badge2Color = new Color(0.969f, 0.718f, 0.192f, 1f), // Gold
                action = () =>
                {
                    if (weaponController != null) weaponController.ApplyBuff("damage", 0.20f);
                }
            });

            pool.Add(new UpgradeOptionData
            {
                title = "Chronos Hourglass",
                description = "-15% Attack Cooldown on all equipped weapons.",
                synergy = "Haste: Attack faster and trigger combos frequently.",
                badge1 = "Passive",
                badge2 = "Cooldown",
                badge2Color = new Color(0.533f, 0.329f, 0.816f, 1f), // Purple
                action = () =>
                {
                    if (weaponController != null) weaponController.ApplyBuff("cooldown", 0.15f);
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
