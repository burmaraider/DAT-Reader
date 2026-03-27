using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text;

public class Testing : DataExtractor
{
    private static void ShowObjectsWithProperty(List<DATModel> datModels, string propName, bool rescueOnly, bool excludeAIRail = true, bool excludeExtras = false, string excludeValue = null, bool excludeInvisible=false)
    {
        var filteredModels = datModels.Where(dat => !rescueOnly || Path.GetFileNameWithoutExtension(dat.Filename) == "_RESCUEATTHERUINS").ToList();
        var matchingWorldObjects = filteredModels
        .SelectMany(
            dat => dat.WorldObjects
            .Where(wo => wo.options.ContainsKey(propName) && (!excludeAIRail || wo.ObjectType != "AIRail"))
            .Select(
                wo => new
                {
                    World = Path.GetFileNameWithoutExtension(dat.Filename),
                    WorldObjectName = wo.Name,
                    WorldObjectType = wo.ObjectType,
                    WorldObjectFilename = wo.Filename,
                    WorldObjectSkin = wo.Skin,
                    Val = wo.Properties.FirstOrDefault(x => x.Name.Equals(propName, System.StringComparison.OrdinalIgnoreCase)).Value
                }))
                .ToList();

        var s = $"Testing {propName}";
        if (excludeExtras)
        {
            s += $" (excluding extra object types)";
            matchingWorldObjects = matchingWorldObjects.Where(x =>
                    x.WorldObjectType != "PortalZone"
                    && x.WorldObjectType != "BuyZone"
                    && x.WorldObjectType != "SoftLandingZone"
                    && x.WorldObjectType != "Wind"
                    && x.WorldObjectType != "Timer"
                    && x.WorldObjectType != "DamageBrush"
                    && x.WorldObjectType != "AIBarrier"
                    && x.WorldObjectType != "Ladder")
            .ToList();
        }

        if (excludeInvisible)
        {
            s += $" (excluding invis)";
            matchingWorldObjects = matchingWorldObjects.Where(x => x.WorldObjectType != "InvisibleBrush")
            .ToList();
        }
        

        if (excludeValue != null)
        {
            s += $" (excluding {propName}={excludeValue})";
            matchingWorldObjects = matchingWorldObjects.Where(x => x.Val != excludeValue).ToList();
        }

        s += ":\r\n";
        foreach (var x in matchingWorldObjects)
        {
            s += $"{x.World}-{x.WorldObjectName} ({x.WorldObjectType}): {propName}={x.Val} (Filename={x.WorldObjectFilename} | Skin={x.WorldObjectSkin})\r\n";
        }

        Debug.Log(s);
    }

    private static void ShowBSPObjectsWithRotationInDegrees(List<DATModel> datModels)
    {
        var matchingWorldObjects = datModels
            .SelectMany(
                dat => dat.WorldObjects
                    .Where(wo => wo.ObjectType != "AIRail")
                    .Where(wo => wo.IsBSP && wo.RotationInDegrees.HasValue && (wo.RotationInDegrees.Value.x != 0 || wo.RotationInDegrees.Value.y != 0 || wo.RotationInDegrees.Value.z != 0))
                    .Select(
                        wo => new
                        {
                            World = Path.GetFileNameWithoutExtension(dat.Filename),
                            WorldObjectName = wo.Name,
                            WorldObjectType = wo.ObjectType,
                            WorldObjectFilename = wo.Filename,
                            WorldObjectSkin = wo.Skin,
                            Val = wo.RotationInDegrees
                        }))
                        .ToList();

        var s = $"Testing BSP with RotationInDegrees";

        s += ":\r\n";
        foreach (var x in matchingWorldObjects)
        {
            s += $"{x.World}-{x.WorldObjectName} ({x.WorldObjectType}): RotationInDegrees={x.Val} (Filename={x.WorldObjectFilename} | Skin={x.WorldObjectSkin})\r\n";
        }

        Debug.Log(s);
    }

