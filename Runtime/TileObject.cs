﻿using elZach.Common;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
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
        [HideInInspector]
        public string guid = System.Guid.NewGuid().ToString();
        public List<TileBehaviourBase> behaviours = new List<TileBehaviourBase>();
        [Header("Size")]
        public Vector3 boundSize = Vector3.one;
        public Vector3 offset = Vector3.zero;
        public bool roundUp = false;
        public Button<TileObject> calcBoundsButton = new Button<TileObject>(x => x.CalcBounds());
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
            newSize.x *= 2f; // this is because our basic 1,1,1 tile is centered on x,z but on the bottom for y so a extent of 0.5,1,0.5 := 1,1,1 cube
            newSize.z *= 2f;
            boundSize = newSize;
        }

        public int3 GetSize(Vector3 rasterScale)
        {
            if(roundUp)
                return new int3(
                    Mathf.Max(1, Mathf.CeilToInt(boundSize.x / rasterScale.x)),
                    Mathf.Max(1, Mathf.CeilToInt(boundSize.y / rasterScale.y)),
                    Mathf.Max(1, Mathf.CeilToInt(boundSize.z / rasterScale.z))
                    );
            else
                return new int3(
                    Mathf.Max(1, Mathf.FloorToInt(boundSize.x / rasterScale.x)),
                    Mathf.Max(1, Mathf.FloorToInt(boundSize.y / rasterScale.y)),
                    Mathf.Max(1, Mathf.FloorToInt(boundSize.z / rasterScale.z))
                    );
        }

        //[Reorderable]
        public Variant[] variants;
        
        public void PlaceBehaviour(PlacedTile placed, params PlacedTile[] neighbours)
        {
            foreach (var behaviour in behaviours)
                behaviour.OnPlacement(placed, neighbours);
        }

        public void UpdateBehaviour(PlacedTile placed, params PlacedTile[] neighbours)
        {
            foreach (var behaviour in behaviours)
                behaviour.OnUpdatedNeighbour(placed, neighbours);
        }


