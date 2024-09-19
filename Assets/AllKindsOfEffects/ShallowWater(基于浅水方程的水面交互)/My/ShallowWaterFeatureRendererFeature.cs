using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShallowWaterFeatureRendererFeature : ScriptableRendererFeature
{

    [System.Serializable]
    public class ShallowWaterSettings
    {
        public Material renderDepthMaterial;
        public ComputeShader shallowWaterComputeShader;
        public int heightMapSize = 512;
        public float damping = 0.99f;
        public float travelSpeed = 0.45f;
        public Transform waterTarget;
        public Camera camera;
        public List<Renderer> renderers;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public ShallowWaterSettings settings = new ShallowWaterSettings();

    private ShallowWaterFeatureRendererFeaturePass shallowWaterPass;

    public override void Create()
    {
        shallowWaterPass = new ShallowWaterFeatureRendererFeaturePass(
            settings.renderDepthMaterial,
            settings.shallowWaterComputeShader,
            settings.heightMapSize,
            settings.damping,
            settings.travelSpeed,
            settings.waterTarget,
            settings.camera,
            settings.renderers
        );

        shallowWaterPass.renderPassEvent = settings.renderPassEvent;
    }

    // 将自定义的Pass添加到渲染队列
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.renderDepthMaterial != null && settings.shallowWaterComputeShader != null)
        {
            var depthTexture = renderer.cameraColorTarget;
            shallowWaterPass.Setup(depthTexture);
            renderer.EnqueuePass(shallowWaterPass);
        }
    }

    class ShallowWaterFeatureRendererFeaturePass : ScriptableRenderPass
    {
        private Material renderDepthMaterial; // 渲染深度材质
        private ComputeShader shallowWaterComputeShader; // 用于浅水模拟的计算着色器
        private RenderTargetIdentifier depthTextureID; // 深度图的目标标识符

        private int heightMapSize;
        private float damping;
        private float travelSpeed;
        private Transform waterTarget;
        private Camera camera;
        private List<Renderer> renderers;

        private int csMainKernel;
        private int csUpdateBufferKernel;

        private ComputeBuffer bufferA;
        private ComputeBuffer bufferB;
        private ComputeBuffer bufferC;

        private Queue<ComputeBuffer> bufferQueue = new Queue<ComputeBuffer>();

        public ShallowWaterFeatureRendererFeaturePass(
            Material renderDepthMaterial,
            ComputeShader shallowWaterComputeShader,
            int heightMapSize,
            float damping,
            float travelSpeed,
            Transform waterTarget,
            Camera camera,
            List<Renderer> renderers)
        {
            this.renderDepthMaterial = renderDepthMaterial;
            this.shallowWaterComputeShader = shallowWaterComputeShader;
            this.heightMapSize = heightMapSize;
            this.damping = damping;
            this.travelSpeed = travelSpeed;
            this.waterTarget = waterTarget;
            this.camera = camera;
            this.renderers = renderers;

            // 初始化计算着色器的Kernel
            csMainKernel = shallowWaterComputeShader.FindKernel("CSMain");
            csUpdateBufferKernel = shallowWaterComputeShader.FindKernel("UpdateBufferCS");
        }

        // 设置渲染的目标纹理
        public void Setup(RenderTargetIdentifier depthTexture)
        {
            depthTextureID = depthTexture;
        }

        // 执行渲染通道的逻辑
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("ShallowWaterPass");

            // 设置渲染目标为深度图
            cmd.SetRenderTarget(depthTextureID);
            cmd.ClearRenderTarget(true, true, Color.black);

            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, projectionMatrix);

            // 渲染每个Renderer
            foreach (var renderer in renderers)
            {
                cmd.DrawRenderer(renderer, renderDepthMaterial);
            }

            // 进行浅水方程的计算
            ComputeBuffer current = bufferQueue.Dequeue();
            ComputeBuffer previous = bufferQueue.Dequeue();
            ComputeBuffer prePrevious = bufferQueue.Dequeue();

            cmd.SetComputeIntParam(shallowWaterComputeShader, "_ShallowWaterSize", heightMapSize);
            cmd.SetComputeFloatParam(shallowWaterComputeShader, "Damping", damping);
            cmd.SetComputeFloatParam(shallowWaterComputeShader, "TravelSpeed", travelSpeed);

            // 将计算缓冲区传递给计算着色器
            cmd.SetComputeBufferParam(shallowWaterComputeShader, csMainKernel, "CurrentBuffer", current);
            cmd.SetComputeBufferParam(shallowWaterComputeShader, csMainKernel, "PrevBuffer", previous);
            cmd.SetComputeBufferParam(shallowWaterComputeShader, csMainKernel, "PrevPrevBuffer", prePrevious);

            cmd.DispatchCompute(shallowWaterComputeShader, csMainKernel, heightMapSize / 8, heightMapSize / 8, 1);

            // 将计算结果传递给水面渲染材质
            waterTarget.GetComponent<Renderer>().sharedMaterial.SetBuffer("_ShallowWaterBuffer", current);
            waterTarget.GetComponent<Renderer>().sharedMaterial.SetInt("_ShallowWaterSize", heightMapSize);

            // 将缓冲区放回队列
            bufferQueue.Enqueue(current);
            bufferQueue.Enqueue(previous);
            bufferQueue.Enqueue(prePrevious);

            // 提交命令
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            // 清理代码，如果需要的话
        }
    }
}