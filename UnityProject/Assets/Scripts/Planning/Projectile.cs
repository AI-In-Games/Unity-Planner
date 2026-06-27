using System;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Flies a spawned projectile and delivers its payload on impact, not when it was fired. Three
    /// modes: homing (tracks a victim, hits within a radius, misses after a lifetime), point (a
    /// thrown grenade to a fixed spot), and directional (flies a heading and hits a target only if it
    /// passes within range of it, so aim matters). Added by <see cref="CombatFx"/>.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private enum Mode { Homing, Point, Directional }

        private Mode m_Mode;
        private Transform m_Target;
        private Vector3 m_Point;
        private Vector2 m_Direction;
        private float m_Speed;
        private float m_HitRadius;
        private float m_Deadline;
        private float m_RangeLeft;
        private GameObject m_Explosion;
        private float m_ExplosionLife;
        private Action m_OnHit;

        public void LaunchHoming(Transform victim, float speed, float hitRadius, float maxLife,
            GameObject explosion, float explosionLife, Action onHit)
        {
            m_Mode = Mode.Homing;
            m_Target = victim;
            m_Speed = speed;
            m_HitRadius = hitRadius;
            m_Deadline = Time.time + maxLife;
            m_Explosion = explosion;
            m_ExplosionLife = explosionLife;
            m_OnHit = onHit;
        }

        public void LaunchPoint(Vector3 point, float speed, GameObject explosion, float explosionLife)
        {
            m_Mode = Mode.Point;
            m_Point = new Vector3(point.x, point.y, transform.position.z);
            m_Speed = speed;
            m_Explosion = explosion;
            m_ExplosionLife = explosionLife;
        }

        public void LaunchDirectional(Vector2 direction, float speed, float range, Transform target,
            float hitRadius, GameObject explosion, float explosionLife, Action onHit)
        {
            m_Mode = Mode.Directional;
            m_Direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.right;
            m_Speed = speed;
            m_RangeLeft = range;
            m_Target = target;
            m_HitRadius = hitRadius;
            m_Explosion = explosion;
            m_ExplosionLife = explosionLife;
            m_OnHit = onHit;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(m_Direction.y, m_Direction.x) * Mathf.Rad2Deg);
        }

        private void Update()
        {
            switch (m_Mode)
            {
                case Mode.Homing: UpdateHoming(); break;
                case Mode.Directional: UpdateDirectional(); break;
                default: UpdatePoint(); break;
            }
        }

        private void UpdateHoming()
        {
            if (m_Target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 target = new Vector3(m_Target.position.x, m_Target.position.y, transform.position.z);
            Aim(target);
            transform.position = Vector3.MoveTowards(transform.position, target, m_Speed * Time.deltaTime);

            if (SoldierMoveExecutor.PlanarDistance(transform.position, m_Target.position) <= m_HitRadius)
            {
                Detonate();
                return;
            }

            if (Time.time >= m_Deadline)
                Destroy(gameObject);
        }

        private void UpdateDirectional()
        {
            float step = m_Speed * Time.deltaTime;
            transform.position += new Vector3(m_Direction.x, m_Direction.y, 0f) * step;
            m_RangeLeft -= step;

            if (m_Target != null && SoldierMoveExecutor.PlanarDistance(transform.position, m_Target.position) <= m_HitRadius)
            {
                Detonate();
                return;
            }

            if (m_RangeLeft <= 0f)
                Destroy(gameObject);
        }

        private void UpdatePoint()
        {
            Aim(m_Point);
            transform.position = Vector3.MoveTowards(transform.position, m_Point, m_Speed * Time.deltaTime);
            if ((transform.position - m_Point).sqrMagnitude > 0.0025f)
                return;

            CombatFx.Explosion(m_Explosion, m_Point, m_ExplosionLife);
            Destroy(gameObject);
        }

        private void Detonate()
        {
            CombatFx.Explosion(m_Explosion, transform.position, m_ExplosionLife);
            m_OnHit?.Invoke();
            Destroy(gameObject);
        }

        private void Aim(Vector3 target)
        {
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }
}
