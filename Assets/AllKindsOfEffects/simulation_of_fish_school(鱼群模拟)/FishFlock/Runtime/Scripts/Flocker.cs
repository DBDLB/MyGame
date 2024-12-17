using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class Flocker : MonoBehaviour
{
    struct BoidData
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector4 individualData;
    };
    
    public bool isComputeShader = true;
    public static bool enable = true;
    public static float fishNumControl = 1;

    [SerializeField] int numFish = 2048;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Mesh mesh;
    [SerializeField] Material Datamaterial;
    [SerializeField] Material material;

    [Header("鱼群设置")]
    [SerializeField] float maxSpeed = 8f;
    [SerializeField] float sdfWeight = 8f;
    [SerializeField] Vector2 fishRandomScale = new Vector2(0.5f, 1.5f);
    [SerializeField] Vector3 offsetFishStart = new Vector3(0f, 0f,0f);
    [SerializeField] float fishNoiseY = 0.5f;
    [Header("摆尾幅度")]
    [Range(0f, 10f)]
    [SerializeField] float fishTailAmplitude = 0.3f;
    [Header("摆尾频率")]
    [Range(0f, 10f)]
    [SerializeField] float fishTailFrequency = 1f;
    [Header("摆尾速度")]
    [SerializeField] float fishTailSpeed = 5f;
    public Texture2D fishSDF;
    
    private float separationRange = 3f;
    private float separationWeight = 6f;
    private float cohesionRange = 6f;
    private float cohesionWeight = 0.2f;
    private float alignmentRange = 5f;
    private float alignmentWeight = 2f;
    
    
    ComputeBuffer boidBuffer;
    ComputeBuffer outputDataBuffer;
    
    private RenderTexture testTextureB;
    private RenderTexture testTextureC;
    private RenderTexture testTextureA;
    private RenderTexture testTextureD;
    private int Frame;
    public int Size = 512;
    bool isInitialized = false;

    Vector3 boxMin;
    Vector3 boxMax;
    private void  CreateRT(out RenderTexture rt)
    {
        rt = RenderTexture.GetTemporary(Size, Size, 0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
    }

    void OnEnable()
    {
        if (enable)
        {
            if (isComputeShader)
            {
                boidBuffer = new ComputeBuffer(numFish, sizeof(float) * 3 * 2 + sizeof(float) * 4);
                outputDataBuffer = new ComputeBuffer(numFish, sizeof(float) * 3 * 3);
                material.EnableKeyword("INSTNCED_INDIRECT");
                material.EnableKeyword("COMPUTE_SHADER_ON");
                InitializeBoids();
            }
            else
            {
                CreateRT(out testTextureB);
                CreateRT(out testTextureC);
                CreateRT(out testTextureA);
                isInitialized = false;
                material.EnableKeyword("INSTNCED_INDIRECT");
                material.DisableKeyword("COMPUTE_SHADER_ON");
                Vector3 center = transform.position + offsetFishStart;
                boxMin = transform.position - transform.localScale / 2;
                boxMax = transform.position + transform.localScale / 2;
                Datamaterial.SetVector("_BoxMin", boxMin);
                Datamaterial.SetVector("_BoxMax", boxMax);
                Datamaterial.SetFloat("_MaxSpeed", maxSpeed / 5.3f);
                Datamaterial.SetFloat("_SDFWeight", sdfWeight - 2);
                Datamaterial.SetFloat("_TimeStep", Time.deltaTime);
                Datamaterial.SetVector("_Center", center);
                Datamaterial.SetVector("_IndividualData", new Vector4(fishRandomScale.x, fishRandomScale.y, 0, 0));
                if (testTextureD)
                {
                    testTextureA = testTextureD;
                }

                Graphics.Blit(null, testTextureA, Datamaterial, 0);
            }
        }
    }



    void UpdateFunctionOnCPU()
    {
        boxMin = transform.position - transform.localScale / 2;
        boxMax = transform.position + transform.localScale / 2;
        Datamaterial.SetVector("_BoxMin",boxMin);
        Datamaterial.SetVector("_BoxMax",boxMax);
        Datamaterial.SetFloat("_MaxSpeed", maxSpeed/ 5.3f);
        Datamaterial.SetFloat("_SDFWeight", sdfWeight-2);
        Datamaterial.SetTexture("_SDF", fishSDF);
        int index = Time.frameCount % 2;
        Datamaterial.SetFloat("_TimeStep", Time.deltaTime);
        Datamaterial.SetFloat("_NoiseScale", fishNoiseY);
        
        Datamaterial.SetTexture("_TestMap",testTextureA);
        testTextureA = index == 0 ? testTextureC : testTextureB;
        Graphics.Blit(null, testTextureA, Datamaterial, 1);
        material.SetTexture("_TestMap",testTextureA);
        material.SetVector("_BoxMin",boxMin);
        material.SetVector("_BoxMax",boxMax);
        material.SetFloat("_FishtailAmplitude",fishTailAmplitude);
        material.SetFloat("_FishtailFrequency",fishTailFrequency);
        material.SetFloat("_FishtailSpeed",fishTailSpeed);
        var bounds = new Bounds(transform.position, transform.localScale);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, numFish);
    }
    
    void UpdateFunctionOnGPU(){
        //draw stuff
        computeShader.SetBuffer(0, "_Boids", boidBuffer);
        computeShader.SetBuffer(0, "_Output", outputDataBuffer);
        computeShader.SetFloat("_TimeStep", Time.deltaTime);
        
        computeShader.SetFloat("_MaxSpeed", maxSpeed);
        computeShader.SetVector("_SACWeight", new Vector4(separationWeight, alignmentWeight, cohesionWeight));
        computeShader.SetVector("_SACRange", new Vector4(separationRange, alignmentRange, cohesionRange));
        computeShader.SetTexture(0, "_SDF", fishSDF);
        computeShader.SetFloat("_NoiseScale", fishNoiseY);
        computeShader.SetFloat("_SDFWeight", sdfWeight);
        
        Vector3 boxMin = transform.position - transform.localScale / 2;
        Vector3 boxMax = transform.position + transform.localScale / 2;
        computeShader.SetVector("_BoxMin", new Vector4(boxMin.x, boxMin.y, boxMin.z, 0.0f));
        computeShader.SetVector("_BoxMax", new Vector4(boxMax.x, boxMax.y, boxMax.z, 0.0f));
        material.SetFloat("_FishtailAmplitude",fishTailAmplitude);
        material.SetFloat("_FishtailFrequency",fishTailFrequency);
        material.SetFloat("_FishtailSpeed",fishTailSpeed);

        

        int groups = Mathf.CeilToInt(numFish / 64f);
        computeShader.Dispatch(0, groups, 1, 1);

        material.SetBuffer("_Boids", outputDataBuffer);
        var bounds = new Bounds(transform.position, transform.localScale);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, numFish);
    }

    void OnDisable()
    {
        if (enable)
        {
            if (isComputeShader)
            {
                boidBuffer.Release();
                boidBuffer = null;

                outputDataBuffer.Release();
                outputDataBuffer = null;
            }
            else
            {
                RenderTexture.ReleaseTemporary(testTextureB);
                RenderTexture.ReleaseTemporary(testTextureC);
                RenderTexture.ReleaseTemporary(testTextureA);
            }
            material.DisableKeyword("INSTNCED_INDIRECT");
            material.DisableKeyword("COMPUTE_SHADER_ON");
        }
    }

    void Update()
    {
        if (enable)
        {
            if (isComputeShader)
            {
                UpdateFunctionOnGPU();
            }
            else
            {

                if (!isInitialized)
                {
                    Vector3 center = transform.position + offsetFishStart;
                    boxMin = transform.position - transform.localScale / 2;
                    boxMax = transform.position + transform.localScale / 2;
                    Datamaterial.SetVector("_BoxMin", boxMin);
                    Datamaterial.SetVector("_BoxMax", boxMax);
                    Datamaterial.SetFloat("_MaxSpeed", maxSpeed / 5.3f);
                    Datamaterial.SetFloat("_SDFWeight", sdfWeight - 2);
                    Datamaterial.SetFloat("_TimeStep", Time.deltaTime);
                    Datamaterial.SetVector("_Center", center);
                    Datamaterial.SetVector("_IndividualData", new Vector4(fishRandomScale.x, fishRandomScale.y, 0, 0));
                    if (testTextureA != null)
                    {
                        Graphics.Blit(null, testTextureA, Datamaterial, 0);
                    }

                    testTextureD = testTextureA;
                    isInitialized = true;
                }
                // material.SetTexture("_TestMap",testTextureA);
                // material.SetVector("_BoxMin",boxMin);
                // material.SetVector("_BoxMax",boxMax);
                // var bounds = new Bounds(Vector3.zero, Vector3.one * 256);
                // Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, numFish);

                UpdateFunctionOnCPU();
            }
        }
    }

    void InitializeBoids(){
        BoidData[] initValue = new BoidData[numFish];
        Vector3 center = transform.position + offsetFishStart;
        for(int i = 0; i < numFish; i++){
            initValue[i].individualData = new Vector3(Random.Range(1, 5), Random.Range(fishRandomScale.x,fishRandomScale.y), Random.Range(0.0f, 1.0f));
            initValue[i].position = center + Random.insideUnitSphere * 3f;
            initValue[i].velocity = Random.onUnitSphere * maxSpeed;
        }
        boidBuffer.SetData(initValue);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
#endif
}
