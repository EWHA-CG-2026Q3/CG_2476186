using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class S07_DepthTest2 : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;
    [SerializeField] private Color backgroundColor = new Color(0.73f, 0.73f, 0.73f, 1f);

    // 삼각형 1 (파랑)
    [SerializeField] private Vector3 vertexA1 = new Vector3(120, 210, 0.5f);
    [SerializeField] private Vector3 vertexB1 = new Vector3(80, 40, 0.8f);
    [SerializeField] private Vector3 vertexC1 = new Vector3(190, 40, 0.2f);
    [SerializeField] private Color color1 = new Color(0.23f, 0.51f, 1f, 1f);

    // 삼각형 2 (주황)
    [SerializeField] private Vector3 vertexA2 = new Vector3(70, 160, 0.5f);
    [SerializeField] private Vector3 vertexB2 = new Vector3(20, 70, 0.2f);
    [SerializeField] private Vector3 vertexC2 = new Vector3(130, 70, 0.8f);
    [SerializeField] private Color color2 = new Color(1f, 0.4f, 0.24f, 1f);

    // 삼각형 3 (초록)
    [SerializeField] private Vector3 vertexA3 = new Vector3(190, 220, 0.35f);
    [SerializeField] private Vector3 vertexB3 = new Vector3(150, 30, 0.35f);
    [SerializeField] private Vector3 vertexC3 = new Vector3(240, 30, 0.35f);
    [SerializeField] private Color color3 = new Color(0.3f, 0.9f, 0.4f, 1f);

    private Texture2D canvasTexture;
    private float[,] depthBuffer;

    void OnEnable() { RedrawAll(); }
    void OnValidate() { RedrawAll(); }

    private void RedrawAll()
    {
        RawImage targetImage = GetComponent<RawImage>();
        if (targetImage == null) return;

        if (canvasTexture == null || canvasTexture.width != canvasWidth || canvasTexture.height != canvasHeight)
        {
            canvasTexture = new Texture2D(canvasWidth, canvasHeight);
            canvasTexture.filterMode = FilterMode.Point;
        }
        depthBuffer = new float[canvasWidth, canvasHeight];

        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                canvasTexture.SetPixel(x, y, backgroundColor);
                depthBuffer[x, y] = float.MaxValue;
            }
        }

        // TODO 0: 그리는 순서를 바꿔도 깊이 테스트 덕분에 결과가 같아야 정상
        DrawTriangle(vertexA3, vertexB3, vertexC3, color3);
        DrawTriangle(vertexA1, vertexB1, vertexC1, color1);
        DrawTriangle(vertexA2, vertexB2, vertexC2, color2);

        canvasTexture.Apply();
        targetImage.texture = canvasTexture;
    }

    private void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
        int maxX = Mathf.Min(canvasWidth - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
        int maxY = Mathf.Min(canvasHeight - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

        float denom = a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y);
        if (Mathf.Abs(denom) < 1e-6f) return;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                float px = x;
                float py = y;
                float w1 = (px * (b.y - c.y) + b.x * (c.y - py) + c.x * (py - b.y)) / denom;
                float w2 = (a.x * (py - c.y) + px * (c.y - a.y) + c.x * (a.y - py)) / denom;
                float w3 = 1f - w1 - w2;
                bool isInside = w1 >= 0f && w2 >= 0f && w3 >= 0f;

                if (isInside)
                {
                    // TODO 1: 무게중심좌표로 보간된 z 계산
                    float interpolatedZ = w1 * a.z + w2 * b.z + w3 * c.z;

                    // TODO 2: 더 가까울 때(z가 더 작을 때)만 색과 깊이 갱신
                    if (interpolatedZ < depthBuffer[x, y])
                    {
                        canvasTexture.SetPixel(x, y, color);
                        depthBuffer[x, y] = interpolatedZ;
                    }
                }
            }
        }
    }
}
