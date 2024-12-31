Shader "Underwater"
{
    Properties
    {
        [Tex(_MainColor)]_MainTex ("Main Tex", 2D) = "white" { }
        [HideInInspector]_MainColor ("Main Color", Color) = (1, 1, 1, 1)
        
        _WaterLineColor ("Water Line Color", Color) = (0.2, 0.2, 0.7, 1)
        
        _Density ("Density", Range(0, 1)) = 0.1
        _GradientMap("Gradient Map", 2D) = "white" { }
        _NearbyTint("Nearby Tint", Color) = (1, 1, 1, 1)
        
        //焦散
        [Space(10)]
        [Header(Caustic)]
        _CausticMap("焦散图", 2D) = "black" {}
        _CausticsSize("焦散大小",float) = 1
        _CausticsSpeed("焦散速度", Float) = 0.2
        
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
        
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _CameraPos;
        half4 _MainColor;
        float _Size;
        float _Aspect;
        float _MainCameraNear;
        // float4x4 _InvV;
        // float4x4 _InvP;
        float4x4 _InvVP;

        float4 _WaterLineColor;
        
        float _Density;
        float4 _NearbyTint;

        float _CausticsSize;
        float _CausticsSpeed;
        CBUFFER_END
        float4 _CameraCorners[4];
        float4 _CameraFarCorners[4];
        ENDHLSL
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 worldPos: TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D_X_FLOAT(_WaterDepthWorldSpace);SAMPLER(sampler_WaterDepthWorldSpace);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_GradientMap);SAMPLER(sampler_GradientMap);
            TEXTURE2D(_CausticMap);SAMPLER(sampler_ScreenTextures_linear_repeat);
            
            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
                o.worldPos = TransformObjectToWorld(o.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            

            float2 CausticUVs(float2 rawUV, float2 offset, float sizeScale, float timeScale)
            {
                //anim
                float2 uv = rawUV * _CausticsSize * sizeScale + float2(_Time.y, _Time.x) * timeScale;
                return uv + offset * 0.25;
            }

            float2 hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }
            
            float PerlinNoise(float2 uv)
            {
                float2 p = floor(uv);
                float2 f = frac(uv);
            
                f = f * f * (3.0 - 2.0 * f);
            
                float2 uv00 = p + float2(0.0, 0.0);
                float2 uv10 = p + float2(1.0, 0.0);
                float2 uv01 = p + float2(0.0, 1.0);
                float2 uv11 = p + float2(1.0, 1.0);
            
                float2 g00 = hash(uv00);
                float2 g10 = hash(uv10);
                float2 g01 = hash(uv01);
                float2 g11 = hash(uv11);
            
                float n00 = dot(g00, f - float2(0.0, 0.0));
                float n10 = dot(g10, f - float2(1.0, 0.0));
                float n01 = dot(g01, f - float2(0.0, 1.0));
                float n11 = dot(g11, f - float2(1.0, 1.0));
            
                float2 fade_xy = f * f * (3.0 - 2.0 * f);
                float n_x0 = lerp(n00, n10, fade_xy.x);
                float n_x1 = lerp(n01, n11, fade_xy.x);
                float n_xy = lerp(n_x0, n_x1, fade_xy.y);
            
                return n_xy;
            }
            
            half4 frag(v2f i): SV_Target
            {
                //获取主相机的近裁面四个角的世界坐标并使用屏幕空间的uv插值得到世界坐标
                float4 worldPosition = lerp(lerp(_CameraCorners[0], _CameraCorners[1], i.uv.x),lerp(_CameraCorners[2], _CameraCorners[3], i.uv.x),i.uv.y);

                //计算深度uv，先将UV挪到中心，然后乘以 近裁面的宽度与拍深度相机的宽度的比例，再挪回去（缩放，只采样图的一部分）
                float2 DepthUV = float2((i.uv.x-0.5)*distance(_CameraCorners[1],_CameraCorners[0])/(_Size*_Aspect*2)+0.5,(0+_MainCameraNear)/(_Size*2));
                float waterDepth = SAMPLE_TEXTURE2D(_WaterDepthWorldSpace, sampler_WaterDepthWorldSpace, DepthUV);
                // depth +=1;
                #if defined(SHADER_API_GLCORE) || defined (SHADER_API_GLES) || defined (SHADER_API_GLES3)        // OpenGL平台 //
                depth = depth * 2 - 1;
                #endif
                
                float underWater;
                if(waterDepth == 0)
                {
                    underWater =  0;
                }
                else if(waterDepth > worldPosition.y)
                {
                    underWater = 1;
                }
                else
                {
                    underWater = 0;
                }


                //采样屏幕颜色
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float2 noiseDepthUV = float2((i.uv.x-0.5)*distance(_CameraCorners[1],_CameraCorners[0])/(_Size*_Aspect*2)+0.5,i.uv.y);
                float noisewaterDepth = PerlinNoise(noiseDepthUV*5+_Time.y*0.3);
                noisewaterDepth = noisewaterDepth;
                half4 underWaterCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(i.uv.x-noisewaterDepth*0.02,i.uv.y-noisewaterDepth*0.05));
                
                //水下颜色
                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, float2(i.uv.x-noisewaterDepth*0.02,i.uv.y-noisewaterDepth*0.05));
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float depthAmount = 1 - exp(-linearDepth * _Density);
                float4 rampColor = SAMPLE_TEXTURE2D(_GradientMap,sampler_GradientMap, float2(depthAmount, 0.1));
                // float4 nearColor = col * _NearbyTint;
                // col = lerp(nearColor, rampColor, rampColor.a);
                float3 outColor = lerp(underWaterCol, rampColor * _NearbyTint, depthAmount);

                //焦散
                #if defined(SHADER_API_GLCORE) || defined (SHADER_API_GLES) || defined (SHADER_API_GLES3)        // OpenGL平台 //
                depth = depth * 2 - 1;
                #endif
                
                #if UNITY_UV_STARTS_AT_TOP
                i.uv.y = 1 - i.uv.y ;
                #endif
                
                float4 NDC = float4(i.uv * 2 - 1,depth,1.0); //NDC空间
                //将深度值转换为世界空间
                float4 positionWorldSpace = mul(UNITY_MATRIX_I_VP, NDC);
                positionWorldSpace = positionWorldSpace / positionWorldSpace.w;


                //水线
                float waterLineA = (smoothstep(0,0.01,waterDepth - worldPosition.y)).xxx;
                float waterLineB = (smoothstep(0,0.01,worldPosition.y - waterDepth)).xxx;

                float waterLine = 1-saturate(max(waterLineA,waterLineB));

                
                float4 color = float4(lerp(col,outColor,underWater)+waterLine*_WaterLineColor,1);
                color = float4(lerp(color,_WaterLineColor,waterLine));
                
                return float4(color.xyz, 1);
            }
            ENDHLSL            
        }
        
        Pass
        {
            Name "WaterDepth"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);SAMPLER(sampler_CameraDepthTexture);
            
            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            half4 frag(v2f i): SV_Target
            {

                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);

                #if defined(SHADER_API_GLCORE) || defined (SHADER_API_GLES) || defined (SHADER_API_GLES3)        // OpenGL平台 //
                depth = depth * 2 - 1;
                #endif
                
                #if UNITY_UV_STARTS_AT_TOP
                i.uv.y = 1 - i.uv.y ;
                #endif
                
                float4 NDC = float4(i.uv * 2 - 1,depth,1.0); //NDC空间
                //将深度值转换为世界空间
                float4 positionWorldSpace = mul(_InvVP, NDC);
                
                //传递世界空间.y的深度值
                return float4(positionWorldSpace.yyy, 1);
            }
            ENDHLSL            
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
