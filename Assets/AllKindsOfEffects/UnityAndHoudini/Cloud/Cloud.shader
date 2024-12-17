Shader "MY_test/Cloud"{
    Properties{
        [HDR]_BaseColor("Base Color",Color) = (1.0,1.0,1.0,1.0)
        _CloudF("CloudF",2D) = "white"{}
        _CloudB("CloudB",2D) = "white"{}
        _BumpTex("Bump Texture",2D) = "white"{}
        _BumpScale("Bump Scale",Float) = 1.0
        _Gloss("Gloss",Float) = 1.0
        _Brightness("Brightness",Float) = 1.0
    }
    SubShader{
        Tags{
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalRenderPipeline"
            "IgnoreProjector"="True"
            "Queue"="Transparent"
        }
        
        pass{
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            HLSLPROGRAM
                #pragma vertex Vertex
                #pragma fragment Pixel
                
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

                TEXTURE2D(_CloudF);     SAMPLER(sampler_CloudF);
                TEXTURE2D(_CloudB);     SAMPLER(sampler_CloudB);

                TEXTURE2D(_BumpTex);    SAMPLER(sampler_BumpTex);

                CBUFFER_START(UnityPerMaterial)
                    float _BumpScale;
                    float4 _MainTex_ST;
                    float4 _BumpTex_ST;
                    half4 _BaseColor;
                    float _Gloss;
                    float _Brightness;
                CBUFFER_END

                struct vertexInput{

                    float4 vertex:POSITION;
                    float3 normal:NORMAL;
                    float2 uv_MainTex:TEXCOORD0;
                    float4 tangent:TANGENT;
                    //注意tangent是float4类型，因为其w分量是用于控制切线方向的。
                };

                struct vertexOutput{

                    float4 pos:SV_POSITION;
                    float2 uv_MainTex:TEXCOORD0;
                    float2 uv_BumpTex:TEXCOORD1;
                    float4 TW1:TEXCOORD2;
                    float4 TW2:TEXCOORD3;
                    float4 TW3:TEXCOORD4;
                };

                vertexOutput Vertex(vertexInput v){

                    vertexOutput o;
                    o.pos = TransformObjectToHClip(v.vertex.xyz);
                    float3 worldNormal = TransformObjectToWorldNormal(v.normal);
                    float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
                    float3 worldBinormal = cross(worldNormal,worldTangent) * v.tangent.w;

                    float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                    //计算世界法线，世界切线和世界副切线

                    o.uv_MainTex = v.uv_MainTex;

                    o.TW1 = float4(worldTangent.x,worldBinormal.x,worldNormal.x,worldPos.x);
                    o.TW2 = float4(worldTangent.y,worldBinormal.y,worldNormal.y,worldPos.y);
                    o.TW3 = float4(worldTangent.z,worldBinormal.z,worldNormal.z,worldPos.z);

                    return o;
                }

                half4 Pixel(vertexOutput i):SV_TARGET{

                    /* 提取在顶点着色器中的数据 */
                    float3x3 TW = float3x3(i.TW1.xyz,i.TW2.xyz,i.TW3.xyz);
                    float3 worldPos = half3(i.TW1.w,i.TW2.w,i.TW3.w);
                    
                    /* 先计算最终的世界法线 */
                    float4 normalTex = SAMPLE_TEXTURE2D(_BumpTex,sampler_BumpTex,i.uv_MainTex);        //对法线纹理采样
                    float3 bump = UnpackNormal(normalTex);                              //解包，也就是将法线从-1,1重新映射回0,1
                    bump.xy *= _BumpScale;
                    bump.z = sqrt(1.0 - saturate(dot(bump.xy,bump.xy)));
                    // //这个z的计算是因为法线仅存储x和y信息，而z可以由x^2 + y^2 + z^2 = 1反推出来。（法线是单位矢量）
                    bump = mul(TW,bump);             //将切线空间中的法线转换到世界空间中

                    /*计算纹理颜色*/
                    Light light = GetMainLight();
                    half4 cloud_F = _BaseColor * SAMPLE_TEXTURE2D(_CloudF,sampler_CloudF,i.uv_MainTex);
                    half4 cloud_B = _BaseColor * SAMPLE_TEXTURE2D(_CloudB,sampler_CloudB,i.uv_MainTex);

                    // return float4(SAMPLE_TEXTURE2D(_CloudF,sampler_CloudF,i.uv_MainTex).xyz,1);

                    // albedo.xyz *= light.color.xyz;

                    /*计算漫反射光照*/
                    half3 lightDir = light.direction;
                    lightDir.x = lightDir.x*0.5+0.5;
                    lightDir.y = lightDir.y*0.5+0.5;
                    lightDir.z = lightDir.z*0.5+0.5;

                    half3 cloud_x = lerp(cloud_B.x,cloud_F.x,lightDir.x);
                    cloud_x = max(cloud_x,cloud_F.x*0.1);
                    cloud_x = max(cloud_x,cloud_B.x*0.1);
                    half3 cloud_y = lerp(cloud_B.y,cloud_F.y,lightDir.y);
                    cloud_y = max(cloud_y,cloud_F.y*0.1);
                    cloud_y = max(cloud_y,cloud_B.y*0.1);
                    half3 cloud_z = lerp(cloud_B.z,cloud_F.z,1-lightDir.z);
                    cloud_z = max(cloud_z,cloud_F.z*0.1);
                    
                    half cloud_a = lerp(cloud_B.a,cloud_F.a,1-lightDir.z);

                    half3 cloud = min(cloud_x,min(cloud_y,cloud_z)) *light.color.xyz;
                    cloud = saturate(cloud*_Brightness);
                    //cloud = cloud * saturate(dot(light.direction,bump));
                    
                    //half3 diffuse = albedo * saturate(dot(lightDir,worldNormal));

                    /*计算Blinn-Phong高光*/
                    //half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos);
                    // half3 halfDir = normalize(viewDir + lightDir);
                    //half3 specular = pow(saturate(dot(halfDir,worldNormal)),_Gloss) * albedo;
                    
                    //return half4(diffuse + UNITY_LIGHTMODEL_AMBIENT.xyz * albedo + specular,albedo.a);
                    return float4(cloud,cloud_a);
                }
            ENDHLSL
        }
    }
}