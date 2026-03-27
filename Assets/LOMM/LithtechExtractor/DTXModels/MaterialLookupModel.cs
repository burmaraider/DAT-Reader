using System.Collections.Generic;
using UnityEngine;

public class MaterialLookupModel2
{
    public UnityDTXModel DTXModel { get; set; }
    public string RelativeLookupPath { get; set; }
    public string PathToPNG { get; set; }
    public string Name { get; set; }
    public Material Material { get; set; }
    public float? SurfaceAlpha { get; set; }
    public List<string> RelativeSpritePaths { get; set; } = new List<string>();
}