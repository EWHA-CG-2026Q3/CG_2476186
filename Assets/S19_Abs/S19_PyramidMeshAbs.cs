// S19_PyramidMeshAbs.cs
// S19 출석인정과제 / 오브젝트: 금색 피라미드
// S19_DiamondMesh_Faceted를 복제해 사각뿔(밑면 1개, 옆면 4개)로 바꾼 버전.
// 삼각형마다 정점을 따로 두어(5 → 18) normal과 UV 좌표를 면마다 갖게 함.
// 이 스크립트는 메시를 만들고 보관하는 역할만 함 — 이동/회전/스케일 계산은 별도 스크립트가 담당.
// 머티리얼이 비어 있거나 분홍색(셰이더 오류)이면 URP Lit 머티리얼을 자동으로 붙임.
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S19_PyramidMeshAbs : MonoBehaviour
{
    [Header("사각뿔 원본 정점 (0~3=밑면 사각형, 4=위 꼭짓점)")]
    [SerializeField]
    Vector3[] baseVertices = new Vector3[]
    {
        new Vector3(0f,   0f, 0f),     // 0 — 밑면 사각형
        new Vector3(1f,   0f, 0f),     // 1
        new Vector3(1f,   0f, 1f),     // 2
        new Vector3(0f,   0f, 1f),     // 3
        new Vector3(0.5f, 1f, 0.5f),   // 4 — 위 꼭짓점
    };

    // 면마다 정점 번호 3개
    // 바깥에서 봤을 때 시계 방향 = 앞면 (Unity 규칙)
    static readonly int[] triangles = new int[]
    {
        0,1,2, 0,2,3,                 // 밑면 2개 (아래에서 봄)
        4,1,0, 4,2,1, 4,3,2, 4,0,3,   // 옆면 4개
    };

    Mesh mesh;

    public Vector3[] BaseVertices => baseVertices;

    void OnEnable()
    {
        EnsureLitMaterial();

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        SetVertices(baseVertices);
    }

    public void SetVertices(Vector3[] v)
    {
        // 정점: 삼각형 배열의 번호를 그 정점으로 바꿔 차례로 적음 (면마다 3개, 모두 18개)
        //       0,1,2 → v[0], v[1], v[2]
        Vector3[] facetedVertices =
        {
            v[0], v[1], v[2],   v[0], v[2], v[3],                         // 밑면 2개
            v[4], v[1], v[0],   v[4], v[2], v[1],   v[4], v[3], v[2],   v[4], v[0], v[3],   // 옆면 4개
        };

        // 삼각형: 정점을 면마다 따로 적었으므로, 적은 순서대로
        int[] facetedTriangles =
        {
             0,  1,  2,    3,  4,  5,                                // 밑면 2개
             6,  7,  8,    9, 10, 11,   12, 13, 14,   15, 16, 17,   // 옆면 4개
        };

        // UV 좌표: 면 하나씩 텍스처에 대응시킴
        Vector2 uvTopCenter = new Vector2(0.5f, 1);   // 텍스처의 위 가운데를 가리키는 UV 좌표
        Vector2 uvTopLeft = new Vector2(0, 1);      // 텍스처의 왼쪽 위를 가리키는 UV 좌표
        Vector2 uvTopRight = new Vector2(1, 1);      // 텍스처의 오른쪽 위를 가리키는 UV 좌표
        Vector2 uvBottomLeft = new Vector2(0, 0);      // 텍스처의 왼쪽 아래를 가리키는 UV 좌표
        Vector2 uvBottomRight = new Vector2(1, 0);      // 텍스처의 오른쪽 아래를 가리키는 UV 좌표

        // facetedVertices와 같은 순서로 하나씩 짝지음
        Vector2[] uv =
        {
            // 밑면 2개: 사각형 하나에 그림 한 장 (x, z → u, v)
            uvBottomLeft, uvBottomRight, uvTopRight,
            uvBottomLeft, uvTopRight, uvTopLeft,

            // 옆면 4개: (위 꼭짓점, 오른쪽, 왼쪽) → (위 가운데, 오른쪽 아래, 왼쪽 아래)
            uvTopCenter, uvBottomRight, uvBottomLeft,
            uvTopCenter, uvBottomRight, uvBottomLeft,
            uvTopCenter, uvBottomRight, uvBottomLeft,
            uvTopCenter, uvBottomRight, uvBottomLeft,
        };

        mesh.Clear();
        mesh.vertices = facetedVertices;     // 18개
        mesh.triangles = facetedTriangles;   // 적은 순서대로
        mesh.uv = uv;                        // 정점과 같은 18개
        mesh.RecalculateNormals();           // 정점마다 한 면에만 속하므로 면의 normal 그대로
        mesh.RecalculateBounds();
    }

    // 머티리얼이 없거나 셰이더가 깨져 있으면(분홍색) Lit 머티리얼을 새로 만들어 붙임
    void EnsureLitMaterial()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Material current = meshRenderer.sharedMaterial;

        bool needsMaterial = current == null
                          || current.shader == null
                          || !current.shader.isSupported
                          || current.shader.name == "Hidden/InternalErrorShader";
        if (!needsMaterial) return;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");   // URP가 아닌 프로젝트용 대비
        if (lit == null) return;

        meshRenderer.sharedMaterial = new Material(lit) { name = "Pyramid_Lit (자동 생성)" };
    }
}
