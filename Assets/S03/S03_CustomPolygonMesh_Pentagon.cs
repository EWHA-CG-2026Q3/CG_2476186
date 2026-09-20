using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh_Pentagon : MonoBehaviour
{
    void Start()
    {
        Vector3[] vertices = new Vector3[]
        {
            new Vector3( 0f,    1f, 0f), // 0 위
            new Vector3( 2.5f,  0f, 0f), // 1 오른쪽
            new Vector3( 1.5f, -1f, 0f), // 2 오른쪽 아래
            new Vector3(-1.5f, -1f, 0f), // 3 왼쪽 아래
            new Vector3(-2.5f,  0f, 0f), // 4 왼쪽
        };

        // 0번 정점을 기준으로 삼각형 3개 (부채꼴 방식)
        int[] triangles = new int[9]
        {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        // GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}
