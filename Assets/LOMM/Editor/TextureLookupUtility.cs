using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

public class TextureLookupUtility : DataExtractor
{
    private static void CreatePNGAssetAndSetTextureLookup(UnityDTXModel unityDTXModel)
    {
        try
        {
            byte[] pngData = unityDTXModel.Texture2D.EncodeToPNG();
            if (pngData == null || pngData.Length == 0)
            {
                if (ShowLogErrors)
                {
                    Debug.LogError($"Could not convert {unityDTXModel.DTXModel.RelativePathToDTX} to PNG!");
                }
            }

            string texturePath = Path.GetDirectoryName(Path.Combine(TexturePath, unityDTXModel.DTXModel.RelativePathToDTX)).ConvertFolderSeperators();
            Directory.CreateDirectory(texturePath);

            string pngFilenameOnly = Path.GetFileNameWithoutExtension(unityDTXModel.DTXModel.RelativePathToDTX) + ".png";
            string pathToPNG = Path.Combine(texturePath, pngFilenameOnly);
            File.WriteAllBytes(pathToPNG, pngData);

            var textureLookup = new TextureLookupModel
            {
                UnityPathAndFilenameToPNG = pathToPNG,
                UseFullbright = unityDTXModel.DTXModel.Header.UseFullBright,
                TransparencyType = unityDTXModel.TransparencyType
            };

            UnityLookups.TextureLookups.Add(unityDTXModel.DTXModel.RelativePathToDTX.ConvertFolderSeperators(), textureLookup);
        }
        catch (Exception ex)
        {
            if (ShowLogErrors)
            {
                Debug.LogError($"Error creating texture for {(unityDTXModel == null ? "<null>" : unityDTXModel.DTXModel.RelativePathToDTX)}: {ex.Message}");
            }
        }
    }

    private static void CreateLookups(List<UnityDTXModel> unityDTXModels)
    {
        AssetDatabase.StartAssetEditing();
        int i = 0;
        foreach (var unityDTXModel in unityDTXModels)
        {
            i++;
            float progress = (float)i / unityDTXModels.Count;
            EditorUtility.DisplayProgressBar("Creating Textures", $"Item {i} of {unityDTXModels.Count}", progress);

            CreatePNGAssetAndSetTextureLookup(unityDTXModel);
        }

        RefreshAssetDatabase();
    }

    private static bool TrySetExistingLookups(List<UnityDTXModel> unityDTXModels)
    {
        var pngFiles = Directory.GetFiles(TexturePath, "*.png", SearchOption.AllDirectories);
        if (pngFiles.Length == 0)
        {
            return false;
        }

        foreach (var pngFile in pngFiles)
        {
            var relativePathToDTX = Path.ChangeExtension(Path.GetRelativePath(TexturePath, pngFile), "dtx");
            var matchingUnityDTX = unityDTXModels.FirstOrDefault(x => relativePathToDTX.Equals(x.DTXModel.RelativePathToDTX, StringComparison.OrdinalIgnoreCase));
            if (matchingUnityDTX == null)
            {
                Debug.LogError($"Found PNG that has no texture reference: {pngFile}");
                continue;
            }

            UnityLookups.TextureLookups.Add(
                relativePathToDTX,
                new TextureLookupModel
                {
                    UnityPathAndFilenameToPNG = pngFile,
                    TransparencyType = matchingUnityDTX.TransparencyType,
                    UseFullbright = matchingUnityDTX.DTXModel.Header.UseFullBright
                });
        }

        return true;
    }

    public static void SetLookups(bool alwaysCreate)
    {
        UnityLookups.TextureLookups.Clear();

        var unityDTXModels = GetAllUnityDTXModels();

        if (!alwaysCreate)
        {
            if (TrySetExistingLookups(unityDTXModels))
            {
                return;
            }
        }

        CreateLookups(unityDTXModels);
    }
}
