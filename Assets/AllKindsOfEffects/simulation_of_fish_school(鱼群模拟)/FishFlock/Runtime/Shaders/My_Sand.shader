Shader "Unlit/My_Sand"
{
    Properties{
		_HighlightColor ("Highlight Color", color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _Noise ("Noise", 2D) = "gray" {}
        _Normal ("Normal Map", 2D) = "bump" {}
        _NormalIntensity ("Normal Intensity", Range(0, 1)) = 1.0
        _Color ("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _MetallicTex ("Metallic Texture", 2D) = "white" {} //default to white, so we can multiply thsi with the metallic value
        [Gamma]_Metallic ("Metallic", Range(0, 1)) = 1.0
        _SmoothnessTex ("Smoothness (Roughness) Texture", 2D) = "white" {} // same as above
        _Smoothness ("Smoothness Multiplier)", Range(0, 1)) = 1.0
        _BRDF_Lut("BRDF Lookup", 2D) = "white" {}
        [Toggle]_RoughnessWorflow("Use Roughness Workflow", Float) = 0.0
        [Toggle]_AlphaIsSmoothness("Alpha is Smoothness (Roughness)", Float) = 0.0
		[Toggle(_ADD_LIGHTS)]_AddLights("AddLights", float)=1.0
	}
	SubShader
	{
		Tags
		{
			"RenderPipeLine"="UniversalRenderPipeline"
			"RenderType"="Opaque"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
		#include "GraterNPBR.cginc"

		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
		#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
		#pragma multi_compile _ _SHADOWS_SOFT
		#pragma shader_feature _ADD_LIGHTS

		CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Noise_ST;
            half _Smoothness;
            half _Metallic;
            half4 _Color;
            half4 _HighlightColor;
            half _NormalIntensity;
            int _RoughnessWorflow;
            int _AlphaIsSmoothness;
		CBUFFER_END

		TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);
		TEXTURE2D(_Normal);	SAMPLER(sampler_Normal);
		TEXTURE2D(_Noise);	SAMPLER(sampler_Noise);
		TEXTURE2D(_MetallicTex);	SAMPLER(sampler_MetallicTex);
		TEXTURE2D(_SmoothnessTex);	SAMPLER(sampler_SmoothnessTex);
		TEXTURE2D(_BRDF_Lut);	SAMPLER(sampler_BRDF_Lut);

		struct a2v{
			float4 positionOS: POSITION;
			float2 uv: TEXCOORD0;
			float3 normalOS: NORMAL;
			float4 tangent:TANGENT;
		};

		struct v2f{
			float4 positionCS: SV_POSITION;
			float2 uv: TEXCOORD0;
			float3 normalWS: TEXCOORD1;
			float3 positionWS: TEXCOORD2;
			float3 BtangentWS: TEXCOORD3;
			float3 tangentWS: TEXCOORD4;
		};

		inline half decode_roughness(half value){
                half roughness;
                if(_RoughnessWorflow){
                    roughness = 1.0 - ((1.0 - value) * (_Smoothness));
                }
                else{
                    roughness = 1.0 - value * _Smoothness;
                }
                roughness *= roughness;
                roughness = lerp(0.02, 0.98, roughness);
                return roughness;
            }

            inline half3 map_normal(float2 uv, half3 normal, half3 tangent, half3 bitangent){
                half3 tangentSpaceNormal = UnpackNormal(SAMPLE_TEXTURE2D(_Normal,sampler_Normal, uv));
                tangentSpaceNormal = lerp(half3(0, 0, 1.0), tangentSpaceNormal, _NormalIntensity);
                float3x3 tbn = float3x3(
                    tangent.x, bitangent.x, normal.x,
                    tangent.y, bitangent.y, normal.y,
                    tangent.z, bitangent.z, normal.z
                );
                return normalize(mul(tbn, tangentSpaceNormal));
            }

            inline void sample_smoothness_metallic(float2 uv, out half smoothness, out half metallic){

                
                half4 metallic_val = SAMPLE_TEXTURE2D(_MetallicTex,sampler_MetallicTex, uv) * _Metallic;
                metallic = metallic_val.r;
                if(_AlphaIsSmoothness){
                    smoothness = metallic_val.a;
                }
                else{
                    smoothness = SAMPLE_TEXTURE2D(_SmoothnessTex,sampler_SmoothnessTex, uv);
                }
            }

			//间接光漫反射 球谐函数 光照探针
         	real3 SH_IndirectionDiff(float3 normalWS)
         	{
         	    real4 SHCoefficients[7];
         	    SHCoefficients[0]=unity_SHAr;
         	    SHCoefficients[1]=unity_SHAg;
         	    SHCoefficients[2]=unity_SHAb;
         	    SHCoefficients[3]=unity_SHBr;
         	    SHCoefficients[4]=unity_SHBg;
         	    SHCoefficients[5]=unity_SHBb;
         	    SHCoefficients[6]=unity_SHC;
         	    float3 Color=SampleSH9(SHCoefficients,normalWS);
         	    return max(0,Color);
         	}


            inline float3 indirect_lighting(float3 albedo, float3 normal, float nDotV, float3 reflDir, half3 f0, half roughness, half metallic){
                nDotV = lerp(0, 0.99, nDotV);
                
                ////////////////// INDIRECT IRRADIANCE ///////////////////////////////
                half3 indirectColor = SH_IndirectionDiff(normal);
                float3 f = dfg_f(nDotV, f0, roughness);
                float kd = (1 - f) * (1 - metallic);

                float3 indirectDiffuse = indirectColor * kd * albedo;
                
                ///////////////// INDIRECT REFLECTION ///////////////////////////////
                float2 environmentBrdf = SAMPLE_TEXTURE2D(_BRDF_Lut,sampler_BRDF_Lut, float2(nDotV, roughness)).xy;
                //return float3(environmentBrdf, 0);
                float lod = get_lod_from_roughness(roughness);
                half4 rgbm = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,samplerunity_SpecCube0, reflDir, lod);
                half3 indirectSpecular = DecodeHDREnvironment(rgbm, unity_SpecCube0_HDR);
                
                indirectSpecular = indirectSpecular * (f * environmentBrdf.x + environmentBrdf.y);
                return indirectDiffuse + indirectSpecular;
            }

            inline float3 direct_lighting(half3 albedo, half nDotV, half nDotL, half nDotH, half vDotH, half roughness, half metallic, half f0){
                float d = dfg_d(nDotH, roughness);
                float3 f = dfg_f_roughless(vDotH, f0, roughness);
                float g = dfg_g(nDotV, nDotL, roughness);

                float3 ks = f;
                float3 kd = (1.0 - f) * (1.0 - metallic);
                float3 diffuse = kd * albedo;
                half ks_denom = 4 * nDotV * nDotL;
                ks_denom = max(ks_denom, 0.001);
                float3 reflection = d * f * g / ks_denom;
                return diffuse + reflection * PI;
            }

			static const float3 random_vector = float3(1.334f, 2.241f, 3.919f);
            static const float two_pi = 6.28;

			float random_from_pos(float3 pos){
                return frac(dot(pos, random_vector) * 383.8438);
            }

			float3 random_normal_from_pos(float3 pos){
                half r1 = random_from_pos(pos);
                half r2 = random_from_pos(pos + random_vector);
                half oneminusr1 = sqrt(1 - r1 * r1);
                
                return float3(
                    oneminusr1 * cos(two_pi * r2),
                    oneminusr1 * sin(two_pi * r2),
                    r1
                );
            }
            
            float3 random_normal_from_noise(float r1, float r2){
                half oneminusr1 = sqrt(1 - r1 * r1);
                
                return float3(
                    oneminusr1 * cos(two_pi * r2),
                    oneminusr1 * sin(two_pi * r2),
                    r1
                );
            }
		
			// struct PBR
			// {
			// 	Light light;
			// 	float3 normalWS;
			// 	float3 customNormalWS;
			// 	float3 viewDirWS;
			// 	float3 positionWS;
			// 	float2 uv;
			// };

		real4 CalculateLight(v2f i,Light light)
		{
				///////////// SAMPLING TEXTURES ////////////////
                half4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw) * _Color;
                half smoothness, metallic;
                sample_smoothness_metallic(i.uv, smoothness, metallic);
                //since everything is in gamma space...
                //we should probably convert the color to gamma space too...

                //extract highlight
                half r1 = SAMPLE_TEXTURE2D(_Noise,sampler_Noise, (i.uv* _Noise_ST.xy + _Noise_ST.zw));
                half r2 = SAMPLE_TEXTURE2D(_Noise,sampler_Noise, (i.uv* _Noise_ST.xy + _Noise_ST.zw)  * 1.3f);
                half r3 = SAMPLE_TEXTURE2D(_Noise,sampler_Noise, (i.uv* _Noise_ST.xy + _Noise_ST.zw) * 0.1  + _Time.rr * 0.2);

                float3 randomNormal = random_normal_from_noise(r1, r2);
                half3 normal = normalize(i.normalWS);
                half3 tangent = normalize(i.tangentWS);
                half3 bitangent = normalize(i.BtangentWS);
                float3x3 tbn = float3x3(
                    tangent.x, bitangent.x, normal.x,
                    tangent.y, bitangent.y, normal.y,
                    tangent.z, bitangent.z, normal.z
                );
                float3 wsRandomNormal = normalize(mul(tbn, randomNormal));
            
			
                //return float4(randomNormal, 1.0f);
				float3 viewDirWS = (_WorldSpaceCameraPos.xyz - i.positionWS);
                half3 halfDir = normalize(viewDirWS + light.direction);
               
                //return float4(randomNormal, 1.0f);


                ///////////// BASE COMPUTATIONS /////////////////
                half3 worldNormal = map_normal(i.uv, normal, tangent, bitangent);//normalize(i.normal);

                half baseNormal = saturate(dot(worldNormal, halfDir));
                baseNormal = pow(baseNormal, 32);
                half highlight = saturate(dot(normalize(viewDirWS), wsRandomNormal));
                highlight = pow(highlight, 16) * 0.8 * r3;

				half test = highlight;
            

                worldNormal = normal;

                //return baseNormal * highlight;


                half3 viewDir = normalize(viewDirWS);
                half3 lightDir = normalize(light.direction);
                half3 halfVector = normalize(viewDir + lightDir);
                
                half roughness = decode_roughness(smoothness);
                metallic = lerp(0.02, 0.98, metallic);

                half nDotV = saturate(dot(worldNormal, viewDir));
                half nDotL = saturate(dot(worldNormal, lightDir));
                half nDotH = saturate(dot(worldNormal, halfVector));
                half vDotH = saturate(dot(viewDir, halfVector));

                half3 f0 = 0.04;
                f0 = lerp(f0, col, metallic);

                ///////////// UNITY OPERATIONS ///////////////////
                half lighting = light.shadowAttenuation*light.distanceAttenuation;
                lighting = min(nDotL, lighting);

                //welp!
                //lighting = smoothstep(0.5, 0.52, lighting);
                
                highlight *= lighting;

                //return dfg_d(nDotH, roughness);
                //return float4(dfg_f(nDotV, f0, roughness), 1.0);
                //return dfg_g(nDotV, lightDir, roughness);
                
                ///////////// COMPOSITION ////////////////////////
                float3 direct = direct_lighting(col, nDotV, nDotL, nDotH, vDotH, roughness, metallic, f0);
                float3 indirect = indirect_lighting(col, worldNormal, nDotV, reflect(-viewDir, worldNormal), f0, roughness, metallic);
                float3 ambient = 0.03 * col;
                direct += ambient;
                float3 lightAmount =light.color.rgb * lighting;

                float4 composite = float4(direct * lightAmount + indirect, 1.0);
                composite += lerp(highlight * _HighlightColor, 1.0, highlight * 0.3);
			return float4(composite);
		};

		ENDHLSL

		Pass
		{
			Tags{
				"LightMode"="UniversalForward"
			}

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			v2f vert(a2v v)
			{
				v2f o;
				o.positionCS = TransformObjectToHClip(v.positionOS);
				o.uv = v.uv;
				float3 worldNormal = TransformObjectToWorldNormal(v.normalOS);
				o.normalWS = worldNormal;
                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(worldNormal,worldTangent) * v.tangent.w;
				o.tangentWS = worldTangent;
				o.BtangentWS = worldBinormal;
				o.positionWS = TransformObjectToWorld(v.positionOS);
				return o;
			}

			real4 frag(v2f i):SV_TARGET
			{

				//法线部分得到世界空间法线
    //             float4 nortex=SAMPLE_TEXTURE2D(_Normal,sampler_Normal,i.uv.xy);
    //             float3 norTS=UnpackNormalScale(nortex,_NormalScale);
    //             norTS.z=sqrt(1-saturate(dot(norTS.xy,norTS.xy)));
    //             float3x3 T2W={i.tangentWS.xyz,i.BtangentWS.xyz,i.normalWS.xyz};
    //             T2W=transpose(T2W);
    //             float3 N=NormalizeNormalPerPixel(mul(T2W,norTS));
				//
				// float3 normalWS = N;
				// float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
    
				#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
				    half4 shadowMask = inputData.shadowMask;
				#elif !defined (LIGHTMAP_ON)
				    half4 shadowMask = unity_ProbesOcclusion;
				#else
				    half4 shadowMask = half4(1, 1, 1, 1);
				#endif
    
				real4 color = 0;

				//Calculate Main Light
				//Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));

				Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
				color+= CalculateLight(i,light);
				//float test = 0;

				#if _ADD_LIGHTS
				int addLightsCount = GetAdditionalLightsCount();
				for(int t=0; t<addLightsCount;t++)
				{
					Light light0 = GetAdditionalLight(t, i.positionWS, shadowMask);
					color+=CalculateLight(i,light0);
				}
				#endif

				return color;
			}
			ENDHLSL
		}
		pass{

			Tags{
				"LightMode"="ShadowCaster"
			}
			HLSLPROGRAM
			#pragma vertex vertShadow
			#pragma fragment fragShadow

			half3 _LightDirection;

			v2f vertShadow(a2v v)
			{
				v2f o;
				o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS.xyz);
				o.uv = v.uv;

				float3 positionWS = ApplyShadowBias(o.positionWS, o.normalWS, _LightDirection);
				o.positionCS = TransformWorldToHClip(positionWS);

				#if UNITY_REVERSED_Z
				    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
				    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif
				return o;
			}
			half4 fragShadow(v2f i):SV_TARGET
			{
				return 0;
			}

			ENDHLSL
		}
	} 
}
