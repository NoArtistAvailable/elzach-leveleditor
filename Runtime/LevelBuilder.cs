using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using elZach.Common; 
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace elZach.LevelEditor
{
    [ExecuteAlways]
    public class LevelBuilder : MonoBehaviour
    {

        [System.Serializable]
        public class IntTileDictionary : SerializableDictionary<int3,PlacedTile> { }

        [Header("Setup")]
        public TileAtlas tileSet;
        public Vector3 floorBoundaries = new Vector3(20, 5, 20);
        [HideInInspector]
        public List<IntTileDictionary> layers = new  List<IntTileDictionary>();

        public int3 FloorSize(TileAtlas.TagLayer layer)
        {
            return new int3(
                Mathf.FloorToInt(floorBoundaries.x / layer.rasterSize.x),
                Mathf.FloorToInt(floorBoundaries.y / layer.rasterSize.y),
                Mathf.FloorToInt(floorBoundaries.z / layer.rasterSize.z));
        }

        public void ChangeFloorBoundaries(int3 floorSize, TileAtlas.TagLayer layer)
        {
            floorBoundaries = new Vector3(
                floorSize.x * layer.rasterSize.x,
                floorSize.y * layer.rasterSize.y,
                floorSize.z * layer.rasterSize.z
                );
        }

        public bool TilePositionInFloorSize(int3 tilePos, TileAtlas.TagLayer layer)
        {
            var floorSize = FloorSize(layer);
            var rasterSize = layer.rasterSize;
            int halfX = Mathf.FloorToInt(floorSize.x / 2f); // * rasterSize.x / 2f);
            int halfZ = Mathf.FloorToInt(floorSize.z / 2f); // * rasterSize.z / 2f);
            if (tilePos.x < -halfX || tilePos.x >= halfX)
                return false;
            if (tilePos.y < -floorSize.y || tilePos.y > floorSize.y)
                return false;
            if (tilePos.z < -halfZ || tilePos.z >= halfZ)
                return false;
            return true;
        }

        public int3 WorldPositionToTilePosition(Vector3 worldPos, TileAtlas.TagLayer layer)
        {
            var rasterSize = layer.rasterSize;
            var localP = transform.InverseTransformPoint(worldPos);
            return new int3(Mathf.FloorToInt(localP.x / rasterSize.x), Mathf.RoundToInt(localP.y / rasterSize.y), Mathf.FloorToInt(localP.z / rasterSize.z));
        }

        public Vector3 TilePositionToLocalPosition(int3 tilePos, TileAtlas.TagLayer layer)
        {
            var rasterSize = layer.rasterSize;
            return new Vector3(
                tilePos.x * rasterSize.x + rasterSize.x * 0.5f,
                tilePos.y * rasterSize.y,
                tilePos.z * rasterSize.z + rasterSize.z * 0.5f
                );
        }

        public Vector3 TilePositionToLocalPosition(int3 tilePos, int3 size, TileAtlas.TagLayer layer)
        {
            var rasterSize = layer.rasterSize;
            return new Vector3(
                tilePos.x * rasterSize.x + rasterSize.x * 0.5f * size.x,
                tilePos.y * rasterSize.y,
                tilePos.z * rasterSize.z + rasterSize.z * 0.5f * size.z
                );
        }

        public PlacedTile[] GetNeighbours(PlacedTile target, int layerIndex)
        {
            List<PlacedTile> neighbs = new List<PlacedTile>();
            // for now only get direct neighbours
            PlacedTile neighb;
            if (layers[layerIndex].TryGetValue(target.position + new int3(-1, 0, 0), out neighb))
                neighbs.Add(neighb);
            if (layers[layerIndex].TryGetValue(target.position + new int3(1, 0, 0), out neighb))
                neighbs.Add(neighb);
            if (layers[layerIndex].TryGetValue(target.position + new int3(0, 0, -1), out neighb))
                neighbs.Add(neighb);
            if (layers[layerIndex].TryGetValue(target.position + new int3(0, 0, 1), out neighb))
                neighbs.Add(neighb);

            return neighbs.ToArray();
        }

        public PlacedTile GetTile(int3 pos, int layerIndex)
        {
            if (layerIndex >= layers.Count) return null;
            layers[layerIndex].TryGetValue(pos, out var foundTile);
            return foundTile;
        }

#if UNITY_EDITOR

        public void PlaceTile(string guid, int3 position, Vector3 euler, int layerIndex, TileAtlas.TagLayer tagLayer, bool updateNeighbours = true)
        {
            Undo.RegisterCompleteObjectUndo(this, "Created New Tile Object");
            TileObject atlasTile;
            tileSet.TileFromGuid.TryGetValue(guid, out atlasTile);
            if (!atlasTile) { Debug.LogError("No Tile with guid [" + guid + "] found in atlas " + tileSet.name, tileSet); return; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(atlasTile.prefab) as GameObject;
            
            if (!go) { Debug.LogError("No GameObject found at Tile with guid " + guid); return; }

            if (layerIndex >= layers.Count)
                for (int i = 0; i <= layerIndex - layers.Count; i++)
                    layers.Add(new IntTileDictionary());

            int3 tileSize = atlasTile.GetSize(tagLayer.rasterSize);            
            go.transform.SetParent(transform);

            //atlasTile.size

            go.transform.localPosition = TilePositionToLocalPosition(position, tileSize, tileSet.layers[layerIndex]);
            go.transform.localRotation = Quaternion.Euler(euler);
            var placedTile = new PlacedTile(guid, position, atlasTile, go, tagLayer);
            for (int x = 0; x < tileSize.x; x++)
                for (int y = 0; y < tileSize.y; y++)
                    for (int z = 0; z < tileSize.z; z++)
                    {
                        var pos = position + new int3(x, y, z);
                        PlacedTile alreadyPlaced;
                        if (layers[layerIndex].TryGetValue(pos, out alreadyPlaced))
                            RemoveTile(alreadyPlaced, layerIndex);
                        if (layers[layerIndex].ContainsKey(pos)) layers[layerIndex].Remove(pos);
                        layers[layerIndex].Add(pos, placedTile);
                    }
            var neighbs = GetNeighbours(placedTile, layerIndex);
            atlasTile.PlaceBehaviour(placedTile, neighbs);
            if (updateNeighbours)
                foreach (var neighb in neighbs)
                {
                    //Undo.RecordObject(neighb.placedObject, "Before Updating Object");
                    neighb.tileObject.UpdateBehaviour(neighb, GetNeighbours(neighb, layerIndex));
                }

            Undo.RegisterCreatedObjectUndo(placedTile.placedObject, "Created New Tile Object");
            EditorUtility.SetDirty(gameObject);
        }

        public void UpdateMultiple(int3 startPosition, int3 endPosition, int layerIndex)
        {
            Debug.LogWarning("Doesnt seem to work for now");
            int3 minPosition = math.min(startPosition, endPosition);
            int3 maxPosition = math.max(startPosition, endPosition);
            int y = minPosition.y;
            for(int x = 0; x <= endPosition.x - startPosition.x; x++)
                for (int z = 0; z <= endPosition.z - startPosition.z; z++)
                {
                    PlacedTile val;
                    if (layers[layerIndex].TryGetValue(new int3(x, y, z), out val))
                        val.tileObject.UpdateBehaviour(val, GetNeighbours(val, layerIndex));
                }
        }

        public void RemoveTile(int3 position, int layerIndex)
        {
            if (layerIndex >= layers.Count) return;
            PlacedTile target;
            if (layers[layerIndex].TryGetValue(position, out target))
            {
                RemoveTile(target,layerIndex);
            }
        }

        public void RemoveTile(PlacedTile target, int layerIndex)
        {
            Undo.RegisterCompleteObjectUndo(this, "Before Removed TileObject");
            if (target.placedObject) Undo.DestroyObjectImmediate(target.placedObject);
            
            //DestroyImmediate(target.placedObject);
            int3 tileSize = target.tileObject.GetSize(target.layer.rasterSize);
            for (int x2 = 0; x2 < tileSize.x; x2++)
                for (int y2 = 0; y2 < tileSize.y; y2++)
                    for (int z2 = 0; z2 < tileSize.z; z2++)
                    {
                        layers[layerIndex].Remove(target.position + new int3(x2, y2, z2));
                    }
        }

        public void RemovePlacedTiles(TileObject tile, int layerIndex)
        {
            Undo.RegisterCompleteObjectUndo(this, "Before RemovedPlaced Tiles");
            List<int3> keysToRemove = new List<int3>();
            foreach (var kvp in layers[layerIndex])
            {
                if (kvp.Value.tileObject == tile)
                {
                    if (kvp.Value.placedObject)
                    {
                        Undo.DestroyObjectImmediate(kvp.Value.placedObject);
                        DestroyImmediate(kvp.Value.placedObject);
                    }
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
                layers[layerIndex].Remove(key);
        }

        public void ClearLevel()
        {
            Undo.RegisterCompleteObjectUndo(this, "Before Clearing Level");
            foreach (var layer in layers)
            {
                foreach (var tile in layer.Values)
                {
                    if (tile.placedObject)
                    {
                        Undo.DestroyObjectImmediate(tile.placedObject);
                        DestroyImmediate(tile.placedObject);
                    }
                }
                layer.Clear();
            }
        }

        public void ClearLayer(int index)
        {
            var layer = layers[index];
            foreach (var tile in layer.Values)
            {
                DestroyImmediate(tile.placedObject);
            }
            layer.Clear();
        }

        public void RemoveLayer(int index)
        {
            if (layers.Count <= index) return;
            Debug.Log("Removing Layer [" + index + "] of "+layers.Count);
            ClearLayer(index);
            layers.RemoveAt(index);
        }

        public void ToggleLayerActive(bool value, int index)
        {
            foreach (var placedTile in layers[index].Values)
                placedTile.placedObject.SetActive(value);
        }

        public void UpdateTilePositionsOnLayer(TileAtlas.TagLayer layer, int index)
        {
            if (index == -1) return; // there should be no tiles on defaultlayer
            Debug.Log("[LevelBuilder] Update Position called on layer " + layer.name + " and index " + index);
            foreach(var placed in layers[index].Values)
            {
                placed.placedObject.transform.localPosition = TilePositionToLocalPosition(placed.position, layer);
            }
        }

        public void MoveExistingTilesToLayer(TileObject tile, int fromIndex, int toIndex)
        {
            Debug.Log("Moving all tiles " + tile.name + " from " + fromIndex + " to index.");
            if (fromIndex >= layers.Count) return;
            if (toIndex >= layers.Count)
                for (int i = 0; i <= toIndex - layers.Count; i++)
                    layers.Add(new IntTileDictionary());

            for (int i = 0; i < layers.Count; i++)
            {
                if (i == toIndex) continue;
                if (layers[i].Any(x => x.Value.tileObject == tile))
                    MoveTilesFromLayerToLayer(tile, i, toIndex);
            }
        }

        private void MoveTilesFromLayerToLayer(TileObject tile, int fromIndex, int toIndex)
        {
            List<int3> keysToRemove = new List<int3>();
            foreach (var kvp in layers[fromIndex])
            {
                if (kvp.Value.tileObject == tile)
                {
                    if (!layers[toIndex].ContainsKey(kvp.Key))
                    {
                        //Debug.Log("Adding " + kvp.Key + " to layer " + toIndex);
                        layers[toIndex].Add(kvp.Key, kvp.Value);
                    }
                    else
                    {
                        //Debug.Log(kvp.Key + " already set in " + toIndex);
                        if(kvp.Value.placedObject)
                            DestroyImmediate(kvp.Value.placedObject);
                    }
                    //layers[fromIndex].Remove(kvp.Key);
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
                layers[fromIndex].Remove(key);
        }

#endif

    }
}