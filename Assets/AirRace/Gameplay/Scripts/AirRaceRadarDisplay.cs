using UnityEngine;
using UnityEngine.UI;

namespace AirRace.Gameplay
{
    /// <summary>Compact heading-up route radar for checkpoints and energy cans.</summary>
    public sealed class AirRaceRadarDisplay : Graphic
    {
        [SerializeField] private float worldRadius = 340f;
        private Vector3 aircraftPosition;
        private float heading;
        private Vector3[] checkpoints = new Vector3[0];
        private Vector3[] cans = new Vector3[0];
        private int nextCheckpoint;
        private Vector3 finishPosition;
        private bool hasFinish;

        public void SetRoute(Vector3 position, float yaw, Vector3[] gates, int next, Vector3[] pickups, Vector3 finish, bool includeFinish)
        {
            aircraftPosition = position;
            heading = yaw;
            checkpoints = gates ?? new Vector3[0];
            cans = pickups ?? new Vector3[0];
            nextCheckpoint = next;
            finishPosition = finish;
            hasFinish = includeFinish;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.43f;
            Vector2 center = rect.center;
            Color grid = new Color(0.48f, 0.88f, 0.78f, 0.52f);
            DrawCircle(vh, center, radius, grid, 44f, 40);
            DrawCircle(vh, center, radius * 0.66f, new Color(grid.r, grid.g, grid.b, 0.35f), 1.1f, 36);
            DrawCircle(vh, center, radius * 0.33f, new Color(grid.r, grid.g, grid.b, 0.28f), 1.1f, 32);
            DrawLine(vh, center + Vector2.left * radius, center + Vector2.right * radius, new Color(grid.r, grid.g, grid.b, 0.28f), 1f);
            DrawLine(vh, center + Vector2.down * radius, center + Vector2.up * radius, new Color(grid.r, grid.g, grid.b, 0.28f), 1f);

            if (checkpoints.Length > 1)
            {
                for (int i = 0; i < checkpoints.Length; i++)
                {
                    Vector2 a = ToRadar(checkpoints[i], center, radius);
                    Vector2 b = ToRadar(checkpoints[(i + 1) % checkpoints.Length], center, radius);
                    Color route = i < nextCheckpoint ? new Color(0.38f, 0.63f, 0.6f, 0.45f) : new Color(0.88f, 0.96f, 0.78f, 0.8f);
                    DrawLine(vh, a, b, route, 2.1f);
                }
                for (int i = 0; i < checkpoints.Length; i++)
                {
                    Vector2 p = ToRadar(checkpoints[i], center, radius);
                    Color gateColor = i == nextCheckpoint ? new Color(1f, 0.76f, 0.08f, 1f) :
                        i < nextCheckpoint ? new Color(0.36f, 0.62f, 0.58f, 0.95f) : Color.white;
                    DrawCircle(vh, p, i == nextCheckpoint ? 7f : 4.2f, gateColor, 3f, 12);
                }
            }

            if (hasFinish)
            {
                Vector2 finish = ToRadar(finishPosition, center, radius);
                DrawCircle(vh, finish, 7.5f, new Color(1f, 0.22f, 0.16f, 1f), 3f, 16);
            }

            foreach (Vector3 can in cans)
            {
                Vector2 p = ToRadar(can, center, radius);
                DrawDiamond(vh, p, 5.5f, new Color(0.18f, 0.84f, 1f, 1f));
            }

            // Nose triangle points to the top of this heading-up radar.
            Vector2 plane = center + Vector2.up * 2f;
            AddTriangle(vh, plane + new Vector2(0f, 9f), plane + new Vector2(-6f, -6f), plane + new Vector2(6f, -6f), new Color(0.28f, 1f, 0.77f));
        }

        private Vector2 ToRadar(Vector3 world, Vector2 center, float radius)
        {
            Vector3 relative = Quaternion.Euler(0f, -heading, 0f) * (world - aircraftPosition);
            Vector2 map = new Vector2(relative.x, relative.z) / worldRadius;
            if (map.sqrMagnitude > 1f) map.Normalize();
            return center + map * radius;
        }

        private static void DrawCircle(VertexHelper vh, Vector2 center, float radius, Color color, float thickness, int segments)
        {
            Vector2 previous = center + Vector2.up * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 current = center + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
                DrawLine(vh, previous, current, color, thickness);
                previous = current;
            }
        }

        private static void DrawDiamond(VertexHelper vh, Vector2 p, float size, Color color)
        {
            AddTriangle(vh, p + Vector2.up * size, p + Vector2.right * size, p + Vector2.down * size, color);
            AddTriangle(vh, p + Vector2.down * size, p + Vector2.left * size, p + Vector2.up * size, color);
        }

        private static void DrawLine(VertexHelper vh, Vector2 a, Vector2 b, Color color, float thickness)
        {
            Vector2 delta = b - a;
            if (delta.sqrMagnitude < 0.0001f) return;
            Vector2 normal = new Vector2(-delta.y, delta.x).normalized * thickness * 0.5f;
            int start = vh.currentVertCount;
            AddVert(vh, a - normal, color); AddVert(vh, a + normal, color);
            AddVert(vh, b + normal, color); AddVert(vh, b - normal, color);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = vh.currentVertCount;
            AddVert(vh, a, color); AddVert(vh, b, color); AddVert(vh, c, color);
            vh.AddTriangle(start, start + 1, start + 2);
        }

        private static void AddVert(VertexHelper vh, Vector2 position, Color color)
        {
            vh.AddVert(position, color, Vector2.zero);
        }
    }
}
