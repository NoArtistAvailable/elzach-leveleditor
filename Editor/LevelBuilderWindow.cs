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
            window.floorPlane = new Plane(Vector3.up, t ? t.transform.position : Vector3.zero);
            window.Show();
        }

        static LevelBuilder t
        {
            get
            {
                if (!_t)
                {
                    _t = FindObjectOfType<LevelBuilder>();
                    //if (!_t)
                    //{
                    //    var go = new GameObject("Level Builder");
                    //    _t = go.AddComponent<LevelBuilder>();
                    //    _t.tileSet = ScriptableObject.CreateInstance<TileAtlas>();
                    //    _t.tileSet.tiles.Add(ScriptableObject.CreateInstance<TileObject>());
                    //}
                }
                return _t;
            }
        }
        static LevelBuilder _t;
        bool painting = true;
        TileObject selectedTile;
        Plane floorPlane;
        int3 tileMousePosition;
        int targetHeigth = 0;
        bool[] _layerVis;
        bool[] layerVisibility { get
            {
                if (_layerVis == null) _layerVis = new bool[t.layers.Count];
                if (t.layers.Count > _layerVis.Length)
                {
                    bool[] biggerBoolArray = new bool[t.layers.Count];
                    for (int i = 0; i < _layerVis.Length; i++)
                        biggerBoolArray[i] = _layerVis[i];
                    _layerVis = biggerBoolArray;
                }
                return _layerVis;
            }
        }

        public enum RasterVisibility { None, WhenPainting, Always }
        public RasterVisibility rasterVisibility = RasterVisibility.WhenPainting;
        Color rasterColor = new Color(0f, 0.5f, 0.8f, 0.3f);

        private void OnGUI()
        {
            if (!CheckForRequirements()) return;
            EditorGUILayout.BeginHorizontal();
            painting = GUILayout.Toggle(painting, "painting","Button");
            EditorGUILayout.LabelField(new GUIContent(layerIndex+":"+paletteIndex),GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
            //if (painting)
            {
                //if (layerIndex < -1 || layerIndex >= t.tileSet.layers.Count) return;
                layerIndex = Mathf.Clamp(layerIndex, -1, t.tileSet.layers.Count - 1);
                paletteIndex = Mathf.Clamp(paletteIndex, 0, 
                    layerIndex == -1 ? (t.tileSet.tiles.Count-1) : (t.tileSet.layers[layerIndex].layerObjects.Count-1));
                paletteIndex = Mathf.Max(0, paletteIndex);
                if (layerIndex == -1)
                    selectedTileGuid = t.tileSet.tiles[paletteIndex].guid;
                else if (t.tileSet.layers[layerIndex]?.layerObjects.Count>0)
                    selectedTileGuid = t.tileSet.layers[layerIndex]?.layerObjects[paletteIndex]?.guid;
                if(!string.IsNullOrEmpty(selectedTileGuid))
                    t.tileSet.TileFromGuid.TryGetValue(selectedTileGuid,out selectedTile);
                if(!selectedTile)
                {
                    selectedTile = t.tileSet.tiles[0];
                    selectedTileGuid = selectedTile.guid;
                }
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Grid Visibility ");
            rasterVisibility = (RasterVisibility) EditorGUILayout.EnumPopup(rasterVisibility);
            rasterColor = EditorGUILayout.ColorField(rasterColor);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            targetHeigth = EditorGUILayout.IntField("Heigth: ", targetHeigth);
            // activeLayer = EditorGUILayout.IntField("Layer:", activeLayer);
            EditorGUILayout.EndHorizontal();
            Event e = Event.current;
            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                Rect myRect = GUILayoutUtility.GetRect(100, 40, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUI.Box(myRect, "Drag and Drop Prefabs to this Box!");
                if (myRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    }
                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        //Debug.Log(DragAndDrop.objectReferences.Length);
                        for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                        {
                            var draggedTile = DragAndDrop.objectReferences[i] as TileObject;
                            if (!draggedTile)
                            {
                                var draggedGameObject = DragAndDrop.objectReferences[i] as GameObject;
                                draggedTile = TileObject.CreateNewTileFileFromPrefabs(draggedGameObject);
                            }
                            t.tileSet.tiles.Add(draggedTile);
                            if (layerIndex != -1)
                            {
                                t.tileSet.layers[layerIndex].layerObjects.Add(draggedTile);
                            }
                        }
                        t.tileSet.GetDictionaryFromList();
                        DragAndDrop.AcceptDrag();
                        e.Use();
                    }
                }
            }
            DrawPalette(t.tileSet, Event.current);
            if (GUILayout.Button("Select Atlas")) Selection.activeObject = t.tileSet;
            if (GUILayout.Button("Clear Level")) t.ClearLevel();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!t || !t.tileSet || t.tileSet.tiles == null || t.tileSet.tiles.Count == 0 || t.tileSet.TileFromGuid == null || t.tileSet.TileFromGuid.Count == 0) return;
            var activeLayer = layerIndex == -1 ? t.tileSet.defaultLayer : t.tileSet.layers[layerIndex];
            if (painting) sceneView.Repaint(); // <- is this neccessary? 2020.2.0a15 seems to call repaint just every few OnScenGUI
            DrawRaster();
            OnScreenUI(sceneView);
            floorPlane = new Plane(t.transform.up, t.transform.position + Vector3.up * activeLayer.rasterSize.y*targetHeigth);
            var e = Event.current;
            OnSceneGridBrush(e, sceneView, activeLayer);
        }

        int3? rectStartTile;
        void OnSceneGridBrush(Event e, SceneView sceneView, TileAtlas.TagLayer activeLayer)
        {
            if (painting && activeLayer != t.tileSet.defaultLayer)
            {
                if (e.isMouse)
                {
                    Ray guiRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    float d;
                    floorPlane.Raycast(guiRay, out d);
                    Vector3 worldPos = guiRay.GetPoint(d);
                    //Debug.Log("worldPos: " + worldPos+"; mousePos: "+tileMousePosition);
                    var newTileMousePosition = t.WorldPositionToTilePosition(worldPos, activeLayer);
                    if (t.TilePositionInFloorSize(newTileMousePosition, activeLayer))
                        tileMousePosition = newTileMousePosition;
                }
                //Handles.DrawWireDisc(t.TilePositionToLocalPosition(tileMousePosition, selectedTile.size), Vector3.up, 0.5f * selectedTile.size.x);
                Handles.color = activeLayer.color + Color.gray;
                int3 brushSize = selectedTile ? selectedTile.GetSize(activeLayer.rasterSize) : new int3(1, 1, 1);
                DrawBrushGizmo(tileMousePosition, brushSize, activeLayer);
                if (rectStartTile != null && !rectStartTile.Value.Equals(tileMousePosition)) // rectbrush
                {
                    for (int x = 0; x <= Mathf.Abs(Mathf.FloorToInt((tileMousePosition.x - rectStartTile.Value.x) / brushSize.x)); x++)
                        for (int z = 0; z <= Mathf.Abs(Mathf.FloorToInt((tileMousePosition.z - rectStartTile.Value.z) / brushSize.z)); z++)
                            DrawBrushGizmo(rectStartTile.Value 
                                + new int3(
                                    tileMousePosition.x > rectStartTile.Value.x ? x : -x, 
                                    0,
                                    tileMousePosition.z > rectStartTile.Value.z ? z : -z), brushSize, activeLayer);
                }
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.modifiers == EventModifiers.None)
                {
                    if (e.type == EventType.MouseDown)
                        DrawTiles(sceneView, e, tileMousePosition, true);
                    else
                        DrawTiles(sceneView, e, tileMousePosition, false);
                }
                else if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 1 && e.modifiers == EventModifiers.None)
                    EraseTiles(sceneView, e, tileMousePosition);
                else if( e.type == EventType.MouseDown && (e.button == 0 || e.button == 1) && e.modifiers == EventModifiers.Shift)
                {
                    rectStartTile = tileMousePosition;
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                else if(e.type == EventType.MouseDrag && (e.button == 0 || e.button == 1) && rectStartTile != null && e.modifiers == EventModifiers.Shift)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                else if( (e.type == EventType.MouseUp) && (e.button == 0 || e.button == 1) && rectStartTile != null)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                    if (e.button == 0) DrawMultipleTiles(rectStartTile.Value, tileMousePosition, selectedTile, activeLayer);
                    else if (e.button == 1) EraseMultipleTiles(rectStartTile.Value, tileMousePosition);
                    rectStartTile = null;
                }
            }
        }

        void DrawBrushGizmo(int3 brushPosition, int3 brushSize, TileAtlas.TagLayer activeLayer)
        {
            Handles.DrawWireCube(
                t.TilePositionToLocalPosition(brushPosition, brushSize, activeLayer) + Vector3.up * brushSize.y * 0.5f * activeLayer.rasterSize.y, 
                new Vector3(brushSize.x * activeLayer.rasterSize.x, brushSize.y * activeLayer.rasterSize.y, brushSize.z * activeLayer.rasterSize.z));
        }

        void OnScreenUI(SceneView sceneView)
        {
            var activeLayer = layerIndex == -1 ? t.tileSet.defaultLayer : t.tileSet.layers[layerIndex];

            if (!painting) return;
            Handles.BeginGUI();
            var icon_eye = EditorGUIUtility.IconContent("VisibilityOn");

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            //targetHeigth = EditorGUILayout.IntSlider(targetHeigth, 0, t.floorSize.y);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            var floorSize = t.FloorSize(activeLayer);
            targetHeigth = Mathf.RoundToInt(GUILayout.VerticalSlider(targetHeigth, floorSize.y, -floorSize.y, GUILayout.Height(100), GUILayout.Width(12)));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            Color guiColor = GUI.backgroundColor;
            for (int i = 0; i < Mathf.Min(t.layers.Count,t.tileSet.layers.Count); i++)
            {
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = t.tileSet.layers[i].color + (i == layerIndex ? Color.gray : Color.clear);
                if(GUILayout.Button((i+":"+t.tileSet.layers[i].name), "Button", GUILayout.Width(100)))
                {
                    layerIndex = i;
                    Repaint();
                }
                bool vis = !layerVisibility[i];
                vis = GUILayout.Toggle(vis, icon_eye, "Button", GUILayout.Width(30), GUILayout.Height(19));
                if(vis != !layerVisibility[i])
                {
                    layerVisibility[i] = !vis;
                    t.ToggleLayerActive(vis, i);
                }
                GUILayout.Label(t.layers[i].Keys.Count.ToString(), "HelpBox", GUILayout.Height(19));
                GUILayout.EndHorizontal();
            }
            GUI.backgroundColor = guiColor;
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            DrawOnScreenPalette();
            GUILayout.Space(22);

            Handles.EndGUI();
        }

        void DrawRaster()
        {
            var activeLayer = layerIndex == -1 ? t.tileSet.defaultLayer : t.tileSet.layers[layerIndex];
            var rasterSize = activeLayer.rasterSize;
            var floorSize = t.FloorSize(activeLayer);
            if (rasterVisibility == RasterVisibility.Always || (rasterVisibility == RasterVisibility.WhenPainting && painting))
            {
                Handles.matrix = t.transform.localToWorldMatrix;
                Handles.color = rasterColor;
                float zOffset = rasterSize.z * floorSize.z * 0.5f;
                for (int x = -Mathf.FloorToInt(floorSize.x / 2); x < Mathf.CeilToInt(floorSize.x / 2); x++)
                {
                    Handles.DrawLine(new Vector3(x * rasterSize.x, targetHeigth * rasterSize.y, -zOffset), new Vector3(x * rasterSize.x, targetHeigth * rasterSize.y, zOffset));
                }
                float xOffset = rasterSize.x * floorSize.x * 0.5f;
                for (int z = -Mathf.FloorToInt(floorSize.z / 2); z < Mathf.CeilToInt(floorSize.z / 2); z++)
                {
                    Handles.DrawLine(new Vector3(-xOffset, targetHeigth * rasterSize.y, z * rasterSize.z), new Vector3(xOffset, targetHeigth * rasterSize.y, z * rasterSize.z));
                }
                if (painting)
                {
                    Vector3 handleStart = new Vector3(-Mathf.FloorToInt(floorSize.x / 2 * rasterSize.x) , targetHeigth, -Mathf.FloorToInt(floorSize.z / 2 * rasterSize.z));
                    Vector3 handle = Handles.PositionHandle(handleStart, Quaternion.identity);
                    if (!Mathf.Approximately(handle.x,handleStart.x) || !Mathf.Approximately(handle.y,handleStart.y) || !Mathf.Approximately(handle.z,handleStart.z))
                    {
                        t.ChangeFloorBoundaries(new int3(Mathf.FloorToInt(handle.x * -2 / rasterSize.x), floorSize.y, Mathf.FloorToInt(handle.z * -2 / rasterSize.z)), activeLayer);
                        targetHeigth = Mathf.Clamp(Mathf.FloorToInt(handle.y), -floorSize.y, floorSize.y);
                    }
                }
                //Handles.FreeMoveHandle(new Vector3(-Mathf.FloorToInt(t.floorSize.x / 2), targetHeigth, -Mathf.FloorToInt(t.floorSize.z / 2)), Quaternion.identity, 1f, Vector3.one);
            }
        }

        string selectedTileGuid;
        int paletteIndex;
        int layerIndex = -1;
        Vector2 paletteScroll;
        void DrawPalette(TileAtlas atlas, Event e)
        {
            TileAtlas.TagLayer activeLayer = layerIndex == -1 ? atlas.defaultLayer : (layerIndex < atlas.layers.Count) ? atlas.layers[layerIndex] : atlas.defaultLayer;
            //DragTest
            //Rect myRect = GUILayoutUtility.GetRect(100, 40, GUILayout.ExpandWidth(true));
            //GUI.Box(myRect, "Drag and Drop Prefabs to this Box!");
            //if (myRect.Contains(e.mousePosition))
            //{
            //    if (e.type == EventType.DragUpdated)
            //    {
            //        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            //        //Debug.Log("Drag Updated!");
            //        e.Use();
            //    }
            //    else if (e.type == EventType.DragPerform)
            //    {
            //        DragAndDrop.AcceptDrag();
            //        Debug.Log("Drag Perform!");
            //        Debug.Log(DragAndDrop.objectReferences.Length);
            //        if (atlas.layers.Count > 0)
            //            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
            //            {
            //                atlas.layers[0].layerObjects.Add(DragAndDrop.objectReferences[i] as TileObject);
            //            }
            //        e.Use();
            //    }
            //}
            //if (e.type == EventType.DragExited || e.type == EventType.MouseUp)
            //{
            //    //Debug.Log("Drag exited");
            //    DragAndDrop.PrepareStartDrag();
            //}
            //------
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            Color guiColor = GUI.color;
            //EditorGUIUtility.IconContent("Settings") 
            if (GUILayout.Button("Ξ", GUILayout.Width(18), GUILayout.Height(activeLayer == atlas.defaultLayer ? 60 : 30)))
                layerIndex = -1;
            for (int i = 0; i < atlas.layers.Count; i++)
            {
                int index = i;
                GUI.color = atlas.layers[i].color;
                Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(i.ToString()), "Button", GUILayout.Width(18), GUILayout.Height(activeLayer == atlas.layers[i] ? 60 : 30));
                if (GUI.Button(buttonRect, i.ToString()))
                {
                    layerIndex = i;
                    if(e.button == 1)
                    {
                        LayerRightClickMenu(e, index, atlas);
                    }
                        
                }
            }
            GUI.color = guiColor;
            if(GUILayout.Button("+", GUILayout.Width(18), GUILayout.Height(18)))
            {
                atlas.AddTagLayer();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (activeLayer != atlas.defaultLayer)
            {
                bool changedBefore = GUI.changed;
                activeLayer.name = EditorGUILayout.TextField(activeLayer.name);
                if (!changedBefore && GUI.changed) UnityEditor.EditorUtility.SetDirty(atlas);
            }
            else
                EditorGUILayout.HelpBox("Unsorted objects, rightclick and move to a layer to use.", MessageType.Info,true);

            if (layerIndex >= 0 && layerIndex < atlas.layers.Count)
            {
                //activeLayer.rasterSize = EditorGUILayout.Vector3Field("raster ", activeLayer.rasterSize);
                bool changedBefore = GUI.changed;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Rastersize");
                activeLayer.rasterSize.x = EditorGUILayout.DelayedFloatField(activeLayer.rasterSize.x);
                activeLayer.rasterSize.y = EditorGUILayout.DelayedFloatField(activeLayer.rasterSize.y);
                activeLayer.rasterSize.z = EditorGUILayout.DelayedFloatField(activeLayer.rasterSize.z);
                EditorGUILayout.EndHorizontal();
                if (!changedBefore && GUI.changed) t.UpdateTilePositionsOnLayer(activeLayer, layerIndex);
            }

            if (activeLayer!= atlas.defaultLayer && activeLayer.layerObjects.Count == 0) { EditorGUILayout.HelpBox("Drag and Drop Tiles here.", MessageType.Info, true); return; }

            //-----
            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll);
            List<GUIContent> paletteIcons = new List<GUIContent>();
            if (activeLayer == atlas.defaultLayer)
            {
                foreach (var atlasTile in atlas.TileFromGuid.Values)
                {
                    // Get a preview for the prefab
                    if (!atlasTile) { continue; }
                    Texture2D texture = null;
                    if (atlasTile.prefabs.Length != 0) texture = AssetPreview.GetAssetPreview(atlasTile.prefabs[0]);
                    paletteIcons.Add(texture ? new GUIContent(texture) : new GUIContent("No Preview Available"));
                }
                if (paletteIcons.Count == 0) EditorGUILayout.HelpBox("No unsorted tiles in atlas",MessageType.Info);
            }
            else
                foreach (var atlasTile in activeLayer.layerObjects)
                {
                    // Get a preview for the prefab
                    if (!atlasTile) continue;
                    Texture2D texture = null;
                    if (atlasTile.prefabs.Length != 0) texture = AssetPreview.GetAssetPreview(atlasTile.prefabs[0]);
                    paletteIcons.Add(texture ? new GUIContent(texture) : new GUIContent("No Preview Available"));
                }

            if (activeLayer != atlas.defaultLayer) paletteIcons.Add(EditorGUIUtility.IconContent("Toolbar Plus"));
            // Display the grid
            
            //paletteIndex = GUILayout.SelectionGrid(paletteIndex, paletteIcons.ToArray(), 4, GUILayout.Width(position.width-38));
            float columnCount = 4f;
            EditorGUILayout.BeginHorizontal();
            GUIStyle iconLabel = "Label";
            iconLabel.alignment = TextAnchor.UpperCenter;
            iconLabel.normal.textColor = Color.white;
            for(int i=0; i < paletteIcons.Count; i++)
            {
                Rect buttonRect = GUILayoutUtility.GetRect(paletteIcons[i],"Button", GUILayout.Width((position.width - 60) / columnCount));
                TileObject buttonTileObject = activeLayer == atlas.defaultLayer ? atlas.tiles[i] : i < activeLayer.layerObjects.Count ? activeLayer.layerObjects[i] : null;
                bool clickHere = false;
                if (buttonRect.Contains(e.mousePosition))
                {
                    switch (e.type)
                    {
                        //case EventType.MouseDrag:
                        //    DragAndDrop.PrepareStartDrag();

                        //    DragAndDrop.SetGenericData("TileObject", buttonTileObject);
                        //    DragAndDrop.objectReferences = new Object[] { buttonTileObject };
                        //    DragAndDrop.StartDrag("Drag");
                        //    break;
                        //case EventType.DragExited:
                        //    clickHere = true;
                        //    break;
                        case EventType.MouseDown:
                            clickHere = true;
                            break;
                    }
                }
                TileAtlas.TagLayer tileLayer = null;
                foreach (var layer in atlas.layers)
                    if (layer.layerObjects.Contains(buttonTileObject))
                    {
                        tileLayer = layer;
                        break;
                    }

                GUI.backgroundColor = tileLayer!=null?tileLayer.color:guiColor;
                GUI.Toggle(buttonRect, paletteIndex == i, paletteIcons[i], "Button");
                if (buttonTileObject) GUI.Label(buttonRect, buttonTileObject.name, iconLabel);
                if (clickHere)
                {
                    paletteIndex = i;
                    if (activeLayer == atlas.defaultLayer)
                        Selection.activeObject = buttonTileObject;
                    if (activeLayer != atlas.defaultLayer && i == paletteIcons.Count - 1)
                    {
                        AddTileToLayerMenu(atlas,activeLayer,e);
                        paletteIndex = 0;
                    }
                    else if (e.button == 1 ) // rightclick
                    {
                        TileRightClickMenu(e, buttonTileObject, atlas);
                    }

                }
                if (i % (int)(columnCount) == columnCount-1)// && i != 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (activeLayer == atlas.defaultLayer)
                selectedTileGuid = atlas.tiles[paletteIndex].guid;
            else
            {
                if (paletteIndex >= activeLayer.layerObjects.Count) paletteIndex = 0;
                if (activeLayer.layerObjects[paletteIndex])
                    selectedTileGuid = activeLayer.layerObjects[paletteIndex].guid;
                else selectedTileGuid = "";
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = guiColor;
        }

        Vector2 onScreenPaletteScroll;
        void DrawOnScreenPalette()
        {
            bool guiChangedBefore = GUI.changed;
            TileAtlas.TagLayer activeLayer = layerIndex == -1 ? t.tileSet.defaultLayer : (layerIndex < t.tileSet.layers.Count) ? t.tileSet.layers[layerIndex] : t.tileSet.defaultLayer;
            if (activeLayer == t.tileSet.defaultLayer) return;

            onScreenPaletteScroll = EditorGUILayout.BeginScrollView(onScreenPaletteScroll);
            List<GUIContent> paletteIcons = new List<GUIContent>();

            foreach (var atlasTile in activeLayer.layerObjects)
            {
                // Get a preview for the prefab
                if (!atlasTile || atlasTile.prefabs.Length == 0) continue;
                Texture2D texture = AssetPreview.GetAssetPreview(atlasTile.prefabs[0]);
                paletteIcons.Add(new GUIContent(texture));
            }
            paletteIndex = GUILayout.SelectionGrid(paletteIndex, paletteIcons.ToArray(), 1, "Button", GUILayout.Width(40), GUILayout.Height(paletteIcons.Count*40));
            EditorGUILayout.EndScrollView();
            if (!guiChangedBefore && GUI.changed) Repaint();
        }

        void TileRightClickMenu(Event e, TileObject buttonTileObject, TileAtlas atlas)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Select Tile Object"), false, () => 
            {
                Selection.activeObject = buttonTileObject;
            });
            for (int i = 0; i < atlas.layers.Count; i++)
            {
                if (i == layerIndex) continue;
                int weird = i;
                menu.AddItem(new GUIContent("Move to Layer/Layer " + weird+ " : "+atlas.layers[weird].name), false, () =>
                {
                    Debug.Log(weird + ":" + atlas.layers.Count + " obj: " + buttonTileObject.name);
                    atlas.MoveTileToLayer(buttonTileObject, atlas.layers[weird]);
                    t.MoveExistingTilesToLayer(buttonTileObject, layerIndex ,weird);
                });
            }

            menu.AddItem(new GUIContent("Remove/Clear Tiles from Layer"), false, () =>
            {
                if(layerIndex > 0 && layerIndex < atlas.layers.Count)
                    t.RemovePlacedTiles(buttonTileObject, layerIndex);
            });
            menu.AddItem(new GUIContent("Remove/Clear Tiles from Layer and move to Unsorted"), false, () =>
            {
                if (layerIndex > 0 && layerIndex < atlas.layers.Count)
                {
                    t.RemovePlacedTiles(buttonTileObject, layerIndex);
                    atlas.RemoveTileFromLayer(buttonTileObject, atlas.layers[layerIndex]);
                }
            });
            menu.AddItem(new GUIContent("Remove/Clear Tiles and remove from Atlas"), false, () =>
            {
                if (layerIndex > 0 && layerIndex < atlas.layers.Count)
                {
                    t.RemovePlacedTiles(buttonTileObject, layerIndex);
                    atlas.RemoveTileFromLayer(buttonTileObject, atlas.layers[layerIndex]);
                }
                atlas.RemoveFromAtlas(buttonTileObject);
            });
            // ----------------- disabled multi layer support for now and see if a need arises -----------------//
            //for (int i = 0; i < atlas.layers.Count; i++)
            //{
            //    if (i == layerIndex) continue;
            //    int weird = i;
            //    menu.AddItem(new GUIContent("Add to Layer/Layer " + weird +" : "+atlas.layers[weird].name), false, () =>
            //    {
            //        Debug.Log(weird + ":" + atlas.layers.Count + " obj: " + buttonTileObject.name);
            //        atlas.MoveTileToLayer(buttonTileObject, atlas.layers[weird], true);
            //    });
            //}
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Move to and create new Layer"), false, () =>
            {
                atlas.AddTagLayer();
                atlas.MoveTileToLayer(buttonTileObject, atlas.layers[atlas.layers.Count - 1]);
            });
            menu.ShowAsContext();

            e.Use();
        }

        void LayerRightClickMenu(Event e, int index, TileAtlas atlas)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove Layer " + index + ":" + atlas.layers[index].name), false, () =>
            {
                atlas.RemoveLayer(atlas.layers[index]);
                t.RemoveLayer(index);
                paletteIndex = 0;
                layerIndex--;
            });
            menu.AddItem(new GUIContent("Delete all objects at " + index + ":" + atlas.layers[index].name), false, () => 
            {
                t.ClearLayer(index);
            });
            menu.AddItem(new GUIContent("Change Layer Color"), false, () =>
            {
                atlas.layers[index].color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 0.5f, 0.6f, 0.8f);
            });
            menu.ShowAsContext();
            e.Use();
        }

        void AddTileToLayerMenu(TileAtlas atlas, TileAtlas.TagLayer layer, Event e)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent("Tile from other layer [Not Implemented]"));
            menu.AddDisabledItem(new GUIContent("Tile from project files [Not Implemented]"));
            menu.AddDisabledItem(new GUIContent("Generate Tile from Gameobject [Not Implemented]"));
            menu.AddItem(new GUIContent("Drag and Drop files here!"), true, () => { });
            menu.ShowAsContext();
            e.Use();
        }

        public void ChangeTileToLayer(TileAtlas atlas, int i2, TileObject buttonTileObject)
        {
            Debug.Log(i2 + ":" + atlas.layers.Count + " obj: " + buttonTileObject.name);
            atlas.layers[i2].layerObjects.Add(buttonTileObject);
        }

        private void DrawTiles(SceneView view, Event e, int3 tilePosition, bool replaceSame = false)
        {
            
            if (layerIndex == -1) return;
            GUIUtility.hotControl = 0;
            e.Use();
            if (!replaceSame)
                if (t.GetTile(tilePosition, layerIndex)?.guid == selectedTileGuid)
                {
                    return;
                }
            t.PlaceTile(selectedTileGuid, tilePosition, layerIndex, t.tileSet.layers[layerIndex]);
        }

        void DrawMultipleTiles(int3 startPosition, int3 endPosition, TileObject tile, TileAtlas.TagLayer layer)
        {
            int3 brushSize = tile.GetSize(layer.rasterSize);
            int3 minPosition = math.min(startPosition, endPosition);
            int3 maxPosition = math.max(startPosition, endPosition);
            int widthX = Mathf.FloorToInt((maxPosition.x - minPosition.x) / brushSize.x);
            int widthZ = Mathf.FloorToInt((maxPosition.z - minPosition.z) / brushSize.z);
            for (int x = 0; x <= widthX; x++)
                for (int z = 0; z <= widthZ; z++)
                    t.PlaceTile(tile.guid,
                        minPosition + new int3(
                            x*brushSize.x,
                            0,
                            z*brushSize.z),
                        layerIndex, layer, true); // <-- turn to false if performance becomes problematic

            //t.UpdateMultiple(minPosition - new int3(1, 0, 1), maxPosition + new int3(2,0,2), layerIndex);
        }

        void EraseTiles(SceneView view, Event e, int3 tilePosition)
        {
            if (layerIndex == -1) return;
            GUIUtility.hotControl = 0;
            t.RemoveTile(tilePosition, layerIndex);
            e.Use();
        }

        void EraseMultipleTiles(int3 startPosition, int3 endPosition)
        {
            //int3 brushSize = tile.GetSize(layer.rasterSize);
            int3 minPosition = math.min(startPosition, endPosition);
            int3 maxPosition = math.max(startPosition, endPosition);
            int widthX = Mathf.FloorToInt((maxPosition.x - minPosition.x));
            int widthZ = Mathf.FloorToInt((maxPosition.z - minPosition.z));
            for (int x = 0; x <= widthX; x++)
                for (int z = 0; z <= widthZ; z++)
                    t.RemoveTile(minPosition + new int3(x, 0, z), layerIndex);

        }

        bool CheckForRequirements()
        {
            if (!t)
            {
                EditorGUILayout.HelpBox("Please Create LevelBuilder in Scene", MessageType.Warning);
                if (GUILayout.Button("Create LevelBuilder"))
                {
                    var go = new GameObject("Level Builder");
                    _t = go.AddComponent<LevelBuilder>();
                }
                return false;
            }
            if (!t.tileSet)
            {
                EditorGUILayout.HelpBox("Please Apply TileAtlas to LevelBuilder", MessageType.Warning);
                if (GUILayout.Button("Create New Tile Atlas"))
                {
                    var tileAtlas = CreateInstance<TileAtlas>();
                    var path = EditorUtility.SaveFilePanelInProject("New TileAtlas", "My Tile Atlas", "asset", "Choose destination of new asset");
                    AssetDatabase.CreateAsset(tileAtlas, path);
                    AssetDatabase.Refresh();
                    t.tileSet = (TileAtlas)AssetDatabase.LoadAssetAtPath(path, typeof(TileAtlas));
                    EditorGUIUtility.PingObject(t.tileSet);
                }
                TileAtlas chosen = null;
                chosen = (TileAtlas)EditorGUILayout.ObjectField("Use Preexisting ",chosen, typeof(TileAtlas), false);
                if (chosen) t.tileSet = chosen;
                return false;
            }
            if (t.tileSet.tiles == null || t.tileSet.tiles.Count == 0)
            {
                EditorGUILayout.HelpBox("Please add Tiles to TileAtlas", MessageType.Warning);
                if(GUILayout.Button("Ping Atlas"))
                    EditorGUIUtility.PingObject(t.tileSet);
                return false;
            }
            if (t.tileSet.TileFromGuid == null || t.tileSet.TileFromGuid.Keys.Count == 0) { EditorGUILayout.HelpBox("Please click 'Dictionary from List' in TileAtlas", MessageType.Warning); return false; }
            return true;
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