using System.Collections.Generic;
using System.IO;
using AIInGames.Planning.PDDL;
using AIInGames.Planning.Unity;
using UnityEditor;
using UnityEngine;

namespace AIInGames.Planning.Demo.EditorTools
{
    /// <summary>
    /// Converts a PDDL domain into a JSON-backed <see cref="DomainAsset"/>. The asset captures the
    /// domain's types, predicates, and actions, and serializes to JSON on disk through the asset's
    /// own serialization. Call <see cref="ConvertFile"/> or <see cref="LoadOrConvert"/> from a scene
    /// builder to get an asset, converting on first use.
    /// </summary>
    public static class PddlToAssetConverter
    {
        public static DomainAsset Convert(string pddlText, out string error)
        {
            PDDLParser parser = new PDDLParser();
            IParseResult<IDomain> result = parser.ParseDomain(pddlText);
            if (!result.Success)
            {
                error = result.Errors.Count > 0 ? result.Errors[0].Message : "PDDL domain parse failed.";
                return null;
            }

            IDomain domain = result.Result;
            if (!PddlDomainImporter.TryConvert(domain, out List<ActionDefinition> actions, out error))
                return null;

            DomainAsset asset = ScriptableObject.CreateInstance<DomainAsset>();
            asset.DomainName = domain.Name;

            asset.Requirements.Clear();
            foreach (string requirement in domain.Requirements)
                asset.Requirements.Add(requirement);

            asset.Types.Clear();
            foreach (IType type in domain.Types)
            {
                // "object" is the implicit root, not a declared type; skip it so it is not nested
                // under the editor's synthesized root.
                if (type.Name == "object")
                    continue;
                asset.Types.Add(new TypeDefinition(type.Name, type.ParentType != null ? type.ParentType.Name : "object"));
            }

            asset.Predicates.Clear();
            foreach (IPredicate predicate in domain.Predicates)
            {
                PredicateDefinition definition = new PredicateDefinition(predicate.Name);
                foreach (IParameter parameter in predicate.Parameters)
                    definition.Parameters.Add(new PredicateParameter(
                        parameter.Name, parameter.Type != null ? parameter.Type.Name : "object"));
                asset.Predicates.Add(definition);
            }

            asset.Actions.Clear();
            for (int i = 0; i < actions.Count; i++)
                asset.Actions.Add(actions[i]);

            error = null;
            return asset;
        }

        public static DomainAsset ConvertFile(string pddlPath, string assetPath)
        {
            DomainAsset asset = Convert(File.ReadAllText(pddlPath), out string error);
            if (asset == null)
            {
                Debug.LogError($"[PDDL->Asset] {pddlPath}: {error}");
                return null;
            }

            EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PDDL->Asset] {assetPath}: {asset.Actions.Count} actions, {asset.Predicates.Count} predicates, {asset.Types.Count} types");
            return asset;
        }

        public static DomainAsset LoadOrConvert(string pddlPath, string assetPath)
        {
            DomainAsset existing = AssetDatabase.LoadAssetAtPath<DomainAsset>(assetPath);
            return existing != null ? existing : ConvertFile(pddlPath, assetPath);
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
