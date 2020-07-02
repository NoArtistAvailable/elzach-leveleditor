using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace elZach.LevelEditor
{
    public abstract class TileBehaviourBase : ScriptableObject, ITileBehaviour
    {
        public abstract void OnPlacement(PlacedTile placedTile, params PlacedTile[] neighbours);
        public abstract void OnUpdatedNeighbour(PlacedTile placedTile, params PlacedTile[] neighbours);
    }
}