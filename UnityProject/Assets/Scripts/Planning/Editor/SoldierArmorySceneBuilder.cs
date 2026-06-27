using AIInGames.Planning.Demo;
using AIInGames.Planning.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIInGames.Planning.Demo.EditorTools
{
    /// <summary>
    /// Builds the weapon-pickup variant of the Soldier demo from the prefab set. The soldier starts
    /// unarmed and must reach the ammo cache lying in the world, pick it up (a rifle then appears in
    /// its slot), then chase and shoot the Player. Uses the extended soldier-armory domain. Run the
    /// menu item, then press Play.
    /// </summary>
    public static class SoldierArmorySceneBuilder
    {
        private const string DomainPddl = "Assets/StreamingAssets/Domains/SoldierArmory/soldier-armory-domain.pddl";
        private const string DomainAssetPath = "Assets/Planning/Domains/SoldierArmory.asset";
        private const string ScenePath = "Assets/Scenes/SoldierArmoryDemo.unity";
        private const float ShootRange = 2.5f;
        private const float ArmoryRadius = 0.8f;

        [MenuItem("AI In Games/Tutorial/3 - Armory Pickup (domain extension)")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetupCamera();
            SoldierPrefabs.TileFloor(9f, 5f);

            Transform soldier = SoldierPrefabs.PlaceActor("Soldier", new Vector3(-5f, 2f, 0f), withRifle: false, withGrenade: false).transform;
            Transform player = SoldierPrefabs.PlaceActor("Player", new Vector3(4f, 0f, 0f), withRifle: false, withGrenade: false).transform;
            Transform armory = SoldierPrefabs.Place("Ammo", new Vector3(-1f, -3f, 0f)).transform;

            DomainAsset domain = PddlToAssetConverter.ConvertFile(DomainPddl, DomainAssetPath);
            GameObject rifle = SoldierPrefabs.Load("Rifle");
            GameObject bullet = SoldierPrefabs.Load("Bullet");
            GameObject explosion = SoldierPrefabs.Load("Explosion");

            GameObject host = new GameObject("SoldierArmoryDemo");
            SoldierArmoryDemo demo = host.AddComponent<SoldierArmoryDemo>();
            SerializedObject demoObject = new SerializedObject(demo);
            demoObject.FindProperty("m_Domain").objectReferenceValue = domain;
            demoObject.FindProperty("m_Soldier").objectReferenceValue = soldier;
            demoObject.FindProperty("m_Player").objectReferenceValue = player;
            demoObject.FindProperty("m_Armory").objectReferenceValue = armory;
            demoObject.FindProperty("m_RiflePrefab").objectReferenceValue = rifle;
            demoObject.FindProperty("m_BulletPrefab").objectReferenceValue = bullet;
            demoObject.FindProperty("m_ExplosionPrefab").objectReferenceValue = explosion;
            demoObject.FindProperty("m_ShootRange").floatValue = ShootRange;
            demoObject.FindProperty("m_ArmoryRadius").floatValue = ArmoryRadius;
            demoObject.ApplyModifiedPropertiesWithoutUndo();

            SoldierPrefabs.AddPlayerController(player, bullet, explosion, new Vector2(7f, 4f), canShoot: false, target: null);

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SoldierArmory] Demo scene built at {ScenePath}. Open it and press Play.");
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
