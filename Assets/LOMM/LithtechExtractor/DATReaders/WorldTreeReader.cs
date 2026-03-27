using System.IO;

public static class WorldTreeReader
{
    private static void SkipLayout(BinaryReader binaryReader, ref byte nCurByte, ref byte nCurBit)
    {
        if (nCurBit == 8)
        {
            nCurByte = binaryReader.ReadByte();
            nCurBit = 0;
        }

        bool bSubdivide = (nCurByte & (1 << nCurBit)) > 0;
        nCurBit++;

        if (bSubdivide)
        {
            for (int i = 0; i < 4; i++)
            {
                SkipLayout(binaryReader, ref nCurByte, ref nCurBit);
            }
        }
    }

    public static void SkipWorldTree(BinaryReader binaryReader)
    {
        var vBoxMin = LTUtils.ReadLTVector(ref binaryReader);
        var vBoxMax = LTUtils.ReadLTVector(ref binaryReader);

        var nodeCount = binaryReader.ReadInt32();
        var terrainDepth = binaryReader.ReadInt32();

        byte flags = 0;
        byte bitFlagToCheck = 8;
        SkipLayout(binaryReader, ref flags, ref bitFlagToCheck);
    }
}