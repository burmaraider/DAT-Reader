using System.Collections.Generic;

public class PieceModel
{
    public ushort MaterialIndex { get; set; }
    public float SpecularPower { get; set; }
    public float SpecularScale { get; set; }
    public float LodWeight { get; set; }
    public ushort Padding { get; set; }
    public string Name { get; set; }
    public List<LODModel> LODs { get; set; }
}