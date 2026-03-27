using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

public class MaterialLookupUtility : DataExtractor
{
    private class ComparerStringFloat : IEqualityComparer<(string, float)>
    {
        public bool Equals((string, float) x, (string, float) y)
        {
            return string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) && x.Item2 == y.Item2;
        }

        public int GetHashCode((string, float) obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1) ^ obj.Item2.GetHashCode();
        }
    }

    private class MaterialInfo
    {
        public string UnityPathAndFilenameToMaterial { get; set; }
        public bool UseBlendedTransparency { get; set; }
    }

    private static void SetMaterialAlphaForBlendedTextures(List<MaterialInfo> materialInfos)
    {
        var blendedTranparentMaterials = materialInfos.Where(x => x.UseBlendedTransparency).ToList();

        AssetDatabase.StartAssetEditing();

        foreach (var blendedTranparentMaterial in blendedTranparentMaterials)
        {
            TextureImporter importer = AssetImporter.GetAtPath(blendedTranparentMaterial.UnityPathAndFilenameToMaterial) as TextureImporter;
            if (importer != null)
            {
                importer.alphaIsTransparency = false;
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.SaveAndReimport();
            }
        }

        RefreshAssetDatabase();
    }

    private static void SetMaterialPropsTransparent(Material material, float? surfaceAlpha)
    {
        material.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetFloat("_Blend", 0);   // 0 = Alpha, 1 = Premultiply (keep it 0 for alpha)
        material.SetFloat("_AlphaClip", 0f); // enable alpha clipping
        material.SetInt("_Cull", (int)CullMode.Off);
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)RenderQueue.Transparent;

        if (surfaceAlpha != null)
        {
            Color c = new Color(1f, 1f, 1f, surfaceAlpha.Value);
            material.SetColor("_BaseColor", c);
        }
    }

    private static void SetMaterialPropsOpaqueAlphaClip(Material material)
    {
        material.SetFloat("_Surface", 0f); // 0 = Opaque, 1 = Transparent
        material.SetOverrideTag("RenderType", "Opaque");
        material.SetFloat("_AlphaClip", 1f); // enable alpha clipping
        material.SetFloat("_Cutoff", 0.5f);  // default threshold
        material.EnableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void SetMaterialPropsOpaqueNoAlpha(Material material)
    {
        material.SetFloat("_Surface", 0f); // 0 = Opaque, 1 = Transparent
        material.SetOverrideTag("RenderType", "Opaque");
        material.SetFloat("_AlphaClip", 0f); // enable alpha clipping
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)RenderQueue.Geometry;
    }

    private static bool CreateMaterial(TextureLookupModel textureLookup, string pathToMaterial, float? surfaceAlpha)
    {
        Texture2D texture2d = AssetDatabase.LoadAssetAtPath<Texture2D>(textureLookup.UnityPathAndFilenameToPNG);

        Shader shader = Shader.Find(textureLookup.UseFullbright ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        Material material = new Material(shader);
        material.name = Path.GetFileNameWithoutExtension(pathToMaterial);
        material.mainTexture = texture2d;
        material.SetFloat("_Smoothness", 0f);
        material.SetFloat("_BlendModePreserveSpecular", 0f);

        if (surfaceAlpha.HasValue || textureLookup.TransparencyType == TransparencyTypes.BlendedTransparency)
        {
            SetMaterialPropsTransparent(material, surfaceAlpha);
        }
        else if (textureLookup.TransparencyType == TransparencyTypes.NoTransparency)
        {
            SetMaterialPropsOpaqueNoAlpha(material);
        }
        else if (textureLookup.TransparencyType == TransparencyTypes.ClipOnly)
        {
            SetMaterialPropsOpaqueAlphaClip(material);
        }
        else
        {
            Debug.LogError($"CreateMaterial - could not determine type for {textureLookup.UnityPathAndFilenameToPNG} and {textureLookup.TransparencyType}");
            return false;
        }

        AssetDatabase.CreateAsset(material, pathToMaterial);

        return true;
    }

    private static List<MaterialInfo> CreateMaterialsFromTextureLookups()
    {
        var materialInfos = new List<MaterialInfo>();
        int i = 0;
        foreach (var textureLookup in UnityLookups.TextureLookups)
        {
            i++;
            float progress = (float)i / UnityLookups.TextureLookups.Count;
            EditorUtility.DisplayProgressBar("Creating Materials", $"Item {i} of {UnityLookups.TextureLookups.Count}", progress);

            var pathToMaterial = Path.ChangeExtension(Path.Combine(MaterialPath, textureLookup.Key), "mat").ConvertFolderSeperators();
            Directory.CreateDirectory(Path.GetDirectoryName(pathToMaterial));

            if (CreateMaterial(textureLookup.Value, pathToMaterial, null))
            {
                bool useBlendedTransparency = textureLookup.Value.TransparencyType == TransparencyTypes.BlendedTransparency;
                materialInfos.Add(new MaterialInfo {  UnityPathAndFilenameToMaterial = pathToMaterial, UseBlendedTransparency = useBlendedTransparency });
            }
        }

        return materialInfos;
    }

    private static Dictionary<(string, float), TextureLookupModel> GetSurfaceAlphaTextures(List<DATModel> datModels)
    {
        var worldObjectsWithAlpha = datModels.SelectMany(
            dat => dat.WorldObjects
                .Where(wo => wo.WorldObjectType == WorldObjectTypes.VisibleVolume)
                .SelectMany(wo => wo.Properties
                    .Where(prop => prop.Name == "SurfaceAlpha")
                    .Select(prop => new { DATFilename = dat.Filename, WorldObjectName = wo.Name, SurfaceAlpha = prop.FloatValue })))
            .ToList();

        Dictionary<(string, float), TextureLookupModel> texturesWithAlpha = new Dictionary<(string, float), TextureLookupModel>(new ComparerStringFloat());
        foreach (var dat in datModels)
        {
            foreach (var bsp in dat.BSPModels)
            {
                var matchingWorldObject = worldObjectsWithAlpha.FirstOrDefault(x => x.DATFilename == dat.Filename && x.WorldObjectName == bsp.WorldName);
                if (matchingWorldObject == null)
                {
                    continue;
                }

                if (bsp.TextureNames?.Count > 0)
                {
                    foreach (var textureName in bsp.TextureNames.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        if (textureName.Contains("Invisible", StringComparison.OrdinalIgnoreCase)
                            || textureName.Contains("WaterMarker.dtx", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Skip this one if the texture name (path) isn't found - that's when a DAT has a path to a DTX
                        // but the file isn't found (or not found at the location specified).
                        if (!UnityLookups.TextureLookups.TryGetValue(textureName, out var textureLookup))
                        {
                            continue;
                        }

                        bool alreadyInDictionary = texturesWithAlpha.ContainsKey((textureName, matchingWorldObject.SurfaceAlpha));
                        if (!alreadyInDictionary)
                        {
                            texturesWithAlpha.Add((textureName, matchingWorldObject.SurfaceAlpha), textureLookup);
                        }
                    }
                }
            }
        }

        return texturesWithAlpha;
    }

    private static List<MaterialInfo> CreateSurfaceAlphaMaterials(List<DATModel> datModels)
    {
        var surfaceAlphaTextures = GetSurfaceAlphaTextures(datModels);

        var materialInfos = new List<MaterialInfo>();
        foreach (var surfaceAlphaTexture in surfaceAlphaTextures)
        {
            string relativePathToDTX = surfaceAlphaTexture.Key.Item1;
            float surfaceAlpha = surfaceAlphaTexture.Key.Item2;

            string newTextureName = UnityLookups.GetSurfaceAlphaMaterialName(relativePathToDTX, surfaceAlpha);

            var pathToMaterial = Path.ChangeExtension(Path.Combine(MaterialPath, newTextureName), "mat").ConvertFolderSeperators();
            Directory.CreateDirectory(Path.GetDirectoryName(pathToMaterial));

            if (CreateMaterial(surfaceAlphaTexture.Value, pathToMaterial, surfaceAlpha))
            {
                materialInfos.Add(new MaterialInfo { UnityPathAndFilenameToMaterial = pathToMaterial, UseBlendedTransparency = true });
            }
        }

        return materialInfos;
    }

    private static void AddSpritePathsToMaterialLookups(List<SPRModel> sprModels)
    {
        foreach (var sprModel in sprModels)
        {
            if (sprModel.DTXPaths == null || sprModel.DTXPaths.Length == 0)
            {
                continue;
            }

            var firstDTX = sprModel.DTXPaths[0];
            if (UnityLookups.MaterialLookups.TryGetValue(firstDTX, out var material))
            {
                UnityLookups.MaterialLookups.Add(sprModel.RelativePathToSprite, material);
            }
        }
    }

    private static List<MaterialInfo> CreateMaterials(List<DATModel> datModels)
    {
        AssetDatabase.StartAssetEditing();

        var textureBasedMaterialInfos = CreateMaterialsFromTextureLookups();
        var surfaceAlphaMaterialInfos = CreateSurfaceAlphaMaterials(datModels);

        var materialInfos = textureBasedMaterialInfos.Concat(surfaceAlphaMaterialInfos).ToList();

        RefreshAssetDatabase();

        return materialInfos;
    }

    private static void CreateLookups(List<DATModel> datModels, List<SPRModel> sprModels)
    {
        var materialInfos = CreateMaterials(datModels);

        SetMaterialAlphaForBlendedTextures(materialInfos);

        AddSpritePathsToMaterialLookups(sprModels);
    }

    private static bool TrySetExistingLookups(List<SPRModel> sprModels)
    {
        var matFiles = Directory.GetFiles(MaterialPath, "*.mat", SearchOption.AllDirectories);
        if (matFiles.Length == 0)
        {
            return false;
        }

        AssetDatabase.StartAssetEditing();

        foreach (var matFile in matFiles)
        {
            var relativePathToDTX = Path.ChangeExtension(Path.GetRelativePath(MaterialPath, matFile), "dtx");
            var material = AssetDatabase.LoadAssetAtPath<Material>(matFile);
            UnityLookups.MaterialLookups.Add(relativePathToDTX, material);
        }

        RefreshAssetDatabase();

        AddSpritePathsToMaterialLookups(sprModels);

        return true;
    }

    public static void SetLookups(bool alwaysCreate, List<DATModel> datModels, List<SPRModel> sprModels)
    {
        UnityLookups.MaterialLookups.Clear();

        if (!alwaysCreate)
        {
            if (TrySetExistingLookups(sprModels))
            {
                return;
            }
        }

        CreateLookups(datModels, sprModels);
    }
}
