using System;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace Rendering.CustomRenderPipeline
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class FrustumMeshVolumetricLightWithShadow : MonoBehaviour
    {
        // Start is called before the first frame update
        public Material Material;
        private Mesh mesh;

        static private float floatEPS => Single.Epsilon;

        [SerializeField] private float frustumLightHeight = 1;
        public float FrustumLightHeight
        {
            get => frustumLightHeight;
            set => Mathf.Min(0, value);
        }

        public float sclaedFrustumHeight
        {
            get => frustumLightHeight * this.transform.lossyScale.z;
        }

        [SerializeField] private float frustumLightWidth = 1;
        public float FrustumLightWidth
        {
            get => frustumLightWidth;
            set => Mathf.Min(0, value);
        }

        public float scaledFrustumWidth
        {
            get => frustumLightWidth * this.transform.lossyScale.x;
        }

        [SerializeField] private float frustumLightDepth = 2;

        public float FrustumLightDepth
        {
            get => frustumLightDepth;
            set => Mathf.Min(0, value);
        }

        public float scaledFrustumDepth
        {
            get => frustumLightDepth * this.transform.lossyScale.y;
        }

        [SerializeField] private float frustumHalfAngle = 30;

        public float FrustumSlopeAngle
        {
            get => frustumHalfAngle;
            set => Mathf.Clamp(value, 0, 45);
        }
        public bool enabledCurve = false;
        public AnimationCurve curve = AnimationCurve.Constant(0,1,1);
        public float scaledFrustumAspect
        {
            get => sclaedFrustumHeight / (scaledFrustumWidth + Single.Epsilon);
        }

        public float scaledFrustumHalfAngleTan
        {
            get
            {
                float halfBottomHeight = frustumLightDepth * Mathf.Tan(frustumHalfAngle * Mathf.Deg2Rad) + 0.5f * frustumLightHeight;
                var scaledhalfBH = this.transform.lossyScale.z * halfBottomHeight;
                return (scaledhalfBH - sclaedFrustumHeight * 0.5f)  / (scaledFrustumDepth + floatEPS);
            }
        }

        public float scaledFrustumHalfAngle => Mathf.Atan(scaledFrustumHalfAngleTan) * Mathf.Rad2Deg;

        #region shadow
        [SerializeField] [HideInInspector] private GameObject shadowCameraGO;
        [SerializeField] [HideInInspector] private Camera shadowCamera;
        
        private static string SHADOW_CAMERA_NAME = "Shadow Camera";
        [SerializeField] [HideInInspector] private RenderTexture Shadowmap;
        public Texture2D ShadowmapTexture;
        public int ShadowmapResolution = 256;
        public int ShadowmapWidth => ShadowmapResolution;
        public int ShadowmapHeight
        {
            get => Mathf.CeilToInt((float)ShadowmapWidth * scaledFrustumAspect);
        }

        [HideInInspector] public Matrix4x4 ShadowCameraMatrix;

        
    #if UNITY_EDITOR
        private void TryOrAddShadowCamera()
        {
            shadowCameraGO = FindChildByName(this.gameObject, SHADOW_CAMERA_NAME);
            if (shadowCameraGO == null)
            {
                var newCameraGO = new GameObject(SHADOW_CAMERA_NAME);
                newCameraGO.hideFlags = HideFlags.DontSave & HideFlags.NotEditable;
                newCameraGO.transform.hideFlags = HideFlags.NotEditable;
                newCameraGO.transform.parent = this.transform;
                shadowCameraGO = newCameraGO;
            }

            if (shadowCameraGO != null)
            {
                if (!shadowCameraGO.TryGetComponent<Camera>(out Camera shadowCamera))
                {
                    shadowCamera = shadowCameraGO.gameObject.AddComponent<Camera>();
                }
                this.shadowCamera = shadowCamera;
                this.shadowCamera.enabled = false;
                this.shadowCamera.clearFlags = CameraClearFlags.SolidColor; 
                this.shadowCamera.backgroundColor = Color.black;
            }
        }

        private GameObject FindChildByName(GameObject go, string name)
        {
            if (go == null)
            {
                return null;
            }
            
            var count = this.gameObject.transform.childCount;
            for (var i = 0; i < count; i++)
            {
                var childTransform = this.gameObject.transform.GetChild(i);
                if (childTransform.name == name)
                {
                    return childTransform.gameObject;
                }
            }

            return null;
        }
    #endif

    #if UNITY_EDITOR
        private void InitShadowmap()
        {
            if (Shadowmap == null || Shadowmap.width != ShadowmapWidth || Shadowmap.height != ShadowmapHeight)
            {
                if (Shadowmap != null)
                {
                    Shadowmap.Release();
                }
                RenderTextureDescriptor desc = new RenderTextureDescriptor(ShadowmapWidth, ShadowmapHeight,
                    RenderTextureFormat.Depth, 32, 0);
                Shadowmap = new RenderTexture(desc);
                Shadowmap.name = String.Concat(this.gameObject.name, "_Shadowmap");
                Shadowmap.Create();
            }

            this.shadowCamera.targetTexture = Shadowmap;
        }
    #endif
        
    #if UNITY_EDITOR
        [ContextMenu("调整阴影相机位置")]
        private void SetShadowCameraFrustum()
        {
            if (shadowCameraGO == null || shadowCamera == null)
            {
                Debug.LogWarning("未设置阴影相机");
                return;
            }
            InitShadowmap();
            shadowCameraGO.transform.localRotation = Quaternion.Euler(90, 0, 0);

            float near = sclaedFrustumHeight * 0.5f / scaledFrustumHalfAngleTan;
            shadowCameraGO.transform.localPosition = new Vector3(0, near / this.transform.lossyScale.y, 0);
            shadowCamera.nearClipPlane= near;
            shadowCamera.farClipPlane = scaledFrustumDepth + near;
            shadowCamera.fieldOfView = scaledFrustumHalfAngle * 2;
        }
    #endif
        
    #if UNITY_EDITOR
        [ContextMenu("烘焙阴影图")]
        private void BakeShadowmap()
        {
            SetShadowCameraFrustum();
            
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = Shadowmap;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;
            shadowCamera.targetTexture = Shadowmap;
            shadowCamera.Render();
            RenderTextureDescriptor des = new RenderTextureDescriptor(Shadowmap.width, Shadowmap.height, RenderTextureFormat.RFloat, 0, 0);
            var shadow = RenderTexture.GetTemporary(des);
            Graphics.Blit(Shadowmap, shadow);
            SaveShadowmapTexture(shadow, Shadowmap.name);
            RenderTexture.ReleaseTemporary(shadow);
            SetShadowData();
        }
    #endif

    #if UNITY_EDITOR
        private void SaveShadowmapTexture(RenderTexture rt, string name)
        {
            string path = null;
            if (ShadowmapTexture != null)
            {
                path = AssetDatabase.GetAssetPath(ShadowmapTexture);
                AssetDatabase.DeleteAsset(path);
                ShadowmapTexture = null;
            }
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
                foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
                {
                    path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        path = Path.GetDirectoryName(path);
                        break;
                    }
                }
                path = AssetDatabase.GenerateUniqueAssetPath($"{path}/{name}.exr");
            }

            if (ShadowmapTexture == null)
            {
                ShadowmapTexture = new Texture2D(ShadowmapWidth, ShadowmapHeight, TextureFormat.RFloat, false, true);
            }

            var oldRT = RenderTexture.active;
            RenderTexture.active = rt;
            ShadowmapTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            ShadowmapTexture.name = Shadowmap.name;
            ShadowmapTexture.Apply();
            RenderTexture.active = oldRT;
            ShadowmapTexture = SaveShadowmapTexture(ShadowmapTexture, path);
        }

        public Texture2D SaveShadowmapTexture(Texture2D tex, string path)
        {
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            byte[] dataBytes = tex.EncodeToEXR();
            File.WriteAllBytes(path, dataBytes);
            AssetDatabase.Refresh();
            
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            TextureImporterSettings setting = new TextureImporterSettings();
            if (importer != null)
            {
                importer.ReadTextureSettings(setting);
                // setting.textureType = TextureImporterType.SingleChannel;
                // setting.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                setting.wrapMode = TextureWrapMode.Clamp;
                setting.filterMode = FilterMode.Point;
                setting.mipmapEnabled = false;
                setting.sRGBTexture = false;
                
                TextureImporterPlatformSettings platformSetting_pc = importer.GetPlatformTextureSettings("Standalone");
                platformSetting_pc.overridden = true;
                platformSetting_pc.format = TextureImporterFormat.R16;
                importer.SetPlatformTextureSettings(platformSetting_pc);
                
                TextureImporterPlatformSettings platformSetting_android = importer.GetPlatformTextureSettings("Android");
                platformSetting_android.overridden = true;
                platformSetting_android.format = TextureImporterFormat.R8;
                importer.SetPlatformTextureSettings(platformSetting_android);
                
                TextureImporterPlatformSettings platformSetting_ios = importer.GetPlatformTextureSettings("iPhone");
                platformSetting_ios.overridden = true;
                platformSetting_ios.format = TextureImporterFormat.R16;
                importer.SetPlatformTextureSettings(platformSetting_ios);
                
                importer.SetTextureSettings(setting);
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            else
            {
                Debug.LogError("importer = null) " + path);
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    #endif

        private void SetShadowData()
        {
            var camera = shadowCamera;
            ShadowCameraMatrix = GetShadowTransform(camera.projectionMatrix, camera.worldToCameraMatrix);
            Material.SetTexture("_VolumetricLightDepth", ShadowmapTexture);
        }
        #endregion
        
        private void OnEnable()
        {
    #if UNITY_EDITOR
            TryOrAddShadowCamera();
            InitShadowmap();
    #endif
            mesh = CreateFrustumMesh(frustumLightWidth, frustumLightHeight, frustumLightDepth, frustumHalfAngle);
            GetVolumeBoundFaces(mesh, ref planes);

            SetMaterial();
            
            SetHGPhaseFactor();
        }
        
        private void OnDisable()
        {
            if (mesh != null)
            {
                mesh = null;
            }
        }
        
        void Start()
        {
        }

        
        // Update is called once per frame
        void Update()
        {
            // #if UNITY_EDITOR
    //         BakeShadowmap();
    // #endif
            if (!this.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
            {
                meshFilter = this.gameObject.AddComponent<MeshFilter>();
            }
            meshFilter.sharedMesh = mesh;
            
            if (!this.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
            {
                meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
            }

            meshRenderer.sharedMaterial = Material;
#if  UNITY_EDITOR
            GetVolumeBoundFaces(mesh, ref planes);

            SetMaterial();
            
            SetHGPhaseFactor();
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            mesh = CreateFrustumMesh(frustumLightWidth, frustumLightHeight, frustumLightDepth, frustumHalfAngle);
            GetVolumeBoundFaces(mesh, ref planes);

            SetMaterial();
            
            SetHGPhaseFactor();
        }
#endif
        public void SetMaterial()
        {
            if (Material == null)
            {
                Debug.LogError("Missing Material");
                return;
            }
            //var extinction = Extinction;//Mathf.Log(1f / volumEnd) / FrustumDepth;
            // Material.SetFloat("_TransmittanceExtinction", Extinction);
            
            // bound
            for (var planeIndex = 0; planeIndex < 6; planeIndex++)
            {
                Material.SetVector($"_BoundaryPlanes_{planeIndex}", planes[planeIndex]);
            }
            
            Material.SetVector("_LightPosition", new Vector4(frustumLightWidth, frustumLightHeight,
                frustumLightDepth, Mathf.Tan(frustumHalfAngle * Mathf.Deg2Rad)));

            float tanHalfFov = scaledFrustumHalfAngleTan;

            float top = sclaedFrustumHeight * 0.5f;
            float bottom = -top;
            float right = scaledFrustumWidth * 0.5f;
            float left = -right;
            float nearPlane = top / tanHalfFov;
            float farPlane = nearPlane + scaledFrustumDepth;
            Matrix4x4 projectionMatrix = new Matrix4x4();
            projectionMatrix[0, 0] = 2.0f * nearPlane / (right - left);
            projectionMatrix[1, 1] = 2.0f * nearPlane / (top - bottom);
            projectionMatrix[0, 2] = (right + left) / (right - left);
            projectionMatrix[1, 2] = (top + bottom) / (top - bottom);
            projectionMatrix[2, 2] = -(farPlane + nearPlane) / (farPlane - nearPlane);
            projectionMatrix[2, 3] = -2.0f * farPlane * nearPlane / (farPlane - nearPlane);
            projectionMatrix[3, 2] = -1.0f;

            var position = TransformPosition(this.transform.localToWorldMatrix, new Vector3(0, nearPlane / this.transform.lossyScale.y, 0));
            var rotation = this.transform.rotation * Quaternion.Euler(90, 0, 0);
            Matrix4x4 worldToCameraMatrix = Matrix4x4.Inverse(Matrix4x4.TRS(position, rotation, new Vector3(1, 1, -1)));
            var ShadowCameraMatrix = GetShadowTransform(projectionMatrix, worldToCameraMatrix);
            Material.SetVector("_ShadowVMatrix_0", ShadowCameraMatrix.GetRow(0));
            Material.SetVector("_ShadowVMatrix_1", ShadowCameraMatrix.GetRow(1));
            Material.SetVector("_ShadowVMatrix_2", ShadowCameraMatrix.GetRow(2));
            Material.SetVector("_ShadowVMatrix_3", ShadowCameraMatrix.GetRow(3));
            
        }
        
        public static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
        {
            // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
            // apply z reversal to projection matrix. We need to do it manually here.
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;

            var textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;
            // textureScaleAndBias maps texture space coordinates from [-1,1] to [0,1]

            // Apply texture scale and offset to save a MAD in shader.
            return textureScaleAndBias * worldToShadow;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireMesh(mesh, 0, transform.position, transform.rotation, transform.lossyScale);
        }
        
        private List<Vector4> planes = new List<Vector4>(6);
        
        private void GetVolumeBoundFaces(Mesh mesh, ref List<Vector4> planes)
        {
            planes.Clear();
            var count = mesh.triangles.Length;
            var transformMatrix = this.transform.localToWorldMatrix;
            for (var i = 0; 2 * i + 2 < count; i += 3)
            {
                var v1 = mesh.vertices[mesh.triangles[i * 2]];
                var v2 = mesh.vertices[mesh.triangles[i * 2 + 1]];
                var v3 = mesh.vertices[mesh.triangles[i * 2 + 2]];
                
                var plane = new Plane(v1, v2, v3);
                planes.Add(new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance));
            }
        }

        private static Mesh CreateFrustumMesh(float topWidth, float topHeight, float depth, float slope, bool isBox = false)
        {
            float tl = topWidth * 0.5f;
            float tr = topWidth * 0.5f;
            float tt = topHeight * 0.5f;
            float tb = topHeight * 0.5f;
            
            float delta = depth * Mathf.Tan(slope * Mathf.Deg2Rad);
            float bt = tt + delta;
            float bb = tb + delta;

            float aspectRcp = topWidth / (topHeight + floatEPS);
            float bl = tl +  delta * aspectRcp;
            float br = tr +  delta * aspectRcp;
            if (isBox)
            {
                tl = bl;
                tr = br;
                tt = bt;
                tb = bb;
            }

            var mesh = new Mesh();
            Vector3[] vertices = new Vector3[8];
            
            vertices[0] = new Vector3(-bl, -depth, -bb);
            vertices[1] = new Vector3(br, -depth, -bb);
            vertices[2] = new Vector3(tr, 0, -tb);
            vertices[3] = new Vector3(-tl, 0, -tb);
            vertices[4] = new Vector3(-bl, -depth, bt);
            vertices[5] = new Vector3(br, -depth, bt);
            vertices[6] = new Vector3(tr, 0, tt);
            vertices[7] = new Vector3(-tl, 0, tt);
            
            mesh.vertices = vertices;

            int[] triangles = new int[36];
            
            triangles[0] = 3; triangles[1] = 6; triangles[2] = 2;
            triangles[3] = 3; triangles[4] = 7; triangles[5] = 6;
            triangles[6] = 1; triangles[7] = 6; triangles[8] = 5;
            triangles[9] = 1; triangles[10] = 2; triangles[11] = 6;
            triangles[12] = 5; triangles[13] = 7; triangles[14] = 4;
            triangles[15] = 5; triangles[16] = 6; triangles[17] = 7;
            triangles[18] = 4; triangles[19] = 3; triangles[20] = 0;
            triangles[21] = 4; triangles[22] = 7; triangles[23] = 3;
            triangles[24] = 0; triangles[25] = 2; triangles[26] = 1;
            triangles[27] = 0; triangles[28] = 3; triangles[29] = 2;
            triangles[30] = 4; triangles[31] = 1; triangles[32] = 5;
            triangles[33] = 4; triangles[34] = 0; triangles[35] = 1;

            mesh.triangles = triangles;

            mesh.RecalculateNormals();
            return mesh;
        }
        
        private static Mesh CreateFrustumMesh(float topSize, float bottomSize, float depth)
        {
            var topCenter = new Vector3(0, 0, 0);
            var bottomCenter = new Vector3(0, -depth, 0);

            var mesh = new Mesh();
            Vector3[] vertices = new Vector3[8];
            
            vertices[0] = bottomCenter + 0.5f * bottomSize * new Vector3(-1, 0, -1);
            vertices[1] = bottomCenter + 0.5f * bottomSize * new Vector3(1, 0, -1);
            vertices[2] = topCenter + 0.5f * topSize * new Vector3(1, 0, -1);
            vertices[3] = topCenter + 0.5f * topSize * new Vector3(-1, 0, -1);
            vertices[4] = bottomCenter + 0.5f * bottomSize * new Vector3(-1, 0, 1);
            vertices[5] = bottomCenter + 0.5f * bottomSize * new Vector3(1, 0, 1);
            vertices[6] = topCenter + 0.5f * topSize * new Vector3(1, 0, 1);
            vertices[7] = topCenter + 0.5f * topSize * new Vector3(-1, 0, 1);

            mesh.vertices = vertices;

            int[] triangles = new int[36];
            
            triangles[0] = 3; triangles[1] = 6; triangles[2] = 2;
            triangles[3] = 3; triangles[4] = 7; triangles[5] = 6;
            triangles[6] = 1; triangles[7] = 6; triangles[8] = 5;
            triangles[9] = 1; triangles[10] = 2; triangles[11] = 6;
            triangles[12] = 5; triangles[13] = 7; triangles[14] = 4;
            triangles[15] = 5; triangles[16] = 6; triangles[17] = 7;
            triangles[18] = 4; triangles[19] = 3; triangles[20] = 0;
            triangles[21] = 4; triangles[22] = 7; triangles[23] = 3;
            triangles[24] = 0; triangles[25] = 2; triangles[26] = 1;
            triangles[27] = 0; triangles[28] = 3; triangles[29] = 2;
            triangles[30] = 4; triangles[31] = 1; triangles[32] = 5;
            triangles[33] = 4; triangles[34] = 0; triangles[35] = 1;

            mesh.triangles = triangles;

            mesh.RecalculateNormals();
            return mesh;
        }
        
        public void GetVolumeBoundFaces(Camera camera, ref List<Vector4> planes)
        {
            planes.Clear();
            Matrix4x4 matrix_VP = camera.projectionMatrix * camera.worldToCameraMatrix;
            
            var m0 = matrix_VP.GetRow(0);
            var m1 = matrix_VP.GetRow(1);
            var m2 = matrix_VP.GetRow(2);
            var m3 = matrix_VP.GetRow(3);
            planes.Add( -(m3 + m0));
            planes.Add( -(m3 - m0));
            planes.Add( -(m3 + m1));
            planes.Add( -(m3 - m1));
            planes.Add( -(m3 + m2)); // ignore near
            planes.Add( -(m3 - m2));
        }

        private static Vector3 TransformPosition(Matrix4x4 matrix, Vector3 pos)
        {
            var v4 = new Vector4(pos.x, pos.y, pos.z, 1);
            var v4Transformed = matrix * v4;
            v4Transformed /= v4Transformed.w;
            return v4Transformed;
        }

        private static Mesh CreateFrustumMesh(Camera camera)
        {
            Vector3[] bottomFrustumCorners = new Vector3[4];
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, bottomFrustumCorners);
            
            Vector3[] topFrustumCorners = new Vector3[4];
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, topFrustumCorners);
            
            var mesh = new Mesh();
            Vector3[] vertices = new Vector3[8];

            vertices[0] = bottomFrustumCorners[0];
            vertices[1] = bottomFrustumCorners[3];
            vertices[2] = topFrustumCorners[3];
            vertices[3] = topFrustumCorners[0];
            vertices[4] = bottomFrustumCorners[1];
            vertices[5] = bottomFrustumCorners[2];
            vertices[6] = topFrustumCorners[2];
            vertices[7] = topFrustumCorners[1];
            
            mesh.vertices = vertices;
            
            int[] triangles = new int[36];
            
            triangles[0] = 3; triangles[1] = 6; triangles[2] = 2;
            triangles[3] = 3; triangles[4] = 7; triangles[5] = 6;
            triangles[6] = 1; triangles[7] = 6; triangles[8] = 5;
            triangles[9] = 1; triangles[10] = 2; triangles[11] = 6;
            triangles[12] = 5; triangles[13] = 7; triangles[14] = 4;
            triangles[15] = 5; triangles[16] = 6; triangles[17] = 7;
            triangles[18] = 4; triangles[19] = 3; triangles[20] = 0;
            triangles[21] = 4; triangles[22] = 7; triangles[23] = 3;
            triangles[24] = 0; triangles[25] = 2; triangles[26] = 1;
            triangles[27] = 0; triangles[28] = 3; triangles[29] = 2;
            triangles[30] = 4; triangles[31] = 1; triangles[32] = 5;
            triangles[33] = 4; triangles[34] = 0; triangles[35] = 1;
            
            mesh.triangles = triangles;
            
            mesh.RecalculateNormals();
            return mesh;
        }

        public void SetHGPhaseFactor()
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraToLight = Vector3.Normalize(this.transform.position - Camera.main.transform.position);
            float distance = Vector3.Dot(cameraForward, cameraToLight);
            
            float clamp01 = Mathf.Clamp01(distance);
            
            float hg = curve.Evaluate(clamp01);
            if (enabledCurve)
            {
                Material.SetFloat("_HGPhaseCurve", hg);
            }
            else
            {
                Material.SetFloat("_HGPhaseCurve", 1);
            }
        }
    }
}

