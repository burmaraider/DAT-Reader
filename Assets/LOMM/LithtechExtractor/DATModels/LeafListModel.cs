using System.Collections.Generic;

public class LeafListModel
{
    public short LeafListCount { get; set; }
    public short LeafListIndex { get; set; }
    public List<LeafModel> Leafs { get; set; } = new List<LeafModel>();
    public int PolyCount { get; set; }
    public short[] Polies { get; set; }
    public int Cardinal { get; set; }
}
