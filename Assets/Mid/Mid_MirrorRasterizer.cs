// Mid_MirrorRasterizer.cs
// 중간 프로젝트 / 선택 ② 래스터화 — 마녀의 거울
// S06의 소프트웨어 래스터라이저(barycentric 판정)로 그린 그림을 거울 면에 띄움.
//   거울 면: 타원 위에 점 segments개 + 가운데 점으로 만든 타원판 메시 (반복문)
//   그림: 정면에서 본 블랙홀 — 별이 가득한 검은 우주에서, 은빛 별 가루가 가는 줄무늬를 그리며
//         소용돌이치다 가운데 검은 구멍으로 빨려 들어감
//   모든 별과 줄무늬는 삼각형 하나씩으로 래스터화함. 겹치는 곳은 색을 더해(additive) 밝게 쌓임.
//   Play 중에는 줄무늬가 돌면서 안쪽으로 빨려 들어가도록 일정 간격으로 다시 그림
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_MirrorRasterizer : MonoBehaviour
{
    [Header("거울 면 (반지름: 가로, 세로)")]
    public float halfWidth = 0.42f;
    public float halfHeight = 0.68f;
    [Range(8, 128)] public int segments = 48;

    [Header("래스터화 캔버스")]
    public int canvasWidth = 320;
    public int canvasHeight = 512;
    public float redrawInterval = 0.05f;

    [Header("블랙홀")]
    [Range(200, 4000)] public int streaks = 1600;   // 소용돌이 줄무늬 수
    [Range(0, 3000)] public int backgroundStars = 900;
    public float horizon = 0.17f;       // 검은 구멍 반지름 (캔버스 짧은 쪽 절반 = 1)
    public float outerRadius = 1.25f;   // 소용돌이가 시작되는 바깥 반지름
    public float inflowSpeed = 0.05f;   // 1초에 바깥→안쪽으로 흘러드는 비율
    public float swirlSpeed = 0.35f;    // 소용돌이가 도는 빠르기
    public Color dustColor = new Color(0.85f, 0.88f, 0.95f);   // 은빛 별 가루

    Mesh mesh;
    Texture2D canvas;
    Color32[] pixels;
    Color32[] background;   // 별 배경은 한 번만 그려 두고 매번 복사
    MaterialPropertyBlock block;
    float nextDraw;

    // 줄무늬마다 고정된 무작위 값 (처음 위치, 각도, 밝기)
    float[] seedRadius, seedAngle, seedBright;

    void OnEnable()
    {
        BuildMesh();
        canvas = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave,
        };
        pixels = new Color32[canvasWidth * canvasHeight];
        MakeSeeds();
        DrawBackground();
        Draw(0f);
    }

    void OnDisable()
    {
        if (canvas != null) DestroyImmediate(canvas);
    }

    void Update()
    {
        if (!Application.isPlaying || Time.time < nextDraw) return;
        nextDraw = Time.time + redrawInterval;
        Draw(Time.time);
    }

    // ---- 거울 면 메시: 타원판 ----
    void BuildMesh()
    {
        if (mesh == null)
        {
            mesh = new Mesh { name = "Mid_MirrorSurface" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        Vector3[] vertices = new Vector3[segments + 1];
        Vector3[] normals = new Vector3[segments + 1];
        Vector2[] uv = new Vector2[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI / segments * i;
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            vertices[i + 1] = new Vector3(cos * halfWidth, sin * halfHeight, 0f);
            uv[i + 1] = new Vector2(0.5f + cos * 0.5f, 0.5f + sin * 0.5f);
        }
        for (int i = 0; i <= segments; i++) normals[i] = Vector3.back;   // 앞면은 -z 쪽(보는 사람 쪽)

        for (int i = 0; i < segments; i++)
        {
            // -z에서 볼 때 시계 방향
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = (i + 1) % segments + 1;
            triangles[i * 3 + 2] = i + 1;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    void MakeSeeds()
    {
        Random.State saved = Random.state;
        Random.InitState(2476186);
        seedRadius = new float[streaks];
        seedAngle = new float[streaks];
        seedBright = new float[streaks];
        for (int i = 0; i < streaks; i++)
        {
            seedRadius[i] = Random.value;
            seedAngle[i] = Random.value * Mathf.PI * 2f;
            seedBright[i] = Random.Range(0.35f, 1f);
        }
        Random.state = saved;
    }

    Vector2 Center => new Vector2(canvasWidth * 0.5f, canvasHeight * 0.5f);
    float Unit => Mathf.Min(canvasWidth, canvasHeight) * 0.5f;

    // ---- 배경: 검은 우주 + 무작위 별 (한 번만) ----
    void DrawBackground()
    {
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(2, 2, 5, 255);

        Random.State saved = Random.state;
        Random.InitState(7);
        for (int i = 0; i < backgroundStars; i++)
        {
            Vector2 p = new Vector2(Random.value * canvasWidth, Random.value * canvasHeight);
            float size = Random.value < 0.92f ? Random.Range(0.6f, 1.1f) : Random.Range(1.4f, 2.2f);
            float b = Random.Range(0.25f, 1f);
            Color c = Random.value < 0.15f ? new Color(1f, 0.85f, 0.6f) * b : new Color(0.9f, 0.92f, 1f) * b;
            // 별 하나 = 작은 삼각형 두 개 (마름모)
            DrawTriangle(p + Vector2.up * size, p + Vector2.right * size, p + Vector2.down * size, c, true);
            DrawTriangle(p + Vector2.up * size, p + Vector2.down * size, p + Vector2.left * size, c, true);
        }
        Random.state = saved;

        if (background == null || background.Length != pixels.Length) background = new Color32[pixels.Length];
        System.Array.Copy(pixels, background, pixels.Length);
    }

    // ---- 매 프레임: 소용돌이 ----
    void Draw(float time)
    {
        System.Array.Copy(background, pixels, pixels.Length);
        Vector2 center = Center;
        float unit = Unit;

        // 1) 은빛 줄무늬: 바깥에서 안쪽으로 빨려 들며 돎. 안쪽일수록 빽빽하고 밝고 빠르며 길게 늘어남
        for (int i = 0; i < streaks; i++)
        {
            // t: 1 = 바깥, 0 = 구멍. 시간에 따라 줄어들다 0에 닿으면 다시 바깥에서 시작
            float t = Mathf.Repeat(seedRadius[i] - time * inflowSpeed, 1f);
            float r = horizon + (outerRadius - horizon) * t * t;             // 제곱: 안쪽에 더 많이 모이게
            float angle = seedAngle[i] - Mathf.Log(r / horizon) * 1.6f       // 나선 모양으로 감김
                        + time * swirlSpeed / Mathf.Max(0.15f, r);           // 안쪽일수록 빨리 돎

            float near = 1f - Mathf.Clamp01((r - horizon) / (outerRadius - horizon));   // 구멍에 가까울수록 1
            float edgeFade = Mathf.Clamp01((r - horizon) / 0.04f);                       // 구멍 가장자리에서 사라짐
            float bright = seedBright[i] * Mathf.Lerp(0.08f, 0.9f, Mathf.Pow(near, 2.5f)) * edgeFade;
            Color c = dustColor * bright;

            // 줄무늬 = 나선을 따라 휘어진 가는 띠. 짧은 조각 3개를 이어 그림 (조각마다 삼각형 2개)
            float arc = Mathf.Lerp(0.9f, 0.12f, Mathf.Sqrt(t));   // 안쪽일수록 길게 휘감김 (라디안)
            float width = Mathf.Lerp(0.0075f, 0.005f, t) * unit;
            const int pieces = 3;
            Vector2 prev = Spiral(center, unit, r, angle);
            for (int s = 1; s <= pieces; s++)
            {
                float u = (float)s / pieces;
                // 꼬리 쪽은 조금 더 바깥(빨려 들기 전 자리)에 있도록 반지름을 늘림
                Vector2 next = Spiral(center, unit, r * (1f + 0.08f * u), angle - arc * u);
                Vector2 d = next - prev;
                if (d.sqrMagnitude < 1e-4f) continue;
                Vector2 side = new Vector2(-d.y, d.x).normalized * width * (1f - 0.6f * u);   // 꼬리로 갈수록 가늘게
                Color pc = c * (1f - 0.55f * u);                                               // 꼬리로 갈수록 흐리게
                DrawTriangle(prev + side, next + side, next - side, pc, true);
                DrawTriangle(prev + side, next - side, prev - side, pc, true);
                prev = next;
            }
        }

        // 2) 구멍: 가장자리가 부드럽게 어두워지도록 반지름을 줄여 가며 검은 원을 겹쳐 그림
        for (int k = 0; k < 4; k++)
        {
            float rr = horizon * (1.08f - 0.04f * k);
            float a = 0.35f + 0.22f * k;   // 마지막 원은 완전히 검게
            DrawDisc(center, rr * unit, new Color(0f, 0f, 0f, Mathf.Min(1f, a)));
        }

        canvas.SetPixels32(pixels);
        canvas.Apply();

        if (block == null) block = new MaterialPropertyBlock();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.GetPropertyBlock(block);
        block.SetTexture("_BaseMap", canvas);
        block.SetTexture("_EmissionMap", canvas);
        meshRenderer.SetPropertyBlock(block);
    }

    static Vector2 Spiral(Vector2 center, float unit, float r, float angle)
        => center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r * unit;

    // 원 = 가운데 점 + 원 위의 이웃한 두 점으로 만든 삼각형 48개. 색의 알파만큼 덮어 칠함
    void DrawDisc(Vector2 center, float r, Color color)
    {
        const int n = 48;
        for (int i = 0; i < n; i++)
        {
            float a0 = 2f * Mathf.PI / n * i, a1 = 2f * Mathf.PI / n * (i + 1);
            DrawTriangle(center,
                center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r,
                center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r, color, false);
        }
    }

    // S06과 같은 방법: 픽셀 중심이 삼각형 안에 있는지 barycentric coordinate로 판정해 색칠.
    // 캔버스 전체 대신 삼각형을 감싸는 사각형 안의 픽셀만 검사함.
    // additive = true: 원래 색에 더함(빛이 쌓임), false: 알파만큼 섞어 덮음
    void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, bool additive)
    {
        float denom = a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y);
        if (Mathf.Abs(denom) < 1e-6f) return;

        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
        int maxX = Mathf.Min(canvasWidth - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
        int maxY = Mathf.Min(canvasHeight - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

        int cr = (int)(color.r * 255f), cg = (int)(color.g * 255f), cb = (int)(color.b * 255f);
        float alpha = color.a;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float w1 = (p.x * (b.y - c.y) + b.x * (c.y - p.y) + c.x * (p.y - b.y)) / denom;
                float w2 = (a.x * (p.y - c.y) + p.x * (c.y - a.y) + c.x * (a.y - p.y)) / denom;
                float w3 = 1f - w1 - w2;
                if (w1 < 0f || w2 < 0f || w3 < 0f) continue;

                int k = y * canvasWidth + x;
                Color32 old = pixels[k];
                if (additive)
                    pixels[k] = new Color32((byte)Mathf.Min(255, old.r + cr), (byte)Mathf.Min(255, old.g + cg), (byte)Mathf.Min(255, old.b + cb), 255);
                else
                    pixels[k] = new Color32((byte)(old.r + (cr - old.r) * alpha), (byte)(old.g + (cg - old.g) * alpha), (byte)(old.b + (cb - old.b) * alpha), 255);
            }
        }
    }
}
