Shader "MY_PBR_MultiLight"
{
    Properties{
		_BaseColor("BaseColor", Color) = (1,1,1,1)
		_SpecularColor("SpecularColor", Color) = (1,1,1,1)
		_MainTex("MainTex",2D) = "white"{}
    	[NoScaleOffset]_MaskMap("MaskMap(Metallic,AO,Smoothness)",2D)="white"{}
    	_Metallic("Metallic",Range(0,1)) = 1
        _Smoothness("Smoothness",Range(0,1)) = 1
		_SpecularRange("SpecularRange", Range(0,100)) = 50
    	[NoScaleOffset][Normal]_NormalMap("NormalMap",2D)="Bump"{}
    	_NormalScale("NormalScale",Range(0,1))=1
		_TestValue("Test",Range(1,20))=5.0
		[Toggle(_ADD_LIGHTS)]_AddLights("AddLights", float)=1.0
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
		#include "PbrFunction.hlsl"

		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
		#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
		#pragma multi_compile _ _SHADOWS_SOFT
		#pragma shader_feature _ADD_LIGHTS

		CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _BaseColor;
			float4 _SpecularColor;
			float _SpecularRange;
			float _TestValue;
		    float _Smoothness;
			float _Metallic;
			float _NormalScale;
		CBUFFER_END

		TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);
		TEXTURE2D(_MaskMap);    SAMPLER(sampler_MaskMap);
		TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);

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

		struct PBR
		{
			Light light;
			float3 normalWS;
			float3 viewDirWS;
			float3 positionWS;
			float2 uv;
		};

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
                //return float4(IndirColor,1);
                //间接光部分计算完成
                float4 color=float4((IndirColor+DirectColor*pbr.light.shadowAttenuation*pbr.light.distanceAttenuation),1);
			return color;
		};

		ENDHLSL

		Pass
		{
			Tags{
				"LightMode"="UniversalForward"
				"RenderType"="Opaque"
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
		
		Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
	} 
}
