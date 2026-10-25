using UnityEngine;
using UnityEngine.UI;

// 빈 오브젝트 VirtualCamera에 붙이는 스크립트
// 카메라를 기준으로 잰 정점 p = V × M × 정점, 그다음 P를 곱하고 w로 나눠서(원근 나눗셈) 캔버스에 와이어프레임을 그림
public class S12_VirtualCamera_Finish : MonoBehaviour
{
    [Header("그릴 오브젝트들")]
    public MeshFilter[] targets;

    [Header("캔버스")]
    public RawImage canvasImage;      // 캔버스를 보여 줄 RawImage
    public int width = 400;
    public int height = 300;

    [Header("카메라 범위 (원근투영)")]
    public float fieldOfView = 90f;   // 세로 시야각(도)
    public float near = 0.3f;
    public float far = 20f;

    [Header("색")]
    public Color32 backgroundColor = new Color32(20, 20, 20, 255);
    public Color32 lineColor = new Color32(255, 255, 255, 255);

    Texture2D canvas;
    Color32[] pixels;
    Matrix4x4 P;                      // 투영 행렬 (프레임마다 갱신)

    void Start()
    {
        canvas = new Texture2D(width, height, TextureFormat.RGBA32, false);
        canvas.filterMode = FilterMode.Point;
        pixels = new Color32[width * height];
        if (canvasImage != null) canvasImage.texture = canvas;
    }

    // 움직이는 오브젝트(Update에서 움직임)가 다 갱신된 뒤에 그리려고 LateUpdate 사용
    void LateUpdate()
    {
        DrawSceneOnCanvas();
    }

    // ---------------------------------------------------------------
    // V = R⁻¹ × T⁻¹ : 먼저 이동을 되돌리고(T⁻¹), 그다음 회전을 되돌림(R⁻¹)
    Matrix4x4 BuildViewMatrix(Transform cam)
    {
        Matrix4x4 Tinv = Matrix4x4.Translate(-cam.position);                    // T⁻¹
        Matrix4x4 Rinv = Matrix4x4.Rotate(Quaternion.Inverse(cam.rotation));    // R⁻¹
        return MultiplyMatrixMatrix(Rinv, Tinv);                                // V = R⁻¹ × T⁻¹ (직접 만든 곱셈)
    }

    // ---------------------------------------------------------------
    // 원근 투영 행렬 P: 1행 1/(tan(fov/2) * aspect), 2행 1/tan(fov/2), 4행 [0 0 1 0]
    Matrix4x4 BuildProjectionMatrix()
    {
        float aspect = (float)width / height;                     // 캔버스의 가로세로 비 (400 ÷ 300)
        float t = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);  // tan(fov / 2): 각도를 라디안으로 바꿔 넣음

