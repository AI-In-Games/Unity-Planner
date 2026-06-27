using System.Collections.Generic;
using System.Text;
using AIInGames.Planning.Runtime;
using UnityEngine;

namespace AIInGames.Planning.Unity
{
    public static class ProblemSerializer
    {
        public static string ToJson(ProblemData problem, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(problem, prettyPrint);
        }

        public static ProblemData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new ProblemData();

            ProblemData result = JsonUtility.FromJson<ProblemData>(json);
            return result ?? new ProblemData();
        }

        /// <summary>
        /// Parses a PDDL problem text and returns the equivalent ProblemData.
        /// Returns false and sets error if parsing or conversion fails.
        /// </summary>
        public static bool TryFromPddl(string pddlText, out ProblemData data, out string error)
        {
            if (!PddlProblemImporter.TryParse(pddlText, out PlanningProblemDefinition problem, out error))
            {
                data = null;
                return false;
            }

            data = ToProblemData(problem);
            return true;
        }

        public static PlanningProblemDefinition ToProblemDefinition(ProblemData data)
        {
            data ??= new ProblemData();

            PlanningProblemBuilder builder = PlanningProblemDefinition.Builder(data.name);

            if (data.objects != null)
            {
                for (int i = 0; i < data.objects.Count; i++)
                {
                    ProblemObjectData obj = data.objects[i];
                    builder.Object(obj.name, string.IsNullOrEmpty(obj.type) ? "object" : obj.type);
                }
            }

            ApplyPredicates(data.initial, builder.InitialPredicate);
            ApplyPredicates(data.goal, builder.GoalPredicate);

            return builder.Build();
        }

        public static ProblemData ToProblemData(PlanningProblemDefinition problem, string domainName = null)
        {
            ProblemData data = new ProblemData
            {
                name = problem.Name,
                domain = string.IsNullOrEmpty(domainName) ? "new-domain" : domainName
            };

            for (int i = 0; i < problem.Objects.Count; i++)
            {
                PlanningObject obj = problem.Objects[i];
                data.objects.Add(new ProblemObjectData { name = obj.Name, type = obj.Type });
            }

            AddPredicates(data.initial, problem.InitialState);
            AddPredicates(data.goal, problem.GoalState);
            return data;
        }

        public static ProblemData ToProblemData(ProblemAsset asset)
        {
            ProblemData data = new ProblemData
            {
                name = asset.ProblemName,
                domain = asset.Domain != null ? asset.Domain.DomainName : "new-domain"
            };

            for (int i = 0; i < asset.Objects.Count; i++)
            {
                ObjectDefinition obj = asset.Objects[i];
                data.objects.Add(new ProblemObjectData { name = obj.ObjectName, type = obj.ObjectType });
            }

            for (int i = 0; i < asset.InitialState.Count; i++)
                AddConditionNode(data.initial, asset.InitialState[i], true);

            for (int i = 0; i < asset.GoalState.Count; i++)
                AddConditionNode(data.goal, asset.GoalState[i], true);

            return data;
        }

        public static void Apply(ProblemData data, ProblemAsset asset, DomainAsset domain = null)
        {
            asset.ProblemName = data.name;
            asset.Domain = domain;

            asset.Objects.Clear();
            if (data.objects != null)
            {
                for (int i = 0; i < data.objects.Count; i++)
                {
                    ProblemObjectData obj = data.objects[i];
                    asset.Objects.Add(new ObjectDefinition(obj.name, obj.type));
                }
            }

            asset.InitialState.Clear();
            AddConditionNodes(asset.InitialState, data.initial);

            asset.GoalState.Clear();
            AddConditionNodes(asset.GoalState, data.goal);
            asset.RegeneratePddl();
        }

