using AIInGames.Planning.Runtime;
using AIInGames.Planning.Unity;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Step 1: the smallest planner loop. The Soldier domain (a JSON DomainAsset) is projected onto
    /// two locations, where the soldier is ("open") and where you are ("target"); at(soldier, target)
    /// is just "within shoot range." The planner runs one find per frame, the soldier pursues and
    /// shoots, and replans as you move. The player is a self-contained PlayerController.
    /// </summary>
    public sealed class SoldierDemo : MonoBehaviour
    {
        private const string Open = "open";
        private const string Target = "target";

        [SerializeField] private DomainAsset m_Domain;
        [SerializeField] private Transform m_Soldier;
        [SerializeField] private Transform m_Player;
        [SerializeField] private GameObject m_BulletPrefab;
        [SerializeField] private GameObject m_ExplosionPrefab;
        [SerializeField] private float m_SoldierSpeed = 3f;
        [SerializeField] private float m_ShootRange = 2.5f;
        [SerializeField] private float m_ShotWindup = 0.4f;
        [SerializeField] private float m_BulletSpeed = 14f;
        [SerializeField] private float m_ExplosionLife = 1.2f;

        private PlannerService m_Service;
        private PlannerLoop m_Loop;
        private PlanExecutor m_Executor;
        private PlanResult m_Plan;
        private PlayerController m_PlayerCtrl;

        private bool m_AwaitingPlan;
        private Vector3 m_SoldierStart;

        private void Start()
        {
            if (m_Domain == null)
            {
                Debug.LogError("[Soldier] No DomainAsset assigned. Rebuild the scene.");
                enabled = false;
                return;
            }

            m_SoldierStart = m_Soldier.position;
            m_PlayerCtrl = m_Player.GetComponent<PlayerController>();

#if PLANNING_DEBUG
            PlanCacheDiagnostics.Enabled = true;
            PlanCacheDiagnostics.Reset();
#endif

            m_Service = new PlannerService(PlanFinders.BitVector(m_Domain.Actions));
            m_Loop = new PlannerLoop(m_Service);

            m_Executor = new PlanExecutor();
            m_Executor.RegisterExecutor("move", new SoldierMoveExecutor(m_Soldier, ResolveLocation, m_SoldierSpeed, _ => m_ShootRange));
            m_Executor.RegisterExecutor("shoot", new SoldierShootExecutor(m_Soldier, () => m_Player.position, InRange, OnSoldierFires, m_ShotWindup));

            m_Executor.OnActionStarted += action => Debug.Log($"[Soldier] start  {action.Name}");
            m_Executor.OnActionCompleted += action => Debug.Log($"[Soldier] done   {action.Name}");
            m_Executor.OnActionFailed += (action, reason) => Debug.Log($"[Soldier] failed {action.Name} ({reason})");
            m_Executor.OnPlanCompleted += () => Debug.Log("[Soldier] plan complete");
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
            m_Plan = null;
            Debug.Log("[Soldier] restart");
        }

        private void RequestPlan()
        {
            if (m_AwaitingPlan)
                return;

            m_AwaitingPlan = true;
            m_Service.Submit(BuildProblem(), OnPlanReady, priority: 0, ownerKey: "soldier1", replaceOwner: true);
        }

        private void OnPlanReady(PlanResult result)
        {
            m_AwaitingPlan = false;
            m_Plan = result;
            if (result.Success && result.Actions.Count > 0)
            {
                Debug.Log($"[Soldier] plan ({result.Actions.Count} steps): {PlanText(result)}");
                m_Executor.StartPlan(result);
            }
            else
            {
                Debug.Log("[Soldier] no plan found");
            }
        }

        private PlanningProblemDefinition BuildProblem()
        {
            return PlanningProblemDefinition.Builder("soldier-hunt")
                .Object("soldier1", "agent")
                .Object("enemy1", "enemy")
                .Object("rifle1", "weapon")
                .Object(Open, "location")
                .Object(Target, "location")
                .Initially("has-weapon", "soldier1", "rifle1")
                .Initially("weapon-loaded", "rifle1")
                .Initially("enemy-alive", "enemy1")
                .Initially("enemy-at", "enemy1", Target)
                .Initially("connected", Open, Target)
                .Initially("connected", Target, Open)
                .Initially("at", "soldier1", InRange() ? Target : Open)
                .GoalNot("enemy-alive", "enemy1")
                .Build();
        }

        private Vector3 ResolveLocation(string location)
        {
            return location == Target ? m_Player.position : m_SoldierStart;
        }

        private bool InRange()
        {
            return SoldierMoveExecutor.PlanarDistance(m_Soldier.position, m_Player.position) <= m_ShootRange;
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
                DemoGizmos.Circle(m_Soldier.position, m_ShootRange, InRange() ? Color.green : new Color(0.9f, 0.4f, 0.4f));
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 380, Screen.height - 24f));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label(m_PlayerCtrl != null && m_PlayerCtrl.Alive
                ? "WASD: move. Stay out of the soldier's range."
                : "ELIMINATED. Press R to restart.");

            GUILayout.Label($"Range: {m_ShootRange:0.0}   In range: {InRange()}");
            GUILayout.Label($"Executor: {(m_Executor != null ? m_Executor.State.ToString() : "-")}");
#if PLANNING_DEBUG
            GUILayout.Label($"Plan cache: {PlanCacheDiagnostics.Misses} miss / {PlanCacheDiagnostics.Hits} hit");
#endif

            GUILayout.Space(4);
            DemoHud.DrawPlan(m_Plan, m_Executor);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
