using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorTint : VolumeComponent
{
    //【设置颜色参数】
    public ColorParameter colorChange = new ColorParameter(Color.white, true);//如果有两个true,则为HDR设置
    public FloatParameter _startRange = new FloatParameter(0f, true);
    public FloatParameter _endRange = new FloatParameter(0f, true);
    public FloatParameter _scanLineInterval = new FloatParameter(0f, true);
    public FloatParameter _scanLineWidth = new FloatParameter(0f, true);
    public FloatParameter _scanLineBrightness = new FloatParameter(0f, true);
    public FloatParameter _centerFadeout = new FloatParameter(0f, true);
    public Vector3Parameter _scanCenter = new Vector3Parameter(new Vector3(0, 0, 0), true);
    public Vector3Parameter _SceneJitterDirection = new Vector3Parameter(new Vector3(0, 0, 0), true);
    public FloatParameter _sceneJitterStrength = new FloatParameter(0f, true);
    public FloatParameter _flickerStrength = new FloatParameter(0f, true);
    public FloatParameter _useSceneJitter = new FloatParameter(0f, true);
    public FloatParameter _noiseStrength = new FloatParameter(0f, true);
}
