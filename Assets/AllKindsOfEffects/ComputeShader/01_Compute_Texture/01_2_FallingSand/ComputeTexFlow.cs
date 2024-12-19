using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ComputeTexFlow : MonoBehaviour 
{
	public ComputeShader shader;
	private Texture2D initTex;
	public Material _mat;
	public Collider mc;

	private int size_X;
	private int size_Y;
	
	private int _kernel;
	private Vector2Int dispatchCount;


#if UNITY_EDITOR
	UnityEditor.TextureImporterPlatformSettings tex0_setting;
#endif
	
	public static RenderTexture tex;
	//Initial texture
	// R = 1 is particle
	// G = no meaning now
	// B = 1 is obstacle

    //Mouse input
    private Camera cam;
    private RaycastHit hit;
    private Vector2 mousePos;
    private Vector2 defaultposition = new Vector2(-9, -9); //make it far away
	public static int mouseMode = 0;
	public static int brushSize = 2;
	
	//动物
	private List<Original_Animal> animals = new List<Original_Animal>();
	
	//元素
	private Elemental element;

	void Start () 
	{
		Application.targetFrameRate = 180;//锁定最大帧率为180帧
#if UNITY_EDITOR
		//设置纹理可读写
		// tex0_setting = TextureTool.SetTextureReadableAndCancelNormalMap(initTex);
		// initTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(UnityEditor.AssetDatabase.GetAssetPath(initTex));
#endif
		//创建元素
		element = new Elemental();
		
		//创建纹理
		CreateTexture2D();
		
		//For mouse input
        cam = Camera.main;
	
        size_X = initTex.width;
        size_Y = initTex.height;
		_kernel = shader.FindKernel ("CSMain");

		tex = new RenderTexture (initTex.width, initTex.height, 0);
		tex.wrapMode = TextureWrapMode.Clamp;
		tex.filterMode = FilterMode.Point;
		tex.enableRandomWrite = true;
		tex.Create ();
		

		
		//初始化纹理
		InitializeTexture();
		
		//创建生物
		CreateAnimal();
		
		Graphics.Blit(initTex, tex);
		
		
		_mat.SetTexture ("_MainTex", tex);
		shader.SetTexture (_kernel, "Result", tex);
		shader.SetInt("_Size_X",size_X);
		shader.SetInt("_Size_Y",size_Y);

        uint threadX = 0;
        uint threadY = 0;
        uint threadZ = 0;
        shader.GetKernelThreadGroupSizes(_kernel, out threadX, out threadY, out threadZ);
		dispatchCount.x = Mathf.CeilToInt(size_X / threadX);
		dispatchCount.y = Mathf.CeilToInt(size_Y / threadY);
		
	}

	private int textureWidth;
	private int textureHeight;
	private void CreateTexture2D()
	{
		Texture2D tex_test = null;
		if (Resources.Load<Texture2D>("LittleWorld") == null)
		{
			int screenWidth = Screen.width;
			int screenHeight = Screen.height;
			float aspectRatio = (float)screenWidth / screenHeight;
			textureWidth = screenWidth / 8; // 将纹理的宽度设置为屏幕宽度的一半
			textureHeight = Mathf.RoundToInt(textureWidth / aspectRatio); // 根据屏幕的长宽比计算出纹理的高度
			
			tex_test = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
			bool tex_testIsReadable = tex_test.isReadable;
			for (int y = 0; y < tex_test.height; ++y)
			{
				for (int x = 0; x < tex_test.width; ++x)
				{
					Color color = new Color(x / (float)tex_test.width, y / (float)tex_test.height, 1.0f,
						0.5f); // A通道设置为0.5，表示半透明
					tex_test.SetPixel(x, y, color);
				}
			}

			tex_test.Apply(false);
			byte[] bytes = tex_test.EncodeToPNG();
			string relativePath = Application.dataPath + "/Resources/LittleWorld.png";
			System.IO.File.WriteAllBytes(relativePath, bytes);
		}

		if (tex_test != null)
		{
			initTex = tex_test;
			bool initTexIsReadable =initTex.isReadable;
			textureWidth = initTex.width;
			textureHeight = initTex.height;
		}
		else
		{
			Texture2D LoadTexture = Resources.Load<Texture2D>("LittleWorld");
			initTex = new Texture2D(LoadTexture.width, LoadTexture.height, TextureFormat.ARGB32, false);
			Graphics.CopyTexture(LoadTexture, initTex);
			bool initTexIsReadable =initTex.isReadable;
			initTex.Apply();
			textureWidth = initTex.width;
			textureHeight = initTex.height;
			
		}

		// initTex = tex_test;
	}
	private void InitializeTexture()
	{
		for (int y = 0; y < initTex.height; ++y)
		{
			for (int x = 0; x < initTex.width; ++x)
			{
				initTex.SetPixel(x, y, Color.black);
			}
		}
		initTex.Apply();
	}
	
	private Color IntArrayTOColor(int[] intArray)
	{
		return new Color(intArray[0] / 255f, intArray[1] / 255f, intArray[2] / 255f, intArray[3] / 255f);
	}
	
	private void CreateAnimal()
	{
		Original_Animal animal = new Original_Animal(new Vector2Int(initTex.width/2,initTex.width/2));
		animals.Add(animal);
		
		initTex.SetPixel(animal.positionHead.x, animal.positionHead.y, Color.red);
		initTex.SetPixel(animal.positionBody_A.x, animal.positionBody_A.y, Color.red);
		initTex.SetPixel(animal.positionBody_B.x, animal.positionBody_B.y, Color.red);
		// var pixels = initTex.GetPixels();
		//
		// pixels[animal.position.x] = Color.red;  // 设置第一个像素为红色
		// pixels[animal.position.y] = Color.red;  // 设置第二个像素为红色
		// for (int y = 0; y < initTex.height; ++y)
		// {
		// 	for (int x = 0; x < initTex.width; ++x)
		// 	{
		// 		initTex.SetPixel(x, y, pixels[y * initTex.width + x]);
		// 	}
		// }
		initTex.Apply();
	}

	private void SaveToTexture()
	{
		RenderTexture.active = tex;
		initTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
		initTex.Apply();
		RenderTexture.active = null;
	}
	
	private System.Random rng = new System.Random();
	// 定义四个方向
	Vector2Int[] directions = new Vector2Int[]
	{
		new Vector2Int(-1, 0), // 左
		new Vector2Int(0, 1), // 上
		new Vector2Int(1, 0), // 右
		new Vector2Int(0, -1),// 下
	};
	
	private void AnimalMove()
	{
		// 获取所有红色像素的位置
		// List<Vector2Int> redPixelPositions = GetRedPixelPositions();
		// 检查每个红色像素的邻居
		foreach (Original_Animal animal in animals)
		{
			bool hasBlackNeighbor = false;
			// 随机化方向数组的顺序
			if (Random.Range(0, 10) > 8)
			{
				directions = directions.OrderBy(x => rng.Next()).ToArray();
			}

			foreach (Vector2Int dir in directions)
			{
				Vector2Int neighborPos = animal.positionHead + dir;
				if (neighborPos.x >= 0 && neighborPos.x < initTex.width && neighborPos.y >= 0 &&
				    neighborPos.y < initTex.height)
				{
					Color neighborColor = initTex.GetPixel(neighborPos.x, neighborPos.y);
					if (neighborColor == Color.black || neighborColor == IntArrayTOColor(element.grass)) // 如果邻居像素的颜色是黑色
					{
						hasBlackNeighbor = true;
						//保存RenderTexture到Texture2D
						SaveToTexture();
						// 移动红色像素到邻居的位置
						initTex.SetPixel(animal.positionBody_B.x, animal.positionBody_B.y, Color.black);
						initTex.SetPixel(animal.positionBody_A.x, animal.positionBody_A.y, Color.red);
						initTex.SetPixel(animal.positionHead.x, animal.positionHead.y, Color.red);
						initTex.SetPixel(neighborPos.x, neighborPos.y, Color.red);
						// 更新动物的位置
						animal.positionBody_B = animal.positionBody_A;
						animal.positionBody_A = animal.positionHead;
						animal.positionHead = neighborPos;
						initTex.Apply();
						
						//重新将Texture2D加载到RenderTexture
						Graphics.Blit(initTex, tex);
						return;
					}
				}
				if (!hasBlackNeighbor)
				{
					SaveToTexture();
					// 如果positionHead四周都没有黑色像素，让positionBody_B成为positionHead
					(animal.positionBody_B, animal.positionHead) = (animal.positionHead, animal.positionBody_B);
					initTex.Apply();

					//重新将Texture2D加载到RenderTexture
					Graphics.Blit(initTex, tex);
					continue;
				}
			}
		}
	}
	
	private void SetColor(ComputeShader shader)
	{
		//设置元素颜色
		shader.SetInts("soil", element.soil);	
		shader.SetInts("sand", element.sand);	
		shader.SetInts("animals", element.animals);	
		shader.SetInts("grass", element.grass);	
		shader.SetInts("water", element.water);	
	}
	
	
	private float timer = 0.0f;
	private float waitTime = 0.1f; // 每秒调用一次
	void Update()
	{

		timer += Time.deltaTime;
		
		if (timer > waitTime)
		{
			AnimalMove();
			timer = timer - waitTime;
		}
		
	  // if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
	  // {
		 //  if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
		 //  {
			//   Vector2 screenPos = new Vector2(Input.mousePosition.x * textureWidth / Screen.width / textureWidth,
			// 	  Input.mousePosition.y * textureHeight / Screen.height / textureHeight);
			//   if (mousePos != screenPos) mousePos = screenPos;
		 //  }
		 //  else
		 //  {
			//   if (mousePos != defaultposition) mousePos = defaultposition;
		 //  }
	  // }
	bool isOverUI = false;
	#if UNITY_EDITOR || UNITY_STANDALONE
	// 在PC上，直接调用IsPointerOverGameObject方法
	  isOverUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
	#elif UNITY_IOS || UNITY_ANDROID
	// 在移动设备上，传递触摸事件的ID作为参数给IsPointerOverGameObject方法
	if (Input.touchCount > 0)
	{
	    isOverUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
	}
	#endif
	
	  if (!isOverUI)
	  {
		  if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
		  {
			  Vector2 screenPos = new Vector2(Input.mousePosition.x * textureWidth / Screen.width / textureWidth,
				  Input.mousePosition.y * textureHeight / Screen.height / textureHeight);
			  if (mousePos != screenPos) mousePos = screenPos;
		  }
		  else
		  {
			  if (mousePos != defaultposition) mousePos = defaultposition;
		  }
	  }

  //Run compute shader
  SetColor(shader);
		shader.SetInt("_Size",brushSize);
		shader.SetInt("_MouseMode", mouseMode);	
        shader.SetVector("_MousePos", mousePos);		
		shader.SetFloat("_Time",Time.time);
		shader.Dispatch (_kernel,dispatchCount.x , dispatchCount.y, 1);
	}
	
	void OnApplicationQuit()
	{
#if UNITY_EDITOR
		// TextureTool.SaveAndResetTextureReadable(initTex, tex0_setting);
		// TextureTool.SetTextureReadable(initTex, false, true);
#endif
	}
}
