using AIInGames.Planning.Demo;
using AIInGames.Planning.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIInGames.Planning.Demo.EditorTools
{
    /// <summary>
    /// Builds the cover variant of the Soldier demo from the prefab set. The soldier starts with an
    /// unloaded weapon, so the planner must first reach the Crate (cover), take cover, and reload
    /// before chasing and shooting the Player. Reuses the Soldier domain. Range and cover radius are
    /// drawn as gizmos. Run the menu item, then press Play.
    /// </summary>
    public static class SoldierCoverSceneBuilder
    {
        private const string DomainPddl = "Assets/StreamingAssets/Domains/Soldier/soldier-domain.pddl";
        private const string DomainAssetPath = "Assets/Planning/Domains/Soldier.asset";
        private const string ScenePath = "Assets/Scenes/SoldierCoverDemo.unity";
        private const float ShootRange = 2.5f;
        private const float CoverRadius = 1.6f;

        [MenuItem("AI In Games/Tutorial/2 - Cover and Reload")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetupCamera();
            SoldierPrefabs.TileFloor(9f, 5f);

            Transform soldier = SoldierPrefabs.PlaceActor("Soldier", new Vector3(-5f, -2f, 0f), withRifle: true, withGrenade: false).transform;
            Transform player = SoldierPrefabs.PlaceActor("Player", new Vector3(4f, 0f, 0f), withRifle: false, withGrenade: false).transform;
            Transform cover = SoldierPrefabs.CrateWall(new Vector3(0f, 3f, 0f), 3, 2).transform;

            DomainAsset domain = PddlToAssetConverter.ConvertFile(DomainPddl, DomainAssetPath);
            GameObject bullet = SoldierPrefabs.Load("Bullet");
            GameObject explosion = SoldierPrefabs.Load("Explosion");

            GameObject host = new GameObject("SoldierCoverDemo");
            SoldierCoverDemo demo = host.AddComponent<SoldierCoverDemo>();
            SerializedObject demoObject = new SerializedObject(demo);
            demoObject.FindProperty("m_Domain").objectReferenceValue = domain;
            demoObject.FindProperty("m_Soldier").objectReferenceValue = soldier;
            demoObject.FindProperty("m_Player").objectReferenceValue = player;
            demoObject.FindProperty("m_Cover").objectReferenceValue = cover;
            demoObject.FindProperty("m_BulletPrefab").objectReferenceValue = bullet;
            demoObject.FindProperty("m_ExplosionPrefab").objectReferenceValue = explosion;
            demoObject.FindProperty("m_ShootRange").floatValue = ShootRange;
            demoObject.FindProperty("m_CoverRadius").floatValue = CoverRadius;
            demoObject.ApplyModifiedPropertiesWithoutUndo();

            SoldierPrefabs.AddPlayerController(player, bullet, explosion, new Vector2(7f, 4f), canShoot: false, target: null);

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SoldierCover] Demo scene built at {ScenePath}. Open it and press Play.");
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
