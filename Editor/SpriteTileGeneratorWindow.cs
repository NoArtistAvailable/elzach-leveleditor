using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using elZach.Common;
using UnityEngine.Rendering;

namespace elZach.LevelEditor
{
    public class SpriteTileGeneratorWindow : EditorWindow
    {
        public enum TileType { Floor, ThinWall, Block }
        TileType tileType;

        string lastFolder = "Assets";
        Sprite targetSprite;
        Sprite additionalSprite_bottom, additionalSprite_top;
        string tileName;
        GameObject previewGO;
        Material previewMat;

        Editor gameObjectEditor;

        [MenuItem("Window/LevelBuilder/SpriteTile Generator")]
        public static void Init()
        {
            SpriteTileGeneratorWindow window = (SpriteTileGeneratorWindow)EditorWindow.GetWindow(typeof(SpriteTileGeneratorWindow));
            window.titleContent = new GUIContent("3D Sprite Tile");
            window.minSize = new Vector2(100, 100);
            window.Show();
        }

        private void OnDestroy()
        {
            if (gameObjectEditor) DestroyImmediate(gameObjectEditor);
            if (previewGO) DestroyImmediate(previewGO);
        }

        private void OnGUI()
        {
            var selectedTileType = (TileType)EditorGUILayout.EnumPopup("Tile Type", tileType);
            EditorGUILayout.BeginHorizontal();
            if (targetSprite)
            {
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(125), GUILayout.MaxHeight(125));
                DrawSpritePreview(rect, targetSprite);
            }
            if (tileType == TileType.Block) EditorGUILayout.BeginVertical();
            Sprite selectedSprite = EditorGUILayout.ObjectField(targetSprite, typeof(Sprite), true) as Sprite;

            Sprite selectedTop = null;
            if (tileType == TileType.Block) {
                selectedTop = EditorGUILayout.ObjectField(additionalSprite_top, typeof(Sprite), true) as Sprite;
                EditorGUILayout.EndVertical();
            }

            if (selectedSprite != targetSprite || selectedTileType != tileType || selectedTop != additionalSprite_top)
            {
                targetSprite = selectedSprite;
                tileType = selectedTileType;
                additionalSprite_top = selectedTop;
                if (previewGO)
                {
                    DestroyImmediate(previewGO);
                    DestroyImmediate(gameObjectEditor);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (targetSprite && !previewGO) CreatePreviewObject(targetSprite, tileType);

            if (targetSprite && tileName == null)
                tileName = targetSprite.name;
            if (tileName != null) tileName = EditorGUILayout.TextField("Tile Name", tileName);

            if (GUILayout.Button("Save as Tile"))
            {
                string targetFolder = EditorUtility.OpenFolderPanel("Save Floortile", lastFolder, "");
                targetFolder = SpriteTileGenerator.EnsureAssetDataPath(targetFolder);
                lastFolder = targetFolder;
                //SpriteTileGenerator.PrefabFromSprite(floorSprite, targetFolder);
                var mat = SpriteTileGenerator.MaterialFromTex(targetSprite.texture, targetFolder);
                Mesh mesh;
                switch (tileType) {
                    case TileType.Block:
                        mesh = CreateBlockMesh(targetSprite, additionalSprite_top);
                        break;
                    case TileType.ThinWall:
                        mesh = SpriteTileGenerator.CreateMeshFromSprite(targetSprite);
                        TransformMesh(mesh, Quaternion.Euler(270f, 90f, 90f), Vector3.up * 0.5f);
                        break;
                    default:
                        mesh = SpriteTileGenerator.CreateMeshFromSprite(targetSprite);
                        break;
                }
                SaveGameobject(mesh, mat, targetFolder, tileName);
            }

            if (previewGO)
            {
                GUIStyle bgColor = new GUIStyle();
                if (gameObjectEditor == null)
                    gameObjectEditor = Editor.CreateEditor(previewGO);
                gameObjectEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(200, 200), bgColor);
            }
        }

