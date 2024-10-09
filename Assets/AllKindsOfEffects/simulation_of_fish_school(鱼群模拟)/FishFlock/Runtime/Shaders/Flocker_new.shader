Shader "Flocker_new"
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
        
        _TestMap("_TestMap", 2D) = "white" {}

        [HideInInspector] _SrcBlend ("__src", Int) = 1.0
        [HideInInspector] _DstBlend ("__dst", Int) = 0.0
        [HideInInspector] _ZWrite ("__zw", Int) = 1.0





        [Header(Fog)][Space(10)]
        _FogToggle ("雾效强度", Range(0.0,1.0)) = 1.0

        
    }
    
    HLSLINCLUDE
     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    
     struct BoidOutputData {
         float3 position;
         float3 velocity;
         float3 param3;
     };
    
     static const float3 colors[] = {
         float3(0, 0, 0),
         float3(0, 1, 1),
         float3(1, 1, 0),
         float3(1, 0, 1),
         float3(1, 1, 1)
     };

     // #if defined(INSTNCED_INDIRECT)
     // uint instanceID;
     // #endif
    
     // #if defined(USE_STRUCTURED_BUFFER)
         StructuredBuffer<BoidOutputData> _Boids;
     // #endif
    
      CBUFFER_START (UnityPerMaterial)
      	float4 _BaseMap_ST;
      	half4 _BaseColor;
      	half _Cutoff;
      	float _EdgeAlphaFadeDistance;
      	half _LightColorIntensity;
      	half _FogToggle;
        float4 _BoxMin;
        float4 _BoxMax;
        float _FishtailFrequency;
        float _FishtailAmplitude;
        float _FishtailSpeed;
     CBUFFER_END

    float2 Id2Uv(int id, int size)
    {
        float2 uv;
        uv.x = ((id % size)+0.5) / (float)size;
        uv.y = ((id / size)+0.5) / (float)size;
        return uv;
    }
    
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
            #pragma shader_feature_local _ COMPUTE_SHADER_ON
            #pragma shader_feature_local FISH_OFF INSTNCED_INDIRECT
            #pragma multi_compile_instancing
            struct appdata
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                half4 tangentOS : TANGENT;
            	half4 normalOS : NORMAL;
                uint instanceID: SV_InstanceID;
                float4 vertexColor : COLOR;
                // UNITY_VERTEX_INPUT_INSTANCE_ID  //GPU Instancing
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 color : COLOR;
                float4 positionCS  : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID  //GPU Instancing
            };

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);  SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_heightMap);  SAMPLER(sampler_heightMap);
            TEXTURE2D(_TestMap);  SAMPLER(sampler_TestMap);
            // SAMPLER(sampler_PointRepeat);


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
            
            float3 Remap(float3 original_value, float3 original_min, float3 original_max, float3 new_min, float3 new_max)
            {
                return new_min + (original_value - original_min) / (original_max - original_min) * (new_max - new_min);
            }
            
            
            v2f vert (appdata v)
            {
                v2f o;
                #if defined (INSTNCED_INDIRECT)
                #if defined (COMPUTE_SHADER_ON)
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                uint instanceID = v.instanceID;
                BoidOutputData bd = _Boids[instanceID];
                unity_ObjectToWorld = 0.0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(bd.position, 1.0f);
                unity_ObjectToWorld._m00_m11_m22 = bd.param3.y;
                float3 up = float3(0, 1, 0);
                float3 forward = normalize(bd.velocity);
                float3 right = normalize(cross(up, forward));
                float3 up2 = cross(forward, right);
                float3x3 rot = float3x3(right, up2, forward);
                v.positionOS.z += sin((v.positionOS.x * 1.1 + _Time.b * length(normalize(bd.velocity)) * _FishtailSpeed + instanceID / 1024)*_FishtailFrequency) * _FishtailAmplitude* v.vertexColor.r;
                float3 centerOffset = v.positionOS.xyz;
                float3 rotatedLocal = forward * centerOffset.x + up2 * centerOffset.y + right * centerOffset.z;
                float3 rotatedNormal = forward * v.normalOS.x + up2  * v.normalOS.y + right * v.normalOS.z;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(rotatedLocal);
                o.positionWS = vertexInput.positionWS;
                o.positionCS = vertexInput.positionCS;
                o.normalWS = TransformObjectToWorldNormal(rotatedNormal);
                o.uv = TRANSFORM_TEX(v.texcoord, _BaseMap);
                #else
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                uint instanceID = v.instanceID;
                float2 uv = Id2Uv(instanceID*2, 512);
                float2 uv1 = Id2Uv(instanceID*2+1, 512);
                BoidOutputData bd;
                float4 position = SAMPLE_TEXTURE2D_LOD(_TestMap, sampler_PointRepeat, uv,0);
                bd.position= position.xyz;
                bd.position = Remap(bd.position, 0,1, _BoxMin, _BoxMax);
                bd.velocity= SAMPLE_TEXTURE2D_LOD(_TestMap, sampler_PointRepeat, uv1,0);
                bd.velocity = bd.velocity*2-1;
                unity_ObjectToWorld = 0.0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(bd.position, 1.0f);
                unity_ObjectToWorld._m00_m11_m22 = position.w;
                float3 up = float3(0, 1, 0);
                float3 forward = normalize(bd.velocity);
                float3 right = normalize(cross(up, forward));
                float3 up2 = cross(forward, right);
                float3x3 rot = float3x3(right, up2, forward);
                v.positionOS.z += sin((v.positionOS.x * 1.1 + _Time.b * length(normalize(bd.velocity)) * _FishtailSpeed + instanceID / 1024)*_FishtailFrequency) * _FishtailAmplitude* v.vertexColor.r;
                float3 centerOffset = v.positionOS.xyz;
                float3 rotatedLocal = forward * centerOffset.x + up2 * centerOffset.y + right * centerOffset.z;
                float3 rotatedNormal = forward * v.normalOS.x + up2  * v.normalOS.y + right * v.normalOS.z;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(rotatedLocal);
                o.positionWS = vertexInput.positionWS;
                o.positionCS = vertexInput.positionCS;
                o.normalWS = TransformObjectToWorldNormal(rotatedNormal);
                o.uv = TRANSFORM_TEX(v.texcoord, _BaseMap);
                #endif
            #else
            o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            #endif
                        return o;
            }

            float4 frag (v2f i) : SV_Target0
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float2 heightUV  = i.uv;
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, heightUV);
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

                // return UNITY_ACCESS_INSTANCED_PROP(_Boids, instanceID, finalColor);
                return float4(finalColor);
            }
            ENDHLSL
        }
    }
}
