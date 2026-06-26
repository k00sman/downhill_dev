Shader "Custom/CylindricalBillboard"
{
    Properties
    {
        _Albedo         ("Albedo",          2D)     = "white" {}
        _Smoothness     ("Smoothness",      Range(0,1)) = 0.2
        _Metallic       ("Metallic",        Range(0,1)) = 0.0
        _Cutoff         ("Alpha Clip Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"            = "TransparentCutout"
            "RenderPipeline"        = "UniversalPipeline"
            "Queue"                 = "AlphaTest"
            "IgnoreProjector"       = "True"
        }

        // No shadow caster pass = does not cast shadows
        // No shadow receiver keyword needed; we explicitly exclude it below

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back          // Render back face only
            ZWrite On
            Blend Off           // Opaque blend mode; alpha clipping handles transparency

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP lighting features
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog

            // Disable shadow receiving
            #pragma multi_compile _ _SHADOWS_SOFT
            // Do NOT include _MAIN_LIGHT_SHADOWS_CASCADE so shadows are never sampled

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);

            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo_ST;
                float  _Smoothness;
                float  _Metallic;
                float  _Cutoff;
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
            };
			
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

                // --- Cylindrical billboard ---
                // Object position in world space (pivot)
                float3 pivotWS = TransformObjectToWorld(float3(0, 0, 0));

                // Camera-to-pivot direction, flattened to XZ plane (cylindrical = Y locked)
                float3 camPos   = GetCameraPositionWS();
                float3 toCamera = camPos - pivotWS;
                toCamera.y      = 0.0;
                toCamera        = normalize(toCamera);

                // Build rotation axes
                float3 fwd   = toCamera;                        // Z axis faces camera
                float3 up    = float3(0, 1, 0);                 // Y stays world up
                float3 right = normalize(cross(up, fwd));       // X axis

                // Vertex offset from pivot in object space, then reproject onto billboard axes
                // Use only XY of object-space position so Y (height) is preserved
                float3 posOS   = IN.positionOS.xyz;
                float3 offsetWS = right * posOS.x + up * posOS.y;  // ignore posOS.z (card depth)

                float3 finalWS = pivotWS + offsetWS;

                // Normal: face toward camera (flat card, so just use fwd)
                float3 normalWS = fwd;

                OUT.positionCS = TransformWorldToHClip(finalWS);
                OUT.positionWS = finalWS;
                OUT.normalWS   = normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _Albedo);
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
			    #ifdef LOD_FADE_CROSSFADE
               clip(unity_LODFade.x - Dither4x4(IN.positionCS.xy));
                #endif
				
                // Sample albedo + alpha
                half4 albedoSample = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, IN.uv);

                // Alpha clip
                clip(albedoSample.a - _Cutoff);

                // --- PBR lighting ---
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS        = IN.positionWS;
                lightingInput.normalWS          = normalize(IN.normalWS);
                lightingInput.viewDirectionWS   = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                lightingInput.shadowCoord       = float4(0, 0, 0, 0); // no shadow receiving
                lightingInput.fogCoord          = IN.fogFactor;
                lightingInput.vertexLighting    = half3(0, 0, 0);
                lightingInput.bakedGI           = half3(0, 0, 0);
                lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo      = albedoSample.rgb;
                surfaceData.alpha       = albedoSample.a;
                surfaceData.metallic    = _Metallic;
                surfaceData.smoothness  = _Smoothness;
                surfaceData.occlusion   = 1.0;
                surfaceData.normalTS    = half3(0, 0, 1);

                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);

                // Apply fog
                color.rgb = MixFog(color.rgb, IN.fogFactor);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
