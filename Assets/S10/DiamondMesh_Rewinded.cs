using UnityEngine;

// Diamond_Mesh에 붙임. S10_DiamondChain_Finish가 BaseVertices를 읽고 SetVertices로 결과를 넣음.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DiamondMesh_Rewinded : MonoBehaviour
{
    // 로컬 좌표의 원래 꼭짓점 (기준점 = 다이아몬드를 감싸는 상자의 한쪽 모서리)
    Vector3[] baseVertices = new Vector3[]
    {
        new Vector3(0.5f, 0f,   0.5f), // 0 아래 꼭짓점
        new Vector3(0f,   0.5f, 0f),   // 1
        new Vector3(1f,   0.5f, 0f),   // 2
        new Vector3(1f,   0.5f, 1f),   // 3
        new Vector3(0f,   0.5f, 1f),   // 4
        new Vector3(0.5f, 1f,   0.5f), // 5 위 꼭짓점
    };

    Mesh mesh;

    public Vector3[] BaseVertices => baseVertices;

    void Awake()
    {
        int[] triangles = new int[]
        {
            // 위쪽 면 4개
            1, 5, 2,
            2, 5, 3,
            3, 5, 4,
            4, 5, 1,
            // 아래쪽 면 4개
            1, 2, 0,
            2, 3, 0,
            3, 4, 0,
            4, 1, 0,
        };

        mesh = new Mesh();
        mesh.vertices = baseVertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        // 재질은 Inspector의 Mesh Renderer > Materials에서 지정
    }

    // 변환이 끝난 월드 좌표 꼭짓점을 메시에 넣음
    public void SetVertices(Vector3[] verts)
    {
        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
