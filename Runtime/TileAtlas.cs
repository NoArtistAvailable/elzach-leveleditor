using elZach.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;

namespace elZach.LevelEditor
{
    [CreateAssetMenu(menuName="LevelEditor/TileAtlas")]
    public class TileAtlas : ScriptableObject
    {
        [System.Serializable]
        public class TagLayer
        {
            public string name;
            public Color color;
            public float3 rasterSize = new float3(1, 1, 1);
            public List<TileObject> layerObjects = new List<TileObject>();

            public TagLayer()
            {
                name = "Layer";
                color = Color.white;//UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 0.5f, 0.6f, 0.8f);
            }
            public TagLayer(string name, Color color, Vector3 size)
            {
                this.name = name;
                this.color = color;
                this.rasterSize = size;
            }

        }

        //[Reorderable]
        public List<TileObject> tiles;
        [HideInInspector]
        public SerializableDictionary<string, TileObject> TileFromGuid = new SerializableDictionary<string, TileObject>();
        [HideInInspector]
        public TagLayer defaultLayer = new TagLayer("Default", Color.gray, Vector3.one);
        public List<TagLayer> layers = new List<TagLayer>();
        
        public TagLayer AddTagLayer()
        {
            var newLayer = new TagLayer("Layer", UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 0.5f, 0.6f, 0.8f), Vector3.one);
            layers.Add(newLayer);
            return newLayer;
        }

#if UNITY_EDITOR
        [Button("Dictionary From List")]
        public void GetDictionaryFromList()
        {
            TileFromGuid.Clear();
            foreach(var tile in tiles)
            {
                if (!tile) continue;
                if (TileFromGuid.ContainsKey(tile.guid))
                {
                    Debug.LogWarning("Guid already exists in Dictionary, make sure that each tile has a unique ID. Removed Tile: " + tile.name + " from atlas " + this.name);
                    tiles.Remove(tile);
                    return;
                }
                TileFromGuid.Add(tile.guid, tile);
            }
            if (tiles.Contains(null))
            {
                for (int i = tiles.Count - 1; i >= 0; i--)
                    if (tiles[i] == null) tiles.RemoveAt(i);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void MoveTileToLayer(TileObject tile, TagLayer layer, bool allowMultiLayer = false)
        {
            if (!tiles.Contains(tile)) { Debug.LogWarning("Atlas doesnt contain tile " + tile.name, this); return; }
            if (layer.layerObjects.Contains(tile)) { Debug.Log("Layer already contains tile " + tile.name, this); return; }

            if (!allowMultiLayer)
                foreach (var lay in layers)
                    if (lay != layer)
                        if (lay.layerObjects.Contains(tile))
                            lay.layerObjects.Remove(tile);

            layer.layerObjects.Add(tile);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void RemoveTileFromLayer(TileObject tile, TagLayer layer)
        {
            if(layer.layerObjects.Contains(tile))
                layer.layerObjects.Remove(tile);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void RemoveFromAtlas(TileObject tile)
        {
            foreach (var layer in layers)
                if (layer.layerObjects.Contains(tile))
                    RemoveTileFromLayer(tile, layer);
            TileFromGuid.Remove(tile.guid);
            tiles.Remove(tile);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void RemoveLayer(TagLayer layer)
        {
            layers.Remove(layer);
            UnityEditor.EditorUtility.SetDirty(this);
        }

#endif

        public TileObject GetTile(int index)
        {
            return tiles[index];
        }

    }
}