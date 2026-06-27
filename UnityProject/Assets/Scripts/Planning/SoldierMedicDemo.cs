using AIInGames.Planning.Runtime;
using AIInGames.Planning.Unity;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// A duel that shows goal priority by arbitration. The planner pursues a single hard goal at a
    /// time, so "survive" is made the higher-priority goal in code: while healthy the soldier's goal
    /// is to kill you, but once its health drops to low it commits to a heal goal (as far as the
    /// medpacks left allow) and retreats, then resumes hunting. The domain is uniform cost with one shoot action; the priority
    /// lives entirely in which goal the agent submits each replan. Two single-use medpacks each raise
    /// the soldier one band, so a full recovery (low -> mid -> full) visits both. Hold Space to drain
    /// its health bar. The domain is a plain PDDL DomainAsset (uniform cost); only the goal selection
    /// is special. The player is a self-contained PlayerController whose hits are applied through
    /// TargetHit.
    /// </summary>
    public sealed class SoldierMedicDemo : MonoBehaviour
    {
        private const float FullThreshold = 0.6f;
        private const float MidThreshold = 0.3f;

        private const string Open = "open";
        private const string MedA = "med-a";
        private const string MedB = "med-b";
        private const string Target = "target";

        [SerializeField] private DomainAsset m_Domain;
        [SerializeField] private Transform m_Soldier;
        [SerializeField] private Transform m_Player;
        [SerializeField] private Transform m_MedA;
        [SerializeField] private Transform m_MedB;
        [SerializeField] private GameObject m_BulletPrefab;
        [SerializeField] private GameObject m_ExplosionPrefab;
        [SerializeField] private float m_SoldierSpeed = 3f;
        [SerializeField] private float m_ShootRange = 2.5f;
        [SerializeField] private float m_MedRadius = 0.8f;
        [SerializeField] private float m_ShotWindup = 0.5f;
        [SerializeField] private float m_BulletSpeed = 14f;
        [SerializeField] private float m_ExplosionLife = 1.2f;
        [SerializeField] private float m_ShotDamage = 0.08f;

        private PlannerService m_Service;
        private PlannerLoop m_Loop;
        private PlanExecutor m_Executor;
        private PlanResult m_Plan;
        private PlayerController m_PlayerCtrl;

        private bool m_AwaitingPlan;
        private bool m_Stale = true;
        private bool m_SoldierAlive = true;
        private bool m_Healing;
        private float m_Health = 1f;
        private string m_LastBand;
        private Vector3 m_SoldierStart;

        private void Start()
        {
            if (m_Domain == null)
            {
                Debug.LogError("[Medic] No DomainAsset assigned. Rebuild the scene.");
                enabled = false;
                return;
            }

            m_SoldierStart = m_Soldier.position;
            m_PlayerCtrl = m_Player.GetComponent<PlayerController>();
            if (m_PlayerCtrl != null)
                m_PlayerCtrl.TargetHit += ApplyPlayerDamage;
            m_LastBand = HealthBand();

            m_Service = new PlannerService(PlanFinders.BitVector(m_Domain.Actions));
            m_Loop = new PlannerLoop(m_Service);

            m_Executor = new PlanExecutor();
            m_Executor.RegisterExecutor("move", new SoldierMoveExecutor(m_Soldier, ResolveLocation, m_SoldierSpeed, ArrivalRadius));
            m_Executor.RegisterExecutor("heal", new SimulatedActionExecutor(0.6f));
            m_Executor.RegisterExecutor("shoot", new SoldierShootExecutor(m_Soldier, () => m_Player.position, InRange, OnSoldierFires, m_ShotWindup));

            m_Executor.OnActionStarted += action => Debug.Log($"[Medic] start  {action.Name}");
            m_Executor.OnActionCompleted += OnActionCompleted;
            m_Executor.OnActionFailed += (action, reason) => Debug.Log($"[Medic] failed {action.Name} ({reason})");
            m_Executor.OnPlanCompleted += () => Debug.Log("[Medic] plan complete");

            Debug.Log("[Medic] ready. It hunts you, but survival is the higher-priority goal: when low it heals to full first.");
        }

        private void Update()
        {
            if (m_PlayerCtrl == null || !m_PlayerCtrl.Alive || !m_SoldierAlive)
            {
                if (Input.GetKeyDown(KeyCode.R))
                    Restart();
                return;
            }

            m_Executor.Update();

            if (m_Stale)
            {
                m_Stale = false;
                m_Executor.Cancel();
                RequestPlan();
                return;
            }

            if (!m_AwaitingPlan && m_Executor.State != PlanExecutor.ExecutionState.Executing)
                RequestPlan();
        }

        private void OnDestroy()
        {
            if (m_PlayerCtrl != null)
                m_PlayerCtrl.TargetHit -= ApplyPlayerDamage;
            m_Loop?.Dispose();
        }

        private void ApplyPlayerDamage()
        {
            if (!m_SoldierAlive)
                return;

            string before = HealthBand();
            m_Health -= m_ShotDamage;
            if (m_Health <= 0f)
            {
                m_Health = 0f;
                m_SoldierAlive = false;
                Debug.Log("[Medic] the soldier is down. You win.");
                return;
            }

            if (HealthBand() != before)
                m_Stale = true;
        }

        private void Restart()
        {
            m_Executor.Cancel();
            m_Soldier.position = m_SoldierStart;
            m_PlayerCtrl.ResetState();

            if (m_MedA != null)
                m_MedA.gameObject.SetActive(true);
            if (m_MedB != null)
                m_MedB.gameObject.SetActive(true);

            m_SoldierAlive = true;
            m_Healing = false;
            m_Health = 1f;
            m_LastBand = HealthBand();
            m_AwaitingPlan = false;
            m_Stale = true;
            m_Plan = null;
            Debug.Log("[Medic] restart");
        }

        // Goal arbitration: survival outranks the kill. Commit to healing once low, but only while a
        // heal is actually achievable with the medpacks left, and hold the goal through a multi-step
        // recovery so the soldier does not flip back to fighting after a single pack. With no usable
        // pack it stays in fight mode, so it never commits to an unreachable heal goal.
        private void UpdateGoalMode()
        {
            if (!m_Healing)
            {
                if (HealthBand() == "health-low" && SurviveGoalBand() != null)
                    m_Healing = true;
            }
            else if (HealthBand() == "health-full" || SurviveGoalBand() == null)
            {
                m_Healing = false;
            }
        }

        // The best health band reachable from the current one with the medpacks still available, or
        // null when no heal is possible. Each heal consumes one pack and raises the soldier one band.
        private string SurviveGoalBand()
        {
            int packs = (PackReady(m_MedA) ? 1 : 0) + (PackReady(m_MedB) ? 1 : 0);
            string band = HealthBand();
            if (band == "health-low")
                return packs >= 2 ? "health-full" : (packs == 1 ? "health-mid" : null);
            if (band == "health-mid")
                return packs >= 1 ? "health-full" : null;
            return null;
        }

        private void RequestPlan()
        {
            if (m_AwaitingPlan)
                return;

            UpdateGoalMode();
            m_LastBand = HealthBand();
            Debug.Log($"[Medic] replanning: at={AtLocation()}, health={m_LastBand}, goal={(m_Healing ? "survive" : "kill")}");
            m_AwaitingPlan = true;
            m_Service.Submit(BuildProblem(), OnPlanReady, priority: 0, ownerKey: "soldier1", replaceOwner: true);
        }

        private void OnPlanReady(PlanResult result)
        {
            m_AwaitingPlan = false;
            m_Plan = result;

            if (result.Success && result.Actions.Count > 0)
            {
                Debug.Log($"[Medic] plan ({result.Actions.Count} steps): {PlanText(result)}");
                m_Executor.StartPlan(result);
            }
            else
            {
                Debug.Log("[Medic] no plan found");
            }
        }

        private void OnActionCompleted(GroundedAction action)
        {
            Debug.Log($"[Medic] done   {action.Name}");

            if (action.Name.StartsWith("heal-to-mid"))
            {
                m_Health = 0.5f;
                ConsumePack(action.Argument("?l"));
            }
            else if (action.Name.StartsWith("heal-to-full"))
            {
                m_Health = 1f;
                ConsumePack(action.Argument("?l"));
            }
        }

        private void ConsumePack(string location)
        {
            if (location == MedA && m_MedA != null)
                m_MedA.gameObject.SetActive(false);
            else if (location == MedB && m_MedB != null)
                m_MedB.gameObject.SetActive(false);
        }

        private PlanningProblemDefinition BuildProblem()
        {
            PlanningProblemBuilder builder = PlanningProblemDefinition.Builder("soldier-medic")
                .Object("soldier1", "agent")
                .Object("enemy1", "enemy")
                .Object(Open, "location")
                .Object(MedA, "location")
                .Object(MedB, "location")
                .Object(Target, "location")
                .Initially("enemy-alive", "enemy1")
                .Initially("enemy-at", "enemy1", Target)
                .Initially("at", "soldier1", AtLocation())
                .Initially(HealthBand(), "soldier1");

            string[] locations = { Open, MedA, MedB, Target };
            for (int i = 0; i < locations.Length; i++)
                for (int j = 0; j < locations.Length; j++)
                    if (i != j)
                        builder.Initially("connected", locations[i], locations[j]);

            if (PackReady(m_MedA))
                builder.Initially("medpack-at", MedA);
            if (PackReady(m_MedB))
                builder.Initially("medpack-at", MedB);

            string survive = m_Healing ? SurviveGoalBand() : null;
            if (survive != null)
                builder.Goal(survive, "soldier1");
            else
                builder.GoalNot("enemy-alive", "enemy1");

            return builder.Build();
        }

        private static bool PackReady(Transform station)
        {
            return station != null && station.gameObject.activeSelf;
        }

        private string HealthBand()
        {
            if (m_Health > FullThreshold)
                return "health-full";
            if (m_Health > MidThreshold)
                return "health-mid";
            return "health-low";
        }

        private Vector3 ResolveLocation(string location)
        {
            if (location == Target)
                return m_Player.position;
            if (location == MedA)
                return m_MedA.position;
            if (location == MedB)
                return m_MedB.position;
            return m_SoldierStart;
        }

        private float ArrivalRadius(string location)
        {
            if (location == Target)
                return m_ShootRange;
            if (location == MedA || location == MedB)
                return m_MedRadius;
            return 0.5f;
        }

        private string AtLocation()
        {
            if (PackReady(m_MedA) && SoldierMoveExecutor.PlanarDistance(m_Soldier.position, m_MedA.position) <= m_MedRadius)
                return MedA;
            if (PackReady(m_MedB) && SoldierMoveExecutor.PlanarDistance(m_Soldier.position, m_MedB.position) <= m_MedRadius)
                return MedB;
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
            if (PackReady(m_MedA))
                DemoGizmos.Circle(m_MedA.position, m_MedRadius, new Color(0.4f, 0.85f, 0.45f));
            if (PackReady(m_MedB))
                DemoGizmos.Circle(m_MedB.position, m_MedRadius, new Color(0.4f, 0.85f, 0.45f));
        }

        private void OnGUI()
        {
            DrawHealthBar();

            GUILayout.BeginArea(new Rect(12, 12, 430, Screen.height - 24f));
            GUILayout.BeginVertical(GUI.skin.box);

            if (!m_SoldierAlive)
                GUILayout.Label("The soldier is down. You win. Press R to restart.");
            else if (m_PlayerCtrl == null || !m_PlayerCtrl.Alive)
                GUILayout.Label("ELIMINATED. Press R to restart.");
            else
                GUILayout.Label("WASD: move. Hold Space: fire (any range). Drop it to win.");

            GUILayout.Label($"Soldier health: {Mathf.RoundToInt(m_Health * 100f)}% ({HealthBand().Replace("health-", "")})   Goal: {(m_Healing ? "survive" : "kill")}");
            GUILayout.Label($"Medpacks: A {(PackReady(m_MedA) ? "ready" : "used")}, B {(PackReady(m_MedB) ? "ready" : "used")}");
            GUILayout.Label("Survival outranks the kill: when low it heals as far as the medpacks allow, else it fights.");

            GUILayout.Space(4);
            DemoHud.DrawPlan(m_Plan, m_Executor);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawHealthBar()
        {
            if (!m_SoldierAlive || m_Soldier == null)
                return;
            Camera camera = Camera.main;
            if (camera == null)
                return;

            Vector3 screen = camera.WorldToScreenPoint(m_Soldier.position + Vector3.up * 1.3f);
            if (screen.z <= 0f)
                return;

            const float width = 56f;
            const float height = 8f;
            float x = screen.x - width * 0.5f;
            float y = Screen.height - screen.y;

            Color previous = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x - 1f, y - 1f, width + 2f, height + 2f), Texture2D.whiteTexture);
            GUI.color = new Color(0.3f, 0.05f, 0.05f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = m_Health > FullThreshold ? new Color(0.3f, 0.85f, 0.3f) : (m_Health > MidThreshold ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f));
            GUI.DrawTexture(new Rect(x, y, width * Mathf.Clamp01(m_Health), height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
