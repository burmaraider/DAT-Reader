using System;

public class DTXHeaderModel
{
    public UInt32 ResourceType { get; set; }
    public Int32 Version { get; set; }
    public UInt16 BaseWidth { get; set; }
    public UInt16 BaseHeight { get; set; }
    public UInt16 MipMapCount { get; set; }
    public UInt16 SectionCount { get; set; }
    public Int32 Flags { get; set; }
    public Int32 UserFlags { get; set; }
    public byte TextureGroup { get; set; }
    public byte MipMapsToUse { get; set; }
    public byte BPPFormat { get; set; }
    public byte MipMapOffset { get; set; }
    public byte MipMapTexCoordinateOffset { get; set; }
    public byte TexturePriority { get; set; }
    public float DetailTextureScale { get; set; }
    public Int16 DetailTextureAngle { get; set; }

    public bool UseFullBright
    {
        get
        {
            return (Flags & (int)DTXFlags.FULLBRIGHT) != 0;
        }
    }

    public bool Prefer4444
    {
        get
        {
            return (Flags & (int)DTXFlags.PREFER4444) != 0;
        }
    }
}
