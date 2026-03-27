using System;
using System.IO;
using UnityEngine;

public static class WAVReader
{
    public static AudioClip ReadWAVModel(string filename)
    {
        byte[] fileBytes = File.ReadAllBytes(filename);
        int channels = BitConverter.ToInt16(fileBytes, 22);
        int sampleRate = BitConverter.ToInt32(fileBytes, 24);
        int subchunk2 = BitConverter.ToInt32(fileBytes, 40);
        int samples = subchunk2 / 2; // 16-bit audio = 2 bytes per sample

        float[] data = new float[samples];
        int offset = 44;
        for (int i = 0; i < samples; i++)
        {
            short sample = BitConverter.ToInt16(fileBytes, offset);
            data[i] = sample / 32768f;
            offset += 2;
        }

        AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(filename), samples / channels, channels, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}