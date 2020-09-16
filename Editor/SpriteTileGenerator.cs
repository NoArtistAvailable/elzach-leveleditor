using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace elZach.LevelEditor
{
    public static class SpriteTileGenerator
    {
        public static List<TileObject> GenerateFromSpriteSheet(string path, bool isRelativePath)
        {
            string filePath = isRelativePath ? path : ("Assets" + path.Substring(Application.dataPath.Length));

            List<TileObject> export = new List<TileObject>();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            string folderPath = filePath.Substring(0, filePath.LastIndexOf("/") + 1) + tex.name;
            FolderCheck(filePath, tex.name);
            Material mat = MaterialFromTex(tex, folderPath);

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).OfType<Sprite>().ToArray();
            //Mesh mesh = new Mesh();
            //mesh.subMeshCount = sprites.Length;
            //List<Vector3> allVerts = new List<Vector3>();
            //List<Vector2> allUVs = new List<Vector2>();
            //List<ContainerForSubMesh> submeshes = new List<ContainerForSubMesh>();
            for (int index = 0; index < sprites.Length; index++)
            {
                var sprite = sprites[index];
                Debug.Log("Creating " + sprite.name);
                //foreach (var vert in sprite.vertices)
                //    Debug.Log("vert: (" + vert.x.ToString("0.00") + ";" + vert.y.ToString("0.00") + ")");
                //foreach (var uv in sprite.uv)
                //    Debug.Log("uv: (" + uv.x.ToString("0.00") + ";" + uv.y.ToString("0.00") + ")");
                //return;

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
                AssetDatabase.CreateAsset(mesh, folderPath + "/meshes/" + sprite.name + ".asset");
                AssetDatabase.SaveAssets();
                GameObject meshHolder = new GameObject();
                var filter = meshHolder.AddComponent<MeshFilter>();
                filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(folderPath + "/meshes/" + sprite.name + ".asset");
                var rend = meshHolder.AddComponent<MeshRenderer>();
                rend.sharedMaterial = mat;
                var go = PrefabUtility.SaveAsPrefabAsset(meshHolder, folderPath + "/prefabs/" + sprite.name + ".prefab");
                export.Add(TileObject.CreateNewTileFileFromPrefabs(go));
                GameObject.DestroyImmediate(meshHolder);
                //submeshes.Add(new ContainerForSubMesh(verts, uvs, faces));
                //allVerts.AddRange(verts);
                //allUVs.AddRange(uvs);
            }
            //mesh.SetVertices(allVerts);
            //mesh.SetUVs(0, allUVs);
            //for (int i = 0; i < submeshes.Count; i++)
            //{
            //    mesh.SetTriangles(submeshes[i].faces, i);
            //    //submeshes[i].DrawDebugMesh(3f);
            //}
            //string savePath = filePath.Substring(0, filePath.LastIndexOf("/") + 1) + tex.name + "-mesh.asset";
            //AssetDatabase.CreateAsset(mesh, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return export;
        }

        public static void FolderCheck(string filePath, string texName)
        {
            string folderPath = filePath.Substring(0, filePath.LastIndexOf("/") + 1) + texName;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(filePath.Substring(0, filePath.LastIndexOf("/")), texName);
                AssetDatabase.Refresh();
            }
            if (!AssetDatabase.IsValidFolder(folderPath + "/meshes"))
            {
                AssetDatabase.CreateFolder(folderPath, "meshes");
                AssetDatabase.Refresh();
            }
            if (!AssetDatabase.IsValidFolder(folderPath + "/prefabs"))
            {
                AssetDatabase.CreateFolder(folderPath, "prefabs");
                AssetDatabase.Refresh();
            }
        }

        public static Material MaterialFromTex(Texture2D tex, string folderPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(folderPath + "/" + tex.name + "_material.mat");
            if (mat) return mat;
            mat = new Material(Shader.Find("Diffuse"));
            mat.mainTexture = tex;
            AssetDatabase.CreateAsset(mat, folderPath + "/" + tex.name + "_material.mat");
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Material>(folderPath + "/" + tex.name + "_material.mat");
        }

        public static GameObject PrefabFromSprite(Sprite sprite)
        {
            Debug.Log("Creating " + sprite.name);
            Texture2D tex = sprite.texture;
            string filePath = AssetDatabase.GetAssetPath(tex);
            string folderPath = filePath.Substring(0, filePath.LastIndexOf("/") + 1) + tex.name;
            FolderCheck(filePath, tex.name);
            var mat = MaterialFromTex(tex, folderPath);

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
            AssetDatabase.CreateAsset(mesh, folderPath + "/meshes/" + sprite.name + ".asset");
            AssetDatabase.SaveAssets();
            GameObject meshHolder = new GameObject();
            var filter = meshHolder.AddComponent<MeshFilter>();
            filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(folderPath + "/meshes/" + sprite.name + ".asset");
            var rend = meshHolder.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            var go = PrefabUtility.SaveAsPrefabAsset(meshHolder, folderPath + "/prefabs/" + sprite.name + ".prefab");
            GameObject.DestroyImmediate(meshHolder);

            AssetDatabase.SaveAssets();
            return go;
        }

        public static TileObject TileFromSprite(Sprite sprite)
        {
            var go = PrefabFromSprite(sprite);
            return TileObject.CreateNewTileFileFromPrefabs(go);
        }

        public class ContainerForSubMesh
        {
            public Vector3[] verts;
            public Vector2[] uvs;
            public int[] faces;

            public ContainerForSubMesh(Vector3[] verts, Vector2[] uvs, int[] faces)
            {
                this.verts = verts;
                this.uvs = uvs;
                this.faces = faces;
            }

            public Mesh GetMesh()
            {
                Mesh mesh = new Mesh();
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(faces, 0);
                return mesh;
            }

            public void DrawDebugMesh(float time)
            {
                Debug.Log(verts.Length);
                for (int i = 0; i < faces.Length - 2; i += 3)
                {
                    Debug.Log("drawing from " + faces[i] + " to " + faces[i + 1] + " to " + faces[i + 2]);
                    Debug.DrawLine(verts[faces[i]], verts[faces[i + 1]], Color.white, time);
                    Debug.DrawLine(verts[faces[i + 1]], verts[faces[i + 2]], Color.white, time);
                    Debug.DrawLine(verts[faces[i + 2]], verts[faces[i]], Color.white, time);
                }
            }
        }

        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Create Tile From Sprite",true)]
        static bool ValidateCreateTileFromSprite()
        {
            if (UnityEditor.Selection.activeObject as Sprite)
                return true;
            else
                return false;
        }

        [UnityEditor.MenuItem("Assets/Create/LevelEditor/Tiles/Create Tile From Sprite")]
        static List<TileObject> CreateTileFromSprite()
        {
            List<TileObject> newTiles = new List<TileObject>();
            foreach (var selectedObject in UnityEditor.Selection.objects)
            {
                var sprite = selectedObject as Sprite;
                newTiles.Add(TileFromSprite(sprite));
            }
            return newTiles;
        }
    }
}
