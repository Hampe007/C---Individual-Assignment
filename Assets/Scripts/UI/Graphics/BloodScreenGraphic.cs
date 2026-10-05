using UnityEngine;
using UnityEngine.UI;

namespace GameMenus
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BloodScreenGraphic : MaskableGraphic
    {
        [SerializeField] private Texture2D splatAtlas;
        [SerializeField] private Rect[] splatUvs;
        [SerializeField, Range(0f, 1f)] private float coverage;
        [SerializeField, Range(0f, 1f)] private float drain;
        [SerializeField, Range(0f, 0.15f)] private float creepSpeed = 0.025f;
        [SerializeField, Range(0f, 1f)] private float trailOpacity = 0.72f;
        private Vector2 impactOrigin = new(0.5f, 0.5f);
        private float wetAge;

        public override Texture mainTexture => splatAtlas != null ? splatAtlas : Texture2D.whiteTexture;
        public float Coverage => coverage;

        public void SetImpactOrigin(Vector2 origin)
        {
            impactOrigin = new Vector2(Mathf.Clamp01(origin.x), Mathf.Clamp01(origin.y));
        }

        public void SetEffect(float coverage, float drain = 0f)
        {
            if (this.coverage <= 0f)
            {
                wetAge = 0f;
            }
            else
            {
                wetAge += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            }
            this.coverage = Mathf.Clamp01(coverage);
            this.drain = Mathf.Clamp01(drain);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (coverage <= 0f || drain >= 1f || splatAtlas == null || splatUvs == null || splatUvs.Length == 0)
            {
                return;
            }
            Rect bounds = rectTransform.rect;
            float seal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.86f, 1f, coverage));
            if (seal > 0f)
            {
                Vector2 center = bounds.center - Vector2.up * bounds.height * Mathf.Pow(drain, 1.2f) * 12f;
                Splat(vertices, center, new Vector2(bounds.width * 8f, bounds.height * 6f) * seal, 0f, splatUvs[0], color);
            }

            const int count = 108;
            for (int splat = 0; splat < count; splat++)
            {
                int wave = splat / 18;
                float appearance = wave * 0.13f + Noise(splat, 8) * 0.04f;
                float growth = Mathf.Clamp01((coverage - appearance) / 0.055f);
                if (growth <= 0f)
                {
                    continue;
                }
            
                float fall = Mathf.Clamp01((drain - Noise(splat, 7) * 0.2f) / 0.8f);
                float horizontal = splat < 36 ? impactOrigin.x + (Noise(splat, 1) - 0.5f) * 0.9f : Noise(splat, 1);
                float vertical = splat < 36 ? impactOrigin.y + (Noise(splat, 2) - 0.5f) * 0.75f : Noise(splat, 2);
                Vector2 center = new(bounds.xMin + bounds.width * horizontal, bounds.yMin + bounds.height * vertical);
                float creep = wetAge * creepSpeed * (0.35f + Noise(splat, 9));
                float drop = (creep + fall * fall * 2.7f) * bounds.height;
                bool droplet = splat % 3 == 0;
                float size = bounds.height * (droplet ? 0.018f + Noise(splat, 3) * 0.07f : 0.14f + Noise(splat, 3) * 0.36f);
                size *= Mathf.Lerp(0.75f, 1f, Mathf.SmoothStep(0f, 1f, growth));
                Color tint = Color.Lerp(color * 0.75f, color, Noise(splat, 6));
                tint.a = Mathf.SmoothStep(0f, 1f, growth) * (droplet ? 0.82f : 0.96f);
                tint.a *= 1f - seal * 0.85f * (1f - Mathf.SmoothStep(0.1f, 0.5f, drain));
                Rect uv = splatUvs[splat % splatUvs.Length];
                if (droplet && drop > size * 0.2f)
                {
                    Color trail = tint;
                    trail.a *= trailOpacity * (1f - Mathf.SmoothStep(0.55f, 1f, drain));
                    Vector2 trailCenter = center - Vector2.up * drop * 0.5f;
                    Splat(vertices, trailCenter, new Vector2(size * 0.24f, size + drop), 0f, uv, trail);
                }
            
                center.y -= drop;
                Vector2 scale = new(size * (0.8f + Noise(splat, 4) * 0.4f), size * (1f + fall * 0.35f));
                Splat(vertices, center, scale, Noise(splat, 5) * 360f, uv, tint);
            }
        }

        private static float Noise(int splat, int salt)
        {
            return Mathf.Repeat(Mathf.Sin((splat + 1) * 127.1f + salt * 311.7f) * 43758.5453f, 1f);
        }

        private static void Splat(VertexHelper vertices, Vector2 center, Vector2 size, float angle, Rect uv, Color tint)
        {
            int index = vertices.currentVertCount;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Vector2 half = size * 0.5f;
            vertices.AddVert(center + (Vector2)(rotation * new Vector3(-half.x, -half.y)), tint, new Vector2(uv.xMin, uv.yMin));
            vertices.AddVert(center + (Vector2)(rotation * new Vector3(-half.x, half.y)), tint, new Vector2(uv.xMin, uv.yMax));
            vertices.AddVert(center + (Vector2)(rotation * new Vector3(half.x, half.y)), tint, new Vector2(uv.xMax, uv.yMax));
            vertices.AddVert(center + (Vector2)(rotation * new Vector3(half.x, -half.y)), tint, new Vector2(uv.xMax, uv.yMin));
            vertices.AddTriangle(index, index + 1, index + 2);
            vertices.AddTriangle(index, index + 2, index + 3);
        }
    }
}
