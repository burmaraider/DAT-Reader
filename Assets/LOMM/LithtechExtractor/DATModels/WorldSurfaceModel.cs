using static LTTypes;

public class WorldSurfaceModel
{
    public LTVector UV1 { get; set; }
    public LTVector UV2 { get; set; }
    public LTVector UV3 { get; set; }
    public short TextureIndex { get; set; }
    public int Flags { get; set; }
    public byte Unknown1 { get; set; }
    public byte Unknown2 { get; set; }
    public byte Unknown3 { get; set; }
    public byte Unknown4 { get; set; }
    public byte UseEffect { get; set; }
    public string EffectName { get; set; }
    public string EffectParams { get; set; }
    public short TextureFlags { get; set; }
}
