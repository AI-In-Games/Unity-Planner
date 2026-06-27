using System.IO;
using AIInGames.Planning.Unity;
using UnityEditor;
using UnityEngine;

namespace AIInGames.Planning.Unity.Editor
{
    /// <summary>
    /// PDDL output actions for a DomainAsset: preview to the Console and export to a file. Kept out of
    /// the Domain Editor window so the window only wires the menu and this stays independently testable.
    /// </summary>
    public static class DomainPddlActions
    {
        public static void LogPreview(DomainAsset domain)
        {
            if (domain == null)
                return;
            Debug.Log($"[PDDL] {domain.DomainName}\n{DomainSerializer.ToPddl(domain)}");
        }

        public static void Export(DomainAsset domain)
        {
            if (domain == null)
                return;

            string path = EditorUtility.SaveFilePanel(
                "Export PDDL Domain", Application.dataPath, domain.DomainName + ".pddl", "pddl");
            if (string.IsNullOrEmpty(path))
                return;

            File.WriteAllText(path, DomainSerializer.ToPddl(domain));
            Debug.Log($"[PDDL] Exported {domain.DomainName} to: {path}");
        }
    }
}
