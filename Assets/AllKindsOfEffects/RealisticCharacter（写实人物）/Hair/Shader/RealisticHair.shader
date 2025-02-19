Shader "RealisticHair"
{
    Properties
    {
        [Tex(_MainColor)]_MainTex ("Main Tex", 2D) = "white" { }
        [HideInInspector]_MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _CutoffMaxDistance("Max Distance", float) = 10
    	_DitherThresold("Dither Thresold", Range(0, 0.99)) = 0
        _RIDO("RIDO", 2D) = "white" { }
        _FlowMapTexture("FlowMapTexture", 2D) = "white" { }
        _RootColor("Root Color", Color) = (1, 1, 1, 1)
        _TipColor("Tip Color", Color) = (1, 1, 1, 1)

        _BaseColorStrength("Base Color Strength", float) = 1

        _TEST("TEST", float) = 1

        [Toggle(_ADD_LIGHTS)]_AddLights("AddLights", float)=1.0

        [Foldout(1, 1, 0, 0)]_Other ("Other_Foldout", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Hair.hlsl"

        #pragma shader_feature _ADD_LIGHTS

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4 _MainColor;
        half4 _RootColor;
        half4 _TipColor;
        half _TEST;
        half _BaseColorStrength;
        half _CutoffMaxDistance;
        float _DitherThresold;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            cull off
        	zwrite on

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
                float3 normalOS: NORMAL;
                float4 tangentOS:TANGENT;
            };

            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
                float4 normalWS:NORMAL;
                float3 worldPos: TEXCOORD1;
                float4 tangentWS:TANGENT;
                float4 BtangentWS:TEXCOORD2;
            	float4 screenPos:TEXCOORD3;
            	float3 BtangentSS:TEXCOORD4;
            };

            struct FragmentData
            {
	            half4 target0 : SV_Target0;
            	half4 target1 : SV_Target1;
            	//float depth : SV_Depth;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_RIDO);
            SAMPLER(sampler_RIDO);

            TEXTURE2D(_FlowMapTexture);
            SAMPLER(sampler_FlowMapTexture);

            // Bayer Dither Matrix
            const static float dither[64] = {
                1, 49, 13, 61, 4, 52, 16, 64,
                33, 17, 45, 29, 36, 20, 48, 32,
                9, 57, 5, 53, 12, 60, 8, 56,
                41, 25, 37, 21, 44, 28, 40, 24,
                3, 51, 15, 63, 2, 50, 14, 62,
                35, 19, 47, 31, 34, 18, 46, 30,
                11, 59, 7, 55, 10, 58, 6, 54,
                43, 27, 39, 23, 42, 26, 38, 22
            };

                const static  float4x4 thresholdMatrix =
        {  1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
          13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
           4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
          16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
        };

            FHairTransmittanceData InitHairTransmittanceData(bool bMultipleScatterEnable = true)
            {
            	FHairTransmittanceData o;
            	o.bUseLegacyAbsorption = true;
            	o.bUseSeparableR = true;
            	o.bUseBacklit = true;
            	o.bClampBSDFValue = true;

            	o.OpaqueVisibility = 1;
            	o.LocalScattering = 0;
            	o.GlobalScattering = 1;
            	o.ScatteringComponent = 1;

            	return o;
            }

            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
            	o.screenPos = ComputeScreenPos(o.vertex);
            	
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.normalWS.xyz=normalize(TransformObjectToWorldNormal(v.normalOS.xyz));
                o.tangentWS.xyz=normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                o.BtangentWS.xyz=cross(o.normalWS.xyz,o.tangentWS.xyz)*v.tangentOS.w*unity_WorldTransformParams.w;
                return o;
            }

            half4 frag(v2f i): SV_Target
            {

                // float3 norTS=UnpackNormalScale(nortex,_NormalScale);
                float3x3 T2W={i.tangentWS.xyz,i.BtangentWS.xyz,i.normalWS.xyz};
                T2W=transpose(T2W);
                Light light = GetMainLight();
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _MainColor;
                float4 RIDO = SAMPLE_TEXTURE2D(_RIDO, sampler_RIDO, i.uv);
            	// clip(RIDO.a-0.1);
                float4 FlowMap = SAMPLE_TEXTURE2D(_FlowMapTexture, sampler_FlowMapTexture, i.uv);
            	            	                // return float4(RIDO.bbb, 1);

                float3 Normal_test = normalize(lerp(float4(0,0,0.3,1),float4(0,0,-0.3,1), RIDO.g).rgb+(FlowMap.rgb*2-1)*(float3(1,1,0)));

                float3 N=NormalizeNormalPerPixel(mul(T2W,Normal_test));

                FGBufferData GBuffer;
                GBuffer.BaseColor = lerp(_RootColor, _TipColor, RIDO.b);
                GBuffer.BaseColor = clamp((GBuffer.BaseColor+GBuffer.BaseColor)*0.8,0.001,0.999) * light.color*_BaseColorStrength;
                GBuffer.Roughness = 0.38;
                GBuffer.Metallic = 0.2;
                GBuffer.CustomData = 0;
                GBuffer.Specular = 0.5;

                // FHairTransmittanceData transmittanceData;
                // transmittanceData.OpaqueVisibility = 1;
                // transmittanceData.LocalScattering = 1;
                // transmittanceData.GlobalScattering = 1;
                // transmittanceData.ScatteringComponent = 1;
                // transmittanceData.bUseLegacyAbsorption = 1;
                // transmittanceData.bUseSeparableR = 1;
                // transmittanceData.bUseBacklit = 1;
                // transmittanceData.bClampBSDFValue = 1;

                FShadowTerms shadowTerms;
                shadowTerms.SurfaceShadow = 1;
                shadowTerms.TransmissionShadow = 1;
                shadowTerms.TransmissionThickness = 1;
                shadowTerms.HairTransmittance = InitHairTransmittanceData(true);



                half3 lightDir = light.direction;
                half3 viewDir = normalize(GetWorldSpaceViewDir(i.worldPos));

            	//按距离动态裁剪
				float distance = length(i.worldPos - _WorldSpaceCameraPos);
				float dynamicCutoff = lerp(0.5, 0.05, saturate(distance / _CutoffMaxDistance));

				//Dither
            	// 获取当前像素在 Bayer 矩阵中的位置
            	float2 screenUV = i.screenPos.xy/(i.screenPos.w + 0.0001);
				float2 posSS = screenUV * _ScreenParams.xy;
            	uint index = (uint(posSS.x) % 8) * 8 + uint(posSS.y) % 8;
                // int x = int(i.uv.x * 8.0) % 8;
                // int y = int(i.uv.y * 8.0) % 8;
                // int index = y * 8 + x;
            	// 获取对应的阈值
                float threshold = (saturate(1.0 - _DitherThresold)- dither[index]/65*RIDO.bbb);
            	
				clip(threshold);
				clip(RIDO.a - dynamicCutoff);

            	
                float3 color = HairBxDF(GBuffer, N, viewDir, lightDir,0,0,shadowTerms);

				half4 shadowMask = half4(1, 1, 1, 1);

                #if _ADD_LIGHTS
				int addLightsCount = GetAdditionalLightsCount();
				for(int t=0; t<addLightsCount;t++)
				{
					Light light0 = GetAdditionalLight(t, i.worldPos, shadowMask);

					color+=HairBxDF(GBuffer, N, viewDir, light0.direction,0,0,shadowTerms) * light0.color;
				}
				#endif

                float PixelDepthOffset = ((1-RIDO.r)-0.5)*_TEST;
                // return float4(RIDO.rrr, 1);
                // return float4(i.worldPos.xyz, 1);
                return float4(color*(1-RIDO.r*_TEST), 1);
            }
            ENDHLSL
        }
    	
    	Pass
        {
            Name "HairAASource"
            Tags { "LightMode" = "HairAA_Source" }

            // -------------------------------------
            // Render State Commands
            ZWrite on
            ZTest lequal
            Cull off

            HLSLPROGRAM
            #pragma target 2.0
            
            #pragma vertex vert
            #pragma fragment frag
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
                float4 tangentOS:TANGENT;
                float3 normalOS: NORMAL;
            };
            
            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 worldPos: TEXCOORD1;
                float4 tangentWS:TANGENT;
                float4 normalWS:NORMAL;
                float4 BtangentWS:TEXCOORD2;
                float3 TangentSS:TEXCOORD3;
                float3 BtangentSS:TEXCOORD4;
            };
            
            TEXTURE2D(_RIDO);
            SAMPLER(sampler_RIDO);
            TEXTURE2D(_FlowMapTexture);
            SAMPLER(sampler_FlowMapTexture);
            
            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS.xyz=normalize(TransformObjectToWorldNormal(v.normalOS.xyz));
                o.tangentWS.xyz=normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                o.TangentSS = normalize(TransformWorldToViewDir(o.tangentWS.xyz));
                o.BtangentWS.xyz=cross(o.normalWS.xyz,o.tangentWS.xyz)*v.tangentOS.w*unity_WorldTransformParams.w;
                o.BtangentSS = normalize(TransformWorldToViewDir(o.BtangentWS.xyz));
                
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i): SV_Target
            {
                float3x3 T2W={i.tangentWS.xyz,i.BtangentWS.xyz,i.normalWS.xyz};
                T2W=transpose(T2W);
                float4 RIDO = SAMPLE_TEXTURE2D(_RIDO, sampler_RIDO, i.uv);
                float4 FlowMap = SAMPLE_TEXTURE2D(_FlowMapTexture, sampler_FlowMapTexture, i.uv);
                float3 Btangent = (FlowMap.rgb*2-1);
                float3 BtangentWS=NormalizeNormalPerPixel(mul(T2W,Btangent));
                float3 BtangentSS = normalize(TransformWorldToViewDir(BtangentWS.xyz));

                float distance = max(1-length(i.worldPos - _WorldSpaceCameraPos),0.1);
                float dynamicCutoff = lerp(0.5, 0.05, saturate(distance / _CutoffMaxDistance));
                clip(RIDO.a - dynamicCutoff);
                return float4(normalize(BtangentSS.xy*0.5+0.5),distance, 1);
            }
            ENDHLSL            
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
