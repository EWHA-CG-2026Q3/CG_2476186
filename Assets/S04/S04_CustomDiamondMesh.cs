using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_CustomDiamondMesh : MonoBehaviour
{
    void Start()
    {
        // 정점 6개: S3 정육면체 정점 중 0, 1, 5, 4번(y=0인 아래 사각형) + 위/아래 꼭짓점 추가
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f,   0f, 0f),   // 0 (정육면체 0번)
            new Vector3(1f,   0f, 0f),   // 1 (정육면체 1번)
            new Vector3(1f,   0f, 1f),   // 2 (정육면체 5번)
            new Vector3(0f,   0f, 1f),   // 3 (정육면체 4번)
            new Vector3(0.5f,  1f, 0.5f), // 4 위 꼭짓점 (추가)
            new Vector3(0.5f, -1f, 0.5f), // 5 아래 꼭짓점 (추가)
        };

        // 삼각형 8개 (밖에서 볼 때 시계 방향이 앞면)
        int[] triangles = new int[]
        {
            0, 4, 1,  // 위쪽 면 A
            1, 4, 2,  // 위쪽 면 B
            2, 4, 3,  // 위쪽 면 C
            3, 4, 0,  // 위쪽 면 D

            0, 1, 5,  // 아래쪽 면 E
            1, 2, 5,  // 아래쪽 면 F
            2, 3, 5,  // 아래쪽 면 G
            3, 0, 5,  // 아래쪽 면 H
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        // 재질은 Inspector의 Mesh Renderer > Materials에서 직접 지정한 걸 사용
    }
}
