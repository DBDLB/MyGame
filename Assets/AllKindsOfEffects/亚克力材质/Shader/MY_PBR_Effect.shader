Shader "MY_PBR_Effect"
{
    Properties{
    	[Tex(_MainColor)]_MainTex ("Main Tex", 2D) = "white" { }
//        [HideInInspector]_MainColor ("Main Color", Color) = (1, 1, 1, 1)
    	_BaseColor("BaseColor", Color) = (1,1,1,1)
		_SpecularColor("SpecularColor", Color) = (1,1,1,1)
//		_MainTex("MainTex",2D) = "white"{}
    	[NoScaleOffset]_MaskMap("MaskMap(Metallic,AO,Smoothness)",2D)="white"{}
    	_Metallic("Metallic",Range(0,1)) = 1
        _Smoothness("Smoothness",Range(0,1)) = 1
		_SpecularRange("SpecularRange", Range(0,100)) = 50
    	[NoScaleOffset][Normal]_NormalMap("NormalMap",2D)="Bump"{}
    	_NormalScale("NormalScale",Range(0,1))=1
		_TestValue("Test",Range(1,20))=5.0
		[Toggle(_ADD_LIGHTS)]_AddLights("AddLights", float)=1.0
		
        [Foldout(1, 1, 1, 1)]
    	_AcrylicFoldout1 ("亚克力板效果（混合模式要改成Alpha）_Foldout", float) = 1
    	[Toggle(_ACRYLIC_BOARD_ON)]_UseAcrylic("使用亚克力", Range( 0 , 1)) = 0
		_BaseSmoothness ("贴图处光滑度", Range(0, 1)) = 0
        _heightScale ("亚克力板厚度", Range(0, 1)) = 0
        _FBAlpha("背后投影透明度", Range(0, 1)) = 0.25
        _AcrylicColor("亚克力板颜色", Color) = (1.0, 1.0, 1.0, 1.0)
	    _AcrylicIridescenceMask("镭射遮罩", 2D) = "white" {}
        _AcrylicIridescenceAlpha ("镭射整体影响值", range(0,1)) = 0.1
        _AcrylicIridescenceHueScaler("色带重复度 H Tillling", range(0,10)) = 0.1
        _AcrylicIridescenceHueOffset("色带偏移 H Offset", range(0,1)) = 0.1
        _AcrylicIridescenceSaturateMin("色带饱和度最小值 S Min", range(0,2)) = 0.1
        _AcrylicIridescenceSaturateMax("色带饱和度最大值 S Max", range(0,2)) = 0.1
        _AcrylicIridescenceBrightness("色带明度 V Scale", range(0,10)) = 0.1
        _AcrylicIridescenceTint("镭射偏色", Color) = (1.0, 1.0, 1.0, 0.1)
        [Foldout(2, 1, 1, 1)]
        _UseGlossyControl("油光遮罩 _Foldout", float) = 0
        _CubeMap("CubeMap", cube) = ""{}
        _GlossyScale("亚克力板油光强度", Range(0, 5)) = 0
//        _ScanLine("油光遮罩", 2D) = "white" {}
//        _ScanLineRotateAngle("油光遮罩旋转角度", Range(0, 360)) = 0
        
    	
    	[Foldout(1, 1, 0, 0)]_Other ("Other_Foldout", float) = 1
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
			"RenderType"="Opaque"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
		#include "Assets/AllKindsOfEffects/PBR/PbrFunction.hlsl"

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
			float4 _BaseMap_ST;

			//亚克力
			half _UseAcrylic;
			float _BaseSmoothness;
			float _heightScale;
			float _FBAlpha;
			half4 _AcrylicColor;
			half _AcrylicIridescenceAlpha;
			half _AcrylicIridescenceHueScaler;
			half _AcrylicIridescenceHueOffset;
			half _AcrylicIridescenceSaturateMin;
			half _AcrylicIridescenceSaturateMax;
			half _AcrylicIridescenceBrightness;
			half4 _AcrylicIridescenceTint;
			// half _UseMatCap;
			// half _MatCapScale;
			// half _MatCapRotateAngle;
			half _UseGlossyControl;
			half _GlossyScale;
			// half _ScanLineRotateAngle;
		CBUFFER_END

		TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);
		TEXTURE2D(_MaskMap);    SAMPLER(sampler_MaskMap);
		TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);

		TEXTURE2D(_AcrylicIridescenceMask); 
		// TEXTURE2D(_MatCap);                 
		// TEXTURE2D(_ScanLine);               
		TEXTURECUBE(_CubeMap);
		SAMPLER(sampler_CubeMap);

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
			float3 viewDirTS;
			float3 positionWS;
			float2 uv;
		};

		float2 ParallaxMapping(float2 texCoords, float3 viewDir,float _height)
    	{ 
    	    //half3 height = 0.5;
    	    float2 p = viewDir.xy / viewDir.z * (_height * 0.5);
    	    return texCoords - p;
    	}

		float2 RotateUV(float angle, float2 uv)
    	{
    	
    	    float2 pivot = float2(0.5, 0.5);
    	    float cosAngle = cos(angle * PI / 180);
    	    float sinAngle = sin(angle * PI / 180);
    	    float2x2 rot = float2x2(cosAngle, -sinAngle, sinAngle, cosAngle);
    	    //Rotate
    	    uv -= pivot;
    	    uv = mul(rot, uv) + pivot;
    	    return uv;
    	}

    	half ConvertRgbToGrayscale(half3 rgb)
    	{
    	    return 0.2989f * rgb.r + 0.5870f * rgb.g + 0.1140f * rgb.b;
    	}

		real4 CalculateLight(PBR pbr)
		{
			float3 positionWS=pbr.positionWS;
			Light mainLight=pbr.light;
			float3 L=normalize(mainLight.direction);
        	float3 V=SafeNormalize(_WorldSpaceCameraPos-positionWS);
        	float3 H=normalize(V+L);
        	float NdotV=max(saturate(dot(pbr.normalWS,V)),0.000001);//不取0 避免除以0的计算错误
        	float NdotL=max(saturate(dot(pbr.normalWS,L)),0.000001);
        	float HdotV=max(saturate(dot(H,V)),0.000001);
        	float NdotH=max(saturate(dot(H,pbr.normalWS)),0.000001);
        	float LdotH=max(saturate(dot(H,L)),0.000001);
			
			float4 albedoAlpha = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,pbr.uv)*_BaseColor;
			
			#ifdef _ACRYLIC_BOARD_ON
            half3 viewDirWS = SafeNormalize(pbr.viewDirWS.xyz);
            half3 normalWS = pbr.normalWS;
            half3 viewDirTS = pbr.viewDirTS;
            float iridescenceMask = SAMPLE_TEXTURE2D(_AcrylicIridescenceMask, sampler_LinearRepeat, pbr.uv).r;
            float ndotv = dot(normalWS, viewDirWS);
            // half ndotl = fragmentData.ndotml;
            float3 halfDir = normalize(mainLight.direction + viewDirWS);
            float ndoth = dot(normalWS, halfDir);
            float h = frac(ndoth + ndotv) * _AcrylicIridescenceHueScaler + _AcrylicIridescenceHueOffset;
            float s = lerp(_AcrylicIridescenceSaturateMin, _AcrylicIridescenceSaturateMax, 1 - ndotv);
            float v = _AcrylicIridescenceBrightness;
            float3 iridescenceColor = HsvToRgb(float3(h, s, v)) * _AcrylicIridescenceTint.rgb;
            iridescenceColor= iridescenceColor * _AcrylicIridescenceAlpha;
            float2 heightUV = pbr.uv ;
            heightUV = ParallaxMapping(heightUV,viewDirTS,_heightScale);

            half4 baseMap_B  = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,heightUV)*_BaseColor * (NdotV);
            // float2 ScanLineUV = pbr.uv;
            // ScanLineUV = RotateUV(_ScanLineRotateAngle, ScanLineUV);
            // half4 ScanLineMask  = SAMPLE_TEXTURE2D(_ScanLine, sampler_LinearRepeat, ScanLineUV);
            albedoAlpha = lerp(albedoAlpha, baseMap_B * _FBAlpha, 1-albedoAlpha.a) ;
            albedoAlpha = lerp( lerp(_AcrylicColor,half4(iridescenceColor,_AcrylicIridescenceTint.a) , iridescenceMask), albedoAlpha, albedoAlpha.a);
            albedoAlpha = saturate(albedoAlpha);
			#endif
			
        	// real3 Albedo=albedoAlpha.rgb;
        	float4 Mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,pbr.uv);
        	float Metallic=lerp(0,Mask.r,_Metallic);
        	float AO=Mask.g;
        	float smoothness=lerp(0,Mask.a,_Smoothness);
			
			#ifdef _ACRYLIC_BOARD_ON
			smoothness = smoothness * max((1-albedoAlpha.a),_BaseSmoothness);
			#endif
			
        	float TEMProughness=1-smoothness;//中间粗糙度
        	float roughness=pow(TEMProughness,2);// 粗糙度
			float3 F0=lerp(0.04,albedoAlpha.rgb,Metallic);
			

			
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
                float3 DirectDiffColor=KD*albedoAlpha.rgb*mainLight.color*NdotL;//分母要除PI 但是积分后乘PI 就没写
                //return float4(DirectDiffColor,1);
                float3 DirectColor=DirectSpeColor+DirectDiffColor;
                //return float4(DirectColor,1);
    //间接光部分
                float3 SHcolor=SH_IndirectionDiff(pbr.normalWS)*AO;
                float3 IndirKS=IndirF_Function(NdotV,F0,roughness);
                float3 IndirKD=(1-IndirKS)*(1-Metallic);
                float3 IndirDiffColor=SHcolor*IndirKD*albedoAlpha.rgb;
                //return float4(IndirDiffColor,1);
                //漫反射部分完成 后面是高光
				float3 IndirSpeCubeColor = float3(0,0,0);
				#ifdef _ACRYLIC_BOARD_ON
				float3 reflectDir = reflect(-pbr.viewDirWS, pbr.normalWS);
				IndirSpeCubeColor = IndirSpeCube(pbr.normalWS,V,roughness,AO);
				IndirSpeCubeColor = lerp(IndirSpeCubeColor,SAMPLE_TEXTURECUBE(_CubeMap, sampler_CubeMap, reflectDir).xyz,_UseGlossyControl);
				IndirSpeCubeColor*=_GlossyScale;
				#else
                IndirSpeCubeColor=IndirSpeCube(pbr.normalWS,V,roughness,AO);
				#endif
			
                //return float4(IndirSpeCubeColor,1);
                float3 IndirSpeCubeFactor=IndirSpeFactor(roughness,smoothness,BRDFSpeSection,F0,NdotV);
                float3 IndirSpeColor=IndirSpeCubeColor*IndirSpeCubeFactor;
                //return float4(IndirSpeColor,1);
                float3 IndirColor=IndirSpeColor+IndirDiffColor;
                //return float4(IndirColor,1);
                //间接光部分计算完成
                float4 color=float4((IndirColor+DirectColor*pbr.light.shadowAttenuation*pbr.light.distanceAttenuation),albedoAlpha.a);
			
			return color;
		};

		ENDHLSL

		Pass
		{
			Tags{
				"LightMode"="UniversalForward"
			}
			ZWrite[_ZWrite]
			Cull[_Cull]
			Blend [_SrcBlend] [_DstBlend]

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature_local _ _ACRYLIC_BOARD_ON

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
				half3 viewDirTS = mul(T2W, viewDirWS);

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

				float NdotV = saturate(dot(normalWS, viewDirWS));
				PBR mainPBR;
				mainPBR.light = light;
				mainPBR.normalWS = normalWS;
				mainPBR.viewDirWS = viewDirWS;
				mainPBR.viewDirTS = viewDirTS;
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
					pbr.viewDirTS = viewDirTS;
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
			ZWrite[_ZWrite]
			Cull[_Cull]
			Blend [_SrcBlend] [_DstBlend]
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
	CustomEditor "Scarecrow.SimpleShaderGUI"
}
