using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Athena
{
    public static class TextureTool
    {
        public static void SetTextureReadable(Texture2D texture, bool isReadable, bool import = true, bool forceImport = true)
        {
            bool originIsReadable = texture.isReadable;
            if (originIsReadable != isReadable || forceImport)
            {
                var assetPath = AssetDatabase.GetAssetPath(texture);
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                if (importer)
                {
                    importer.isReadable = isReadable;
                    if (import) importer.SaveAndReimport();
                }
            }
        }
        
        public static TextureImporterPlatformSettings SetTextureReadableAndCancelNormalMap(Texture2D texture2D, int? maxSize = null)
        {
            var importer = TextureTool.GetTextureImporter(texture2D);
            if (importer.textureType == TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.Default;
                importer.isReadable = true;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
            }
            var setting = TextureTool.GetTextureImporterPlatformSettings(texture2D, EditorUserBuildSettings.activeBuildTarget);

            if (!setting.overridden)
                TextureTool.SetTextureOverride(texture2D, true, EditorUserBuildSettings.activeBuildTarget, true);
            if (maxSize != null && setting.maxTextureSize != maxSize.Value)
                TextureTool.SetTextureMaxSize(texture2D, maxSize.Value, EditorUserBuildSettings.activeBuildTarget, false);
            TextureTool.SetTextureFormat(texture2D, TextureImporterFormat.RGBA32, EditorUserBuildSettings.activeBuildTarget);
            TextureTool.SetTextureReadable(texture2D, true, true);
            return setting;
        }

        public static TextureImporterFormat SetTextureFormat(Texture2D texture, TextureImporterFormat format, BuildTarget target, bool import = true)
        {
            var importer = GetTextureImporter(texture);
            var setting = GetTextureImporterPlatformSettings(importer, target);
            var originFormat = setting.format;
            if (setting.overridden == false || setting.format != format)
            {
                setting.overridden = true;
                setting.format = format;
                importer.SetPlatformTextureSettings(setting);
                if (import) importer.SaveAndReimport();
            }
            return originFormat;
        }

        public static bool SetTextureOverride(Texture2D texture, bool isOverride, BuildTarget target, bool import = true)
        {
            var importer = GetTextureImporter(texture);
            var setting = GetTextureImporterPlatformSettings(importer, target);
            var originOverridden = setting.overridden;
            if (setting.overridden != isOverride)
            {
                setting.overridden = isOverride;
                importer.SetPlatformTextureSettings(setting);
                if (import) importer.SaveAndReimport();
            }
            return originOverridden;
        }

        public static int SetTextureMaxSize(Texture2D texture, int maxSize, BuildTarget target, bool import = true)
        {
            var importer = GetTextureImporter(texture);
            var setting = GetTextureImporterPlatformSettings(importer, target);
            var originFormat = setting.format;
            if (setting.maxTextureSize != maxSize)
            {
                setting.maxTextureSize = maxSize;
                importer.SetPlatformTextureSettings(setting);
                if (import)
                {
                    importer.SaveAndReimport();
                }
            }
            return maxSize;
        }

        public static TextureImporter GetTextureImporter(Texture2D texture)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            return (TextureImporter)AssetImporter.GetAtPath(assetPath);
        }

        public static TextureImporterPlatformSettings GetTextureImporterPlatformSettings(Texture2D texture, BuildTarget target)
        {
            var importer = GetTextureImporter(texture);
            return GetTextureImporterPlatformSettings(importer, target);
        }

        public static TextureImporterPlatformSettings GetTextureImporterPlatformSettings(TextureImporter importer, BuildTarget target)
        {
            TextureImporterPlatformSettings setting = null;
            switch (target)
            {
                case BuildTarget.Android:
                case BuildTarget.iOS:
                    setting = importer.GetPlatformTextureSettings(target.ToString());
                    break;
                default:
                    setting = importer.GetDefaultPlatformTextureSettings();
                    break;
            }
            return setting;
        }

        [Serializable]
        public class CombineTexChannelData
        {
            public EChannel fromChannel;
            public EChannel toChannel;
            // 检测到纯白时是否需要使用默认纯色
            public bool checkPureColor;
            public float checkPureColorValue = 1.0f;
            [Range(0, 1)]
            public float defaultPureColorValue;

            public CombineTexChannelData()
            {
                fromChannel = EChannel.R;
                toChannel = EChannel.R;
            }
            
            public CombineTexChannelData(EChannel from, EChannel to)
            {
                this.fromChannel = from;
                this.toChannel = to;
            }
            
            public CombineTexChannelData(EChannel from, EChannel to, bool checkPureColor, float checkPureColorValue = 1, float defaultPureColorValue = 0)
            {
                this.fromChannel = from;
                this.toChannel = to;
                this.checkPureColor = checkPureColor;
                this.checkPureColorValue = checkPureColorValue;
                this.defaultPureColorValue = defaultPureColorValue;
            }
        }
        
        public static void CombineTexture(Texture2D targetTex, List<CombineTexChannelData> combineTexDatas, bool isGamma = false)
        {
            int maxSize = Mathf.Max(targetTex.width, targetTex.height);

            //设置为可读写状态
            var tex0_setting = SetTextureReadableAndCancelNormalMap(targetTex, maxSize);
            
            var tex0Colors = targetTex.GetPixels();

            //替换通道颜色
            foreach (var combineTexData in combineTexDatas)
            {
                SetColors(tex0Colors, combineTexData.defaultPureColorValue, combineTexData.toChannel);
            }

            //检测并设置对应通道为纯色
            foreach (var combineTexData in combineTexDatas)
            {
                if (combineTexData.checkPureColor && IsPureColor(tex0Colors, combineTexData.toChannel, combineTexData.checkPureColorValue)) //检测纯色
                {
                    SetColors(tex0Colors, combineTexData.defaultPureColorValue, combineTexData.toChannel);
                }
            }
            
            targetTex.SetPixels(tex0Colors);
            targetTex.Apply();
            
            SaveAndResetTextureReadable(targetTex, tex0_setting);
            SetTextureReadable(targetTex, false, true);
        }

        /// <summary>
        /// 将tex1的通道转换到tex0的通道上(需要一一对应)
        /// </summary>
        public static void CombineTexture(Texture2D targetTex, Texture2D sourceTex, List<CombineTexChannelData> combineTexDatas, bool isGamma = false)
        {
            if (targetTex == null || sourceTex == null)
            {
                Debug.LogError("贴图为空，不能进行合并");
                return;
            }
            
            //设置为可读写状态
            int maxSize = Mathf.Max(targetTex.width, targetTex.height);
            var tex0_setting = SetTextureReadableAndCancelNormalMap(targetTex, maxSize);
            int sourceMaxSize = Mathf.Max(sourceTex.width, sourceTex.height);
            var tex1_setting = SetTextureReadableAndCancelNormalMap(sourceTex, sourceMaxSize);
            sourceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(sourceTex));
            
            int minSize = Mathf.Min(sourceMaxSize, maxSize);
            if (minSize != maxSize)
            {
                SetTextureReadableAndCancelNormalMap(targetTex, minSize);
                SetTextureReadableAndCancelNormalMap(sourceTex, minSize);
                sourceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(sourceTex));
                targetTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(targetTex));
            }

            var tex0Colors = targetTex.GetPixels();
            var tex1Colors = sourceTex.GetPixels();

            //替换通道颜色
            foreach (var combineTexData in combineTexDatas)
            {
                SetColors(tex1Colors, tex0Colors, combineTexData.fromChannel, combineTexData.toChannel, isGamma);
            }

            //检测并设置对应通道为纯色
            foreach (var combineTexData in combineTexDatas)
            {
                if (combineTexData.checkPureColor && IsPureColor(tex0Colors, combineTexData.toChannel, combineTexData.checkPureColorValue)) //检测纯色
                {
                    SetColors(tex0Colors, combineTexData.defaultPureColorValue, combineTexData.toChannel);
                }
            }
            
            targetTex.SetPixels(tex0Colors);
            targetTex.Apply();
            
            SaveAndResetTextureReadable(targetTex, tex0_setting);
            if (tex1_setting != null)
            {
                ResetTextureReadable(sourceTex, tex0_setting);
            }
            SetTextureReadable(sourceTex, false, true);
        }
        
        public static void SaveAndResetTextureReadable(Texture2D texture2D, TextureImporterPlatformSettings setting)
        {
            var path = AssetDatabase.GetAssetPath(texture2D);
            var newPath = SaveTex(texture2D, path);
            texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
            var import = GetTextureImporter(texture2D);
            import.SetPlatformTextureSettings(setting);
            SetTextureReadable(texture2D, false, true);
        }
        
        public static void ResetTextureReadable(Texture2D texture2D, TextureImporterPlatformSettings setting)
        {
            var import = GetTextureImporter(texture2D);
            import.SetPlatformTextureSettings(setting);
            SetTextureReadable(texture2D, false, true);
        }
        
        public static void SetColors(Color[] sourceColors, Color[] targetColors, EChannel from, EChannel to, bool isGamma = false)
        {
            for (var i = 0; i < targetColors.Length; i++)
            {
                switch (to)
                {
                    case EChannel.R:
                        targetColors[i].r = GetColorPassValue(sourceColors[i], from, isGamma);
                        break;
                    case EChannel.G:
                        targetColors[i].g = GetColorPassValue(sourceColors[i], from, isGamma);
                        break;
                    case EChannel.B:
                        targetColors[i].b = GetColorPassValue(sourceColors[i], from, isGamma);
                        break;
                    case EChannel.A:
                        targetColors[i].a = GetColorPassValue(sourceColors[i], from, isGamma);
                        break;
                }
            }
        }

        public static void SetColors(Color[] colors, float color, EChannel to)
        {
            for (var i = 0; i < colors.Length; i++)
            {
                switch (to)
                {
                    case EChannel.R:
                        colors[i].r = color;
                        break;
                    case EChannel.G:
                        colors[i].g = color;
                        break;
                    case EChannel.B:
                        colors[i].b = color;
                        break;
                    case EChannel.A:
                        colors[i].a = color;
                        break;
                }
            }
        }
        
        public static void SetColors(Color[] colors, int i, float color, EChannel to)
        {
            switch (to)
            {
                case EChannel.R:
                    colors[i].r = color;
                    break;
                case EChannel.G:
                    colors[i].g = color;
                    break;
                case EChannel.B:
                    colors[i].b = color;
                    break;
                case EChannel.A:
                    colors[i].a = color;
                    break;
            }
        }
        
        public static float GetColorPassValue(Color color, EChannel from, bool isGamma = false)
        {
            float value = 0.0f;
            switch (from)
            {
                case EChannel.R:
                    value = isGamma ? color.gamma.r : color.r;
                    break;
                case EChannel.G:
                    value = isGamma ? color.gamma.g : color.g;
                    break;
                case EChannel.B:
                    value = isGamma ? color.gamma.b : color.b;
                    break;
                case EChannel.A:
                    value = color.a;
                    break;
            }
            return value;
        }
        
        /// <summary>
        /// 通道是否为纯色
        /// </summary>
        public static bool IsPureColor(Color[] colors, EChannel pass, float pureColor)
        {
            bool isDiff = false;
            for (int i = 0; i < colors.Length; i++)
            {
                switch (pass)
                {
                    case EChannel.R:
                        if (colors[i].r != pureColor)
                            isDiff = true;
                        break;
                    case EChannel.G:
                        if (colors[i].g != pureColor)
                            isDiff = true;
                        break;
                    case EChannel.B:
                        if (colors[i].b != pureColor)
                            isDiff = true;
                        break;
                    case EChannel.A:
                        if (colors[i].a != pureColor)
                            isDiff = true;
                        break;
                    default:
                        if (colors[i].r != pureColor || colors[i].g != pureColor || colors[i].b != pureColor)
                            isDiff = true;
                        break;
                }

                if (isDiff) //存在不同的就直接返回
                {
                    break;
                }
            }

            return !isDiff;
        }
        
        public static void ChangeNameExtension(string texPath, string sourceExt, string destExt)
        {
            var texName = Path.GetFileNameWithoutExtension(texPath);
            if (texName.ToUpper().EndsWith(sourceExt))
            {
                try
                {
                    var texDirectoryName = Path.GetDirectoryName(texPath);
                    var texNameEx = Path.GetExtension(texPath);
                    var newName = texName.Remove(texName.ToUpper().LastIndexOf(sourceExt.ToUpper())) + destExt;
                    var targetPath = texDirectoryName + "/" + newName + texNameEx;
                    // Debug.Log(targetPath);
                    // AssetDatabase.RenameAsset(normalMapPath, targetPath);
                    File.Move(texPath, targetPath);
                    File.Move(texPath + ".meta", targetPath + ".meta");
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.Message);
                }
            }
        }

        public static string SaveTex(Texture2D tex, string assetPath)
        {
            var extension = Path.GetExtension(assetPath).ToLower();
            byte[] bytes;
            switch (extension)
            {
                case ".png":
                    bytes = tex.EncodeToPNG();
                    break;
                case ".jpg":
                    bytes = tex.EncodeToJPG();
                    break;
                case ".tga":
                    bytes = tex.EncodeToTGA();
                    break;
                default:
                    bytes = tex.EncodeToPNG();
                    assetPath = assetPath.Replace(Path.GetExtension(assetPath), ".png");
                    Debug.LogWarning($"save texture {tex.name} fail, extension {extension} is not support, so save as png");
                    break;
            }
            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(Path.GetFullPath(assetPath), bytes);
            AssetDatabase.ImportAsset(assetPath);
            return assetPath;
        }

        public static Color[] GetColors(Texture2D tex)
        {
            var assetPath = AssetDatabase.GetAssetPath(tex);
            var importer = (TextureImporter) AssetImporter.GetAtPath(assetPath);
            var read = importer.isReadable;
            if (!read)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var colors = tex.GetPixels();
            if (!read)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            return colors;
        }

        public static Texture2D CreateTexture2D(RenderTexture rt)
        {
            var tex2D = new Texture2D(rt.width, rt.height);
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, tex2D.width, tex2D.height), 0, 0);
            tex2D.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return tex2D;
        }

        [Serializable]
        public enum EChannel
        {
            R = 1,
            G = 1 << 1,
            B = 1 << 2,
            A = 1 << 3,
        }

        private static class ColorMask
        {
            public const int R = (int) EChannel.R;
            public const int G = (int) EChannel.G;
            public const int B = (int) EChannel.B;
            public const int A = (int) EChannel.A;
            public const int RA = R | A;
            public const int RGB = R | G | B;
            public const int RGBA = R | G | B | A;
        }

        private static class ChannelOffset
        {
            public const int NoOffset = 0;

            public static int RGetFrom(EChannel channel)
            {
                return Mathf.RoundToInt(Mathf.Log((int) channel, 2)) << 6;
            }

            public static int GGetFrom(EChannel channel)
            {
                return ((Mathf.RoundToInt(Mathf.Log((int) channel, 2)) + 3) % 4) << 4;
            }

            public static int BGetFrom(EChannel channel)
            {
                return ((Mathf.RoundToInt(Mathf.Log((int) channel, 2)) + 2) % 4) << 2;
            }

            public static int AGetFrom(EChannel channel)
            {
                return (Mathf.RoundToInt(Mathf.Log((int) channel, 2)) / 2 + 1) % 4;
            }

            private static float GetVal(Color color, int index)
            {
                index %= 4;
                switch (index)
                {
                    case 0:
                        return color.r;
                    case 1:
                        return color.g;
                    case 2:
                        return color.b;
                    case 3:
                        return color.a;
                }

                Debug.LogError($"get color wrong, mask is out ou range, mask: {index}");
                return 0;
            }

            public static float GetValueFromColor(Color color, int mask, EChannel channel)
            {
                switch (channel)
                {
                    case EChannel.R:
                        return GetVal(color, ((mask >> 6) + 0) % 4);
                    case EChannel.G:
                        return GetVal(color, ((mask >> 4) + 1) % 4);
                    case EChannel.B:
                        return GetVal(color, ((mask >> 2) + 2) % 4);
                    case EChannel.A:
                        return GetVal(color, (mask + 3) % 4);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
                }
            }
        }
    }

    public enum ETexType
    {
        Color,
        Normal,
        MSO,
        Mask,
        UVFlow
    }
}
