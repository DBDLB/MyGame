Shader "PostProcess/ColorTint"//名字开放位置
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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

                output.positionCS = pos;
                output.uv   = uv;
                return output;
            }

            float GetCameraFOV()
            {
                //https://answers.unity.com/questions/770838/how-can-i-extract-the-fov-information-from-the-pro.html
                float t = unity_CameraProjection._m11;
                float Rad2Deg = 180 / 3.1415;
                float fov = atan(1.0f / t) * 2.0 * Rad2Deg;
                return fov;
            }

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            TEXTURE2D_X_FLOAT(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);

            TEXTURE2D_X_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);


            // float4 _ColorTint;
        CBUFFER_START (UnityPerMaterial)
            float4 _CameraNormalsTexture_TexelSize;
            float depth_threshold = 0.01;
            float normal_threshold = 0.01;
            float outline_width = 1;
            float4 outline_color;
        CBUFFER_END
            
            float get_depth(float2 uv)
            {
                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                depth = LinearEyeDepth(depth, _ZBufferParams);
                // float3 ndc = float3(uv * 2 - 1, depth);
                // float4 view = dot(_ZBufferParams, float4(ndc, 1));
                // view.xyz /= view.w;
                return depth;
            }

            half4 frag (Varyings input) : SV_Target
            {
                Light light = GetMainLight();
                half3 lightDir = light.direction;
                float3 normal = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, input.uv).xyz*2-1;
                float3 normal_V = TransformWorldToViewDir(normal);
                float depth = get_depth(input.uv);
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.uv);

                // float3 offset = float3((1 / _ScreenParams.x), (1 / _ScreenParams.y), 0);
                float2 uvs[4];
                // uvs[0] = input.uv+ offset.zy;
                // uvs[1] = input.uv- offset.zy;
                // uvs[2] = input.uv+ offset.xz;
                // uvs[3] = input.uv- offset.xz;

                uvs[0] = input.uv + _CameraNormalsTexture_TexelSize.xy * float2(0, -1)*outline_width;
                uvs[1] = input.uv + _CameraNormalsTexture_TexelSize.xy * float2(-1, 0)*outline_width;
                uvs[2] = input.uv + _CameraNormalsTexture_TexelSize.xy * float2(1, 0)*outline_width;
                uvs[3] = input.uv + _CameraNormalsTexture_TexelSize.xy * float2(0, 1)*outline_width;

                float depth_diff = 0.0;
                float nearest_depth = depth;
                float2 nearest_uv = input.uv;

                float3 normal_sum = 0;
                for (int i= 0;  i < 4;  i++)
                {
                    float d = get_depth(uvs[i]);
                    depth_diff += (depth -d);
                    // depth_diff= d;
                    if (d < nearest_depth)
                    {
                        nearest_depth = d;
                        nearest_uv = uvs[i];
                    }

                    float3 n = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uvs[i]).xyz*2-1;
                    n = TransformWorldToViewDir(n);
                    float3 normal_diff = normal_V - n;

                    float3 normal_edge_bias = float3(1, 1, 1);
                    float normal_bias_diff = dot(normal_diff, normal_edge_bias);
                    float normal_indicator = smoothstep(-0.01, 0.01, normal_bias_diff);

                    normal_sum += dot(normal_diff, normal_diff) * normal_indicator;
                }

                // float depth_edge = step(0.005, depth_diff);
                float depth_edge = step(depth_threshold, depth_diff);

                float indicator = sqrt(normal_sum);
                // float normal_edge = step(2, indicator);
                float normal_edge = step(normal_threshold, indicator);
                
                float3 nearest = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, nearest_uv);

                float ld = saturate(normalize(dot((normal), -lightDir)));

                float3 edge_mix;
                if (depth_edge > 0.0)
                {
                    edge_mix = lerp(color, nearest * (ld > 0 ? 0.1 : 2)*outline_color, depth_edge);
                }
                else
                {
                    edge_mix = lerp(color, nearest * (ld > 0 ? 0.1 : 2)*outline_color, normal_edge);
                }

                // if (depth_edge > 0.0)
                // {
                //     edge_mix = lerp(0, nearest * (ld > 0 ? 0.1 : 2), depth_edge);
                // }
                // else
                // {
                //     edge_mix = lerp(0, nearest * (ld > 0 ? 0.1 : 2), normal_edge);
                // }
                
                 float3 edgeMix = lerp(0,  (ld > 0 ? 0.1 : 2), normal_edge);
                // ALBEDO = vec3(depth_diff);


                return float4(edge_mix, 1.0);
                // return color * _ColorTint ;
            }
            ENDHLSL
        }
     
    }
}