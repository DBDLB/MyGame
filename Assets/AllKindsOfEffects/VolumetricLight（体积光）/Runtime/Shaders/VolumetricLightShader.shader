Shader "Athena/FrustumMeshVolumetricLightShadow" 
{
    Properties {
        
	    _LightColor("灯光颜色", Color) = (1, 1, 1, 1)
        _LightIntensity("灯光强度", Range(0.1, 200)) = 45
        
        _AbsorptionRatio ("Absorption Ratio", Range(0, 1)) = 0.01
        _HGFactor ("HG Phase Factor", Range(-1, 1)) = 0.45
        _TransmittanceExtinction ("Transmittance Extinction", Range(0.01, 10)) = 0.25
        _IncomingLoss ("Extinction遮挡强度", range(0, 1)) = 1
        _OutScatteringLoss ("_OutScatteringLoss", range(0, 1)) = 0
        _DistanceFalloffCoe ("光强度距离衰减Lerp", range(0, 1)) = 1
    	_Corner("Corner", range(0, 1)) = 0.4
    	_Outset("Outset", range(0, 1)) = 0.8
    	_Smooth("Smooth", range(0, 1)) = 0.7
        
        [HideInInspector] _LightPosition("_LightPosition", Vector) = (0, 0, 0, 0.5) 
        [HideInInspector] _BoundaryPlanes_0("_BoundaryPlanes_0", Vector) = (0, 1, 0, 0) 
        [HideInInspector] _BoundaryPlanes_1("_BoundaryPlanes_1", Vector) = (1, 0, 0, -0.5) 
        [HideInInspector] _BoundaryPlanes_2("_BoundaryPlanes_2", Vector) = (0, 0, 1, -0.5) 
        [HideInInspector] _BoundaryPlanes_3("_BoundaryPlanes_3", Vector) = (-1, 0, 0, -0.5) 
        [HideInInspector] _BoundaryPlanes_4("_BoundaryPlanes_4", Vector) = (0, -1, 0, -1) 
        [HideInInspector] _BoundaryPlanes_5("_BoundaryPlanes_5", Vector) = (1, 1, 1, 1) 
    	
    	[HideInInspector] _ShadowVMatrix_0("_ShadowVMatrix_0", Vector) = (1, 1, 1, 1) 
    	[HideInInspector] _ShadowVMatrix_1("_ShadowVMatrix_1", Vector) = (1, 1, 1, 1) 
    	[HideInInspector] _ShadowVMatrix_2("_ShadowVMatrix_2", Vector) = (1, 1, 1, 1) 
    	[HideInInspector] _ShadowVMatrix_3("_ShadowVMatrix_3", Vector) = (1, 1, 1, 1) 
        
    	_VolumetricLightDepth("Volumetirc Light Shadow", 2D) = "black"{}
        [HideInInspector] _MainTex("Main Tex", 2D) = "grey"{}
    }
    SubShader {
        Tags{"RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _BaseColor;
        CBUFFER_END

        #define PHI (1.61803398874989484820459 * 00000.1)
		#define PI00 (3.14159265358979323846264 * 00000.1)
		#define SQ2 (1.41421356237309504880169 * 10000.0)
		#define E (2.71828182846)
		#define BIAS_X (1.31)
		#define BIAS_Y (1.17)
		#define BIAS_Z (1.57)
        real Luminance(real3 linearRgb)
        {
            return dot(linearRgb, real3(0.2126729, 0.7151522, 0.0721750));
        }
        inline float gold_noise(float2 pos, float seed)
		{
		    return frac(tan(distance(pos * (PHI + seed), float2(PHI, PI00))) * SQ2) * 2 - 1;
		}

        #pragma multi_compile _ _VOLUMETRIC_LIGHT_DEPTHTEX_ENABLE
        ENDHLSL

        Pass {
            Name "ForwardLit"
            Tags{"LightMode" = "FrustumVolumetricLight(Shadow)"} 

            Blend One OneMinusSrcAlpha, One SrcAlpha
            ZClip True 
            ZTest Always
            Cull Front 
            ZWrite Off
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertFrustum
            #pragma fragment FragFrustum
            #pragma multi_compile _ _VOLUMETRIC_LIGHT_NOISE_ENABLE
            #pragma multi_compile _ _VOLUMETRIC_LIGHT_SHADOW_ENABLE

            #include "FrumstumVolumetricLightCore.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "ScreenBlur"
            Tags{"LightMode" = "NULL"}
            ZTest Always
            ZWrite Off
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
			#include "FastMathLab.hlsl"
            #include "Fullscreen.hlsl"
            
            TEXTURE2D_X(_MainTex); SAMPLER(sampler_MainTex);
            // To do Tri ?
            TEXTURE2D_X(_BlueNoiseTexUniformTri); SAMPLER(sampler_BlueNoiseTexUniformTri);

            // depth
			TEXTURE2D_X_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
            
            

            CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_TexelSize;
            float4 _BlueNoiseTexUniformTri_TexelSize;
			CBUFFER_END
            
            #define M_TAU 6.283185307
            #define MAX_FILTER_SIZE 3.1
            //static const float M_TAU = 6.283185307;
			//static const float MAX_FILTER_SIZE = 3.1;

            float randf(float2 pos, float seed)
            {
                return gold_noise(pos, seed) *.5 + .5;
            }
            float min4(float4 v)
            {
            	return min(v.x, min(v.y, min(v.z, v.w)));
            }

            float max4(float4 v)
            {
            	return max(v.x, max(v.y, max(v.z, v.w)));
            }
            
            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = input.uv.xy;

            	//note: R is uniform, GBA is TPDF
				float2 uv_bn = screenUV * _ScreenParams.xy * _BlueNoiseTexUniformTri_TexelSize.xy;
				float4 rnd = SAMPLE_TEXTURE2D_LOD(_BlueNoiseTexUniformTri, sampler_BlueNoiseTexUniformTri, uv_bn, 0);
            	
				//note: disc jitter
				float s, c;
				sincos ( rnd.x * M_TAU, s, c);
				float2 rot = MAX_FILTER_SIZE * float2(c, s);
				float ofsrnd = 0.25 * rnd.y;

				//note: fastSqrtNR0 from https://github.com/michaldrobot/ShaderFastLibs
				float4 ofsDist = float4( fastSqrtNR0(     ofsrnd),
				                         fastSqrtNR0(0.25+ofsrnd),
				                         fastSqrtNR0(0.50+ofsrnd),
				                         fastSqrtNR0(0.75+ofsrnd) );

                float2 ofs0 = ofsDist[0] * float2( rot.x,   rot.y);
				float2 ofs1 = ofsDist[1] * float2(-rot.y,   rot.x);
				float2 ofs2 = ofsDist[2] * float2(-rot.x,  -rot.y);
				float2 ofs3 = ofsDist[3] * float2( rot.y,  -rot.x);

            	//note: texel centers, [0;_VolumetricTexSize] (half resolution)
            	float2 uv_px = (screenUV * (_MainTex_TexelSize.zw - 1.0f) + 0.5f) * _MainTex_TexelSize.xy;
            	
				float4 uv01_px = uv_px.xyxy + float4(ofs0 * _MainTex_TexelSize.xy, ofs1 * _MainTex_TexelSize.xy);
				float4 uv23_px = uv_px.xyxy + float4(ofs2 * _MainTex_TexelSize.xy, ofs3 * _MainTex_TexelSize.xy);

            	float4 fogsample0 = SAMPLE_TEXTURE2D_X( _MainTex, sampler_MainTex, uv01_px.xy );
				float4 fogsample1 = SAMPLE_TEXTURE2D_X( _MainTex, sampler_MainTex, uv01_px.zw );
				float4 fogsample2 = SAMPLE_TEXTURE2D_X( _MainTex, sampler_MainTex, uv23_px.xy );
				float4 fogsample3 = SAMPLE_TEXTURE2D_X( _MainTex, sampler_MainTex, uv23_px.zw );
				float4 col = 0;

            	
            	#if _VOLUMETRIC_LIGHT_DEPTHTEX_ENABLE
            		float d0 = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture,  uv01_px.xy).r, _ZBufferParams);
            		float d1 = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture,  uv01_px.zw).r, _ZBufferParams);
            		float d2 = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture,  uv23_px.xy).r, _ZBufferParams);
            		float d3 = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture,  uv23_px.zw).r, _ZBufferParams);
	
            		//note: function to pack/unpack floats from bytes is built into Unity
					float4 d = float4(d0, d1, d2, d3);
            		
            		//note: edge-detection
					float mind = min4( d );
					float maxd = max4( d );
					float diffd = maxd-mind;
					float avg = dot(d, float4(0.25f, 0.25f, 0.25f, 0.25f) );
					bool d_edge = (diffd/avg) < 0.1;
					
					
	
            		//note: only necessary to sample full-resolution depth if volumetric samples were on a depth-edge
					if ( d_edge )
					{
					    col += fogsample0;
						col += fogsample1;
						col += fogsample2;
						col += fogsample3;
						col *= 0.25f;
					}
					else
					{
					    float2 bguv = screenUV;
					    float bgdepth = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r, _ZBufferParams);
	
					    //note: depth weighing from https://www.ppsloan.org/publications/ProxyPG.pdf#page=5
					    float4 dd = abs( d - bgdepth);
					    float4 w = 1.0 / (dd + 0.00001);
					    float sumw = w.x + w.y + w.z + w.w;
	
						col += fogsample0 * w.x;
						col += fogsample1 * w.y;
						col += fogsample2 * w.z;
						col += fogsample3 * w.w;
						col /= sumw;
					}
            	#else
            		col += fogsample0;
					col += fogsample1;
					col += fogsample2;
					col += fogsample3;
					col *= 0.25f;
            	#endif
            	
            	col.xyz += (2.0 * rnd.yzw - 1.0) / 255.0; //note: 8bit tpdf dithering
				col.a = Luminance(col.xyz);
            	return col;
            }
            ENDHLSL
        }
    	
        Pass
        {
            Name "TimeSample"
            Tags{"LightMode" = "NULL"}
            ZTest Always
            ZWrite Off
            Cull Off
            Blend one zero
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Fullscreen.hlsl"
            #pragma vertex FullscreenVert
            #pragma fragment Fragment

            #pragma multi_compile __ USE_CLAMP
			#pragma multi_compile __ USE_CLIPPING
			#pragma multi_compile __ USE_OPTIMIZATIONS
            #pragma multi_compile __ USE_VARIANCE_CLIP
			#pragma multi_compile __ USE_YCOCG

            TEXTURE2D_X(_VolumetricLightTexture);
            SAMPLER(sampler_VolumetricLightTexture);

            TEXTURE2D_X(_VolumetricLightHistoryTexture);
            SAMPLER(sampler_VolumetricLightHistoryTexture);

            TEXTURE2D(_VolumetricLightMotionTexture);
            SAMPLER(sampler_VolumetricLightMotionTexture);

            
            CBUFFER_START(UnityPerMaterial)
			float4 _VolumetricLightTexture_TexelSize;

            float4 _VolumetricLightHistoryBlendParams;
			CBUFFER_END
            
			

            // https://software.intel.com/en-us/node/503873
	        float3 RGB_YCoCg(float3 c)
	        {
		        // Y = R/4 + G/2 + B/4
		        // Co = R/2 - B/2
		        // Cg = -R/4 + G/2 - B/4
		        return float3(
			         c.x/4.0 + c.y/2.0 + c.z/4.0,
			         c.x/2.0 - c.z/2.0,
			        -c.x/4.0 + c.y/2.0 - c.z/4.0
		        );
	        }

	        // https://software.intel.com/en-us/node/503873
	        float3 YCoCg_RGB(float3 c)
	        {
		        // R = Y + Co - Cg
		        // G = Y + Cg
		        // B = Y - Co - Cg
		        return saturate(float3(
			        c.x + c.y - c.z,
			        c.x + c.z,
			        c.x - c.y - c.z
		        ));
	        }

            float4 SampleColor(TEXTURE2D_PARAM(tex, samplerName), float2 uv)
	        {
	            float4 c = SAMPLE_TEXTURE2D_X(tex, samplerName, uv);
	            #if USE_YCOCG
	        		return float4(RGB_YCoCg(c.rgb), c.a);
	            #else
	        		return c; 
	            #endif
	        }
            

            float4 TransformColor(float4 c)
	        {
	        	#if USE_YCOCG
	        		return float4(RGB_YCoCg(c.rgb), c.a);
	            #else
	        		return c; 
	            #endif
	        }

            float4 ResolveColor(float4 c)
	        {
	        	#if USE_YCOCG
	        		return float4(YCoCg_RGB(c.rgb), c.a);
	            #else
	        		return c; 
	            #endif
	        }

            half4 SampleTexture(TEXTURE2D_PARAM(Texture, Sampler), float2 uv)
			{
				return SAMPLE_TEXTURE2D_X(Texture, Sampler, uv);
			}

			void minmax(in float2 uv, out half4 cmin , out half4 cmax, out half4 cavg , out half4 cstd)
			{
				float2 du = float2(_VolumetricLightTexture_TexelSize.x, 0.0);
				float2 dv = float2(0.0, _VolumetricLightTexture_TexelSize.y);

				half4 ctl = SampleColor(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), uv - dv - du);
				half4 ctc = SampleColor(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), uv - dv);
				half4 ctr = SampleColor(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), uv - dv + du);
				half4 cml = SampleColor(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), uv - du);
				half4 cmc = SampleColor(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), uv);
				half4 cmr = SampleColor(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), uv + du);
				half4 cbl = SampleColor(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), uv + dv - du);
				half4 cbc = SampleColor(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), uv + dv);
				half4 cbr = SampleColor(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), uv + dv + du);

				cmin  = min(ctl, min(ctc, min(ctr, min(cml, min(cmc, min(cmr, min(cbl, min(cbc, cbr))))))));
				cmax = max(ctl, max(ctc, max(ctr, max(cml, max(cmc, max(cmr, max(cbl, max(cbc, cbr))))))));

				cavg  = (ctl + ctc + ctr + cml + cmc + cmr + cbl + cbc + cbr) / 9.0;
			#if USE_VARIANCE_CLIP
	        	half4 csqr = (ctl * ctl + ctc * ctc + ctr * ctr + cml * cml + cmc * cmc + cmr * cmr + cbl * cbl + cbc * cbc + cbr * cbr) / 9.0;
	        	cstd = sqrt(abs(csqr - cavg * cavg));
	        #else
				cstd = 0;	
	        #endif
			}
            
