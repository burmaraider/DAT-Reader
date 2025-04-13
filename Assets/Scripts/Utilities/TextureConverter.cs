using UnityEngine;
using System.IO;

public class TextureConverter : MonoBehaviour
{
    private static readonly string SourceRootFolder = "c:\\LOMM\\Data\\";
    private static readonly string DestinationRootFolder = "C:\\temp\\LOMM\\Converted\\";

    public void OnEnable()
    {
        UIActionManager.OnExportTextures += ExportTextures;
    }

    public void OnDisable()
    {
        UIActionManager.OnExportTextures -= ExportTextures;
    }

    private void ExportTextures()
    {
        var files = Directory.GetFiles(SourceRootFolder, "*.dtx", SearchOption.AllDirectories);
        foreach(var file in files)
        {
            var relativePath = Path.GetRelativePath(SourceRootFolder, file);
            var unityDtx = DTX.LoadDTX(SourceRootFolder, relativePath);
            if (unityDtx == null)
            {
                Debug.LogError($"Got back null for file {file}");
                continue;
            }

            var newFilePathAndName = Path.ChangeExtension(Path.Combine(DestinationRootFolder, relativePath), ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(newFilePathAndName));

            byte[] pngBytes = unityDtx.Texture2D.EncodeToPNG();
            File.WriteAllBytes(newFilePathAndName, pngBytes);
        }
    }
}