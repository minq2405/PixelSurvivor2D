using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Lightweight frame-by-frame walk animator for enemies.
    /// Uses sliced spritesheets (e.g. zombie_walk, creeper_walk, spider_walk).
    /// Respects Time.timeScale so it automatically pauses when the game pauses.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyAnimation : MonoBehaviour
    {
        [Header("Frames & Timing")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private float framesPerSecond = 10f;
        [SerializeField] private bool autoFlipX = true;

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private EnemyHealth health;
        private float frameTimer = 0f;
        private int currentFrame = 0;

        public Sprite[] WalkFrames => walkFrames;
        public float FPS => framesPerSecond;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<EnemyHealth>();
        }

        private void Start()
        {
            if (walkFrames != null && walkFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = walkFrames[0];
            }
        }

        public void SetWalkFrames(Sprite[] frames, float fps = 10f)
        {
            walkFrames = frames;
            framesPerSecond = Mathf.Max(fps, 1f);
            currentFrame = 0;
            frameTimer = 0f;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (walkFrames != null && walkFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = walkFrames[0];
            }
        }

        private void Update()
        {
            if (walkFrames == null || walkFrames.Length == 0) return;
            if (health != null && !health.IsAlive) return;

            // Frame animation ticking
            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(framesPerSecond, 1f);

            if (frameTimer >= frameDuration)
            {
                int steps = (int)(frameTimer / frameDuration);
                frameTimer -= steps * frameDuration;
                currentFrame = (currentFrame + steps) % walkFrames.Length;

                if (spriteRenderer != null && currentFrame < walkFrames.Length)
                {
                    spriteRenderer.sprite = walkFrames[currentFrame];
                }
            }

            // Directional flip (only if EnemyAI is not already managing flip)
            if (autoFlipX && rb != null && spriteRenderer != null && GetComponent<EnemyAI>() == null)
            {
                if (spriteRenderer.flipX && rb.linearVelocity.x > 0.25f)
                {
                    spriteRenderer.flipX = false;
                }
                else if (!spriteRenderer.flipX && rb.linearVelocity.x < -0.25f)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }
    }
}