#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0


			half4 clip_aabb(float3 aabb_min, float3 aabb_max, float4 p, float4 q)
			{
			#if USE_OPTIMIZATIONS
				// note: only clips towards aabb center (but fast!)
				float3 p_clip = 0.5 * (aabb_max + aabb_min);
				float3 e_clip = 0.5 * (aabb_max - aabb_min) + FLT_EPS;

				float4 v_clip = q - float4(p_clip, p.w);
				float3 v_unit = v_clip.xyz / e_clip;
				float3 a_unit = abs(v_unit);
				float ma_unit = max(a_unit.x, max(a_unit.y, a_unit.z));

				if (ma_unit > 1.0)
					return float4(p_clip, p.w) + v_clip / ma_unit;
				else
					return q;// point inside aabb
			#else
				float4 r = q - p;
				float3 rmax = aabb_max - p.xyz;
				float3 rmin = aabb_min - p.xyz;

				const float eps = FLT_EPS;

				if (r.x > rmax.x + eps)
					r *= (rmax.x / r.x);
				if (r.y > rmax.y + eps)
					r *= (rmax.y / r.y);
				if (r.z > rmax.z + eps)
					r *= (rmax.z / r.z);

				if (r.x < rmin.x - eps)
					r *= (rmin.x / r.x);
				if (r.y < rmin.y - eps)
					r *= (rmin.y / r.y);
				if (r.z < rmin.z - eps)
					r *= (rmin.z / r.z);

				return p + r;
			#endif
			}
            
            float GetDepthMeter(float z)
            {
    #if UNITY_REVERSED_Z
                float dx_Z = z;
    #else
                float dx_Z = (z + 1) * 0.5;
    #endif
                return LinearEyeDepth(dx_Z, _ZBufferParams);
            }
            
            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            	float2 screenUV = input.uv.xy;
            	
                // reprojection
            	// https://github.com/playdeadgames/temporal/blob/master/Assets/Shaders/TemporalReprojection.shader
            	float2 ss_vel = SampleTexture(_VolumetricLightMotionTexture, sampler_VolumetricLightMotionTexture, screenUV);

                float2 previousScreenUV = screenUV - ss_vel;
            	// float depthMax = 0;//GetMaxDepth(screenUV);
				// float currentDepth = GetDepthMeter(depthMax);

                half4 texel0 = TransformColor(SampleTexture(TEXTURE2D_ARGS(_VolumetricLightTexture, sampler_VolumetricLightTexture), screenUV));
				half4 texel1 = TransformColor(SampleTexture(TEXTURE2D_ARGS(_VolumetricLightHistoryTexture, sampler_VolumetricLightHistoryTexture), previousScreenUV));
        #if USE_CLAMP
				half4 cmin, cmax, cavg, cstd;
				minmax(screenUV, cmin, cmax, cavg, cstd);
			#if USE_VARIANCE_CLIP
            	float gamma = _VolumetricLightHistoryBlendParams.z;
            	cmin = cavg - gamma * cstd;
            	cmax = cavg + gamma * cstd;
			#endif
            	
            	// shrink chroma min-max
			#if USE_YCOCG
				float2 chroma_extent = 0.25 * 0.5 * (cmax.r - cmin.r);
				float2 chroma_center = texel0.gb;
				cmin.yz = chroma_center - chroma_extent;
				cmax.yz = chroma_center + chroma_extent;
				cavg.yz = chroma_center;
			#endif

            #if USE_CLIPPING
            	texel1 = clip_aabb(cmin.xyz, cmax.xyz, clamp(cavg, cmin, cmax), texel1);
			#else
            	texel1 = clamp(texel1, cmin, cmax);
            #endif
		#endif
            	
            	// feedback weight from unbiased luminance diff (t.lottes)
			#if USE_YCOCG
				float lum0 = texel0.r;
				float lum1 = texel1.r;
			#else
				float lum0 = Luminance(texel0.rgb);
				float lum1 = Luminance(texel1.rgb);
			#endif
				float unbiased_diff = abs(lum0 - lum1) / max(lum0, max(lum1, 0.2));
				float unbiased_weight = 1.0 - unbiased_diff;
				float unbiased_weight_sqr = unbiased_weight * unbiased_weight;
				float color_feedback = lerp(_VolumetricLightHistoryBlendParams.x, _VolumetricLightHistoryBlendParams.y, unbiased_weight_sqr);
            	
            	float4 color_temporal = lerp(texel0, texel1, color_feedback);
				color_temporal = ResolveColor(color_temporal);
            	float4 color_current = ResolveColor(texel0);
            	float blend = 1;
            	
            	if (previousScreenUV.x > 1 || previousScreenUV.x < 0 || previousScreenUV.y > 1 || previousScreenUV.y < 0)
            	{
            		blend = 0;
            	}
            	float4 color = lerp(color_current, color_temporal, blend);
            	return color;
            }
            ENDHLSL
        }
        
    	Pass
        {
        	Name "ScreenBlend"
            Tags{"LightMode" = "NULL"}
            ZTest Always
            ZWrite Off
            Cull Off
            

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_VolLight);
            SAMPLER(sampler_VolLight);

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 col = half4(SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, input.uv));
            	half4 col2 = half4(SAMPLE_TEXTURE2D_X(_VolLight, sampler_VolLight, input.uv));
                return col+col2;
            }
            ENDHLSL
        }
    }
}