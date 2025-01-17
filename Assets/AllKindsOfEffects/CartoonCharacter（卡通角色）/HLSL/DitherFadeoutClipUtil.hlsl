void DoDitherFadeoutClip(float2 SV_POSITIONxy, float ditherOpacity)
{
    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(SV_POSITIONxy.x) % 4) * 4 + uint(SV_POSITIONxy.y) % 4;
    clip(ditherOpacity - DITHER_THRESHOLDS[index]);
}

