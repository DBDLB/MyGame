Shader "Unlit/test"
{
    Properties
    {
        [Header(Unlit)][Space(10)]
        [SinglelineTexture(_BaseColor)][MainTexture] _BaseMap("Albedo(rgba)", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [Header(EdgeAlphaFalloff)][Space(10)]
        [Toggle(USE_EDGE_ALPHA_FALLOFF)]_FallOffEnable("开启底部渐变", float) = 0
        [ShowIf(_FallOffEnable)]_EdgeAlphaFadeDistance("Edge Alpha Fade Distance", Range(0,3)) = 0.5
        _LightColorIntensity("光照颜色影响强度", Range(0.0, 1.0)) = 0

        [Header(RenderingSettings)][Space(10)]
        [RenderingMode] _Mode("混合模式", Int) = 0
        [ShowIf(_ALPHATEST_ON)] _Cutoff("不透明蒙版剪辑值", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("剔除模式", Int) = 2
        
        _Varnished("Varnished", Float) = 0.0
        iResolution("Resolution", Vector) = (1,1,1,1)

        [HideInInspector] _SrcBlend ("__src", Int) = 1.0
        [HideInInspector] _DstBlend ("__dst", Int) = 0.0
        [HideInInspector] _ZWrite ("__zw", Int) = 1.0





        [Header(Fog)][Space(10)]
        _FogToggle ("雾效强度", Range(0.0,1.0)) = 1.0

        
    }
    
    HLSLINCLUDE
     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    // 定义常量
            #define PG2020W 32u
            #define PG2020H 18u
            #define ALPHA_X 0.5
            #define ALPHA_Y 0.5
            #define MICROFACETRELATIVEAREA 1.
            #define LOGMICROFACETDENSITY 5.
            #define MAXANISOTROPY 8.
            #define VARNISHED true
            #define ALPHA_DIC 0.5
            #define N 999999
            #define NLEVELS 8
            #define DISTRESOLUTION 32
            #define PI 3.141592
            #define IPI 0.318309
            #define ISQRT2 0.707106



            // 定义 uint 数组为 StructuredBuffer 或者 ConstantBuffer
            // StructuredBuffer<uint> pg2020_bitfield;
            uint pg2020_bitfield[18] = {0x0u,0x0u,0x0u,0x003e7c00u,0x00024400u,0x00327c00u,0x00220400u,0x003e0400u,0x0u,0x0u,0x30e30e0u,0x4904900u,0x49e49e0u,0x4824820u,0x31e31e0u,0x0u,0x0u,0x0u};

            // 定义一个 bool 函数
            bool pg2020(uint x, uint y)
            {
                uint id = x + (PG2020H-1u-y)*PG2020W;
                if (id >= PG2020W*PG2020H) return false;
                return 0u != (pg2020_bitfield[id/32u] & (1u << (id & 31u)));
            }

            // texel 函数
            float texel(int s, int t)
            {
                if (s < 0 || s >= int(PG2020W) || t < 0 || t >= int(PG2020H) || pg2020(uint(s), uint(t)))
                    return 0.0;
                return 1.0;
            }

            // 三角过滤函数
            float pg2020TriangleFilter(float2 st)
            {
                float s = st.x * float(PG2020W) - 0.5;
                float t = st.y * float(PG2020H) - 0.5;
                int s0 = int(floor(s));
                int t0 = int(floor(t));
                float ds = s - float(s0);
                float dt = t - float(t0);
                return (1.0 - ds) * (1.0 - dt) * texel(s0, t0) +
                       (1.0 - ds) * dt * texel(s0, t0 + 1) +
                       ds * (1.0 - dt) * texel(s0 + 1, t0) +
                       ds * dt * texel(s0 + 1, t0 + 1);
            }

            // Beckmann P22 函数
            float p22_beckmann_anisotropic(float x, float y, float alpha_x, float alpha_y)
            {
                float x_sqr = x * x;
                float y_sqr = y * y;
                float sigma_x = alpha_x * 0.707106; // ISQRT2
                float sigma_y = alpha_y * 0.707106;
                float sigma_x_sqr = sigma_x * sigma_x;
                float sigma_y_sqr = sigma_y * sigma_y;
                return exp(-0.5 * ((x_sqr / sigma_x_sqr) + (y_sqr / sigma_y_sqr))) / (2.0 * 3.141592 * sigma_x * sigma_y);
            }

            // 光照 NDF 函数
            float ndf_beckmann_anisotropic(float3 omega_h, float alpha_x, float alpha_y)
            {
                float slope_x = - (omega_h.x / omega_h.z);
                float slope_y = - (omega_h.y / omega_h.z);
                float cos_theta = omega_h.z;
                float cos_2_theta = omega_h.z * omega_h.z;
                float cos_4_theta = cos_2_theta * cos_2_theta;
                float beckmann_p22 = p22_beckmann_anisotropic(slope_x, slope_y, alpha_x, alpha_y);
                return beckmann_p22 / cos_4_theta;
            }

            // Fresnel 函数
            float3 fresnel_schlick(float wo_dot_wh, float3 F0)
            {
                return F0 + (1.0 - F0) * pow(1.0 - wo_dot_wh, 5.0);
            }

            // Specular 函数
            float3 f_specular(float3 wo, float3 wi)
            {
                if (wo.z <= 0.0) return float3(0.0, 0.0, 0.0);
                if (wi.z <= 0.0) return float3(0.0, 0.0, 0.0);
                float3 wh = normalize(wo + wi);
                if (wh.z <= 0.0) return float3(0.0, 0.0, 0.0);
                if (dot(wo, wh) <= 0.0 || dot(wi, wh) <= 0.0)
                return (0.0);

                float wi_dot_wh = clamp(dot(wi, wh), 0.0, 1.0);
                float D = ndf_beckmann_anisotropic(wh, 0.1, 0.1);
                float G1wowh = min(1.0, 2.0 * wh.z * wo.z / dot(wo, wh));
                float G1wiwh = min(1.0, 2.0 * wh.z * wi.z / dot(wi, wh));
                float G = G1wowh * G1wiwh;

                float3 F = fresnel_schlick(wi_dot_wh, float3(1.0, 1.0, 1.0));
                return (D * F * G) / (4.0 * wo.z);
            }

            // Diffuse 函数
            float3 f_diffuse(float3 wo, float3 wi)
            {
                if (wo.z <= 0.0) return float3(0.0, 0.0, 0.0);
                if (wi.z <= 0.0) return float3(0.0, 0.0, 0.0);
                return float3(0.8, 0.0, 0.0) * 0.318309 * wi.z; // IPI = 0.318309
            }

            float erfinv(float x)
            {
                float w, p;
                w = -log((1.0 - x) * (1.0 + x));
                if (w < 5.000000)
                {
                    w = w - 2.500000;
                    p = 2.81022636e-08;
                    p = 3.43273939e-07 + p * w;
                    p = -3.5233877e-06 + p * w;
                    p = -4.39150654e-06 + p * w;
                    p = 0.00021858087 + p * w;
                    p = -0.00125372503 + p * w;
                    p = -0.00417768164 + p * w;
                    p = 0.246640727 + p * w;
                    p = 1.50140941 + p * w;
                }
                else
                {
                    w = sqrt(w) - 3.000000;
                    p = -0.000200214257;
                    p = 0.000100950558 + p * w;
                    p = 0.00134934322 + p * w;
                    p = -0.00367342844 + p * w;
                    p = 0.00573950773 + p * w;
                    p = -0.0076224613 + p * w;
                    p = 0.00943887047 + p * w;
                    p = 1.00167406 + p * w;
                    p = 2.83297682 + p * w;
                }
                return p * x;
            }

            float hashIQ(uint n)
            {
                // integer hash copied from Hugo Elias
                n = (n << 13U) ^ n;
                n = n * (n * n * 15731U + 789221U) + 1376312589U;
                return float(n & 0x7fffffffU) / float(0x7fffffff);
            }

            int pyramidSize(int level)
            {
                return int(pow(2., float(NLEVELS - 1 - level)));
            }
            
            float normalDistribution1D(float x, float mean, float std_dev) {
                float xMinusMean = x - mean;
                float xMinusMeanSqr = xMinusMean * xMinusMean;
                return exp(-xMinusMeanSqr / (2. * std_dev * std_dev)) /
                       (std_dev * 2.506628);
                // 2.506628 \approx sqrt(2 * \pi)
            }

            float sampleNormalDistribution(float U, float mu, float sigma)
            {
                float x = sigma * 1.414213f * erfinv(2.0f * U - 1.0f) + mu;
                return x;
            }

            float P_procedural(float x, int i, int level)
            {
            // We use even functions
            x = abs(x);
            // After 4 standard deviation sigma, we consider that the distribution equals zero
            float sigma_dist_4 = 4. * ALPHA_DIC / 1.414214; // alpha_dist = 0.5 so sigma_dist \approx 0.3535 (0.5 / sqrt(2))
            if(x >= sigma_dist_4) return 0.;
            
            int nMicrofacetsCurrentLevel = int(pow(2., float(level)));
            float density = 0.;
            // Dictionary should be precomputed, but we cannot use memory with Shadertoy
            // So we generate it on the fly with a very limited number of lobes
            nMicrofacetsCurrentLevel = min(32, nMicrofacetsCurrentLevel);
            
            for (int n = 0; n < nMicrofacetsCurrentLevel; ++n) {
                
                float U_n = hashIQ(uint(i*7333+n*5741));
                // alpha roughness equals sqrt(2) * RMS roughness
                //     ALPHA_DIC     =   1.414214 * std_dev
                // std_dev = ALPHA_DIC / 1.414214 
                float currentMean = sampleNormalDistribution(U_n, 0., ALPHA_DIC / 1.414214);
                density += normalDistribution1D(x, currentMean, 0.05) +
                           normalDistribution1D(-x, currentMean, 0.05);
            }
            // 0.5 comes from that in each loop iteration, we sum two PDFs.
            // This value is essential for P_procedural to be a PDF (integral equal one).
            return density / float(nMicrofacetsCurrentLevel) * 0.5;
            }

            float P22_theta_alpha(float2 slope_h, int l, int s0, int t0)
            {
                
                // Coherent index
                // Eq. 18, Alg. 3, line 1
                s0 *= 1 << l;
                t0 *= 1 << l;
            
                // Seed pseudo random generator
                // Alg. 3, line 2
                int rngSeed = s0 + 1549 * t0;
            
                // Alg.3, line 3
                float uMicrofacetRelativeArea = hashIQ(uint(rngSeed) * 13U);
                // Discard cells by using microfacet relative area
                // Alg.3, line 4
                if (uMicrofacetRelativeArea > MICROFACETRELATIVEAREA)
                    return 0.f;
            
                // Number of microfacets in a cell
                // Alg. 3, line 5
                float n = pow(2., float(2 * l - (2 * (NLEVELS - 1))));
                n *= exp(LOGMICROFACETDENSITY);
            
                // Corresponding continuous distribution LOD
                // Alg. 3, line 6
                float l_dist = log(n) / 1.38629; // 2. * log(2) = 1.38629
                
                // Alg. 3, line 7
                float uDensityRandomisation = hashIQ(uint(rngSeed) * 2171U);
            
                // Fix density randomisation to 2 to have better appearance
                // Notation in the paper: \zeta
                float densityRandomisation = 2.;
                
                // Sample a Gaussian to randomise the distribution LOD around the distribution level l_dist
                // Alg. 3, line 8
                l_dist = sampleNormalDistribution(uDensityRandomisation, l_dist, densityRandomisation);
            
                // Alg. 3, line 9
                int l_disti = clamp(int(round(l_dist)), 0, NLEVELS);
            
                // Alg. 3, line 10
                if (l_disti == NLEVELS)
                    return p22_beckmann_anisotropic(slope_h.x, slope_h.y, ALPHA_X, ALPHA_Y);
            
                // Alg. 3, line 13
                float uTheta = hashIQ(uint(rngSeed));
                float theta = 2.0 * PI * uTheta;
            
                // Uncomment to remove random distribution rotation
                // Lead to glint alignments with a small N
                // theta = 0.;
            
                float cosTheta = cos(theta);
                float sinTheta = sin(theta);
                
                float2 scaleFactor = float2(ALPHA_X / ALPHA_DIC,
                                        ALPHA_Y / ALPHA_DIC);
            
                // Rotate and scale slope
                // Alg. 3, line 16
                slope_h = float2(slope_h.x * cosTheta / scaleFactor.x + slope_h.y * sinTheta / scaleFactor.y,
                               -slope_h.x * sinTheta / scaleFactor.x + slope_h.y * cosTheta / scaleFactor.y);
            
                // Alg. 3, line 17
                float u1 = hashIQ(uint(rngSeed) * 16807U);
                float u2 = hashIQ(uint(rngSeed) * 48271U);
            
                // Alg. 3, line 18
                int i = int(u1 * float(N));
                int j = int(u2 * float(N));
                
                float P_i = P_procedural(slope_h.x, i, l_disti);
                float P_j = P_procedural(slope_h.y, j, l_disti);
            
                // Alg. 3, line 19
                return P_i * P_j / (scaleFactor.x * scaleFactor.y);
            }

            float P22_floorP(int l, float2 slope_h, float2 st, float2 dst0, float2 dst1)
            {
                // Convert surface coordinates to appropriate scale for level
                float pyrSize = float(pyramidSize(l));
                st[0] = st[0] * pyrSize - 0.5f;
                st[1] = st[1] * pyrSize - 0.5f;
                dst0[0] *= pyrSize;
                dst0[1] *= pyrSize;
                dst1[0] *= pyrSize;
                dst1[1] *= pyrSize;
            
                // Compute ellipse coefficients to bound filter region
                float A = dst0[1] * dst0[1] + dst1[1] * dst1[1] + 1.;
                float B = -2. * (dst0[0] * dst0[1] + dst1[0] * dst1[1]);
                float C = dst0[0] * dst0[0] + dst1[0] * dst1[0] + 1.;
                float invF = 1. / (A * C - B * B * 0.25f);
                A *= invF;
                B *= invF;
                C *= invF;
            
                // Compute the ellipse's bounding box in texture space
                float det = -B * B + 4. * A * C;
                float invDet = 1. / det;
                float uSqrt = sqrt(det * C), vSqrt = sqrt(A * det);
                int s0 = int(ceil(st[0] - 2. * invDet * uSqrt));
                int s1 = int(floor(st[0] + 2. * invDet * uSqrt));
                int t0 = int(ceil(st[1] - 2. * invDet * vSqrt));
                int t1 = int(floor(st[1] + 2. * invDet * vSqrt));
            
                // Scan over ellipse bound and compute quadratic equation
                float sum = 0.f;
                float sumWts = 0.;
                int nbrOfIter = 0;
            
                for (int it = t0; it <= t1; ++it)
                {
                    float tt = float(it) - st[1];
                    for (int is = s0; is <= s1; ++is)
                    {
                        float ss = float(is) - st[0];
                        // Compute squared radius and filter SDF if inside ellipse
                        float r2 = A * ss * ss + B * ss * tt + C * tt * tt;
                        if (r2 < 1.)
                        {
                            // Weighting function used in pbrt-v3 EWA function
                            float alpha = 2.;
                            float W_P = exp(-alpha * r2) - exp(-alpha);
                            // Alg. 2, line 3
                            sum += P22_theta_alpha(slope_h, l, is, it) * W_P;
                            
                            sumWts += W_P;
                        }
                        nbrOfIter++;
                        // Guardrail (Extremely rare case.)
                        if (nbrOfIter > 100)
                            break;
                    }
                    // Guardrail (Extremely rare case.)
                    if (nbrOfIter > 100)
                        break;
                }
                return sum / sumWts;
            }
    

            float3 f_P(float3 wo, float3 wi, float2 uv)
            {
                // 如果 wo 或 wi 的 z 分量小于等于 0，直接返回 (0, 0, 0)
                if (wo.z <= 0.0)
                    return float3(0.0, 0.0, 0.0);
                if (wi.z <= 0.0)
                    return float3(0.0, 0.0, 0.0);
            
                // 计算半向量 wh
                float3 wh = normalize(wo + wi);
                if (wh.z <= 0.0)
                    return float3(0.0, 0.0, 0.0);
            
                // 如果局部掩蔽和阴影存在，直接返回 (0, 0, 0)
                if (dot(wo, wh) <= 0.0 || dot(wi, wh) <= 0.0)
                    return float3(0.0, 0.0, 0.0);
            
                // 计算斜率
                float2 slope_h = float2(-wh.x / wh.z, -wh.y / wh.z);
                float2 texCoord = uv;
            
                float D_P = 0.0;
                float P22_P = 0.0;
            
                // 计算纹理偏导数
                float2 dst0 = ddx(texCoord);
                float2 dst1 = ddy(texCoord);
            
                // 计算椭圆的主轴和副轴
                float dst0Length = length(dst0);
                float dst1Length = length(dst1);
            
                // 交换 dst0 和 dst1 以保证 dst0 的长度大于 dst1
                if (dst0Length < dst1Length)
                {
                    float2 tmp = dst0;
                    dst0 = dst1;
                    dst1 = tmp;
                }
                float majorLength = length(dst0);
                float minorLength = length(dst1);
            
                // 限制椭圆的离心率
                if (minorLength * MAXANISOTROPY < majorLength && minorLength > 0.0)
                {
                    float scale = majorLength / (minorLength * MAXANISOTROPY);
                    dst1 *= scale;
                    minorLength *= scale;
                }
            
                // 如果没有足迹，计算 Cook-Torrance BRDF
                if (minorLength == 0.0)
                {
                    D_P = ndf_beckmann_anisotropic(wh, ALPHA_X, ALPHA_Y);
                }
                else
                {
                    // 选择 LOD
                    float l = max(0.0, float(NLEVELS) - 1.0 + log2(minorLength));
                    int il = int(floor(l));
            
                    // 计算权重 w
                    float w = l - float(il);
            
                    // 计算 P22_P
                    P22_P = lerp(P22_floorP(il, slope_h, texCoord, dst0, dst1),
                                 P22_floorP(il + 1, slope_h, texCoord, dst0, dst1),
                                 w);
            
                    // 计算 D_P
                    D_P = P22_P / (wh.z * wh.z * wh.z * wh.z);
                }
            
                // 计算 V-cavity 掩蔽和阴影
                float G1wowh = min(1.0, 2.0 * wh.z * wo.z / dot(wo, wh));
                float G1wiwh = min(1.0, 2.0 * wh.z * wi.z / dot(wi, wh));
                float G = G1wowh * G1wiwh;
            
                // Fresnel 设置为 1.0 简化计算，但可以使用真实的 Fresnel 项
                float3 F = float3(1.0, 1.0, 1.0);
            
                // 计算最终结果
                return (F * G * D_P) / (4.0 * wo.z);
            }


          	float4 _BaseMap_ST;
      	    half4 _BaseColor;
      	    half _Cutoff;
      	    float _EdgeAlphaFadeDistance;
      	    half _LightColorIntensity;
      	    half _FogToggle;
            half _Varnished;
            float4 iResolution;



    ENDHLSL
    
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            
            Cull[_Cull]
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 4.5
            
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            struct appdata
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                half4 tangentOS : TANGENT;
            	half4 normalOS : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS  : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);  SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_heightMap);  SAMPLER(sampler_heightMap);


            float SampleSceneDepth(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
            }
            
            float GetDepthFade(float3 positionWS, float Distance)
            {
                float4 ScreenPosition = ComputeScreenPos(TransformWorldToHClip(positionWS));
                float depth = LinearEyeDepth(SampleSceneDepth(ScreenPosition.xy / ScreenPosition.w).r, _ZBufferParams);
                return saturate((depth - ScreenPosition.w) / Distance);
            }

            void AthenaAlphaDiscard(real alpha, real cutoff, real offset = 0.0h)
            {
                #ifdef _ALPHATEST_ON
                    clip(alpha - cutoff + offset);
                #endif
            }
            
            
            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionWS = vertexInput.positionWS;
                o.positionCS = vertexInput.positionCS;
                o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS.xyz);
                o.uv = TRANSFORM_TEX(v.texcoord, _BaseMap);
                return o;
            }

            float4 frag (v2f i) : SV_Target0
            {
                // Light intensity
                float3 lightIntensity = (100000.0);

                float2 fragCoord = i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                float iTime = _Time.y;
                // Texture position
                float2 uv = fragCoord/iResolution.y * 400.;
                
                // Vertex position
                float3 vertexPos = float3(fragCoord - iResolution.xy/2., 0.);
                
                // Light position (varies over time)
                float x_i = cos(iTime*0.6) * iResolution.x / 2.;
                float y_i = cos(iTime) * iResolution.y / 2.;
                float3 lightPos = float3(x_i, y_i, 100);
                
                // Camera position
                float3 cameraPos = float3(0, 0, 100);
                
                // Compute normal from PG2020 heightfield
                float diff = 10.;
                float hPG2020sm1t0 = pg2020TriangleFilter(float2((fragCoord.x - diff)/iResolution.x, (fragCoord.y)/iResolution.y));
                float hPG2020s1t0 = pg2020TriangleFilter(float2((fragCoord.x + diff)/iResolution.x, (fragCoord.y)/iResolution.y));
                float hPG2020s0tm1 = pg2020TriangleFilter(float2((fragCoord.x)/iResolution.x, (fragCoord.y - diff)/iResolution.y));
                float hPG2020s0t1 = pg2020TriangleFilter(float2((fragCoord.x)/iResolution.x, (fragCoord.y + diff)/iResolution.y));
                float2 slope = float2((hPG2020s1t0 - hPG2020sm1t0)/2.,
                                  (hPG2020s0t1 - hPG2020s0tm1)/2.);
                slope *= 4.;
                float3 vertexNormal = float3(-slope.x, -slope.y, 1.) / sqrt(slope.x*slope.x+slope.y*slope.y+1.);
                
                float3 vertexTangent = float3(1., 0., 0.);
                // Gram–Schmidt process
                vertexTangent = vertexTangent - (dot(vertexNormal, vertexTangent) / dot(vertexNormal, vertexNormal)) * vertexNormal;
                float3 vertexBinormal = cross(vertexNormal, vertexTangent);
                
                // Matrix for transformation to tangent space
                float3x3  toLocal = float3x3(
                    vertexTangent.x, vertexBinormal.x, vertexNormal.x,
                    vertexTangent.y, vertexBinormal.y, vertexNormal.y,
                    vertexTangent.z, vertexBinormal.z, vertexNormal.z ) ;
                
                // Incident direction
                float3 wi = normalize(mul(toLocal, normalize(lightPos - vertexPos)));
                // Observer direction
                float3 wo = normalize(mul(toLocal, normalize(cameraPos - vertexPos)));
                
                float3 radiance_glint = (0.);
                float3 radiance_diffuse = (0.);
                float3 radiance = (0.);
                
                float distanceSquared = distance(vertexPos, lightPos);
                distanceSquared *= distanceSquared;
                float3 Li = lightIntensity / distanceSquared;
                
                radiance_diffuse = f_diffuse(wo, wi) * Li;
                
                // Call our physically based glinty BRDF
                radiance_glint = f_P(wo, wi, uv) * Li;
                
                radiance = 0.33*radiance_diffuse + float3(0.13f,0.,0.);
                    
                radiance += 0.5*radiance_glint;
                if(VARNISHED){
                    radiance += 0.17 * f_specular(wo, wi) * Li;
                }
                return float4(radiance_diffuse,1);
                // Gamma
                radiance = pow(radiance, (1.0 / 2.2));
            
                // Output to screen
                float4 fragColor = float4(radiance, 1.0);

                return fragColor;
                }
                ENDHLSL
        }
    }
}           
