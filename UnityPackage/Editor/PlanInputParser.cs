using System.Collections.Generic;
using AIInGames.Planning.Runtime;

namespace AIInGames.Planning.Unity.Editor
{
    public static class PlanInputParser
    {
        /// <summary>
        /// Parses a multiline string of "name:type" entries into a list of PlanningObjects.
        /// Returns false and sets error if any line is malformed.
        /// </summary>
        public static bool TryParseObjects(string text, out List<PlanningObject> objects, out string error)
        {
            objects = new List<PlanningObject>();
            error   = null;

            if (string.IsNullOrWhiteSpace(text))
                return true;

            string[] lines = SplitLines(text);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line))
                    continue;

                int colon = line.IndexOf(':');
                if (colon < 1)
                {
                    error = $"Objects line {i + 1}: expected 'name:type', got '{line}'";
                    return false;
                }

                string name = line.Substring(0, colon).Trim();
                string type = line.Substring(colon + 1).Trim();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
                {
                    error = $"Objects line {i + 1}: name and type must not be empty (got '{name}':'{type}')";
                    return false;
                }

                objects.Add(new PlanningObject(name, type));
            }

            return true;
        }

        /// <summary>
        /// Parses a multiline string of predicates into a PlanningState.
        /// Prefix a line with ! to set the predicate to false.
        /// Returns false and sets error if any line is malformed.
        /// </summary>
        public static bool TryParseState(string text, string label, out PlanningState state, out string error)
        {
            state = new PlanningState();
            error = null;

            if (string.IsNullOrWhiteSpace(text))
                return true;

            string[] lines = SplitLines(text);
            foreach (string raw in lines)
            {
                if (string.IsNullOrEmpty(raw))
                    continue;

                bool value = true;
                string line = raw;
                if (line[0] == '!')
                {
                    value = false;
                    line  = line.Substring(1).Trim();
                }

                if (string.IsNullOrEmpty(line))
                {
                    error = $"Empty predicate name in {label}";
                    return false;
                }

                state.SetPredicate(line, value);
            }

            return true;
        }

        private static string[] SplitLines(string text)
        {
            string normalized = text
                .Replace("\r\n",   "\n")
                .Replace("\u2028", "\n")
                .Replace("\u2029", "\n")
                .Replace("\u0085", "\n")
                .Replace("\r",     "\n");
            string[] raw = normalized.Split('\n');
            List<string> trimmed = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
                trimmed.Add(raw[i].Trim());
            return trimmed.ToArray();
        }
    }
}