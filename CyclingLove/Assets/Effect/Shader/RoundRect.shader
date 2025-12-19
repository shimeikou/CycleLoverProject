Shader "UI/RoundRect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Radius  ("Radius", Range(0, 0.5)) = 0.08
        _Smooth  ("Smooth", Range(0.0001, 0.05)) = 0.005
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float _Radius;
            float _Smooth;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float RoundRectMask(float2 uv, float radius, float smooth)
            {
                float2 p = abs(uv - 0.5);
                float2 q = max(p - (0.5 - radius), 0.0);
                float d = length(q);
                return 1.0 - smoothstep(radius - smooth, radius, d);
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float mask = RoundRectMask(i.uv, _Radius, _Smooth);
                col.a *= mask;
                return col;
            }
            ENDHLSL
        }
    }
}
