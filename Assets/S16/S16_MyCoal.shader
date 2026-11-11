Shader "CG/S16_MyCoal"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0, 0, 0, 1)
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Properties와 같은 이름: 재질 Inspector의 값을 받음
            float4 _BaseColor;
            float4 _EmissionColor;

            float4 vert(float3 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);
            }

            // 바탕색에 Emission을 더함. 1을 넘는 값은 Bloom으로 번짐
            float4 frag() : SV_Target
            {
                return float4(_BaseColor.rgb + _EmissionColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}
