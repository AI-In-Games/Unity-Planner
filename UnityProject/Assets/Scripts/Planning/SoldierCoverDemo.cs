using AIInGames.Planning.Runtime;
using AIInGames.Planning.Unity;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Step 2: the soldier starts with an unloaded weapon, so the planner must reach the crate cover,
    /// take cover, and reload before it can shoot. Adds a third location and two non-spatial facts
    /// (in-cover, weapon-loaded) tracked here and updated as actions complete. The player is a
    /// self-contained PlayerController.
    /// </summary>
    public sealed class SoldierCoverDemo : MonoBehaviour
    {
        private const string Open = "open";
        private const string Cover = "cover";
        private const string Target = "target";

        [SerializeField] private DomainAsset m_Domain;
        [SerializeField] private Transform m_Soldier;
        [SerializeField] private Transform m_Player;
        [SerializeField] private Transform m_Cover;
        [SerializeField] private GameObject m_BulletPrefab;
        [SerializeField] private GameObject m_ExplosionPrefab;
        [SerializeField] private float m_SoldierSpeed = 3f;
        [SerializeField] private float m_ShootRange = 2.5f;
        [SerializeField] private float m_CoverRadius = 1.6f;
        [SerializeField] private float m_ShotWindup = 0.4f;
        [SerializeField] private float m_BulletSpeed = 14f;
        [SerializeField] private float m_ExplosionLife = 1.2f;

        private PlannerService m_Service;
        private PlannerLoop m_Loop;
        private PlanExecutor m_Executor;
        private PlanResult m_Plan;
        private PlayerController m_PlayerCtrl;

        private bool m_AwaitingPlan;
        private bool m_WeaponLoaded;
        private bool m_InCover;
        private Vector3 m_SoldierStart;

        private void Start()
        {
            if (m_Domain == null)
            {
                Debug.LogError("[SoldierCover] No DomainAsset assigned. Rebuild the scene.");
                enabled = false;
                return;
            }

            m_SoldierStart = m_Soldier.position;
            m_PlayerCtrl = m_Player.GetComponent<PlayerController>();

            m_Service = new PlannerService(PlanFinders.BitVector(m_Domain.Actions));
            m_Loop = new PlannerLoop(m_Service);

            m_Executor = new PlanExecutor();
            m_Executor.RegisterExecutor("move", new SoldierMoveExecutor(m_Soldier, ResolveLocation, m_SoldierSpeed, ArrivalRadius));
            m_Executor.RegisterExecutor("take-cover", new SimulatedActionExecutor(0.4f));
            m_Executor.RegisterExecutor("reload", new SimulatedActionExecutor(0.6f));
            m_Executor.RegisterExecutor("shoot", new SoldierShootExecutor(m_Soldier, () => m_Player.position, InRange, OnSoldierFires, m_ShotWindup));
            m_Executor.RegisterExecutor("throw-grenade", new SimulatedActionExecutor(0.2f));

            m_Executor.OnActionStarted += OnActionStarted;
            m_Executor.OnActionCompleted += OnActionCompleted;
            m_Executor.OnActionFailed += (action, reason) => Debug.Log($"[SoldierCover] failed {action.Name} ({reason})");
            m_Executor.OnPlanCompleted += () => Debug.Log("[SoldierCover] plan complete");

            Debug.Log("[SoldierCover] ready. The soldier is unloaded and must reach cover and reload first.");
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

        private void OnActionStarted(GroundedAction action)
        {
            Debug.Log($"[SoldierCover] start  {action.Name}");
            if (action.Name.StartsWith("move"))
                m_InCover = false;
        }

        private void OnActionCompleted(GroundedAction action)
        {
            Debug.Log($"[SoldierCover] done   {action.Name}");
            if (action.Name.StartsWith("take-cover"))
                m_InCover = true;
            else if (action.Name.StartsWith("reload"))
                m_WeaponLoaded = true;
        }

        private void Restart()
        {
            m_Executor.Cancel();
            m_Soldier.position = m_SoldierStart;
            m_PlayerCtrl.ResetState();
            m_AwaitingPlan = false;
            m_WeaponLoaded = false;
            m_InCover = false;
            m_Plan = null;
            Debug.Log("[SoldierCover] restart");
        }

        private void RequestPlan()
        {
            if (m_AwaitingPlan)
                return;

            Debug.Log($"[SoldierCover] replanning: at={AtLocation()}, loaded={m_WeaponLoaded}, in-cover={m_InCover}");
            m_AwaitingPlan = true;
            m_Service.Submit(BuildProblem(), OnPlanReady, priority: 0, ownerKey: "soldier1", replaceOwner: true);
        }

        private void OnPlanReady(PlanResult result)
        {
            m_AwaitingPlan = false;
            m_Plan = result;
            if (result.Success && result.Actions.Count > 0)
            {
                Debug.Log($"[SoldierCover] plan ({result.Actions.Count} steps): {PlanText(result)}");
                m_Executor.StartPlan(result);
            }
            else
            {
                Debug.Log("[SoldierCover] no plan found");
            }
        }

        private PlanningProblemDefinition BuildProblem()
        {
            PlanningProblemBuilder builder = PlanningProblemDefinition.Builder("soldier-cover")
                .Object("soldier1", "agent")
                .Object("enemy1", "enemy")
                .Object("rifle1", "weapon")
                .Object(Open, "location")
                .Object(Cover, "location")
                .Object(Target, "location")
                .Initially("has-weapon", "soldier1", "rifle1")
                .Initially("enemy-alive", "enemy1")
                .Initially("enemy-at", "enemy1", Target)
                .Initially("cover-at", Cover)
                .Initially("connected", Open, Cover)
                .Initially("connected", Cover, Open)
                .Initially("connected", Open, Target)
                .Initially("connected", Target, Open)
                .Initially("connected", Cover, Target)
                .Initially("connected", Target, Cover)
                .Initially("at", "soldier1", AtLocation());

            if (m_WeaponLoaded)
                builder.Initially("weapon-loaded", "rifle1");
            if (m_InCover)
                builder.Initially("in-cover", "soldier1");

            builder.GoalNot("enemy-alive", "enemy1");
            return builder.Build();
        }

        private Vector3 ResolveLocation(string location)
        {
            if (location == Target)
                return m_Player.position;
            if (location == Cover)
                return m_Cover.position;
            return m_SoldierStart;
        }

        private float ArrivalRadius(string location)
        {
            if (location == Target)
                return m_ShootRange;
            if (location == Cover)
                return m_CoverRadius;
            return 0.5f;
        }

        private string AtLocation()
        {
            if (SoldierMoveExecutor.PlanarDistance(m_Soldier.position, m_Cover.position) <= m_CoverRadius)
                return Cover;
            if (InRange())
                return Target;
            return Open;
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
            if (m_Cover != null)
                DemoGizmos.Circle(m_Cover.position, m_CoverRadius, new Color(0.6f, 0.8f, 0.95f));
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 400, Screen.height - 24f));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label(m_PlayerCtrl != null && m_PlayerCtrl.Alive
                ? "WASD: move. The soldier must reach cover and reload before it can shoot."
                : "ELIMINATED. Press R to restart.");

            GUILayout.Label($"At: {AtLocation()}   Loaded: {m_WeaponLoaded}   In cover: {m_InCover}");
            GUILayout.Label($"Executor: {(m_Executor != null ? m_Executor.State.ToString() : "-")}");

            GUILayout.Space(4);
            DemoHud.DrawPlan(m_Plan, m_Executor);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
