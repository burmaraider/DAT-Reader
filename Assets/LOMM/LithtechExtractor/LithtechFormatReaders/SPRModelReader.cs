using System.IO;

public static class SPRModelReader
{
    public static SPRModel ReadSPRModel(string projectPath, string relativePathToSPR)
    {
        relativePathToSPR = relativePathToSPR.ConvertFolderSeperators();
        string filenameWithFullPath = Path.Combine(projectPath, relativePathToSPR);
        if (!File.Exists(filenameWithFullPath))
        {
            return null;
        }

        using BinaryReader binaryReader = new BinaryReader(File.Open(filenameWithFullPath, FileMode.Open));

        var dtxCount = binaryReader.ReadInt32();

        // Jump past the header and read the first texture.
        binaryReader.BaseStream.Position = 20; 
        var unitySPR = new SPRModel
        {
            RelativePathToSprite = relativePathToSPR,
            DTXPaths = new string[dtxCount]
        };

        for (int i = 0; i < dtxCount; i++)
        {
            int strLength = binaryReader.ReadUInt16();
            byte[] strData = binaryReader.ReadBytes(strLength);
            string dtxPath = System.Text.Encoding.UTF8.GetString(strData);
            unitySPR.DTXPaths[i] = dtxPath.ConvertFolderSeperators();
        }

        binaryReader.Close();

        return unitySPR;
    }
}