using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using elZach.common;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace elZach.LevelEditor
{
    [ExecuteAlways]
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Setup")]
        public TileAtlas tileSet;
        public Vector3 floorBoundaries = new Vector3(20, 5, 20);

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

        public List<SerializableDictionary<int3, PlacedTile>> layers = new  List<SerializableDictionary<int3, PlacedTile>>();

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

#if UNITY_EDITOR

        public void PlaceTile(string guid, int3 position, int layerIndex)
        {
            TileObject atlasTile;
            tileSet.TileFromGuid.TryGetValue(guid, out atlasTile);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(atlasTile.prefab) as GameObject;
            if (!go) { Debug.LogError("No GameObject found at Tile with guid " + guid); return; }

            if (layerIndex >= layers.Count)
                for (int i = 0; i <= layerIndex - layers.Count; i++)
                    layers.Add(new SerializableDictionary<int3, PlacedTile>());

            for (int x = 0; x < atlasTile.size.x; x++)
                for (int y = 0; y < atlasTile.size.y; y++)
                    for (int z = 0; z < atlasTile.size.z; z++)
                    {
                        PlacedTile alreadyPlaced;
                        if (layers[layerIndex].TryGetValue(position + new int3(x, y, z), out alreadyPlaced))
                        {
                            RemoveTile(alreadyPlaced, layerIndex);
                        }
                    }
            
            go.transform.SetParent(transform);

            //atlasTile.size

            go.transform.localPosition = TilePositionToLocalPosition(position, atlasTile.size, tileSet.layers[layerIndex]);
            var placedTile = new PlacedTile(guid, position, atlasTile, go);
            for (int x = 0; x < atlasTile.size.x; x++)
                for (int y = 0; y < atlasTile.size.y; y++)
                    for (int z = 0; z < atlasTile.size.z; z++)
                        layers[layerIndex].Add(position + new int3(x, y, z), placedTile);
            var neighbs = GetNeighbours(placedTile, layerIndex);
            if (atlasTile.behaviour) atlasTile.behaviour.OnPlacement(placedTile, neighbs);
            foreach (var neighb in neighbs)
                neighb.tileObject.behaviour?.OnUpdatedNeighbour(neighb, GetNeighbours(neighb, layerIndex));
            EditorUtility.SetDirty(gameObject);
        }

        public void RemoveTile(int3 position, int layerIndex)
        {
            PlacedTile target;
            if (layers[layerIndex].TryGetValue(position, out target))
            {
                RemoveTile(target,layerIndex);
            }
        }

        public void RemoveTile(PlacedTile target, int layerIndex)
        {
            DestroyImmediate(target.placedObject);
            for (int x2 = 0; x2 < target.tileObject.size.x; x2++)
                for (int y2 = 0; y2 < target.tileObject.size.y; y2++)
                    for (int z2 = 0; z2 < target.tileObject.size.z; z2++)
                    {
                        layers[layerIndex].Remove(target.position + new int3(x2, y2, z2));
                    }
        }

        public void ClearLevel()
        {
            foreach (var layer in layers)
            {
                foreach (var tile in layer.Values)
                {
                    DestroyImmediate(tile.placedObject);
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

        public void ToggleLayerActive(bool value, int index)
        {
            foreach (var placedTile in layers[index].Values)
                placedTile.placedObject.SetActive(value);
        }

#endif

    }
}