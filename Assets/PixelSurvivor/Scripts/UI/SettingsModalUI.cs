using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.Audio;

namespace PixelSurvivor.UI
{
    public enum SettingsTab
    {
        Sound,
        Controls,
        About
    }

    /// <summary>
    /// Manages the Settings Modal in the Main Menu according to settings_menu_layout.json:
    /// - 3 tabs: Sound (default), Controls, About
    /// - Volume sliders for Music and Effects + Mute toggle
    /// - PC Keyboard controls summary
    /// - About & Credits info
    /// </summary>
    public class SettingsModalUI : MonoBehaviour
    {
        [Header("Header Elements")]
        [SerializeField] private Button btnClose;

        [Header("Tab Navigation Buttons")]
        [SerializeField] private Button tabSoundBtn;
        [SerializeField] private Button tabControlsBtn;
        [SerializeField] private Button tabAboutBtn;

        [Header("Tab Borders & Visuals")]
        [SerializeField] private RoundedBorderImage tabSoundBorder;
        [SerializeField] private RoundedBorderImage tabControlsBorder;
        [SerializeField] private RoundedBorderImage tabAboutBorder;

        [SerializeField] private TMP_Text tabSoundText;
        [SerializeField] private TMP_Text tabControlsText;
        [SerializeField] private TMP_Text tabAboutText;

        [Header("Tab Content Panels")]
        [SerializeField] private GameObject soundContentPanel;
        [SerializeField] private GameObject controlsContentPanel;
        [SerializeField] private GameObject aboutContentPanel;

        [Header("Sound Tab Controls")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider effectsSlider;
        [SerializeField] private TMP_Text musicPercentText;
        [SerializeField] private TMP_Text effectsPercentText;
        [SerializeField] private Button btnToggleMute;
        [SerializeField] private TMP_Text muteButtonText;

        [Header("Dynamic Box Sizing")]
        [SerializeField] private RectTransform settingsBoxRect;
        [SerializeField] private float soundTabHeight = 395f;
        [SerializeField] private float controlsTabHeight = 495f;
        [SerializeField] private float aboutTabHeight = 440f;

        [Header("Styling Colors")]
        private static readonly Color TabActiveBorder = new Color(0.980f, 0.800f, 0.082f, 1f); // #FACC15
        private static readonly Color TabActiveBg = new Color(0.094f, 0.204f, 0.471f, 1f);     // #183478
        private static readonly Color TabActiveText = Color.white;

        private static readonly Color TabInactiveBorder = new Color(0.169f, 0.290f, 0.573f, 1f); // #2B4A92
        private static readonly Color TabInactiveBg = new Color(0.075f, 0.153f, 0.361f, 1f);     // #13275C
        private static readonly Color TabInactiveText = new Color(0.820f, 0.847f, 0.878f, 1f);   // #D1D8E0

        private SettingsTab currentTab = SettingsTab.Sound;
        private bool isMuted = false;

        private const string PREF_MUSIC_VOL = "Settings_MusicVolume";
        private const string PREF_SFX_VOL = "Settings_SFXVolume";
        private const string PREF_MUTED = "Settings_IsMuted";

        private void Awake()
        {
            // Wire button listeners
            if (btnClose != null) btnClose.onClick.AddListener(CloseModal);

            if (tabSoundBtn != null) tabSoundBtn.onClick.AddListener(() => SelectTab(SettingsTab.Sound));
            if (tabControlsBtn != null) tabControlsBtn.onClick.AddListener(() => SelectTab(SettingsTab.Controls));
            if (tabAboutBtn != null) tabAboutBtn.onClick.AddListener(() => SelectTab(SettingsTab.About));

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
                if (musicPercentText == null)
                    musicPercentText = musicSlider.transform.parent.Find("Percent")?.GetComponent<TMP_Text>();
            }

            if (effectsSlider != null)
            {
                effectsSlider.onValueChanged.AddListener(OnEffectsSliderChanged);
                if (effectsPercentText == null)
                    effectsPercentText = effectsSlider.transform.parent.Find("Percent")?.GetComponent<TMP_Text>();
            }

            if (btnToggleMute != null) btnToggleMute.onClick.AddListener(ToggleMute);
        }

        private void OnEnable()
        {
            // Load saved settings
            LoadSettings();

            // Always default to Sound tab as specified
            SelectTab(SettingsTab.Sound);
        }

        public void SelectTab(SettingsTab tab)
        {
            AudioManager.Instance?.PlayUiClick();
            currentTab = tab;

            // Toggle Content Panels
            if (soundContentPanel != null) soundContentPanel.SetActive(tab == SettingsTab.Sound);
            if (controlsContentPanel != null) controlsContentPanel.SetActive(tab == SettingsTab.Controls);
            if (aboutContentPanel != null) aboutContentPanel.SetActive(tab == SettingsTab.About);

            // Update Tab Button Visuals
            UpdateTabVisual(tabSoundBorder, tabSoundText, tab == SettingsTab.Sound);
            UpdateTabVisual(tabControlsBorder, tabControlsText, tab == SettingsTab.Controls);
            UpdateTabVisual(tabAboutBorder, tabAboutText, tab == SettingsTab.About);

            // Dynamic Box Height based on Tab Content
            UpdateBoxHeight(tab);
        }

