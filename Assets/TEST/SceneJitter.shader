Shader "PostProcess/SceneJitter"//名字开放位置
{
        SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };



            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            TEXTURE2D_X_FLOAT(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);

            TEXTURE2D_X_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);


            float4 _ColorTint;
            float4 _CameraNormalsTexture_TexelSize;
            float depth_threshold = 0.01;

            float get_depth(float2 uv)
            {
                float depth = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, uv,0).r;
                depth = Linear01Depth(depth, _ZBufferParams);
                // float3 ndc = float3(uv * 2 - 1, depth);
                // float4 view = dot(_ZBufferParams, float4(ndc, 1));
                // view.xyz /= view.w;
                return depth;
            }

            float4 GetWorldPositionFromDepthValue( float2 uv, float linearDepth ) 
            {
                float camPosZ = _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * linearDepth;

                // unity_CameraProjection._m11 = near / t，其中t是视锥体near平面的高度的一半。
                // 投影矩阵的推导见：http://www.songho.ca/opengl/gl_projectionmatrix.html。
                // 这里求的height和width是坐标点所在的视锥体截面（与摄像机方向垂直）的高和宽，并且
                // 假设相机投影区域的宽高比和屏幕一致。
                float height = 2 * camPosZ / unity_CameraProjection._m11;
                float width = _ScreenParams.x / _ScreenParams.y * height;

                float camPosX = width * uv.x - width / 2;
                float camPosY = height * uv.y - height / 2;
                float4 camPos = float4(camPosX, camPosY, camPosZ, 1.0);
                return mul(unity_CameraToWorld, camPos);
            }


float _startRange;
float _endRange;
float _scanLineInterval;
float _scanLineWidth;
float _scanLineBrightness;
float _centerFadeout;
float3 _scanCenter;
float3 _SceneJitterDirection;
float _sceneJitterStrength;
float _flickerStrength;
float _useSceneJitter;
float _noiseStrength;

            // 2min循环一次的时间
#define _TIME_CYCLE_TWO_MIN fmod(_Time.y, 120)

            float2 hash(float2 p)//不用sin的更好
    {
        float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
        p3 = p3 + dot(p3,float3(p3.y,p3.z,p3.x)+19.19);
        return -1.0 + 2.0*frac((p3.xx+p3.yz)*float2(p3.z,p3.y));
    }
        
    float simplex2d(float2 p)
    {
        const float K1 = 0.366025404; // (sqrt(3)-1)/2;
        const float K2 = 0.211324865; // (3-sqrt(3))/6;
    
        //将输入点进行坐标偏移，向下取整得到原点，转换到超立方体空间
        float2 i = floor(p + (p.x + p.y) * K1);
        //得到转换前输入点到原点距离向量（单形空间下）
        float2 a = p - (i - (i.x + i.y) * K2);
        //确定顶点在哪个三角形内
        float2 o = (a.x < a.y) ? float2(0.0, 1.0) : float2(1.0, 0.0);
        //得到转换前输入点到第二个顶点的距离向量
        float2 b = a - o + K2;
        //得到转换前输入点到第三个顶点的距离向量
        float2 c = a - 1.0 + 2.0 * K2;
        //根据权重计算每个顶点的贡献度
        float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
        float3 n = h * h * h * h * float3(dot(a, hash(i)), dot(b, hash(i + o)), dot(c, hash(i + 1.0)));
        //乘以系数，做归一化处理
        return dot(float3(70.0, 70.0, 70.0), n);
    }

    float ScanLine (float2 noiseUV, float distanceToCenter,float change)
    {
        // 平行扫描线范围遮罩
        float scanLineStartRange = smoothstep(_startRange - distanceToCenter * 2.5, _startRange, distanceToCenter);
        float scanLineEndRange = 1-smoothstep(_endRange - distanceToCenter * 2.5, _endRange, distanceToCenter);
        // float scanLineRange =  scanLineRange2 * scanLineRange2;
    
        // float noise = SAMPLE_TEXTURE2D_LOD(_noiseMap, sampler_noiseMap, noiseUV,0).r;
        float noise = lerp(1.0,simplex2d(noiseUV),_noiseStrength);
        
        // 平行扫描线
        float wave = frac(distanceToCenter / _scanLineInterval - _TIME_CYCLE_TWO_MIN *change);
        float scanLine1 = smoothstep(0.5 - _scanLineWidth * 1/distanceToCenter , 0.5, wave);
        float scanLine2 = smoothstep(0.5 + _scanLineWidth * 1/distanceToCenter , 0.5, wave);
        float scanLine = scanLine2 * scanLine2;
        // DEBUG_OUTPUT(scanLine1, 1015001, "MA/地形/奇幻效果");
        scanLine = scanLine * scanLineStartRange * scanLineEndRange * _scanLineBrightness * _centerFadeout * noise;


    
        return scanLine;
    }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);


                output.positionCS = pos;
                output.uv   = uv;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                Light light = GetMainLight();
                half3 lightDir = light.direction;
                float3 normal = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, input.uv).xyz*2-1;
                float3 normal_V = TransformWorldToViewDir(normal);

                                float depth = get_depth(input.uv);
                float3 worldpos = GetWorldPositionFromDepthValue( input.uv, depth );
                
                 float distanceToCenter = distance(_scanCenter, worldpos);
            float2 noiseUV = worldpos.xz;
            //闪烁
            float flicker = lerp(1.0,frac(_TIME_CYCLE_TWO_MIN * 30.0)*3.0 , _flickerStrength);
            float scanLine = ScanLine(noiseUV, distanceToCenter,1.0)*_sceneJitterStrength * flicker*0.01;
            // scanLine = scanLine + ScanLine(noiseUV, distanceToCenter,0.6)*_sceneJitterStrength*0.4 * flicker;


    //                 half3x3 tangentSpaceTransform = half3x3(WorldSpaceTangent, WorldSpaceBiTangent, WorldSpaceNormal);
    // half3 viewDirTS = mul(tangentSpaceTransform, viewDirWS);
    //             float2 p = viewDirTS.xy / viewDirTS.z * (height * _Parallax + _ParallaxHeight * 0.5);
            
            worldpos = worldpos + _SceneJitterDirection * scanLine;
            // output.positionCS = TransformWorldToHClip(worldpos);

                
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.uv + scanLine * normal);


                


                return float4(color);
                return color * _ColorTint ;
            }
            ENDHLSL
        }
     
    }
}