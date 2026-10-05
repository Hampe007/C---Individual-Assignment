using UnityEngine;
using UnityEngine.UI;

namespace GameMenus
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RadialProgressGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)] private float progress;

        public float Progress
        {
            get => progress;
            set
            {
                progress = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect bounds = rectTransform.rect;
            float radius = Mathf.Min(bounds.width, bounds.height) * 0.5f;
            Ring(vertices, bounds.center, radius, new Color(color.r, color.g, color.b, 0.18f), 1f);
            Ring(vertices, bounds.center, radius, color, progress);
        }

        private static void Ring(VertexHelper vertices, Vector2 center, float radius, Color tint, float amount)
        {
            int segments = Mathf.CeilToInt(64f * amount);
            for (int segment = 0; segment < segments; segment++)
            {
                float start = Mathf.Min(segment / 64f, amount) * Mathf.PI * 2f;
                float end = Mathf.Min((segment + 1f) / 64f, amount) * Mathf.PI * 2f;
                Vector2 first = new(Mathf.Sin(start), Mathf.Cos(start));
                Vector2 second = new(Mathf.Sin(end), Mathf.Cos(end));
                int index = vertices.currentVertCount;
                vertices.AddVert(center + first * radius, tint, Vector2.zero);
                vertices.AddVert(center + second * radius, tint, Vector2.zero);
                vertices.AddVert(center + second * (radius - 4f), tint, Vector2.zero);
                vertices.AddVert(center + first * (radius - 4f), tint, Vector2.zero);
                vertices.AddTriangle(index, index + 1, index + 2);
                vertices.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
