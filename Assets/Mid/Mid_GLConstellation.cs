// Mid_GLConstellation.cs
// 중간 프로젝트 / 선택 ③ OpenGL 프로그래밍
// GL 클래스로 밤하늘에 북두칠성을 그림: 별 = 카메라를 향한 작은 마름모(GL.QUADS), 별자리 선 = GL.LINES.
// S06처럼 정점을 직접 호출해 그리는 immediate mode. URP는 OnPostRender를 부르지 않으므로
// 카메라가 그리기를 마칠 때(RenderPipelineManager.endCameraRendering) 그림.
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class Mid_GLConstellation : MonoBehaviour
{
    [Header("별 위치 (이 오브젝트 기준, 미터). 기본값: 북두칠성")]
    public Vector3[] stars =
    {
        new Vector3(-6.0f,  1.2f, 0f),  // 0 요광 (손잡이 끝)
        new Vector3(-4.2f,  1.9f, 0f),  // 1 개양
        new Vector3(-2.6f,  1.6f, 0f),  // 2 옥형
        new Vector3(-1.0f,  0.9f, 0f),  // 3 천권 (국자와 손잡이가 만나는 별)
        new Vector3( 0.0f, -0.6f, 0f),  // 4 천기
        new Vector3( 2.4f, -0.4f, 0f),  // 5 천선
        new Vector3( 2.6f,  1.8f, 0f),  // 6 천추
    };

    [Header("선으로 이을 별 번호 쌍")]
    public Vector2Int[] lines =
    {
        new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3),
        new Vector2Int(3, 4), new Vector2Int(4, 5), new Vector2Int(5, 6), new Vector2Int(6, 3),
    };

    public Color starColor = new Color(1f, 0.95f, 0.8f, 1f);
    public Color lineColor = new Color(0.55f, 0.75f, 1f, 0.6f);
    public float starSize = 0.25f;
    public float twinkleSpeed = 2f;

    [Tooltip("카메라와 별 사이에 지붕 같은 물체가 있으면 그 별과 선을 숨김")]
    public bool hideBehindObjects = true;

    Material lineMaterial;
    bool[] visible;

    void OnEnable() => RenderPipelineManager.endCameraRendering += Draw;
    void OnDisable() => RenderPipelineManager.endCameraRendering -= Draw;

    void OnDestroy()
    {
        if (lineMaterial != null) DestroyImmediate(lineMaterial);
    }

    void CreateMaterial()
    {
        if (lineMaterial != null) return;
        lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
        lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);   // 정자 지붕 뒤의 별은 가려지게
    }

    void Draw(ScriptableRenderContext context, Camera cam)
    {
        if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;
        if (stars == null || stars.Length == 0) return;

        // GL 선은 화면 그리기가 끝난 뒤 덧그려서 깊이 비교가 안 될 수 있음.
        // 그래서 카메라에서 별까지 광선을 쏘아 지붕·기둥 같은 물체에 막히면 그 별은 그리지 않음
        if (visible == null || visible.Length != stars.Length) visible = new bool[stars.Length];
        Vector3 eye = cam.transform.position;
        for (int i = 0; i < stars.Length; i++)
            visible[i] = !hideBehindObjects || !Physics.Linecast(eye, transform.TransformPoint(stars[i]), ~0, QueryTriggerInteraction.Ignore);

        CreateMaterial();
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadProjectionMatrix(cam.projectionMatrix);   // P
        GL.modelview = cam.worldToCameraMatrix;          // V
        GL.MultMatrix(transform.localToWorldMatrix);     // M: 이 오브젝트를 옮기면 별자리도 따라감

        // 1) 별자리 선
        GL.Begin(GL.LINES);
        GL.Color(lineColor);
        foreach (Vector2Int pair in lines)
        {
            if (pair.x < 0 || pair.y < 0 || pair.x >= stars.Length || pair.y >= stars.Length) continue;
            if (!visible[pair.x] || !visible[pair.y]) continue;   // 한쪽 별이라도 가려지면 선도 숨김
            GL.Vertex(stars[pair.x]);
            GL.Vertex(stars[pair.y]);
        }
        GL.End();

        // 2) 별: 카메라를 향한 마름모. 카메라의 오른쪽/위쪽을 이 오브젝트 좌표로 바꿔 씀
        Vector3 right = transform.InverseTransformDirection(cam.transform.right) * starSize;
        Vector3 up = transform.InverseTransformDirection(cam.transform.up) * starSize;
        float time = Application.isPlaying ? Time.time : 0f;

        GL.Begin(GL.QUADS);
        for (int i = 0; i < stars.Length; i++)
        {
            if (!visible[i]) continue;
            float twinkle = 0.75f + 0.25f * Mathf.Sin(time * twinkleSpeed + i * 1.7f);   // 별마다 다르게 깜빡임
            GL.Color(new Color(starColor.r, starColor.g, starColor.b, starColor.a * twinkle));
            Vector3 p = stars[i];
            GL.Vertex(p + up * twinkle);
            GL.Vertex(p + right * twinkle * 0.6f);
            GL.Vertex(p - up * twinkle);
            GL.Vertex(p - right * twinkle * 0.6f);
        }
        GL.End();

        GL.PopMatrix();
    }
}
