using UnityEngine;
using UnityEngine.InputSystem;
using PixelSurvivor.Combat;

namespace PixelSurvivor
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Character Identity")]
        public string characterId = "Steve";
        public string characterName = "Steve";

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 4f;

        public float MoveSpeed => moveSpeed;
        public Vector2 FacingDirection { get; private set; } = Vector2.right;
        public Vector2 MoveInput => moveInput;

        [Header("Component References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private SimpleSpriteAnimator spriteAnimator;
        [SerializeField] private Rigidbody2D rb;

        private Vector2 moveInput;
        private bool isMovementLocked = false;
        private float slowMultiplier = 1f;
        private Coroutine slowCoroutine;

        private void Awake()
        {
            // Time.timeScale is strictly managed by TimeScaleManager (v2.1)

            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteAnimator == null) spriteAnimator = GetComponent<SimpleSpriteAnimator>();

            // Setup 2D Top-Down Rigidbody settings
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (GetComponent<PixelSurvivor.Combat.PassiveController>() == null)
            {
                gameObject.AddComponent<PixelSurvivor.Combat.PassiveController>();
            }
        }

        private void Start()
        {
            if (PlayerStats.Instance != null)
            {
                moveSpeed = PlayerStats.Instance.EffectiveMoveSpeed;
            }
        }

        private void Update()
        {
            if (isMovementLocked)
            {
                moveInput = Vector2.zero;
                return;
            }

            Vector2 input = Vector2.zero;

            // 1. Unity New Input System Keyboard read
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
            }

            // 2. Fallback to Unity Legacy Input System (Axes & WASD/Arrows)
            if (input.sqrMagnitude < 0.01f)
            {
                try
                {
                    float h = Input.GetAxisRaw("Horizontal");
                    float v = Input.GetAxisRaw("Vertical");
                    if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
                    {
                        input.x = h;
                        input.y = v;
                    }
                }
                catch
                {
                    // Handled safely if legacy input is completely disabled in project settings
                }
            }

            moveInput = input.normalized;
            if (moveInput.sqrMagnitude > 0.01f)
            {
                FacingDirection = moveInput;
            }

            // Handle horizontal flip
            if (moveInput.x > 0.05f && spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
            }
            else if (moveInput.x < -0.05f && spriteRenderer != null)
            {
                spriteRenderer.flipX = true;
            }

            // Update animations
            bool isMoving = moveInput.sqrMagnitude > 0.01f;
            if (spriteAnimator != null && spriteAnimator.enabled)
            {
                spriteAnimator.SetRunning(isMoving);
            }
            else if (animator != null && animator.enabled)
            {
                animator.SetFloat("Speed", isMoving ? 1.0f : 0.0f);
            }
        }

        private void FixedUpdate()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;

            // Unity 6 Physics 2D API: rb.linearVelocity replaces deprecated rb.velocity
            rb.linearVelocity = isMovementLocked ? Vector2.zero : moveInput * (moveSpeed * slowMultiplier);
        }

        public void ApplySlow(float multiplier, float duration)
        {
            if (slowCoroutine != null)
            {
                StopCoroutine(slowCoroutine);
            }
            slowCoroutine = StartCoroutine(SlowRoutine(multiplier, duration));
        }

        private System.Collections.IEnumerator SlowRoutine(float multiplier, float duration)
        {
            slowMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.45f, 0.85f, 1.0f, 1.0f); // Frosted ice tint
            }

            yield return new WaitForSeconds(duration);

            slowMultiplier = 1f;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            slowCoroutine = null;
        }

        public void SetMovementLocked(bool locked)
        {
            isMovementLocked = locked;
            if (locked && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        public void SetMoveSpeedFromStats(float speed)
        {
            moveSpeed = speed;
        }

        public void ApplySpeedBuff(float percentage)
        {
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddBonusMoveSpeed(percentage);
            }
            else
            {
                moveSpeed *= (1f + percentage);
                Debug.Log($"<color=#10B981>[Buff]</color> Player base move speed increased to: {moveSpeed:F2} m/s");
            }
        }

        public void ConfigureCharacter(string id, RuntimeAnimatorController controller, Sprite[] idle, Sprite[] run, float speed)
        {
            characterId = id;
            characterName = id;
            moveSpeed = speed;

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.ApplyCharacterProfile(id);
                moveSpeed = PlayerStats.Instance.EffectiveMoveSpeed;
            }

            if (spriteAnimator != null && idle != null && run != null)
            {
                // Disable conflicting Mecanim Animator if SimpleSpriteAnimator is active
                if (animator != null)
                {
                    animator.enabled = false;
                }
                spriteAnimator.enabled = true;
                spriteAnimator.SetFrames(idle, run, 10f);
            }
            else if (controller != null && animator != null)
            {
                animator.enabled = true;
                animator.runtimeAnimatorController = controller;
            }
        }
    }
}
