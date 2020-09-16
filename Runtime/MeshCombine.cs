using elZach.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCombine : MonoBehaviour
{
    public float mergeDistance = 0.01f;

#if UNITY_EDITOR
    [Button("Combine Child Meshes")]
    public void CombineChildMeshesOnObject()
    {
        Transform parent = transform;
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>(); // only uses uv 0 right now
        List<int> tris = new List<int>(); // doesnt take submeshes into account yet
        int vertOffset = 0;
        for (int i =0; i < filters.Length; i++)
        {
            Transform childTrans = filters[i].transform;
            Mesh childMesh = Application.isPlaying ? filters[i].mesh : filters[i].sharedMesh;

            for(int v = 0; v < childMesh.vertices.Length; v++)
            {
                verts.Add(parent.InverseTransformPoint // move from worldspace into parent space
                        (childTrans.TransformPoint(childMesh.vertices[v]))); // move into world space
                uvs.Add(childMesh.uv[v]);
            }
            for(int t = 0; t < childMesh.triangles.Length; t++)
            {
                tris.Add(childMesh.triangles[t] + vertOffset);
            }
            vertOffset += childMesh.vertices.Length;
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        UnityEditor.MeshUtility.Optimize(mesh);
        
        UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/" + gameObject.name + ".asset");
        UnityEditor.AssetDatabase.SaveAssets();
    }

    [Button("Get Mesh Details")]
    public void GetMeshDetailsOnObject()
    {
        var filter = GetComponent<MeshFilter>();
        if (!filter) filter = GetComponentInChildren<MeshFilter>();
    }

    public static void CombineMesh()
    {

    }
#endif
}
