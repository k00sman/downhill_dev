Shader "Skybox/Horizontal Skybox URP"
{
    Properties
    {
        _Color1     ("Top Color",      Color) = (1, 1, 1, 0)
        _Color2     ("Horizon Color",  Color) = (1, 1, 1, 0)
        _Color3     ("Bottom Color",   Color) = (1, 1, 1, 0)
        _Exponent1  ("Exponent Factor for Top Half",    Float) = 1.0
        _Exponent2  ("Exponent Factor for Bottom Half", Float) = 1.0
        _Intensity  ("Intensity Amplifier",             Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Background"
            "Queue"          = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color1;
                half4 _Color2;
                half4 _Color3;
                half  _Exponent1;
                half  _Exponent2;
                half  _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 texcoord   : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord   : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.texcoord   = IN.texcoord;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float p  = normalize(IN.texcoord).y;
                float p1 = 1.0 - pow(saturate(1.0 - p), _Exponent1); // top
                float p3 = 1.0 - pow(saturate(1.0 + p), _Exponent2); // bottom
                float p2 = 1.0 - p1 - p3;                             // horizon

                half4 color = (_Color1 * p1 + _Color2 * p2 + _Color3 * p3) * _Intensity;
                return color;
            }

            ENDHLSL
        }
    }
}
