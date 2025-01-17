using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ToonPerCharacterRenderController : MonoBehaviour
{
    public Transform headBoneTransform;
    [Header("Perspective removal")]
    [Range(0, 1)]
    public float perspectiveRemovalAmount = 0;
    [Header("Perspective removal (Sphere, using head transform as sphere center)")]
    public float perspectiveRemovalRadius = 1;
    [Header("Perspective removal (world height)")]
    public float perspectiveRemovalStartHeight = 0;
    public float perspectiveRemovalEndHeight = 1;
    
    public List<Renderer> allRenderers = new List<Renderer>();

    private void OnEnable()
    {
        GetComponentsInChildren<Renderer>(allRenderers);
        allRenderers = allRenderers.FindAll(x =>
        {
            foreach(var mat in x.sharedMaterials)
            {
                // prevents null if shader fails to conpile
                if(mat)
                    if(mat.shader)
                        if (mat.shader.name.EndsWith("MY_PBR_MultiLight_Cartoon"))
                            return true;
            }
            return false;
        });
    }

    List<Material> tempMaterialList = new List<Material>();
    private void LateUpdate()
    {
#if UNITY_EDITOR
        foreach (var renderer in allRenderers)
        {
            renderer.GetSharedMaterials(tempMaterialList);
            foreach (var material in tempMaterialList)
            {
                UpdateMaterial(material);
            }
        }
#else
        foreach (var renderer in allRenderers)
        {
            renderer.GetMaterials(tempMaterialList);
            foreach (var material in tempMaterialList)
            {
                UpdateMaterial(material);
            }
        }
#endif
    }


    void UpdateMaterial(Material input)
    {
        input.SetFloat("_PerspectiveRemovalAmount", perspectiveRemovalAmount);
        input.SetFloat("_PerspectiveRemovalRadius", perspectiveRemovalRadius);
        input.SetFloat("_PerspectiveRemovalStartHeight", perspectiveRemovalStartHeight);
        input.SetFloat("_PerspectiveRemovalEndHeight", perspectiveRemovalEndHeight);
        input.SetVector("_HeadBonePositionWS", headBoneTransform.position);
    }
}
