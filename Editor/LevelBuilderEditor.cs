using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace elZach.LevelEditor
{
    [CustomEditor(typeof(LevelBuilder))]
    public class LevelBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if(GUILayout.Button("Level Builder Window"))
            {
                LevelBuilderWindow.Init();
            }

            DrawDefaultInspector();
        }
    }
}