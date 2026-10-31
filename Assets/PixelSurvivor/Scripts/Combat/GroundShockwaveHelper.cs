using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    /// <summary>
    /// Shared utility for Ground Earthquake / Tremor Shockwave:
    /// - Spawns a glowing Red Telegraph Oval warning zone before impact.
    /// - Spawns the 8-frame vfx_dia_chan animated shockwave.
    /// - Performs accurate ellipse damage detection on the Player.
    /// </summary>
    public static class GroundShockwaveHelper
    {
        private static Sprite[] diaChanFrames;
        private static Sprite softCircleSprite;

        public static Sprite[] GetDiaChanFrames()
        {
            if (diaChanFrames != null && diaChanFrames.Length > 0) return diaChanFrames;

            diaChanFrames = Resources.LoadAll<Sprite>("VFX/vfx_dia_chan");
#if UNITY_EDITOR
            if (diaChanFrames == null || diaChanFrames.Length == 0)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PixelSurvivor/Particles/VFX/vfx_dia_chan.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in loaded) if (o is Sprite s) list.Add(s);
                // Sort by frame index
                list.Sort((a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));
                diaChanFrames = list.ToArray();
            }
#endif
            return diaChanFrames;
        }

        private static int ExtractIndex(string name)
        {
            int idx = name.LastIndexOf('_');
            if (idx >= 0 && int.TryParse(name.Substring(idx + 1), out int val)) return val;
            return 0;
        }

        private static Sprite GetSoftCircleSprite()
        {
            if (softCircleSprite != null) return softCircleSprite;

            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];
            float radius = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - radius) / radius;
                    float dy = (y - radius) / radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= 1f)
                    {
                        float alpha = Mathf.SmoothStep(1f, 0f, d);
                        cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        cols[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            softCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return softCircleSprite;
        }

        /// <summary>
        /// Spawns a pulsing Red Oval Telegraph Zone.
        /// </summary>
        public static GameObject SpawnTelegraph(Vector3 center, float width, float height, float duration, Action onComplete = null)
        {
            GameObject container = new GameObject("Telegraph_GroundZone");
            container.transform.position = center;

            // 1. Red Border LineRenderer (40 segments in an ellipse)
            GameObject ringObj = new GameObject("Ring");
            ringObj.transform.SetParent(container.transform, false);

            LineRenderer lr = ringObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.15f, 0.15f, 0.85f);
            lr.endColor = new Color(1f, 0.15f, 0.15f, 0.85f);
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.sortingOrder = 9;

            int segments = 40;
            lr.positionCount = segments;
            float rx = width * 0.5f;
            float ry = height * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry, 0f));
            }

            // 2. Soft Red Translucent Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(container.transform, false);
            fillObj.transform.localScale = new Vector3(width, height, 1f);

            SpriteRenderer fillSr = fillObj.AddComponent<SpriteRenderer>();
            fillSr.sprite = GetSoftCircleSprite();
            fillSr.color = new Color(1f, 0.15f, 0.15f, 0.25f);
            fillSr.sortingOrder = 8;

            // 3. Telegraph Controller to handle pulsation and auto-completion
            var ctrl = container.AddComponent<TelegraphOvalController>();
            ctrl.Initialize(lr, fillSr, duration, onComplete);

            return container;
        }

        /// <summary>
        /// Spawns the Dia Chan Earthquake VFX and performs ellipse damage detection.
        /// </summary>
        public static GameObject SpawnEarthquakeVFX(
            Vector3 center,
            float width,
            float height,
            float damage,
            float knockbackForce = 6f,
            Color? tintColor = null,
            Transform playerTransform = null)
        {
            Sprite[] frames = GetDiaChanFrames();
            if (frames == null || frames.Length == 0) return null;

            GameObject vfxObj = new GameObject("VFX_DiaChan_Shockwave");
            vfxObj.transform.position = center;

            // Native aspect ratio is 116 / 50 = 2.32. Native width at PPU 32 is 3.625m.
            // Note: Visible crack pixels span 80px of 116px (visibleRatio = 116 / 80 = 1.45f).
            // Multiplying by 1.45f aligns the visible ground fissure edges exactly with the red telegraph indicator ring.
            float visibleRatio = 116f / 80f; // 1.45f
            float scaleFactor = (width / 3.625f) * visibleRatio;
            vfxObj.transform.localScale = Vector3.one * scaleFactor;

            SpriteRenderer sr = vfxObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 18; // Above grass and decals
            if (tintColor.HasValue) sr.color = tintColor.Value;

            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit") ?? Shader.Find("Sprites/Default");
            if (unlit != null) sr.material = new Material(unlit);

            AnimatedVFX anim = vfxObj.AddComponent<AnimatedVFX>();
            anim.Initialize(frames, 14f);

            // Ellipse Damage Check to Player
            if (playerTransform == null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) playerTransform = pc.transform;
            }

            if (playerTransform != null)
            {
                Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)center;
                float rx = width * 0.5f;
                float ry = height * 0.5f;

                float normX = toPlayer.x / rx;
                float normY = toPlayer.y / ry;

                if ((normX * normX + normY * normY) <= 1.0f)
                {
                    var ph = playerTransform.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        Vector2 kbDir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector2.up;
                        ph.TakeDamage(damage, kbDir, knockbackForce, DamageSourceType.BossSkill);
                    }
                }
            }

            return vfxObj;
        }
    }

    /// <summary>
    /// Component managing telegraph oval duration, pulsation, and cleanup.
    /// </summary>
    public class TelegraphOvalController : MonoBehaviour
    {
        private LineRenderer lr;
        private SpriteRenderer fill;
        private float duration;
        private float elapsed = 0f;
        private Action onComplete;

        public void Initialize(LineRenderer line, SpriteRenderer fillSr, float life, Action callback)
        {
            lr = line;
            fill = fillSr;
            duration = Mathf.Max(0.1f, life);
            onComplete = callback;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // Pulse fill opacity faster as impact nears
            if (fill != null)
            {
                float freq = Mathf.Lerp(6f, 18f, progress);
                float pulse = 0.20f + 0.15f * Mathf.Sin(elapsed * freq);
                fill.color = new Color(1f, 0.15f, 0.15f, pulse);
            }

            if (elapsed >= duration)
            {
                onComplete?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
