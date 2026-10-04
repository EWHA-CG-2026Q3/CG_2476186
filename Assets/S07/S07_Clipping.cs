using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class S07_Clipping : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;
    [SerializeField] private int clipMargin = 40;
    [SerializeField] private Color fillColor = new Color(1f, 0.6f, 0.2f, 1f);

    // 왼쪽 경계(x=40)와 위쪽 경계(y=216)를 동시에 넘어가도록 설계한 좌표
    [SerializeField] private List<Vector2> polygon = new List<Vector2> {
        new Vector2(20, 120), new Vector2(110, 240), new Vector2(200, 120)
    };

    private Texture2D canvasTexture;
    private RawImage targetImage;

    void OnEnable() { RedrawAll(); }
    void OnValidate() { RedrawAll(); }

    private void RedrawAll()
    {
        targetImage = GetComponent<RawImage>();
        if (targetImage == null) return;

        if (canvasTexture == null || canvasTexture.width != canvasWidth || canvasTexture.height != canvasHeight)
        {
            canvasTexture = new Texture2D(canvasWidth, canvasHeight);
            canvasTexture.filterMode = FilterMode.Point;
        }

        for (int x = 0; x < canvasWidth; x++)
            for (int y = 0; y < canvasHeight; y++)
                canvasTexture.SetPixel(x, y, Color.black);

        DrawMarginOutline();

        List<Vector2> clipped = polygon;
        clipped = ClipLeft(clipped, clipMargin);
        clipped = ClipRight(clipped, canvasWidth - clipMargin);
        clipped = ClipBottom(clipped, clipMargin);
        clipped = ClipTop(clipped, canvasHeight - clipMargin);
        FillPolygon(clipped, fillColor);

        canvasTexture.Apply();
        targetImage.texture = canvasTexture;
    }

    private void DrawMarginOutline()
    {
        Color gray = Color.gray;
        int right = canvasWidth - clipMargin;
        int top = canvasHeight - clipMargin;
        for (int x = clipMargin; x <= right; x++)
        {
            canvasTexture.SetPixel(x, clipMargin, gray);
            canvasTexture.SetPixel(x, top, gray);
        }
        for (int y = clipMargin; y <= top; y++)
        {
            canvasTexture.SetPixel(clipMargin, y, gray);
            canvasTexture.SetPixel(right, y, gray);
        }
    }

    private List<Vector2> ClipLeft(List<Vector2> input, float boundary)
    {
        List<Vector2> output = new List<Vector2>();
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            Vector2 previous = input[(i - 1 + input.Count) % input.Count];
            bool currentInside = current.x >= boundary;
            bool previousInside = previous.x >= boundary;
            if (currentInside)
            {
                if (!previousInside) output.Add(GetIntersectionX(previous, current, boundary));
                output.Add(current);
            }
            else if (previousInside)
            {
                output.Add(GetIntersectionX(previous, current, boundary));
            }
        }
        return output;
    }

    // 오른쪽 경계: 안쪽 = x <= boundary
    private List<Vector2> ClipRight(List<Vector2> input, float boundary)
    {
        List<Vector2> output = new List<Vector2>();
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            Vector2 previous = input[(i - 1 + input.Count) % input.Count];
            bool currentInside = current.x <= boundary;
            bool previousInside = previous.x <= boundary;
            if (currentInside)
            {
                if (!previousInside) output.Add(GetIntersectionX(previous, current, boundary));
                output.Add(current);
            }
            else if (previousInside)
            {
                output.Add(GetIntersectionX(previous, current, boundary));
            }
        }
        return output;
    }

    // 아래쪽 경계: 안쪽 = y >= boundary
    private List<Vector2> ClipBottom(List<Vector2> input, float boundary)
    {
        List<Vector2> output = new List<Vector2>();
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            Vector2 previous = input[(i - 1 + input.Count) % input.Count];
            bool currentInside = current.y >= boundary;
            bool previousInside = previous.y >= boundary;
            if (currentInside)
            {
                if (!previousInside) output.Add(GetIntersectionY(previous, current, boundary));
                output.Add(current);
            }
            else if (previousInside)
            {
                output.Add(GetIntersectionY(previous, current, boundary));
            }
        }
        return output;
    }

    // 위쪽 경계: 안쪽 = y <= boundary
    private List<Vector2> ClipTop(List<Vector2> input, float boundary)
    {
        List<Vector2> output = new List<Vector2>();
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            Vector2 previous = input[(i - 1 + input.Count) % input.Count];
            bool currentInside = current.y <= boundary;
            bool previousInside = previous.y <= boundary;
            if (currentInside)
            {
                if (!previousInside) output.Add(GetIntersectionY(previous, current, boundary));
                output.Add(current);
            }
            else if (previousInside)
            {
                output.Add(GetIntersectionY(previous, current, boundary));
            }
        }
        return output;
    }

    private Vector2 GetIntersectionX(Vector2 p1, Vector2 p2, float boundaryX)
    {
        float t = (boundaryX - p1.x) / (p2.x - p1.x);
        return new Vector2(boundaryX, p1.y + t * (p2.y - p1.y));
    }

    private Vector2 GetIntersectionY(Vector2 p1, Vector2 p2, float boundaryY)
    {
        float t = (boundaryY - p1.y) / (p2.y - p1.y);
        return new Vector2(p1.x + t * (p2.x - p1.x), boundaryY);
    }

    private void FillPolygon(List<Vector2> poly, Color color)
    {
        if (poly.Count < 3) return;
        for (int i = 1; i < poly.Count - 1; i++)
            DrawTriangle(poly[0], poly[i], poly[i + 1], color);
    }

    private void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
        int maxX = Mathf.Min(canvasWidth - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
        int maxY = Mathf.Min(canvasHeight - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                if (IsInsideTriangle(new Vector2(x, y), a, b, c))
                    canvasTexture.SetPixel(x, y, color);
    }

    private bool IsInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float denom = a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y);
        if (Mathf.Abs(denom) < 1e-6f) return false;
        float w1 = (p.x * (b.y - c.y) + b.x * (c.y - p.y) + c.x * (p.y - b.y)) / denom;
        float w2 = (a.x * (p.y - c.y) + p.x * (c.y - a.y) + c.x * (a.y - p.y)) / denom;
        float w3 = 1f - w1 - w2;
        return w1 >= 0f && w2 >= 0f && w3 >= 0f;
    }
}
