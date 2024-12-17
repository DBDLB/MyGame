//配合Blender的Ocean 使用，文件中有现成的。
//或者链接：https://www.youtube.com/watch?v=rV6TJ7YDJY8&t=432s。
Shader "Unlit URP Shader"
{
    Properties
    {
        _OceanColorShallow ("Ocean Color Shallow", Color) = (1, 1, 1, 1)
        _OceanColorDeep ("Ocean Color Deep", Color) = (1, 1, 1, 1)
        _BubblesColor ("Bubbles Color", Color) = (1, 1, 1, 1)
        _Specular ("Specular", Color) = (1, 1, 1, 1)
        _Gloss ("Gloss", Range(8.0, 256)) = 20
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale("Bump Scale",range(0,1)) = 1
        _DisplacementMap("Displacement Map", 2D) = "white" {}
        _Bubbles ("Bubbles", 2D) = "black" { }
        _SkyboxMap("Skybox", Cube) = "white" {}
        
        _Tile("Tile",float) = 1
        _FrameRate("Frame Rate",int) = 30
        _Interpolate("Interpolate",range(0,1)) = 0.5
        
        //test
        _FresnelScale ("Fresnel Scale", Range(0, 1)) = 0.5
    }
 
    SubShader
    {
         Tags { "Queue"="Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

 
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
 
 
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
 
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 positionSS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 binormalWS : TEXCOORD4;
                float2 uv : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            CBUFFER_START(UnityPerMaterial)
            float _Tile;
            half4 _BaseColor;
            float _BumpScale;
            float4 _Displace_ST;
            float _Displace_TexelSize;
            int _FrameRate;
            float _Interpolate;
            half _FresnelScale;
            half4 _OceanColorShallow;
            half4 _OceanColorDeep;
            half4 _BubblesColor;
            half4 _Specular;
            half _Gloss;
            CBUFFER_END
            TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
            TEXTURE2D(_DisplacementMap);SAMPLER(sampler_DisplacementMap);
            TEXTURE2D(_Bubbles);SAMPLER(sampler_Bubbles);
            TEXTURECUBE(_SkyboxMap);SAMPLER(sampler_SkyboxMap);
            // SamplerState sampler_LinearRepeat;

            UNITY_INSTANCING_BUFFER_START(Props) 
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color) 
            UNITY_INSTANCING_BUFFER_END(Props) 

            
            void Unity_Flipbook_InvertY_half (half2 UV, half Width, half Height, half Tile, half2 Invert, out half2 Out)
            {
                Tile = floor(fmod(Tile + half(0.00001), Width*Height));
                half2 tileCount = half2(1.0, 1.0) / half2(Width, Height);
                half base = floor((Tile + half(0.5)) * tileCount.x);
                half tileX = (Tile - Width * base);
                half tileY = (Invert.y * Height - (base + Invert.y * 1));
                Out = (UV + half2(tileX, tileY)) * tileCount;
            }


            float3 GetFFTOceanFlipBook_NormalTS(float2 UV)
            {
                float4 tileUV = float4(UV,0,1) * _Tile;
                tileUV = frac(tileUV);
                float4 spacedUV = (4.0/512.0 + tileUV) * (1 - (4.0/512.0 * 2.0));
                float timeRate = _FrameRate * _TimeParameters.x;
                
                float rate1 = floor(fmod(timeRate,64));
                float rate2 = floor(fmod(timeRate+3,64));

                float ddx_A = dot(ddx(tileUV.a),ddx(tileUV.a));
                float ddx_R = dot(ddx(tileUV.r),ddx(tileUV.r));
                float ddx_G = dot(ddx(tileUV.g),ddx(tileUV.g));
                float ddx_Final =  max(0,log2(max(0,ddx_A + ddx_R)) * 0.5);
                float ddx_Final2 = max(0,log2(max(0,ddx_G + ddx_A)) * 0.5);

                float lod_Level = ddx_Final + ddx_Final2;

                float2 uv_FlipBook1,uv_FlipBook2;
                Unity_Flipbook_InvertY_half(spacedUV.xy,8,8,rate1,float2(0,1) ,uv_FlipBook1);
                Unity_Flipbook_InvertY_half(spacedUV.xy,8,8,rate2,float2(0,1) ,uv_FlipBook2);

                float4 normalTS1 = SAMPLE_TEXTURE2D_LOD(_BumpMap,sampler_LinearRepeat,uv_FlipBook1,lod_Level);
                float3 unpackedNormal1 = UnpackNormal(normalTS1);
                float4 normalTS2 = SAMPLE_TEXTURE2D_LOD(_BumpMap,sampler_LinearRepeat,uv_FlipBook2,lod_Level);
                float3 unpackedNormal2 = UnpackNormal(normalTS2);

                float3 unpackedNormal = lerp(unpackedNormal1,unpackedNormal2,_Interpolate);
                return unpackedNormal;
            }

            float4 GetFFTOceanFlipBook_Displacement(float2 UV)
            {
                float4 tileUV = float4(UV,0,1) * _Tile;
                tileUV = frac(tileUV);
                float4 spacedUV = (4.0/512.0 + tileUV) * (1 - (4.0/512.0 * 2.0));
                float timeRate = _FrameRate * _TimeParameters.x;
                
                float rate1 = floor(fmod(timeRate,64));
                float rate2 = floor(fmod(timeRate+3,64));

                float2 uv_FlipBook1,uv_FlipBook2;
                Unity_Flipbook_InvertY_half(spacedUV.xy,8,8,rate1,float2(0,1) ,uv_FlipBook1);
                Unity_Flipbook_InvertY_half(spacedUV.xy,8,8,rate2,float2(0,1) ,uv_FlipBook2);

                float4 displacement1 = SAMPLE_TEXTURE2D_LOD(_DisplacementMap,sampler_LinearRepeat,uv_FlipBook1,0);
                float4 displacement2 = SAMPLE_TEXTURE2D_LOD(_DisplacementMap,sampler_LinearRepeat,uv_FlipBook2,0);
                float4 displacement = lerp(displacement1,displacement2,_Interpolate);
                return displacement;
            }


            Varyings vert(Attributes v)
            {
                Varyings o;
                ZERO_INITIALIZE(Varyings, o);
                float4 displcae = GetFFTOceanFlipBook_Displacement(float4(v.uv.xy,0,0));
                v.positionOS += float4(displcae.xyz, 0);
                UNITY_SETUP_INSTANCE_ID(v); 
                UNITY_TRANSFER_INSTANCE_ID(v, o); 
                VertexPositionInputs positionInput = GetVertexPositionInputs(v.positionOS);
                o.positionWS = positionInput.positionWS;
                o.positionCS = positionInput.positionCS;
                o.positionSS = positionInput.positionNDC;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS,v.tangentOS);
                o.normalWS = normalInput.normalWS;
                o.tangentWS = normalInput.tangentWS;
                o.binormalWS = normalInput.bitangentWS;
                o.uv = v.uv;
 
                return o;
            }

            
            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                // half4 c;
                
                float3 normal = normalize(i.normalWS);
                float3 tangent = normalize(i.tangentWS);
                float3 binormal = normalize(i.binormalWS);

                float3x3 TBNmatrix = transpose(float3x3(tangent, binormal, normal));
                
                float3 unpackedNormal = GetFFTOceanFlipBook_NormalTS(i.uv);
                float3 unpackedNormalStrength = unpackedNormal * _BumpScale;
                float3 normalWS = normalize(mul(TBNmatrix, unpackedNormalStrength.xyz).xyz);

                half3 viewDirWS =  normalize(GetWorldSpaceViewDir(i.positionWS));
                
                
                //采样反射探头
                float3 reflectedDirection = reflect(viewDirWS, normalize(normalWS));
                float4 sky = SAMPLE_TEXTURECUBE_LOD(_SkyboxMap, sampler_SkyboxMap, -reflectedDirection,
                                                   0.1 * UNITY_SPECCUBE_LOD_STEPS);
                
                //菲涅尔
                half fresnel = saturate(_FresnelScale + (1 - _FresnelScale) * pow(1 - dot(normalWS, viewDirWS), 5));
                half facing  = saturate(dot(viewDirWS, normalWS));                
                half3 oceanColor = lerp(_OceanColorShallow, _OceanColorDeep, facing);

                Light light = GetMainLight();
                float3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w)*0.1;
                
                //泡沫颜色
                half3 bubblesDiffuse = _BubblesColor.rbg * light.color.rgb * saturate(dot(light.direction, normalWS));
                //海洋颜色
                half3 oceanDiffuse = oceanColor * light.color.rgb * saturate(dot(light.direction, normalWS));
                half3 halfDir = normalize(light.direction + viewDirWS);
                half3 specular = light.color.rgb * _Specular.rgb * pow(max(0, dot(normalWS, halfDir)), _Gloss);

                half bubbles = SAMPLE_TEXTURE2D(_Bubbles, sampler_Bubbles, i.uv).r;

                half3 diffuse = lerp(oceanDiffuse, bubblesDiffuse, bubbles);
                
                half3 col = ambient + lerp(diffuse, sky, fresnel) + specular ;

                
                
                return float4(col ,1);
            }
            ENDHLSL
        }
    }
}

