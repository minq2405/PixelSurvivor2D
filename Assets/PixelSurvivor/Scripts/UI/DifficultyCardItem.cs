using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PixelSurvivor.Combat;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Individual Difficulty Card item for the Fullscreen Difficulty Selection screen.
    /// Handles 2-click selection pattern:
    /// - 1st Click (or Hover): Focuses card, scales up smoothly (1.08x), activates theme glow.
    /// - 2nd Click (on the same card): Confirms selection and deploys the run with this difficulty.
    /// </summary>
    public class DifficultyCardItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Difficulty Configuration")]
        [SerializeField] private GameDifficulty difficulty = GameDifficulty.Normal;

        [Header("Visual Components")]
        [SerializeField] private Image cardImage;
        [SerializeField] private GameObject selectionGlow;
        [SerializeField] private Button actionButton;

        [Header("Animation & Scale Settings")]
        [SerializeField] private float normalScale = 1.0f;
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private float animSpeed = 14f;

        private DifficultySelectionUI parentModal;
        private bool isSelected = false;
        private bool isHovered = false;
        private float currentTargetScale = 1.0f;
        private float lastClickTimestamp = -1f;

        public GameDifficulty Difficulty => difficulty;
        public bool IsSelected => isSelected;

        public void Initialize(DifficultySelectionUI modal, GameDifficulty diff)
        {
            parentModal = modal;
            difficulty = diff;
            isSelected = false;
            isHovered = false;
            currentTargetScale = normalScale;
            transform.localScale = Vector3.one * normalScale;

            if (cardImage == null) cardImage = GetComponent<Image>();
            if (actionButton == null) actionButton = GetComponent<Button>();
            if (selectionGlow == null)
            {
                Transform glowT = transform.Find("Highlight_Glow");
                if (glowT != null) selectionGlow = glowT.gameObject;
            }

            if (selectionGlow != null)
            {
                selectionGlow.SetActive(false);
            }

            if (actionButton != null)
            {
                // Disable color tint overlay so card doesn't get covered in dark/gray/colored tint on click
                actionButton.transition = Selectable.Transition.None;
                var colors = actionButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = Color.white;
                colors.selectedColor = Color.white;
                actionButton.colors = colors;

                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(HandleButtonClick);
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            currentTargetScale = isHovered ? hoverScale : normalScale;

            if (selectionGlow != null)
            {
                selectionGlow.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            currentTargetScale = hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            currentTargetScale = normalScale;
        }

        private void HandleButtonClick()
        {
            // Release focus so Unity EventSystem doesn't draw a persistent selection outline/tint
            EventSystem.current?.SetSelectedGameObject(null);

            // Prevent duplicate firing within the same 0.25 second window
            if (Time.unscaledTime - lastClickTimestamp < 0.25f)
            {
                return;
            }
            lastClickTimestamp = Time.unscaledTime;

            if (parentModal != null)
            {
                parentModal.OnCardClicked(this);
            }
        }

        private void Update()
        {
            if (Mathf.Abs(transform.localScale.x - currentTargetScale) > 0.001f)
            {
                float newScale = Mathf.Lerp(transform.localScale.x, currentTargetScale, Time.unscaledDeltaTime * animSpeed);
                transform.localScale = new Vector3(newScale, newScale, 1f);
            }
        }
    }
}
