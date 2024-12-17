#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Rendering.CustomRenderPipeline
{
    internal static class FrustumVolumetricLightFactory
    {
        [MenuItem("GameObject/Light/Frustum Volumetric Light (With Shadow)")]
        public static void CreateDecal(MenuCommand menuCommand)
        {
            string filePath =
                EditorUtility.SaveFilePanelInProject("Create new frustum volumetric light", String.Empty, String.Empty, String.Empty);
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            
            var material = new Material(Shader.Find("Athena/FrustumMeshVolumetricLightShadow"));

            if (material == null)
            {
                Debug.LogWarning("Cannot find <Athena/FrustumMeshVolumetricLightShadow> material");
            }
        
            var go = CoreEditorUtils.CreateGameObject("Frustum Volumetric Light (with shadow)", menuCommand.context);
            filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);
            go.name = filePath.Substring(filePath.LastIndexOf("/") + 1);
            var meshVolumetricLight =  go.AddComponent<FrustumMeshVolumetricLightWithShadow>();
            meshVolumetricLight.Material = material;
        
        
            var shadowmapTexture = new Texture2D(meshVolumetricLight.ShadowmapWidth, meshVolumetricLight.ShadowmapHeight, TextureFormat.RFloat, false, true);
            var shadowmapPath = AssetDatabase.GenerateUniqueAssetPath(filePath + "_shadowmap.exr");
            meshVolumetricLight.ShadowmapTexture = meshVolumetricLight.SaveShadowmapTexture(shadowmapTexture, shadowmapPath);

            var matPath = AssetDatabase.GenerateUniqueAssetPath(filePath + "_mat.mat");
        
            AssetDatabase.CreateAsset(meshVolumetricLight.Material, matPath);
        }
    }
}

#endif