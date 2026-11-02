using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Represents one of the 3 selectable reward cards in the Level Up Modal.
    /// Supports a 2-click selection mechanic:
    /// - Click 1: Highlight / Hover preview state (Gold border, scale 1.02x, confirm hint text).
    /// - Click 2: Confirm and execute the upgrade.
    /// </summary>
    public class LevelUpCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Card Identity")]
        public int cardIndex = 0; // 0, 1, 2 (maps to hotkeys 1, 2, 3)

        [Header("UI Element References")]
        [SerializeField] private RoundedBorderImage cardBackground;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text badge1Text;
        [SerializeField] private GameObject badge1Container;
        [SerializeField] private TMP_Text badge2Text;
        [SerializeField] private RoundedBorderImage badge2Background;
        [SerializeField] private GameObject badge2Container;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text synergyText;
        [SerializeField] private TMP_Text hotkeyText;
        [SerializeField] private TMP_Text confirmHintText;

        [Header("Colors & Visual Styling")]
        [SerializeField] private Color normalBgColor = new Color(0.082f, 0.165f, 0.396f, 1f);       // #152A65
        [SerializeField] private Color normalBorderColor = new Color(0.220f, 0.357f, 0.686f, 1f);   // #385BAF
        [SerializeField] private Color selectedBgColor = new Color(0.118f, 0.239f, 0.580f, 1f);     // #1E3D94
        [SerializeField] private Color selectedBorderColor = new Color(0.980f, 0.800f, 0.082f, 1f); // #FACC15 Gold
        [SerializeField] private Color hoverBorderColor = new Color(0.353f, 0.557f, 0.965f, 1f);    // #5A8EF6 Cyan

        private bool isSelected = false;
        private Action onCardClickedCallback;
        private Action executeUpgradeAction;

        public bool IsSelected => isSelected;

        private void Awake()
        {
            EnsureBadge2Exists();
        }

        public void EnsureBadge2Exists()
        {
            if (badge2Container != null && badge2Text != null && badge2Background != null) return;

            Transform titleParent = titleText != null ? titleText.transform.parent : (transform.Find("Card/Title_Row") ?? transform.Find("Title_Row"));

            // 1. Try finding existing Badge_2 under titleParent or in hierarchy
            if (badge2Container == null && titleParent != null)
            {
                Transform found = titleParent.Find("Badge_2");
                if (found != null) badge2Container = found.gameObject;
            }

            if (badge2Container == null)
            {
                Transform foundDeep = transform.Find("Badge_2");
                if (foundDeep != null) badge2Container = foundDeep.gameObject;
            }

            // 2. Clone from sibling Card that has Badge_2 (e.g. Card 1)
            if (badge2Container == null)
            {
#pragma warning disable CS0618
                LevelUpCardUI[] allCards = transform.parent != null ? transform.parent.GetComponentsInChildren<LevelUpCardUI>(true) : FindObjectsByType<LevelUpCardUI>(FindObjectsSortMode.None);
#pragma warning restore CS0618
                foreach (var sibling in allCards)
                {
                    if (sibling != null && sibling != this && sibling.badge2Container != null)
                    {
                        GameObject clone = Instantiate(sibling.badge2Container, titleParent != null ? titleParent : transform);
                        clone.name = "Badge_2";
                        badge2Container = clone;
                        break;
                    }
                }
            }

            // 3. Fallback: Create programmatically if no sibling had Badge_2
            if (badge2Container == null && titleParent != null)
            {
                GameObject newBadge = new GameObject("Badge_2", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBorderImage));
                newBadge.transform.SetParent(titleParent, false);
                RectTransform bRt = newBadge.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0f, 0.5f);
                bRt.anchorMax = new Vector2(0f, 0.5f);
                bRt.pivot = new Vector2(0f, 0.5f);
                bRt.sizeDelta = new Vector2(50f, 20f);

                RoundedBorderImage rbi = newBadge.GetComponent<RoundedBorderImage>();
                rbi.CornerRadius = 6f;
                rbi.BorderThickness = 0f;

                GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(newBadge.transform, false);
                RectTransform lRt = labelObj.GetComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.sizeDelta = Vector2.zero;
                lRt.anchoredPosition = Vector2.zero;

                TextMeshProUGUI tmp = labelObj.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    tmp.font = titleText.font;
                    tmp.fontSize = 13f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                }

                badge2Container = newBadge;
            }

            if (badge2Container != null)
            {
                if (badge2Background == null) badge2Background = badge2Container.GetComponent<RoundedBorderImage>();
                if (badge2Text == null) badge2Text = badge2Container.GetComponentInChildren<TMP_Text>();
            }
        }

        public void BindCard(
            int index,
            string title,
            string description,
            string synergy,
            string badge1,
            string badge2,
            Color badge2Color,
            Sprite icon,
            Action clickCallback,
            Action upgradeAction)
        {
            cardIndex = index;
            onCardClickedCallback = clickCallback;
            executeUpgradeAction = upgradeAction;

            EnsureBadge2Exists();

            if (titleText != null)
            {
                titleText.text = title;
                titleText.ForceMeshUpdate();
            }

            if (descriptionText != null) descriptionText.text = description;

            if (synergyText != null)
            {
                bool hasSynergy = !string.IsNullOrEmpty(synergy);
                synergyText.gameObject.SetActive(hasSynergy);
                if (hasSynergy) synergyText.text = synergy;
            }

            // Hide obsolete badge1 so only the rounded pill badge appears
            if (badge1Container != null) badge1Container.SetActive(false);

            if (badge2Container != null)
            {
                bool hasBadge = !string.IsNullOrEmpty(badge2);
                badge2Container.SetActive(hasBadge);

                if (hasBadge)
                {
                    if (badge2Text != null)
                    {
                        badge2Text.text = badge2;
                        badge2Text.ForceMeshUpdate();
                    }

                    if (badge2Background != null)
                    {
                        badge2Background.FillColor = badge2Color;
                        badge2Background.CornerRadius = 6f;
                        badge2Background.BorderThickness = 0f;
                    }

                    // Auto-align badge to the right of the title text based on title length
                    RectTransform badgeRT = badge2Container.GetComponent<RectTransform>();
                    if (badgeRT != null && titleText != null)
                    {
                        float titleWidth = titleText.preferredWidth;
                        float textWidth = badge2Text != null ? badge2Text.preferredWidth : 30f;
                        float badgeWidth = Mathf.Max(38f, textWidth + 16f);

                        badgeRT.anchorMin = new Vector2(0f, 0.5f);
                        badgeRT.anchorMax = new Vector2(0f, 0.5f);
                        badgeRT.pivot = new Vector2(0f, 0.5f);
                        badgeRT.anchoredPosition = new Vector2(titleWidth + 10f, 0f);
                        badgeRT.sizeDelta = new Vector2(badgeWidth, 20f);
                    }
                }
            }

            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }

            if (hotkeyText != null)
            {
                hotkeyText.text = (index + 1).ToString();
            }

            SetSelected(false);
        }

        public void ExecuteUpgrade()
        {
            executeUpgradeAction?.Invoke();
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (cardBackground != null)
            {
                if (isSelected)
                {
                    cardBackground.FillColor = selectedBgColor;
                    cardBackground.BorderColor = selectedBorderColor;
                    cardBackground.BorderThickness = 4f;
                }
                else
                {
                    cardBackground.FillColor = normalBgColor;
                    cardBackground.BorderColor = normalBorderColor;
                    cardBackground.BorderThickness = 2f;
                }
            }

            transform.localScale = isSelected ? new Vector3(1.02f, 1.02f, 1f) : Vector3.one;

            if (confirmHintText != null)
            {
                confirmHintText.gameObject.SetActive(isSelected);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onCardClickedCallback?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isSelected && cardBackground != null)
            {
                cardBackground.BorderColor = hoverBorderColor;
                transform.localScale = new Vector3(1.01f, 1.01f, 1f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isSelected)
            {
                if (cardBackground != null) cardBackground.BorderColor = normalBorderColor;
                transform.localScale = Vector3.one;
            }
        }
    }
}
