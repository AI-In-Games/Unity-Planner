using System;
using System.Collections.Generic;
using System.Text;

namespace AIInGames.Planning.Runtime
{
    public readonly struct PlanningPredicate : IEquatable<PlanningPredicate>
    {
        private readonly string[] m_Arguments;

        public string Name { get; }
        public IReadOnlyList<string> Arguments => m_Arguments ?? Array.Empty<string>();

        public PlanningPredicate(string name, params string[] arguments)
        {
            Name = NormalizeToken(name, nameof(name));
            m_Arguments = NormalizeArguments(arguments);
        }

        public string Key => BuildKey(Name, m_Arguments);

        public static PlanningPredicate Create(string name, params string[] arguments)
        {
            return new PlanningPredicate(name, arguments);
        }

        public static string BuildKey(string name, params string[] arguments)
        {
            name = NormalizeToken(name, nameof(name));

            if (arguments == null || arguments.Length == 0)
                return name;

            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append('(');
            for (int i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append(NormalizeToken(arguments[i], nameof(arguments)));
            }
            sb.Append(')');
            return sb.ToString();
        }

        public bool Equals(PlanningPredicate other)
        {
            return string.Equals(Key, other.Key, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlanningPredicate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public override string ToString()
        {
            return Key;
        }

        private static string[] NormalizeArguments(string[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
                return Array.Empty<string>();

            string[] copy = new string[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
                copy[i] = NormalizeToken(arguments[i], nameof(arguments));
            return copy;
        }

        private static string NormalizeToken(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Predicate names and arguments cannot be null, empty, or whitespace.", paramName);
            return value.Trim();
        }
    }
}
