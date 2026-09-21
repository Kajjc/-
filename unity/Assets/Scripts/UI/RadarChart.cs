using UnityEngine;
using UnityEngine.UI;

namespace Arena.UI
{
    // Гипотеза Г8 (docs/feature-hypotheses.md): треугольный радар на 3 оси
    // (напор/эмпатия/логика), нарисованный процедурной сеткой — весь UI проекта
    // и так собирается кодом (Theme.cs), без готовых ассетов/библиотек графиков.
    public class RadarChart : Graphic
    {
        private const float MaxLevel = 3f;

        // Три оси под 120° друг к другу, первая — строго вверх, поэтому фигура
        // читается как классический треугольный радар, а не произвольный полигон.
        private static readonly Vector2[] AxisDirections =
        {
            new Vector2(0f, 1f),
            new Vector2(-0.8660254f, -0.5f),
            new Vector2(0.8660254f, -0.5f),
        };

        public Color GridColor = new Color(1f, 1f, 1f, 0.15f);

        private float napor, empatiya, logika;

        public void SetValues(float naporLevel, float empatiyaLevel, float logikaLevel)
        {
            napor = Mathf.Clamp01(naporLevel / MaxLevel);
            empatiya = Mathf.Clamp01(empatiyaLevel / MaxLevel);
            logika = Mathf.Clamp01(logikaLevel / MaxLevel);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            Vector2 center = rect.center;

            AddTriangleFan(vh, center, radius, 1f, 1f, 1f, GridColor);
            AddTriangleFan(vh, center, radius, napor, empatiya, logika, color);
        }

        private static void AddTriangleFan(VertexHelper vh, Vector2 center, float radius, float v0, float v1, float v2, Color fill)
        {
            int baseIndex = vh.currentVertCount;
            vh.AddVert(center, fill, Vector2.zero);
            vh.AddVert(center + AxisDirections[0] * radius * v0, fill, Vector2.zero);
            vh.AddVert(center + AxisDirections[1] * radius * v1, fill, Vector2.zero);
            vh.AddVert(center + AxisDirections[2] * radius * v2, fill, Vector2.zero);

            vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
            vh.AddTriangle(baseIndex, baseIndex + 3, baseIndex + 1);
        }
    }
}
