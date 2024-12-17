Shader "FlockerData"
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
        _SDF("SDF", 2D) = "white" {}

        [Header(RenderingSettings)][Space(10)]
        [RenderingMode] _Mode("混合模式", Int) = 0
        [ShowIf(_ALPHATEST_ON)] _Cutoff("不透明蒙版剪辑值", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("剔除模式", Int) = 2

        [HideInInspector] _SrcBlend ("__src", Int) = 1.0
        [HideInInspector] _DstBlend ("__dst", Int) = 0.0
        [HideInInspector] _ZWrite ("__zw", Int) = 1.0

        _TestMap("_TestMap", 2D) = "white" {}




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
         // StructuredBuffer<BoidOutputData> _Boids;
     // #endif

    float3 _Center;
    
      CBUFFER_START (UnityPerMaterial)
      	float4 _BaseMap_ST;
      	half4 _BaseColor;
      	half _Cutoff;
      	float _EdgeAlphaFadeDistance;
      	half _LightColorIntensity;
      	half _FogToggle;
        float4 _BoxMin;
        float4 _BoxMax;
        float _MaxSpeed;
        float _SDFWeight;
        float4 _IndividualData;
        float _NoiseScale;
     CBUFFER_END
    float _TimeStep;
    #define UNITY_SPECCUBE_LOD_STEPS 6
    float2 Id2Uv(int id, int size)
    {
        float2 uv;
        uv.x = ((id % size)+0.5) / (float)size;
        uv.y = ((id / size)+0.5) / (float)size;
        return uv;
    }
    uint Uv2Id(float2 uv, int size)
    {
        return uint(uv.x * size ) + uint(uv.y * size) * size;
    }

    float3 Remap(float3 original_value, float3 original_min, float3 original_max, float3 new_min, float3 new_max)
    {
        return new_min + (original_value - original_min) / (original_max - original_min) * (new_max - new_min);
    }

    float RandomRange(float min, float max, float seed)
    {
        return min + (max - min) * frac(sin(seed) * 43758.5453);
    }
    
    float3 RandomFloat3(float2 seed)
    {
        float3 rand;
        rand.x = frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
        rand.y = frac(sin(dot(seed, float2(63.7264, 10.873))) * 52342.2314);
        rand.z = frac(sin(dot(seed, float2(28.233, 58.5453))) * 33452.5453);
        return rand;
    }
    
    float3 RandomOnUnitSphere(float2 randomAngles)
    {
        float phi = randomAngles.x * 2.0 * 3.14159265359;
        float costheta = randomAngles.y * 2.0 - 1.0;
    
        float rho = sqrt(1.0 - costheta * costheta);
        float x = rho * cos(phi);
        float y = rho * sin(phi);
        float z = costheta;
    
        return float3(x, y, z);
    }

    ENDHLSL
    
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
        LOD 100
        
        Pass
        {
            Name "InitializeBoids"
            
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
            #pragma multi_compile_instancing
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
                float3 color : COLOR;
                float4 positionCS  : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;

            };

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SDF);  SAMPLER(sampler_SDF);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);  SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_heightMap);  SAMPLER(sampler_heightMap);

            uint _Size;


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
            
            float3 safeNormalize(float3 val)
            {
                if(length(val) == 0){
                    return val;
                }
                return normalize(val);
            }
            
            float3 ClampToMaxSpeed(float3 currentSpeed)
            {
                //return currentSpeed;
                float speedMagnitude = length(currentSpeed);
                if(speedMagnitude == 0){
                    return currentSpeed;
                }
                float3 speedNormalized = normalize(currentSpeed);
                if(speedMagnitude > _MaxSpeed){
                    return speedNormalized * _MaxSpeed;
                }
                else{
                    return currentSpeed;
                }
            }
            


            
            v2f vert (appdata v)
            {
                v2f o;
                    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                    o.positionWS = vertexInput.positionWS;
                    o.positionCS = vertexInput.positionCS;
                    o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w);
                    o.normalWS = TransformObjectToWorldNormal(v.normalOS.xyz);
                    o.uv = TRANSFORM_TEX(v.texcoord, _BaseMap);
                return o;
            }

            float4 frag (v2f i) : SV_Target0
            {
                float2 uv = i.uv;
                int id = Uv2Id(uv, 512);
                
                float fishRandomScale =RandomRange(_IndividualData.x, _IndividualData.y,id);
                float3 randomPosition = normalize(RandomOnUnitSphere(RandomFloat3(uv)));
                float4 position = float4(float3(randomPosition.x,randomPosition.y*(_BoxMax.y - _BoxMin.y)*0.4,randomPosition.z)+ _Center,fishRandomScale);
                position.xyz = Remap(position.xyz, _BoxMin, _BoxMax, 0, 1);
                float4 velocity = float4(normalize(RandomOnUnitSphere(RandomFloat3(uv)) * _MaxSpeed),0);
                velocity = velocity*0.5+0.5;
                // velocity = float3(1, 0, 0);
                // position*=100;
                float4 finalData = lerp(position,velocity, id%2);
                return float4(finalData);
            }
            ENDHLSL
        }
        
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
            #pragma multi_compile_instancing
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
                float3 color : COLOR;
                float4 positionCS  : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SDF);  SAMPLER(sampler_SDF);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);  SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_heightMap);  SAMPLER(sampler_heightMap);
            TEXTURE2D(_TestMap);  SAMPLER(sampler_TestMap);
            // SAMPLER(sampler_PointRepeat);

            uint _Size;


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
            
            float3 safeNormalize(float3 val)
            {
                if(length(val) == 0){
                    return val;
                }
                return normalize(val);
            }
            
            float3 ClampToMaxSpeed(float3 currentSpeed)
            {
                //return currentSpeed;
                float speedMagnitude = length(currentSpeed);
                if(speedMagnitude == 0){
                    return currentSpeed;
                }
                float3 speedNormalized = normalize(currentSpeed);
                if(speedMagnitude > _MaxSpeed){
                    return speedNormalized * _MaxSpeed;
                }
                else{
                    return currentSpeed;
                }
            }
            
            
            v2f vert (appdata v)
            {
                v2f o;
                    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                    o.positionWS = vertexInput.positionWS;
                    o.positionCS = vertexInput.positionCS;
                    o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w);
                    o.normalWS = TransformObjectToWorldNormal(v.normalOS.xyz);
                    o.uv = TRANSFORM_TEX(v.texcoord, _BaseMap);
                return o;
            }


            float4 frag (v2f i) : SV_Target0
            {
                
                int id = Uv2Id(i.uv, 512);
                uint instanceID = id/2;
                float2 uv1 = Id2Uv(instanceID*2, 512);
                float2 uv2 = Id2Uv(instanceID*2+1, 512);
                BoidOutputData bd;
                float4 position = SAMPLE_TEXTURE2D_LOD(_TestMap, sampler_PointRepeat, uv1,0);
                bd.position = position.xyz;
                // bd.position = bd.position*255;
                // bd.position/=100;
                bd.position = Remap(bd.position, 0,1, _BoxMin, _BoxMax);
                
                float4 velocity = SAMPLE_TEXTURE2D_LOD(_TestMap, sampler_PointRepeat, uv2,0);
                bd.velocity = velocity.xyz;
                // bd.velocity = bd.velocity*255;
                bd.velocity = bd.velocity*2-1;
                // bd.velocity/=1000;

                // bd.position += bd.velocity * _TimeStep*10;
            
                float3 uv = (bd.position - _BoxMin) / (_BoxMax - _BoxMin);
                    uv.x = 1.0f - uv.x;
                    uv.z = 1.0f - uv.z;
                    float4 sdfSumAndNoise = SAMPLE_TEXTURE2D_LOD(_SDF, sampler_PointRepeat, uv.xz,0);
                    float3 sdfSum = sdfSumAndNoise.xyz;
                    float sdfNoise = sdfSumAndNoise.w;
                    sdfSum.z = 1.0f - sdfSum.z;
                    sdfSum.x = 1.0f - sdfSum.x;
                    sdfSum = sdfSum * 2.0f - 1.0f;
                    sdfSum.y = 0.0f;
                    float sdfMagnitude = length(sdfSum);
                    float3 direction = reflect(bd.velocity, safeNormalize(sdfSum)) + sdfSum * 4;
                    sdfSum = safeNormalize(direction) * _MaxSpeed - bd.velocity;
                    sdfSum = ClampToMaxSpeed(sdfSum) * sdfMagnitude;
                    
                    float3 horizonSum = bd.velocity;
                    horizonSum.y = 0.0f;
                    horizonSum = safeNormalize(horizonSum) * _MaxSpeed - bd.velocity;
                    horizonSum = ClampToMaxSpeed(horizonSum);
                
                    float dft = _BoxMax.y - bd.position.y;
                    float dfb = bd.position.y - _BoxMin.y;
                    float dfl = bd.position.x - _BoxMin.x;
                    float dfr = _BoxMax.x - bd.position.x;
                    float dff = _BoxMax.z - bd.position.z;
                    float dfbe = bd.position.z - _BoxMin.z;
                
                    float3 topPushoff = float3(-_MaxSpeed, -_MaxSpeed, -_MaxSpeed) * float3(exp(-dfr * 0.5f), 0, exp(-dff * 0.5f));
                    float3 bottomPushoff = float3(_MaxSpeed, _MaxSpeed, _MaxSpeed) * float3(exp(-dfl * 0.5f),0,exp(-dfbe * 0.5f));
                    float3 surfaceSum = topPushoff + bottomPushoff;
                    surfaceSum *= _MaxSpeed;
                
                    float3 newAcclr = 0.0f;
                
                    float3 noise = RandomOnUnitSphere(RandomFloat3(i.uv));
                    noise= float3(noise.x,0,noise.z);
                
                    newAcclr =  sdfSum * _SDFWeight + horizonSum * 16.0f + surfaceSum * 5.5f + noise*4;
                    newAcclr = ClampToMaxSpeed(newAcclr);
                
                    bd.velocity += newAcclr * _TimeStep;
                    bd.velocity = ClampToMaxSpeed(bd.velocity*5 * RandomRange(0.3, 1.2f, id));
                    bd.position += bd.velocity * _TimeStep*5;
                    if(bd.position.x > _BoxMax.x){
                        bd.position.x = _BoxMin.x;
                    }
                    if(bd.position.x < _BoxMin.x){
                        bd.position.x = _BoxMax.x;
                    }
                    if(bd.position.y > _BoxMax.y){
                        bd.position.y = _BoxMin.y;
                    }
                    if(bd.position.y < _BoxMin.y){
                        bd.position.y = _BoxMax.y;
                    }
                    if(bd.position.z > _BoxMax.z){
                        bd.position.z = _BoxMin.z;
                    }
                    if(bd.position.z < _BoxMin.z){
                        bd.position.z = _BoxMax.z;
                    }
                
                // bd.velocity*=1000;
                
                bd.velocity = safeNormalize(bd.velocity);
                bd.position = Remap(bd.position, _BoxMin, _BoxMax, 0, 1);
                bd.velocity = bd.velocity*0.5+0.5;

                position.xyz = bd.position;
                velocity.xyz = bd.velocity;
                // bd.position*=100;
                float4 finalData = lerp(position,velocity, id%2);
                return float4(finalData);

                
                
                

                //return UNITY_ACCESS_INSTANCED_PROP(_Boids, instanceID, finalColor);
                return float4(bd.position, 1.0);
            }
            ENDHLSL
        }
    }
}
