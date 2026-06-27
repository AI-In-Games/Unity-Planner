using System.Collections.Generic;

namespace AIInGames.Planning.Runtime
{
    /// <summary>
    /// Converts a PlanningState-based domain into bit-vector form for fast search.
    /// Only predicate-based state is compiled; typed values (Int, Float, Bool, String keys) are ignored.
    /// </summary>
    public static class DomainCompiler
    {
        public static CompiledDomain Compile(PlanningState initial, PlanningState goal, List<GroundedAction> actions)
        {
            PredicateIndex index = BuildIndex(initial, goal, actions);
            BitState initialState = CompileState(index, initial);
            BuildGoalMasks(index, goal, out ulong[] goalTrueMask, out ulong[] goalFalseMask,
                out int[] goalTruePredicates, out int[] goalFalsePredicates);

            CompiledAction[] compiledActions = new CompiledAction[actions.Count];
            for (int i = 0; i < actions.Count; i++)
                compiledActions[i] = CompileAction(index, actions[i], i);

            int predCount = index.Count;
            List<int>[] trueAchieverLists = new List<int>[predCount];
            List<int>[] falseAchieverLists = new List<int>[predCount];
            for (int i = 0; i < predCount; i++)
            {
                trueAchieverLists[i] = new List<int>();
                falseAchieverLists[i] = new List<int>();
            }

            for (int ai = 0; ai < compiledActions.Length; ai++)
            {
                CompiledAction ca = compiledActions[ai];
                for (int pi = 0; pi < predCount; pi++)
                {
                    int word = PredicateIndex.WordOf(pi);
                    ulong bit = PredicateIndex.MaskOf(pi);
                    if ((ca.AddMask[word] & bit) != 0)
                        trueAchieverLists[pi].Add(ai);
                    if ((ca.RemoveMask[word] & bit) != 0)
                        falseAchieverLists[pi].Add(ai);
                }
            }

            int[][] achieversForTrue = new int[predCount][];
            int[][] achieversForFalse = new int[predCount][];
            for (int i = 0; i < predCount; i++)
            {
                achieversForTrue[i] = trueAchieverLists[i].ToArray();
                achieversForFalse[i] = falseAchieverLists[i].ToArray();
            }

            GroundedAction[] originalActionsArray = actions.ToArray();

            return new CompiledDomain(index, initialState, goalTrueMask, goalFalseMask,
                goalTruePredicates, goalFalsePredicates,
                compiledActions, originalActionsArray, achieversForTrue, achieversForFalse);
        }

        internal static PredicateIndex BuildIndex(PlanningState initial, PlanningState goal, List<GroundedAction> actions)
        {
            PredicateIndex index = new PredicateIndex();

            foreach (KeyValuePair<string, bool> kvp in initial.Predicates)
                index.Register(kvp.Key);

            foreach (KeyValuePair<string, bool> kvp in goal.Predicates)
                index.Register(kvp.Key);

            for (int i = 0; i < actions.Count; i++)
            {
                foreach (KeyValuePair<string, bool> kvp in actions[i].Preconditions.Predicates)
                    index.Register(kvp.Key);
                foreach (KeyValuePair<string, bool> kvp in actions[i].Effects.Predicates)
                    index.Register(kvp.Key);
            }

            return index;
        }

        internal static BitState CompileState(PredicateIndex index, PlanningState state)
        {
            int wordCount = index.WordCount;
            ulong[] words = wordCount > 0 ? new ulong[wordCount] : System.Array.Empty<ulong>();

            foreach (KeyValuePair<string, bool> kvp in state.Predicates)
            {
                if (kvp.Value && index.TryGetIndex(kvp.Key, out int bitIndex))
                    words[PredicateIndex.WordOf(bitIndex)] |= PredicateIndex.MaskOf(bitIndex);
            }

            return new BitState(words);
        }

        internal static void BuildGoalMasks(PredicateIndex index, PlanningState goal,
            out ulong[] trueMask, out ulong[] falseMask,
            out int[] truePredicates, out int[] falsePredicates)
        {
            int wordCount = index.WordCount;
            if (wordCount == 0)
            {
                trueMask = System.Array.Empty<ulong>();
                falseMask = System.Array.Empty<ulong>();
                truePredicates = System.Array.Empty<int>();
                falsePredicates = System.Array.Empty<int>();
                return;
            }

            trueMask = new ulong[wordCount];
            falseMask = new ulong[wordCount];
            List<int> trueList = new List<int>();
            List<int> falseList = new List<int>();

            foreach (KeyValuePair<string, bool> kvp in goal.Predicates)
            {
                if (!index.TryGetIndex(kvp.Key, out int bitIndex))
                    continue;

                int word = PredicateIndex.WordOf(bitIndex);
                ulong bit = PredicateIndex.MaskOf(bitIndex);

                if (kvp.Value)
                {
                    trueMask[word] |= bit;
                    trueList.Add(bitIndex);
                }
                else
                {
                    falseMask[word] |= bit;
                    falseList.Add(bitIndex);
                }
            }

            truePredicates = trueList.ToArray();
            falsePredicates = falseList.ToArray();
        }

        internal static CompiledAction CompileAction(PredicateIndex index, GroundedAction action, int originalIndex)
        {
            int wordCount = index.WordCount;
            ulong[] precondTrue;
            ulong[] precondFalse;
            ulong[] addMask;
            ulong[] removeMask;

            if (wordCount == 0)
            {
                precondTrue = System.Array.Empty<ulong>();
                precondFalse = System.Array.Empty<ulong>();
                addMask = System.Array.Empty<ulong>();
                removeMask = System.Array.Empty<ulong>();
            }
            else
            {
                precondTrue = new ulong[wordCount];
                precondFalse = new ulong[wordCount];
                addMask = new ulong[wordCount];
                removeMask = new ulong[wordCount];
            }

            foreach (KeyValuePair<string, bool> kvp in action.Preconditions.Predicates)
            {
                if (!index.TryGetIndex(kvp.Key, out int bitIndex))
                    continue;

                int word = PredicateIndex.WordOf(bitIndex);
                ulong bit = PredicateIndex.MaskOf(bitIndex);

                if (kvp.Value)
                    precondTrue[word] |= bit;
                else
                    precondFalse[word] |= bit;
            }

            foreach (KeyValuePair<string, bool> kvp in action.Effects.Predicates)
            {
                if (!index.TryGetIndex(kvp.Key, out int bitIndex))
                    continue;

                int word = PredicateIndex.WordOf(bitIndex);
                ulong bit = PredicateIndex.MaskOf(bitIndex);

                if (kvp.Value)
                    addMask[word] |= bit;
                else
                    removeMask[word] |= bit;
            }

            return new CompiledAction(originalIndex, action.Cost, precondTrue, precondFalse, addMask, removeMask);
        }
    }
}
