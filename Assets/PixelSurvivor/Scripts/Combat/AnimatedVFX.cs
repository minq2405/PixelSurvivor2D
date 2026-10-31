using System;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AnimatedVFX : MonoBehaviour
    {
        [Header("Animation Frames")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float fps = 18f;
        [SerializeField] private bool destroyOnComplete = true;
        [SerializeField] private bool loop = false;

        [Header("Render Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private float timer = 0f;
        private int currentFrame = 0;
        private Action onCompleteCallback;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(Sprite[] animFrames, float frameRate = 18f, Action onComplete = null, bool loopAnim = false)
        {
            frames = animFrames;
            fps = Mathf.Max(1f, frameRate);
            onCompleteCallback = onComplete;
            loop = loopAnim;
            currentFrame = 0;
            timer = 0f;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 25; // Renders above players and tiles

                // Assign unlit material so VFX glow brilliantly
                Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit") 
                                  ?? Shader.Find("Sprites/Default");
                if (unlitShader != null)
                {
                    spriteRenderer.material = new Material(unlitShader);
                }
            }

            if (frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;

            timer += Time.deltaTime;
            float frameInterval = 1f / fps;

            if (timer >= frameInterval)
            {
                timer -= frameInterval;
                currentFrame++;

                if (loop)
                {
                    currentFrame %= frames.Length;
                    spriteRenderer.sprite = frames[currentFrame];
                }
                else if (currentFrame < frames.Length)
                {
                    spriteRenderer.sprite = frames[currentFrame];
                }
                else
                {
                    onCompleteCallback?.Invoke();

                    if (destroyOnComplete)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