        Matrix4x4 P = Matrix4x4.zero;                             // 16칸 모두 0에서 시작
        P.m00 = 1f / (t * aspect);                                // 1행 1열
        P.m11 = 1f / t;                                           // 2행 2열
        P.m32 = 1f;                                               // 4행 3열: 4행이 [0 0 1 0], w = z
        return P;                                                 // 3행(깊이 자리)은 0 그대로
    }

    // ---------------------------------------------------------------
    // 행렬 × 행렬 = 행렬 × 벡터를 열마다 네 번
    // (A × B)의 n번째 열 = A × (B의 n번째 열)
    Matrix4x4 MultiplyMatrixMatrix(Matrix4x4 A, Matrix4x4 B)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int n = 0; n < 4; n++)
        {
            Vector4 col = MultiplyMatrixVector(A, B.GetColumn(n));
            result.SetColumn(n, col);
        }
        return result;
    }

    // 행렬 × 벡터 = 각 열에 벡터의 성분을 곱해 더한 것
    Vector4 MultiplyMatrixVector(Matrix4x4 A, Vector4 v)
    {
        return A.GetColumn(0) * v.x
             + A.GetColumn(1) * v.y
             + A.GetColumn(2) * v.z
             + A.GetColumn(3) * v.w;
    }

    // ---------------------------------------------------------------
    void DrawSceneOnCanvas()
    {
        if (canvas == null) return;
        ClearCanvas();

        Matrix4x4 V = BuildViewMatrix(transform);   // 프레임마다 한 번
        P = BuildProjectionMatrix();                // 프레임마다 한 번

        foreach (MeshFilter mf in targets)
        {
            if (mf == null || mf.sharedMesh == null) continue;

            Matrix4x4 VM = V * mf.transform.localToWorldMatrix;   // V × M (오브젝트마다 한 번)
            Vector3[] verts = mf.sharedMesh.vertices;
            int[] tris = mf.sharedMesh.triangles;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = VM.MultiplyPoint(verts[tris[i]]);
                Vector3 b = VM.MultiplyPoint(verts[tris[i + 1]]);
                Vector3 c = VM.MultiplyPoint(verts[tris[i + 2]]);

                DrawEdge(a, b);
                DrawEdge(b, c);
                DrawEdge(c, a);
            }
        }

        canvas.SetPixels32(pixels);
        canvas.Apply();
    }

    // 카메라를 기준으로 잰 좌표 p를 캔버스 픽셀 위치로 (원근투영: P를 곱하고 w로 나눔)
    bool ToPixel(Vector3 p, out Vector2 pixel)
    {
        pixel = Vector2.zero;
        if (p.z < near || p.z > far) return false;   // 카메라 뒤(z < 0)도 여기서 걸러짐

        Vector4 clip = P * new Vector4(p.x, p.y, p.z, 1f);                    // P × (x, y, z, 1): 점이므로 네 번째 성분 1
        Vector2 ndc = new Vector2(clip.x / clip.w, clip.y / clip.w);          // w(= z)로 나눔: Perspective division

        pixel.x = width * 0.5f + ndc.x * (width * 0.5f);     // 캔버스 가운데 + NDC * 가로 절반
        pixel.y = height * 0.5f + ndc.y * (height * 0.5f);   // 캔버스 가운데 + NDC * 세로 절반
        return true;
    }

    void DrawEdge(Vector3 p0, Vector3 p1)
    {
        if (!ToPixel(p0, out Vector2 s)) return;
        if (!ToPixel(p1, out Vector2 e)) return;
        DrawLine(Mathf.RoundToInt(s.x), Mathf.RoundToInt(s.y),
                 Mathf.RoundToInt(e.x), Mathf.RoundToInt(e.y));
    }

    // Bresenham 선 그리기 (캔버스 밖 픽셀은 건너뜀)
    void DrawLine(int x0, int y0, int x1, int y1)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        // 화면 밖으로 아주 멀리 나간 선 때문에 오래 걸리는 것 방지
        int maxSteps = (dx + dy) + 1;
        if (maxSteps > 20000) return;

        while (true)
        {
            SetPixel(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    void SetPixel(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        pixels[y * width + x] = lineColor;
    }

    void ClearCanvas()
    {
        for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor;
    }

    // Scene 뷰에서 카메라 축과 담기는 범위(Frustum, 절두체) 표시
    void OnDrawGizmos()
    {
        float aspect = (float)width / height;
        float t = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        Gizmos.matrix = transform.localToWorldMatrix;

        float hn = near * t, wn = hn * aspect;   // near 면의 세로/가로 절반
        float hf = far * t, wf = hf * aspect;    // far 면의 세로/가로 절반

        Vector3[] n = {
            new Vector3(-wn,  hn, near), new Vector3( wn,  hn, near),
            new Vector3( wn, -hn, near), new Vector3(-wn, -hn, near) };
        Vector3[] f = {
            new Vector3(-wf,  hf, far), new Vector3( wf,  hf, far),
            new Vector3( wf, -hf, far), new Vector3(-wf, -hf, far) };

        Gizmos.color = Color.yellow;
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(n[i], n[(i + 1) % 4]);   // near 면
            Gizmos.DrawLine(f[i], f[(i + 1) % 4]);   // far 면
            Gizmos.DrawLine(n[i], f[i]);             // 옆 모서리
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * 1.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(Vector3.zero, Vector3.right);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(Vector3.zero, Vector3.up);
    }
}
