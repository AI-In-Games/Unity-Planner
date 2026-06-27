using System;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// A self-contained player actor: WASD movement clamped to the arena, faces the mouse, and (when
    /// enabled) fires directional bullets toward the cursor that hit the target only if aimed at it.
    /// Owns its own death (swap to the splash decal) and reset, so the scene demos only deal with the
    /// soldier AI. The scene builder wires the prefabs and, for the showdown, the target and fire
    /// toggle; a demo subscribes <see cref="TargetHit"/> to take damage on the soldier.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float m_MoveSpeed = 4.5f;
        [SerializeField] private Vector2 m_ArenaHalfExtents = new Vector2(7f, 4f);
        [SerializeField] private bool m_CanShoot;
        [SerializeField] private Transform m_Target;
        [SerializeField] private GameObject m_BulletPrefab;
        [SerializeField] private GameObject m_ExplosionPrefab;
        [SerializeField] private float m_BulletSpeed = 16f;
        [SerializeField] private float m_BulletRange = 20f;
        [SerializeField] private float m_HitRadius = 0.55f;
        [SerializeField] private float m_ExplosionLife = 1.2f;
        [SerializeField] private float m_FireInterval = 0.1f;

        public event Action TargetHit;
        public bool Alive { get; private set; } = true;

        private Vector3 m_Start;
        private float m_NextShot;

        private void Awake()
        {
            m_Start = transform.position;
        }

        private void Update()
        {
            if (!Alive)
                return;

            Move();
            DemoFacing.FaceMouse(transform, Camera.main);

            if (m_CanShoot && Input.GetKey(KeyCode.Space) && Time.time >= m_NextShot)
                Fire();
        }

        public void Kill()
        {
            if (!Alive)
                return;
            Alive = false;
            DemoActor.ShowSplash(transform);
        }

        public void ResetState()
        {
            transform.position = m_Start;
            transform.rotation = Quaternion.identity;
            Alive = true;
            DemoActor.ResetVisual(transform);
        }

        private void Move()
        {
            Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0f);
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            Vector3 next = transform.position + direction * (m_MoveSpeed * Time.deltaTime);
            next.x = Mathf.Clamp(next.x, -m_ArenaHalfExtents.x, m_ArenaHalfExtents.x);
            next.y = Mathf.Clamp(next.y, -m_ArenaHalfExtents.y, m_ArenaHalfExtents.y);
            transform.position = next;
        }

        private void Fire()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            m_NextShot = Time.time + m_FireInterval;
            Vector3 mouse = camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = new Vector2(mouse.x - transform.position.x, mouse.y - transform.position.y);
            CombatFx.Fire(m_BulletPrefab, m_ExplosionPrefab, transform.position, direction,
                m_BulletSpeed, m_BulletRange, m_Target, m_HitRadius, m_ExplosionLife, () => TargetHit?.Invoke());
        }

        private void OnDrawGizmos()
        {
            DemoGizmos.Rect(m_ArenaHalfExtents, new Color(0.4f, 0.4f, 0.45f));
        }
    }
}
