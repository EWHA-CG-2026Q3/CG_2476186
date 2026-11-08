// Mid_PedestalMesh.cs
// 중간 프로젝트 / 필수 ① 기하 모델링 (반복문 메시 2) — 혼천의 받침대
// 단면 모양(반지름, 높이) 점 몇 개를 직접 정하고, 그 점들을 y축으로 한 바퀴 돌려 n각기둥을 쌓음.
//   직접 정한 정점: profile (받침의 옆모습)
//   반복문으로 만든 정점: 옆모습의 각 점을 원 위에 segments개씩 (각도 = 360° / segments * i)
//   위·아래 뚜껑: 가운데 점 + 원 위의 이웃한 두 점 (수업 힌트의 원판)
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_PedestalMesh : MonoBehaviour
{
    [Header("옆모습: x = 반지름, y = 높이 (아래에서 위 순서)")]
    public Vector2[] profile =
    {
        new Vector2(0.30f, 0.00f),   // 바닥 받침
        new Vector2(0.30f, 0.05f),
        new Vector2(0.20f, 0.09f),
        new Vector2(0.08f, 0.14f),   // 기둥 시작
        new Vector2(0.06f, 0.70f),   // 기둥 끝
        new Vector2(0.12f, 0.76f),   // 위 받침
        new Vector2(0.12f, 0.80f),
    };

    [Range(3, 64)] public int segments = 24;   // 작게 하면 n각기둥, 크게 하면 원기둥에 가까워짐

    Mesh mesh;

    void OnEnable() => Build();
    void OnValidate() { if (mesh != null) Build(); }

    public void Build()
    {
        if (mesh == null)
        {
            mesh = new Mesh { name = "Mid_Pedestal" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
        if (profile == null || profile.Length < 2) return;

        var vertices = new System.Collections.Generic.List<Vector3>();
        var normals = new System.Collections.Generic.List<Vector3>();
        var uv = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();

        // 옆면: 옆모습의 이웃한 두 점(k, k+1) 사이를 띠 하나로. 띠마다 정점을 따로 두어 꺾인 곳이 또렷하게 보임
        for (int k = 0; k < profile.Length - 1; k++)
        {
            Vector2 p0 = profile[k], p1 = profile[k + 1];
            float dr = p1.x - p0.x, dy = p1.y - p0.y;
            int start = vertices.Count;

            for (int i = 0; i <= segments; i++)
            {
                float angle = 2f * Mathf.PI / segments * i;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);

                // 띠의 바깥 방향: 옆모습의 기울기(dr, dy)에 수직
                Vector3 n = new Vector3(dy * cos, -dr, dy * sin).normalized;

                vertices.Add(new Vector3(p0.x * cos, p0.y, p0.x * sin)); normals.Add(n); uv.Add(new Vector2((float)i / segments, p0.y));
                vertices.Add(new Vector3(p1.x * cos, p1.y, p1.x * sin)); normals.Add(n); uv.Add(new Vector2((float)i / segments, p1.y));
            }

            for (int i = 0; i < segments; i++)
            {
                int a = start + i * 2;   // (i, 아래)
                int c = a + 1;           // (i, 위)
                int b = a + 2;           // (i+1, 아래)
                int d = a + 3;           // (i+1, 위)
                triangles.AddRange(new[] { a, c, b, b, c, d });
            }
        }

        AddCap(profile[profile.Length - 1], Vector3.up, vertices, normals, uv, triangles);
        AddCap(profile[0], Vector3.down, vertices, normals, uv, triangles);

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    // 뚜껑: 가운데 점 하나 + 원 위의 점 segments개, i마다 삼각형 하나
    void AddCap(Vector2 edge, Vector3 normal, System.Collections.Generic.List<Vector3> vertices,
        System.Collections.Generic.List<Vector3> normals, System.Collections.Generic.List<Vector2> uv,
        System.Collections.Generic.List<int> triangles)
    {
        int center = vertices.Count;
        vertices.Add(new Vector3(0f, edge.y, 0f)); normals.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI / segments * i;
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            vertices.Add(new Vector3(edge.x * cos, edge.y, edge.x * sin)); normals.Add(normal);
            uv.Add(new Vector2(0.5f + cos * 0.5f, 0.5f + sin * 0.5f));
        }

        for (int i = 0; i < segments; i++)
        {
            int p = center + 1 + i;
            int q = center + 1 + (i + 1) % segments;
            // 위 뚜껑은 위에서, 아래 뚜껑은 아래에서 볼 때 시계 방향
            if (normal.y > 0) triangles.AddRange(new[] { center, q, p });
            else triangles.AddRange(new[] { center, p, q });
        }
    }
}
