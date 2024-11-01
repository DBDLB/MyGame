using UnityEngine;
using UnityEditor;
using System;

public class RenderingModeDrawer : MaterialPropertyDrawer
{
    public static readonly string[] blendNames = Enum.GetNames(typeof(RenderingMode));

    public static void SetupMaterialWithBlendMode(Material material, RenderingMode blendMode)
    {
        switch (blendMode)
        {
            case RenderingMode.Opaque:
                material.SetOverrideTag("RenderType", "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                //material.DisableKeyword("_ALPHABLEND_ON");
                material.renderQueue = -1;
                break;
            case RenderingMode.Cutout:
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                //material.DisableKeyword("_ALPHABLEND_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                break;
            case RenderingMode.Alpha:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                //material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
            case RenderingMode.OneAlpha:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)(int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                //material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
            case RenderingMode.OneAplhaDecal:
                //  material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_ZWrite", 0);
                break;
            case RenderingMode.AplhaDecal:
                //  material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                break;
        }
    }

    public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
    {
        EditorGUI.showMixedValue = prop.hasMixedValue;
        var mode = (RenderingMode)prop.floatValue;

        EditorGUI.BeginChangeCheck();
        mode = (RenderingMode)EditorGUI.Popup(position, label, (int)mode, blendNames);

        if (EditorGUI.EndChangeCheck())
        {
            prop.floatValue = (float)mode;
            for (int i = 0; i < editor.targets.Length; i++)
            {
                Material mat = editor.targets[i] as Material;
                if (mat != null)
                {
                    SetupMaterialWithBlendMode(mat, mode);
                }
            }
        }
    }
}
