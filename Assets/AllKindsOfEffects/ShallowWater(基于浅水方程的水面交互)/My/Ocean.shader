// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Scarecrow/Ocean"
{
    Properties
    {
        _OceanColorShallow ("Ocean Color Shallow", Color) = (1, 1, 1, 1)
        _OceanColorDeep ("Ocean Color Deep", Color) = (1, 1, 1, 1)
        _DeepRange("深度距离",Range(0.01,10)) = 1
        _BubblesColor ("Bubbles Color", Color) = (1, 1, 1, 1)
        _Specular ("Specular", Color) = (1, 1, 1, 1)
        _Gloss ("Gloss", Range(8.0, 256)) = 20
        _FresnelScale ("Fresnel Scale", Range(0, 1)) = 0.5
//        _Displace ("Displace", 2D) = "black" { }
//        _Normal ("Normal", 2D) = "black" { }
//        _Bubbles ("Bubbles", 2D) = "black" { }
        _SkyboxMap("Skybox", Cube) = "white" {}
        
        //焦散
        [Space(10)]
        [Header(Caustic)]
        _CausticMap("焦散图", 2D) = "black" {}
        _CausticsSize("焦散大小",float) = 1
        _CausticsSpeed("焦散速度", Float) = 0.2
        _CausticsOffset("焦散偏移", Float) = 0.5
        _CausticsBlendDistance("焦散混合距离", Float) = 1
        _CausticsIntensity("焦散强度", Float) = 1
        
        //岸边
        [Space(10)]
        [Header(Shore)]
        _ShoreRange("岸边范围", Float) = 1
        _ShoreColor("岸边颜色",Color) = (1.0,1.0,1.0,1.0)
        _ShoreEdgeWidth("岸边宽度",Range(0,1)) = 0.2
        _ShoreEdgeIntensity("岸边边缘强度",Range(0,1)) = 0.2
        
        //泡沫
        [Space(10)]
        [Header(Foam)]
        [HDR] _FoamColor("泡沫颜色",Color) = (1.0,1.0,1.0,1.0)
        _FoamRange("泡沫范围", Float) = 1.5
        _FoamBlend("泡沫混合",Range(0,1)) = 0.2
        _FoamWidth("泡沫宽度", Float) = 0.3
        _FoamFrequency("泡沫频率", Float) = 20
        _FoamSpeed("泡沫速度", Float) = -1.8
        _FoamNoiseSize("泡沫Noise强度", vector) = (12,35,0,0)
        _FoamDissolve("泡沫溶解", Float) = 1.7
        _FoamIntensity("泡沫强度", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 worldPos: TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
            half _DeepRange;
            
            half4 _OceanColorShallow;
            half4 _OceanColorDeep;
            half4 _BubblesColor;
            half4 _Specular;
            float _Gloss;
            half _FresnelScale;
            // float4 _Displace_ST;
            
            float _CausticsSize;
            float _CausticsSpeed;
            float _CausticsOffset;
            float _CausticsBlendDistance;
            float _CausticsIntensity;

            float _ShoreRange;
            float _ShoreEdgeWidth;
            float4 _ShoreColor;
            float _ShoreEdgeIntensity;

            float4 _FoamColor;
            float _FoamRange;
            float _FoamBlend;
            float _FoamWidth;
            float _FoamFrequency;
            float _FoamSpeed;
            float4 _FoamNoiseSize;
            float _FoamDissolve;
            float _FoamIntensity;
            
            CBUFFER_END
            // TEXTURE2D(_Normal);SAMPLER(sampler_Normal);
            // TEXTURE2D(_Displace);SAMPLER(sampler_Displace);
            TEXTURE2D(_Bubbles);SAMPLER(sampler_Bubbles);
            TEXTURE2D(_CausticMap);SAMPLER(sampler_ScreenTextures_linear_repeat);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);SAMPLER(sampler_CameraDepthTexture);
            
            TEXTURECUBE(_SkyboxMap);SAMPLER(sampler_SkyboxMap);
            // SamplerState sampler_LinearRepeat;
            
            
            
            // inline half3 SamplerReflectProbe(UNITY_ARGS_TEXCUBE(tex), half3 refDir, half roughness, half4 hdr)
            // {
            //     roughness = roughness * (1.7 - 0.7 * roughness);
            //     half mip = roughness * 6;
            //     //对反射探头进行采样
            //     //UNITY_SAMPLE_TEXCUBE_LOD定义在HLSLSupport.cginc，用来区别平台
            //     half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, refDir, mip);
            //     //采样后的结果包含HDR,所以我们需要将结果转换到RGB
            //     //定义在UnityCG.cginc
            //     return DecodeHDR(rgbm, hdr);
            // }

            float2 CausticUVs(float2 rawUV, float2 offset, float sizeScale, float timeScale)
            {
                //anim
                float2 uv = rawUV * _CausticsSize * sizeScale + float2(_Time.y, _Time.x) * timeScale;
                return uv + offset * 0.25;
            }

            half3 Caustics(float3 worldPos, float depth)
            {
                float2 causticUV1 = CausticUVs(worldPos.xz, 0, 1, 0.1 * _CausticsSpeed);
                float2 causticUV2 = CausticUVs(worldPos.xz, 0, 0.8, -0.1 * _CausticsSpeed);
                half upperMask = saturate(-worldPos.y + _CausticsOffset);
                half lowerMask = saturate((worldPos.y - _CausticsOffset) / _CausticsBlendDistance + _CausticsBlendDistance);
                float3 caustics1 = SAMPLE_TEXTURE2D(_CausticMap, sampler_ScreenTextures_linear_repeat, causticUV1).rgb;
                float3 caustics2 = SAMPLE_TEXTURE2D(_CausticMap, sampler_ScreenTextures_linear_repeat, causticUV2).rgb;
                float3 caustics = min(caustics1, caustics2);
                caustics *= _CausticsIntensity;
                caustics *= 2 * saturate(0.5 * atan(5 * (depth - 0.5)) + 0.5); // 反正切函数调整岸边的焦散硬边
            
                caustics *= min(upperMask, lowerMask) * 2;
                return caustics;
            }

            float2 GradientNoiseDir( float2 x )
		    {
		    	const float2 k = float2( 0.3183099, 0.3678794 );
		    	x = x * k + k.yx;
		    	return -1.0 + 2.0 * frac( 16.0 * k * frac( x.x * x.y * ( x.x + x.y ) ) );
		    }

            float GradientNoise( float2 UV, float Scale )
		    {
		    	float2 p = UV * Scale;
		    	float2 i = floor( p );
		    	float2 f = frac( p );
		    	float2 u = f * f * ( 3.0 - 2.0 * f );
		    	return lerp( lerp( dot( GradientNoiseDir( i + float2( 0.0, 0.0 ) ), f - float2( 0.0, 0.0 ) ),
		    			dot( GradientNoiseDir( i + float2( 1.0, 0.0 ) ), f - float2( 1.0, 0.0 ) ), u.x ),
		    			lerp( dot( GradientNoiseDir( i + float2( 0.0, 1.0 ) ), f - float2( 0.0, 1.0 ) ),
		    			dot( GradientNoiseDir( i + float2( 1.0, 1.0 ) ), f - float2( 1.0, 1.0 ) ), u.x ), u.y );
		    }

            // float3 ReconstructViewPos(float2 uv, float depth, float2 p11_22, float2 p13_31)
            // {
            //    // #if defined(_ORTHOGRAPHIC)
            //    //     float3 viewPos = float3(((uv.xy * 2.0 - 1.0 - p13_31) * p11_22), depth);
            //    // #else
            //        float3 viewPos = float3(depth * ((uv.xy * 2.0 - 1.0 - p13_31) * p11_22), depth);
            //    // #endif
            //    return viewPos;
            // }
            
            
            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv;
                // float4 displcae = SAMPLE_TEXTURE2D_LOD(_Displace,sampler_LinearRepeat, float4(o.uv, 0, 0),0.1 * UNITY_SPECCUBE_LOD_STEPS);
                // v.vertex += float4(displcae.xyz, 0);
                o.pos = TransformObjectToHClip(v.vertex);
                o.screenPosition = ComputeScreenPos(o.pos);
                
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            uniform StructuredBuffer<float> _ShallowWaterBuffer;
            float4 _ShallowWaterParams;
            int _ShallowWaterSize;
            half4 frag(v2f i): SV_Target
            {
                //获取水波高度
                float2 ShallowWaterUV = (i.worldPos.xz - _ShallowWaterParams.xy) * _ShallowWaterParams.w * float2(1, -1) + float2(0.5, 0.5);
                ShallowWaterUV = saturate(ShallowWaterUV);
                float inShowBound = ShallowWaterUV.x > 0 && ShallowWaterUV.y > 0 && ShallowWaterUV.x < 1 && ShallowWaterUV.y < 1;
                float inArea = inShowBound ? 1 : 0;
                int uvXInt = floor(ShallowWaterUV.x * _ShallowWaterSize);
                int uvYInt = floor(ShallowWaterUV.y * _ShallowWaterSize);
                int index = uvYInt * _ShallowWaterSize + uvXInt;
                float heightValue = -_ShallowWaterBuffer[index] * inArea;
                // heightValue = -heightValue;
                //return heightValue;
                //转法线
                //float3 ShallowWaterNormal = float3(ddx(heightValue), 1, ddy(heightValue));

                // 获取邻近点的水波高度，来手动计算高度差
                int indexXPlus1 = uvYInt * _ShallowWaterSize + min(uvXInt + 1, _ShallowWaterSize - 1);
                int indexXMinus1 = uvYInt * _ShallowWaterSize + max(uvXInt - 1, 0);
                int indexYPlus1 = min(uvYInt + 1, _ShallowWaterSize - 1) * _ShallowWaterSize + uvXInt;
                int indexYMinus1 = max(uvYInt - 1, 0) * _ShallowWaterSize + uvXInt;
                
                float heightRight = _ShallowWaterBuffer[indexXPlus1] * inArea;
                float heightLeft = _ShallowWaterBuffer[indexXMinus1] * inArea;
                float heightUp = _ShallowWaterBuffer[indexYPlus1] * inArea;
                float heightDown = _ShallowWaterBuffer[indexYMinus1] * inArea;
                
                // 通过手动计算的邻近高度差分来得到法线
                float3 ShallowWaterNormal;
                ShallowWaterNormal.x = heightLeft - heightRight; // x方向的梯度
                ShallowWaterNormal.z = heightDown - heightUp;    // z方向的梯度
                ShallowWaterNormal.y = 1.0;  // y方向的权重，可以适当调节
                ShallowWaterNormal = normalize(ShallowWaterNormal); // 归一化法线
                
                half3 normal =  ShallowWaterNormal;
                //half3 normal = float3(0,1,0);
                half bubbles = SAMPLE_TEXTURE2D(_Bubbles, sampler_Bubbles, i.uv).r;

                Light light = GetMainLight();
                half3 lightDir = light.direction;
                half3 viewDir = normalize(GetWorldSpaceViewDir(i.worldPos));
                half3 reflectDir = reflect(viewDir, normal);               
                // reflectDir *= sign(reflectDir.y);
                
                //获取深度
                float2 screenPos = i.screenPosition.xy / i.screenPosition.w;
                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenPos).r;
                float depthValue = LinearEyeDepth(depth, _ZBufferParams);
                float waterDepth = i.screenPosition.w;
                float depthDifference = min(1,exp(-(depthValue - waterDepth)/_DeepRange));
                depthDifference = max(0,depthDifference - heightValue*0.5);


                
                //采样反射探头
                half3 sky = SAMPLE_TEXTURECUBE_LOD(_SkyboxMap, sampler_SkyboxMap, -reflectDir,
                                                   0.1 * UNITY_SPECCUBE_LOD_STEPS);
                
                //菲涅尔
                half fresnel = saturate(_FresnelScale + (1 - _FresnelScale) * pow(1 - dot(normal, viewDir), 5));
                // fresnel = pow(fresnel, 2);
                // fresnel = fresnel < 0.6 ? 0.05 : 0.7;
                
                half facing = saturate(dot(viewDir, normal));
                // facing = facing < 0.225 ? 0 : 1;
                half3 oceanColor = lerp(lerp(_OceanColorShallow, _OceanColorDeep, facing),_OceanColorShallow,depthDifference);
                
                float3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w)*0.1;


                //岸边部分
                float WaterShore = saturate((depthDifference)/_ShoreRange);
                float ShoreEdge = smoothstep(1-_ShoreEdgeWidth,1,WaterShore) * _ShoreEdgeIntensity;

                //泡沫部分
                float foamSize = saturate(depthDifference/_FoamRange);
                float foamMask = smoothstep(_FoamBlend,1,foamSize + 0.1);
                float foamNoise = (1-foamSize) + sin((1-foamSize) * _FoamFrequency + _FoamSpeed * _Time.y) + GradientNoise(i.uv * _FoamNoiseSize.xy * _FoamNoiseSize.w,1);
                foamNoise = foamNoise - _FoamDissolve;
                float foam = saturate(step((1-foamSize)-_FoamWidth,foamNoise) * foamMask * _FoamIntensity);
                
                //泡沫颜色
                half3 bubblesDiffuse = _BubblesColor.rbg * light.color.rgb * saturate(dot(lightDir, normal));

                //WaterOpacity
                float4 WaterOpacity = pow(1-fresnel,2.5);

                //焦散部分z
                float3 caustics = saturate(Caustics(i.worldPos,saturate(depthDifference+_CausticsBlendDistance)));
                
                //海洋颜色
                half4 baseMap = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenPos);
                // half3 oceanDiffuse = oceanColor * light.color.rgb + (baseMap.rgb * depthDifference);
                half3 oceanDiffuse = oceanColor * light.color.rgb;
                half diffuseControl = saturate(dot(lightDir, normal));
                oceanDiffuse *= diffuseControl < 0.7 ? 0.9 : 1;
                half3 halfDir = normalize(lightDir + viewDir);
                half3 specular = light.color.rgb * _Specular.rgb * pow(max(0, dot(normal, halfDir)), _Gloss);
                
                half3 diffuse = lerp(oceanDiffuse, bubblesDiffuse, bubbles);
                
                diffuse = lerp(diffuse ,float4(caustics*10,1) + diffuse ,WaterOpacity * depthDifference);
                diffuse =lerp(diffuse,_ShoreColor,WaterShore)+ ShoreEdge;
                diffuse = lerp(diffuse,_FoamColor,foam);
                
                half3 col = ambient + lerp(diffuse, sky, fresnel) + specular ;
                col = lerp(col, baseMap.rgb , depthDifference);
                
                return half4(col, 1);
            }
            ENDHLSL
            
        }
    }
}
