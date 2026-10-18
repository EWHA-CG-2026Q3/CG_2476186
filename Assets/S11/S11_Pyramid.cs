using UnityEngine;

// 빈 오브젝트에 붙이면 피라미드 메시를 만들어 줌 (에디트 모드에서도 생김)
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S11_Pyramid : MonoBehaviour
{
    void OnEnable()
    {
        Mesh m = new Mesh();
        m.name = "Pyramid";

        m.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, -0.5f), // 0
            new Vector3( 0.5f, 0, -0.5f), // 1
            new Vector3( 0.5f, 0,  0.5f), // 2
            new Vector3(-0.5f, 0,  0.5f), // 3
            new Vector3( 0,    1,  0)     // 4 꼭대기
        };

        m.triangles = new int[]
        {
            0, 2, 1,  0, 3, 2,                          // 밑면
            0, 1, 4,  1, 2, 4,  2, 3, 4,  3, 0, 4       // 옆면 4개
        };

        m.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = m;

        MeshRenderer r = GetComponent<MeshRenderer>();
        if (r.sharedMaterial == null)
            r.sharedMaterial = new Material(Shader.Find("Standard"));
    }
}
