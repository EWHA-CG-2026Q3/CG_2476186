using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh_Kite : MonoBehaviour
{
    void Start()
    {
        Vector3[] vertices = new Vector3[]
        {
            new Vector3( 0f,  1.5f, 0f), // 0 위
            new Vector3( 1f,  0f,   0f), // 1 오른쪽
            new Vector3( 0f, -1f,   0f), // 2 아래
            new Vector3(-1f,  0f,   0f), // 3 왼쪽
        };

        // 0-2 대각선으로 삼각형 2개
        int[] triangles = new int[6] { 0, 1, 2, 0, 2, 3 };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        // GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}
