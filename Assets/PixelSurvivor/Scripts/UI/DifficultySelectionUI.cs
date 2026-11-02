using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.Combat;
using PixelSurvivor.Audio;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Fullscreen Difficulty Selection Screen (v2.2 Master Specification).
    /// Features:
    /// - Fullscreen UI panel replacing the small modal popup.
    /// - 3 large difficulty cards (Normal, Hard, Hardcore).
    /// - True 2-Click Selection UX:
    ///   * 1st Click (or Hover): Focuses card, zooms up smoothly (1.08x), activates theme glow.
    ///   * 2nd Click (on the same focused card): Confirms difficulty and deploys match.
    ///   * Clicking another card shifts focus to that new card.
    /// - Back button to return cleanly to Character Selection.
    /// - Zero missing unicode character warnings in console.
    /// </summary>
    public class DifficultySelectionUI : MonoBehaviour
    {
        [Header("Fullscreen Panels")]
        [SerializeField] private GameObject screenPanel;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Button backButton;

        [Header("Difficulty Cards")]
        [SerializeField] private DifficultyCardItem normalCard;
        [SerializeField] private DifficultyCardItem hardCard;
        [SerializeField] private DifficultyCardItem hardcoreCard;

        private Action<GameDifficulty> onDifficultyChosen;
        private DifficultyCardItem currentlySelectedCard;

        private void Awake()
        {
            if (screenPanel == null)
            {
                // Try locating existing panel in hierarchy
                Transform existing = transform.Find("DifficultySelectPanel") ?? transform.Find("Difficulty_Select_Modal");
                if (existing != null)
                {
                    screenPanel = existing.gameObject;
                    WireHierarchyComponents(existing);
                }
                else
                {
                    BuildFullscreenUI();
                }
            }
            else
            {
                WireHierarchyComponents(screenPanel.transform);
            }
        }

        public void WireHierarchyComponents(Transform root)
        {
            if (root == null) return;

            Transform container = cardContainer;
            if (container == null)
            {
                container = root.Find("CardContainer") ??
                            root.Find("Inner_Card_Container") ??
                            root.Find("Modal_Frame/Inner_Card_Container");
                cardContainer = container;
            }

            if (container != null)
            {
                Transform normT = container.Find("Card_Normal");
                if (normT != null)
                {
                    if (normalCard == null) normalCard = normT.GetComponent<DifficultyCardItem>();
                    if (normalCard == null) normalCard = normT.gameObject.AddComponent<DifficultyCardItem>();
                }

                Transform hardT = container.Find("Card_Hard");
                if (hardT != null)
                {
                    if (hardCard == null) hardCard = hardT.GetComponent<DifficultyCardItem>();
                    if (hardCard == null) hardCard = hardT.gameObject.AddComponent<DifficultyCardItem>();
                }

                Transform hcoreT = container.Find("Card_Hardcore");
                if (hcoreT != null)
                {
                    if (hardcoreCard == null) hardcoreCard = hcoreT.GetComponent<DifficultyCardItem>();
                    if (hardcoreCard == null) hardcoreCard = hcoreT.gameObject.AddComponent<DifficultyCardItem>();
                }
            }

            if (normalCard != null) normalCard.Initialize(this, GameDifficulty.Normal);
            if (hardCard != null) hardCard.Initialize(this, GameDifficulty.Hard);
            if (hardcoreCard != null) hardcoreCard.Initialize(this, GameDifficulty.Hardcore);

            if (backButton == null)
            {
                Transform btnTrans = root.Find("Btn_Back") ??
                                     root.Find("Btn_Close") ??
                                     root.Find("Modal_Frame/Btn_Close");
                if (btnTrans != null) backButton = btnTrans.GetComponent<Button>();
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnClickBack);
            }
        }

        public void Show(Action<GameDifficulty> callback)
        {
            onDifficultyChosen = callback;
            WireHierarchyComponents(screenPanel != null ? screenPanel.transform : transform);

            if (screenPanel != null)
            {
                screenPanel.SetActive(true);
            }
            gameObject.SetActive(true);

            // Reset cards selection state on every show
            currentlySelectedCard = null;
            if (normalCard != null) normalCard.SetSelected(false);
            if (hardCard != null) hardCard.SetSelected(false);
            if (hardcoreCard != null) hardcoreCard.SetSelected(false);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void Hide()
        {
            if (screenPanel != null)
            {
                screenPanel.SetActive(false);
            }
            if (gameObject != null && gameObject != screenPanel)
            {
                // If attached to a separate manager, don't deactivate manager, just screenPanel
            }
            else if (gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called when any DifficultyCardItem is clicked.
        /// Confirms immediately with 1 single click!
        /// </summary>
        public void OnCardClicked(DifficultyCardItem card)
        {
            if (card == null) return;
            ConfirmDifficulty(card.Difficulty);
        }

        public void SelectCard(DifficultyCardItem card)
        {
            currentlySelectedCard = card;

            if (normalCard != null) normalCard.SetSelected(card == normalCard);
            if (hardCard != null) hardCard.SetSelected(card == hardCard);
            if (hardcoreCard != null) hardcoreCard.SetSelected(card == hardcoreCard);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }
        }

        public void ConfirmDifficulty(GameDifficulty diff)
        {
            Debug.Log($"<color=#10B981>[DifficultySelectionUI]</color> Difficulty confirmed: <b>{diff}</b>. Starting run...");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }

            Hide();
            onDifficultyChosen?.Invoke(diff);
        }

        public void OnClickBack()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUiClick();
            }

            Hide();
            MainMenuManager mmm = FindAnyObjectByType<MainMenuManager>();
            if (mmm != null)
            {
                mmm.OnClickNewGame();
            }
        }

        #region Procedural Fallback Builder

        private void BuildFullscreenUI()
        {
            screenPanel = new GameObject("DifficultySelectPanel");
            screenPanel.transform.SetParent(transform, false);

            Canvas canvas = screenPanel.GetComponent<Canvas>() ?? screenPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95;

            var scaler = screenPanel.GetComponent<CanvasScaler>() ?? screenPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (screenPanel.GetComponent<GraphicRaycaster>() == null)
            {
                screenPanel.AddComponent<GraphicRaycaster>();
            }

            // 1. Fullscreen Dark Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(screenPanel.transform, false);
            RectTransform bgRT = bgObj.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.04f, 0.07f, 1f);

            // 2. Title & Subtitle Header
            TMP_FontAsset pixelFont = Resources.Load<TMP_FontAsset>("Fonts/VCR_OSD_MONO SDF");

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(screenPanel.transform, false);
            RectTransform titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -80f);
            titleRT.sizeDelta = new Vector2(900f, 60f);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "SELECT DIFFICULTY";
            if (pixelFont != null) titleTxt.font = pixelFont;
            titleTxt.fontSize = 46f;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(0.97f, 0.98f, 0.99f, 1f);

            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(screenPanel.transform, false);
            RectTransform subRT = subObj.AddComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.5f, 1f);
            subRT.anchorMax = new Vector2(0.5f, 1f);
            subRT.pivot = new Vector2(0.5f, 1f);
            subRT.anchoredPosition = new Vector2(0f, -145f);
            subRT.sizeDelta = new Vector2(900f, 35f);
            var subTxt = subObj.AddComponent<TextMeshProUGUI>();
            subTxt.text = "CLICK ONCE TO PREVIEW - CLICK AGAIN TO CONFIRM";
            if (pixelFont != null) subTxt.font = pixelFont;
            subTxt.fontSize = 20f;
            subTxt.alignment = TextAlignmentOptions.Center;
            subTxt.color = new Color(0.6f, 0.65f, 0.72f, 1f);

            // 3. Card Container (Centered horizontally & vertically)
            GameObject contObj = new GameObject("CardContainer");
            contObj.transform.SetParent(screenPanel.transform, false);
            RectTransform contRT = contObj.AddComponent<RectTransform>();
            contRT.anchorMin = new Vector2(0.5f, 0.5f);
            contRT.anchorMax = new Vector2(0.5f, 0.5f);
            contRT.pivot = new Vector2(0.5f, 0.5f);
            contRT.anchoredPosition = new Vector2(0f, -20f);
            contRT.sizeDelta = new Vector2(1000f, 440f);
            cardContainer = contObj.transform;

            // 4. Create the 3 Cards
            Sprite normSp = Resources.Load<Sprite>("Difficulty/Difficulty_Card_Normal");
            Sprite hardSp = Resources.Load<Sprite>("Difficulty/Difficulty_Card_Hard");
            Sprite hcoreSp = Resources.Load<Sprite>("Difficulty/Difficulty_Card_Hardcore");

            normalCard = CreateCardElement("Card_Normal", contObj.transform, new Vector2(-330f, 0f), normSp, GameDifficulty.Normal, new Color(0.063f, 0.725f, 0.506f, 0.9f));
            hardCard = CreateCardElement("Card_Hard", contObj.transform, new Vector2(0f, 0f), hardSp, GameDifficulty.Hard, new Color(0.961f, 0.620f, 0.047f, 0.9f));
            hardcoreCard = CreateCardElement("Card_Hardcore", contObj.transform, new Vector2(330f, 0f), hcoreSp, GameDifficulty.Hardcore, new Color(0.937f, 0.267f, 0.267f, 0.9f));

            // 5. Back Button (Bottom Center)
            GameObject btnBackObj = new GameObject("Btn_Back");
            btnBackObj.transform.SetParent(screenPanel.transform, false);
            RectTransform backRT = btnBackObj.AddComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0f);
            backRT.anchorMax = new Vector2(0.5f, 0f);
            backRT.pivot = new Vector2(0.5f, 0f);
            backRT.anchoredPosition = new Vector2(0f, 60f);
            backRT.sizeDelta = new Vector2(220f, 52f);

            var backImg = btnBackObj.AddComponent<Image>();
            backImg.color = new Color(0.2f, 0.22f, 0.28f, 1f);

            backButton = btnBackObj.AddComponent<Button>();
            ColorBlock cb = backButton.colors;
            cb.highlightedColor = new Color(0.3f, 0.35f, 0.45f, 1f);
            cb.pressedColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            backButton.colors = cb;

            GameObject bLabelObj = new GameObject("Label");
            bLabelObj.transform.SetParent(btnBackObj.transform, false);
            RectTransform blRT = bLabelObj.AddComponent<RectTransform>();
            blRT.anchorMin = Vector2.zero;
            blRT.anchorMax = Vector2.one;
            blRT.offsetMin = Vector2.zero;
            blRT.offsetMax = Vector2.zero;
            var blTxt = bLabelObj.AddComponent<TextMeshProUGUI>();
            blTxt.text = "< BACK";
            if (pixelFont != null) blTxt.font = pixelFont;
            blTxt.fontSize = 22f;
            blTxt.fontStyle = FontStyles.Bold;
            blTxt.alignment = TextAlignmentOptions.Center;
            blTxt.color = Color.white;

            WireHierarchyComponents(screenPanel.transform);
            screenPanel.SetActive(false);
        }

        private DifficultyCardItem CreateCardElement(string name, Transform parent, Vector2 pos, Sprite sprite, GameDifficulty diff, Color glowColor)
        {
            GameObject cardObj = new GameObject(name);
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(240f, 390f);

            // Highlight Glow (border around card)
            GameObject glowObj = new GameObject("Highlight_Glow");
            glowObj.transform.SetParent(cardObj.transform, false);
            RectTransform glowRT = glowObj.AddComponent<RectTransform>();
            glowRT.anchorMin = Vector2.zero;
            glowRT.anchorMax = Vector2.one;
            glowRT.offsetMin = new Vector2(-6f, -6f);
            glowRT.offsetMax = new Vector2(6f, 6f);
            var glowImg = glowObj.AddComponent<Image>();
            glowImg.color = glowColor;
            glowImg.raycastTarget = false;
            glowObj.SetActive(false);

            // Card Image
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = sprite;
            cardImg.raycastTarget = true;

            // Card Button
            var btn = cardObj.AddComponent<Button>();
            btn.targetGraphic = cardImg;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cb.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            btn.colors = cb;

            DifficultyCardItem item = cardObj.AddComponent<DifficultyCardItem>();
            SetPrivateField(item, "difficulty", diff);
            SetPrivateField(item, "cardImage", cardImg);
            SetPrivateField(item, "selectionGlow", glowObj);
            SetPrivateField(item, "actionButton", btn);

            return item;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        #endregion
    }
}
