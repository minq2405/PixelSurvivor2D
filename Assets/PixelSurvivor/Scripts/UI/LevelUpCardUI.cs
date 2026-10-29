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

            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;

            if (synergyText != null)
            {
                bool hasSynergy = !string.IsNullOrEmpty(synergy);
                synergyText.gameObject.SetActive(hasSynergy);
                if (hasSynergy) synergyText.text = synergy;
            }

            if (badge1Text != null) badge1Text.text = badge1;
            if (badge1Container != null) badge1Container.SetActive(!string.IsNullOrEmpty(badge1));

            if (badge2Text != null) badge2Text.text = badge2;
            if (badge2Container != null) badge2Container.SetActive(!string.IsNullOrEmpty(badge2));
            if (badge2Background != null && !string.IsNullOrEmpty(badge2))
            {
                badge2Background.FillColor = badge2Color;
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
