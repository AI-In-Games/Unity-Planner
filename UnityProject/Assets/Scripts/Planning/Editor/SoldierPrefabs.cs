using AIInGames.Planning.Demo;
using UnityEditor;
using UnityEngine;

namespace AIInGames.Planning.Demo.EditorTools
{
    /// <summary>
    /// Loads and instantiates the Soldier prefab set for the demo scene builders: actors (Player,
    /// Soldier) with a Rifle and Grenade dropped into their RifleSlot and GrenadeSlot, plain item
    /// markers (MedPack, Ammo, Crate, ...), and the Bullet and Explosion effect prefabs.
    /// </summary>
    public static class SoldierPrefabs
    {
        private const string Root = "Assets/Soldier/";

        public static GameObject Load(string prefabName)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(Root + prefabName + ".prefab");
        }

        public static GameObject Place(string prefabName, Vector3 position)
        {
            GameObject prefab = Load(prefabName);
            if (prefab == null)
            {
                Debug.LogError($"[SoldierPrefabs] Missing prefab {Root}{prefabName}.prefab");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            return instance;
        }

        /// <summary>Tiles the FloorTile prefab to cover the given half-extents, seamlessly at the
        /// prefab's own sprite size, behind everything else.</summary>
        public static GameObject TileFloor(float halfWidth, float halfHeight)
        {
            GameObject prefab = Load("FloorTile");
            if (prefab == null)
                return null;

            float size = SpriteWorldSize(prefab);
            GameObject parent = new GameObject("Floor");

            int columns = Mathf.CeilToInt(halfWidth * 2f / size);
            int rows = Mathf.CeilToInt(halfHeight * 2f / size);
            float startX = -halfWidth + size * 0.5f;
            float startY = -halfHeight + size * 0.5f;

            for (int i = 0; i <= columns; i++)
                for (int j = 0; j <= rows; j++)
                {
                    GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    tile.transform.SetParent(parent.transform);
                    tile.transform.position = new Vector3(startX + i * size, startY + j * size, 0f);
                    SpriteRenderer renderer = tile.GetComponentInChildren<SpriteRenderer>();
                    if (renderer != null)
                        renderer.sortingOrder = -10;
                }

            return parent;
        }

        /// <summary>Builds a block of Crate prefabs packed edge to edge, centered on the position.
        /// Returns the parent transform, used as the logical cover/location point.</summary>
        public static GameObject CrateWall(Vector3 center, int columns, int rows)
        {
            GameObject prefab = Load("Crate");
            if (prefab == null)
                return null;

            float size = SpriteWorldSize(prefab);
            GameObject parent = new GameObject("Cover");
            parent.transform.position = center;

            float offsetX = (columns - 1) * 0.5f * size;
            float offsetY = (rows - 1) * 0.5f * size;
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < rows; r++)
                {
                    GameObject crate = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    crate.transform.SetParent(parent.transform);
                    crate.transform.localPosition = new Vector3(c * size - offsetX, r * size - offsetY, 0f);
                }

            return parent;
        }

        private static float SpriteWorldSize(GameObject prefab)
        {
            SpriteRenderer renderer = prefab.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
                return renderer.sprite.bounds.size.x;
            return 1f;
        }

        public static GameObject PlaceActor(string actorPrefab, Vector3 position, bool withRifle, bool withGrenade)
        {
            GameObject actor = Place(actorPrefab, position);
            if (actor == null)
                return null;

            if (withRifle)
                AttachToSlot(actor, "RifleSlot", "Rifle");
            if (withGrenade)
                AttachToSlot(actor, "GrenadeSlot", "Grenade");
            return actor;
        }

        /// <summary>Adds and wires the self-contained PlayerController on the player actor. For the
        /// showdown demo, canShoot and target let the player return fire at the soldier.</summary>
        public static void AddPlayerController(Transform player, GameObject bullet, GameObject explosion,
            Vector2 arenaHalfExtents, bool canShoot, Transform target)
        {
            PlayerController controller = player.gameObject.AddComponent<PlayerController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("m_ArenaHalfExtents").vector2Value = arenaHalfExtents;
            serialized.FindProperty("m_BulletPrefab").objectReferenceValue = bullet;
            serialized.FindProperty("m_ExplosionPrefab").objectReferenceValue = explosion;
            serialized.FindProperty("m_CanShoot").boolValue = canShoot;
            serialized.FindProperty("m_Target").objectReferenceValue = target;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AttachToSlot(GameObject actor, string slotName, string itemPrefab)
        {
            Transform slot = FindChild(actor.transform, slotName);
            if (slot == null)
                return;

            GameObject prefab = Load(itemPrefab);
            if (prefab == null)
                return;

            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            item.transform.SetParent(slot, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }
            return null;
        }
    }
}
