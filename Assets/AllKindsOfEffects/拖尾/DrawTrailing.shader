Shader "DrawTrailing"
{
    Properties
    {
        [Tex(_MainColor)]_MainTex ("Main Tex", 2D) = "white" { }
        [HideInInspector]_MainColor ("Main Color", Color) = (1, 1, 1, 1)
        
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
        half4 _MainColor;
        CBUFFER_END
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
                float3 worldPos: TEXCOORD1;
                float2 uv: TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float cubeSdf(float3 p,float r){
                p.z-=1.;
            
                float3 p_1 = abs(p) - (r *0.5);
                float inner = min(max(max(p_1.x,p_1.y),p_1.z),0.0);//inner points
                float outer =length(max(0,p_1));//outer points
                return inner + outer;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
                o.worldPos = vertexPos.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            float4 testPostion[5];
            float4 PlayerPostion;
            int ArrayCount;

            half4 frag(v2f i): SV_Target
            {
               float2 playerOffset = testPostion[0].xz/256;
               float player = length(i.uv-0.5);
               player = 1 - pow(player/0.02, 2);
               float4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv - playerOffset);
               player = max(player, col.r);
               
               float other = 0;
               for (int j = 1; j < ArrayCount; j++)
               {
                    float ot = length(i.uv - 0.5 + (testPostion[j].xz/256 - PlayerPostion.xz/256));
                    ot = max(1-pow(ot/0.02, 2),0);
                    other += ot; 
               }
               float4 otc = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv - playerOffset);
               other = max(other, otc.g);

               
               float2 quad = i.uv - 0.5;
               float2 len = 0.485;
               // quad = pow(1-length(max(quad ,-len) - min(quad,len)),200);
               quad = step(0.99,(1-length(max(quad ,-len) - min(quad,len))));
               // quad = saturate(quad);
               // return quad.xxxx;
               // return smoothstep(0 , 1, saturate(pow(quad.xyxy,20)));
               return float4(player.r , other*quad.x, 0 , 1)- 0.002 ;
            }
            ENDHLSL            
        }
        
         Pass
        {
            Name "PONG"
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
                float3 worldPos: TEXCOORD1;
                float2 uv: TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
                o.worldPos = vertexPos.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            half4 frag(v2f i): SV_Target
            {
                //UV [0, 1], PLAYER [-world, world] / RTSize;
                float4 rt = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv) * 0.999;
                return rt;
            }
            ENDHLSL            
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
