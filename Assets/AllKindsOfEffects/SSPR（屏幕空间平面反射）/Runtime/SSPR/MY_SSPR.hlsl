#ifndef MA_REFLECTION_INCLUDE
#define MA_REFLECTION_INCLUDE

float4x4 unity_MatrixPreviousVP;
	
	#if defined(_REFLECTION_SSPR)
		TEXTURE2D(_SSPRCameraTexture);		SAMPLER(sampler_ScreenTextures_linear_clamp);
		TEXTURE2D(_MotionVector);
 
		void SampleReflection(inout float3 IndirColor,half3 positionWS,half3 normalizeWS, half smoothness)
        {
			// half3 positionWS = varyings.positionWS;
			// half occlusion = fragmentData.surfaceData.occlusion;//ao
			// half smoothness = fragmentData.surfaceData.smoothness;
			// half skyOcclusion = fragmentData.giData.shadowMask.a;//so
			
			//解决漂移，使用上一帧vp
			float4 reflectPositionCS = mul(unity_MatrixPreviousVP, float4(positionWS,1));//这里的PREV_VP是SSPR传递，如果有untiy的可去掉Feautre中计算
			
			reflectPositionCS /= reflectPositionCS.w;
			float2 temp = reflectPositionCS.xy * reflectPositionCS.xy;
			reflectPositionCS.xy = saturate(reflectPositionCS.xy * 0.5 + 0.5);
			
			#if UNITY_UV_STARTS_AT_TOP
				reflectPositionCS.y = 1 - reflectPositionCS.y;
			#endif

			//因为使用了上一帧vp,为了适配蒙皮动画，如果没有motion向量，动画将会走样
			float2 Motion = 0;
			#if defined(ENABLE_MOTION_VECTOR)
				float2 motionVector = SAMPLE_TEXTURE2D_X(_MotionVector, sampler_ScreenTextures_linear_clamp, reflectPositionCS.xy).xy;
				bool mask = (motionVector.x <0.99f);
				Motion = mask * ((motionVector - 0.5f) * rcp(0.5f - HALF_MIN));	// camera motion
			#endif
			// real mip = PerceptualRoughnessToMipmapLevel(fragmentData.brdfData.perceptualRoughness);
			real mip = PerceptualRoughnessToMipmapLevel(1);
			half4 opaqueColor = SAMPLE_TEXTURE2D_X_LOD(_SSPRCameraTexture, sampler_ScreenTextures_linear_clamp, reflectPositionCS.xy - Motion, 0).xyzw;
			// opaqueColor.xyz = opaqueColor.xyz * occlusion * skyOcclusion;
			half mask = opaqueColor.w * smoothness;
			// fragmentData.giData.bakedReflection = lerp(fragmentData.giData.bakedReflection, opaqueColor, mask);
			IndirColor = lerp(IndirColor, opaqueColor, mask);
        }
	#else
			void SampleReflection(inout float3 IndirColor,half3 positionWS,half3 normalizeWS, half smoothness){}
	#endif
#endif
