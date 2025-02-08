using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UnderwaterVolume : VolumeComponent, IPostProcessComponent
{
    public BoolParameter IsUnderwater = new BoolParameter(false);
    public bool IsActive() => this.IsUnderwater.value;

    public bool IsTileCompatible() => false;
}
