using System.Collections;
using UnityEngine;

namespace PixelSurvivor.Combat
{
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private TextMesh textMesh;
        [SerializeField] private float duration = 0.8f;
        [SerializeField] private float floatSpeed = 1.4f;
        [SerializeField] private Vector3 punchScale = new Vector3(1.65f, 1.65f, 1f);

        private static Font pixelFont;

        public static void Spawn(Vector3 position, float damage, bool isCrit = false)
        {
            if (!Application.isPlaying) return;

            GameObject numObj = new GameObject("DamageNumber");
            numObj.transform.position = position + (Vector3)(Random.insideUnitCircle * 0.2f) + Vector3.up * 0.4f;

            DamageNumber dn = numObj.AddComponent<DamageNumber>();
            dn.Initialize(damage, isCrit);
        }

        public static void Spawn(Vector3 position, float damage, string customText)
        {
            SpawnText(position, customText);
        }

        public static void SpawnText(Vector3 position, string customText, Color? color = null)
        {
            if (!Application.isPlaying) return;

            GameObject numObj = new GameObject("DamageNumber_Text");
            numObj.transform.position = position + Vector3.up * 0.4f;

            DamageNumber dn = numObj.AddComponent<DamageNumber>();
            dn.InitializeText(customText, color ?? Color.white);
        }

        private static void EnsureFont()
        {
            if (pixelFont == null)
            {
                pixelFont = Resources.Load<Font>("Fonts/VCR_OSD_MONO");
                if (pixelFont == null)
                {
                    try
                    {
                        pixelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                    catch
                    {
                        // Fallback safely if Unity changes built-in name
                    }
                }
            }
        }

        public void InitializeText(string customText, Color color)
        {
            EnsureFont();

            textMesh = gameObject.AddComponent<TextMesh>();
            if (pixelFont != null)
            {
                textMesh.font = pixelFont;
                MeshRenderer mr = GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.material = pixelFont.material;
                    mr.sortingOrder = 30; // Above all sprites and VFX
                }
            }

            textMesh.text = customText;
            textMesh.fontSize = 50;
            textMesh.characterSize = 0.075f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;

            StartCoroutine(AnimateRoutine());
        }

        public void Initialize(float damage, bool isCrit)
        {
            EnsureFont();

            textMesh = gameObject.AddComponent<TextMesh>();
            if (pixelFont != null)
            {
                textMesh.font = pixelFont;
                MeshRenderer mr = GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.material = pixelFont.material;
                    mr.sortingOrder = 30; // Above all sprites and VFX
                }
            }

            textMesh.text = Mathf.RoundToInt(damage).ToString();
            textMesh.fontSize = isCrit ? 60 : 44;
            textMesh.characterSize = isCrit ? 0.085f : 0.068f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = isCrit ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(1f, 0.85f, 0.1f, 1f);

            StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            Vector3 startScale = punchScale;
            Vector3 targetScale = Vector3.one;
            transform.localScale = startScale;

            float elapsed = 0f;
            Color baseColor = textMesh.color;
            float randomXDrift = Random.Range(-0.3f, 0.3f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Punch scale transition
                if (t < 0.25f)
                {
                    transform.localScale = Vector3.Lerp(startScale, targetScale, t / 0.25f);
                }
                else
                {
                    transform.localScale = targetScale;
                }

                // Float upward with slight drift
                transform.position += new Vector3(randomXDrift * Time.deltaTime, floatSpeed * Time.deltaTime, 0f);

                // Fade out in last 40% of duration
                if (t > 0.6f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                    textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }

                yield return null;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
