using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.Combat;

namespace PixelSurvivor.UI
{
    public enum RewardModalType
    {
        LevelUp,
        BossChest
    }

    public class PendingRewardRequest
    {
        public RewardModalType Type;
        public int Level;
        public bool GuaranteesEvolution;

        public PendingRewardRequest(RewardModalType type, int level, bool guaranteesEvolution)
        {
            Type = type;
            Level = level;
            GuaranteesEvolution = guaranteesEvolution;
        }
    }

    /// <summary>
    /// Manages the Level Up Modal (v2.1 Architecture), including:
    /// - Freezing game simulation via TimeScaleManager token.
    /// - 2-Click Selection mechanic (Preview -> Confirm).
    /// - Sequential Multi-Level & Boss Chest Queuing (FIFO queue, zero skipped choices).
    /// - 6 Weapons (Lv 1 -> 8).
    /// - 9 Passives with Lv 1 -> 5 scaling & 4 active slots (PassiveController).
    /// - Evolution Gating (>= 09:00, max 2/run) & 6 Mythic Evolved Weapons.
    /// - Evolution Pity system (from 08:00, weight x3, guarantee <= 3 rolls, Swap card).
    /// - Backup card pool (Hearty Meal, Emerald Pouch, Scholar's Tome, Bounty Mark).
    /// - Emerald Reroll economy.
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

        private readonly Queue<PendingRewardRequest> pendingRewards = new Queue<PendingRewardRequest>();
        private PendingRewardRequest currentRequest = null;

        [Header("In-Run Emerald Reroll Economy (v2.1 Ladder)")]
        private static readonly int[] RerollCosts = { 2, 3, 5, 8, 12, 18 };
        private int rerollsUsedThisRun = 0;
        private int paidRerollsThisLevel = 0;
        private bool hasUsedFreeRerollThisLevel = false;

        [Header("All Available Weapon Prefabs (6 Weapons)")]
        [SerializeField] private GameObject[] allWeaponPrefabs = new GameObject[0];

        [Header("Passive Sprites (9 Passives)")]
        [SerializeField] private Sprite iconBookDamage;
        [SerializeField] private Sprite iconFeatherSpeed;
        [SerializeField] private Sprite iconHeartLife;
        [SerializeField] private Sprite iconShieldArmor;
        [SerializeField] private Sprite iconClockCooldown;
        [SerializeField] private Sprite iconEmeraldWisdom;
        [SerializeField] private Sprite iconEnderEye;
        [SerializeField] private Sprite iconPotionRegen;
        [SerializeField] private Sprite iconTotemRevive;

        [Header("Backup Card Sprites (4 Cards)")]
        [SerializeField] private Sprite iconHeartyMeal;
        [SerializeField] private Sprite iconEmeraldPouch;
        [SerializeField] private Sprite iconScholarsTome;
        [SerializeField] private Sprite iconBountyMark;

        private int selectedIndex = -1; // -1 = none selected
        private WeaponController weaponController;
        private PlayerController playerController;
        private PlayerHealth playerHealth;
        private int pityRollCounter = 0;

        public static LevelUpManager Instance { get; private set; }

        public bool IsOpen => levelUpModalPanel != null && levelUpModalPanel.activeSelf;
        public int CurrentLevel => currentLevel;

        public bool CanRerollMore
        {
            get
            {
                if (!hasUsedFreeRerollThisLevel) return true;
                bool isVillager = playerController != null && playerController.characterId == "Villager";
                int maxPaid = isVillager ? 6 : 5;
                return paidRerollsThisLevel < maxPaid;
            }
        }

