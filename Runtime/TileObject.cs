using elZach.Common;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace elZach.LevelEditor
{
    [CreateAssetMenu(menuName = "LevelEditor/TileObject", fileName = "TileObject")]
    public class TileObject : ScriptableObject
    {
        [System.Serializable]
        public class Variant
        {
            public GameObject prefab;
            public Neighbour condition;
            public Vector3 position = Vector3.zero;
            public Vector3 rotation = Vector3.zero;
            public Vector3 scale = Vector3.one;
        }

        [System.Flags]
        public enum Neighbour { None = 1 << 0, FrontLeft = 1 << 1, Front = 1 << 2, FrontRight = 1 << 3, Left = 1 << 4, Right = 1 << 5, BackLeft = 1 << 6, Back = 1 << 7, BackRight = 1 << 8 }

        public GameObject prefab;
        public string guid = System.Guid.NewGuid().ToString();
        public TileBehaviour behaviour;
        [Header("Not In Use yet")]
        public Vector3 boundSize = Vector3.one;
        [Header("Will be generated in future")]
        public int3 size = new int3(1, 1, 1);
        public int3 GetSize(Vector3 rasterScale)
        {
            return new int3(
                Mathf.FloorToInt(boundSize.x / rasterScale.x),
                Mathf.FloorToInt(boundSize.y / rasterScale.y),
                Mathf.FloorToInt(boundSize.z / rasterScale.z)
                );
        }

        //[Reorderable]
        public Variant[] variants;
        

#if UNITY_EDITOR
        [Button("Get new guid")]
        public void GetNewGuid()
        {
            guid = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [UnityEditor.MenuItem("Assets/Tiles/Create Tile From Prefab",true)]
        static bool CreateFromPrefabValidate()
        {
            bool valid = UnityEditor.Selection.activeObject is GameObject;
            return valid;
        }
        [UnityEditor.MenuItem("Assets/Tiles/Create Tile From Prefab")]
        static void CreateFromPrefab()
        {
            foreach (var selectedObject in UnityEditor.Selection.gameObjects)
            {
                GameObject prefab = selectedObject; // as GameObject;
                string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
                TileObject tile = CreateInstance<TileObject>();
                tile.prefab = prefab;
                var behaviour = Resources.Load<TileBehaviour>("TileBehaviours/DefaultTileBehaviour");
                tile.behaviour = behaviour;
                if (path.Contains(".prefab")) path = path.Replace(".prefab", "");
                UnityEditor.AssetDatabase.CreateAsset(tile, path + ".asset");
            }
        }
        

#endif
    }
}