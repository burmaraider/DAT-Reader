using System.IO;

public static class WorldModelReader
{
    private static WorldExtentsModel ReadExtents(BinaryReader b, int version)
    {
        var model = new WorldExtentsModel();
        
        if (version == DATVersions.Version56) //SHOGO
        {
            b.BaseStream.Position += 8; // skip some padding
        }
        else
        {
            model.LMGridSize = b.ReadSingle();
            model.ExtentsMin = LTUtils.ReadLTVector(ref b);
            model.ExtentsMax = LTUtils.ReadLTVector(ref b);
        }

        return model;
    }

    private static string ReadProperties(BinaryReader b)
    {
        int stringLength = b.ReadInt32();
        if (stringLength > 0)
        {
            return LTUtils.ReadString(stringLength, ref b);
        }

        return null;
    }

    private static WorldHeaderModel ReadHeader(BinaryReader b, int version)
    {
        var header = new WorldHeaderModel();
        header.ObjectDataPos = b.ReadInt32();
        header.RenderDataPos = b.ReadInt32();

        if (version != DATVersions.Version56)
        {
            header.Dummy1 = b.ReadInt32();
            header.Dummy2 = b.ReadInt32();
            header.Dummy3 = b.ReadInt32();
            header.Dummy4 = b.ReadInt32();
            header.Dummy5 = b.ReadInt32();
            header.Dummy6 = b.ReadInt32();
            header.Dummy7 = b.ReadInt32();
            header.Dummy8 = b.ReadInt32();
        }

        return header;
    }

    public static WorldModel ReadWorldModel(BinaryReader b, int version)
    {
        var model = new WorldModel();
        model.Header = ReadHeader(b, version);
        model.WorldProperties = ReadProperties(b);
        model.Extents = ReadExtents(b, version);

        return model;
    }
}