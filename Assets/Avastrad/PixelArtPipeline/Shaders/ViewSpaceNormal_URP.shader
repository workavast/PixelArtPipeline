Shader "Hidden/ViewSpaceNormal_URP"
{
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normal);
                o.normal = normalize(mul((float3x3)UNITY_MATRIX_V, normalWS));
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                return half4(i.normal * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }
    }
}