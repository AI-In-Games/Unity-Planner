using UnityEditor;
using UnityEngine;
using AIInGames.Planning.Unity;

namespace AIInGames.Planning.Unity.Editor
{
    /// <summary>
    /// Custom inspector for DomainAsset with button to open domain editor.
    /// </summary>
    [CustomEditor(typeof(DomainAsset))]
    public class DomainAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open in Domain Editor", GUILayout.Height(30)))
            {
                DomainEditorWindow.OpenWindow((DomainAsset)target);
            }
        }
    }
}
