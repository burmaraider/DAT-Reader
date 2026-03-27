using System;
using System.Collections.Generic;
using System.Linq;

public class DATModel
{
    public string Filename { get; set; }
    public int Version { get; set; }
    public WorldModel WorldModel { get; set; }
    public List<WorldObjectModel> WorldObjects { get; set; }
    public List<BSPModel> BSPModels { get; set; }

    public List<string> GetAllBSPTextures()
    {
        var distinctTextures = BSPModels.SelectMany(x => x.TextureNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctTextures;
    }

    public List<string> GetAllWorldObjectFilenames()
    {
        var distinctList = WorldObjects.Where(x => !string.IsNullOrEmpty(x.Filename)).Select(x => x.Filename)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctList;
    }

    public List<string> GetAllWorldObjectSkins()
    {
        var distinctList = WorldObjects.SelectMany(x => x.SkinsLowercase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctList;
    }

    public DATModel(int version)
    {
        Version = version;
        BSPModels = new List<BSPModel>();
    }

    public void SetAllWorldProperties()
    {
        foreach(var worldObject in WorldObjects)
        {
            worldObject.FlattenProperties();
        }
    }
}