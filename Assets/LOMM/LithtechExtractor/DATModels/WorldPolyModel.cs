using System;
using System.Collections.Generic;
using static LTTypes;

public class WorldPolyModel
{
    public long IndexAndNumVerts { get; set; }
    public byte LoVerts { get; set; }
    public byte HiVerts{ get; set; }
    public LTVector Center { get; set; }
    public Int16 LightmapWidth { get; set; }
    public Int16 LightmapHeight { get; set; }
    public Int16 UnknownNum { get; set; }
    public Int16[] UnknownList { get; set; }
    public LTVector O { get; set; }
    public LTVector P { get; set; }
    public LTVector Q { get; set; }
    public int SurfaceIndex { get; set; }
    public int PlaneIndex { get; set; }
    public List<VertexColorModel> VertexColorList { get; set; }
}