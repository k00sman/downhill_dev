Shader "Custom/FlatLitBillboard"
{
    Properties
    {
        _Albedo         ("Albedo",                  2D)             = "white" {}
        _Cutoff         ("Alpha Clip Threshold",    Range(0,1))     = 0.5
        _NormalOverride ("Normal Override",         Range(0,1))     = 1.0
        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull           ("Render Face",             Float)          = 2.0
        [Toggle(_RECEIVE_SHADOWS)]
        _ReceiveShadows ("Receive Shadows",         Float)          = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma shader_feature_local _RECEIVE_SHADOWS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);

            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo_ST;
                float  _Cutoff;
                float  _NormalOverride;
                float  _Cull;
                float  _ReceiveShadows;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                float4 shadowCoord: TEXCOORD4;
            };

            // 4x4 Bayer dither matrix for LOD crossfade
            float Dither4x4(float2 screenPos)
            {
                float2 pos = fmod(screenPos, 4.0);
                float4x4 m = float4x4(
                    -0.5,     0.5,   -0.375,  0.625,
                     0.25,  -0.75,    0.375, -0.625,
                    -0.25,   0.75,   -0.125,  0.875,
                     1.0,   -1.0,     0.125, -0.875);
                return m[int(pos.x)][int(pos.y)];
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = nrmInputs.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _Albedo);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);

                #if defined(_RECEIVE_SHADOWS)
                    OUT.shadowCoord = GetShadowCoord(posInputs);
                #else
                    OUT.shadowCoord = float4(0, 0, 0, 0);
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // LOD crossfade dither
                #ifdef LOD_FADE_CROSSFADE
                    clip(unity_LODFade.x - Dither4x4(IN.positionCS.xy));
                #endif

                half4 albedoSample = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, IN.uv);
                clip(albedoSample.a - _Cutoff);

                float3 geoNormal  = normalize(IN.normalWS);
                float3 fakeNormal = normalize(_MainLightPosition.xyz);
                float3 normal     = normalize(lerp(geoNormal, fakeNormal, _NormalOverride));

                InputData lightingInput               = (InputData)0;
                lightingInput.positionWS              = IN.positionWS;
                lightingInput.normalWS                = normal;
                lightingInput.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                lightingInput.shadowCoord             = IN.shadowCoord;
                lightingInput.fogCoord                = IN.fogFactor;
                lightingInput.vertexLighting          = half3(0, 0, 0);
                lightingInput.bakedGI                 = half3(0, 0, 0);
                lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                SurfaceData surfaceData  = (SurfaceData)0;
                surfaceData.albedo       = albedoSample.rgb;
                surfaceData.alpha        = albedoSample.a;
                surfaceData.metallic     = 0.0;
                surfaceData.smoothness   = 0.0;
                surfaceData.occlusion    = 1.0;
                surfaceData.normalTS     = half3(0, 0, 1);

                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                color.rgb   = MixFog(color.rgb, IN.fogFactor);

                return color;
            }
            ENDHLSL
        }

        // No ShadowCaster pass = does not cast shadows
    }

    FallBack "Universal Render Pipeline/Lit"
}