        public static string ToPddl(ProblemData data)
        {
            data ??= new ProblemData();

            StringBuilder sb = new StringBuilder();
            sb.Append("(define (problem "); sb.Append(data.name); sb.AppendLine(")");
            sb.Append("  (:domain "); sb.Append(data.domain); sb.AppendLine(")");

            if (data.objects != null && data.objects.Count > 0)
            {
                sb.AppendLine("  (:objects");
                for (int i = 0; i < data.objects.Count; i++)
                {
                    ProblemObjectData obj = data.objects[i];
                    sb.Append("    "); sb.Append(obj.name);
                    if (!string.IsNullOrEmpty(obj.type) && obj.type != "object")
                    {
                        sb.Append(" - "); sb.Append(obj.type);
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("  )");
            }

            sb.AppendLine("  (:init");
            AppendPredicateList(sb, data.initial, 2);
            sb.AppendLine("  )");

            sb.Append("  (:goal ");
            AppendGoal(sb, data.goal);
            sb.AppendLine(")");
            sb.Append(")");
            return sb.ToString();
        }

        private delegate PlanningProblemBuilder PredicateSetter(string predicate, bool value, params string[] args);

        private static void ApplyPredicates(List<ProblemPredicateData> predicates, PredicateSetter setter)
        {
            if (predicates == null)
                return;

            for (int i = 0; i < predicates.Count; i++)
            {
                ProblemPredicateData pred = predicates[i];
                setter(pred.predicate, pred.value, pred.args?.ToArray() ?? new string[0]);
            }
        }

        private static void AddPredicates(List<ProblemPredicateData> target, PlanningState state)
        {
            foreach (KeyValuePair<string, bool> kvp in state.Predicates)
            {
                ProblemPredicateData pred = ParsePredicateKey(kvp.Key);
                pred.value = kvp.Value;
                target.Add(pred);
            }
        }

        private static ProblemPredicateData ParsePredicateKey(string key)
        {
            ProblemPredicateData data = new ProblemPredicateData();
            int open = key.IndexOf('(');
            int close = key.LastIndexOf(')');
            if (open < 0 || close <= open)
            {
                data.predicate = key;
                return data;
            }

            data.predicate = key.Substring(0, open);
            string args = key.Substring(open + 1, close - open - 1);
            if (!string.IsNullOrEmpty(args))
                data.args.AddRange(args.Split(','));
            return data;
        }

        private static void AddConditionNodes(List<ConditionNode> target, List<ProblemPredicateData> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
                target.Add(ToConditionNode(source[i]));
        }

        private static ConditionNode ToConditionNode(ProblemPredicateData pred)
        {
            ConditionNode literal = new ConditionNode
            {
                Type = ConditionNode.NodeType.Predicate,
                PredicateName = pred.predicate,
                Arguments = new List<string>(pred.args ?? new List<string>())
            };

            if (pred.value)
                return literal;

            ConditionNode not = new ConditionNode { Type = ConditionNode.NodeType.Not };
            not.Children.Add(literal);
            return not;
        }

        private static void AddConditionNode(List<ProblemPredicateData> target, ConditionNode node, bool value)
        {
            switch (node.Type)
            {
                case ConditionNode.NodeType.Predicate:
                    target.Add(new ProblemPredicateData
                    {
                        predicate = node.PredicateName,
                        args = new List<string>(node.Arguments),
                        value = value
                    });
                    break;

                case ConditionNode.NodeType.Not:
                    if (node.Children.Count > 0)
                        AddConditionNode(target, node.Children[0], !value);
                    break;

                case ConditionNode.NodeType.And:
                    for (int i = 0; i < node.Children.Count; i++)
                        AddConditionNode(target, node.Children[i], value);
                    break;
            }
        }

        private static void AppendPredicateList(StringBuilder sb, List<ProblemPredicateData> predicates, int indent)
        {
            if (predicates == null)
                return;

            string padding = new string(' ', indent * 2);
            for (int i = 0; i < predicates.Count; i++)
            {
                sb.Append(padding);
                AppendPredicate(sb, predicates[i]);
                sb.AppendLine();
            }
        }

        private static void AppendGoal(StringBuilder sb, List<ProblemPredicateData> predicates)
        {
            if (predicates == null || predicates.Count == 0)
            {
                sb.Append("()");
                return;
            }

            if (predicates.Count == 1)
            {
                AppendPredicate(sb, predicates[0]);
                return;
            }

            sb.Append("(and");
            for (int i = 0; i < predicates.Count; i++)
            {
                sb.Append(' ');
                AppendPredicate(sb, predicates[i]);
            }
            sb.Append(')');
        }

        private static void AppendPredicate(StringBuilder sb, ProblemPredicateData pred)
        {
            if (!pred.value)
                sb.Append("(not ");

            sb.Append('(');
            sb.Append(pred.predicate);
            if (pred.args != null)
            {
                for (int i = 0; i < pred.args.Count; i++)
                {
                    sb.Append(' ');
                    sb.Append(pred.args[i]);
                }
            }
            sb.Append(')');

            if (!pred.value)
                sb.Append(')');
        }
    }
}
