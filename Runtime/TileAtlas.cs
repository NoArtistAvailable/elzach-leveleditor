using elZach.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

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
        public SerializableDictionary<string, TileObject> TileFromGuid = new SerializableDictionary<string, TileObject>();
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
                if (TileFromGuid.ContainsKey(tile.guid))
                {
                    Debug.LogWarning("Guid already exists in Dictionary, make sure that each tile has a unique ID. Removed Tile: " + tile.name + " from atlas " + this.name);
                    tiles.Remove(tile);
                    return;
                }
                TileFromGuid.Add(tile.guid, tile);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        public TileObject GetTile(int index)
        {
            return tiles[index];
        }

        public void MoveTileToLayer(TileObject tile, TagLayer layer, bool allowMultiLayer = false)
        {
            if (!tiles.Contains(tile)) { Debug.LogWarning("Atlas doesnt contain tile "+tile.name, this); return; }
            if (layer.layerObjects.Contains(tile)) { Debug.Log("Layer already contains tile "+tile.name, this); return; }

            if (!allowMultiLayer)
                foreach(var lay in layers)
                    if (lay != layer)
                        if(lay.layerObjects.Contains(tile))
                            lay.layerObjects.Remove(tile);

            layer.layerObjects.Add(tile);
        }

        public void RemoveLayer(TagLayer layer)
        {
            layers.Remove(layer);
        }

    }
}