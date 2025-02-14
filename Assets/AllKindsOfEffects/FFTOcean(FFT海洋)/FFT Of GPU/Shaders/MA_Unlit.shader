Shader "Unlit/MA_Unlit"
{
    Properties
    {
        [Header(Unlit)][Space(10)]
        [SinglelineTexture(_BaseColor)][MainTexture] _BaseMap("Albedo(rgba)", 2D) = "white" {}
        [HideInInspector][MainColor] _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [Header(EdgeAlphaFalloff)][Space(10)]
        [Toggle(USE_EDGE_ALPHA_FALLOFF)]_FallOffEnable("开启底部渐变", float) = 0
        [ShowIf(_FallOffEnable)]_EdgeAlphaFadeDistance("Edge Alpha Fade Distance", Range(0,3)) = 0.5
        _LightColorIntensity("光照颜色影响强度", Range(0.0, 1.0)) = 0

        [Header(RenderingSettings)][Space(10)]
        [RenderingMode] _Mode("混合模式", Int) = 0
        [ShowIf(_ALPHATEST_ON)] _Cutoff("不透明蒙版剪辑值", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("剔除模式", Int) = 2

        [HideInInspector] _SrcBlend ("__src", Int) = 1.0
        [HideInInspector] _DstBlend ("__dst", Int) = 0.0
        [HideInInspector] _ZWrite ("__zw", Int) = 1.0





        [Header(Fog)][Space(10)]
        _FogToggle ("雾效强度", Range(0.0,1.0)) = 1.0

        
    }
    
    HLSLINCLUDE
     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
      //CBUFFER_START (UnityPerMaterial)
      	float4 _BaseMap_ST;
      	half4 _BaseColor;
      	half _Cutoff;
      	float _EdgeAlphaFadeDistance;
      	half _LightColorIntensity;
      	half _FogToggle;
     //CBUFFER_END
    ENDHLSL
    
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            
            Cull[_Cull]
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 4.5
            
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            struct appdata
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                half4 tangentOS : TANGENT;
            	half4 normalOS : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS  : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                float4 grabPassUV : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);  SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_heightMap);  SAMPLER(sampler_heightMap);


            float SampleSceneDepth(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
            }
            
            float GetDepthFade(float3 positionWS, float Distance)
            {
                float4 ScreenPosition = ComputeScreenPos(TransformWorldToHClip(positionWS));
                float depth = LinearEyeDepth(SampleSceneDepth(ScreenPosition.xy / ScreenPosition.w).r, _ZBufferParams);
                return saturate((depth - ScreenPosition.w) / Distance);
            }

            void AthenaAlphaDiscard(real alpha, real cutoff, real offset = 0.0h)
            {
                #ifdef _ALPHATEST_ON
                    clip(alpha - cutoff + offset);
                #endif
            }
            
            
            v2f vert (appdata v)
            {
                v2f o;
                float4 clipSpacePos = TransformObjectToHClip(v.positionOS);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionWS = vertexInput.positionWS;
                o.positionCS = vertexInput.positionCS;
                o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS.xyz);
                o.uv = TRANSFORM_TEX(v.texcoord, _BaseMap);
                o.grabPassUV = ComputeScreenPos(clipSpacePos);
                return o;
            }

            float4 frag (v2f i) : SV_Target0
            {
                float2 heightUV  = i.uv;
                float2 grabPassUV = i.grabPassUV.xy/i.grabPassUV.w;
                half4 baseMap = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, grabPassUV);
                half4 finalColor;
                finalColor.rgb = _BaseColor.rgb * baseMap.rgb;
                finalColor.a = _BaseColor.a * baseMap.a;

                #ifndef _ALPHATEST_ON
                float depthfade = GetDepthFade(i.positionWS, _EdgeAlphaFadeDistance);
                finalColor.a *= saturate(depthfade);
                #endif
                // sample the texture
                AthenaAlphaDiscard(finalColor.a, _Cutoff);

                Light light = GetMainLight();
                half3 color = finalColor.rgb;
                color = lerp(color,color * light.color,_LightColorIntensity);
                finalColor.rgb = color;
                
                return float4(finalColor);
            }
            ENDHLSL
        }
    }
}
