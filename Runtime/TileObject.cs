using elZach.Common;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace elZach.LevelEditor
{
    [CreateAssetMenu(menuName = "LevelEditor/TileObject", fileName = "TileObject")]
    public class TileObject : ScriptableObject
    {
        [System.Serializable]
        public class Variant
        {
            public GameObject prefab;
            public Neighbour condition;
            public Vector3 position = Vector3.zero;
            public Vector3 rotation = Vector3.zero;
            public Vector3 scale = Vector3.one;
        }

        [System.Flags]
        public enum Neighbour { None = 1 << 0, FrontLeft = 1 << 1, Front = 1 << 2, FrontRight = 1 << 3, Left = 1 << 4, Right = 1 << 5, BackLeft = 1 << 6, Back = 1 << 7, BackRight = 1 << 8 }

        public GameObject prefab;
        public string guid = System.Guid.NewGuid().ToString();
        public TileBehaviour behaviour;
        public int3 size = new int3(1, 1, 1);

        [Reorderable]
        public Variant[] variants;

        public TileObject()
        {
            guid = System.Guid.NewGuid().ToString();
        }

#if UNITY_EDITOR
        [Button("Get new guid")]
        public void GetNewGuid()
        {
            guid = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}