using elZach.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using elZach.common;

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
                color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 0.5f, 0.6f, 0.8f);
            }
            public TagLayer(string name, Color color, Vector3 size)
            {
                this.name = name;
                this.color = color;
                this.rasterSize = size;
            }

        }


        public List<TileObject> tiles;
        public SerializableDictionary<string, TileObject> TileFromGuid = new SerializableDictionary<string, TileObject>();
        public TagLayer defaultLayer = new TagLayer("Default", Color.gray, Vector3.one);
        public List<TagLayer> layers = new List<TagLayer>();
        

#if UNITY_EDITOR
        [Button("Dictionary From List")]
        public void GetDictionaryFromList()
        {
            TileFromGuid.Clear();
            foreach(var tile in tiles)
            {
                TileFromGuid.Add(tile.guid, tile);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        public TileObject GetTile(int index)
        {
            return tiles[index];
        }

    }
}