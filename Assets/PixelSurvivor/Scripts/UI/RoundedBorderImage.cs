using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Procedural UI Graphic component that renders a rounded rectangle with customizable
    /// corner radius, border thickness, fill color, and border color directly in Unity Inspector.
    /// No custom sprites or 9-slicing required.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/PixelSurvivor/Rounded Border Image")]
    public class RoundedBorderImage : Graphic
    {
        [Header("Border & Radius Settings")]
        [Tooltip("Corner radius in pixels.")]
        [Min(0f)]
        [SerializeField] private float cornerRadius = 16f;

        [Tooltip("Thickness of the border in pixels. Set to 0 for filled shape without border.")]
        [Min(0f)]
        [SerializeField] private float borderThickness = 3f;

        [Range(2, 32)]
        [Tooltip("Resolution of corner curves. Higher = smoother curves, lower = more pixelated/faceted.")]
        [SerializeField] private int cornerSegments = 20;

        [Header("Anti-Aliasing")]
        [Tooltip("Enables sub-pixel smooth feathering along curved and diagonal edges.")]
        [SerializeField] private bool antiAliased = true;
        [Range(0.5f, 2f)]
        [SerializeField] private float featherWidth = 1f;

        [Header("Colors")]
        [Tooltip("Fill/Background color inside the card.")]
        [SerializeField] private Color fillColor = new Color(0.043f, 0.059f, 0.094f, 0.95f); // Dark Obsidian

        [Tooltip("Border outline color.")]
        [SerializeField] private Color borderColor = new Color(0.98f, 0.8f, 0.08f, 1f); // Vibrant Gold

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                cornerRadius = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public float BorderThickness
        {
            get => borderThickness;
            set
            {
                borderThickness = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public bool AntiAliased
        {
            get => antiAliased;
            set
            {
                antiAliased = value;
                SetVerticesDirty();
            }
        }

        public float FeatherWidth
        {
            get => featherWidth;
            set
            {
                featherWidth = Mathf.Clamp(value, 0.5f, 2f);
                SetVerticesDirty();
            }
        }

        public Color FillColor
        {
            get => fillColor;
            set
            {
                fillColor = value;
                SetVerticesDirty();
            }
        }

        public Color BorderColor
        {
            get => borderColor;
            set
            {
                borderColor = value;
                SetVerticesDirty();
            }
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set
            {
                cornerSegments = Mathf.Clamp(value, 2, 32);
                SetVerticesDirty();
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            cornerRadius = Mathf.Max(0f, cornerRadius);
            borderThickness = Mathf.Max(0f, borderThickness);
            cornerSegments = Mathf.Clamp(cornerSegments, 2, 32);
            SetVerticesDirty();
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float rOuter = Mathf.Clamp(cornerRadius, 0f, maxRadius);
            float thickness = Mathf.Clamp(borderThickness, 0f, maxRadius);

            bool hasBorder = thickness > 0.001f && borderColor.a > 0.001f;
            bool hasFill = fillColor.a > 0.001f;

            // Generate outer contour points
            List<Vector2> outerPoints = GenerateContour(rect, rOuter);

            // Draw Outer Anti-Aliasing Fringe (Soft sub-pixel edge)
            Color edgeColor = hasBorder ? borderColor : fillColor;
            if (antiAliased && edgeColor.a > 0.001f)
            {
                Color clearEdge = new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0f);
                List<Vector2> fringePoints = GenerateContour(
                    new Rect(rect.xMin - featherWidth, rect.yMin - featherWidth, rect.width + 2f * featherWidth, rect.height + 2f * featherWidth),
                    rOuter + featherWidth
                );
                int countFringe = outerPoints.Count;
                for (int i = 0; i < countFringe; i++)
                {
                    int next = (i + 1) % countFringe;
                    Vector2 o0 = outerPoints[i];
                    Vector2 o1 = outerPoints[next];
                    Vector2 f1 = fringePoints[next];
                    Vector2 f0 = fringePoints[i];
                    AddQuad(vh, rect, o0, o1, f1, f0, edgeColor, edgeColor, clearEdge, clearEdge);
                }
            }

            if (!hasBorder)
            {
                // Pure fill without border
                if (hasFill)
                {
                    DrawFilledContour(vh, outerPoints, rect, fillColor);
                }
                return;
            }

            // Check if thickness consumes the entire rect
            if (thickness >= maxRadius || (rect.width - 2f * thickness) <= 0.001f || (rect.height - 2f * thickness) <= 0.001f)
            {
                // Border occupies whole area
                DrawFilledContour(vh, outerPoints, rect, borderColor);
                return;
            }

            // Generate inner contour
            Rect innerRect = new Rect(
                rect.xMin + thickness,
                rect.yMin + thickness,
                rect.width - 2f * thickness,
                rect.height - 2f * thickness
            );

            float rInner = Mathf.Max(0f, rOuter - thickness);
            List<Vector2> innerPoints = GenerateContour(innerRect, rInner);

            // Draw Border Ring (Quads between outer and inner points)
            int count = outerPoints.Count;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;

                Vector2 o1 = outerPoints[i];
                Vector2 o2 = outerPoints[next];
                Vector2 i2 = innerPoints[next];
                Vector2 i1 = innerPoints[i];

                AddQuad(vh, rect, o1, o2, i2, i1, borderColor);
            }

            // Draw Fill (Inner region)
            if (hasFill)
            {
                DrawFilledContour(vh, innerPoints, rect, fillColor);
            }
        }

        private List<Vector2> GenerateContour(Rect r, float radius)
        {
            List<Vector2> points = new List<Vector2>();

            if (radius <= 0.001f)
            {
                // Plain rectangle (sharp 90-degree corners)
                points.Add(new Vector2(r.xMax, r.yMax)); // Top-Right
                points.Add(new Vector2(r.xMin, r.yMax)); // Top-Left
                points.Add(new Vector2(r.xMin, r.yMin)); // Bottom-Left
                points.Add(new Vector2(r.xMax, r.yMin)); // Bottom-Right
                return points;
            }

            // 4 Corner centers
            Vector2 cTopRight = new Vector2(r.xMax - radius, r.yMax - radius);
            Vector2 cTopLeft = new Vector2(r.xMin + radius, r.yMax - radius);
            Vector2 cBottomLeft = new Vector2(r.xMin + radius, r.yMin + radius);
            Vector2 cBottomRight = new Vector2(r.xMax - radius, r.yMin + radius);

            // Quadrant 1: Top-Right (0 to 90 deg)
            AddArcPoints(points, cTopRight, radius, 0f, Mathf.PI * 0.5f);

            // Quadrant 2: Top-Left (90 to 180 deg)
            AddArcPoints(points, cTopLeft, radius, Mathf.PI * 0.5f, Mathf.PI);

            // Quadrant 3: Bottom-Left (180 to 270 deg)
            AddArcPoints(points, cBottomLeft, radius, Mathf.PI, Mathf.PI * 1.5f);

            // Quadrant 4: Bottom-Right (270 to 360 deg)
            AddArcPoints(points, cBottomRight, radius, Mathf.PI * 1.5f, Mathf.PI * 2f);

            return points;
        }

        private void AddArcPoints(List<Vector2> points, Vector2 center, float radius, float startAngle, float endAngle)
        {
            int segments = Mathf.Max(1, cornerSegments);
            float step = (endAngle - startAngle) / segments;

            for (int i = 0; i <= segments; i++)
            {
                // For continuous loop, omit duplicate connecting points between corners
                if (i == 0 && points.Count > 0) continue;

                float angle = startAngle + i * step;
                Vector2 pt = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                points.Add(pt);
            }
        }

        private void DrawFilledContour(VertexHelper vh, List<Vector2> contour, Rect bounds, Color color)
        {
            if (contour == null || contour.Count < 3) return;

            Vector2 center = bounds.center;
            int centerIdx = vh.currentVertCount;
            vh.AddVert(CreateUIVertex(center, bounds, color));

            int startIdx = vh.currentVertCount;
            for (int i = 0; i < contour.Count; i++)
            {
                vh.AddVert(CreateUIVertex(contour[i], bounds, color));
            }

            int n = contour.Count;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                vh.AddTriangle(centerIdx, startIdx + i, startIdx + next);
            }
        }

        private void AddQuad(VertexHelper vh, Rect bounds, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, Color c0, Color c1, Color c2, Color c3)
        {
            int startIndex = vh.currentVertCount;

            vh.AddVert(CreateUIVertex(v0, bounds, c0));
            vh.AddVert(CreateUIVertex(v1, bounds, c1));
            vh.AddVert(CreateUIVertex(v2, bounds, c2));
            vh.AddVert(CreateUIVertex(v3, bounds, c3));

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        private void AddQuad(VertexHelper vh, Rect bounds, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, Color color)
        {
            AddQuad(vh, bounds, v0, v1, v2, v3, color, color, color, color);
        }

        private UIVertex CreateUIVertex(Vector2 pos, Rect bounds, Color color)
        {
            UIVertex vert = UIVertex.simpleVert;
            vert.position = pos;
            vert.color = color;
            vert.uv0 = new Vector2(
                bounds.width > 0 ? (pos.x - bounds.xMin) / bounds.width : 0.5f,
                bounds.height > 0 ? (pos.y - bounds.yMin) / bounds.height : 0.5f
            );
            return vert;
        }
    }
}
