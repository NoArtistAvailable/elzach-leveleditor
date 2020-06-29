using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace elZach.LevelEditor
{
    public class LevelBuilderWindow : EditorWindow
    {
        [MenuItem("Window/LevelBuilder")]
        static void Init()
        {
            LevelBuilderWindow window = (LevelBuilderWindow)EditorWindow.GetWindow(typeof(LevelBuilderWindow));
            window.titleContent = new GUIContent("Level Builder");
            window.minSize = new Vector2(100, 100);
            window.floorPlane = new Plane(Vector3.up, t.transform.position);
            window.Show();
        }

        static LevelBuilder t
        {
            get
            {
                if (!_t)
                {
                    _t = FindObjectOfType<LevelBuilder>();
                    if (!_t)
                    {
                        var go = new GameObject("Level Builder");
                        _t = go.AddComponent<LevelBuilder>();
                    }
                }
                return _t;
            }
        }
        static LevelBuilder _t;
        bool painting = true;
        Plane floorPlane;
        int3 tileMousePosition;
        int activeLayer;
        int targetHeigth = 0;
        bool[] _layerVis;
        bool[] layerVisibility { get { if (_layerVis == null) _layerVis = new bool[t.layers.Count]; return _layerVis; } }

        public enum RasterVisibility { None, WhenPainting, Always }
        public RasterVisibility rasterVisibility = RasterVisibility.Always;

        private void OnGUI()
        {
            painting = GUILayout.Toggle(painting, "painting","Button");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Grid Visibility ");
            rasterVisibility = (RasterVisibility) EditorGUILayout.EnumPopup(rasterVisibility);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            targetHeigth = EditorGUILayout.IntField("Heigth: ", targetHeigth);
            activeLayer = EditorGUILayout.IntField("Layer:", activeLayer);
            EditorGUILayout.EndHorizontal();
            DrawPalette();
            if(GUILayout.Button("Clear Level"))t.ClearLevel(); 
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if(painting) sceneView.Repaint(); // <- is this neccessary? 2020.2.0a15 seems to call repaint just every few OnScenGUI
            DrawRaster();
            OnScreenUI(sceneView);
            floorPlane = new Plane(Vector3.up, t.transform.position + Vector3.up *t.rasterSize.y*targetHeigth);
            var e = Event.current;
            if (painting)
            {
                if (e.isMouse)
                {
                    Ray guiRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    float d;
                    floorPlane.Raycast(guiRay, out d);
                    Vector3 worldPos = guiRay.GetPoint(d);
                    //Debug.Log("worldPos: " + worldPos+"; mousePos: "+tileMousePosition);
                    var newTileMousePosition = t.WorldPositionToTilePosition(worldPos);
                    if (t.TilePositionInFloorSize(newTileMousePosition))
                        tileMousePosition = newTileMousePosition;
                }
                Handles.DrawWireDisc(new Vector3(tileMousePosition.x + 0.5f, tileMousePosition.y, tileMousePosition.z + 0.5f), Vector3.up, 0.2f);
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.modifiers == EventModifiers.None)
                    DrawTiles(sceneView, e, tileMousePosition);
                else if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 1 && e.modifiers == EventModifiers.None)
                    EraseTiles(sceneView, e, tileMousePosition);
            }
        }

        void OnScreenUI(SceneView sceneView)
        {
            if (!painting) return;
            Handles.BeginGUI();
            var icon_eye = EditorGUIUtility.IconContent("VisibilityOn");
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            //targetHeigth = EditorGUILayout.IntSlider(targetHeigth, 0, t.floorSize.y);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            targetHeigth = Mathf.RoundToInt(GUILayout.VerticalSlider(targetHeigth, t.floorSize.y, -t.floorSize.y, GUILayout.Height(100), GUILayout.Width(12)));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            for (int i = 0; i < t.layers.Count; i++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(i == activeLayer, ("Layer " + i), "Button", GUILayout.Width(100))) { activeLayer = i; }
                bool vis = layerVisibility[i];
                vis = GUILayout.Toggle(vis, icon_eye, "Button", GUILayout.Width(30), GUILayout.Height(19));
                if(vis != layerVisibility[i])
                {
                    layerVisibility[i] = vis;
                    t.ToggleLayerActive(vis, i);
                }
                GUILayout.Label(t.layers[i].Keys.Count.ToString(), "HelpBox", GUILayout.Height(19));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            Handles.EndGUI();
        }

        void DrawRaster()
        {
            if (rasterVisibility == RasterVisibility.Always || (rasterVisibility == RasterVisibility.WhenPainting && painting))
            {
                Handles.matrix = t.transform.localToWorldMatrix;
                Handles.color = new Color(0f, 0.5f, 0.8f);
                float zOffset = t.rasterSize.z * t.floorSize.z * 0.5f;
                for (int x = -Mathf.FloorToInt(t.floorSize.x / 2); x < Mathf.CeilToInt(t.floorSize.x / 2); x++)
                {
                    Handles.DrawLine(new Vector3(x * t.rasterSize.x, targetHeigth * t.rasterSize.y, -zOffset), new Vector3(x * t.rasterSize.x, targetHeigth * t.rasterSize.y, zOffset));
                }
                float xOffset = t.rasterSize.x * t.floorSize.x * 0.5f;
                for (int z = -Mathf.FloorToInt(t.floorSize.z / 2); z < Mathf.CeilToInt(t.floorSize.z / 2); z++)
                {
                    Handles.DrawLine(new Vector3(-xOffset, targetHeigth * t.rasterSize.y, z * t.rasterSize.z), new Vector3(xOffset, targetHeigth * t.rasterSize.y, z * t.rasterSize.z));
                }
                if (painting)
                {
                    Vector3 handle = Handles.PositionHandle(new Vector3(-Mathf.FloorToInt(t.floorSize.x / 2), targetHeigth, -Mathf.FloorToInt(t.floorSize.z / 2)), Quaternion.identity);
                    t.floorSize = new int3(Mathf.FloorToInt(handle.x * -2), t.floorSize.y, Mathf.FloorToInt(handle.z * -2));
                    targetHeigth = Mathf.Clamp(Mathf.FloorToInt(handle.y), -t.floorSize.y, t.floorSize.y);
                }
                //Handles.FreeMoveHandle(new Vector3(-Mathf.FloorToInt(t.floorSize.x / 2), targetHeigth, -Mathf.FloorToInt(t.floorSize.z / 2)), Quaternion.identity, 1f, Vector3.one);
            }
        }

        string selectedTileGuid;
        int paletteIndex;
        Vector2 paletteScroll;
        void DrawPalette()
        {
            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll);
            List<GUIContent> paletteIcons = new List<GUIContent>();
            foreach (var atlasTile in t.tileSet.TileFromGuid.Values)
            {
                // Get a preview for the prefab
                if (!atlasTile) continue;
                Texture2D texture = AssetPreview.GetAssetPreview(atlasTile.prefab);
                paletteIcons.Add(new GUIContent(texture));
            }

            // Display the grid
            paletteIndex = GUILayout.SelectionGrid(paletteIndex, paletteIcons.ToArray(), 4, GUILayout.Width(position.width-20));
            selectedTileGuid = t.tileSet.tiles[paletteIndex].guid;
            EditorGUILayout.EndScrollView();
        }

        private void DrawTiles(SceneView view, Event e, int3 tilePosition)
        {
            GUIUtility.hotControl = 0;
            
            //Vector2 mousePos = e.mousePosition;
            //mousePos.y = view.camera.pixelHeight - mousePos.y;
            //Ray ray = view.camera.ScreenPointToRay(mousePos);
            //float d;
            //floorPlane.Raycast(ray, out d);
            //var worldPos = ray.GetPoint(d);
            //var tilePos = t.WorldPositionToTilePosition(worldPos);
            //Debug.Log("WorldPos: " + worldPos + " : TilePos " + tilePos);
            t.PlaceTile(selectedTileGuid, tilePosition, activeLayer);
            e.Use();
        }

        void EraseTiles(SceneView view, Event e, int3 tilePosition)
        {
            GUIUtility.hotControl = 0;
            //EditorGUIUtility.AddCursorRect(view.position, MouseCursor.ArrowMinus);
            //Vector2 mousePos = e.mousePosition;
            //mousePos.y = view.camera.pixelHeight - mousePos.y;
            //Ray ray = view.camera.ScreenPointToRay(mousePos);
            //float d;
            //floorPlane.Raycast(ray, out d);
            //var worldPos = ray.GetPoint(d);
            //var tilePos = t.WorldPositionToTilePosition(worldPos);
            //Debug.Log("WorldPos: " + worldPos + " : TilePos " + tilePos);
            t.RemoveTile(tilePosition, activeLayer);
            e.Use();
        }

        void OnFocus()
        {
            SceneView.duringSceneGui -= this.OnSceneGUI; // Just in case
            SceneView.duringSceneGui += this.OnSceneGUI;
        }

        void OnDestroy()
        {
            SceneView.duringSceneGui -= this.OnSceneGUI;
        }

    }
}