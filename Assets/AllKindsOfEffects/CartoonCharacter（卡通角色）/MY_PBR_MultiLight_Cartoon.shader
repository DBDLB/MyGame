Shader "MY_PBR_MultiLight_Cartoon"
{
    Properties{
    	_BaseMap("BaseMap",2D) = "white"{}
		_BaseColor("BaseColor", Color) = (1,1,1,1)
    	_MixMap("AO,Specular,",2D) = "white"{}
    	
    	[Foldout(1, 1, 1, 1)]
    	_OcclusionMapFoldout ("AO遮罩_Foldout", float) = 1
    	_OcclusionMapChannelMask("OcclusionMapChannelMask(AO遮罩) (Default R)", Vector) = (1,0,0,0)
    	[Range]_OcclusionRemapSlider("Range remap (Default 0~1)", float) = (0.0, 0.0, 0, 1)
    	_OcclusionStrength("OcclusionStrength（强度） (Default 1)", Range(0.0, 1.0)) = 1
    	
    	[Foldout(1, 1, 1, 1)]
    	_SpecularMapFoldout ("Specular_Foldout", float) = 1
    	[HDR]_SpecularColor("_SpecularColor (Default White)", Color) = (1,1,1,1)
//    	[NoScaleOffset]_SpecularMap("_SpecularMap (Use 1 channel) (White = full specular, Black = no specular) (Default White)", 2D) = "white" {}
    	_SpecularMapChannelMask("_SpecularMapChannelMask (Default B)", Vector) = (0,0,1,0)
    	[Range]_SpecularMapRemapMinMaxSlider("Range remap (Default 0~1)", float) = (0.0, 0.0, 0, 1)
    	_SpecularIntensity("_SpecularIntensity (Default 1)", Range(0,100)) = 1
    	_MultiplyBaseColorToSpecularColor("MultiplyBaseColorToSpecularColor (Default 0)", Range(0,1)) = 0
    	
    	[Foldout(1, 1, 1, 1)]
    	_Emission ("Emission_Foldout", float) = 0
    	_EmissionMap("_EmissionMap (Default White)", 2D) = "white" {}
    	_EmissionMapTilingXyOffsetZw("_EmissionMapTilingXyOffsetZw (Default 1,1,0,0)", Vector) = (1,1,0,0)
    	[HDR]_EmissionColor("_EmissionColor (Default Black)", Color) = (0,0,0,0)
    	_EmissionIntensity("_EmissionIntensity (Default 1)", Range(0,100)) = 1
    	_MultiplyBaseColorToEmissionColor("_MultiplyBaseColorToEmissionColor (Default 0)", Range(0,1)) = 0
    	
//		_SpecularColor("SpecularColor", Color) = (1,1,1,1)
//		_BaseMap("BaseMap",2D) = "white"{}
//    	[NoScaleOffset]_MaskMap("MaskMap(Metallic,AO,Smoothness)",2D)="white"{}
//    	_Metallic("Metallic",Range(0,1)) = 1
//        _Smoothness("Smoothness",Range(0,1)) = 1
//		_SpecularRange("SpecularRange", Range(0,100)) = 50
//    	[NoScaleOffset][Normal]_NormalMap("NormalMap",2D)="Bump"{}
//    	_NormalScale("NormalScale",Range(0,1))=1
    	
	    [Foldout(1, 1, 1, 1)]
    	_AlphaOverrideTexFoldout ("通过下面的纹理替换basemap alpha通道_Foldout", float) = 1
    	[Toggle(_ALPHAOVERRIDEMAP)]_UseAlphaOverrideTex("使用AlphaOverrideTex", Range( 0 , 1)) = 0
    	_AlphaOverrideTex ("AlphaOverrideTex", 2D) = "white" {}
    	_AlphaOverrideTexChannelMask ("AlphaOverrideTexChannelMask (Default G)", Vector) = (0,1,0,0)
    	_PerCharacterBaseColorTint ("PerCharacterBaseColorTint", Color) = (1,1,1,1)
        _GlobalVolumeBaseColorTintColor ("GlobalVolumeBaseColorTintColor", Color) = (1,1,1,1)
    	
    	[Foldout(1, 1, 1, 1)]
    	_AlphaTest("使用AlphaTest_Foldout", float) = 0
    	_Cutoff("不透明蒙版剪辑值", Range(0.0, 1.0)) = 0.5
    	
    	[Foldout(1, 1, 1, 1)]
    	_LightingStyleFoldout("LightingStyle_Foldout", float) = 1
    	_CelShadeMidPoint("_CelShadeMidPoint (Default 0)", Range(-1,1)) = 0
    	_CelShadeSoftness("_CelShadeSoftness (Default 0.05)", Range(0,1)) = 0.05
    	_MainLightIgnoreCelShade("_MainLightIgnoreCelShade (fake SSS) (Default 0)", Range(0,1)) = 0
    	
    	[Foldout(1, 1, 1, 1)]
    	_SelfShadowFoldout ("SelfShadow_Foldout", float) = 1
    	_SelfShadowAreaHueOffset("_SelfShadowAreaHueOffset (Default 0)", Range(-1,1)) = 0
    	_SelfShadowAreaSaturationBoost("_SelfShadowAreaSaturationBoost (Default 0.5)", Range(0,1)) = 0.5
    	_SelfShadowAreaValueMul("_SelfShadowAreaValueMul (Default 0.7)", Range(0,1)) = 0.7
    	[HDR]_SelfShadowTintColor("_SelfShadowTintColor (Default White)", Color) = (1,1,1)
    	
    	_LitToShadowTransitionAreaIntensity("LitToShadowTransitionAreaIntensity", Range(0,32)) = 1
    	
    	_LitToShadowTransitionAreaHueOffset("_LitToShadowTransitionAreaHueOffset (Default 0.01)", Range(-1,1)) = 0.01
    	_LitToShadowTransitionAreaSaturationBoost("_LitToShadowTransitionAreaSaturationBoost (Default 0.5)", Range(0,1)) = 0.5
    	_LitToShadowTransitionAreaValueMul("_LitToShadowTransitionAreaValueMul (Default 1)", Range(0,1)) = 1
    	[HDR]_LitToShadowTransitionAreaTintColor("_LitToShadowTransitionAreaTintColor (Default White)", Color) = (1,1,1)
    	
    	[HDR]_LowSaturationFallbackColor("_LowSaturationFallbackColor (Default H:222,S:25,V:50) (Default alpha as intensity = 100)", Color) = (0.3764706,0.4141177,0.5019608,1)
    	
    	[Foldout(1, 1, 1, 1)]
    	_Other ("Other_Foldout", float) = 1
    	// dither
        _DitherOpacity("_DitherOpacity", Range(0,1)) = 1
		_TestValue("Test",Range(1,20))=5.0
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
		#include "Assets/AllKindsOfEffects/CartoonCharacter（卡通角色）/HLSL/DitherFadeoutClipUtil.hlsl"
		#include "Assets/AllKindsOfEffects/CartoonCharacter（卡通角色）/HLSL/InvLerpRemapUtil.hlsl"
		#include "Assets/AllKindsOfEffects/CartoonCharacter（卡通角色）/HLSL/HSVRGBConvert.hlsl"
		

		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
		#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
		#pragma multi_compile _ _SHADOWS_SOFT
		#pragma shader_feature _ADD_LIGHTS
		#pragma shader_feature_local _ _REFLECTION_SSPR

		#pragma shader_feature_local _ALPHAOVERRIDEMAP //(can use _fragment suffix)

		CBUFFER_START(UnityPerMaterial)
			// float4 _MainTex_ST;
			float4 _BaseMap_ST;
			float4 _BaseColor;
			float4 _SpecularColor;
			float _SpecularRange;
			float _TestValue;
		    float _Smoothness;
			float _Metallic;
			float _NormalScale;
			half _Cutoff;
			float _DitherOpacity;
			float _OcclusionStrength;
			float _SpecularIntensity;
			float _MultiplyBaseColorToSpecularColor;

		    half3   _PerCharacterBaseColorTint;
			half3   _GlobalVolumeBaseColorTintColor;
			half4   _AlphaOverrideTexChannelMask;
			half4   _OcclusionMapChannelMask;
			half4   _OcclusionRemapSlider;
			half4   _SpecularMapChannelMask;
			half4   _SpecularMapRemapMinMaxSlider;

			// emission
			float4  _EmissionMapTilingXyOffsetZw;
			half4   _EmissionColor;
			half    _EmissionIntensity;
			half    _MultiplyBaseColorToEmissionColor;

			//SelfShadow
			half _LitToShadowTransitionAreaIntensity;
			half    _SelfShadowAreaHueOffset;
			half    _LitToShadowTransitionAreaHueOffset;
			half    _SelfShadowAreaSaturationBoost;
			half    _LitToShadowTransitionAreaSaturationBoost;
			half    _SelfShadowAreaValueMul;
			half    _LitToShadowTransitionAreaValueMul;
			half4   _LowSaturationFallbackColor;
			half3   _SelfShadowTintColor;
			half3   _LitToShadowTransitionAreaTintColor;

			// lighting style
			half    _CelShadeMidPoint;
			half    _CelShadeSoftness;
			half    _MainLightIgnoreCelShade;

			//去除透视效果
			float3  _HeadBonePositionWS;
			float   _PerspectiveRemovalRadius;
			float   _PerspectiveRemovalAmount;
			float   _PerspectiveRemovalStartHeight;
			float   _PerspectiveRemovalEndHeight;
		CBUFFER_END

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
			half3 additionalLightSum: TEXCOORD5;
			half4 SH_fogFactor: TEXCOORD6; 
		};

		struct PBR
		{
			Light light;
			float3 normalWS;
			float3 viewDirWS;
			float3 positionWS;
			float2 uv;
		};


		struct ToonLightingData
		{
		    float2  uv;
		    half3   normalWS;
		    float3  positionWS;
		    half3   viewDirectionWS;
		    // float   selfLinearEyeDepth;
		    // half    averageShadowAttenuation;
		    half3   SH;
		    // half    isFaceArea; // default 0, = not face, see InitializeLightingData(...)
		    // half    isSkinArea; // default 0, = not skin, see InitializeLightingData(...)
		    float2  SV_POSITIONxy;
		    half3   normalVS;
		    half3   reflectionVectorVS;
		    half    NdotV;

			half3   additionalLightSum;
		
		// #if VaryingsHasTangentWS
		//     half3x3 TBN_WS;
		//     half3   viewDirectionTS;
		// #endif
		//
		// #if NeedCalculateAdditionalLight
		//     half3   additionalLightSum;
		// #endif
		};

		struct ToonSurfaceData
		{
		    half3   albedo;
		    half    alpha;
		    half3   emission;
		    half    occlusion;
		    half3   specular;
		    // half3   normalTS;
		    // half    smoothness;
		};

			float4 DoPerspectiveRemoval(float4 originalPositionCS, float perspectiveRemovalAmount, float centerPosVSz)
			{
			    // resources:
			    // - https://zhuanlan.zhihu.com/p/268433650?utm_source=ZHShareTargetIDMore
			    // - https://zhuanlan.zhihu.com/p/332804613
			
			    // resources link's demo method
			    /*
			    float originalPositionCSZ = output.positionCS.z;
			    float4 perspectiveCorrectPosVS = mul(UNITY_MATRIX_I_P, output.positionCS);
			    perspectiveCorrectPosVS.z -= centerPosVSz;
			    perspectiveCorrectPosVS.z *= lerp(1,0.1,perspectiveCorrectUsage); // Flatten model's pos z in view space
			    perspectiveCorrectPosVS.z += centerPosVSz;    
			    output.positionCS = mul(UNITY_MATRIX_P, perspectiveCorrectPosVS);
			    output.positionCS.z = originalPositionCSZ;
			    */
			
			    // our method
			    float2 newPosCSxy = originalPositionCS.xy;
			    newPosCSxy *= abs(originalPositionCS.w); // cancel Hardware w-divide
			    newPosCSxy *= rcp(abs(centerPosVSz)); // do our flattened w-divide
			    originalPositionCS.xy = lerp(originalPositionCS.xy, newPosCSxy, perspectiveRemovalAmount); // apply 0~100% perspective removal  
			
			    return originalPositionCS;    
			}

			float4 DoPerspectiveRemoval(float4 originalPositionCS, float3 positionWS, float3 removalCenterPositionWS, float removalRadius, float removalAmount, float removalStartHeight, float removalEndHeight)
			{
			    // only do perspective removal if is perspective camera
			    // high level function contain global disable logic, to reduce code complexity of this .hlsl's user code
			    // if(_GlobalShouldDisableToonPerspectiveRemoval || unity_OrthoParams.w == 1)
			    //     return originalPositionCS;
			
			    float perspectiveRemovalAreaSphere = saturate(removalRadius - distance(positionWS,removalCenterPositionWS) / removalRadius);
			    float perspectiveRemovalAreaWorldHeight = saturate(invLerp(removalStartHeight, removalEndHeight, positionWS.y));
			    float perspectiveRemovalFinalAmount = removalAmount * perspectiveRemovalAreaSphere * perspectiveRemovalAreaWorldHeight;
			    float centerPosVSz = mul(UNITY_MATRIX_V, float4(removalCenterPositionWS,1)).z;
			
			    return DoPerspectiveRemoval(originalPositionCS, perspectiveRemovalFinalAmount, centerPosVSz);
			}
		
		ENDHLSL

		Pass
		{
			Tags{
				"LightMode"="UniversalForward"
				"RenderType"="Opaque"
			}
			
			ZWrite[_ZWrite]
			Cull[_Cull]
			Blend [_SrcBlend] [_DstBlend]

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local _ALPHATEST_ON
			#pragma shader_feature_local _EMISSION_ON

			TEXTURE2D(_BaseMap);	SAMPLER(sampler_BaseMap);
			TEXTURE2D(_AlphaOverrideTex);	SAMPLER(sampler_AlphaOverrideTex);
			TEXTURE2D(_MixMap);    SAMPLER(sampler_MixMap);

			#if _EMISSION_ON
			TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
			#endif

			half4 GetFinalBaseColor(v2f input)
			{
			    half4 color = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
			#if _ALPHAOVERRIDEMAP
			    color.a = dot(SAMPLE_TEXTURE2D(_AlphaOverrideTex,sampler_AlphaOverrideTex, input.uv),_AlphaOverrideTexChannelMask);
			#endif
			    color *= _BaseColor; // edit rgba, since _BaseColor is per material, using _BaseColor.a to control alpha should be intentional
			    color.rgb *= _PerCharacterBaseColorTint * _GlobalVolumeBaseColorTintColor; // edit rgb only, since they are not per material, edit alpha per character/globally should be not intended
			    return color;
			}

			void DoClipTestToTargetAlphaValue(half alpha) 
			{
				#if _ALPHATEST_ON
				    clip(alpha - _Cutoff);
				#endif
			}

			half GetFinalOcculsion(v2f input,half4 MixTexValue)
			{

			    half occlusionValue = dot(MixTexValue, _OcclusionMapChannelMask);
			    occlusionValue = invLerpClamp(_OcclusionRemapSlider.x, _OcclusionRemapSlider.y, occlusionValue); // should remap first,
			    occlusionValue = lerp(1, occlusionValue, _OcclusionStrength); // then apply per material and per volume fadeout.
			    return occlusionValue;
			}

			half3 GetFinalSpecularRGB(v2f input, half3 baseColor,half4 MixTexValue)
			{
			// #if _SPECULARHIGHLIGHTS
			    // half4 texValue = SAMPLE_TEXTURE2D(_SpecularMap,sampler_SpecularMap, input.uv);
			    half specularValue = dot(MixTexValue, _SpecularMapChannelMask);
			    specularValue = invLerpClamp(_SpecularMapRemapMinMaxSlider.x, _SpecularMapRemapMinMaxSlider.y, specularValue); // should remap first,
			    half3 specularResult = _SpecularColor * (specularValue * _SpecularIntensity);// then apply intensity / color
			    specularResult *= lerp(1,baseColor,_MultiplyBaseColorToSpecularColor); // let user optionally mix base color to specular color
    			// #if _SPECULARHIGHLIGHTS_TEX_TINT
    			//     specularResult *= lerp(1,tex2D(_SpecularColorTintMap, input.uv),_SpecularColorTintMapUsage);
    			// #endif
				return specularResult;
			// #else
			//     return 0; // default specular value is 0 when turn off
			// #endif
			}

			half3 GetFinalEmissionColor(v2f input, half3 baseColor)
			{
			#if _EMISSION_ON
			    float2 uv = input.uv * _EmissionMapTilingXyOffsetZw.xy + _EmissionMapTilingXyOffsetZw.zw;
			    half3 emissionResult = SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap, uv).rgb * _EmissionColor.rgb * _EmissionIntensity; // alpha is ignored
			    emissionResult *= lerp(1,baseColor,_MultiplyBaseColorToEmissionColor); // let user optionally mix base color to emission color
			    return emissionResult;
			#else
			    return 0; // default emission value is black when turn off
			#endif
			}
			ToonSurfaceData InitializeSurfaceData(v2f input)
			{
				ToonSurfaceData output;

				half4 baseColorFinal = GetFinalBaseColor(input);
				DoClipTestToTargetAlphaValue(baseColorFinal.a);
				DoDitherFadeoutClip(input.positionCS.xy, _DitherOpacity);
				half4 MixTexValue = SAMPLE_TEXTURE2D(_MixMap,sampler_MixMap, input.uv);

    			output.albedo = baseColorFinal.rgb;
    			output.alpha = baseColorFinal.a;
				// occlusion
				output.occlusion = GetFinalOcculsion(input, MixTexValue);
				// specular & roughness
				output.specular = GetFinalSpecularRGB(input, baseColorFinal.rgb,MixTexValue);
				// emission
				output.emission = GetFinalEmissionColor(input, baseColorFinal.rgb);
				return output;
			}

			ToonLightingData InitializeLightingData(v2f input, ToonSurfaceData surfaceData)
			{
			    ToonLightingData lightingData;
			    lightingData.uv = input.uv;
			    lightingData.positionWS = input.positionWS;
			    lightingData.viewDirectionWS = normalize(GetCameraPositionWS() - lightingData.positionWS);  
			
			    half3 normalWS = input.normalWS;
			    lightingData.normalWS = NormalizeNormalPerPixel(normalWS);
			    lightingData.SV_POSITIONxy = input.positionCS.xy;
			    lightingData.normalVS = mul((half3x3)UNITY_MATRIX_V, lightingData.normalWS).xyz;
			
			    lightingData.reflectionVectorVS = reflect(-lightingData.viewDirectionWS,lightingData.normalWS); // see URP Lighting.hlsl
				lightingData.SH = input.SH_fogFactor.xyz;
				lightingData.additionalLightSum = input.additionalLightSum;
			    lightingData.NdotV = saturate(dot(lightingData.normalWS,lightingData.viewDirectionWS));
			    return lightingData;
			}

			half3 ShadeGI(ToonSurfaceData surfaceData, ToonLightingData lightingData)
			{
				half indirectOcclusion = 1;
				half3 indirectLight = lightingData.SH * indirectOcclusion;
				return indirectLight * surfaceData.albedo; 
			}

			half3 CalculateLightIndependentSelfShadowAlbedoColor(ToonSurfaceData surfaceData, ToonLightingData lightingData, half finalShadowArea)
			{
				half3 rawAlbedo = surfaceData.albedo;
    			// half isFace = lightingData.isFaceArea;
    			// half isSkin = lightingData.isSkinArea;
    			float2 uv = lightingData.uv;

				half isLitToShadowTransitionArea = saturate((1-abs(finalShadowArea-0.5)*2)*_LitToShadowTransitionAreaIntensity);

    			// [hsv]
    			half HueOffset = _SelfShadowAreaHueOffset + _LitToShadowTransitionAreaHueOffset * isLitToShadowTransitionArea;
    			half SaturationBoost = _SelfShadowAreaSaturationBoost + _LitToShadowTransitionAreaSaturationBoost * isLitToShadowTransitionArea;
    			half ValueMul = _SelfShadowAreaValueMul * lerp(1,_LitToShadowTransitionAreaValueMul, isLitToShadowTransitionArea);

				half3 originalColorHSV; // for output from ApplyHSVChange(...)
				half3 result = ApplyHSVChange(rawAlbedo, HueOffset, SaturationBoost, ValueMul, originalColorHSV);

    			half3 fallbackColor = rawAlbedo * _LowSaturationFallbackColor.rgb;
    			result = lerp(fallbackColor,result, lerp(1,saturate(originalColorHSV.y * 5),_LowSaturationFallbackColor.a)); //only 0~20% saturation area affected, 0% saturation area use 100% fallback

				// [tint]
				result *= _SelfShadowTintColor;

    			// [lit to shadow area transition tint]
    			result *= lerp(1,_LitToShadowTransitionAreaTintColor, isLitToShadowTransitionArea);

				// result = lerp(result, rawAlbedo * _SkinShadowTintColor, isSkin * _OverrideBySkinShadowTintColor);

				return result;
			}

			half3 ShadeMainLight(ToonSurfaceData surfaceData, ToonLightingData lightingData, Light light)
			{
				half3 N = lightingData.normalWS;
    			half3 L = light.direction;
			
    			half3 V = lightingData.viewDirectionWS;
    			half3 H = normalize(L+V);
			
    			half NoL = dot(N,L); // no need saturate() due to smoothstep()
    			half NoV = saturate(dot(N,V));
    			half NoH = dot(N,H);
    			half VoV = saturate(dot(V,V));
			
    			// half orthographicCameraAmount = lerp(unity_OrthoParams.w,1,_PerspectiveRemovalAmount);
    			half orthographicCameraAmount = lerp(unity_OrthoParams.w,1,0);
				half smoothstepNoL = smoothstep(_CelShadeMidPoint-_CelShadeSoftness,_CelShadeMidPoint+_CelShadeSoftness, NoL);
				half selfLightAttenuation = lerp(smoothstepNoL,1, _MainLightIgnoreCelShade);
				// half selfLightAttenuation = 1;
				half depthDiffShadow = 1;
				half selfShadowMapShadow = 1; // default no self shadow map effect

				half finalShadowArea = selfLightAttenuation * depthDiffShadow * selfShadowMapShadow;
    			half3 inSelfShadowAlbedoColor = CalculateLightIndependentSelfShadowAlbedoColor(surfaceData, lightingData, finalShadowArea);
    			half3 lightColorIndependentLitColor = lerp(inSelfShadowAlbedoColor,surfaceData.albedo, finalShadowArea);

				// half3 result = min(_GlobalMainDirectionalLightMaxContribution, light.color * lightingData.averageShadowAttenuation) * lightColorIndependentLitColor * _GlobalMainDirectionalLightMultiplier;
				return lightColorIndependentLitColor;
			}

			half3 ShadeEmission(ToonSurfaceData surfaceData, ToonLightingData lightingData)
			{
			    // do nothing, just return.
			    // this function is created incase we need to edit emission's equation in the future
			    half3 emissionResult = surfaceData.emission;
			    return emissionResult;
			}

			half3 CompositeAllLightResults(half3 indirectResult, half3 mainLightResult, half3 additionalLightSumResult, half3 emissionResult, ToonSurfaceData surfaceData, ToonLightingData lightingData)
			{
			    half3 directLightResult = mainLightResult;
			// #if NeedCalculateAdditionalLight
			//     directLightResult += additionalLightSumResult;
			// #endif
			    half3 finalLightResult = max(indirectResult, directLightResult); 
			
			#if _EMISSION_ON
			    finalLightResult += emissionResult;
			#endif
			
			    return finalLightResult;
			}

			half3 ShadeAllLights(ToonSurfaceData surfaceData, ToonLightingData lightingData)
			{
				half3 indirectResult = ShadeGI(surfaceData, lightingData);
				Light mainLight = GetMainLight();
				half3 mainLightResult = ShadeMainLight(surfaceData, lightingData, mainLight);

			    half3 additionalLightResult = 0;
			// #if NeedCalculateAdditionalLight
			    // default weaker occlusion for additional light
			    half directOcclusion = lerp(1, surfaceData.occlusion, 0.5); // hardcode 50% usage
			    additionalLightResult =  surfaceData.albedo * directOcclusion;
			// #endif
				
			    half3 emissionResult = 0;
			#if _EMISSION_ON
			    emissionResult = ShadeEmission(surfaceData, lightingData);
			#endif
				
				return CompositeAllLightResults(indirectResult, mainLightResult, additionalLightResult, emissionResult, surfaceData, lightingData);
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
        		real3 Albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,pbr.uv).xyz*_BaseColor.xyz;
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

			v2f vert(a2v v)
			{
				v2f o;
				o.positionCS = TransformObjectToHClip(v.positionOS);
				o.uv = TRANSFORM_TEX(v.uv,_BaseMap);;
				float3 worldNormal = TransformObjectToWorldNormal(v.normalOS);
				o.normalWS = worldNormal;
                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(worldNormal,worldTangent) * v.tangent.w;
				o.tangentWS = worldTangent;
				o.BtangentWS = worldBinormal;
				o.positionWS = TransformObjectToWorld(v.positionOS);

				o.positionCS = DoPerspectiveRemoval(o.positionCS,o.positionWS,_HeadBonePositionWS,_PerspectiveRemovalRadius,_PerspectiveRemovalAmount, _PerspectiveRemovalStartHeight, _PerspectiveRemovalEndHeight);
				
				half3 SH = SampleSH(worldNormal) * 1 ;
				o.SH_fogFactor = half4(SH, 0);
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

				// #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
				//     half4 shadowMask = inputData.shadowMask;
				// #elif !defined (LIGHTMAP_ON)
				//     half4 shadowMask = unity_ProbesOcclusion;
				// #else
				//     half4 shadowMask = half4(1, 1, 1, 1);
				// #endif
				//
				// real4 color = 0;
				//
				// //Calculate Main Light
				// Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
				//
				// PBR mainPBR;
				// mainPBR.light = light;
				// mainPBR.normalWS = normalWS;
				// mainPBR.viewDirWS = viewDirWS;
				// mainPBR.positionWS = i.positionWS;
				// mainPBR.uv = i.uv;
				// color+= CalculateLight(mainPBR);
				// //float test = 0;
				//
				// #if _ADD_LIGHTS
				// int addLightsCount = GetAdditionalLightsCount();
				// for(int t=0; t<addLightsCount;t++)
				// {
				// 	PBR pbr;
				//
				// 	Light light0 = GetAdditionalLight(t, i.positionWS, shadowMask);
				// 	pbr.light = light0;
				// 	pbr.normalWS = normalWS;
				// 	pbr.viewDirWS = viewDirWS;
				// 	pbr.positionWS = i.positionWS;
				// 	pbr.uv = i.uv;
				//
				// 	color+=CalculateLight(pbr);
				// }
				// #endif
				

				ToonSurfaceData surfaceData = InitializeSurfaceData(i);
				ToonLightingData lightingData = InitializeLightingData(i, surfaceData);
				half3 color = ShadeAllLights(surfaceData, lightingData);

				

				half3 indirectResult = ShadeGI(surfaceData, lightingData);

				return float4(color,surfaceData.alpha);
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
            #pragma vertex vert
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

            // // -------------------------------------
            // // Universal Pipeline keywords
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            //
            // //--------------------------------------
            // // GPU Instancing
            // #pragma multi_compile_instancing
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"

   //          half3 GetViewDirectionTangentSpace(half4 tangentWS, half3 normalWS, half3 viewDirWS)
			// {
			//     // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
			//     half3 unnormalizedNormalWS = normalWS;
			//     const half renormFactor = 1.0 / length(unnormalizedNormalWS);
			//
			//     // use bitangent on the fly like in hdrp
			//     // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
			//     half crossSign = (tangentWS.w > 0.0 ? 1.0 : -1.0); // we do not need to multiple GetOddNegativeScale() here, as it is done in vertex shader
			//     half3 bitang = crossSign * cross(normalWS.xyz, tangentWS.xyz);
			//
			//     half3 WorldSpaceNormal = renormFactor * normalWS.xyz;       // we want a unit length Normal Vector node in shader graph
			//
			//     // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
			//     // This is explained in section 2.2 in "surface gradient based bump mapping framework"
			//     half3 WorldSpaceTangent = renormFactor * tangentWS.xyz;
			//     half3 WorldSpaceBiTangent = renormFactor * bitang;
			//
			//     half3x3 tangentSpaceTransform = half3x3(WorldSpaceTangent, WorldSpaceBiTangent, WorldSpaceNormal);
			//     half3 viewDirTS = mul(tangentSpaceTransform, viewDirWS);
			//
			//     return viewDirTS;
			// }
            Varyings vert(Attributes input)
			{
				// v2f o;
				// o.positionCS = TransformObjectToHClip(v.positionOS);
				//
				// o.positionWS = TransformObjectToWorld(v.positionOS);
				//
				// o.positionCS = DoPerspectiveRemoval(o.positionCS,o.positionWS,_HeadBonePositionWS,_PerspectiveRemovalRadius,_PerspectiveRemovalAmount, _PerspectiveRemovalStartHeight, _PerspectiveRemovalEndHeight);


				Varyings output = (Varyings)0;
    			UNITY_SETUP_INSTANCE_ID(input);
    			UNITY_TRANSFER_INSTANCE_ID(input, output);
    			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
			
    			#if defined(REQUIRES_UV_INTERPOLATOR)
    			    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    			#endif
    			output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
			
    			VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    			VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);
			
    			output.normalWS = half3(normalInput.normalWS);
    			#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    			    float sign = input.tangentOS.w * float(GetOddNegativeScale());
    			    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    			#endif
			
    			#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    			    output.tangentWS = tangentWS;
    			#endif
			
    			#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    			    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    			    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    			    output.viewDirTS = viewDirTS;
    			#endif

								output.positionCS = DoPerspectiveRemoval(output.positionCS,TransformObjectToWorld(input.positionOS),_HeadBonePositionWS,_PerspectiveRemovalRadius,_PerspectiveRemovalAmount, _PerspectiveRemovalStartHeight, _PerspectiveRemovalEndHeight);
    			return output;
			}
            
            ENDHLSL
        }
	} 
	CustomEditor "Scarecrow.SimpleShaderGUI"
}
