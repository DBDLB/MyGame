using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GrabScreenBlurRendererFeature : ScriptableRendererFeature
{
	public enum RTSize
	{
		_128 = 128,
		_256 = 256,
		_512 = 512,
		_1024 = 1024
	}
	
	[Serializable]
	public class Setting
	{
		public ComputeShader MipCS;
		public RTSize rtSize = RTSize._128;
		public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingTransparents;
		//
		// public float blurAmount;
		// public Material blurMaterial;
		
		public void Init()
		{
		#if UNITY_EDITOR
					if (MipCS == null)
					{
						MipCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(
							"Assets/AllKindsOfEffects/PreciousStone(玻璃，矿石，玉石)/ComputeShader/MipOpaqueTexture.compute");
					}
		#endif
		}
	}

	[SerializeField]
	public Setting setting;

        private GrabScreenBlurPass grabScreenBlurPass;
        public RenderTexture MipCameraTexture;
        
        private MipSetPass mipSetPass;

        public override void Create()
        {
	    if (setting == null)
	    {
		    setting = new Setting();
		    setting.Init();
	    }
		grabScreenBlurPass = new GrabScreenBlurPass(setting);
		// grabScreenBlurPass.renderPassEvent = setting.renderEvent;
		
		mipSetPass = new MipSetPass();
        }
        
        const int SHADER_NUMTHREAD_X = 8; //must match compute shader's [numthread(x)]
        const int SHADER_NUMTHREAD_Y = 8; //must match compute shader's [numthread(y)]
        
        int GetRTHeight()
        {
	        // return Mathf.CeilToInt((float)setting.rtSize / SHADER_NUMTHREAD_Y) * SHADER_NUMTHREAD_Y;
	        return Mathf.CeilToInt((float)Screen.height / SHADER_NUMTHREAD_Y) * SHADER_NUMTHREAD_Y;
        }
    
        int GetRTWidth()
        {
	        float aspect = (float)Screen.width / Screen.height;
	        return Mathf.CeilToInt(GetRTHeight() * aspect / (float)SHADER_NUMTHREAD_X) * SHADER_NUMTHREAD_X;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
	        var camera = renderingData.cameraData.camera;

	        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) return;
	        if (camera.cameraType == CameraType.Game && camera.CompareTag("MainCamera") == false) //game视图目前只支持主相机
		        return;
	        
	    	var cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
	    	var rtDescriptor = cameraTextureDescriptor;//new RenderTextureDescriptor((int) rtSize.x, (int) rtSize.y);
	    	rtDescriptor.msaaSamples = 1;
	    	rtDescriptor.mipCount = 0;
	    	rtDescriptor.depthBufferBits = 0;
	    	rtDescriptor.sRGB = false;
	    	rtDescriptor.enableRandomWrite = true;
	    	rtDescriptor.dimension = TextureDimension.Tex2D;
        	
	    	var rtSizeCurrent = new Vector2Int(GetRTWidth(), GetRTHeight());
	    	rtDescriptor.width = (int) rtSizeCurrent.x;
	    	rtDescriptor.height = (int) rtSizeCurrent.y;
	    	rtDescriptor.colorFormat = RenderTextureFormat.ARGB32;
	    	rtDescriptor.mipCount = 7;//更高的层数，对于反射内容没有实际意义
	    	rtDescriptor.useMipMap = true;
	        rtDescriptor.autoGenerateMips = false;
	    	MipCameraTexture = GetTexture(MipCameraTexture, "MipCameraTexture", rtDescriptor, true);
	    	grabScreenBlurPass.Setup(MipCameraTexture,rtSizeCurrent);
	
			renderer.EnqueuePass(grabScreenBlurPass);
			
			mipSetPass.Setup(MipCameraTexture);
			renderer.EnqueuePass(mipSetPass);
        }
        
        private RenderTexture GetTexture(RenderTexture renderTexture, string name, RenderTextureDescriptor renderTextureDescriptor, bool useTrilinear = false)
        {
	        if (renderTexture == null)
	        {
		        renderTexture = CreateRenderTexture(name, renderTextureDescriptor, useTrilinear);
	        }
	        else
	        {
		        if (renderTexture.width != renderTextureDescriptor.width || renderTexture.height != renderTextureDescriptor.height)
		        {
			        renderTexture.Release();
			        renderTexture = CreateRenderTexture(name, renderTextureDescriptor, useTrilinear);
		        }
	        }

	        return renderTexture;
        }
        
        private RenderTexture CreateRenderTexture(string name, RenderTextureDescriptor renderTextureDescriptor, bool useTrilinear = false)
        {
	        var reflectTex = new RenderTexture(renderTextureDescriptor);
	        reflectTex.name = name;
	        reflectTex.enableRandomWrite = true;
	        if (useTrilinear) reflectTex.filterMode = FilterMode.Trilinear;
	        reflectTex.Create();
	        return reflectTex;
        }
        
        
     internal class MipSetPass : ScriptableRenderPass
     {
	     private RenderTexture rt;

	     public void Setup(RenderTexture MipCameraTexture)
	     {
	        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
	        this.rt = MipCameraTexture;
	     }
	     public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
	     {
	        CommandBuffer cmd = CommandBufferPool.Get("Mip Set");
	        // cmd.name = "Mip Set";
     
	        cmd.SetGlobalTexture("_MipMapOpaqueTexture", rt);
	        context.ExecuteCommandBuffer(cmd);
	        CommandBufferPool.Release(cmd);
	     }
    
	     public void Dispose()
	     {
     
	     }
     }

	// render pass
	class GrabScreenBlurPass : ScriptableRenderPass
	{
		private Setting setting;
		private RenderTexture MipCameraTexture;
		private Vector2Int rtSize;

		public GrabScreenBlurPass(Setting setting)
		{
			this.setting = setting;

			profilingSampler = new ProfilingSampler(nameof(GrabScreenBlurPass));
		}
		
		public void Setup(RenderTexture MipCameraTexture, Vector2Int rtSize)
		{
			renderPassEvent = setting.renderEvent;
			this.MipCameraTexture = MipCameraTexture;
			this.rtSize = rtSize;
		}
		
		const int SHADER_NUMTHREAD_X = 8; //must match compute shader's [numthread(x)]
		const int SHADER_NUMTHREAD_Y = 8; //must match compute shader's [numthread(y)]


		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer cmd = CommandBufferPool.Get("GrabScreenBlurRendererFeature");
			
			Blit(cmd,renderingData.cameraData.renderer.cameraColorTarget, MipCameraTexture);
			//mips
			int width = rtSize.x;
			int height = rtSize.y;
			var count = MipCameraTexture.mipmapCount;
			int actualCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Log(Mathf.Max(width, height), 2))); //过低的分辨率降低mip层数
			actualCount = Mathf.Min(actualCount, count);
			
			int kernel_GaussianBlur = setting.MipCS.FindKernel("GaussianBlur");
			width = Mathf.Max(1, width / 2);
			height = Mathf.Max(1, height / 2);
			for (int i = 1; i < actualCount; i++)
			{
				cmd.SetComputeTextureParam(setting.MipCS, kernel_GaussianBlur, "_SSRPMipSource", MipCameraTexture, i - 1);
				cmd.SetComputeTextureParam(setting.MipCS, kernel_GaussianBlur, "_SSRPMipDest", MipCameraTexture, i);
				cmd.SetComputeVectorParam(setting.MipCS, "_RTSize", new Vector4(width, height, 0, 0));
				int x, y;
				x = Mathf.Max(1, Mathf.CeilToInt((int)width / SHADER_NUMTHREAD_X));
				y = Mathf.Max(1, Mathf.CeilToInt((int)height / SHADER_NUMTHREAD_Y));
				cmd.DispatchCompute(setting.MipCS, kernel_GaussianBlur, x, y, 1);
				
				// cmd.SetComputeTextureParam(setting.MipCS, kernel_GaussianBlurVertical, "_SSRPMipSource", MipCameraTexture, i);
				// cmd.SetComputeTextureParam(setting.MipCS, kernel_GaussianBlurVertical, "_SSRPMipDest", MipCameraTexture, i);
				// cmd.SetComputeVectorParam(setting.MipCS, "_RTSize", new Vector4(width, height, 0, 0));
				// // int x, y;
				// // x = Mathf.Max(1, Mathf.CeilToInt((int)width / SHADER_NUMTHREAD_X));
				// // y = Mathf.Max(1, Mathf.CeilToInt((int)height / SHADER_NUMTHREAD_Y));
				// cmd.DispatchCompute(setting.MipCS, kernel_GaussianBlurVertical, x, y, 1);
			}
			
			//schedule command buffer
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	}
}