        private void UpdateBoxHeight(SettingsTab tab)
        {
            if (settingsBoxRect == null)
            {
                settingsBoxRect = transform.Find("SettingsBox") as RectTransform;
            }
            if (settingsBoxRect == null) return;

            float targetHeight;
            switch (tab)
            {
                case SettingsTab.Sound:
                    targetHeight = soundTabHeight > 0f ? soundTabHeight : CalculatePanelHeight(soundContentPanel);
                    break;
                case SettingsTab.Controls:
                    targetHeight = controlsTabHeight > 0f ? controlsTabHeight : CalculatePanelHeight(controlsContentPanel);
                    break;
                case SettingsTab.About:
                    targetHeight = aboutTabHeight > 0f ? aboutTabHeight : CalculatePanelHeight(aboutContentPanel);
                    break;
                default:
                    targetHeight = 520f;
                    break;
            }

            settingsBoxRect.sizeDelta = new Vector2(settingsBoxRect.sizeDelta.x, targetHeight);
        }

        private float CalculatePanelHeight(GameObject panel)
        {
            if (panel == null) return 520f;
            float maxBottom = 0f;
            foreach (Transform child in panel.transform)
            {
                RectTransform rt = child as RectTransform;
                if (rt == null || !child.gameObject.activeSelf) continue;
                float bottomEdge = Mathf.Abs(rt.anchoredPosition.y) + rt.rect.height * rt.pivot.y;
                if (bottomEdge > maxBottom) maxBottom = bottomEdge;
            }
            return Mathf.Max(350f, maxBottom + 131f + 25f);
        }

        private void UpdateTabVisual(RoundedBorderImage border, TMP_Text text, bool isActive)
        {
            if (border != null)
            {
                border.BorderColor = isActive ? TabActiveBorder : TabInactiveBorder;
                border.FillColor = isActive ? TabActiveBg : TabInactiveBg;
                border.BorderThickness = isActive ? 3.5f : 2f;
            }

            if (text != null)
            {
                text.color = isActive ? TabActiveText : TabInactiveText;
                text.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        private void LoadSettings()
        {
            float musicVol = AudioManager.Instance != null
                ? AudioManager.Instance.MusicVolume
                : PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.7f);

            float sfxVol = AudioManager.Instance != null
                ? AudioManager.Instance.SFXVolume
                : PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.8f);

            isMuted = AudioManager.Instance != null
                ? AudioManager.Instance.IsMuted
                : (PlayerPrefs.GetInt(PREF_MUTED, 0) == 1 || PlayerPrefs.GetInt("Settings_MuteAll", 0) == 1);

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(musicVol);
                UpdatePercentText(musicPercentText, musicVol);
            }

            if (effectsSlider != null)
            {
                effectsSlider.SetValueWithoutNotify(sfxVol);
                UpdatePercentText(effectsPercentText, sfxVol);
            }

            UpdateMuteVisual();
            ApplyAudioVolume();
        }

        private void OnMusicSliderChanged(float val)
        {
            PlayerPrefs.SetFloat(PREF_MUSIC_VOL, val);
            PlayerPrefs.Save();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(val);
            }

            UpdatePercentText(musicPercentText, val);
            ApplyAudioVolume();
        }

        private void OnEffectsSliderChanged(float val)
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOL, val);
            PlayerPrefs.Save();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(val);
            }

            UpdatePercentText(effectsPercentText, val);
            ApplyAudioVolume();
        }

        private void ToggleMute()
        {
            AudioManager.Instance?.PlayUiClick();
            isMuted = !isMuted;
            PlayerPrefs.SetInt(PREF_MUTED, isMuted ? 1 : 0);
            PlayerPrefs.SetInt("Settings_MuteAll", isMuted ? 1 : 0);
            PlayerPrefs.Save();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMuted(isMuted);
            }

            UpdateMuteVisual();
            ApplyAudioVolume();
        }

        private void UpdateMuteVisual()
        {
            if (muteButtonText != null)
            {
                muteButtonText.text = isMuted ? "Unmute" : "Mute";
            }
        }

        private void ApplyAudioVolume()
        {
            float masterVol = isMuted ? 0f : 1f;
            AudioListener.volume = masterVol;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ApplyVolumeSettings();
            }
        }

        private void UpdatePercentText(TMP_Text textComp, float value)
        {
            if (textComp != null)
            {
                textComp.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
            }
        }

        public void CloseModal()
        {
            AudioManager.Instance?.PlayUiClick();
            gameObject.SetActive(false);
        }
    }
}
