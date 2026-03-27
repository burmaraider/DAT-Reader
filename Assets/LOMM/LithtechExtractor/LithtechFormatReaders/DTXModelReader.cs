using System.IO;

public static class DTXModelReader
{
    private static DTXHeaderModel ReadDTXHeader(BinaryReader reader)
    {
        DTXHeaderModel header = new DTXHeaderModel();
        header.ResourceType = reader.ReadUInt32();
        header.Version = reader.ReadInt32();
        header.BaseWidth = reader.ReadUInt16();
        header.BaseHeight = reader.ReadUInt16();
        header.MipMapCount = reader.ReadUInt16();
        header.SectionCount = reader.ReadUInt16();
        header.Flags = reader.ReadInt32();
        header.UserFlags = reader.ReadInt32();
        header.TextureGroup = reader.ReadByte();
        header.MipMapsToUse = reader.ReadByte();
        header.BPPFormat = reader.ReadByte();
        header.MipMapOffset = reader.ReadByte();
        header.MipMapTexCoordinateOffset = reader.ReadByte();
        header.TexturePriority = reader.ReadByte();
        header.DetailTextureScale = reader.ReadSingle();
        header.DetailTextureAngle = reader.ReadInt16();
        return header;
    }

    public static DTXModel ReadDTXModel(string fileNameAndPath, string relativePathToDTX)
    {
        if (!File.Exists(fileNameAndPath))
        {
            return null;
        }

        var model = new DTXModel();
        model.RelativePathToDTX = relativePathToDTX.ConvertFolderSeperators();

        using var binaryReader = new BinaryReader(File.Open(fileNameAndPath, FileMode.Open));
        model.Header = ReadDTXHeader(binaryReader);

        binaryReader.BaseStream.Position = 164; // Jump to texture data, ignore command strings (always 128 bytes)
        model.Data = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length - 164);
        binaryReader.Close();

        return model;
    }
}