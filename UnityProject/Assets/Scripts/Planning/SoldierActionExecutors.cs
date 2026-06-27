using System;
using AIInGames.Planning.Runtime;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Executes a grounded move by steering the soldier in free 2D space toward the position the
    /// destination location resolves to, chasing it live. The action's effect, at(soldier, dest), is
    /// realized spatially: the move completes once the soldier is within range of that position,
    /// which is the same range that lets it shoot.
    /// </summary>
    public sealed class SoldierMoveExecutor : IActionExecutor
    {
        private readonly Transform m_Mover;
        private readonly Func<string, Vector3> m_Resolve;
        private readonly float m_Speed;
        private readonly Func<string, float> m_ArrivalRadius;

        private string m_Destination;

        public SoldierMoveExecutor(Transform mover, Func<string, Vector3> resolve, float speed, Func<string, float> arrivalRadius)
        {
            m_Mover = mover;
            m_Resolve = resolve;
            m_Speed = speed;
            m_ArrivalRadius = arrivalRadius;
        }

        public bool CanExecute(GroundedAction action) => true;

        public void StartExecution(GroundedAction action)
        {
            m_Destination = action.Argument("?to");
        }

        public bool IsComplete()
        {
            Vector3 target = m_Resolve(m_Destination);
            DemoFacing.Face(m_Mover, new Vector2(target.x - m_Mover.position.x, target.y - m_Mover.position.y));
            Vector3 step = new Vector3(target.x, target.y, m_Mover.position.z);
            m_Mover.position = Vector3.MoveTowards(m_Mover.position, step, m_Speed * Time.deltaTime);
            return PlanarDistance(m_Mover.position, target) <= m_ArrivalRadius(m_Destination);
        }

        public bool HasFailed() => false;

        public void Cancel() { }

        internal static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }

    /// <summary>
    /// Fires after a short wind-up, but only while the target stays within range. If the target
    /// leaves range during the wind-up the shot fails, which makes the demo replan. On completion it
    /// reports the hit.
    /// </summary>
    public sealed class SoldierShootExecutor : IActionExecutor
    {
        private readonly Transform m_Shooter;
        private readonly Func<Vector3> m_Aim;
        private readonly Func<bool> m_InRange;
        private readonly Action m_OnHit;
        private readonly float m_Windup;

        private float m_FireTime;

        public SoldierShootExecutor(Transform shooter, Func<Vector3> aim, Func<bool> inRange, Action onHit, float windup)
        {
            m_Shooter = shooter;
            m_Aim = aim;
            m_InRange = inRange;
            m_OnHit = onHit;
            m_Windup = windup;
        }

        public bool CanExecute(GroundedAction action) => m_InRange();

        public void StartExecution(GroundedAction action)
        {
            m_FireTime = Time.time + m_Windup;
            FaceTarget();
        }

        public bool IsComplete()
        {
            FaceTarget();
            if (Time.time < m_FireTime)
                return false;

            m_OnHit?.Invoke();
            return true;
        }

        private void FaceTarget()
        {
            if (m_Shooter == null || m_Aim == null)
                return;
            Vector3 target = m_Aim();
            DemoFacing.Face(m_Shooter, new Vector2(target.x - m_Shooter.position.x, target.y - m_Shooter.position.y));
        }

        public bool HasFailed() => !m_InRange();

        public void Cancel() { }
    }

    /// <summary>
    /// Lobs a grenade once the soldier is already in grenade range (the plan's move into range puts
    /// it there). It can only start in range, then winds up and detonates at the spot the target
    /// occupied when the throw began, so the target can dodge by leaving the blast radius. The throw
    /// is committed once started: dodging beats the blast, not the throw.
    /// </summary>
    public sealed class ThrowGrenadeExecutor : IActionExecutor
    {
        private readonly Func<bool> m_InRange;
        private readonly Func<Vector3> m_Aim;
        private readonly float m_BlastRadius;
        private readonly float m_Windup;
        private readonly Action<Vector3> m_OnThrow;
        private readonly Action<bool> m_OnDetonate;

        private float m_FireTime;
        private Vector3 m_BlastCenter;

        public ThrowGrenadeExecutor(
            Func<bool> inRange, Func<Vector3> aim, float blastRadius, float windup,
            Action<Vector3> onThrow, Action<bool> onDetonate)
        {
            m_InRange = inRange;
            m_Aim = aim;
            m_BlastRadius = blastRadius;
            m_Windup = windup;
            m_OnThrow = onThrow;
            m_OnDetonate = onDetonate;
        }

        public bool CanExecute(GroundedAction action) => m_InRange();

        public void StartExecution(GroundedAction action)
        {
            m_BlastCenter = m_Aim();
            m_FireTime = Time.time + m_Windup;
            m_OnThrow?.Invoke(m_BlastCenter);
        }

        public bool IsComplete()
        {
            if (Time.time < m_FireTime)
                return false;

            bool hit = SoldierMoveExecutor.PlanarDistance(m_Aim(), m_BlastCenter) <= m_BlastRadius;
            m_OnDetonate?.Invoke(hit);
            return true;
        }

        public bool HasFailed() => false;

        public void Cancel() { }
    }
}
