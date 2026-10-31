using System.Collections;
using UnityEngine;

namespace PixelSurvivor
{
    public class CameraFollow2D : MonoBehaviour
    {
        public static CameraFollow2D Instance { get; private set; }

        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        private Vector3 currentVelocity = Vector3.zero;
        private bool isLocked = false;
        private Vector3 lockedPosition;
        private Camera cam;
        private float defaultOrthoSize = 5f;
        private Coroutine punchZoomCoroutine;

        public bool IsLocked => isLocked;
        public float DefaultOrthoSize => defaultOrthoSize;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            cam = GetComponent<Camera>();
            if (cam != null)
            {
                defaultOrthoSize = cam.orthographicSize;
            }
        }

        private void Start()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (isLocked)
            {
                transform.position = Vector3.SmoothDamp(transform.position, lockedPosition, ref currentVelocity, 0.05f);
                return;
            }

            if (target == null) return;

            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        /// <summary>
        /// Lock camera viewport rigidly at specified position (used during Boss Arena lockdown).
        /// </summary>
        public void Lock(Vector2 position)
        {
            isLocked = true;
            lockedPosition = new Vector3(position.x, position.y, offset.z);
            currentVelocity = Vector3.zero;
        }

        /// <summary>
        /// Unlock camera and resume smooth following on player.
        /// </summary>
        public void Unlock()
        {
            isLocked = false;
            currentVelocity = Vector3.zero;

            if (punchZoomCoroutine != null)
            {
                StopCoroutine(punchZoomCoroutine);
                punchZoomCoroutine = null;
            }

            if (cam != null)
            {
                cam.orthographicSize = defaultOrthoSize;
            }
        }

        /// <summary>
        /// Performs a brief 0.35s punch zoom impact (5.0 -> punchSize -> 5.0) preserving tactical dodging space.
        /// </summary>
        public void DoPunchZoom(float punchSize = 4.4f, float duration = 0.35f)
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            if (punchZoomCoroutine != null) StopCoroutine(punchZoomCoroutine);
            punchZoomCoroutine = StartCoroutine(PunchZoomRoutine(punchSize, duration));
        }

        private IEnumerator PunchZoomRoutine(float punchSize, float duration)
        {
            float halfDuration = duration * 0.4f;
            float returnDuration = duration * 0.6f;

            // 1. Zoom In punch
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                if (cam != null)
                {
                    cam.orthographicSize = Mathf.Lerp(defaultOrthoSize, punchSize, elapsed / halfDuration);
                }
                yield return null;
            }

            // 2. Smooth return to default ortho size
            elapsed = 0f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                if (cam != null)
                {
                    cam.orthographicSize = Mathf.Lerp(punchSize, defaultOrthoSize, elapsed / returnDuration);
                }
                yield return null;
            }

            if (cam != null)
            {
                cam.orthographicSize = defaultOrthoSize;
            }
            punchZoomCoroutine = null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
