using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PixelSurvivor
{
    [RequireComponent(typeof(Button))]
    public class MinecraftButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Scale Feedback")]
        [SerializeField] private float normalScale = 1.0f;
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float scaleSpeed = 20f;

        [Header("Press Physical Offset (Applied to Text/Face)")]
        [SerializeField] private float pressYOffset = -3f;

        [Header("Text Color Feedback (Minecraft Style)")]
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color hoverTextColor = new Color(1f, 1f, 0.35f, 1f); // Classic Minecraft yellow
        [SerializeField] private Color pressedTextColor = new Color(0.85f, 0.85f, 0.25f, 1f);

        [Header("Button Image Tint")]
        [SerializeField] private Color normalImageColor = Color.white;
        [SerializeField] private Color hoverImageColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        [SerializeField] private Color pressedImageColor = new Color(0.72f, 0.72f, 0.72f, 1f);

        [Header("Optional Audio")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;

        private RectTransform rectTransform;
        private Image buttonImage;
        private TMP_Text buttonTmpText;
        private Text buttonLegacyText;
        private Button button;

        private float targetScale = 1.0f;
        private float currentYOffset = 0f;
        private float targetYOffset = 0f;
        private Color targetTextColor;
        private Color targetImageColor;
        private Vector2 baseTextAnchoredPosition = Vector2.zero;
        private bool isHovered = false;
        private bool isPressed = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            buttonTmpText = GetComponentInChildren<TMP_Text>();

            if (buttonTmpText != null)
            {
                baseTextAnchoredPosition = buttonTmpText.rectTransform.anchoredPosition;
            }
            else
            {
                buttonLegacyText = GetComponentInChildren<Text>();
                if (buttonLegacyText != null)
                {
                    baseTextAnchoredPosition = buttonLegacyText.rectTransform.anchoredPosition;
                }
            }

            targetScale = normalScale;
            targetYOffset = 0f;
            targetTextColor = normalTextColor;
            targetImageColor = normalImageColor;

            // Configure Unity Button transition to None so our script has 100% smooth control
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
            }
        }

        private void OnEnable()
        {
            ResetVisuals();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime * scaleSpeed;

            // 1. Smooth Scale on Button
            if (rectTransform != null)
            {
                float newScale = Mathf.Lerp(rectTransform.localScale.x, targetScale, dt);
                rectTransform.localScale = new Vector3(newScale, newScale, 1f);
            }

            // 2. Smooth Text Offset and Color (Simulates physical button press depression)
            if (buttonTmpText != null)
            {
                currentYOffset = Mathf.Lerp(currentYOffset, targetYOffset, dt);
                buttonTmpText.rectTransform.anchoredPosition = baseTextAnchoredPosition + new Vector2(0f, currentYOffset);
                buttonTmpText.color = Color.Lerp(buttonTmpText.color, targetTextColor, dt);
            }
            else if (buttonLegacyText != null)
            {
                currentYOffset = Mathf.Lerp(currentYOffset, targetYOffset, dt);
                buttonLegacyText.rectTransform.anchoredPosition = baseTextAnchoredPosition + new Vector2(0f, currentYOffset);
                buttonLegacyText.color = Color.Lerp(buttonLegacyText.color, targetTextColor, dt);
            }

            // 3. Smooth Image Tint
            if (buttonImage != null)
            {
                buttonImage.color = Color.Lerp(buttonImage.color, targetImageColor, dt);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;

            isHovered = true;
            if (!isPressed)
            {
                targetScale = hoverScale;
                targetYOffset = 0f;
                targetTextColor = hoverTextColor;
                targetImageColor = hoverImageColor;
            }

            PlaySound(hoverSound);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (!isPressed)
            {
                ResetVisuals();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;

            isPressed = true;
            targetScale = pressScale;
            targetYOffset = pressYOffset;
            targetTextColor = pressedTextColor;
            targetImageColor = pressedImageColor;

            PlaySound(clickSound);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            if (isHovered)
            {
                targetScale = hoverScale;
                targetYOffset = 0f;
                targetTextColor = hoverTextColor;
                targetImageColor = hoverImageColor;
            }
            else
            {
                ResetVisuals();
            }
        }

        private void ResetVisuals()
        {
            isHovered = false;
            isPressed = false;
            targetScale = normalScale;
            targetYOffset = 0f;
            targetTextColor = normalTextColor;
            targetImageColor = normalImageColor;
            currentYOffset = 0f;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            }
        }
    }
}
