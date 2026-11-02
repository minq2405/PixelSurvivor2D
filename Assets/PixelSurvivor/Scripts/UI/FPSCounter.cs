using UnityEngine;
using TMPro;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Component dedicated to measuring and displaying Frames Per Second (FPS) on a TMP_Text component.
    /// Uses Time.unscaledDeltaTime so that FPS measurements remain accurate regardless of Time.timeScale.
    /// Refreshes at a configurable interval (default: 0.4s) to eliminate visual flickering.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class FPSCounter : MonoBehaviour
    {
        [Header("Display Settings")]
        [Tooltip("Prefix string before the FPS number. E.g. 'FPS ' produces 'FPS 60'.")]
        [SerializeField] private string prefix = "FPS ";

        [Tooltip("Update frequency in seconds. Lower values refresh faster; higher values make numbers easier to read.")]
        [SerializeField] private float updateInterval = 0.4f;

        [Header("Target Component (Optional)")]
        [Tooltip("Target TMP_Text component. If unassigned, automatically fetches the TMP_Text on this GameObject.")]
        [SerializeField] private TMP_Text targetText;

        private float timeLeft;
        private float accumulator;
        private int frameCount;

        private void Awake()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }
            timeLeft = updateInterval;
        }

        private void Update()
        {
            timeLeft -= Time.unscaledDeltaTime;
            accumulator += Time.unscaledDeltaTime;
            frameCount++;

            if (timeLeft <= 0f)
            {
                int fps = accumulator > 0f ? Mathf.RoundToInt(frameCount / accumulator) : 0;
                if (targetText != null)
                {
                    targetText.text = $"{prefix}{fps}";
                }

                timeLeft = updateInterval;
                accumulator = 0f;
                frameCount = 0;
            }
        }
    }
}
