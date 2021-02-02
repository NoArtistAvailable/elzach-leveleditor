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
        public enum TileType { Floor, ThinWall, Block, Slope }
        TileType tileType;

        string lastFolder = "Assets";
        Sprite targetSprite;
        Sprite additionalSprite_bottom, additionalSprite_top, additionalSprite_side;
        float height = 1f;
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
            var prevTileType = tileType;
            tileType = (TileType)EditorGUILayout.EnumPopup("Tile Type", tileType);
            EditorGUILayout.BeginHorizontal();
            if (targetSprite)
            {
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(125), GUILayout.MaxHeight(125));
                DrawSpritePreview(rect, targetSprite);
            }
            EditorGUILayout.BeginVertical();
            Sprite prevSprite = targetSprite;
            targetSprite = EditorGUILayout.ObjectField(targetSprite, typeof(Sprite), true) as Sprite;

            Sprite prevTop = additionalSprite_top;
            Sprite prevSide = additionalSprite_side;
            float prevHeight = height;
            switch (tileType)
            {
                case TileType.Block:
                    additionalSprite_top = EditorGUILayout.ObjectField(additionalSprite_top, typeof(Sprite), true) as Sprite;
                    break;
                case TileType.Slope:
                    additionalSprite_side = EditorGUILayout.ObjectField(additionalSprite_side, typeof(Sprite), true) as Sprite;
                    height = EditorGUILayout.DelayedFloatField("Height: ", height);
                    break;
            }
            EditorGUILayout.EndVertical();

            if (prevSprite != targetSprite || prevTileType != tileType || prevTop != additionalSprite_top || prevSide != additionalSprite_side || prevHeight != height)
            {
                prevSprite = targetSprite;
                prevTileType = tileType;
                prevTop = additionalSprite_top;
                prevSide = additionalSprite_side;
                prevHeight = height;
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

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Prefab"))
            {
                SaveCurrentGameObject();
            }
            if (GUILayout.Button("Save As Tile"))
            {
                var go = SaveCurrentGameObject();
                SaveAsTile(go);
            }
            EditorGUILayout.EndHorizontal();

            if (previewGO)
            {
                GUIStyle bgColor = new GUIStyle();
                if (gameObjectEditor == null)
                {
                    gameObjectEditor = Editor.CreateEditor(previewGO); // set camera?
                }
                gameObjectEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(200, 200), bgColor);
            }
        }

        GameObject SaveCurrentGameObject()
        {
            string targetFolder = EditorUtility.OpenFolderPanel("Save Floortile", lastFolder, "");
            targetFolder = SpriteTileGenerator.EnsureAssetDataPath(targetFolder);
            lastFolder = targetFolder;
            //SpriteTileGenerator.PrefabFromSprite(floorSprite, targetFolder);
            var mat = SpriteTileGenerator.MaterialFromTex(targetSprite.texture, targetFolder);
            Mesh mesh;
            switch (tileType)
            {
                case TileType.Block:
                    mesh = CreateBlockMesh(targetSprite, additionalSprite_top);
                    break;
                case TileType.ThinWall:
                    mesh = SpriteTileGenerator.CreateMeshFromSprite(targetSprite);
                    TransformMesh(mesh, Quaternion.Euler(270f, 90f, 90f), Vector3.up * 0.5f);
                    break;
                case TileType.Slope:
                    mesh = CreateSlopeMesh(targetSprite, additionalSprite_side, height);
                    break;
                default:
                    mesh = SpriteTileGenerator.CreateMeshFromSprite(targetSprite);
                    break;
            }
            return SaveGameobject(mesh, mat, targetFolder, tileName);
        }

        void SaveAsTile(GameObject go)
        {
            var builder = FindObjectOfType<LevelBuilder>();
            if (!builder) { Debug.LogWarning("Create a level builder in scene."); return; }
            if (!builder.tileSet) { Debug.LogWarning("Add a tileatlas to builder."); return; }
            var newTile = TileObject.CreateNewTileFileFromPrefabs(go);
            builder.tileSet.tiles.Add(newTile);
            builder.tileSet.GetDictionaryFromList();
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
                case TileType.Slope:
                    previewMesh = CreateSlopeMesh(sprite, additionalSprite_side, height);
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

        static Mesh CreateSlopeMesh(Sprite sprite, Sprite sideSprite, float height)
        {
            //Mesh[] partials = new Mesh[4]; // two quads, two tris
            Mesh main = SpriteTileGenerator.CreateMeshFromSprite(sprite);
            Vector3[] verts = main.vertices;
            for (int i = 0; i <= 1; i++)
                verts[i] += Vector3.up * height;
            main.SetVertices(verts);
            main.RecalculateNormals();
            main.RecalculateTangents();
            return main;
            //return CreatePartialQuad(sprite, height);
        }

        public static Mesh CreatePartialQuad(Sprite sprite, float height)
        {
            Vector3[] verts = new Vector3[sprite.vertices.Length];
            Vector2[] uvs = new Vector2[sprite.uv.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = new Vector3(sprite.vertices[i].x, 0f, sprite.vertices[i].y);
                uvs[i] = sprite.uv[i];
            }
            int[] faces = new int[sprite.triangles.Length];
            for (int i = 0; i < faces.Length; i++)
            {
                //Debug.Log(index + " : face " + sprite.triangles[i]);
                faces[i] = sprite.triangles[i];// + vertOffset;
            }
            Mesh mesh = new Mesh();
            mesh.name = sprite.name;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(faces, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
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
            AssetDatabase.Refresh();
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
