using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Gizmo helpers for visualizing the spatial range checks the demos project onto the planner's
    /// predicates. Drawn from OnDrawGizmos, so they show in the Scene view (and the Game view with
    /// gizmos enabled) without adding sprites to the scene.
    /// </summary>
    public static class DemoGizmos
    {
        public static void Circle(Vector3 center, float radius, Color color)
        {
            Color previous = Gizmos.color;
            Gizmos.color = color;

            const int segments = 36;
            Vector3 point = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                Gizmos.DrawLine(point, next);
                point = next;
            }

            Gizmos.color = previous;
        }

        public static void Rect(Vector2 halfExtents, Color color)
        {
            Color previous = Gizmos.color;
            Gizmos.color = color;
            Vector3 a = new Vector3(-halfExtents.x, -halfExtents.y, 0f);
            Vector3 b = new Vector3(halfExtents.x, -halfExtents.y, 0f);
            Vector3 c = new Vector3(halfExtents.x, halfExtents.y, 0f);
            Vector3 d = new Vector3(-halfExtents.x, halfExtents.y, 0f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
            Gizmos.color = previous;
        }
    }
}
