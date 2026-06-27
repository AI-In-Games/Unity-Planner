using AIInGames.Planning.Demo;
using AIInGames.Planning.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIInGames.Planning.Demo.EditorTools
{
    /// <summary>
    /// Builds the basic free-movement Soldier demo from the Soldier prefab set: a Soldier (rifle in
    /// its slot) that pursues and shoots the Player. No location graph; the planner's at predicate is
    /// a distance check at runtime, drawn as a gizmo. Run the menu item, then press Play.
    /// </summary>
    public static class SoldierSceneBuilder
    {
        private const string DomainPddl = "Assets/StreamingAssets/Domains/Soldier/soldier-domain.pddl";
        private const string DomainAssetPath = "Assets/Planning/Domains/Soldier.asset";
        private const string ScenePath = "Assets/Scenes/SoldierDemo.unity";
        private const float ShootRange = 2.5f;

        [MenuItem("AI In Games/Tutorial/1 - Pursuit (Soldier)")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetupCamera();
            SoldierPrefabs.TileFloor(9f, 5f);

            Transform soldier = SoldierPrefabs.PlaceActor("Soldier", new Vector3(-4f, 0f, 0f), withRifle: true, withGrenade: false).transform;
            Transform player = SoldierPrefabs.PlaceActor("Player", new Vector3(3f, 0f, 0f), withRifle: false, withGrenade: false).transform;

            DomainAsset domain = PddlToAssetConverter.ConvertFile(DomainPddl, DomainAssetPath);
            GameObject bullet = SoldierPrefabs.Load("Bullet");
            GameObject explosion = SoldierPrefabs.Load("Explosion");

            GameObject host = new GameObject("SoldierDemo");
            SoldierDemo demo = host.AddComponent<SoldierDemo>();
            SerializedObject demoObject = new SerializedObject(demo);
            demoObject.FindProperty("m_Domain").objectReferenceValue = domain;
            demoObject.FindProperty("m_Soldier").objectReferenceValue = soldier;
            demoObject.FindProperty("m_Player").objectReferenceValue = player;
            demoObject.FindProperty("m_BulletPrefab").objectReferenceValue = bullet;
            demoObject.FindProperty("m_ExplosionPrefab").objectReferenceValue = explosion;
            demoObject.FindProperty("m_ShootRange").floatValue = ShootRange;
            demoObject.ApplyModifiedPropertiesWithoutUndo();

            SoldierPrefabs.AddPlayerController(player, bullet, explosion, new Vector2(7f, 4f), canShoot: false, target: null);

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Soldier] Demo scene built at {ScenePath}. Open it and press Play.");
        }

        private static void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }
}
