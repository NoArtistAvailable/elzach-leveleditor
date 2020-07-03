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
            public GameObject prefab { get { return prefabs[UnityEngine.Random.Range(0, prefabs.Length)]; } }
            public GameObject[] prefabs;
            public Neighbour condition;
            public Vector3 position = Vector3.zero;
            public Vector3 rotation = Vector3.zero;
            public Vector3 scale = Vector3.one;

            public Variant()
            {
                position = Vector3.zero;
                rotation = Vector3.zero;
                scale = Vector3.one;
            }
        }

        [System.Flags]
        public enum Neighbour { None = 1 << 0, FrontLeft = 1 << 1, Front = 1 << 2, FrontRight = 1 << 3, Left = 1 << 4, Right = 1 << 5, BackLeft = 1 << 6, Back = 1 << 7, BackRight = 1 << 8 }

        public GameObject prefab { get { return prefabs[UnityEngine.Random.Range(0, prefabs.Length)]; } }
        public GameObject[] prefabs;
        public string guid = System.Guid.NewGuid().ToString();
        public TileBehaviourBase behaviour;
        [Header("Not In Use yet")]
        public Vector3 boundSize = Vector3.one;
        [Header("Will be generated in future")]
        public int3 size = new int3(1, 1, 1);
        
        [Button("Calc Bounds")]
        public void CalcBounds()
        {
            Bounds bounds = new Bounds();
            bounds.center = prefabs[0].transform.position;
            bounds.size = Vector3.zero;
            Vector3 _size = Vector3.zero;
            foreach(var renderer in prefabs[0].GetComponentsInChildren<Renderer>())
            {
                var rendererBounds = renderer.bounds;
                Vector3 maxPoint = renderer.bounds.max;
                maxPoint = renderer.transform.TransformVector(maxPoint);
                _size = Vector3.Max(_size, maxPoint);
                bounds.Encapsulate(rendererBounds);
            }

            Vector3 newSize = bounds.max - prefabs[0].transform.position;
            newSize.x *= 2f;
            newSize.z *= 2f;
            boundSize = newSize;
        }

        public int3 GetSize(Vector3 rasterScale)
        {
            return new int3(
                Mathf.Max(1, Mathf.FloorToInt(boundSize.x / rasterScale.x)),
                Mathf.Max(1, Mathf.FloorToInt(boundSize.y / rasterScale.y)),
                Mathf.Max(1, Mathf.FloorToInt(boundSize.z / rasterScale.z))
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
        static List<TileObject> CreateFromPrefab()
        {
            List<TileObject> newTiles = new List<TileObject>();
            foreach (var selectedObject in UnityEditor.Selection.gameObjects)
            {
                GameObject prefab = selectedObject; // as GameObject;
                string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
                TileObject tile = CreateInstance<TileObject>();
                tile.prefabs = new GameObject[] { prefab };
                var behaviour = Resources.Load<TileBehaviour>("TileBehaviours/DefaultTileBehaviour");
                tile.behaviour = behaviour;
                if (path.Contains(".prefab")) path = path.Replace(".prefab", "");
                UnityEditor.AssetDatabase.CreateAsset(tile, path + ".asset");
                newTiles.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<TileObject>(path+".asset"));
            }
            return newTiles;
        }

        [UnityEditor.MenuItem("Assets/Tiles/Create Tile and add to Current Atlas", true)]
        static bool ValidateCreateAndAddToAtlas()
        {
            bool valid = UnityEditor.Selection.activeObject is GameObject;
            //var builder = FindObjectOfType<LevelBuilder>();
            //valid &= builder;
            //valid &= builder?.tileSet;
            return valid;
        }

        [UnityEditor.MenuItem("Assets/Tiles/Create Tile and add to Current Atlas")]
        static void CreateAndAddToAtlas()
        {
            var builder = FindObjectOfType<LevelBuilder>();
            if(!builder) { Debug.LogWarning("Create a level builder in scene."); return; }
            if (!builder.tileSet) { Debug.LogWarning("Add a tileatlas to builder."); return; }
            var newTiles = CreateFromPrefab();
            foreach (var selectedObject in newTiles)
            {
                if (selectedObject is TileObject)
                    builder.tileSet.tiles.Add((TileObject)selectedObject as TileObject);
            }
            builder.tileSet.GetDictionaryFromList();
        }

        [UnityEditor.MenuItem("Assets/Tiles/Create Tile and add multiple GameObjects", true)]
        static bool ValidateCreateMultipleGOTile()
        {
            bool valid = UnityEditor.Selection.activeObject is GameObject;
            valid &= UnityEditor.Selection.gameObjects.Length > 1;
            return valid;
        }

        [UnityEditor.MenuItem("Assets/Tiles/Create Tile and add multiple GameObjects")]
        static void CreateMultipleGOTile()
        {
            GameObject firstObject = UnityEditor.Selection.activeGameObject;
            string path = UnityEditor.AssetDatabase.GetAssetPath(firstObject);
            TileObject tile = CreateInstance<TileObject>();
            tile.prefabs = UnityEditor.Selection.gameObjects;
            var behaviour = Resources.Load<TileBehaviour>("TileBehaviours/DefaultTileBehaviour");
            tile.behaviour = behaviour;

            if (path.Contains(".prefab")) path = path.Replace(".prefab", "");
            UnityEditor.AssetDatabase.CreateAsset(tile, path + ".asset");
        }

        [UnityEditor.MenuItem("Assets/Tiles/Add Tile To Current Atlas", true)]
        static bool ValidateAddTileToCurrentAtlas()
        {
            bool valid = UnityEditor.Selection.activeObject is TileObject;
            //var builder = FindObjectOfType<LevelBuilder>();
            //valid &= builder;
            //valid &= builder?.tileSet;
            return valid;
        }

        [UnityEditor.MenuItem("Assets/Tiles/Add Tile To Current Atlas")]
        static void AddTileToCurrentAtlas()
        {
            var builder = FindObjectOfType<LevelBuilder>();
            if (!builder) { Debug.LogWarning("Create a level builder in scene."); return; }
            if (!builder.tileSet) { Debug.LogWarning("Add a tileatlas to builder."); return; }
            foreach (var selectedObject in UnityEditor.Selection.objects)
            {
                if (selectedObject is TileObject)
                    builder.tileSet.tiles.Add((TileObject)selectedObject as TileObject);
            }
            builder.tileSet.GetDictionaryFromList();
        }
        

#endif
    }
}