Shader "MY_PBR_Glass"
{
    Properties{
		_BaseColor("BaseColor", Color) = (1,1,1,1)
		_SpecularColor("SpecularColor", Color) = (1,1,1,1)
		_MainTex("MainTex",2D) = "white"{}
    	[NoScaleOffset]_MaskMap("MaskMap(Metallic,AO,Smoothness)",2D)="white"{}
    	_Metallic("Metallic",Range(0,1)) = 1
        _Smoothness("Smoothness",Range(0,1)) = 1
//		_SpecularRange("SpecularRange", Range(0,100)) = 50
    	[NoScaleOffset][Normal]_NormalMap("NormalMap",2D)="Bump"{}
    	_NormalScale("NormalScale",Range(0,1))=1
    	
    	[Foldout(1, 1, 1, 0)]_Glass ("玻璃效果_Foldout", float) = 0
    	_PixelBlurScale("PixelBlurScale",Range(0,1))=0.5
    	_ior("折射率",Range(0,10))=1.5
    	_Thickness("厚度",Range(0,10))=0.1
    	
    	[Foldout(1, 1, 1, 0)]_Diamond ("钻石效果_Foldout", float) = 0
    	_Cubemap("Environment", CUBE) = "white" {}
    	_CubeNormal("Noraml Cubemap", CUBE) = "white" {}
    	

    	
    	[Foldout(1, 1, 0, 0)]_Other ("Other_Foldout", float) = 1
    	_TestValue("Test",Range(0,20))=5.0
		[Toggle(_ADD_LIGHTS)]_AddLights("AddLights", float)=1.0
		[Toggle(_REFLECTION_SSPR)] _EnableSSPR("开启屏幕空间反射", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", float) = 0
    	[Enum(UnityEngine.Rendering.CullMode)] _Cull("剔除模式", Int) = 2
    	[HideInInspector] _ZWrite ("__zw", Int) = 1.0
	}
	SubShader
	{
		Tags
		{
			"RenderPipeLine"="UniversalPipeline"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
		#include "Assets/AllKindsOfEffects/PBR/PbrFunction.hlsl"
		#include "Assets/AllKindsOfEffects/SSPR（屏幕空间平面反射）/Runtime/SSPR/MY_SSPR.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
		#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
		#pragma multi_compile _ _SHADOWS_SOFT
		#pragma shader_feature _ADD_LIGHTS
		#pragma shader_feature_local _ _REFLECTION_SSPR

		#pragma shader_feature_local _GLASS_ON

		CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _BaseMap_ST;
			float4 _BaseColor;
			float4 _SpecularColor;
			// float _SpecularRange;
			float _TestValue;
		    float _Smoothness;
			float _Metallic;
			float _NormalScale;
			half _Cutoff;

			//Glass
			float _PixelBlurScale;
			float _ior;
			float _Thickness;
		
			float4 _MainTex_TexelSize;
		CBUFFER_END

		TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);
		TEXTURE2D(_MaskMap);    SAMPLER(sampler_MaskMap);
		TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);
		TEXTURE2D(_MipMapOpaqueTexture);  SAMPLER(sampler_MipMapOpaqueTexture);
		TEXTURECUBE(_CubeNormal); SAMPLER(sampler_CubeNormal);
		TEXTURECUBE(_Cubemap); SAMPLER(sampler_Cubemap);

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
			float4 screenPosition : TEXCOORD5;
			float3 normal : TEXCOORD6;
			float3 viewDir : TEXCOORD7;
		};

		struct PBR
		{
			Light light;
			float3 normalWS;
			float3 viewDirWS;
			float3 positionWS;
			float2 uv;
		};

		struct RefractionModelResult
		{
			float dist;
			float3 positionWS;
			float3 rayWS;
		};
		
		#if _GLASS_ON
		half3 SampleOpaquePyramid(float2 uv, float smoothness)
		{
			smoothness = saturate(1 - (1 - smoothness) * _PixelBlurScale);
			const float sampleRanges[] = {1.0, 0.9375, 0.875, 0.75, 0.5, 0.0};
		
			int preLod = 0;
			for(int i = 4; i >= 0; --i)
			{
				preLod = i;
				if(smoothness <= sampleRanges[i])
					break;
			}
		
			half3 ref0 = SAMPLE_TEXTURE2D_LOD(_MipMapOpaqueTexture, sampler_MipMapOpaqueTexture, uv, preLod).rgb;
			half3 ref1 = SAMPLE_TEXTURE2D_LOD(_MipMapOpaqueTexture, sampler_MipMapOpaqueTexture, uv, preLod + 1).rgb;
		
			float step = (sampleRanges[preLod] - smoothness) / (sampleRanges[preLod] - sampleRanges[preLod + 1]);
		
			return lerp(ref0, ref1, step);
		}
		
		RefractionModelResult RefractionModelSphere(real3 V, float3 positionWS, real3 normalWS, real ior, real thickness)
		{
    		// Sphere shape model:
    		//  We approximate locally the shape of the object as sphere, that is tangent to the shape.
    		//  The sphere has a diameter of {thickness}
    		//  The center of the sphere is at {positionWS} - {normalWS} * {thickness} * 0.5
    		//
    		//  So the light is refracted twice: in and out of the tangent sphere
		
    		// First refraction (tangent sphere in)
    		// Refracted ray
    		real3 R1 = refract(-V, normalWS, 1.0 / ior);
    		// Center of the tangent sphere
    		real3 C = positionWS - normalWS * thickness * 0.5;
		
    		// Second refraction (tangent sphere out)
    		real NoR1 = dot(normalWS, R1);
    		// Optical depth within the sphere
    		real dist = -NoR1 * thickness;
    		// Out hit point in the tangent sphere
    		real3 P1 = positionWS + R1 * dist;
    		// Out normal
    		real3 N1 = normalize(C - P1);
    		// Out refracted ray
    		real3 R2 = refract(R1, N1, ior);
    		real N1oR2 = dot(N1, R2);
    		real VoR1 = dot(V, R1);
		
    		RefractionModelResult result;
    		result.dist = dist;
    		result.positionWS = P1;
    		result.rayWS = R2;
		
    		return result;
		}

		RefractionModelResult RefractionModelBox(real3 V, float3 positionWS, real3 normalWS, real ior, real thickness)
		{
		    // Plane shape model:
		    //  We approximate locally the shape of the object as a plane with normal {normalWS} at {positionWS}
		    //  with a thickness {thickness}
		
		    // Refracted ray
		    real3 R = refract(-V, normalWS, 1.0 / ior);
		
		    // Optical depth within the thin plane
		    real dist = thickness / max(dot(R, -normalWS), 1e-5f);
		
		    RefractionModelResult result;
		    result.dist = dist;
		    result.positionWS = positionWS + R * dist;
		    result.rayWS = -V;
		
		    return result;
		}
		#endif
		

		#define MAX_BOUNCE 5
		#define REFRACT_SPREAD float3 (0.0, 0.02, 0.05)
					#define REFRACT_INDEX float3(2.407, 2.426, 2.451)
			#define REFRACT_SPREAD float3 (0.0, 0.02, 0.05)
		#define COS_CRITICAL_ANGLE 0.91
		//通过法线CubeMap实现多次折射
		float3 MultipleRefraction(half3 normalOS, half3 viewDirOS, half ior, int maxRefractionTime, half2 refractionSpread)
		{
			float3 viewDir = normalize(viewDirOS);
			float3 normal = normalize(normalOS);
			float3 reflectDir = reflect(viewDir, normal);
			float fresnelFactor = pow(1 - abs(dot(viewDir, normal)), 2);
			float3 reflectDirW = mul(float4(reflectDir, 0.0), unity_WorldToObject);


			float4 col = SAMPLE_TEXTURECUBE(_Cubemap, sampler_Cubemap, reflectDirW) * fresnelFactor;

			float3 inDir = refract(viewDir, normal, 1.0/REFRACT_INDEX.r);
				// Direction to sample environment cubemap for different colors
				float3 inDirR, inDirG, inDirB;
				for (int bounce = 0; bounce < MAX_BOUNCE; bounce++)
				{
					// Convert normal to -1, 1 range
					half3 inN = SAMPLE_TEXTURECUBE(_CubeNormal, sampler_CubeNormal, inDir).xyz * 2.0 - 1.0;
					if (abs(dot(-inDir, inN)) > COS_CRITICAL_ANGLE)
					{
						// The more bounces we have the heavier dispersion should be
						inDirR = refract(inDir, inN, REFRACT_INDEX.r);
						inDirG = refract(inDir, inN, REFRACT_INDEX.g + bounce * REFRACT_SPREAD.g);
						inDirB = refract(inDir, inN, REFRACT_INDEX.b + bounce * REFRACT_SPREAD.b);
						break;
					}

					// We didn't manage to exit diamond in MAX_BOUNCE
					// To be able exit from diamond to air we need fake our refraction 
					// index other way we'll get float3(0,0,0) as return
					if (bounce == MAX_BOUNCE-1)
					{
						inDirR = refract(inDir, inN, 1/ REFRACT_INDEX.r);
						inDirG = refract(inDir, inN, 1/ (REFRACT_INDEX.g + bounce * REFRACT_SPREAD.g));
						inDirB = refract(inDir, inN, 1/ (REFRACT_INDEX.b + bounce * REFRACT_SPREAD.b));
						break;
					}
					inDir = reflect(inDir, inN);
				}
		    inDirR = TransformObjectToWorldDir(inDirR);
		    inDirG = TransformObjectToWorldDir(inDirG);
		    inDirB = TransformObjectToWorldDir(inDirB);
			col.r += SAMPLE_TEXTURECUBE(_Cubemap, sampler_Cubemap, inDirR).r;
			col.g += SAMPLE_TEXTURECUBE(_Cubemap, sampler_Cubemap, inDirG).g;
			col.b +=  SAMPLE_TEXTURECUBE(_Cubemap, sampler_Cubemap, inDirB).b;

			return col.rgb;
		}

		real4 CalculateLight(PBR pbr)
		{

			// float3 h = normalize(pbr.light.direction+pbr.viewDirWS);
			// float nDotL = max(0, dot(pbr.normalWS, pbr.light.direction));
			// float spec = pow(max(0, dot(h,pbr.normalWS)), _SpecularRange);
			//
			// real4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pbr.uv);
			// real4 lightColor = real4(pbr.light.color, 1.0);
			//
			// real4 diffuse = texColor * nDotL * lightColor;
			// real4 specular = spec * _SpecularColor;
			//
			// real4 color = (diffuse+specular)*pbr.light.shadowAttenuation*pbr.light.distanceAttenuation;
			
			float3 positionWS=pbr.positionWS;
        	real3 Albedo=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,pbr.uv).xyz*_BaseColor.xyz;
        	float4 Mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,pbr.uv);
        	float Metallic=lerp(0,Mask.r,_Metallic);
        	float AO=Mask.g;
        	float smoothness=lerp(0,Mask.a,_Smoothness);
        	float TEMProughness=1-smoothness;//中间粗糙度
        	float roughness=pow(TEMProughness,2);// 粗糙度
			float3 F0=lerp(0.04,Albedo,Metallic);
	
        	Light mainLight=pbr.light;
        	float3 L=normalize(mainLight.direction);
        	float3 V=SafeNormalize(_WorldSpaceCameraPos-positionWS);
        	float3 H=normalize(V+L);
        	float NdotV=max(saturate(dot(pbr.normalWS,V)),0.000001);//不取0 避免除以0的计算错误
        	float NdotL=max(saturate(dot(pbr.normalWS,L)),0.000001);
        	float HdotV=max(saturate(dot(H,V)),0.000001);
        	float NdotH=max(saturate(dot(H,pbr.normalWS)),0.000001);
        	float LdotH=max(saturate(dot(H,L)),0.000001);
			
	//直接光部分
                float D=D_Function(NdotH,roughness);
                //return D;
                float G=G_Function(NdotL,NdotV,roughness);
                //return G;
                float3 F=F_Function(LdotH,F0);
                //return float4(F,1);
                float3 BRDFSpeSection=D*G*F/(4*NdotL*NdotV);
                float3 DirectSpeColor=BRDFSpeSection*mainLight.color*NdotL*PI;
                //return float4(DirectSpeColor,1);
                //高光部分完成 后面是漫反射
                float3 KS=F;
                float3 KD=(1-KS)*(1-Metallic);
                float3 DirectDiffColor=KD*Albedo*mainLight.color*NdotL;//分母要除PI 但是积分后乘PI 就没写
                //return float4(DirectDiffColor,1);
                float3 DirectColor=DirectSpeColor+DirectDiffColor;
                //return float4(DirectColor,1);
    //间接光部分
                float3 SHcolor=SH_IndirectionDiff(pbr.normalWS)*AO;
                float3 IndirKS=IndirF_Function(NdotV,F0,roughness);
                float3 IndirKD=(1-IndirKS)*(1-Metallic);
                float3 IndirDiffColor=SHcolor*IndirKD*Albedo;
                //return float4(IndirDiffColor,1);
                //漫反射部分完成 后面是高光
                float3 IndirSpeCubeColor=IndirSpeCube(pbr.normalWS,V,roughness,AO);
                //return float4(IndirSpeCubeColor,1);
                float3 IndirSpeCubeFactor=IndirSpeFactor(roughness,smoothness,BRDFSpeSection,F0,NdotV);
                float3 IndirSpeColor=IndirSpeCubeColor*IndirSpeCubeFactor;
                //return float4(IndirSpeColor,1);
                float3 IndirColor=IndirSpeColor+IndirDiffColor;
			
				SampleReflection(IndirColor,pbr.positionWS,pbr.normalWS,smoothness);
                //return float4(IndirColor,1);
                //间接光部分计算完成
                float4 color=float4((IndirColor+DirectColor*pbr.light.shadowAttenuation*pbr.light.distanceAttenuation),1);
			return float4(color.xyz,1);
		};

		ENDHLSL

		Pass
		{
			Tags{
				"LightMode"="UniversalForward"
				"RenderType"="Opaque"
			}

			Cull off
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
				o.screenPosition = ComputeScreenPos(o.positionCS);

			
				o.normal = v.normalOS;
				float3 objectCamera = mul(unity_WorldToObject,float4( _WorldSpaceCameraPos,1));
				o.viewDir = normalize(v.positionOS - objectCamera);
				return o;
			}

			real4 frag(v2f i):SV_TARGET
			{

				//法线部分得到世界空间法线
                float4 nortex=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv.xy);
                float3 norTS=UnpackNormalScale(nortex,_NormalScale);
                norTS.z=sqrt(1-saturate(dot(norTS.xy,norTS.xy)));
                float3x3 T2W={i.tangentWS.xyz,i.BtangentWS.xyz,i.normalWS.xyz};
                T2W=transpose(T2W);
                float3 N=NormalizeNormalPerPixel(mul(T2W,norTS));
				 
				float3 normalWS = N;
				float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);

				#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
				    half4 shadowMask = inputData.shadowMask;
				#elif !defined (LIGHTMAP_ON)
				    half4 shadowMask = unity_ProbesOcclusion;
				#else
				    half4 shadowMask = half4(1, 1, 1, 1);
				#endif

				real4 color = 0;

				//Calculate Main Light
				Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));

				PBR mainPBR;
				mainPBR.light = light;
				mainPBR.normalWS = normalWS;
				mainPBR.viewDirWS = viewDirWS;
				mainPBR.positionWS = i.positionWS;
				mainPBR.uv = i.uv;
				color+= CalculateLight(mainPBR);
				//float test = 0;

				#if _ADD_LIGHTS
				int addLightsCount = GetAdditionalLightsCount();
				for(int t=0; t<addLightsCount;t++)
				{
					PBR pbr;

					Light light0 = GetAdditionalLight(t, i.positionWS, shadowMask);
					pbr.light = light0;
					pbr.normalWS = normalWS;
					pbr.viewDirWS = viewDirWS;
					pbr.positionWS = i.positionWS;
					pbr.uv = i.uv;

					color+=CalculateLight(pbr);
				}
				#endif

				#if _GLASS_ON
				//玻璃效果
				RefractionModelResult refractionResult = RefractionModelSphere(viewDirWS, i.positionWS, normalWS, _ior, _Thickness);
				float4 screenPos = TransformWorldToHClip(refractionResult.positionWS);
				float2 screenUV = screenPos.xy / screenPos.w * 0.5 + 0.5;
				screenUV.y = 1 - screenUV.y;
				color.rgb = lerp(SampleOpaquePyramid(screenUV, _Smoothness),color.rgb, _BaseColor.a);
				#endif
				
				return float4(MultipleRefraction( i.normal, i.viewDir, _ior, 5, float2(2.407, 2.426)),1);
			}
			ENDHLSL
		}
