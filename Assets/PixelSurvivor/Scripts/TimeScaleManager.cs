using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelSurvivor
{
    /// <summary>
    /// Centralized, reference-counted TimeScale manager for Pixel Survivor (v2.1).
    /// Enforces the single-authority rule: only TimeScaleManager sets Time.timeScale.
    /// Tracks pause locks using pause sources (PauseToken pattern) so multiple UI modals
    /// (e.g. Pause Menu, Level Up, Game Over, Settings) do not prematurely unpause each other.
    /// </summary>
    public class TimeScaleManager : MonoBehaviour
    {
        public static TimeScaleManager Instance { get; private set; }

        private readonly HashSet<string> pauseSources = new HashSet<string>();
        [SerializeField] private float debugTimeScale = 1.0f;
        [SerializeField] private List<string> activePauseSourcesInspector = new List<string>();

        public bool IsPaused => pauseSources.Count > 0;
        public IReadOnlyCollection<string> ActivePauseSources => pauseSources;
        public float CurrentTimeScale => Time.timeScale;
        public float DebugTimeScale => debugTimeScale;

        public static event Action<bool> OnPauseStateChanged;
        public static event Action<float> OnTimeScaleChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject host = new GameObject("[TimeScaleManager]");
                Instance = host.AddComponent<TimeScaleManager>();
                DontDestroyOnLoad(host);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reset pause locks upon scene transition to ensure fresh gameplay state
            pauseSources.Clear();
            activePauseSourcesInspector.Clear();
            ApplyTimeScale();
            Debug.Log($"<color=#38BDF8>[TimeScaleManager]</color> Scene '{scene.name}' loaded. Reset pause locks, timeScale restored to {Time.timeScale:F1}.");
        }

        /// <summary>
        /// Request a pause token with an identified source name.
        /// As long as at least one source holds a pause token, Time.timeScale remains 0.
        /// </summary>
        public void AcquirePause(string source)
        {
            if (string.IsNullOrEmpty(source)) source = "UnknownSource";

            bool wasPaused = IsPaused;
            if (pauseSources.Add(source))
            {
                SyncInspectorList();
            }

            ApplyTimeScale();

            if (!wasPaused && IsPaused)
            {
                OnPauseStateChanged?.Invoke(true);
            }

            Debug.Log($"<color=#FACC15>[TimeScaleManager]</color> AcquirePause by '{source}'. Active locks ({pauseSources.Count}): [{string.Join(", ", pauseSources)}]");
        }

        /// <summary>
        /// Release the pause token held by the specified source.
        /// When all tokens are released, Time.timeScale restores to normal / debug speed.
        /// </summary>
        public void ReleasePause(string source)
        {
            if (string.IsNullOrEmpty(source)) source = "UnknownSource";

            bool wasPaused = IsPaused;
            if (pauseSources.Remove(source))
            {
                SyncInspectorList();
            }

            ApplyTimeScale();

            if (wasPaused && !IsPaused)
            {
                OnPauseStateChanged?.Invoke(false);
                Debug.Log($"<color=#10B981>[TimeScaleManager]</color> ReleasePause by '{source}'. All locks released! Resumed at timeScale = {Time.timeScale:F1}.");
            }
            else if (IsPaused)
            {
                Debug.Log($"<color=#F59E0B>[TimeScaleManager]</color> ReleasePause by '{source}', but still paused by ({pauseSources.Count}): [{string.Join(", ", pauseSources)}]");
            }
        }

        /// <summary>
        /// Clears all pause tokens unconditionally (e.g. restart run or return to main menu).
        /// </summary>
        public void ResetAllPauses()
        {
            bool wasPaused = IsPaused;
            pauseSources.Clear();
            SyncInspectorList();
            ApplyTimeScale();

            if (wasPaused)
            {
                OnPauseStateChanged?.Invoke(false);
            }
            Debug.Log("<color=#10B981>[TimeScaleManager]</color> ResetAllPauses called. Game unpaused.");
        }

        /// <summary>
        /// Adjust debug simulation speed multiplier (e.g. 1.0x, 2.0x, 5.0x for testing).
        /// If currently paused by any token, the new scale will take effect once unpaused.
        /// </summary>
        public void SetDebugTimeScale(float scale)
        {
            debugTimeScale = Mathf.Max(0f, scale);
            ApplyTimeScale();
            Debug.Log($"<color=#38BDF8>[TimeScaleManager]</color> DebugTimeScale set to {debugTimeScale:F1}x");
        }

        private void ApplyTimeScale()
        {
            float target = IsPaused ? 0f : debugTimeScale;
            Time.timeScale = target;
            OnTimeScaleChanged?.Invoke(target);
        }

        private void SyncInspectorList()
        {
            activePauseSourcesInspector.Clear();
            activePauseSourcesInspector.AddRange(pauseSources);
        }

        private void OnApplicationQuit()
        {
            Time.timeScale = 1.0f;
        }
    }
}
