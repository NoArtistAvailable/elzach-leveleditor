using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace elZach.LevelEditor
{
    public class TileBehaviourRandomPosition : TileBehaviourBase
    {
        [Range(0f,1f)]
        public float xRange, yRange, zRange;

        public override void OnPlacement(PlacedTile placedTile, params PlacedTile[] neighbours)
        {
            Vector3 rasterSize = placedTile.layer.rasterSize;
            placedTile.placedObject.transform.position += new Vector3(
                placedTile.layer.rasterSize.x * (Random.value - 0.5f) * xRange,
                placedTile.layer.rasterSize.y * (Random.value) * yRange,
                placedTile.layer.rasterSize.z * (Random.value - 0.5f) * zRange
                );
        }

        public override void OnUpdatedNeighbour(PlacedTile placedTile, params PlacedTile[] neighbours)
        {
            //throw new System.NotImplementedException();
        }
    }
}