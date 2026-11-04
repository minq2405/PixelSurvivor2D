using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Audio
{
    public enum WeaponSlashArchetype
    {
        Broadsword,  // Standard 1.0f pitch
        TwinDaggers, // 1.35f pitch (rapid, sharp)
        BattleAxe,   // 0.75f pitch (heavy, deep thud)
        DarkScythe   // 0.85f pitch (dark sweep)
    }

    public enum BossRoarType
    {
        NetherGolem, // 1.0f pitch
        Warden       // 0.7f pitch (deep, ominous terror)
    }

    /// <summary>
    /// Centralized Lightweight Audio Manager:
    /// - Streamlined 18 SFX + 3 BGM architecture with dynamic pitch modulation
    /// - Dynamic ascending pitch for EXP pickup chains
    /// - Pitch presets for shared weapon slashes and boss roars
    /// - AudioSource pooling for simultaneous overlapping sound effects
    /// - PlayerPrefs volume integration (Settings_MusicVolume, Settings_SFXVolume, Settings_MuteAll)
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string PREF_MUSIC_VOL = "Settings_MusicVolume";
        private const string PREF_SFX_VOL = "Settings_SFXVolume";
        private const string PREF_MUTE = "Settings_MuteAll";

        [Header("Audio Sources & Pooling")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private int sfxPoolSize = 16;

        [Header("1. Weapon SFX (Shared Core)")]
        [Tooltip("Shared melee slash SFX (Daggers, Sword, Axe, Scythe)")]
        [SerializeField] private AudioClip sfxSlash;
        [Tooltip("Shared ranged shooting SFX (Crossbow, Gatling, Throwables)")]
        [SerializeField] private AudioClip sfxShoot;
        [Tooltip("Shared energy/magic beam SFX (Crystal Laser, Oblivion Death Ray)")]
        [SerializeField] private AudioClip sfxLaser;
        [Tooltip("Heavy shockwave/explosion SFX (Solar Nova, Earthquake, Execute)")]
        [SerializeField] private AudioClip sfxHeavyImpact;

        [Header("2. Combat & Enemy SFX (Shared Core)")]
        [Tooltip("Generic hit impact for all enemies")]
        [SerializeField] private AudioClip sfxHit;
        [Tooltip("Generic poof/death sound for all common monsters")]
        [SerializeField] private AudioClip sfxEnemyDeath;
        [Tooltip("Generic explosion for Creeper, TNT, Magma hazards")]
        [SerializeField] private AudioClip sfxExplosion;

        [Header("3. Player SFX")]
        [SerializeField] private AudioClip sfxPlayerHurt;
        [SerializeField] private AudioClip sfxPlayerDeath;
        [SerializeField] private AudioClip sfxRevive;

        [Header("4. Drops & Economy SFX")]
        [Tooltip("EXP gem pickup with auto ascending pitch modulation")]
        [SerializeField] private AudioClip sfxExpPickup;
        [Tooltip("Emerald gem, Cooked Steak, Magnet pickup sound")]
        [SerializeField] private AudioClip sfxItemPickup;
        [Tooltip("Lucky Block & chest damage/break sound")]
        [SerializeField] private AudioClip sfxChestHit;

        [Header("5. Boss SFX")]
        [Tooltip("Shared roar for Golem (pitch 1.0) and Warden (pitch 0.7)")]
        [SerializeField] private AudioClip sfxBossRoar;
        [Tooltip("Heavy slam/punch for Golem & Warden")]
        [SerializeField] private AudioClip sfxBossSlam;
        [Tooltip("Arena lockdown siren & Warden heartbeat countdown")]
        [SerializeField] private AudioClip sfxBossAlarm;

        [Header("6. UI SFX")]
        [SerializeField] private AudioClip sfxUiClick;
        [SerializeField] private AudioClip sfxUiLevelUp;
        [SerializeField] private AudioClip sfxWin;

        [Header("7. BGM Clips")]
        [SerializeField] private AudioClip bgmMenu;
        [SerializeField] private AudioClip bgmBattle;
        [SerializeField] private AudioClip bgmBoss;

        // Runtime pooling & states
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private int currentPoolIndex = 0;

        // EXP dynamic ascending pitch state
        private float currentExpPitch = 1.0f;
        private float lastExpPickupTime = -1f;
        private const float EXP_PITCH_RESET_DELAY = 0.7f;
        private const float EXP_PITCH_STEP = 0.05f;
        private const float MAX_EXP_PITCH = 1.6f;

        // Volume caches
        public float MusicVolume { get; private set; } = 0.7f;
        public float SFXVolume { get; private set; } = 0.8f;
        public bool IsMuted { get; private set; } = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureAudioManagerExists()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("AudioManager");
                Instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            AutoLoadMissingClipsFromResources();
            LoadVolumeSettings();
        }

        private void AutoLoadMissingClipsFromResources()
        {
            sfxSlash ??= Resources.Load<AudioClip>("Audio/SFX/sfx_slash");
            sfxShoot ??= Resources.Load<AudioClip>("Audio/SFX/sfx_shoot");
            sfxLaser ??= Resources.Load<AudioClip>("Audio/SFX/sfx_laser");
            sfxHeavyImpact ??= Resources.Load<AudioClip>("Audio/SFX/sfx_heavy_impact");
            sfxHit ??= Resources.Load<AudioClip>("Audio/SFX/sfx_hit");
            sfxEnemyDeath ??= Resources.Load<AudioClip>("Audio/SFX/sfx_enemy_death");
            sfxExplosion ??= Resources.Load<AudioClip>("Audio/SFX/sfx_heavy_impact");
            sfxPlayerHurt ??= Resources.Load<AudioClip>("Audio/SFX/sfx_hit");
            sfxPlayerDeath ??= Resources.Load<AudioClip>("Audio/SFX/sfx_player_death");
            sfxRevive ??= Resources.Load<AudioClip>("Audio/SFX/sfx_ui_levelup");
            sfxExpPickup ??= Resources.Load<AudioClip>("Audio/SFX/sfx_exp_pickup");
            sfxItemPickup ??= Resources.Load<AudioClip>("Audio/SFX/sfx_item_pickup");
            sfxChestHit ??= Resources.Load<AudioClip>("Audio/SFX/sfx_hit");
            sfxBossAlarm ??= Resources.Load<AudioClip>("Audio/SFX/sfx_boss_alarm");
            sfxUiClick ??= Resources.Load<AudioClip>("Audio/SFX/sfx_ui_click");
            sfxUiLevelUp ??= Resources.Load<AudioClip>("Audio/SFX/sfx_ui_levelup");
            sfxWin ??= Resources.Load<AudioClip>("Audio/SFX/sfx_win");
            bgmMenu ??= Resources.Load<AudioClip>("Audio/BGM/bgm_menu");
            bgmBattle ??= Resources.Load<AudioClip>("Audio/BGM/bgm_battle");
            bgmBoss ??= Resources.Load<AudioClip>("Audio/BGM/bgm_battle");
        }

        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            // Pool SFX sources
            sfxPool.Clear();
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject poolObj = new GameObject($"SFX_Source_{i}");
                poolObj.transform.SetParent(transform);
                AudioSource src = poolObj.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f; // 2D clean sound
                sfxPool.Add(src);
            }
        }

        public void LoadVolumeSettings()
        {
            MusicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.7f);
            SFXVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.8f);
            IsMuted = PlayerPrefs.GetInt(PREF_MUTE, 0) == 1;
            ApplyVolumeSettings();
        }

        public void ApplyVolumeSettings()
        {
            if (musicSource != null)
            {
                musicSource.volume = IsMuted ? 0f : MusicVolume;
            }
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREF_MUSIC_VOL, MusicVolume);
            ApplyVolumeSettings();
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREF_SFX_VOL, SFXVolume);
            ApplyVolumeSettings();
        }

        public void SetMuted(bool mute)
        {
            IsMuted = mute;
            PlayerPrefs.SetInt(PREF_MUTE, mute ? 1 : 0);
            ApplyVolumeSettings();
        }

        #region Core SFX Playback

        public void PlaySFXCustom(AudioClip clip, float pitch = 1.0f, float volumeMultiplier = 1.0f)
        {
            if (clip == null || IsMuted || sfxPool.Count == 0) return;

            AudioSource src = GetNextAvailableSource();
            if (src != null)
            {
                src.pitch = Mathf.Clamp(pitch, 0.2f, 3.0f);
                src.volume = Mathf.Clamp01(SFXVolume * volumeMultiplier);
                src.clip = clip;
                src.Play();
            }
        }

        private AudioSource GetNextAvailableSource()
        {
            // First check if an idle source exists
            for (int i = 0; i < sfxPool.Count; i++)
            {
                int index = (currentPoolIndex + i) % sfxPool.Count;
                if (!sfxPool[index].isPlaying)
                {
                    currentPoolIndex = (index + 1) % sfxPool.Count;
                    return sfxPool[index];
                }
            }

            // If all busy, round-robin overwrite
            AudioSource overwritten = sfxPool[currentPoolIndex];
            currentPoolIndex = (currentPoolIndex + 1) % sfxPool.Count;
            return overwritten;
        }

        #endregion

        #region 1. Weapon SFX Helpers

        /// <summary>
        /// Plays shared slash SFX with weapon-specific pitch.
        /// </summary>
        public void PlaySlash(WeaponSlashArchetype archetype)
        {
            if (sfxSlash == null) return;

            float pitch = 1.0f;
            switch (archetype)
            {
                case WeaponSlashArchetype.TwinDaggers: pitch = 1.35f; break;
                case WeaponSlashArchetype.BattleAxe:   pitch = 0.75f; break;
                case WeaponSlashArchetype.DarkScythe:  pitch = 0.85f; break;
                case WeaponSlashArchetype.Broadsword:
                default:                              pitch = 1.00f; break;
            }

            pitch += UnityEngine.Random.Range(-0.04f, 0.04f);
            PlaySFXCustom(sfxSlash, pitch, 1.0f);
        }

        public void PlayShoot(bool isRapid = false)
        {
            if (sfxShoot == null) return;
            float pitch = isRapid ? UnityEngine.Random.Range(0.95f, 1.08f) : 1.0f;
            PlaySFXCustom(sfxShoot, pitch, 0.9f);
        }

        public void PlayLaser()
        {
            if (sfxLaser == null) return;
            PlaySFXCustom(sfxLaser, 1.0f, 0.85f);
        }

        public void PlayHeavyImpact()
        {
            if (sfxHeavyImpact == null) return;
            float pitch = UnityEngine.Random.Range(0.85f, 1.02f);
            PlaySFXCustom(sfxHeavyImpact, pitch, 1.15f);
        }

        #endregion

        #region 2. Combat & Enemy Helpers

        public void PlayHit(bool isCrit = false)
        {
            if (sfxHit == null) return;
            float pitch = UnityEngine.Random.Range(0.92f, 1.15f);
            float volume = isCrit ? 1.25f : 0.85f;
            if (isCrit) pitch *= 1.1f;
            PlaySFXCustom(sfxHit, pitch, volume);
        }

        public void PlayEnemyDeath()
        {
            if (sfxEnemyDeath == null) return;
            float pitch = UnityEngine.Random.Range(0.95f, 1.1f);
            PlaySFXCustom(sfxEnemyDeath, pitch, 0.8f);
        }

        public void PlayExplosion()
        {
            if (sfxExplosion == null) return;
            PlaySFXCustom(sfxExplosion, UnityEngine.Random.Range(0.95f, 1.05f), 1.2f);
        }

        #endregion

        #region 3. Player SFX Helpers

        public void PlayPlayerHurt()
        {
            if (sfxPlayerHurt != null)
            {
                PlaySFXCustom(sfxPlayerHurt, UnityEngine.Random.Range(0.95f, 1.05f), 1.1f);
            }
            else if (sfxHit != null)
            {
                // Smart fallback: Re-use sfxHit with lower pitch for player damage
                PlaySFXCustom(sfxHit, 0.88f, 1.15f);
            }
        }

        public void PlayPlayerDeath()
        {
            if (sfxPlayerDeath == null) return;
            PlaySFXCustom(sfxPlayerDeath, 1.0f, 1.2f);
        }

        public void PlayRevive()
        {
            if (sfxRevive != null)
            {
                PlaySFXCustom(sfxRevive, 1.0f, 1.25f);
            }
            else if (sfxUiLevelUp != null)
            {
                // Smart fallback: Re-use level up jingle for revival
                PlaySFXCustom(sfxUiLevelUp, 0.95f, 1.2f);
            }
        }

        #endregion

        #region 4. Drops & Economy Helpers

        /// <summary>
        /// Single EXP file with dynamic rising pitch (1.0 -> 1.05 -> 1.10 ... 1.60)
        /// resets to 1.0 after 0.7s of no gems collected.
        /// </summary>
        public void PlayExpPickup()
        {
            if (sfxExpPickup == null) return;

            if (Time.time - lastExpPickupTime > EXP_PITCH_RESET_DELAY)
            {
                currentExpPitch = 1.0f;
            }
            else
            {
                currentExpPitch = Mathf.Min(currentExpPitch + EXP_PITCH_STEP, MAX_EXP_PITCH);
            }
            lastExpPickupTime = Time.time;

            PlaySFXCustom(sfxExpPickup, currentExpPitch, 0.9f);
        }

        public void PlayItemPickup()
        {
            if (sfxItemPickup == null) return;
            PlaySFXCustom(sfxItemPickup, UnityEngine.Random.Range(0.98f, 1.05f), 1.0f);
        }

        public void PlayChestHit()
        {
            if (sfxChestHit != null)
            {
                PlaySFXCustom(sfxChestHit, UnityEngine.Random.Range(0.95f, 1.1f), 1.0f);
            }
            else if (sfxHit != null)
            {
                // Smart fallback: Re-use sfxHit with 0.80 pitch (heavy solid thud)
                PlaySFXCustom(sfxHit, UnityEngine.Random.Range(0.78f, 0.85f), 1.05f);
            }
        }

        #endregion

        #region 5. Boss SFX Helpers

        public void PlayBossRoar(BossRoarType type)
        {
            if (sfxBossRoar != null)
            {
                float pitch = (type == BossRoarType.Warden) ? 0.70f : 1.00f;
                PlaySFXCustom(sfxBossRoar, pitch, 1.3f);
            }
            else
            {
                // Smart fallback: Ground shaking heavy impact when Boss emerges
                PlayHeavyImpact();
            }
        }

        public void PlayBossSlam()
        {
            if (sfxBossSlam != null)
            {
                PlaySFXCustom(sfxBossSlam, UnityEngine.Random.Range(0.85f, 1.0f), 1.2f);
            }
            else
            {
                // Smart fallback: Re-use sfxHeavyImpact with lower pitch
                if (sfxHeavyImpact != null)
                {
                    PlaySFXCustom(sfxHeavyImpact, UnityEngine.Random.Range(0.75f, 0.88f), 1.25f);
                }
            }
        }

        public void PlayBossAlarm()
        {
            if (sfxBossAlarm == null) return;
            PlaySFXCustom(sfxBossAlarm, 1.0f, 1.1f);
        }

        #endregion

        #region 6. UI SFX Helpers

        private readonly HashSet<Button> boundButtons = new HashSet<Button>();
        private float lastClickSoundTime = -1f;
        private float buttonScanTimer = 0f;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            boundButtons.Clear();
            BindAllSceneButtons();
        }

        private void Update()
        {
            buttonScanTimer += Time.unscaledDeltaTime;
            if (buttonScanTimer >= 0.25f)
            {
                buttonScanTimer = 0f;
                BindAllSceneButtons();
            }
        }

        /// <summary>
        /// Automatically hooks all active and inactive UI Buttons across the entire scene,
        /// ensuring every button click uniformly triggers PlayUiClick().
        /// </summary>
        public void BindAllSceneButtons()
        {
            Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < allButtons.Length; i++)
            {
                Button btn = allButtons[i];
                if (btn == null) continue;
                if (!btn.gameObject.scene.isLoaded) continue;

                if (boundButtons.Add(btn))
                {
                    btn.onClick.AddListener(PlayUiClick);
                }
            }
        }

        public void PlayUiClick()
        {
            if (sfxUiClick == null) return;
            // Debounce: prevent duplicate audio if both button listener and code call PlayUiClick in same 50ms
            if (Time.unscaledTime - lastClickSoundTime < 0.05f) return;
            lastClickSoundTime = Time.unscaledTime;

            PlaySFXCustom(sfxUiClick, UnityEngine.Random.Range(0.98f, 1.02f), 1.0f);
        }

        public void PlayUiLevelUp()
        {
            if (sfxUiLevelUp == null) return;
            PlaySFXCustom(sfxUiLevelUp, 1.0f, 1.15f);
        }

        public void PlayWin()
        {
            if (sfxWin == null) return;
            PlaySFXCustom(sfxWin, 1.0f, 1.25f);
        }

        #endregion

        #region 7. BGM Playback

        public void PlayMenuMusic()
        {
            PlayBGM(bgmMenu);
        }

        public void PlayBattleMusic()
        {
            PlayBGM(bgmBattle);
        }

        public void PlayBossMusic()
        {
            PlayBGM(bgmBoss);
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.volume = IsMuted ? 0f : MusicVolume;
            musicSource.Play();
        }

        public void StopBGM()
        {
            if (musicSource != null) musicSource.Stop();
        }

        #endregion
    }
}