    private static void ShowWorldObjectsWithFileProps(List<DATModel> datModels)
    {

        // Ignore ObjectType = WorldProperties
        //      They generally have 3 properties, but none point to real textures:
        //          BLOODFEUD-WorldProperties0 (WorldProperties): Prop(EnvironmentMap)=TEXTURES\ENVIRONMENTMAPS\OutdoorTest.dtx
        //          BLOODFEUD - WorldProperties0(WorldProperties): Prop(SoftSky) = textures\environmentmaps\clouds\clouds.dtx
        //          BLOODFEUD - WorldProperties0(WorldProperties): Prop(PanSkyTexture) = Textures\SkyPan.dtx
        var matches = datModels
            .SelectMany(dat => dat.WorldObjects
                .Where(wo => wo.ObjectType != "DestructableBrush" && wo.ObjectType != "DestructableProp" && wo.ObjectType != "WorldProperties")
                .SelectMany(wo => wo.Properties
                    .Where(prop => 
                        prop.PropType == LTTypes.PropType.String
                        && prop.StringValue != null
                        && prop.Name != "Filename"
                        && prop.Name != "Skin"
                        && Path.GetExtension(prop.Value).ToLower() != ".wav"
                        && Path.GetExtension(prop.Value).ToLower() != ".scr"
                        && prop.StringValue.Contains("."))
                    .Select(prop => 
                        new
                        {
                            World = Path.GetFileNameWithoutExtension(dat.Filename),
                            wo.Name,
                            wo.ObjectType,
                            PropertyName = prop.Name,
                            PropertyValue = prop.StringValue
                        })))
            .ToList();

        var groupedMatches = matches.GroupBy(x => new { x.ObjectType, x.PropertyName, x.PropertyValue });

        var s = $"Testing WorldObjects with filename-like properties\r\n";
        foreach (var x in groupedMatches)
        {
            s += $"ObjectType={x.Key.ObjectType} : Prop({x.Key.PropertyName})={x.Key.PropertyValue} (count={x.Count()})\r\n";
        }

        Debug.Log(s);
    }

    private static void ShowAllProperties(List<DATModel> datModels, string worldName, string worldObjectName)
    {
        var s = $"Properties for {worldName}-{worldObjectName}\r\n";
        var dat = datModels.FirstOrDefault(dat => Path.GetFileNameWithoutExtension(dat.Filename) == worldName);
        var blueWater = dat.WorldObjects.FirstOrDefault(wo => wo.Name == worldObjectName);
        foreach (var prop in blueWater.Properties)
        {
            s += prop + Environment.NewLine;
        }
        Debug.Log(s);
    }

    private static void ShowWorldObjectsWithVisibleVolumes(List<DATModel> datModels)
    {
        var worldObjectsWithSurfaceAlpha = datModels.SelectMany(
            dat => dat.WorldObjects
                .Where(x => x.WorldObjectType == WorldObjectTypes.VisibleVolume)
                .SelectMany(
                    wo => wo.Properties
                            .Where(prop => prop.Name == "SurfaceAlpha")
                            .Select(prop => new {
                                DATFilename = dat.Filename,
                                WorldObjectName = wo.Name,
                                wo.ObjectType,
                                SurfaceAlpha = prop.Value,
                                wo.WorldObjectType
                            })))
            .ToList();

        var bspNames = datModels.SelectMany(dat => dat.BSPModels.Select(x => new { DATFilename = dat.Filename, BSPName = x.WorldName })).ToList();

        worldObjectsWithSurfaceAlpha = worldObjectsWithSurfaceAlpha.Where(wo => bspNames.Any(bsp => bsp.DATFilename == wo.DATFilename && bsp.BSPName == wo.WorldObjectName)).ToList();

        var s = $"Found {worldObjectsWithSurfaceAlpha.Count} matches\r\n";
        foreach (var worldObject in worldObjectsWithSurfaceAlpha)
        {
            s += $"{Path.GetFileNameWithoutExtension(worldObject.DATFilename)}-{worldObject.WorldObjectName}-{worldObject.ObjectType}={worldObject.SurfaceAlpha}\r\n";
        }
        Debug.Log(s);
    }