        void CreatePreviewObject(Sprite sprite, TileType type)
        {
            if (previewGO) DestroyImmediate(previewGO);
            previewMat = new Material(GraphicsSettings.renderPipelineAsset.defaultMaterial.shader);
            previewMat.mainTexture = sprite.texture;
            previewMat.EnableKeyword("_ALPHATEST_ON");
            Mesh previewMesh;
            switch (type)
            {
                case TileType.Floor:
                    previewMesh = SpriteTileGenerator.CreateMeshFromSprite(sprite);
                    break;
                case TileType.ThinWall:
                    previewMesh = SpriteTileGenerator.CreateMeshFromSprite(sprite);
                    TransformMesh(previewMesh, Quaternion.Euler(270f, 90f, 90f), Vector3.up * 0.5f);
                    break;
                case TileType.Block:
                    previewMesh = CreateBlockMesh(sprite, additionalSprite_top);
                    break;
                default:
                    previewMesh = SpriteTileGenerator.CreateMeshFromSprite(sprite);
                    break;
            }
            previewGO = new GameObject(tileName, typeof(MeshFilter), typeof(MeshRenderer));
            previewGO.GetComponent<MeshFilter>().mesh = previewMesh;
            previewGO.GetComponent<MeshRenderer>().material = previewMat;
            previewGO.hideFlags = HideFlags.HideAndDontSave;
            //return previewGO;
        }

        static Mesh CreateBlockMesh(Sprite sprite, Sprite topSprite)
        {
            Mesh[] partials = new Mesh[5];
            for (int i = 0; i <= 3; i++)
            {
                partials[i] = SpriteTileGenerator.CreateMeshFromSprite(sprite);
                TransformMesh(partials[i],
                    Quaternion.Euler(270f, 90f + 90f * i, 90f),
                    Vector3.up * 0.5f
                        + Quaternion.Euler(0f, 90f * i, 0f).normalized * Vector3.forward * 0.5f);
            }
            if (topSprite)
            {
                partials[4] = SpriteTileGenerator.CreateMeshFromSprite(topSprite);
                TransformMesh(partials[4],
                        Quaternion.Euler(0f, 0f, 0f),
                        Vector3.up * 1f);
            }
            return MeshCombine.CombineMeshes(partials);
        }

        static void TransformMesh(Mesh mesh, Quaternion rot, Vector3 translation)
        {
            var verts = new Vector3[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                verts[i] = rot * mesh.vertices[i] + translation;
            }
            mesh.SetVertices(verts);
        }

        static GameObject SaveGameobject(Mesh mesh, Material mat, string folderPath, string objectName)
        {
            FolderCheck(folderPath);
            FolderCheck(folderPath + "/meshes");
            FolderCheck(folderPath + "/prefabs");
            AssetDatabase.CreateAsset(mesh, folderPath + "/meshes/" + objectName + ".asset");
            AssetDatabase.SaveAssets();
            GameObject meshHolder = new GameObject();
            var filter = meshHolder.AddComponent<MeshFilter>();
            filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(folderPath + "/meshes/" + objectName + ".asset");
            var rend = meshHolder.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            var go = PrefabUtility.SaveAsPrefabAsset(meshHolder, folderPath + "/prefabs/" + objectName + ".prefab");
            GameObject.DestroyImmediate(meshHolder);

            AssetDatabase.SaveAssets();
            Debug.Log("Creating " + objectName, go);
            return go;
        }

        public static void FolderCheck(string folderPath)
        {
            //System.IO.Directory.CreateDirectory
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.Log(folderPath.Substring(0, folderPath.LastIndexOf("/")));
                string parentPath = folderPath.Substring(0, folderPath.LastIndexOf("/"));
                string folderName = folderPath.Substring(folderPath.LastIndexOf("/")+1);
                AssetDatabase.CreateFolder(folderPath.Substring(0, folderPath.LastIndexOf("/")), folderName);
                AssetDatabase.Refresh();
            }
        }

        private static void DrawSpritePreview(Rect position, Sprite sprite)
        {
            Vector2 fullSize = new Vector2(sprite.texture.width, sprite.texture.height);
            Vector2 size = new Vector2(sprite.textureRect.width, sprite.textureRect.height);

            Rect coords = sprite.textureRect;
            coords.x /= fullSize.x;
            coords.width /= fullSize.x;
            coords.y /= fullSize.y;
            coords.height /= fullSize.y;

            Vector2 ratio;
            ratio.x = position.width / size.x;
            ratio.y = position.height / size.y;
            float minRatio = Mathf.Min(ratio.x, ratio.y);

            Vector2 center = position.center;
            position.width = size.x * minRatio;
            position.height = size.y * minRatio;
            position.center = center;

            GUI.DrawTextureWithTexCoords(position, sprite.texture, coords);
        }
    }
}
