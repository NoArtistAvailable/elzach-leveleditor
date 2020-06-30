using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace elZach.LevelEditor
{
    public class TileBehaviour : ScriptableObject, ITileBehaviour
    {
        public enum RotationBehaviour { None, RotateRandomY = 1 << 1, RotateRandomY90Degree = 1 << 2, AlignToNeighbours = 1 << 3 }
        public RotationBehaviour rotation;

        public virtual void OnPlacement(PlacedTile placedTile, params PlacedTile[] neighbours)
        {
            switch (rotation)
            {
                case RotationBehaviour.RotateRandomY:
                    placedTile.placedObject.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f);
                    break;
                case RotationBehaviour.RotateRandomY90Degree:
                    placedTile.placedObject.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 4) * 90f, 0f);
                    break;
                case RotationBehaviour.AlignToNeighbours:
                    AlignRotationToNeighbours(placedTile, neighbours);
                    break;
            }
        }

        public virtual void OnUpdatedNeighbour(PlacedTile placedTile, params PlacedTile[] neighbours)
        {
            if(rotation == RotationBehaviour.AlignToNeighbours)
                AlignRotationToNeighbours(placedTile, neighbours);
        }


#if UNITY_EDITOR
        public void AlignRotationToNeighbours(PlacedTile placedTile, params PlacedTile[] neighbours)
        {
            if (neighbours.Length == 0) return;
            TileObject.Neighbour neighbourMask=TileObject.Neighbour.None;
            foreach(var neighbour in neighbours)
            {
                if (neighbour.guid != placedTile.guid) continue;
                int3 relative = neighbour.position - placedTile.position;
                neighbourMask |= GetNeighbourEnumFromRelative(relative);
                //if (relative.x != 0) // has neighbour on x
                //    placedTile.placedObject.transform.localRotation = Quaternion.identity;
                //else
                //    placedTile.placedObject.transform.localRotation = Quaternion.Euler(Vector3.up * 90f);
            }

            foreach(var variant in placedTile.tileObject.variants)
            {
                if (neighbourMask.HasFlag(variant.condition))
                {
                    //GameObject linkedPrefab = (GameObject)UnityEditor.PrefabUtility.GetPrefabInstanceHandle(placedTile.placedObject) as GameObject;
                    if (true)//(linkedPrefab != variant.prefab)
                    {
                        //Debug.Log("[TileBehaviour] Replacing " + linkedPrefab.name + " by " + variant.prefab.name);
                        var parent = placedTile.placedObject.transform.parent;
                        var position = placedTile.placedObject.transform.localPosition;
                        DestroyImmediate(placedTile.placedObject);
                        placedTile.placedObject = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(variant.prefab) as GameObject;
                        placedTile.placedObject.transform.SetParent(parent);
                        placedTile.placedObject.transform.localPosition = position + variant.position;
                    }
                    var transf = placedTile.placedObject.transform;
                    transf.localRotation = Quaternion.Euler(variant.rotation);
                    transf.localScale = variant.scale;
                    return;
                }
            }

            //if no variants can be applied - rotate to neighbour
            if(neighbourMask.HasFlag(TileObject.Neighbour.Left) || neighbourMask.HasFlag(TileObject.Neighbour.Right))
                placedTile.placedObject.transform.localRotation = Quaternion.identity;
            else
                placedTile.placedObject.transform.localRotation = Quaternion.Euler(Vector3.up * 90f);
        }
#endif
        //--------probably should be moved into a different class---------//
        int3 compare_left = new int3(-1, 0, 0), compare_right = new int3(1, 0, 0), compare_front = new int3(0, 0, 1), compare_back = new int3(0, 0, -1),
            compare_frontLeft = new int3(-1, 0, 1), compare_frontRight = new int3(1, 0, 1),
            compare_backLeft = new int3(-1, 0, -1), compare_backRight = new int3(1, 0, -1);

        public TileObject.Neighbour GetNeighbourEnumFromRelative(int3 relativePosition)
        {
            if (relativePosition.Equals(compare_back)) return TileObject.Neighbour.Back;
            if (relativePosition.Equals(compare_front)) return TileObject.Neighbour.Front;
            if (relativePosition.Equals(compare_left)) return TileObject.Neighbour.Left;
            if (relativePosition.Equals(compare_right)) return TileObject.Neighbour.Right;

            if (relativePosition.Equals(compare_frontLeft)) return TileObject.Neighbour.FrontLeft;
            if (relativePosition.Equals(compare_frontRight)) return TileObject.Neighbour.FrontRight;
            if (relativePosition.Equals(compare_backLeft)) return TileObject.Neighbour.BackLeft;
            if (relativePosition.Equals(compare_backRight)) return TileObject.Neighbour.BackRight;

            return TileObject.Neighbour.None;
        }
    }
}