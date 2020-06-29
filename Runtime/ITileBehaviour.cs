using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace elZach.LevelEditor
{
    public interface ITileBehaviour
    {
        void OnPlacement(PlacedTile placedTile, params PlacedTile[] neighbours);
        void OnUpdatedNeighbour(PlacedTile placedTile, params PlacedTile[] neighbours);
    }
}