        public int CurrentRerollCost
        {
            get
            {
                if (!hasUsedFreeRerollThisLevel) return 0;
                if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

                int paidIdx = Mathf.Clamp(paidRerollsThisLevel, 0, RerollCosts.Length - 1);
                int baseCost = RerollCosts[paidIdx];

                if (playerController != null && playerController.characterId == "Villager")
                {
                    return Mathf.Max(1, Mathf.FloorToInt(baseCost * 0.5f));
                }
                return baseCost;
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            EnsureSpritesLoaded();
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
#if UNITY_EDITOR
                if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
                {
                    TriggerLevelUp(currentLevel + 1);
                }
#endif
                return;
            }

            // Keyboard navigation
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
            pendingRewards.Enqueue(new PendingRewardRequest(RewardModalType.LevelUp, newLevel, false));
            if (currentRequest == null)
            {
                ProcessNextPendingReward();
            }
            else
            {
                UpdateTitleText();
            }
            Debug.Log($"<color=#FACC15>[LEVEL UP QUEUED]</color> Queued Level {newLevel}! (Total pending: {pendingRewards.Count})");
        }

        public void OpenBossChest(bool guaranteesEvolution)
        {
            pendingRewards.Enqueue(new PendingRewardRequest(RewardModalType.BossChest, currentLevel, guaranteesEvolution));
            if (currentRequest == null)
            {
                ProcessNextPendingReward();
            }
            else
            {
                UpdateTitleText();
            }
            Debug.Log($"<color=#F59E0B>[BOSS CHEST QUEUED]</color> Queued Boss Chest (Evolve: {guaranteesEvolution})! (Total pending: {pendingRewards.Count})");
        }

