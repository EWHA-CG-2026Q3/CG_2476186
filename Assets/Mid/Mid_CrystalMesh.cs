// Mid_CrystalMesh.cs
// 중간 프로젝트 / 필수 ① 기하 모델링 (반복문 메시 3) — 자수정 결정
// n각기둥 몸통 + 뾰족한 끝(n각뿔). 옆면 i마다 사각형 1개와 끝의 삼각형 1개를 반복문으로 만듦.
// 결정처럼 면이 또렷하게 보이도록 면마다 정점을 따로 둠(S19의 면 나눈 사각뿔과 같은 방식).
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_CrystalMesh : MonoBehaviour
{
    [Range(3, 12)] public int sides = 6;      // 자수정은 육각기둥
    [Min(0.001f)] public float radius = 0.05f;
    [Min(0.001f)] public float bodyHeight = 0.18f;
    [Min(0.001f)] public float tipHeight = 0.08f;
    [Range(0.3f, 1.5f)] public float topScale = 0.85f;   // 위로 갈수록 살짝 가늘어짐

    Mesh mesh;

    void OnEnable() => Build();
    void OnValidate() { if (mesh != null) Build(); }

    public void Build()
    {
        if (mesh == null)
        {
            mesh = new Mesh { name = "Mid_Crystal" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        Vector3[] bottom = new Vector3[sides];
        Vector3[] top = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = 2f * Mathf.PI / sides * i;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            bottom[i] = dir * radius;
            top[i] = dir * radius * topScale + Vector3.up * bodyHeight;
        }
        Vector3 apex = Vector3.up * (bodyHeight + tipHeight);
        Vector3 baseCenter = Vector3.zero;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        // 바깥에서 볼 때 시계 방향이 앞면
        void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            triangles.Add(vertices.Count); vertices.Add(a);
            triangles.Add(vertices.Count); vertices.Add(b);
            triangles.Add(vertices.Count); vertices.Add(c);
        }

        for (int i = 0; i < sides; i++)
        {
            int j = (i + 1) % sides;
            Tri(bottom[i], top[i], bottom[j]);     // 옆면 사각형 = 삼각형 2개
            Tri(bottom[j], top[i], top[j]);
            Tri(top[i], apex, top[j]);             // 끝의 삼각형
            Tri(baseCenter, bottom[i], bottom[j]); // 밑면: 가운데 점 + 이웃한 두 점
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();   // 정점마다 한 면에만 속하므로 면의 normal 그대로
        mesh.RecalculateBounds();
    }
}
