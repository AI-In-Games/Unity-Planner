using AIInGames.Planning.Demo;
using AIInGames.Planning.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIInGames.Planning.Demo.EditorTools
{
    /// <summary>
    /// Builds the medic duel scene from the Soldier prefab set. The domain is a plain PDDL file
    /// imported to a DomainAsset, like the other demos; this places the actors and items (a Soldier
    /// and Player each with a rifle in their slot, two MedPack stations, and the Bullet and Explosion
    /// effect prefabs) and wires the asset. The demo prioritises survival by switching its planning
    /// goal, not by per-action costs. Range checks are drawn as gizmos by the demo, so no ring sprites
    /// are created here. Run the menu item, then press Play.
    /// </summary>
    public static class SoldierMedicSceneBuilder
    {
        private const string DomainPddl = "Assets/StreamingAssets/Domains/SoldierMedic/soldier-medic-domain.pddl";
        private const string DomainAssetPath = "Assets/Planning/Domains/SoldierMedic.asset";
        private const string ScenePath = "Assets/Scenes/SoldierMedicDemo.unity";
        private const float ShootRange = 2.5f;
        private const float MedRadius = 0.8f;

        [MenuItem("AI In Games/Tutorial/5 - Medic (goal priority: survive vs fight)")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetupCamera();
            SoldierPrefabs.TileFloor(9f, 5f);

            Transform soldier = SoldierPrefabs.PlaceActor("Soldier", new Vector3(-5f, 0f, 0f), withRifle: true, withGrenade: false).transform;
            Transform player = SoldierPrefabs.PlaceActor("Player", new Vector3(4f, 0f, 0f), withRifle: true, withGrenade: false).transform;
            Transform medA = SoldierPrefabs.Place("MedPack", new Vector3(-3f, 3f, 0f)).transform;
            Transform medB = SoldierPrefabs.Place("MedPack", new Vector3(3f, -3f, 0f)).transform;

            DomainAsset domain = PddlToAssetConverter.ConvertFile(DomainPddl, DomainAssetPath);
            GameObject bullet = SoldierPrefabs.Load("Bullet");
            GameObject explosion = SoldierPrefabs.Load("Explosion");

            GameObject host = new GameObject("SoldierMedicDemo");
            SoldierMedicDemo demo = host.AddComponent<SoldierMedicDemo>();
            SerializedObject demoObject = new SerializedObject(demo);
            demoObject.FindProperty("m_Domain").objectReferenceValue = domain;
            demoObject.FindProperty("m_Soldier").objectReferenceValue = soldier;
            demoObject.FindProperty("m_Player").objectReferenceValue = player;
            demoObject.FindProperty("m_MedA").objectReferenceValue = medA;
            demoObject.FindProperty("m_MedB").objectReferenceValue = medB;
            demoObject.FindProperty("m_BulletPrefab").objectReferenceValue = bullet;
            demoObject.FindProperty("m_ExplosionPrefab").objectReferenceValue = explosion;
            demoObject.FindProperty("m_ShootRange").floatValue = ShootRange;
            demoObject.FindProperty("m_MedRadius").floatValue = MedRadius;
            demoObject.ApplyModifiedPropertiesWithoutUndo();

            SoldierPrefabs.AddPlayerController(player, bullet, explosion, new Vector2(7f, 4f), canShoot: true, target: soldier);

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Medic] Demo scene built at {ScenePath}. Open it and press Play.");
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
