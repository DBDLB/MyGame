/////////////////////////////////////////////////////////////////////////////////////
// core functions
/////////////////////////////////////////////////////////////////////////////////////
half3 RGB2HSV(half3 c)
{
	half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
	half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));

	half d = q.x - min(q.w, q.y);
	half e = 1.0e-10;
	return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

half3 HSV2RGB(half3 c)
{
	half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	half3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

/////////////////////////////////////////////////////////////////////////////////////
// high level helper functions
/////////////////////////////////////////////////////////////////////////////////////

// saturationBoost and valueMul must be within 0~1 value
half3 ApplyHSVChange(half3 originalColor, half hueOffset, half saturationBoost, half valueMul)
{
	return ApplyHSVChange(originalColor,hueOffset,saturationBoost,0);
}
half3 ApplyHSVChange(half3 originalColor, half hueOffset, half saturationBoost, half valueMul, out half3 originalColorHSV)
{
    half3 HSV = RGB2HSV(originalColor);
    originalColorHSV = HSV;
    HSV.x += hueOffset;
    HSV.y = lerp(HSV.y, 1, saturationBoost);
    HSV.z *= valueMul;

    return HSV2RGB(HSV);
}

