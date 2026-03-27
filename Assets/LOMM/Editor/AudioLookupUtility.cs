using UnityEngine;
using UnityEditor;
using System.IO;

public class AudioLookupUtility : DataExtractor
{
    private static void CreateAudioClipAssets()
    {
        var wavFiles = Directory.GetFiles(ProjectFolder, "*.wav", SearchOption.AllDirectories);

        AssetDatabase.StartAssetEditing();
        int i = 0;
        foreach (var wavFile in wavFiles)
        {
            i++;
            float progress = (float)i / wavFiles.Length;
            EditorUtility.DisplayProgressBar("Creating Audio Clips", $"Item {i} of {wavFiles.Length}", progress);

            string relativePathToWAV = Path.GetRelativePath(ProjectFolder, wavFile);
            string unityPathToWAV = Path.Combine(AudioClipPath, relativePathToWAV);
            Directory.CreateDirectory(Path.GetDirectoryName(unityPathToWAV));
            File.Copy(wavFile, unityPathToWAV);

            AssetDatabase.ImportAsset(unityPathToWAV);
        }

        RefreshAssetDatabase();

        TrySetExistingLookups();
    }

    private static bool TrySetExistingLookups()
    {
        var wavFiles = Directory.GetFiles(AudioClipPath, "*.wav", SearchOption.AllDirectories);
        if (wavFiles.Length == 0)
        {
            return false;
        }

        foreach (var wavFile in wavFiles)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavFile);

            var relativePathToWAV = Path.GetRelativePath(AudioClipPath, wavFile);
            UnityLookups.AudioLookups.Add(relativePathToWAV, clip);
        }

        return true;
    }

    public static void SetLookups(bool alwaysCreate)
    {
        UnityLookups.AudioLookups.Clear();

        if (!alwaysCreate)
        {
            if (TrySetExistingLookups())
            {
                return;
            }
        }

        CreateAudioClipAssets();
    }
}
