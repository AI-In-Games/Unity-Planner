using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Rotates demo actors to face a direction. The Soldier and Player sprites point up (+y) at zero
    /// rotation, so a heading is offset by -90 degrees to align the sprite's forward with it.
    /// </summary>
    public static class DemoFacing
    {
        private const float ForwardOffset = -90f;

        public static void Face(Transform actor, Vector2 direction)
        {
            if (actor == null || direction.sqrMagnitude < 1e-6f)
                return;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + ForwardOffset;
            actor.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public static void FaceMouse(Transform actor, Camera camera)
        {
            if (actor == null)
                return;
            if (camera == null)
                camera = Camera.main;
            if (camera == null)
                return;

            Vector3 world = camera.ScreenToWorldPoint(Input.mousePosition);
            Face(actor, new Vector2(world.x - actor.position.x, world.y - actor.position.y));
        }
    }
}