#if UNITY_EDITOR

        public static TileObject CreateNewTileFileFromPrefabs(bool createAsset, params GameObject[] selectedObjects)
        {
            if (selectedObjects.Length == 0) { Debug.LogWarning("[TileObject] cannot create TileObject without gameObject or path"); return null; }
            GameObject prefab = selectedObjects[0]; // as GameObject;
            string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
            TileObject tile = CreateInstance<TileObject>();
            tile.prefabs = selectedObjects;
            var behaviour = Resources.Load<TileBehaviour>("TileBehaviours/DefaultTileBehaviour");
            tile.behaviours = new List<TileBehaviourBase>() { behaviour };
            tile.CalcBounds();
            if (path.Contains(".prefab")) path = path.Replace(".prefab", "");
            path = path.Insert(path.LastIndexOf("/") + 1, "tile_");
            if (createAsset)
            {
                UnityEditor.AssetDatabase.CreateAsset(tile, path + ".asset");
                return UnityEditor.AssetDatabase.LoadAssetAtPath<TileObject>(path + ".asset");
            }
            else
            {
                tile.name = prefab.name;
                return tile;
            }
        }

        public static List<TileObject> CreateTileObjectsAt(string folderPath, params GameObject[] selectedObjects)
        {
            var list = new List<TileObject>();
            foreach (var prefab in selectedObjects)
            {
                string path = folderPath+"/"+prefab.name;//UnityEditor.AssetDatabase.GetAssetPath(prefab);
                TileObject tile = CreateInstance<TileObject>();
                tile.prefabs = new GameObject[] { prefab };
                var behaviour = Resources.Load<TileBehaviour>("TileBehaviours/DefaultTileBehaviour");
                tile.behaviours = new List<TileBehaviourBase>() { behaviour };
                tile.CalcBounds();
                if (path.Contains(".prefab")) path = path.Replace(".prefab", "");
                path = path.Insert(path.LastIndexOf("/") + 1, "tile_");
                UnityEditor.AssetDatabase.CreateAsset(tile, path + ".asset");
                list.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<TileObject>(path + ".asset"));
            }
            return list;
            //return UnityEditor.AssetDatabase.LoadAssetAtPath<TileObject>(path + ".asset");
        }

        //[Button("Get new guid")]
        public Button<TileObject> getNewGuidButton = new Button<TileObject>(x => x.GetNewGuid());
        public void GetNewGuid()
        {
            guid = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Create Tile From Prefab",true)]
        static bool CreateFromPrefabAssetsValidate()
        {
            bool valid = UnityEditor.Selection.activeObject is GameObject;
            return valid;
        }
        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Create Tile From Prefab")]
        static List<TileObject> CreateFromPrefabAssets()
        {
            List<TileObject> newTiles = new List<TileObject>();
            foreach (var selectedObject in UnityEditor.Selection.gameObjects)
            {
                newTiles.Add(CreateNewTileFileFromPrefabs(true, selectedObject));
            }
            return newTiles;
        }
        
        static List<TileObject> CreateFromPrefab()
        {
            List<TileObject> newTiles = new List<TileObject>();
            foreach (var selectedObject in UnityEditor.Selection.gameObjects)
            {
                newTiles.Add(CreateNewTileFileFromPrefabs(false, selectedObject));
            }
            return newTiles;
        }

        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Create Tile and add to Current Atlas", true)]
        static bool ValidateCreateAndAddToAtlas()
        {
            bool valid = UnityEditor.Selection.activeObject is GameObject;
            //var builder = FindObjectOfType<LevelBuilder>();
            //valid &= builder;
            //valid &= builder?.tileSet;
            return valid;
        }

        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Create Tile and add to Current Atlas")]
        static void CreateAndAddToAtlas()
        {
            var builder = FindObjectOfType<LevelBuilder>();
            if(!builder) { Debug.LogWarning("Create a level builder in scene."); return; }
            if (!builder.tileSet) { Debug.LogWarning("Add a tileatlas to builder."); return; }
            var newTiles = CreateFromPrefab();
            foreach (var selectedObject in newTiles)
            {
                if (selectedObject is TileObject)
                {
                    selectedObject.name = selectedObject.prefab.name;
                    AssetDatabase.AddObjectToAsset(selectedObject, builder.tileSet);
                    builder.tileSet.tiles.Add((TileObject)selectedObject as TileObject);
                }
            }
            builder.tileSet.GetDictionaryFromList();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(builder.tileSet));
        }

        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Create Tile and add multiple GameObjects", true)]
        static bool ValidateCreateMultipleGOTile()
        {
            bool valid = UnityEditor.Selection.activeObject is GameObject;
            valid &= UnityEditor.Selection.gameObjects.Length > 1;
            return valid;
        }

        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Create Tile and add multiple GameObjects")]
        static void CreateMultipleGOTile()
        {
            CreateNewTileFileFromPrefabs(true, UnityEditor.Selection.gameObjects);
        }

        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Add Tile To Current Atlas", true)]
        static bool ValidateAddTileToCurrentAtlas()
        {
            bool valid = UnityEditor.Selection.activeObject is TileObject;
            //var builder = FindObjectOfType<LevelBuilder>();
            //valid &= builder;
            //valid &= builder?.tileSet;
            return valid;
        }

        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Add Tile To Current Atlas")]
        static void AddTileToCurrentAtlas()
        {
            var builder = FindObjectOfType<LevelBuilder>();
            if (!builder) { Debug.LogWarning("Create a level builder in scene."); return; }
            if (!builder.tileSet) { Debug.LogWarning("Add a tileatlas to builder."); return; }
            foreach (var selectedObject in UnityEditor.Selection.objects)
            {
                if (selectedObject is TileObject)
                {
                    AssetDatabase.AddObjectToAsset(selectedObject, builder.tileSet);
                    builder.tileSet.tiles.Add((TileObject)selectedObject as TileObject);
                }
            }
            builder.tileSet.GetDictionaryFromList();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(builder.tileSet));
        }

#endif
    }
}