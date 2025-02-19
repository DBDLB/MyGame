Shader "HairAA/Blur"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" { }
        _CameraColor ("_Camera Color", 2D) = "white" { }
        _Blur ("Blur", float) = 0
//        _SharpenStrength ("Sharpen Strength", range(0, 1)) = 0
        [Foldout(1, 1, 0, 0)]_Other ("Other_Foldout", float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float _Blur;
        float4 _MainTex_TexelSize;
        float _SharpenStrength;
        CBUFFER_END
        ENDHLSL
        
        Pass
        {
            Name "HairAASource"
            Tags { "LightMode" = "HairAA_Source" }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest Always
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
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CameraColor);
            SAMPLER(sampler_CameraColor);
            
            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS.xyz=normalize(TransformObjectToWorldNormal(v.normalOS.xyz));
                o.tangentWS.xyz=normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                o.BtangentWS.xyz=cross(o.normalWS.xyz,o.tangentWS.xyz)*v.tangentOS.w*unity_WorldTransformParams.w;
                
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i): SV_Target
            {
                float4 col = 0;
                // float2 dir = (float2(_X,_Y)-i.texcoord ) * _Blur * 0.01;
                float4 btangentWS = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                btangentWS.xy = btangentWS.xy*2-1;
                float2 dir = normalize(btangentWS.xy)*_Blur*btangentWS.a*btangentWS.z;

                float2 uvs[5];
                uvs[0] = (i.uv + _MainTex_TexelSize.xy * dir*-2);
                uvs[1] = (i.uv + _MainTex_TexelSize.xy * dir*-1);
                uvs[2] = (i.uv + _MainTex_TexelSize.xy * dir*0);
                uvs[3] = (i.uv + _MainTex_TexelSize.xy * dir*1);
                uvs[4] = (i.uv + _MainTex_TexelSize.xy * dir*2);
                
                float4 CameraColor = SAMPLE_TEXTURE2D(_CameraColor,sampler_CameraColor,i.uv);
                // col += SAMPLE_TEXTURE2D(_CameraColor,sampler_CameraColor,uvs[0])*0.1*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uvs[0]).a;
                // col += SAMPLE_TEXTURE2D(_CameraColor,sampler_CameraColor,uvs[1])*0.2*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uvs[1]).a;
                col += CameraColor * 0.5;
                col += SAMPLE_TEXTURE2D(_CameraColor,sampler_CameraColor,uvs[3])*0.3*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uvs[3]).a;
                col += SAMPLE_TEXTURE2D(_CameraColor,sampler_CameraColor,uvs[4])*0.2*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uvs[4]).a;

                col = lerp(CameraColor,col,btangentWS.a);
                
                return float4(col.xyz,1);
            }
            ENDHLSL            
        }
        
        Pass
        {
            Name "HairAASharpen"
            Tags { "LightMode" = "HairAA_Sharpen" }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest Always
            Cull off

            HLSLPROGRAM
            #pragma target 2.0
            
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CameraColor);
            SAMPLER(sampler_CameraColor);

            float3 mtexSample(const float x, const float y , float2 vv2_Texcoord)
            {
                float2 uv = vv2_Texcoord + float2(x / 1280.0, y / 720.0); // 纹理分辨率：1280,720
                float3 textureColor = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);    // 图像纹理采样
                return textureColor;
            }
            
            float3 sharpen(float strength , float2 uv)
            {
                //卷积核 (以拉普拉斯算子为例)
                float3 f =
                    mtexSample(-1.0, -1.0,uv) * -1.0 +
                    mtexSample(0.0, -1.0,uv) * -1.0 +
                    mtexSample(1.0, -1.0,uv) * -1.0 +
            
                    mtexSample(-1.0, 0.0,uv) * -1.0 +
                    mtexSample(0.0, 0.0,uv) * 9.0 +
                    mtexSample(1.0, 0.0,uv) * -1.0 +
            
                    mtexSample(-1.0, 1.0,uv) * -1.0 +
                    mtexSample(0.0, 1.0,uv) * -1.0 +
                    mtexSample(1.0, 1.0,uv) * -1.0;
            
                return lerp(float4(mtexSample(0.0, 0.0,uv), 1.0), float4(f, 1.0), strength).rgb;
            }

            
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
            };
            
            
            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS.xyz=normalize(TransformObjectToWorldNormal(v.normalOS.xyz));
                o.tangentWS.xyz=normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                o.BtangentWS.xyz=cross(o.normalWS.xyz,o.tangentWS.xyz)*v.tangentOS.w*unity_WorldTransformParams.w;
                
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i): SV_Target
            {
                float4 col = 0;
                col.xyz = sharpen(_SharpenStrength,i.uv);

                
                return float4(col.xyz,1);
            }
            ENDHLSL            
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