        private void ProcessNextPendingReward()
        {
            if (pendingRewards.Count == 0)
            {
                currentRequest = null;
                selectedIndex = -1;
                if (levelUpModalPanel != null)
                {
                    levelUpModalPanel.SetActive(false);
                }

                TimeScaleManager.Instance?.ReleasePause("LevelUpModal");
                return;
            }

            currentRequest = pendingRewards.Dequeue();
            selectedIndex = -1;
            paidRerollsThisLevel = 0;

            if (currentRequest.Type == RewardModalType.LevelUp)
            {
                currentLevel = currentRequest.Level;
                hasUsedFreeRerollThisLevel = false;
            }
            else
            {
                hasUsedFreeRerollThisLevel = true; // Boss chest has no free reroll
            }

            UpdateTitleText();
            PopulateUpgradeCards(currentRequest.Type == RewardModalType.BossChest && currentRequest.GuaranteesEvolution);
            UpdateRerollButtonText();

            if (levelUpModalPanel != null && !levelUpModalPanel.activeSelf)
            {
                levelUpModalPanel.SetActive(true);
            }

            PixelSurvivor.Audio.AudioManager.Instance?.PlayUiLevelUp();

            TimeScaleManager.Instance?.AcquirePause("LevelUpModal");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void UpdateTitleText()
        {
            if (levelTitleText == null) return;

            int extraQueue = pendingRewards.Count;
            string counterTag = extraQueue > 0 ? $" <size=65%><color=#38BDF8>(+{extraQueue} pending)</color></size>" : "";

            if (currentRequest != null && currentRequest.Type == RewardModalType.BossChest)
            {
                string prefix = currentRequest.GuaranteesEvolution ? "MYTHIC EVOLUTION CHEST!" : "BOSS REWARD CHEST!";
                levelTitleText.text = $"{prefix}{counterTag}";
            }
            else
            {
                levelTitleText.text = $"Level {currentLevel}!{counterTag}";
            }
        }

        public void OnCardClicked(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= cardUIs.Length) return;

            if (selectedIndex == cardIndex)
            {
                ConfirmSelection(cardIndex);
            }
            else
            {
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

            PixelSurvivor.Audio.AudioManager.Instance?.PlayUiClick();
            Debug.Log($"<color=#10B981>[Upgrade Confirmed]</color> Executing reward #{cardIndex + 1}!");

            if (cardUIs[cardIndex] != null)
            {
                cardUIs[cardIndex].ExecuteUpgrade();
            }

            // Immediately advance to next queued level-up or boss chest reward
            ProcessNextPendingReward();
        }

        private void OnRerollClicked()
        {
            PixelSurvivor.Audio.AudioManager.Instance?.PlayUiClick();
            bool isBossEvo = currentRequest != null && currentRequest.Type == RewardModalType.BossChest && currentRequest.GuaranteesEvolution;

            if (!hasUsedFreeRerollThisLevel)
            {
                hasUsedFreeRerollThisLevel = true;
                selectedIndex = -1;
                PopulateUpgradeCards(isBossEvo);
                UpdateRerollButtonText();
                Debug.Log("<color=#10B981>[Reroll]</color> Refreshed cards for FREE (Level Free Reroll)!");
                return;
            }

            if (!CanRerollMore)
            {
                Debug.LogWarning("[LevelUpManager] Reached max paid rerolls for this level!");
                return;
            }

            int cost = CurrentRerollCost;
            if (ExperienceManager.Instance == null || ExperienceManager.Instance.CurrentEmeralds < cost)
            {
                Debug.LogWarning($"[LevelUpManager] Not enough emeralds! Need {cost} 💎");
                return;
            }

            ExperienceManager.Instance.SpendEmeralds(cost);
            paidRerollsThisLevel++;
            rerollsUsedThisRun++;
            selectedIndex = -1;
            PopulateUpgradeCards(isBossEvo);
            UpdateRerollButtonText();
            Debug.Log($"<color=#10B981>[Reroll]</color> Refreshed cards for {cost} 💎! (Remaining: {ExperienceManager.Instance.CurrentEmeralds} 💎)");
        }

        private void UpdateRerollButtonText()
        {
            int cost = CurrentRerollCost;
            int curEmeralds = ExperienceManager.Instance != null ? ExperienceManager.Instance.CurrentEmeralds : 0;
            bool isFree = !hasUsedFreeRerollThisLevel;
            bool canRoll = CanRerollMore;
            bool canAfford = isFree || (curEmeralds >= cost);

            if (rerollButtonText != null)
            {
                if (!canRoll)
                {
                    rerollButtonText.text = "Reroll <color=#6B7280>(Limit)</color>";
                }
                else if (isFree)
                {
                    rerollButtonText.text = "Reroll <color=#10B981>(FREE)</color>";
                }
                else
                {
                    string colorHex = canAfford ? "#10B981" : "#EF4444";
                    rerollButtonText.text = $"Reroll (<color={colorHex}>{cost}</color>)";
                }
            }
            if (rerollButton != null)
            {
                rerollButton.interactable = canRoll && canAfford;
            }
        }

        private void PopulateUpgradeCards(bool isBossEvolutionChest = false)
        {
            EnsureSpritesLoaded();
            if (playerController == null) playerController = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (weaponController == null && playerController != null) weaponController = playerController.GetComponent<WeaponController>();
            if (playerHealth == null && playerController != null) playerHealth = playerController.GetComponent<PlayerHealth>();

            float timeElapsed = WaveSpawner.Instance != null ? WaveSpawner.Instance.TimeElapsed : 0f;
            bool isEvolutionTime = timeElapsed >= 540f; // 09:00
            bool isPityTime = timeElapsed >= 480f;      // 08:00

            List<UpgradeOptionData> evolutionCards = new List<UpgradeOptionData>();
            List<UpgradeOptionData> pitySwapCards = new List<UpgradeOptionData>();
            List<UpgradeOptionData> regularCards = new List<UpgradeOptionData>();

            // 1. Check for Evolutions
            if (weaponController != null && weaponController.CanEvolveMore)
            {
                var equipped = weaponController.EquippedWeapons;
                for (int i = 0; i < equipped.Count; i++)
                {
                    var w = equipped[i];
                    if (w == null || w.isEvolved) continue;

                    var pairedType = PassiveController.GetPairedPassiveForWeapon(w.weaponId);
                    if (!pairedType.HasValue) continue;

                    int passiveLv = PassiveController.Instance != null ? PassiveController.Instance.GetLevel(pairedType.Value) : 0;

                    // Condition for Evolution: Weapon Lv8 + Paired Passive Lv5 + Time >= 540s (or Boss Evolution Chest with Lv8 weapon)
                    bool canEvolve = (w.currentLevel >= 8 && passiveLv >= 5 && isEvolutionTime) ||
                                     (isBossEvolutionChest && w.currentLevel >= 8);

                    if (canEvolve)
                    {
                        string wId = w.weaponId;
                        string evoName = PassiveController.GetEvolvedWeaponName(wId);
                        evolutionCards.Add(new UpgradeOptionData
                        {
                            title = evoName,
                            description = GetEvolutionDescription(wId),
                            synergy = $"Mythic Evolution: Combine Lv8 {w.weaponName} + Lv5 {PassiveController.GetPassiveName(pairedType.Value)}!",
                            badge1 = "EVOLUTION",
                            badge2 = "EVOLVE",
                            badge2Color = new Color(0.961f, 0.620f, 0.043f, 1f),
                            icon = w.icon,
                            action = () =>
                            {
                                if (weaponController != null) weaponController.EvolveWeapon(wId);
                            }
                        });
                    }

                    // Condition for Evolution Pity (Time >= 480s, Weapon Lv8, Passive missing or under Lv5)
                    if (w.currentLevel >= 8 && isPityTime && passiveLv < 5)
                    {
                        pityRollCounter++;
                        if (PassiveController.Instance != null && !PassiveController.Instance.HasPassive(pairedType.Value) && !PassiveController.Instance.CanEquipNewPassive)
                        {
                            // 4 slots are full but paired passive is missing -> Offer SWAP Card!
                            var slots = PassiveController.Instance.EquippedSlots;
                            PassiveType candidateOld = PassiveType.EnderEye;
                            for (int s = 0; s < slots.Count; s++)
                            {
                                // Pick an un-paired passive (e.g. EnderEye, PotionOfRegen, or passive whose weapon isn't equipped)
                                string pairedW = PassiveController.GetPairedWeaponName(slots[s].type);
                                if (pairedW == null || !weaponController.HasWeapon(slots[s].type.ToString()))
                                {
                                    candidateOld = slots[s].type;
                                    break;
                                }
                            }

                            PassiveType targetNew = pairedType.Value;
                            pitySwapCards.Add(new UpgradeOptionData
                            {
                                title = $"Swap: {PassiveController.GetPassiveName(targetNew)}",
                                description = $"Replace {PassiveController.GetPassiveName(candidateOld)} with {PassiveController.GetPassiveName(targetNew)} Lv.1 to enable {PassiveController.GetEvolvedWeaponName(w.weaponId)}!",
                                synergy = "Evolution Pity: Guarantees the ability to evolve your Lv.8 weapon.",
                                badge1 = "EVOLUTION PITY",
                                badge2 = "new",
                                badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f),
                                icon = GetPassiveIcon(targetNew),
                                action = () =>
                                {
                                    if (PassiveController.Instance != null)
                                    {
                                        PassiveController.Instance.SwapPassive(candidateOld, targetNew);
                                    }
                                }
                            });
                        }
                    }
                }
            }

            // 2. Equipped Weapon Upgrades (Lv1 -> Lv8)
            if (weaponController != null)
            {
                var equipped = weaponController.EquippedWeapons;
                for (int i = 0; i < equipped.Count; i++)
                {
                    var w = equipped[i];
                    if (w != null && !w.isEvolved && w.currentLevel < w.maxLevel)
                    {
                        string wId = w.weaponId;
                        int nextLevel = w.currentLevel + 1;
                        bool isMax = nextLevel >= w.maxLevel;
                        regularCards.Add(new UpgradeOptionData
                        {
                            title = w.weaponName,
                            description = w.GetNextLevelDescription(),
                            synergy = string.IsNullOrEmpty(w.WeaponSynergy) ? "Upgrade equipped weapon." : w.WeaponSynergy,
                            badge1 = "Upgrade",
                            badge2 = isMax ? "max" : $"Lv {nextLevel}",
                            badge2Color = isMax ? new Color(0.690f, 0.149f, 1.0f, 1f) : new Color(0.055f, 0.647f, 0.914f, 1f),
                            icon = w.icon,
                            action = () =>
                            {
                                if (weaponController != null) weaponController.UpgradeWeapon(wId);
                            }
                        });
                    }
                }

                // 3. New Weapons (if equipped < 4)
                if (equipped.Count < WeaponController.MaxWeapons && allWeaponPrefabs != null)
                {
                    for (int i = 0; i < allWeaponPrefabs.Length; i++)
                    {
                        var prefab = allWeaponPrefabs[i];
                        if (prefab == null) continue;
                        var wb = prefab.GetComponent<WeaponBase>();
                        if (wb != null && !weaponController.HasWeapon(wb.weaponId))
                        {
                            GameObject captured = prefab;
                            regularCards.Add(new UpgradeOptionData
                            {
                                title = wb.weaponName,
                                description = wb.GetBaseDescription(),
                                synergy = string.IsNullOrEmpty(wb.WeaponSynergy) ? "New Weapon: Adds an extra attack source." : wb.WeaponSynergy,
                                badge1 = "New Weapon",
                                badge2 = "new",
                                badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f),
                                icon = wb.icon,
                                action = () =>
                                {
                                    if (weaponController != null) weaponController.EquipNewWeaponFromPrefab(captured);
                                }
                            });
                        }
                    }
                }
            }

