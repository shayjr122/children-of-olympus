using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GMR.Editor
{
    /// <summary>
    /// TextMeshPro setup for the Sandbox UI: imports TMP Essential Resources (shaders, TMP Settings,
    /// LiberationSans) from the uGUI package when missing, then builds a static ASCII SDF font asset and an
    /// outline + soft-shadow material preset used by every UI text. Idempotent.
    /// Batchmode: Unity -batchmode -quit -projectPath game -executeMethod GMR.Editor.TmpResources.EnsureAll
    /// </summary>
    public static class TmpResources
    {
        const string EssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        const string FallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
        const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        const string FontDir = "Assets/Art/Fonts";
        const string FontAssetPath = FontDir + "/UI_LiberationSans SDF.asset";
        const string OutlineMaterialPath = FontDir + "/UI_LiberationSans SDF - Outline.mat";

        // Printable ASCII plus the few symbols the UI uses.
        static readonly string Characters = BuildAscii() + "×…•";

        [MenuItem("GMR/Ensure TextMeshPro Resources")]
        public static void EnsureAll()
        {
            EnsureEssentials();
            var font = EnsureFontAsset();
            EnsureFallback(font);
            EnsureOutlineMaterial(font);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The UI font asset (built if missing).</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
                return font != null ? font : EnsureFontAsset();
            }
        }

        /// <summary>Shared outline + underlay material preset for the UI font.</summary>
        public static Material OutlineMaterial
        {
            get
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
                return material != null ? material : EnsureOutlineMaterial(Font);
            }
        }

        public static void EnsureEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath) != null) return;
            string package = Path.GetFullPath(EssentialsPackage);
            if (!File.Exists(package)) throw new InvalidOperationException("TMP Essential Resources not found at " + package);
            // ImportPackage is asynchronous (it finishes after this method returns), so the build cannot continue now.
            // The imported files live in the repo; this path only runs on a project that lost them.
            UnityEditor.AssetPackage.Package.Import(package, false);
            throw new InvalidOperationException(
                "TMP Essential Resources were missing; their import has started. Run the command again once it finishes. " +
                "Batchmode: Unity -batchmode -quit -projectPath game -importPackage \"" + package + "\" first.");
        }

        static TMP_FontAsset EnsureFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;

            EnsureEssentials();
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (source == null) throw new InvalidOperationException("Source font not found: " + SourceFontPath);
            EnsureFolder(FontDir);

            // 64 pt sampling, 8 px padding: enough distance-field range for a thick outline at large sizes.
            var asset = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, false);
            asset.name = Path.GetFileNameWithoutExtension(FontAssetPath);
            if (!asset.TryAddCharacters(Characters, out string missing) && !string.IsNullOrEmpty(missing))
                UnityEngine.Debug.LogWarning("[GMR/Editor] UI font is missing characters: " + missing);
            asset.atlasPopulationMode = AtlasPopulationMode.Static; // fixed atlas: no runtime edits of the asset

            AssetDatabase.CreateAsset(asset, FontAssetPath);
            asset.atlasTexture.name = asset.name + " Atlas";
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceSynchronousImport);
            UnityEngine.Debug.Log("[GMR/Editor] Created UI font asset " + FontAssetPath);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        }

        /// <summary>Non-ASCII text (accents, Greek) falls back to the essentials' dynamic LiberationSans, not boxes.</summary>
        static void EnsureFallback(TMP_FontAsset font)
        {
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
            if (fallback == null)
            {
                UnityEngine.Debug.LogWarning("[GMR/Editor] No TMP fallback font at " + FallbackFontPath);
                return;
            }
            if (font.fallbackFontAssetTable == null) font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            if (font.fallbackFontAssetTable.Contains(fallback)) return;
            font.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(font);
        }

        static Material EnsureOutlineMaterial(TMP_FontAsset font)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            bool created = material == null;
            if (created) material = new Material(font.material) { name = Path.GetFileNameWithoutExtension(OutlineMaterialPath) };
            else material.CopyPropertiesFromMaterial(font.material);

            // Dark SDF outline plus a soft drop shadow: readable over any part of the scene.
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 1f));
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.6f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.5f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.5f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);

            ShaderUtilities.UpdateShaderRatios(material); // keeps outline + dilate inside the SDF range
            if (created) AssetDatabase.CreateAsset(material, OutlineMaterialPath);
            else EditorUtility.SetDirty(material);
            return material;
        }

        static string BuildAscii()
        {
            var chars = new char[95];
            for (int i = 0; i < chars.Length; i++) chars[i] = (char)(32 + i);
            return new string(chars);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
