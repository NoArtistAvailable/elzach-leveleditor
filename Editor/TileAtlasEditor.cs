using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace elZach.LevelEditor
{
    [CustomEditor(typeof(TileAtlas))]
    public class TileAtlasEditor : Editor
    {
        override public void OnInspectorGUI()
        {
            var t = target as TileAtlas;
            if (GUILayout.Button("Create Dictionary From List"))
            {
                t.GetDictionaryFromList();
            }
            DrawDefaultInspector();
            if(GUILayout.Button("Add all GameObjects from Folder"))
            {
                string path = EditorUtility.OpenFolderPanel("Folder to copy Gameobjects from", "Assets/", "");
                string homePath = AssetDatabase.GetAssetPath(t);
                homePath = homePath.Substring(0, homePath.Length - 6); // .as set
                if (!AssetDatabase.IsValidFolder(homePath))
                {
                    AssetDatabase.CreateFolder(homePath.Substring(0, homePath.Length - t.name.Length - 1), t.name);
                    AssetDatabase.Refresh();
                }
                foreach (var go in GetAtPath<GameObject>(path, false))
                {
                    t.tiles.AddRange(TileObject.CreateTileObjectsAt(homePath, go));
                    EditorUtility.SetDirty(t);
                }
            }
            if(GUILayout.Button("Add all GameObjects from Subfolders"))
            {
                string path = EditorUtility.OpenFolderPanel("Folder to copy Gameobjects from", "Assets/", "");
                string homePath = AssetDatabase.GetAssetPath(t);
                homePath = homePath.Substring(0, homePath.Length - 6); // .as set
                if (!AssetDatabase.IsValidFolder(homePath))
                {
                    AssetDatabase.CreateFolder(homePath.Substring(0,homePath.Length - t.name.Length-1), t.name);
                    AssetDatabase.Refresh();
                }
                foreach (var go in GetAtSubFolders<GameObject>(path, false))
                {
                    t.tiles.AddRange(TileObject.CreateTileObjectsAt(homePath, go));
                    EditorUtility.SetDirty(t);
                }
            }
        }


        public static T[] GetAtSubFolders<T>(string path, bool isRelativePath)
        {
            List<T> list = new List<T>();
            //Debug.Log("path: " + path + "; " + isRelativePath);
            string relativePath = isRelativePath ? path : ("Assets" + path.Substring(Application.dataPath.Length));
            //Debug.Log("relativePath: " + relativePath);
            string[] subFolders = AssetDatabase.GetSubFolders(relativePath);
            foreach(var sub in subFolders)
            {
                list.AddRange(GetAtPath<T>(sub, true));
                //Debug.Log(list.Count);
                list.AddRange(GetAtSubFolders<T>(sub, true));
                //Debug.Log(sub);
            }
            return list.ToArray();
        }

        public static T[] GetAtPath<T>(string path, bool isRelativePath)
        {
            string absolutePath = isRelativePath ? (Application.dataPath + path.Substring(6)) : path;
            string relativePath = isRelativePath ? path : ("Assets" + path.Substring(Application.dataPath.Length));
            //Debug.Log("relativePath: " + relativePath + "; absolute path: " + absolutePath);
            if (!AssetDatabase.IsValidFolder(relativePath)) return new T[0];
            //Debug.Log("is vlaid path");
            ArrayList al = new ArrayList();
            string[] fileEntries = Directory.GetFiles(absolutePath);
            //Debug.Log("FilesCount: " + fileEntries.Length);
            foreach (string fileName in fileEntries)
            {
                string localPath = "Assets" + fileName.Substring(Application.dataPath.Length);
                Object t = AssetDatabase.LoadAssetAtPath(localPath, typeof(T));

                if (t != null)
                    al.Add(t);
            }
            T[] result = new T[al.Count];
            for (int i = 0; i < al.Count; i++)
                result[i] = (T)al[i];

            return result;
        }
    }
}