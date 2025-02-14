﻿// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

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
        _Displace ("Displace", 2D) = "black" { }
        _Normal ("Normal", 2D) = "black" { }
        _Bubbles ("Bubbles", 2D) = "black" { }
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
        Tags { "RenderType"="Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        
        Pass
        {
            Tags{
				"LightMode"="UniversalForward"
				"RenderType"="Opaque"
			}
            ZWrite On
            Cull off
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
            float4 _Displace_ST;
            
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
            TEXTURE2D(_Normal);SAMPLER(sampler_Normal);
            TEXTURE2D(_Displace);SAMPLER(sampler_Displace);
            TEXTURE2D(_Bubbles);SAMPLER(sampler_Bubbles);
            TEXTURE2D(_CausticMap);SAMPLER(sampler_ScreenTextures_linear_repeat);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);SAMPLER(sampler_CameraDepthTexture);
            
            TEXTURECUBE(_SkyboxMap);SAMPLER(sampler_SkyboxMap);
//            SamplerState sampler_LinearRepeat;
            
            
            
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
            
            
            v2f vert(appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _Displace);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float4 displcae = SAMPLE_TEXTURE2D_LOD(_Displace,sampler_LinearRepeat, float4((o.worldPos/256/0.2).xz, 0, 0),0.1 * UNITY_SPECCUBE_LOD_STEPS);
                v.vertex += float4(displcae.xyz, 0);
                o.pos = TransformObjectToHClip(v.vertex);
                o.screenPosition = ComputeScreenPos(o.pos);
                

                return o;
            }
            
            half4 frag(v2f i): SV_Target
            {
                half3 normal =  TransformObjectToWorldNormal(SAMPLE_TEXTURE2D(_Normal,sampler_Normal, i.uv).rgb);
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

                //焦散部分
                float3 caustics = saturate(Caustics(i.worldPos,depthDifference));
                
                //海洋颜色
                half4 baseMap = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenPos);
                half3 oceanDiffuse = oceanColor * light.color.rgb + (baseMap.rgb * depthDifference);
                half diffuseControl = saturate(dot(lightDir, normal));
                oceanDiffuse *= diffuseControl < 0.7 ? 0.9 : 1;
                half3 halfDir = normalize(lightDir + viewDir);
                half3 specular = light.color.rgb * _Specular.rgb * pow(max(0, dot(normal, halfDir)), _Gloss);
                
                half3 diffuse = lerp(oceanDiffuse, bubblesDiffuse, bubbles);
                
                diffuse = lerp(diffuse ,float4(caustics*10,1) + diffuse ,WaterOpacity * depthDifference);
                diffuse =lerp(diffuse,_ShoreColor,WaterShore)+ ShoreEdge;
                diffuse = lerp(diffuse,_FoamColor,foam);
                
                half3 col = ambient + lerp(diffuse, sky, fresnel) + specular ;
                
                return half4(col, 1);
            }
            ENDHLSL
            
        }
        
//        Pass
//        {
//            Name "DepthNormals"
//            Tags
//            {
//                "LightMode" = "DepthNormals"
//            }
//
//            // -------------------------------------
//            // Render State Commands
//            ZWrite On
//            Cull off
//
//            HLSLPROGRAM
//            #pragma target 2.0
//
//            // -------------------------------------
//            // Shader Stages
//            #pragma vertex vert
//            #pragma fragment frag
//
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature_local _NORMALMAP
//            #pragma shader_feature_local _PARALLAXMAP
//            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
//            #pragma shader_feature_local _ALPHATEST_ON
//            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//
//            #pragma shader_feature_local _ COMPUTE_SHADER_ON
//            #pragma shader_feature_local FISH_OFF INSTNCED_INDIRECT
//
//            // -------------------------------------
//            // Unity defined keywords
//            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
//            
//            // Includes
//            // #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
//
//            struct appdata
//            {
//                float4 vertex: POSITION;
//                float3 normalOS : NORMAL;
//                float2 uv: TEXCOORD0;
//            };
//            
//            struct v2f
//            {
//                float4 pos: SV_POSITION;
//                float2 uv: TEXCOORD0;
//                float3 worldPos: TEXCOORD1;
//                float4 screenPosition : TEXCOORD2;
//                float3 normalOS : TEXCOORD3;
//            };
//
//            CBUFFER_START(UnityPerMaterial)
//            float4 _Displace_ST;
//            CBUFFER_END
//            TEXTURE2D(_Displace);SAMPLER(sampler_Displace);
//            
//            
//            v2f vert(appdata v)
//            {
//                v2f o;
//                o.uv = TRANSFORM_TEX(v.uv, _Displace);
//                float4 displcae = SAMPLE_TEXTURE2D_LOD(_Displace,sampler_LinearRepeat, float4(o.uv, 0, 0),0.1 * UNITY_SPECCUBE_LOD_STEPS);
//                v.vertex += float4(displcae.xyz, 0);
//                o.pos = TransformObjectToHClip(v.vertex);
//                o.screenPosition = ComputeScreenPos(o.pos);
//                
//                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
//                return o;
//            }
//
//            float4 frag (v2f i) : SV_Target0
//            {
//                return float4(i.pos.zzzz);
//            }
//            ENDHLSL
//        }
    }
}
