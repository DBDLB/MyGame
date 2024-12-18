Shader "MY_PBR_MultiLight_Glint"
{
    Properties{
		_BaseColor("BaseColor", Color) = (1,1,1,1)
		_SpecularColor("SpecularColor", Color) = (1,1,1,1)
		_MainTex("MainTex",2D) = "white"{}
    	[NoScaleOffset]_MaskMap("MaskMap(Metallic,AO,Smoothness)",2D)="white"{}
    	_Metallic("Metallic",Range(0,1)) = 1
        _Smoothness("Smoothness",Range(0,1)) = 1
//		_SpecularRange("SpecularRange", Range(0,100)) = 50
    	[NoScaleOffset][Normal]_NormalMap("NormalMap",2D)="Bump"{}
    	_NormalScale("NormalScale",Range(0,1))=1
		_TestValue("Test",Range(0,1))=1
		[Toggle(_ADD_LIGHTS)]_AddLights("AddLights", float)=1.0
    	iResolution("Resolution", float) = 1.0
    }
	SubShader
	{
		Tags
		{
			"RenderPipeLine"="UniversalPipeline"
			"RenderType"="Opaque"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
		#include "PbrFunction.hlsl"

		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
		#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
		#pragma multi_compile _ _SHADOWS_SOFT
		#pragma shader_feature _ADD_LIGHTS

		CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _BaseColor;
			float4 _SpecularColor;
			// float _SpecularRange;
			float _TestValue;
		    float _Smoothness;
			float _Metallic;
			float _NormalScale;
			float iResolution;
		CBUFFER_END

		TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);
		TEXTURE2D(_MaskMap);    SAMPLER(sampler_MaskMap);
		TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);

		struct a2v{
			float4 positionOS: POSITION;
			float2 uv: TEXCOORD0;
			float3 normalOS: NORMAL;
			float4 tangent:TANGENT;
		};

		struct v2f{
			float4 positionCS: SV_POSITION;
			float2 uv: TEXCOORD0;
			float3 normalWS: TEXCOORD1;
			float3 positionWS: TEXCOORD2;
			float3 BtangentWS: TEXCOORD3;
			float3 tangentWS: TEXCOORD4;
		};

		struct PBR
		{
			Light light;
			float3 normalWS;
			float3 viewDirWS;
			float3 positionWS;
			float2 uv;
			float3x3 toLocal;
		};

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

		real4 CalculateLight(PBR pbr)
		{

            //Glint
			float3 wi = normalize(mul(pbr.toLocal, normalize(pbr.light.direction)));
            // Observer direction
            float3 wo = normalize(mul(pbr.toLocal, normalize(pbr.viewDirWS)));
            float2 fragCoord = pbr.uv * _MainTex_ST.xy + _MainTex_ST.zw;
            float2 uv = fragCoord/iResolution * 400.0;
            float3 radiance_glint = f_P(wo, wi, uv) * _TestValue;
            //Glint


			// float3 h = normalize(pbr.light.direction+pbr.viewDirWS);
			// float nDotL = max(0, dot(pbr.normalWS, pbr.light.direction));
			// float spec = pow(max(0, dot(h,pbr.normalWS)), _SpecularRange);
			//
			// real4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pbr.uv);
			// real4 lightColor = real4(pbr.light.color, 1.0);
			//
			// real4 diffuse = texColor * nDotL * lightColor;
			// real4 specular = spec * _SpecularColor;
			//
			// real4 color = (diffuse+specular)*pbr.light.shadowAttenuation*pbr.light.distanceAttenuation;
			
			float3 positionWS=pbr.positionWS;
        	real3 Albedo=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,pbr.uv).xyz*_BaseColor.xyz;
        	float4 Mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,pbr.uv);
        	float Metallic=lerp(0,Mask.r,_Metallic);
        	float AO=Mask.g;
        	float smoothness=lerp(0,Mask.a,_Smoothness);
        	float TEMProughness=1-smoothness;//中间粗糙度
        	float roughness=pow(TEMProughness,2);// 粗糙度
			float3 F0=lerp(0.04,Albedo,Metallic);
	
        	Light mainLight=pbr.light;
        	float3 L=normalize(mainLight.direction);
        	float3 V=SafeNormalize(_WorldSpaceCameraPos-positionWS);
        	float3 H=normalize(V+L);
        	float NdotV=max(saturate(dot(pbr.normalWS,V)),0.000001);//不取0 避免除以0的计算错误
        	float NdotL=max(saturate(dot(pbr.normalWS,L)),0.000001);
        	float HdotV=max(saturate(dot(H,V)),0.000001);
        	float NdotH=max(saturate(dot(H,pbr.normalWS)),0.000001);
        	float LdotH=max(saturate(dot(H,L)),0.000001);
			
	//直接光部分
                float D=D_Function(NdotH,roughness);
                //return D;
                float G=G_Function(NdotL,NdotV,roughness);
                //return G;
                float3 F=F_Function(LdotH,F0);
                //return float4(F,1);
                float3 BRDFSpeSection=D*G*F/(4*NdotL*NdotV);
                float3 DirectSpeColor=BRDFSpeSection*mainLight.color*NdotL*PI;
                //return float4(DirectSpeColor,1);
                //高光部分完成 后面是漫反射
                float3 KS=F;
                float3 KD=(1-KS)*(1-Metallic);
                float3 DirectDiffColor=KD*Albedo*mainLight.color*NdotL;//分母要除PI 但是积分后乘PI 就没写
                //return float4(DirectDiffColor,1);
                float3 DirectColor=DirectSpeColor+DirectDiffColor;
                //return float4(DirectColor,1);
    //间接光部分
                float3 SHcolor=SH_IndirectionDiff(pbr.normalWS)*AO;
                float3 IndirKS=IndirF_Function(NdotV,F0,roughness);
                float3 IndirKD=(1-IndirKS)*(1-Metallic);
                float3 IndirDiffColor=SHcolor*IndirKD*Albedo;
                //return float4(IndirDiffColor,1);
                //漫反射部分完成 后面是高光
                float3 IndirSpeCubeColor=IndirSpeCube(pbr.normalWS,V,roughness,AO);
                //return float4(IndirSpeCubeColor,1);
                float3 IndirSpeCubeFactor=IndirSpeFactor(roughness,smoothness,BRDFSpeSection,F0,NdotV);
                float3 IndirSpeColor=IndirSpeCubeColor*IndirSpeCubeFactor;
                //return float4(IndirSpeColor,1);
                float3 IndirColor=IndirSpeColor+IndirDiffColor;
                //return float4(IndirColor,1);
                //间接光部分计算完成
                float4 color=float4((IndirColor+(DirectColor+radiance_glint)*pbr.light.shadowAttenuation*pbr.light.distanceAttenuation),1);
			return color;
		};

		ENDHLSL

		Pass
		{
			Tags{
				"LightMode"="UniversalForward"
			}

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			v2f vert(a2v v)
			{
				v2f o;
				o.positionCS = TransformObjectToHClip(v.positionOS);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				float3 worldNormal = TransformObjectToWorldNormal(v.normalOS);
				o.normalWS = worldNormal;
                float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(worldNormal,worldTangent) * v.tangent.w;
				o.tangentWS = worldTangent;
				o.BtangentWS = worldBinormal;
				o.positionWS = TransformObjectToWorld(v.positionOS);
				return o;
			}

			real4 frag(v2f i):SV_TARGET
			{

				//法线部分得到世界空间法线
                float4 nortex=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv.xy);
                float3 norTS=UnpackNormalScale(nortex,_NormalScale);
                norTS.z=sqrt(1-saturate(dot(norTS.xy,norTS.xy)));
                float3x3 T2W={i.tangentWS.xyz,i.BtangentWS.xyz,i.normalWS.xyz};
                T2W=transpose(T2W);
                float3 Nor=NormalizeNormalPerPixel(mul(T2W,norTS));
				 
				float3 normalWS = Nor;
				float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);

				//Glint
				float3 vertexNormal = i.normalWS;
            	float3 vertexTangent = i.tangentWS;
                // Gram–Schmidt process
                vertexTangent = vertexTangent - (dot(vertexNormal, vertexTangent) / dot(vertexNormal, vertexNormal)) * vertexNormal;
                float3 vertexBinormal = cross(vertexNormal, vertexTangent);

                float3x3  toLocal = float3x3(
                    vertexTangent.x, vertexBinormal.x, vertexNormal.x,
                    vertexTangent.y, vertexBinormal.y, vertexNormal.y,
                    vertexTangent.z, vertexBinormal.z, vertexNormal.z ) ;
				//Glint

				#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
				    half4 shadowMask = inputData.shadowMask;
				#elif !defined (LIGHTMAP_ON)
				    half4 shadowMask = unity_ProbesOcclusion;
				#else
				    half4 shadowMask = half4(1, 1, 1, 1);
				#endif

				real4 color = 0;

				//Calculate Main Light
				Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));

				PBR mainPBR;
				mainPBR.light = light;
				mainPBR.normalWS = normalWS;
				mainPBR.viewDirWS = viewDirWS;
				mainPBR.positionWS = i.positionWS;
				mainPBR.uv = i.uv;
				mainPBR.toLocal = toLocal;
				color+= CalculateLight(mainPBR);
				//float test = 0;

				#if _ADD_LIGHTS
				int addLightsCount = GetAdditionalLightsCount();
				for(int t=0; t<addLightsCount;t++)
				{
					PBR pbr;

					Light light0 = GetAdditionalLight(t, i.positionWS, shadowMask);
					pbr.light = light0;
					pbr.normalWS = normalWS;
					pbr.viewDirWS = viewDirWS;
					pbr.positionWS = i.positionWS;
					pbr.uv = i.uv;
					pbr.toLocal = toLocal;

					color+=CalculateLight(pbr);
				}
				#endif

				return color;
			}
			ENDHLSL
		}
		pass{

			Tags{
				"LightMode"="ShadowCaster"
			}
			HLSLPROGRAM
			#pragma vertex vertShadow
			#pragma fragment fragShadow

			half3 _LightDirection;

			v2f vertShadow(a2v v)
			{
				v2f o;
				o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS.xyz);
				o.uv = v.uv;

				float3 positionWS = ApplyShadowBias(o.positionWS, o.normalWS, _LightDirection);
				o.positionCS = TransformWorldToHClip(positionWS);

				#if UNITY_REVERSED_Z
				    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
				    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif
				return o;
			}
			half4 fragShadow(v2f i):SV_TARGET
			{
				return 0;
			}

			ENDHLSL
		}
		
		Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
	} 
}
