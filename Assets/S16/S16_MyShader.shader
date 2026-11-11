Shader "CG/S16_MyShader"
{
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 정점 셰이더: Object Space 정점에 P x V x M을 곱해 Clip Space 좌표를 돌려줌
            float4 vert(float3 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);
            }

            // 프래그먼트 셰이더: 모든 픽셀을 같은 색으로 칠함
            // (1, 0, 0, 1)이면 Bloom threshold(1)에 걸려 번지므로 빨강을 0.8로 낮춤
            float4 frag() : SV_Target
            {
                return float4(0.8, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}
