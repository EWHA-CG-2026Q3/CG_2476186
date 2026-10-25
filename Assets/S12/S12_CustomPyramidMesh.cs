using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S12_CustomPyramidMesh : MonoBehaviour
{
    void Start()
    {
        // 정점 5개: 아래 사각형(y=0) 4개 + 위 꼭짓점 1개
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f,   0f, 0f),    // 0
            new Vector3(1f,   0f, 0f),    // 1
            new Vector3(1f,   0f, 1f),    // 2
            new Vector3(0f,   0f, 1f),    // 3
            new Vector3(0.5f, 1.2f, 0.5f) // 4 위 꼭짓점
        };

        // 삼각형 6개 (밖에서 볼 때 시계 방향이 앞면)
        int[] triangles = new int[]
        {
            0, 4, 1,  // 옆면 A
            1, 4, 2,  // 옆면 B
            2, 4, 3,  // 옆면 C
            3, 4, 0,  // 옆면 D

            0, 1, 2,  // 바닥 E
            0, 2, 3   // 바닥 F
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        // 재질은 Inspector의 Mesh Renderer > Materials에서 직접 지정
    }
}
