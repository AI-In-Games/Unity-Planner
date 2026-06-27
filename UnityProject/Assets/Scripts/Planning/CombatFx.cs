using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Spawns the throwaway combat visuals from the Soldier prefab set. A bullet homes to a victim
    /// and delivers its hit payload on impact (so damage lands when the projectile arrives, not when
    /// it was fired); a thrown projectile flies to a fixed point; an explosion plays then despawns.
    /// </summary>
    public static class CombatFx
    {
        public static void Explosion(GameObject prefab, Vector3 position, float life)
        {
            if (prefab == null)
                return;
            GameObject go = Object.Instantiate(prefab, Flatten(position), Quaternion.identity);
            Object.Destroy(go, life);
        }

        public static void Bullet(
            GameObject bulletPrefab, GameObject explosionPrefab, Vector3 from, Transform victim,
            float speed, float hitRadius, float maxLife, float explosionLife, System.Action onHit)
        {
            if (bulletPrefab == null || victim == null)
            {
                onHit?.Invoke();
                return;
            }

            GameObject go = Object.Instantiate(bulletPrefab, Flatten(from), Quaternion.identity);
            go.AddComponent<Projectile>().LaunchHoming(victim, speed, hitRadius, maxLife, explosionPrefab, explosionLife, onHit);
        }

        public static void Fire(
            GameObject bulletPrefab, GameObject explosionPrefab, Vector3 from, Vector2 direction,
            float speed, float range, Transform target, float hitRadius, float explosionLife, System.Action onHit)
        {
            if (bulletPrefab == null)
                return;

            GameObject go = Object.Instantiate(bulletPrefab, Flatten(from), Quaternion.identity);
            go.AddComponent<Projectile>().LaunchDirectional(direction, speed, range, target, hitRadius, explosionPrefab, explosionLife, onHit);
        }

        public static void Throw(GameObject projectilePrefab, Vector3 from, Vector3 to, float speed)
        {
            if (projectilePrefab == null)
                return;

            GameObject go = Object.Instantiate(projectilePrefab, Flatten(from), Quaternion.identity);
            go.AddComponent<Projectile>().LaunchPoint(to, speed, null, 0f);
        }

        private static Vector3 Flatten(Vector3 p) => new Vector3(p.x, p.y, 0f);
    }
}
