using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PixelSurvivor.Mobile
{
    /// <summary>
    /// Floating virtual joystick for Android / iOS builds.
    /// - Auto-creates itself at runtime on mobile devices (no scene changes required).
    /// - Touch anywhere on the left part of the screen to spawn the joystick at the finger, drag to move.
    /// - Touches on UI buttons / cards / sliders are ignored so HUD & modals keep working.
    /// - Hidden while the game is paused (timeScale = 0) or when no run is active (Main Menu).
    /// PlayerController reads <see cref="CurrentInput"/> when there is no keyboard input.
    /// </summary>
    public class MobileJoystick : MonoBehaviour
    {
        public static MobileJoystick Instance { get; private set; }

        /// <summary>Analog movement vector (magnitude 0..1).</summary>
        public static Vector2 CurrentInput => Instance != null ? Instance.inputVector : Vector2.zero;

        [Header("Layout (reference resolution 1920x1080)")]
        [SerializeField] private float baseRadius = 130f;
        [SerializeField] private float knobRadius = 32f;
        [SerializeField] [Range(0.2f, 1f)] private float activeScreenWidthRatio = 0.6f;
        [SerializeField] private Vector2 idlePosition = new Vector2(260f, 260f);
        [SerializeField] [Range(0f, 0.5f)] private float deadZone = 0.12f;

        [Header("Behaviour")]
        [Tooltip("Show the idle joystick (faded) even when not touching.")]
        [SerializeField] private bool showWhenIdle = true;
        [Tooltip("Force the joystick to run in the Editor (for testing with mouse / Device Simulator).")]
        [SerializeField] private bool enableInEditor = false;

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform baseRect;
        private RectTransform knobRect;
        private Image baseImage;
        private Image knobImage;
        private CanvasGroup group;

        private Vector2 inputVector;
        private int activeTouchId = -1;
        private bool mouseDragging = false;
        private Vector2 originScreenPos;

        private static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        // ------------------------------------------------------------------
        // Auto bootstrap: on mobile platforms & Editor testing
        // ------------------------------------------------------------------
#if UNITY_EDITOR
        public const string EDITOR_PREF_KEY = "PixelSurvivor_EnableMobileJoystickInEditor";

        [UnityEditor.MenuItem("Tools/Pixel Survivor/Toggle Mobile Joystick in Editor")]
        public static void ToggleEditorJoystickMenu()
        {
            bool current = UnityEditor.EditorPrefs.GetBool(EDITOR_PREF_KEY, true);
            UnityEditor.EditorPrefs.SetBool(EDITOR_PREF_KEY, !current);
            Debug.Log($"<color=#38BDF8>[MobileJoystick]</color> Enable in Editor set to: {!current} (Take effect on next Play)");
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
#if UNITY_EDITOR
            if (!UnityEditor.EditorPrefs.GetBool(EDITOR_PREF_KEY, true)) return;
#else
            if (!Application.isMobilePlatform) return;
#endif

#if UNITY_EDITOR
            Application.targetFrameRate = -1; // Unlimited FPS in Editor
            QualitySettings.vSyncCount = 0;   // No VSync in Editor
#else
            QualitySettings.vSyncCount = 0;   // No VSync on APK/Mobile (prevents snapping to 30 FPS)
            Application.targetFrameRate = 90; // Target max 90 FPS (smooth 60 - 90 FPS range)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif

            var go = new GameObject("MobileJoystick");
            var joystick = go.AddComponent<MobileJoystick>();
#if UNITY_EDITOR
            joystick.enableInEditor = true;
#endif
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildVisuals();
            ResetStick();
        }

        private System.Collections.IEnumerator Start()
        {
            // Delay 2 frames to ensure Android graphics context and surface have settled
            yield return null;
            yield return null;
            QualitySettings.vSyncCount = 0;
#if UNITY_EDITOR
            Application.targetFrameRate = -1; // Unlimited in Editor
#else
            Application.targetFrameRate = 90; // Target 60 - 90 FPS on Android APK
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private bool IsEnabledOnThisPlatform => Application.isMobilePlatform || enableInEditor;

        // ------------------------------------------------------------------
        // Visuals (procedural circle sprites, no assets needed)
        // ------------------------------------------------------------------
        private void BuildVisuals()
        {
            var canvasGo = new GameObject("MobileJoystick_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);

            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40; // Below InGame_Canvas (50) so Pause / Level Up modals render on top

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1.0f;

            group = canvasGo.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            canvasRect = canvasGo.GetComponent<RectTransform>();

            Sprite baseSprite = CreateBaseSprite(256);
            Sprite knobSprite = CreateKnobSprite(128);

            var baseGo = new GameObject("Joystick_Base", typeof(RectTransform), typeof(Image));
            baseGo.transform.SetParent(canvasGo.transform, false);
            baseRect = baseGo.GetComponent<RectTransform>();
            baseRect.anchorMin = baseRect.anchorMax = Vector2.zero;
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.sizeDelta = Vector2.one * baseRadius * 2f;
            baseImage = baseGo.GetComponent<Image>();
            baseImage.sprite = baseSprite;
            baseImage.color = Color.white;
            baseImage.raycastTarget = false;

            var knobGo = new GameObject("Joystick_Knob", typeof(RectTransform), typeof(Image));
            knobGo.transform.SetParent(baseGo.transform, false);
            knobRect = knobGo.GetComponent<RectTransform>();
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = Vector2.one * knobRadius * 2f;
            knobImage = knobGo.GetComponent<Image>();
            knobImage.sprite = knobSprite;
            knobImage.color = Color.white;
            knobImage.raycastTarget = false;
        }

        private static Sprite CreateBaseSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float r = size * 0.5f;
            var pixels = new Color32[size * size];

            float borderThickness = 3.0f;
            float innerR = r - borderThickness;
            Color32 blackBase = new Color32(0, 0, 0, 36);        // ~14% alpha deep black
            Color32 grayBorder = new Color32(180, 188, 200, 85); // ~33% alpha slate gray
            Color32 transparent = new Color32(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > r + 1.0f)
                    {
                        pixels[y * size + x] = transparent;
                    }
                    else if (dist > r)
                    {
                        float aa = Mathf.Clamp01(1f - (dist - r));
                        pixels[y * size + x] = new Color32(grayBorder.r, grayBorder.g, grayBorder.b, (byte)(grayBorder.a * aa));
                    }
                    else if (dist >= innerR)
                    {
                        float t = (dist - innerR) / borderThickness;
                        byte cr = (byte)(blackBase.r + (grayBorder.r - blackBase.r) * t);
                        byte cg = (byte)(blackBase.g + (grayBorder.g - blackBase.g) * t);
                        byte cb = (byte)(blackBase.b + (grayBorder.b - blackBase.b) * t);
                        byte ca = (byte)(blackBase.a + (grayBorder.a - blackBase.a) * t);
                        pixels[y * size + x] = new Color32(cr, cg, cb, ca);
                    }
                    else
                    {
                        pixels[y * size + x] = blackBase;
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateKnobSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float r = size * 0.5f;
            var pixels = new Color32[size * size];

            float borderThickness = 2.0f;
            float innerR = r - borderThickness;
            Color32 knobGray = new Color32(160, 170, 185, 95);    // ~37% alpha slate gray
            Color32 knobBorder = new Color32(200, 210, 225, 130); // ~51% alpha border
            Color32 knobCore = new Color32(220, 228, 240, 140);   // ~55% alpha core
            Color32 transparent = new Color32(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > r + 1.0f)
                    {
                        pixels[y * size + x] = transparent;
                    }
                    else if (dist > r)
                    {
                        float aa = Mathf.Clamp01(1f - (dist - r));
                        pixels[y * size + x] = new Color32(knobBorder.r, knobBorder.g, knobBorder.b, (byte)(knobBorder.a * aa));
                    }
                    else if (dist >= innerR)
                    {
                        pixels[y * size + x] = knobBorder;
                    }
                    else
                    {
                        float t = dist / innerR;
                        byte cr = (byte)(knobCore.r + (knobGray.r - knobCore.r) * t);
                        byte cg = (byte)(knobCore.g + (knobGray.g - knobCore.g) * t);
                        byte cb = (byte)(knobCore.b + (knobGray.b - knobCore.b) * t);
                        byte ca = (byte)(knobCore.a + (knobGray.a - knobCore.a) * t);
                        pixels[y * size + x] = new Color32(cr, cg, cb, ca);
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ------------------------------------------------------------------
        // Update loop
        // ------------------------------------------------------------------
        private void Update()
        {
            bool gameplayActive = IsGameplayActive();
            group.alpha = (IsEnabledOnThisPlatform && gameplayActive && (showWhenIdle || IsDragging)) ? 1f : 0f;

            if (!IsEnabledOnThisPlatform)
            {
                ResetStick();
                return;
            }

            if (!gameplayActive)
            {
                // While paused / in modal (Level Up / Pause): stop movement output immediately
                inputVector = Vector2.zero;

                // If not in an active run (Main Menu / Hero Select), fully reset
                var spawner = PixelSurvivor.Combat.WaveSpawner.Instance;
                if (spawner == null || !spawner.IsRunActive)
                {
                    ResetStick();
                    return;
                }

                // If in active run but paused (Level Up / InGame Pause modal):
                // Only reset if the player voluntarily lifted their finger during pause
                if (Touchscreen.current != null && activeTouchId != -1)
                {
                    bool stillPressed = false;
                    var touches = Touchscreen.current.touches;
                    for (int i = 0; i < touches.Count; i++)
                    {
                        var t = touches[i];
                        if (t.touchId.ReadValue() == activeTouchId && t.press.isPressed)
                        {
                            stillPressed = true;
                            break;
                        }
                    }
                    if (!stillPressed)
                    {
                        ResetStick();
                    }
                }
                else if (mouseDragging && Mouse.current != null && !Mouse.current.leftButton.isPressed)
                {
                    ResetStick();
                }
                return;
            }

            if (Touchscreen.current != null)
            {
                HandleTouches();
            }
            else if (enableInEditor && Mouse.current != null)
            {
                HandleMouse();
            }
        }

        private bool IsDragging => activeTouchId != -1 || mouseDragging;

        private bool IsGameplayActive()
        {
            if (Time.timeScale <= 0.01f) return false; // Paused / Level Up / Game Over / Menu modal
            var spawner = PixelSurvivor.Combat.WaveSpawner.Instance;
            if (spawner != null && !spawner.IsRunActive) return false; // Main Menu / Hero Select
            return true;
        }

        private void HandleTouches()
        {
            var touches = Touchscreen.current.touches;

            // 1. Continue tracking active finger (preserved across pause or active drag)
            if (activeTouchId != -1)
            {
                bool found = false;
                for (int i = 0; i < touches.Count; i++)
                {
                    var t = touches[i];
                    if (t.touchId.ReadValue() != activeTouchId) continue;
                    if (t.press.isPressed)
                    {
                        found = true;
                        UpdateStick(t.position.ReadValue());
                    }
                    break;
                }
                if (!found) ResetStick();
                return;
            }

            // 2. Look for a new finger landing on the joystick area,
            // or re-acquire an existing finger held down on the left side when unpausing
            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                if (!t.press.isPressed) continue;
                Vector2 pos = t.position.ReadValue();
                if (!CanStartAt(pos)) continue;

                activeTouchId = t.touchId.ReadValue();
                BeginStick(pos);
                break;
            }
        }

        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (!mouseDragging && mouse.leftButton.isPressed)
            {
                Vector2 pos = mouse.position.ReadValue();
                if (CanStartAt(pos))
                {
                    mouseDragging = true;
                    BeginStick(pos);
                }
            }
            else if (mouseDragging)
            {
                if (mouse.leftButton.isPressed) UpdateStick(mouse.position.ReadValue());
                else ResetStick();
            }
        }

        private bool CanStartAt(Vector2 screenPos)
        {
            if (screenPos.x > Screen.width * activeScreenWidthRatio) return false;
            return !IsOverInteractiveUI(screenPos);
        }

        /// <summary>True if the screen point hits a Button / Slider / Toggle / clickable card.</summary>
        private static bool IsOverInteractiveUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = screenPos };
            raycastResults.Clear();
            EventSystem.current.RaycastAll(data, raycastResults);
            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject go = raycastResults[i].gameObject;
                if (go == null) continue;
                if (go.GetComponentInParent<Selectable>() != null) return true;
                if (go.GetComponentInParent<IPointerClickHandler>() != null) return true;
                if (go.GetComponentInParent<IDragHandler>() != null) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Stick math
        // ------------------------------------------------------------------
        private void BeginStick(Vector2 screenPos)
        {
            originScreenPos = screenPos;
            baseRect.anchoredPosition = ScreenToCanvas(screenPos);
            knobRect.anchoredPosition = Vector2.zero;
            baseImage.color = new Color(1.1f, 1.1f, 1.1f, 1.25f);
            knobImage.color = new Color(1.1f, 1.1f, 1.1f, 1.25f);
            inputVector = Vector2.zero;
        }

        private void UpdateStick(Vector2 screenPos)
        {
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            Vector2 delta = (screenPos - originScreenPos) / Mathf.Max(0.0001f, scale); // canvas units
            Vector2 clamped = Vector2.ClampMagnitude(delta, baseRadius);
            knobRect.anchoredPosition = clamped;

            Vector2 normalized = clamped / baseRadius; // 0..1
            float mag = normalized.magnitude;
            if (mag < deadZone)
            {
                inputVector = Vector2.zero;
            }
            else
            {
                // Full default speed immediately (no slow walking, consistent roguelite movement)
                inputVector = normalized.normalized;
            }
        }

        private void ResetStick()
        {
            activeTouchId = -1;
            mouseDragging = false;
            inputVector = Vector2.zero;
            if (baseRect != null)
            {
                baseRect.anchoredPosition = idlePosition;
                knobRect.anchoredPosition = Vector2.zero;
                baseImage.color = Color.white;
                knobImage.color = Color.white;
            }
        }

        private Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            return screenPos / Mathf.Max(0.0001f, scale);
        }
    }
}
