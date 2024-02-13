using System;
using System.IO;
using static LithFAQ.LTTypes;
using static LithFAQ.LTUtils;

public class WorldReader
{
    public TWorldHeader WorldHeader = new TWorldHeader();
    public String WorldProperties;
    TWorldExtents WorldExtents = new TWorldExtents();

    public struct TWorldHeader
    {
        public int nVersion;
        public int dwObjectDataPos;
        public int dwRenderDataPos;
        public int dwDummy1;
        public int dwDummy2;
        public int dwDummy3;
        public int dwDummy4;
        public int dwDummy5;
        public int dwDummy6;
        public int dwDummy7;
        public int dwDummy8;
    }
    public struct TWorldExtents
    {
        public float fLMGridSize;
        public LTVector vExtentsMin;
        public LTVector vExtentsMax;
        public LTVector vOffset;
    }

    public void ReadHeader(ref BinaryReader b)
    {
        WorldHeader.nVersion = b.ReadInt32();

        WorldHeader.dwObjectDataPos = b.ReadInt32();
        WorldHeader.dwRenderDataPos = b.ReadInt32();
        
        if (WorldHeader.nVersion == 56)
        {
            return;
        }
        
        WorldHeader.dwDummy1 = b.ReadInt32();
        WorldHeader.dwDummy2 = b.ReadInt32();
        WorldHeader.dwDummy3 = b.ReadInt32();
        WorldHeader.dwDummy4 = b.ReadInt32();
        WorldHeader.dwDummy5 = b.ReadInt32();
        WorldHeader.dwDummy6 = b.ReadInt32();
        WorldHeader.dwDummy7 = b.ReadInt32();
        WorldHeader.dwDummy8 = b.ReadInt32();
    }

    public void ReadPropertiesAndExtents(ref BinaryReader b)
    {
        int nLen = b.ReadInt32();
        if(nLen > 0)
            WorldProperties = ReadString(nLen, ref b);

        if (WorldHeader.nVersion == 56) //SHOGO
        {
            b.BaseStream.Position += 8; // skip some padding
            return;
        }

        WorldExtents.fLMGridSize = b.ReadSingle();
        WorldExtents.vExtentsMin = ReadLTVector(ref b);
        WorldExtents.vExtentsMax = ReadLTVector(ref b);
    }

}