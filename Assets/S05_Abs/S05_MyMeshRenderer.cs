using UnityEngine;
using UnityEngine.UI;

public class S05_MyMeshRenderer : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 1f); // 검정

    [Header("무늬 실습 (줄무늬·체스판 공용)")]
    [SerializeField] private int patternSize = 16;
    [SerializeField] private Color colorA = new Color(1f, 1f, 1f, 1f); // 흰색
    [SerializeField] private Color colorB = new Color(0.3f, 0.5f, 0.8f, 1f); // 하늘색

    private Texture2D canvasTexture;
    private RawImage targetImage;

    void Start()
    {
        targetImage = GetComponent<RawImage>();

        // 1. 빈 텍스처(Texture2D) 생성 — 아직 아무 색도 채워지지 않은 상태
        canvasTexture = new Texture2D(canvasWidth, canvasHeight);

        // 2. 픽셀 경계를 흐리지 않게(확대해도 네모난 픽셀 그대로 보이도록)
        canvasTexture.filterMode = FilterMode.Point;

        // 3. 픽셀 채우기 — 배경을 빨간색으로 채움
        FillBackground(new Color(1f, 0f, 0f, 1f)); // 빨강

        // 4. 지금까지의 SetPixel 변경 사항을 실제로 텍스처에 반영
        canvasTexture.Apply();

        // 5. 완성된 텍스처를 화면의 RawImage에 연결
        targetImage.texture = canvasTexture;
    }

    // 캔버스 전체를 한 가지 색으로 채움 (모든 픽셀이 같은 값)
    private void FillBackground(Color color)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                canvasTexture.SetPixel(x, y, color);
            }
        }
    }
}
