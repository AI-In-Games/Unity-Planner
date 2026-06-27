using AIInGames.Planning.Runtime;
using AIInGames.Planning.Unity;
using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// A free-movement chase where the soldier starts unarmed and must fetch a rifle before it can
    /// engage. It uses the extended soldier-armory domain (weapon-at plus a pick-up action). The plan
    /// becomes: go to the armory, pick up the rifle, then close in and shoot. On pickup the world
    /// rifle is hidden and a rifle prefab is dropped into the soldier's weapon slot. Actors are real
    /// prefabs, shots fire a bullet and an explosion, and ranges are gizmos. The player is a
    /// self-contained PlayerController.
    /// </summary>
    public sealed class SoldierArmoryDemo : MonoBehaviour
    {
        private const string Open = "open";
        private const string Armory = "armory";
        private const string Target = "target";

        [SerializeField] private DomainAsset m_Domain;
        [SerializeField] private Transform m_Soldier;
        [SerializeField] private Transform m_Player;
        [SerializeField] private Transform m_Armory;
        [SerializeField] private GameObject m_RiflePrefab;
        [SerializeField] private GameObject m_BulletPrefab;
        [SerializeField] private GameObject m_ExplosionPrefab;
        [SerializeField] private float m_SoldierSpeed = 3f;
        [SerializeField] private float m_ShootRange = 2.5f;
        [SerializeField] private float m_ArmoryRadius = 0.8f;
        [SerializeField] private float m_ShotWindup = 0.4f;
        [SerializeField] private float m_BulletSpeed = 14f;
        [SerializeField] private float m_ExplosionLife = 1.2f;

        private PlannerService m_Service;
        private PlannerLoop m_Loop;
        private PlanExecutor m_Executor;
        private PlanResult m_Plan;
        private PlayerController m_PlayerCtrl;

        private bool m_AwaitingPlan;
        private bool m_HasWeapon;
        private Vector3 m_SoldierStart;

        private void Start()
        {
            if (m_Domain == null)
            {
                Debug.LogError("[SoldierArmory] No DomainAsset assigned. Rebuild the scene or run Convert Demo Domains to Assets.");
                enabled = false;
                return;
            }

            m_SoldierStart = m_Soldier.position;
            m_PlayerCtrl = m_Player.GetComponent<PlayerController>();

            m_Service = new PlannerService(PlanFinders.BitVector(m_Domain.Actions));
            m_Loop = new PlannerLoop(m_Service);

            m_Executor = new PlanExecutor();
            m_Executor.RegisterExecutor("move", new SoldierMoveExecutor(m_Soldier, ResolveLocation, m_SoldierSpeed, ArrivalRadius));
            m_Executor.RegisterExecutor("pick-up", new SimulatedActionExecutor(0.4f));
            m_Executor.RegisterExecutor("shoot", new SoldierShootExecutor(m_Soldier, () => m_Player.position, InRange, OnSoldierFires, m_ShotWindup));
            m_Executor.RegisterExecutor("take-cover", new SimulatedActionExecutor(0.2f));
            m_Executor.RegisterExecutor("reload", new SimulatedActionExecutor(0.2f));
            m_Executor.RegisterExecutor("throw-grenade", new SimulatedActionExecutor(0.2f));

            m_Executor.OnActionStarted += action => Debug.Log($"[SoldierArmory] start  {action.Name}");
            m_Executor.OnActionCompleted += OnActionCompleted;
            m_Executor.OnActionFailed += (action, reason) => Debug.Log($"[SoldierArmory] failed {action.Name} ({reason})");
            m_Executor.OnPlanCompleted += () => Debug.Log("[SoldierArmory] plan complete");

            Debug.Log("[SoldierArmory] ready. The soldier is unarmed and must reach the armory first.");
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

        private void OnActionCompleted(GroundedAction action)
        {
            Debug.Log($"[SoldierArmory] done   {action.Name}");
            if (!action.Name.StartsWith("pick-up"))
                return;

            m_HasWeapon = true;
            if (m_Armory != null)
                m_Armory.gameObject.SetActive(false);
            EquipRifle();
        }

        private void EquipRifle()
        {
            if (m_RiflePrefab == null)
                return;
            Transform slot = FindChild(m_Soldier, "RifleSlot");
            if (slot == null)
                return;
            GameObject rifle = Instantiate(m_RiflePrefab, slot);
            rifle.transform.localPosition = Vector3.zero;
            rifle.transform.localRotation = Quaternion.identity;
        }

        private void Restart()
        {
            m_Executor.Cancel();
            m_Soldier.position = m_SoldierStart;
            m_PlayerCtrl.ResetState();
            m_AwaitingPlan = false;
            m_HasWeapon = false;
            m_Plan = null;

            if (m_Armory != null)
                m_Armory.gameObject.SetActive(true);
            Transform slot = FindChild(m_Soldier, "RifleSlot");
            if (slot != null)
                for (int i = slot.childCount - 1; i >= 0; i--)
                    Destroy(slot.GetChild(i).gameObject);

            Debug.Log("[SoldierArmory] restart");
        }

        private void RequestPlan()
        {
            if (m_AwaitingPlan)
                return;

            Debug.Log($"[SoldierArmory] replanning: at={AtLocation()}, has-weapon={m_HasWeapon}");
            m_AwaitingPlan = true;
            m_Service.Submit(BuildProblem(), OnPlanReady, priority: 0, ownerKey: "soldier1", replaceOwner: true);
        }

        private void OnPlanReady(PlanResult result)
        {
            m_AwaitingPlan = false;
            m_Plan = result;
            if (result.Success && result.Actions.Count > 0)
            {
                Debug.Log($"[SoldierArmory] plan ({result.Actions.Count} steps): {PlanText(result)}");
                m_Executor.StartPlan(result);
            }
            else
            {
                Debug.Log("[SoldierArmory] no plan found");
            }
        }

        private PlanningProblemDefinition BuildProblem()
        {
            PlanningProblemBuilder builder = PlanningProblemDefinition.Builder("soldier-armory")
                .Object("soldier1", "agent")
                .Object("enemy1", "enemy")
                .Object("rifle1", "weapon")
                .Object(Open, "location")
                .Object(Armory, "location")
                .Object(Target, "location")
                .Initially("weapon-loaded", "rifle1")
                .Initially("enemy-alive", "enemy1")
                .Initially("enemy-at", "enemy1", Target)
                .Initially("connected", Open, Armory)
                .Initially("connected", Armory, Open)
                .Initially("connected", Open, Target)
                .Initially("connected", Target, Open)
                .Initially("connected", Armory, Target)
                .Initially("connected", Target, Armory)
                .Initially("at", "soldier1", AtLocation());

            if (m_HasWeapon)
                builder.Initially("has-weapon", "soldier1", "rifle1");
            else
                builder.Initially("weapon-at", "rifle1", Armory);

            builder.GoalNot("enemy-alive", "enemy1");
            return builder.Build();
        }

        private Vector3 ResolveLocation(string location)
        {
            if (location == Target)
                return m_Player.position;
            if (location == Armory)
                return m_Armory.position;
            return m_SoldierStart;
        }

        private float ArrivalRadius(string location)
        {
            if (location == Target)
                return m_ShootRange;
            if (location == Armory)
                return m_ArmoryRadius;
            return 0.5f;
        }

        private string AtLocation()
        {
            if (m_Armory != null && SoldierMoveExecutor.PlanarDistance(m_Soldier.position, m_Armory.position) <= m_ArmoryRadius)
                return Armory;
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

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name)
                    return child;
            return null;
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
            if (m_Armory != null)
                DemoGizmos.Circle(m_Armory.position, m_ArmoryRadius, new Color(0.95f, 0.75f, 0.3f));
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 400, Screen.height - 24f));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label(m_PlayerCtrl != null && m_PlayerCtrl.Alive
                ? "WASD: move. The soldier is unarmed; it must reach the armory before it can shoot."
                : "ELIMINATED. Press R to restart.");

            GUILayout.Label($"At: {AtLocation()}   Has weapon: {m_HasWeapon}   Executor: {(m_Executor != null ? m_Executor.State.ToString() : "-")}");

            GUILayout.Space(4);
            DemoHud.DrawPlan(m_Plan, m_Executor);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
