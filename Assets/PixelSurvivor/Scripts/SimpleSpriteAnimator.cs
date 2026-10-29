using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor
{
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        [Header("Animation Frames")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private float framesPerSecond = 10f;

        [Header("Current State")]
        [SerializeField] private bool isRunning = false;
        [SerializeField] private bool useUnscaledTime = true;

        private SpriteRenderer spriteRenderer;
        private Image uiImage;
        private float timer = 0f;
        private int currentFrame = 0;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            uiImage = GetComponent<Image>();
        }

        private void Start()
        {
            UpdateSprite();
        }

        private void Update()
        {
            Sprite[] currentAnimation = isRunning && runFrames != null && runFrames.Length > 0 
                ? runFrames 
                : idleFrames;

            if (currentAnimation == null || currentAnimation.Length == 0) return;

            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer += delta;

            float frameDuration = 1f / Mathf.Max(framesPerSecond, 1f);
            if (timer >= frameDuration)
            {
                int steps = (int)(timer / frameDuration);
                timer -= steps * frameDuration;
                currentFrame = (currentFrame + steps) % currentAnimation.Length;
                UpdateSprite();
            }
        }

        public void SetRunning(bool running)
        {
            if (isRunning != running)
            {
                isRunning = running;
                currentFrame = 0;
                timer = 0f;
                UpdateSprite();
            }
        }

        public void SetFrames(Sprite[] newIdle, Sprite[] newRun, float fps = 10f)
        {
            idleFrames = newIdle;
            runFrames = newRun;
            framesPerSecond = fps;
            currentFrame = 0;
            timer = 0f;
            UpdateSprite();
        }

        public void SetFPS(float fps)
        {
            framesPerSecond = Mathf.Max(fps, 1f);
        }

        private void UpdateSprite()
        {
            Sprite[] currentAnimation = isRunning && runFrames != null && runFrames.Length > 0 
                ? runFrames 
                : idleFrames;

            if (currentAnimation == null || currentAnimation.Length == 0) return;

            currentFrame = Mathf.Clamp(currentFrame, 0, currentAnimation.Length - 1);
            Sprite s = currentAnimation[currentFrame];

            if (uiImage != null)
            {
                uiImage.sprite = s;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sprite = s;
            }
        }
    }
}
