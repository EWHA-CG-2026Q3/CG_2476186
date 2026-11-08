// Mid_GlowPulse.cs
// 장식: 빛이 숨 쉬듯 밝아졌다 어두워짐 (촛불의 일렁임, 길에 깔린 빛).
// 같은 오브젝트의 Light 밝기와, 있으면 Renderer의 Emission 색을 함께 바꿈.
using UnityEngine;

public class Mid_GlowPulse : MonoBehaviour
{
    public float speed = 1.5f;
    [Range(0f, 1f)] public float amount = 0.35f;   // 0이면 그대로, 1이면 꺼졌다 두 배까지
    public float phase;
    public bool flicker;                           // 촛불처럼 불규칙하게

    [ColorUsage(false, true)] public Color emission = Color.black;

    Light pulseLight;
    Renderer pulseRenderer;
    MaterialPropertyBlock block;
    float baseIntensity;

    void Start()
    {
        pulseLight = GetComponent<Light>();
        pulseRenderer = GetComponent<Renderer>();
        if (pulseLight != null) baseIntensity = pulseLight.intensity;
        block = new MaterialPropertyBlock();
    }

    void Update()
    {
        float t = Time.time * speed + phase;
        float wave = flicker
            ? Mathf.PerlinNoise(t, phase) * 2f - 1f
            : Mathf.Sin(t);
        float k = 1f + amount * wave;

        if (pulseLight != null) pulseLight.intensity = baseIntensity * k;
        if (pulseRenderer != null && emission != Color.black)
        {
            pulseRenderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", emission * k);
            pulseRenderer.SetPropertyBlock(block);
        }
    }
}
