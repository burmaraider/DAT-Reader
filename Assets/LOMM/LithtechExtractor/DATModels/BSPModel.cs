using System;
using System.Collections.Generic;
using static LTTypes;

public class BSPModel
{
    public Int16 WorldInfoFlags { get; set; }
    public string WorldName { get; set; }
    public int PointCount { get; set; }
    public int PlaneCount { get; set; }
    public int SurfaceCount { get; set; }
    public int UserPortalCount { get; set; }
    public int PolyCount { get; set; }
    public int LeafCount { get; set; }
    public int VertCount { get; set; }
    public int TotalVisListSize { get; set; }
    public int LeafListCount { get; set; }
    public int NodeCount { get; set; }
    public int SectionCount { get; set; }
    public LTVector MinBox { get; set; }
    public LTVector MaxBox { get; set; }
    public LTVector WorldTranslation { get; set; }
    public int NamesLen { get; set; }
    public int TextureCount { get; set; }
    public List<string> TextureNames { get; set; } = new List<string>();
    public List<WorldPolyModel> Polies { get; set; } = new List<WorldPolyModel>();
    public List<LeafListModel> Leafs { get; set; } = new List<LeafListModel>();
    public List<WorldPlaneModel> Planes { get; set; } = new List<WorldPlaneModel>();
    public List<WorldSurfaceModel> Surfaces { get; set; } = new List<WorldSurfaceModel>();
    public List<LTVector> Vertices { get; set; } = new List<LTVector>();

    public int Version { get; set; }
    public Game GameType { get; set; }
}