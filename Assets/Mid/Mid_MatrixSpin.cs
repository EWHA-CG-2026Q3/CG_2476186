// Mid_MatrixSpin.cs
// 중간 프로젝트 / 필수 ④ 변환
// 회전 행렬을 cos, sin으로 직접 채워 만들고, 행렬끼리 곱해 물체의 위치와 방향을 정함.
//   M = T(위치 + 떠오름) × R_기울기(baseEuler) × R_회전(axis, angle)
// 혼천의의 고리들은 부모-자식으로 묶여 있어서, 부모가 돌면 자식이 함께 돌고
// 자식은 그 위에서 자기 축으로 한 번 더 돎.
using UnityEngine;

[ExecuteAlways]
public class Mid_MatrixSpin : MonoBehaviour
{
    [Header("기울기 (도): Unity와 같은 Z → X → Y 순서로 곱함")]
    public Vector3 baseEuler;

    [Header("회전: 기울어진 물체의 자기 축")]
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 20f;

    [Header("떠오름: 위아래로 오르내리는 폭(m)과 속도")]
    public float bobHeight = 0f;
    public float bobSpeed = 1f;

    Vector3 basePosition;

    void OnEnable()
    {
        basePosition = transform.localPosition;
        Apply(0f);
    }

    void OnValidate()
    {
        if (Application.isPlaying || !isActiveAndEnabled) return;
        basePosition = transform.localPosition;   // 에디터에서 옮긴 위치를 기준으로 삼음
        Apply(0f);
    }

    void Update()
    {
        if (Application.isPlaying) Apply(Time.time);
    }

    void Apply(float time)
    {
        float angle = degreesPerSecond * time * Mathf.Deg2Rad;
        Vector3 offset = Vector3.up * (Mathf.Sin(time * bobSpeed) * bobHeight);

        Matrix4x4 tilt = RotY(baseEuler.y * Mathf.Deg2Rad)
                       * RotX(baseEuler.x * Mathf.Deg2Rad)
                       * RotZ(baseEuler.z * Mathf.Deg2Rad);
        Matrix4x4 m = Translate(basePosition + offset) * tilt * RotAxis(axis.normalized, angle);

        // 행렬의 열: 0 = 오른쪽(x), 1 = 위(y), 2 = 앞(z), 3 = 위치
        transform.localPosition = m.GetColumn(3);
        transform.localRotation = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    // ---- 직접 채우는 변환 행렬 ----

    static Matrix4x4 Translate(Vector3 p)
    {
        Matrix4x4 m = Matrix4x4.identity;
        m.m03 = p.x; m.m13 = p.y; m.m23 = p.z;
        return m;
    }

    static Matrix4x4 RotX(float a)
    {
        float c = Mathf.Cos(a), s = Mathf.Sin(a);
        Matrix4x4 m = Matrix4x4.identity;
        m.m11 = c; m.m12 = -s;
        m.m21 = s; m.m22 = c;
        return m;
    }

    static Matrix4x4 RotY(float a)
    {
        float c = Mathf.Cos(a), s = Mathf.Sin(a);
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = c;  m.m02 = s;
        m.m20 = -s; m.m22 = c;
        return m;
    }

    static Matrix4x4 RotZ(float a)
    {
        float c = Mathf.Cos(a), s = Mathf.Sin(a);
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = c; m.m01 = -s;
        m.m10 = s; m.m11 = c;
        return m;
    }

    // 임의의 축 k = (x, y, z)로 a만큼 도는 행렬 (Rodrigues 공식)
    static Matrix4x4 RotAxis(Vector3 k, float a)
    {
        float c = Mathf.Cos(a), s = Mathf.Sin(a), t = 1f - c;
        float x = k.x, y = k.y, z = k.z;
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = t * x * x + c;     m.m01 = t * x * y - s * z; m.m02 = t * x * z + s * y;
        m.m10 = t * x * y + s * z; m.m11 = t * y * y + c;     m.m12 = t * y * z - s * x;
        m.m20 = t * x * z - s * y; m.m21 = t * y * z + s * x; m.m22 = t * z * z + c;
        return m;
    }
}
