using elZach.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace elZach.LevelEditor
{
    public class TileBehaviourPlaceOnGround : TileBehaviourBase
    {
        public bool placeAgainOnUpdate;

        public override void OnPlacement(PlacedTile placedTile, params PlacedTile[] neighbours)
        {
            Place(placedTile);
            
        }

        public override void OnUpdatedNeighbour(PlacedTile placedTile, params PlacedTile[] neighbours)
        {
            if(placeAgainOnUpdate)
                Place(placedTile);
        }

        public void Place(PlacedTile placedTile)
        {
            var myColliders = placedTile.placedObject.GetComponentsInChildren<Collider>();
            var placedTransform = placedTile.placedObject.transform;
            var startPoint = new Vector3(
                placedTransform.position.x,
                (placedTile.position.y + 1) * placedTile.layer.rasterSize.y,
                placedTransform.position.z);
            float closestDistance = placedTile.layer.rasterSize.y;
            foreach (var hit in Physics.RaycastAll(startPoint, Vector3.down, placedTile.layer.rasterSize.y))
            {
                if (myColliders.Contains(hit.collider)) continue;
                closestDistance = Mathf.Min(closestDistance, hit.distance);
            }

            placedTransform.position = startPoint + Vector3.down * closestDistance;
        }
    }
}