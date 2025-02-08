using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Underwater : MonoBehaviour
{
    public Camera Targetcamera;
    public Material UnderWaterMat;
    
    [Header("UnderWaterColors")]
    public Gradient gradient = new Gradient();
    private Texture2D gradientMap;
    // public Material WaterDepthMat;
    
    private Camera maincamera;

    private void OnEnable()
    {
        maincamera = Camera.main;
        Targetcamera = this.GetComponent<Camera>();
        RecomputeGradientMap();
    }
    
    void RecomputeGradientMap(){
        //no mip
        gradientMap = new Texture2D(256, 1, TextureFormat.ARGB32, false);
        for(int i = 0; i < 256; i++){
            gradientMap.SetPixel(i, 0, gradient.Evaluate(i / 256.0f));
        }
        gradientMap.Apply();
        gradientMap.wrapMode = TextureWrapMode.Clamp;
    }

    void Getcorners()
    {
        Vector4[] corners = new Vector4[4];
        float Size = Targetcamera.orthographicSize;
        float aspect = Targetcamera.aspect;

        UnderWaterMat.SetFloat("_Size", Size);
        UnderWaterMat.SetFloat("_Aspect", aspect);
        UnderWaterMat.SetFloat("_MainCameraNear", maincamera.nearClipPlane);
        UnderWaterMat.SetTexture("_GradientMap", gradientMap);

        //左下
        corners[0] = maincamera.ViewportToWorldPoint(new Vector3(0.0f, 0.0f, Camera.main.nearClipPlane));
        //右下
        corners[1] = maincamera.ViewportToWorldPoint(new Vector3(1.0f, 0.0f, Camera.main.nearClipPlane));
        //左上
        corners[2] = maincamera.ViewportToWorldPoint(new Vector3(0.0f, 1.0f, Camera.main.nearClipPlane));
        //石上
        corners[3] = maincamera.ViewportToWorldPoint(new Vector3(1.0f, 1.0f, Camera.main.nearClipPlane));
        UnderWaterMat.SetVectorArray("_CameraCorners", corners);
        
        Vector4[] FarCorners = new Vector4[4];
        FarCorners[0] = maincamera.ViewportToWorldPoint(new Vector3(0.0f, 0.0f, Camera.main.farClipPlane));
        FarCorners[1] = maincamera.ViewportToWorldPoint(new Vector3(1.0f, 0.0f, Camera.main.farClipPlane));
        FarCorners[2] = maincamera.ViewportToWorldPoint(new Vector3(0.0f, 1.0f, Camera.main.farClipPlane));
        FarCorners[3] = maincamera.ViewportToWorldPoint(new Vector3(1.0f, 1.0f, Camera.main.farClipPlane));
        UnderWaterMat.SetVectorArray("_CameraFarCorners", FarCorners);
        UnderWaterMat.SetVector("_CameraPos", maincamera.transform.position);
        // Debug.Log("Half height of the camera viewport in world units:" + corners[0]+"------"+corners[1]+"--------"+maincamera.nearClipPlane);
        
        
        // Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(Targetcamera.projectionMatrix, false);
        // var vMattrix = projectionMatrix;
        // var pMattrix = Targetcamera.worldToCameraMatrix;
        // Matrix4x4 vpMatrix = vMattrix * pMattrix;
        // UnderWaterMat.SetMatrix("_InvP", pMattrix.inverse);
        
        Matrix4x4 viewMatrix = Targetcamera.worldToCameraMatrix;
        Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(Targetcamera.projectionMatrix, false);
        Matrix4x4 vpMatrix = projectionMatrix * viewMatrix;
        Matrix4x4 inverseVPMatrix = vpMatrix.inverse;
        UnderWaterMat.SetMatrix("_InvVP", inverseVPMatrix);
    }

    private void Update()
    {
        Getcorners();
        RecomputeGradientMap();
    }
}