            // 4. Passives (Upgrades & New)
            if (PassiveController.Instance != null)
            {
                var equippedPassives = PassiveController.Instance.EquippedSlots;

                // 4A. Upgrades for equipped passives (< Lv5)
                for (int i = 0; i < equippedPassives.Count; i++)
                {
                    var slot = equippedPassives[i];
                    if (slot != null && slot.level < PassiveSlot.MaxLevel)
                    {
                        PassiveType pType = slot.type;
                        int nextLv = slot.level + 1;
                        bool isMax = nextLv >= PassiveSlot.MaxLevel;
                        int weight = (isPityTime && IsPairedWithLv8Weapon(pType)) ? 3 : 1;

                        for (int w = 0; w < weight; w++)
                        {
                            regularCards.Add(new UpgradeOptionData
                            {
                                title = PassiveController.GetPassiveName(pType),
                                description = GetPassiveDescription(pType, slot.level),
                                synergy = GetPassiveSynergy(pType),
                                badge1 = "Passive Upgrade",
                                badge2 = isMax ? "max" : $"Lv {nextLv}",
                                badge2Color = isMax ? new Color(0.690f, 0.149f, 1.0f, 1f) : new Color(0.055f, 0.647f, 0.914f, 1f),
                                icon = GetPassiveIcon(pType),
                                action = () =>
                                {
                                    PassiveController.Instance.EquipOrUpgradePassive(pType);
                                }
                            });
                        }
                    }
                }

                // 4B. New Passives (if player has free passive slots)
                if (PassiveController.Instance.CanEquipNewPassive)
                {
                    foreach (PassiveType pType in Enum.GetValues(typeof(PassiveType)))
                    {
                        if (!PassiveController.Instance.HasPassive(pType))
                        {
                            PassiveType capturedType = pType;
                            int weight = (isPityTime && IsPairedWithLv8Weapon(pType)) ? 3 : 1;

                            for (int w = 0; w < weight; w++)
                            {
                                regularCards.Add(new UpgradeOptionData
                                {
                                    title = PassiveController.GetPassiveName(capturedType),
                                    description = GetPassiveDescription(capturedType, 0),
                                    synergy = GetPassiveSynergy(capturedType),
                                    badge1 = "New Passive",
                                    badge2 = "new",
                                    badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f),
                                    icon = GetPassiveIcon(capturedType),
                                    action = () =>
                                    {
                                        PassiveController.Instance.EquipOrUpgradePassive(capturedType);
                                    }
                                });
                            }
                        }
                    }
                }
            }

            // Shuffle regular cards
            for (int i = 0; i < regularCards.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, regularCards.Count);
                var temp = regularCards[i];
                regularCards[i] = regularCards[r];
                regularCards[r] = temp;
            }

            // 5. Select exactly 3 final choices
            List<UpgradeOptionData> finalChoices = new List<UpgradeOptionData>();

            // Always prioritize evolution card if available
            if (evolutionCards.Count > 0)
            {
                finalChoices.Add(evolutionCards[0]);
            }

            // Prioritize pity swap card if guarantee is triggered
            if (pitySwapCards.Count > 0 && (finalChoices.Count < 3 || pityRollCounter >= 3))
            {
                finalChoices.Add(pitySwapCards[0]);
                pityRollCounter = 0;
            }

            // Fill from regular cards
            HashSet<string> pickedTitles = new HashSet<string>();
            for (int i = 0; i < finalChoices.Count; i++) pickedTitles.Add(finalChoices[i].title);

            for (int i = 0; i < regularCards.Count && finalChoices.Count < 3; i++)
            {
                if (!pickedTitles.Contains(regularCards[i].title))
                {
                    finalChoices.Add(regularCards[i]);
                    pickedTitles.Add(regularCards[i].title);
                }
            }

            // Fill remaining slots from Backup pool (Hearty Meal, Emerald Pouch, Scholar's Tome, Bounty Mark)
            int backupIndex = 0;
            while (finalChoices.Count < 3)
            {
                finalChoices.Add(GetBackupCardOption(backupIndex++));
            }

            // Bind to the 3 card UI slots
            for (int i = 0; i < cardUIs.Length; i++)
            {
                if (cardUIs[i] == null) continue;
                int idx = i;
                UpgradeOptionData opt = finalChoices[i];
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

        private bool IsPairedWithLv8Weapon(PassiveType passiveType)
        {
            if (weaponController == null) return false;
            var equipped = weaponController.EquippedWeapons;
            for (int i = 0; i < equipped.Count; i++)
            {
                if (equipped[i] != null && equipped[i].currentLevel >= 8)
                {
                    var paired = PassiveController.GetPairedPassiveForWeapon(equipped[i].weaponId);
                    if (paired.HasValue && paired.Value == passiveType) return true;
                }
            }
            return false;
        }

        private string GetEvolutionDescription(string baseWeaponId)
        {
            switch (baseWeaponId)
            {
                case "weapon_magma_broadsword":
                case "weapon_broadsword":
                    return "Cleaves with blazing fury and unleashes Solar Nova explosions. Continuous Fire Aura burns up to 12 nearby enemies (12 dmg/s)!";
                case "weapon_battle_axe":
                    return "Devastating shockwave cleave with 0.4s Stun and high Knockback. Instantly executes regular enemies below 10% HP!";
                case "weapon_twin_daggers":
                    return "Teleports with phantom dashes, striking high-priority Bosses and Elites with 8 rapid flurries (16 dmg each)!";
                case "weapon_dark_scythe":
                    return "Full 360° sweeping death harvest. Grants 4% lifesteal (max 8 HP/s) and regenerates a 30 HP Soul Shield barrier!";
                case "weapon_crystal_laser":
                case "weapon_magic_staff":
                    return "Unleashes 2 consecutive purple Oblivion laser beams back-to-back (2x95 dmg), piercing all enemies in line!";
                case "weapon_crossbow":
                case "weapon_repeating_crossbow":
                    return "Rapid-fire Gatling barrage of 5 explosive energy bolts with 4 pierces and 1.0m AoE detonation on every hit!";
                default:
                    return "Supreme Mythic weapon evolution!";
            }
        }

        public Sprite GetPassiveIcon(PassiveType type)
        {
            EnsureSpritesLoaded();
            switch (type)
            {
                case PassiveType.BookOfDamage: return iconBookDamage;
                case PassiveType.FeatherOfSpeed: return iconFeatherSpeed;
                case PassiveType.HeartOfLife: return iconHeartLife;
                case PassiveType.IronShield: return iconShieldArmor;
                case PassiveType.ChronosClock: return iconClockCooldown;
                case PassiveType.EmeraldOfWisdom: return iconEmeraldWisdom;
                case PassiveType.EnderEye: return iconEnderEye;
                case PassiveType.PotionOfRegen: return iconPotionRegen;
                case PassiveType.TotemOfUndying: return iconTotemRevive;
                default: return null;
            }
        }

        private string GetPassiveDescription(PassiveType type, int currentLevel)
        {
            int nextLv = currentLevel + 1;
            switch (type)
            {
                case PassiveType.BookOfDamage:
                    return $"+8% Weapon Damage to all equipped weapons (Total: +{nextLv * 8}%).";
                case PassiveType.FeatherOfSpeed:
                    return $"+5% Movement Speed bonus (Total: +{nextLv * 5}%).";
                case PassiveType.HeartOfLife:
                    return $"+15 Max Health and instantly restores 15 HP (Total: +{nextLv * 15} HP).";
                case PassiveType.IronShield:
                    return $"+5% Armor damage reduction (Total: +{nextLv * 5}%).";
                case PassiveType.ChronosClock:
                    return $"-5% Weapon Attack Cooldown on all weapons (Total: -{nextLv * 5}% CDR).";
                case PassiveType.EmeraldOfWisdom:
                    if (nextLv >= 5) return "+8% EXP gain (Total: +40%) & +10% Bonus Damage vs Boss/Elites!";
                    return $"+8% EXP gain from collected gems (Total: +{nextLv * 8}%).";
                case PassiveType.EnderEye:
                    return $"+20% EXP and Emerald vacuum pickup range (Total: +{nextLv * 20}%).";
                case PassiveType.PotionOfRegen:
                    return $"+0.4 HP/s Continuous Health Regeneration (Total: +{(nextLv * 0.4f):F1} HP/s).";
                case PassiveType.TotemOfUndying:
                    if (nextLv >= 5) return "Resurrects with 80% HP upon fatal damage + 2.0s Invincibility!";
                    return $"Resurrects with {(30 + nextLv * 10)}% HP upon fatal damage.";
                default:
                    return "Empowers your character.";
            }
        }

        private string GetPassiveSynergy(PassiveType type)
        {
            switch (type)
            {
                case PassiveType.BookOfDamage: return "Synergy: Magma Broadsword -> Solar Flare Greatsword";
                case PassiveType.FeatherOfSpeed: return "Synergy: Twin Daggers -> Shadow Phantom Daggers";
                case PassiveType.HeartOfLife: return "Synergy: Battle Axe -> Executioner's Greataxe";
                case PassiveType.IronShield: return "Synergy: Dark Scythe -> Reaper's Harvest";
                case PassiveType.ChronosClock: return "Synergy: Crystal Laser -> Oblivion Death Ray";
                case PassiveType.EmeraldOfWisdom: return "Synergy: Crossbow -> Gatling Repeater";
                case PassiveType.EnderEye: return "Utility: Vacuum gems and drops across screen.";
                case PassiveType.PotionOfRegen: return "Sustain: Continuous health recovery.";
                case PassiveType.TotemOfUndying: return "Defensive: One-time legendary resurrection upon death.";
                default: return "";
            }
        }

        private UpgradeOptionData GetBackupCardOption(int index)
        {
            EnsureSpritesLoaded();
            switch (index % 4)
            {
                case 0:
                    return new UpgradeOptionData
                    {
                        title = "Hearty Meal",
                        description = "Instantly restore 40% of Max Health.",
                        synergy = "Backup Supply: Sustains health when combat intensifies.",
                        badge1 = "Supply",
                        badge2 = "new",
                        badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f),
                        icon = iconHeartyMeal,
                        action = () =>
                        {
                            if (playerHealth != null) playerHealth.Heal(playerHealth.MaxHealth * 0.40f);
                        }
                    };
                case 1:
                    return new UpgradeOptionData
                    {
                        title = "Emerald Pouch",
                        description = "Instantly receive +25 Emeralds.",
                        synergy = "Backup Supply: Stockpile currency for future rerolls & meta upgrades.",
                        badge1 = "Supply",
                        badge2 = "new",
                        badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f),
                        icon = iconEmeraldPouch,
                        action = () =>
                        {
                            if (ExperienceManager.Instance != null) ExperienceManager.Instance.AddEmeralds(25);
                        }
                    };
                case 2:
                    return new UpgradeOptionData
                    {
                        title = "Scholar's Tome",
                        description = "Instantly grant +150 bonus EXP.",
                        synergy = "Backup Supply: Accelerates progression towards the next level.",
                        badge1 = "Supply",
                        badge2 = "new",
                        badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f),
                        icon = iconScholarsTome,
                        action = () =>
                        {
                            if (ExperienceManager.Instance != null) ExperienceManager.Instance.AddExp(150);
                        }
                    };
                case 3:
                default:
                    return new UpgradeOptionData
                    {
                        title = "Bounty Mark",
                        description = "+10% Bonus damage dealt to Bosses & Elites for the rest of the run.",
                        synergy = "Backup Supply: Decimates dangerous late-game commanders.",
                        badge1 = "Supply",
                        badge2 = "new",
                        badge2Color = new Color(0.125f, 0.749f, 0.420f, 1f),
                        icon = iconBountyMark,
                        action = () =>
                        {
                            if (PlayerStats.Instance != null) PlayerStats.Instance.AddBonusBossEliteDamage(0.10f);
                        }
                    };
            }
        }

        private void EnsureSpritesLoaded()
        {
            if (iconBookDamage == null) iconBookDamage = Resources.Load<Sprite>("Passives/item_book_damage");
            if (iconFeatherSpeed == null) iconFeatherSpeed = Resources.Load<Sprite>("Passives/item_feather_speed");
            if (iconHeartLife == null) iconHeartLife = Resources.Load<Sprite>("Passives/item_heart_max_hp");
            if (iconShieldArmor == null) iconShieldArmor = Resources.Load<Sprite>("Passives/item_shield_armor");
            if (iconClockCooldown == null) iconClockCooldown = Resources.Load<Sprite>("Passives/item_clock_cooldown");
            if (iconEmeraldWisdom == null) iconEmeraldWisdom = Resources.Load<Sprite>("Passives/item_emerald_exp");
            if (iconEnderEye == null) iconEnderEye = Resources.Load<Sprite>("Passives/item_ender_eye_pickup");
            if (iconPotionRegen == null) iconPotionRegen = Resources.Load<Sprite>("Passives/item_potion_regen");
            if (iconTotemRevive == null) iconTotemRevive = Resources.Load<Sprite>("Passives/item_totem_revive");

            if (iconHeartyMeal == null) iconHeartyMeal = Resources.Load<Sprite>("Drops/drop_cooked_steak");
            if (iconEmeraldPouch == null) iconEmeraldPouch = Resources.Load<Sprite>("UI/Icons/icon_emerald");
            if (iconScholarsTome == null) iconScholarsTome = Resources.Load<Sprite>("Passives/item_book_damage");
            if (iconBountyMark == null) iconBountyMark = Resources.Load<Sprite>("Drops/drop_lucky_block");

#if UNITY_EDITOR
            if (iconBookDamage == null) iconBookDamage = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_book_damage.png");
            if (iconFeatherSpeed == null) iconFeatherSpeed = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_feather_speed.png");
            if (iconHeartLife == null) iconHeartLife = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_heart_max_hp.png");
            if (iconShieldArmor == null) iconShieldArmor = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_shield_armor.png");
            if (iconClockCooldown == null) iconClockCooldown = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_clock_cooldown.png");
            if (iconEmeraldWisdom == null) iconEmeraldWisdom = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_emerald_exp.png");
            if (iconEnderEye == null) iconEnderEye = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_ender_eye_pickup.png");
            if (iconPotionRegen == null) iconPotionRegen = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_potion_regen.png");
            if (iconTotemRevive == null) iconTotemRevive = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_totem_revive.png");

            if (iconHeartyMeal == null) iconHeartyMeal = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_cooked_steak.png");
            if (iconEmeraldPouch == null) iconEmeraldPouch = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/UI/Icons/icon_emerald.png");
            if (iconScholarsTome == null) iconScholarsTome = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_book_damage.png");
            if (iconBountyMark == null) iconBountyMark = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_lucky_block.png");
#endif
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
