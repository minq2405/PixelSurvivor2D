using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelSurvivor
{
    public class CharacterPedestalUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Character Information")]
        public string characterId = "Steve";
        public string characterDisplayName = "STEVE";
        [TextArea] public string characterDescription = "Balanced hero with solid stats.";

        [Header("Visual References")]
        [SerializeField] private RectTransform characterTransform;
        [SerializeField] private SimpleSpriteAnimator spriteAnimator;
        [SerializeField] private Animator unityAnimator;
        [SerializeField] private Image selectionRing;

        [Header("Hover Animation Settings")]
        [SerializeField] private float normalScale = 1.0f;
        [SerializeField] private float hoverScale = 1.18f;
        [SerializeField] private float scaleSpeed = 12f;

        private float targetScale = 1.0f;
        private MainMenuManager menuManager;

        private void Awake()
        {
            if (characterTransform == null)
            {
                characterTransform = GetComponent<RectTransform>();
            }
            if (spriteAnimator == null)
            {
                spriteAnimator = GetComponentInChildren<SimpleSpriteAnimator>();
            }
            if (unityAnimator == null)
            {
                unityAnimator = GetComponentInChildren<Animator>();
            }
            targetScale = normalScale;
            if (selectionRing != null)
            {
                selectionRing.enabled = false;
            }
        }

        public void Initialize(MainMenuManager manager)
        {
            menuManager = manager;
        }

        private void Update()
        {
            if (characterTransform != null)
            {
                float currentScale = characterTransform.localScale.x;
                float newScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
                characterTransform.localScale = new Vector3(newScale, newScale, 1f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = hoverScale;
            if (spriteAnimator != null)
            {
                spriteAnimator.SetRunning(true);
            }
            if (unityAnimator != null)
            {
                unityAnimator.SetFloat("Speed", 1.0f);
            }

            if (menuManager != null)
            {
                menuManager.OnCharacterHovered(this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = normalScale;
            if (spriteAnimator != null)
            {
                spriteAnimator.SetRunning(false);
            }
            if (unityAnimator != null)
            {
                unityAnimator.SetFloat("Speed", 0.0f);
            }

            if (menuManager != null)
            {
                menuManager.OnCharacterUnhovered(this);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (menuManager != null)
            {
                menuManager.OnCharacterSelected(characterId);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionRing != null)
            {
                selectionRing.enabled = selected;
            }
        }
    }
}
