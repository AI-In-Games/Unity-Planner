using AIInGames.Planning.Runtime;
using AIInGames.Planning.Unity;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// A free-movement chase where the soldier carries one grenade and a loaded rifle. The planner
    /// prefers the grenade because it is a single ranged action that kills, but the throw is
    /// telegraphed: a gizmo blast ring shows where it will land and the player can run out of it
    /// during the wind-up. A missed grenade is spent, so the planner replans to the rifle. Actors,
    /// the thrown grenade, bullets, and explosions are real prefabs; ranges are gizmos. The player is
    /// a self-contained PlayerController.
    /// </summary>
    public sealed class SoldierGrenadeDemo : MonoBehaviour
    {
        private const string Open = "open";
        private const string Lob = "lob";
        private const string Target = "target";

        [SerializeField] private DomainAsset m_Domain;
        [SerializeField] private Transform m_Soldier;
        [SerializeField] private Transform m_Player;
        [SerializeField] private GameObject m_BulletPrefab;
        [SerializeField] private GameObject m_GrenadePrefab;
        [SerializeField] private GameObject m_ExplosionPrefab;
        [SerializeField] private float m_SoldierSpeed = 3f;
        [SerializeField] private float m_ShootRange = 2.5f;
        [SerializeField] private float m_GrenadeRange = 4.5f;
        [SerializeField] private float m_BlastRadius = 1.5f;
        [SerializeField] private float m_GrenadeWindup = 0.8f;
        [SerializeField] private float m_ShotWindup = 0.4f;
        [SerializeField] private float m_BulletSpeed = 14f;
        [SerializeField] private float m_ExplosionLife = 1.2f;

        private PlannerService m_Service;
        private PlannerLoop m_Loop;
        private PlanExecutor m_Executor;
        private PlanResult m_Plan;
        private PlayerController m_PlayerCtrl;

        private bool m_AwaitingPlan;
        private bool m_HasGrenade = true;
        private bool m_Throwing;
        private Vector3 m_BlastCenter;
        private Vector3 m_SoldierStart;

        private void Start()
        {
            if (m_Domain == null)
            {
                Debug.LogError("[SoldierGrenade] No DomainAsset assigned. Rebuild the scene or run Convert Demo Domains to Assets.");
                enabled = false;
                return;
            }

            m_SoldierStart = m_Soldier.position;
            m_PlayerCtrl = m_Player.GetComponent<PlayerController>();

            m_Service = new PlannerService(PlanFinders.BitVector(m_Domain.Actions));
            m_Loop = new PlannerLoop(m_Service);

            m_Executor = new PlanExecutor();
            m_Executor.RegisterExecutor("throw-grenade", new ThrowGrenadeExecutor(
                InGrenadeRange, () => m_Player.position, m_BlastRadius, m_GrenadeWindup, OnGrenadeThrow, OnGrenadeDetonate));
            m_Executor.RegisterExecutor("move", new SoldierMoveExecutor(m_Soldier, ResolveLocation, m_SoldierSpeed, ArrivalRadius));
            m_Executor.RegisterExecutor("shoot", new SoldierShootExecutor(m_Soldier, () => m_Player.position, InRange, OnSoldierFires, m_ShotWindup));
            m_Executor.RegisterExecutor("take-cover", new SimulatedActionExecutor(0.2f));
            m_Executor.RegisterExecutor("reload", new SimulatedActionExecutor(0.2f));

            m_Executor.OnActionStarted += action => Debug.Log($"[SoldierGrenade] start  {action.Name}");
            m_Executor.OnActionCompleted += action => Debug.Log($"[SoldierGrenade] done   {action.Name}");
            m_Executor.OnActionFailed += (action, reason) => Debug.Log($"[SoldierGrenade] failed {action.Name} ({reason})");
            m_Executor.OnPlanCompleted += () => Debug.Log("[SoldierGrenade] plan complete");

            Debug.Log("[SoldierGrenade] ready. It leads with the grenade; dodge the blast to force it onto the rifle.");
        }

        private void Update()
        {
            if (m_PlayerCtrl == null || !m_PlayerCtrl.Alive)
            {
                if (Input.GetKeyDown(KeyCode.R))
                    Restart();
                return;
            }

            m_Executor.Update();

            if (!m_AwaitingPlan && m_Executor.State != PlanExecutor.ExecutionState.Executing)
                RequestPlan();
        }

        private void OnDestroy()
        {
            m_Loop?.Dispose();
        }

        private void Restart()
        {
            m_Executor.Cancel();
            m_Soldier.position = m_SoldierStart;
            m_PlayerCtrl.ResetState();
            m_AwaitingPlan = false;
            m_HasGrenade = true;
            m_Throwing = false;
            m_Plan = null;
            Debug.Log("[SoldierGrenade] restart");
        }

        private void RequestPlan()
        {
            if (m_AwaitingPlan)
                return;

            Debug.Log($"[SoldierGrenade] replanning: at={AtLocation()}, has-grenade={m_HasGrenade}");
            m_AwaitingPlan = true;
            m_Service.Submit(BuildProblem(), OnPlanReady, priority: 0, ownerKey: "soldier1", replaceOwner: true);
        }

        private void OnPlanReady(PlanResult result)
        {
            m_AwaitingPlan = false;
            m_Plan = result;
            if (result.Success && result.Actions.Count > 0)
            {
                Debug.Log($"[SoldierGrenade] plan ({result.Actions.Count} steps): {PlanText(result)}");
                m_Executor.StartPlan(result);
            }
            else
            {
                Debug.Log("[SoldierGrenade] no plan found");
            }
        }

        private PlanningProblemDefinition BuildProblem()
        {
            PlanningProblemBuilder builder = PlanningProblemDefinition.Builder("soldier-grenade")
                .Object("soldier1", "agent")
                .Object("enemy1", "enemy")
                .Object("rifle1", "weapon")
                .Object(Open, "location")
                .Object(Lob, "location")
                .Object(Target, "location")
                .Initially("has-weapon", "soldier1", "rifle1")
                .Initially("weapon-loaded", "rifle1")
                .Initially("enemy-alive", "enemy1")
                .Initially("enemy-at", "enemy1", Target)
                .Initially("connected", Open, Lob)
                .Initially("connected", Lob, Open)
                .Initially("connected", Lob, Target)
                .Initially("connected", Target, Lob)
                .Initially("at", "soldier1", AtLocation());

            if (m_HasGrenade)
                builder.Initially("has-grenade", "soldier1");

            builder.GoalNot("enemy-alive", "enemy1");
            return builder.Build();
        }

        private Vector3 ResolveLocation(string location)
        {
            return location == Target || location == Lob ? m_Player.position : m_SoldierStart;
        }

        private float ArrivalRadius(string location)
        {
            if (location == Target)
                return m_ShootRange;
            if (location == Lob)
                return m_GrenadeRange;
            return 0.5f;
        }

        private string AtLocation()
        {
            if (InRange())
                return Target;
            if (InGrenadeRange())
                return Lob;
            return Open;
        }

        private bool InRange()
        {
            return SoldierMoveExecutor.PlanarDistance(m_Soldier.position, m_Player.position) <= m_ShootRange;
        }

        private bool InGrenadeRange()
        {
            return SoldierMoveExecutor.PlanarDistance(m_Soldier.position, m_Player.position) <= m_GrenadeRange;
        }

        private void OnGrenadeThrow(Vector3 center)
        {
            Debug.Log("[SoldierGrenade] grenade thrown; dodge the blast ring");
            m_Throwing = true;
            m_BlastCenter = center;
            CombatFx.Throw(m_GrenadePrefab, m_Soldier.position, center, m_BulletSpeed);
        }

        private void OnGrenadeDetonate(bool hit)
        {
            m_Throwing = false;
            m_HasGrenade = false;
            CombatFx.Explosion(m_ExplosionPrefab, m_BlastCenter, m_ExplosionLife);

            Debug.Log(hit ? "[SoldierGrenade] grenade hit" : "[SoldierGrenade] grenade missed; falling back to rifle");
            if (hit)
                m_PlayerCtrl.Kill();
        }

        private void OnSoldierFires()
        {
            CombatFx.Bullet(m_BulletPrefab, m_ExplosionPrefab, m_Soldier.position, m_Player, m_BulletSpeed, 0.4f, 2f, m_ExplosionLife,
                () => m_PlayerCtrl.Kill());
        }

        private static string PlanText(PlanResult result)
        {
            string[] names = new string[result.Actions.Count];
            for (int i = 0; i < result.Actions.Count; i++)
                names[i] = result.Actions[i].Name;
            return string.Join("  ->  ", names);
        }

        private void OnDrawGizmos()
        {
            if (m_Soldier != null)
            {
                DemoGizmos.Circle(m_Soldier.position, m_GrenadeRange, new Color(0.95f, 0.85f, 0.35f));
                DemoGizmos.Circle(m_Soldier.position, m_ShootRange, InRange() ? Color.green : new Color(0.9f, 0.4f, 0.4f));
            }
            if (m_Throwing)
                DemoGizmos.Circle(m_BlastCenter, m_BlastRadius, new Color(1f, 0.55f, 0.2f));
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 400, Screen.height - 24f));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label(m_PlayerCtrl != null && m_PlayerCtrl.Alive
                ? "WASD: move. Lead is a grenade; run out of the blast ring to survive it."
                : "ELIMINATED. Press R to restart.");

            float distance = SoldierMoveExecutor.PlanarDistance(m_Soldier.position, m_Player.position);
            GUILayout.Label($"Distance: {distance:0.0}   Shoot: {m_ShootRange:0.0}   Grenade: {m_GrenadeRange:0.0}");
            GUILayout.Label($"Has grenade: {m_HasGrenade}   Executor: {(m_Executor != null ? m_Executor.State.ToString() : "-")}");

            GUILayout.Space(4);
            DemoHud.DrawPlan(m_Plan, m_Executor);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