//		pass{
//
//			Tags{
//				"LightMode"="ShadowCaster"
//			}
//			HLSLPROGRAM
//			#pragma vertex vertShadow
//			#pragma fragment fragShadow
//
//			half3 _LightDirection;
//
//			v2f vertShadow(a2v v)
//			{
//				v2f o;
//				o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
//				o.normalWS = TransformObjectToWorldNormal(v.normalOS.xyz);
//				o.uv = v.uv;
//
//				float3 positionWS = ApplyShadowBias(o.positionWS, o.normalWS, _LightDirection);
//				o.positionCS = TransformWorldToHClip(positionWS);
//
//				#if UNITY_REVERSED_Z
//				    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
//				#else
//				    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
//				#endif
//				return o;
//			}
//			half4 fragShadow(v2f i):SV_TARGET
//			{
//				return 0;
//			}
//
//			ENDHLSL
//		}
		
//Pass
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
//            Cull[_Cull]
//
//            HLSLPROGRAM
//            #pragma target 2.0
//
//            // -------------------------------------
//            // Shader Stages
//            #pragma vertex DepthNormalsVertex
//            #pragma fragment DepthNormalsFragment
//
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature_local _NORMALMAP
//            #pragma shader_feature_local _PARALLAXMAP
//            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
//            #pragma shader_feature_local _ALPHATEST_ON
//            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//
//            // -------------------------------------
//            // Unity defined keywords
//            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
//
//            // // -------------------------------------
//            // // Universal Pipeline keywords
//            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
//            //
//            // //--------------------------------------
//            // // GPU Instancing
//            // #pragma multi_compile_instancing
//            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
//
//            // -------------------------------------
//            // Includes
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
//            ENDHLSL
//        }
	} 
	CustomEditor "Scarecrow.SimpleShaderGUI"
}
