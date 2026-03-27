using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class UnityLookups
{
    private static Dictionary<string, Material> MaterialByNameLookups { get; set; } = null;

    public static Dictionary<string, TextureLookupModel> TextureLookups { get; set; } = new Dictionary<string, TextureLookupModel>(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, Material> MaterialLookups { get; set; } = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, string> ABCMeshLookups { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<(string, string), GameObject> ABCPrefabLookups { get; set; } = new Dictionary<(string, string), GameObject>(new StringStringComparer());
    public static Dictionary<string, AudioClip> AudioLookups { get; set; } = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

    private static void BuildMaterialByNameLookups()
    {
        if (MaterialByNameLookups == null)
        {
            MaterialByNameLookups = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        }

        foreach(var lookup in MaterialLookups)
        {
            var name = Path.GetFileNameWithoutExtension(lookup.Key);
            if(!MaterialByNameLookups.ContainsKey(name))
            {
                MaterialByNameLookups.Add(name, lookup.Value);
            }
        }
    }

    /// <summary>
    /// Takes in a relative path to a texture, such as:
    ///         textures\leveltextures\terrain\ocean.dtx
    /// and returns:
    ///         textures\leveltextures\terrain\ocean_SurfaceAlpha_0.7.dtx
    /// </summary>
    /// <param name="textureName"></param>
    /// <param name="surfaceAlpha"></param>
    /// <returns></returns>
    public static string GetSurfaceAlphaMaterialName(string textureName, float surfaceAlpha)
    {
        string path = Path.GetDirectoryName(textureName);
        string filename = Path.GetFileNameWithoutExtension(textureName);
        string extension = Path.GetExtension(textureName);
        string newFilename = $"{filename}_SurfaceAlpha_{surfaceAlpha}";
        string newPath = Path.Combine(path, newFilename + extension);

        return newPath;
    }

    public static Material GetMaterial(string relativePathToDTXorSPR)
    {
        if (MaterialLookups.TryGetValue(relativePathToDTXorSPR, out var material))
        {
            return material;
        }

        return null;
    }

    public static Material GetMaterial(string relativePathToDTXorSPR, float surfaceAlpha)
    {
        string lookupPath = relativePathToDTXorSPR;
        var surfaceAlphaMaterialName = GetSurfaceAlphaMaterialName(lookupPath, surfaceAlpha);
        return GetMaterial(surfaceAlphaMaterialName);
    }

    public static Material GetMaterialByName(string name)
    {
        BuildMaterialByNameLookups();
        if (MaterialByNameLookups.TryGetValue(name, out var material))
        {
            return material;
        }

        return null;
    }

    public static GameObject GetABCPrefab(string relativePathToABC, string skins)
    {
        if (ABCPrefabLookups.TryGetValue((relativePathToABC, skins), out var foundPrefab))
        {
            return foundPrefab;
        }

        return null;
    }

    public static GameObject GetFirstMatchingABCPrefab(string relativePathToABC)
    {
        var match = ABCPrefabLookups.FirstOrDefault(grp => grp.Key.Item1.Equals(relativePathToABC, StringComparison.OrdinalIgnoreCase)).Value;

        return match;
    }

    public static AudioClip GetAudioClip(string relativePathToWAV)
    {
        if (AudioLookups.TryGetValue(relativePathToWAV, out var audioClip))
        {
            return audioClip;
        }

        return null;
    }
}
