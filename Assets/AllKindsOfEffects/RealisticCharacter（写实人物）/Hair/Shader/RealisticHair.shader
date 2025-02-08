Shader "RealisticHair"
{
    Properties
    {
        [Tex(_MainColor)]_MainTex ("Main Tex", 2D) = "white" { }
        [HideInInspector]_MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
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
        #include "Assets/AllKindsOfEffects/RealisticCharacter（写实人物）/Hair/Shader/Hair.hlsl"

        #pragma shader_feature _ADD_LIGHTS

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4 _MainColor;
        half4 _RootColor;
        half4 _TipColor;
        half _Cutoff;
        half _TEST;
        half _BaseColorStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            cull off

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
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_RIDO);
            SAMPLER(sampler_RIDO);

            TEXTURE2D(_FlowMapTexture);
            SAMPLER(sampler_FlowMapTexture);


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
                float3 positionVS = vertexPos.positionVS;

                // float4 RIDO = SAMPLE_TEXTURE2D_LOD(_RIDO, sampler_RIDO, v.uv,0);
                // float PixelDepthOffset = ((1-RIDO.r)-0.5)*0.1;
                // positionVS.z -= RIDO.r;
                // float4 positionCS = TransformWViewToHClip(positionVS);
                // float depth = positionCS.z / positionCS.w;
                // o.vertex.z = depth * o.vertex.w; //把偏移后的深度赋值到裁切空间


                // o.normalWS.xyz=normalize(TransformObjectToWorldNormal(v.normalOS.xyz));
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
                float4 FlowMap = SAMPLE_TEXTURE2D(_FlowMapTexture, sampler_FlowMapTexture, i.uv);
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

                clip(RIDO.a - _Cutoff);
                float3 color = HairBxDF(GBuffer, N, viewDir, lightDir,0,0,shadowTerms);

				half4 shadowMask = half4(1, 1, 1, 1);

                #if _ADD_LIGHTS
				int addLightsCount = GetAdditionalLightsCount();
				for(int t=0; t<addLightsCount;t++)
				{
					Light light0 = GetAdditionalLight(t, i.worldPos, shadowMask);

					color+=HairBxDF(GBuffer, N, viewDir, light0.direction,0,0,shadowTerms);
				}
				#endif

                float PixelDepthOffset = ((1-RIDO.r)-0.5)*_TEST;
                // return float4(RIDO.bbb, 1);
                return float4(color*(1-RIDO.r*_TEST), 1);
            }
            ENDHLSL
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