    private static void ShowTexturesThatNeedSurfaceAlpha(List<DATModel> datModels)
    {
        var worldObjectsWithAlpha = datModels.SelectMany(
            dat => dat.WorldObjects
                .Where(wo => wo.WorldObjectType == WorldObjectTypes.VisibleVolume)
                .SelectMany(wo => wo.Properties
                    .Where(prop => prop.Name == "SurfaceAlpha")
                    .Select(prop => new { DATFilename = dat.Filename, WorldObjectName = wo.Name, SurfaceAlpha = prop.FloatValue })))
            .ToList();

        List<TextureWithAlphaModel> texturesWithAlpha = new List<TextureWithAlphaModel>();
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

                        if (!texturesWithAlpha.Any(x => x.TextureName.Equals(textureName, StringComparison.OrdinalIgnoreCase) && x.SurfaceAlpha == matchingWorldObject.SurfaceAlpha))
                        {
                            var textureWithAlpha = new TextureWithAlphaModel { TextureName = textureName, SurfaceAlpha = matchingWorldObject.SurfaceAlpha };
                            texturesWithAlpha.Add(textureWithAlpha);
                        }
                    }
                }
            }
        }

        string s = $"Textures with Alpha - count = {texturesWithAlpha.Count}";
        foreach (var texture in texturesWithAlpha)
        {
            s += $"{texture.TextureName} = {texture.SurfaceAlpha}\r\n";
        }

        Debug.Log(s);
    }

    private static void ShowModelsWithMultipleMaterials(List<ABCModel> abcModels)
    {
        var abcModelsWithMultipleMaterials = abcModels
            .Where(abc => abc.PiecesChunk.GetTotalTextureCount() > 1)
            .ToList();

        var s = "Testing ABC models with multiple materials\r\n";
        foreach(var abcModel in abcModelsWithMultipleMaterials)
        {
            s += $"\tTotalTextureCount={abcModel.PiecesChunk.GetTotalTextureCount()} | "
                + $"PieceCount={abcModel.PiecesChunk.Pieces.Count} | Piece0.Faces={abcModel.PiecesChunk.Pieces[0].LODs[0].Faces.Count} | Piece1.Faces={abcModel.PiecesChunk.Pieces[1].LODs[0].Faces.Count}"
                + $" | Piece0.Vertices={abcModel.PiecesChunk.Pieces[0].LODs[0].Vertices.Count} | Piece1.Vertices={abcModel.PiecesChunk.Pieces[1].LODs[0].Vertices.Count}"
                + $": {abcModel.RelativePathToABCFileLowercase}\r\n";
        }

        Debug.Log(s);
    }

    private static void ShowDuplicateWorldObjectNames(List<DATModel> datModels)
    {
        var worldObjects = datModels.SelectMany(dat => dat.WorldObjects.Select(wo => new { dat.Filename, wo.Name })).ToList();

        var duplicates = worldObjects.GroupBy(x => (x.Filename, x.Name))
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();

        string s = "Duplicate World Objects\r\n";
        foreach (var item in duplicates)
        {
            s += $"\t{item.Filename} - {item.Name}\r\n";
        }

        Debug.Log(s);
    }

    private static void ShowStartPoints(List<DATModel> datModels)
    {
        foreach (var dat in datModels.Where(dat => Path.GetFileNameWithoutExtension(dat.Filename) == "_RESCUEATTHERUINS"))
        {
            string s = $"{dat.Filename}\r\n";
            foreach (var wo in dat.WorldObjects.Where(wo => wo.ObjectType == "StartPoint"))
            {
                s += $"\t{wo.Name}\r\n";
                foreach (var prop in wo.Properties)
                {
                    s += $"\t\t{prop}\r\n";
                }
            }

            Debug.Log(s);
        }
    }

    [MenuItem("Tools/Test All")]
    public static void TestAll()
    {
        //var sprModels = GetAllSPRModels();
        //var datModels = GetAllDATModels();

        //ShowObjectsWithProperty(datModels, "UseRotation", false, true);
        //ShowObjectsWithProperty(datModels, "Transparent", false, true, true, "False");
        //ShowObjectsWithProperty(datModels, "SurfaceAlpha", false, true, true);
        //ShowObjectsWithProperty(datModels, "Rayhit", false, false);
        //ShowObjectsWithProperty(datModels, "Visible", false, true, true, "True", true);
        //ShowObjectsWithProperty(datModels, "SpriteSurfaceName", false, true, true);

        //ShowBSPObjectsWithRotationInDegrees(datModels);

        //ShowWorldObjectsWithFileProps(datModels);

        //ShowAllProperties(datModels, "_RESCUEATTHERUINS", "BlueWater0");
        //ShowAllProperties(datModels, "WEDDINGDAY", "BlueWater0");
        //ShowAllProperties(datModels, "WEDDINGDAY", "BlueWater1");
        //ShowAllProperties(datModels, "WEDDINGDAY", "WorldObject12");

        //ShowWorldObjectsWithVisibleVolumes(datModels);

        //ShowTexturesThatNeedSurfaceAlpha(datModels);

        //var abcModels = GetABCModels();
        //ShowModelsWithMultipleMaterials(abcModels);
        //ShowDuplicateWorldObjectNames(datModels);

        ShowStatsDruidModel();

        //ShowStartPoints(datModels);
    }

    private static void ShowStatsDruidModel()
    {
        var files = Directory.GetFiles(ProjectFolder, "*.abc", SearchOption.AllDirectories);
        var druidFile = files.Where(x => Path.GetFileName(x).ToLower() == "druid.abc").FirstOrDefault();

        var druid = ABCModelReader.ReadABCModel(druidFile, ProjectFolder);

        StringBuilder s = new StringBuilder();
        s.AppendLine("Druid:");
        s.AppendLine($"\tPieces: {druid.PiecesChunk.Pieces.Count}");
        s.AppendLine("\tNodes:");
        s.AppendLine(druid.RootNode.ToFormattedString(false, 2));
        Debug.Log(s);
    }
}
