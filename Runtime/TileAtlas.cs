using elZach.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using elZach.common;

namespace elZach.LevelEditor
{
    public class TileAtlas : ScriptableObject
    {
        public List<TileObject> tiles;
        public SerializableDictionary<string, TileObject> TileFromGuid = new SerializableDictionary<string, TileObject>();

        //[Button("Add New Tile")]
        //public void NewAtlasTile()
        //{
        //    var newTile = new AtlasTile();
        //    tiles.Add(newTile);
        //    TileFromGuid.Add(newTile.guid, newTile);
        //}

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