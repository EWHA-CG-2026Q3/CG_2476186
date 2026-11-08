// Mid_RingMesh.cs
// 중간 프로젝트 / 필수 ① 기하 모델링 (반복문 메시)
// 혼천의의 고리 하나 = 토러스(도넛). 정점을 이중 반복문으로 만듦.
//   바깥 반복 i: 큰 원 위에 점 segments개 (각도 θ = 360° / segments * i)
//   안쪽 반복 j: 그 점을 중심으로 고리의 단면 원 위에 점 sides개 (각도 φ = 360° / sides * j)
// 수업의 원판 힌트(가운데 점 + 원 위의 점 n개)를 한 번 더 감아 만든, 수업에 없던 형태.
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_RingMesh : MonoBehaviour
{
    [Header("큰 원 (고리 전체의 반지름과 나눈 수)")]
    [Min(0.01f)] public float radius = 0.5f;
    [Range(3, 128)] public int segments = 64;

    [Header("단면 (고리 굵기와 나눈 수)")]
    [Min(0.001f)] public float thickness = 0.02f;
    [Range(3, 32)] public int sides = 8;

    Mesh mesh;

    void OnEnable() => Build();
    void OnValidate() { if (mesh != null) Build(); }

    public void Build()
    {
        if (mesh == null)
        {
            mesh = new Mesh { name = "Mid_Ring" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        // 텍스처 이음매 때문에 처음 점을 마지막에 한 번 더 둠: (segments+1) x (sides+1)
        int rowLength = sides + 1;
        Vector3[] vertices = new Vector3[(segments + 1) * rowLength];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i <= segments; i++)
        {
            float theta = 2f * Mathf.PI / segments * i;
            Vector3 outward = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)); // 큰 원의 바깥 방향
            Vector3 center = outward * radius;                                     // 큰 원 위의 점

            for (int j = 0; j <= sides; j++)
            {
                float phi = 2f * Mathf.PI / sides * j;
                // 단면 원 위의 방향: 바깥쪽(outward)과 위쪽(up)을 cos, sin으로 섞음
                Vector3 n = outward * Mathf.Cos(phi) + Vector3.up * Mathf.Sin(phi);

                int k = i * rowLength + j;
                vertices[k] = center + n * thickness;
                normals[k] = n;                                  // 단면 원의 바깥 방향이 곧 normal
                uv[k] = new Vector2((float)i / segments, (float)j / sides);
            }
        }

        // 이웃한 네 점 a(i,j) b(i+1,j) c(i,j+1) d(i+1,j+1)로 사각형 = 삼각형 2개
        // 바깥에서 볼 때 시계 방향이 앞면 (Unity 규칙)
        int[] triangles = new int[segments * sides * 6];
        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            for (int j = 0; j < sides; j++)
            {
                int a = i * rowLength + j;
                int b = a + rowLength;
                int c = a + 1;
                int d = b + 1;

                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}
