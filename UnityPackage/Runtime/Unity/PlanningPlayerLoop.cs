using System;
using AIInGames.Planning.Runtime;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace AIInGames.Planning.Unity
{
    public enum PlanningPlayerLoopPhase
    {
        PreUpdate,
        Update,
        PreLateUpdate
    }

    /// <summary>
    /// Main-thread sync point for planning queue pumps and result dispatch.
    /// </summary>
    public static class PlanningPlayerLoop
    {
        public static event Action Tick;

        public static bool IsInstalled { get; private set; }
        public static PlanningPlayerLoopPhase InstalledPhase { get; private set; }

        public static bool Install(PlanningPlayerLoopPhase phase = PlanningPlayerLoopPhase.PreUpdate)
        {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            RemoveSystem(ref loop, typeof(PlanningPlayerLoop));

            PlayerLoopSystem planningSystem = new PlayerLoopSystem
            {
                type = typeof(PlanningPlayerLoop),
                updateDelegate = Run
            };

            if (!AppendToPhase(ref loop, GetPhaseType(phase), planningSystem))
                return false;

            PlayerLoop.SetPlayerLoop(loop);
            IsInstalled = true;
            InstalledPhase = phase;
            return true;
        }

        public static void Uninstall()
        {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            RemoveSystem(ref loop, typeof(PlanningPlayerLoop));
            PlayerLoop.SetPlayerLoop(loop);
            IsInstalled = false;
        }

        internal static bool AppendToPhase(ref PlayerLoopSystem root, Type phaseType, PlayerLoopSystem system)
        {
            if (root.type == phaseType)
            {
                AppendChild(ref root, system);
                return true;
            }

            if (root.subSystemList == null)
                return false;

            for (int i = 0; i < root.subSystemList.Length; i++)
            {
                PlayerLoopSystem child = root.subSystemList[i];
                if (AppendToPhase(ref child, phaseType, system))
                {
                    root.subSystemList[i] = child;
                    return true;
                }
            }

            return false;
        }

        internal static bool RemoveSystem(ref PlayerLoopSystem root, Type systemType)
        {
            bool removed = false;

            if (root.subSystemList == null)
                return false;

            for (int i = root.subSystemList.Length - 1; i >= 0; i--)
            {
                if (root.subSystemList[i].type == systemType)
                {
                    RemoveChildAt(ref root, i);
                    removed = true;
                    continue;
                }

                PlayerLoopSystem child = root.subSystemList[i];
                if (RemoveSystem(ref child, systemType))
                {
                    root.subSystemList[i] = child;
                    removed = true;
                }
            }

            return removed;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            PlannerLog.Warning = Debug.LogWarning;
            PlannerLog.Info = Debug.Log;
            Tick = null;
            IsInstalled = false;
            InstalledPhase = PlanningPlayerLoopPhase.PreUpdate;
        }

        private static void Run()
        {
            Tick?.Invoke();
        }

        private static Type GetPhaseType(PlanningPlayerLoopPhase phase)
        {
            switch (phase)
            {
                case PlanningPlayerLoopPhase.Update:
                    return typeof(Update);
                case PlanningPlayerLoopPhase.PreLateUpdate:
                    return typeof(PreLateUpdate);
                default:
                    return typeof(PreUpdate);
            }
        }

        private static void AppendChild(ref PlayerLoopSystem parent, PlayerLoopSystem child)
        {
            int oldLength = parent.subSystemList?.Length ?? 0;
            PlayerLoopSystem[] systems = new PlayerLoopSystem[oldLength + 1];
            if (oldLength > 0)
                Array.Copy(parent.subSystemList, systems, oldLength);
            systems[oldLength] = child;
            parent.subSystemList = systems;
        }

        private static void RemoveChildAt(ref PlayerLoopSystem parent, int index)
        {
            int oldLength = parent.subSystemList.Length;
            PlayerLoopSystem[] systems = new PlayerLoopSystem[oldLength - 1];
            if (index > 0)
                Array.Copy(parent.subSystemList, 0, systems, 0, index);
            if (index < oldLength - 1)
                Array.Copy(parent.subSystemList, index + 1, systems, index, oldLength - index - 1);
            parent.subSystemList = systems;
        }
    }
}
