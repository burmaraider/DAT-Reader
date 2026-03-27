using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DTXConverter
{
    private static bool ShowLogError = false;

    /// <summary>
    /// Converts a Texture2D to ARGB32.
    /// </summary>
    /// <param name="originalTexture"></param>
    private static Texture2D ConvertTextureToArgb32(Texture2D originalTexture)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            originalTexture.width,
            originalTexture.height,
            0, // depthBuffer
            RenderTextureFormat.Default,
            RenderTextureReadWrite.sRGB);

        Graphics.Blit(originalTexture, renderTexture);

        Texture2D newTex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false);

        var oldRenderTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;
        newTex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        newTex.Apply();
        RenderTexture.active = oldRenderTexture;
        RenderTexture.ReleaseTemporary(renderTexture);

        return newTex;
    }

    private static TextureFormat GetTextureFormat(byte bytesPerPixel)
    {
        if (bytesPerPixel == (byte)DTXBPP.BPP_S3TC_DXT5) return TextureFormat.DXT5;
        if (bytesPerPixel == (byte)DTXBPP.BPP_S3TC_DXT3) return TextureFormat.DXT5Crunched; // we use crunched as dxt3
        if (bytesPerPixel == (byte)DTXBPP.BPP_S3TC_DXT1) return TextureFormat.DXT1;
        if (bytesPerPixel == (byte)DTXBPP.BPP_32) return TextureFormat.BGRA32;
        return TextureFormat.BGRA32; // Default to BGRA32
    }

    private static Texture2D ReadTextureData(DTXModel model)
    {
        //Version check must come before everything else!!
		if (model.Header.Version == DTXVersions.LT1
            || model.Header.Version == DTXVersions.LT15
            || (model.Header.Version != DTXVersions.LT2 && model.Header.BPPFormat == (byte)DTXBPP.BPP_8P))
        {
            return Create8BitPaletteTexture(model);
        }
        else if (
            model.Header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT1
            || model.Header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT3
            || model.Header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT5)
        {
            return CreateCompressedTexture(model);
        }
        else if (model.Header.BPPFormat == (byte)DTXBPP.BPP_32)
        {
            return Create32bitTexture(model);
        }
        else if (model.Header.BPPFormat == (byte)DTXBPP.BPP_32P)
        {
            return Create32BitPaletteTexture(model);
        }

        return Create32bitTexture(model);
    }

    private static TransparencyTypes FlipTextureAndGetTransparencyType(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;

        Color[] pixels = texture.GetPixels();
        Color[] flipped = new Color[pixels.Length];

        bool foundWhite = false;
        bool foundBlack = false;
        bool foundGrey = false;
        for (int y = 0; y < height; y++)
        {
            int srcRow = y * width;
            int dstRow = (height - 1 - y) * width;
            for (int x = 0; x < width; x++)
            {
                flipped[dstRow + x] = pixels[srcRow + x];
                if (pixels[srcRow + x].a == 1)
                {
                    foundWhite = true;
                }
                else if (pixels[srcRow + x].a == 0)
                {
                    foundBlack = true;
                }
                else
                {
                    foundGrey = true;
                }
            }
        }

        texture.SetPixels(flipped);
        texture.Apply();

        if (foundGrey)
        {
            return TransparencyTypes.BlendedTransparency;
        }

        if (foundBlack && foundWhite)
        {
            return TransparencyTypes.ClipOnly;
        }

        return TransparencyTypes.NoTransparency;
    }

    private static Texture2D Create32bitTexture(DTXModel model)
    {
        // BGRA32 ?
        if (!model.Header.Prefer4444)
        {
            // Force alpha to 1.0 for everything:
            for (int i = 0; i < model.Data.Length; i += 4)
            {
                model.Data[i + 3] = 255; // Set alpha (A) to 255
            }
        }

        var texture2D = new Texture2D(model.Header.BaseWidth, model.Header.BaseHeight, TextureFormat.BGRA32, false);
        try
        {
            texture2D.LoadRawTextureData(model.Data);
        }
        catch(Exception ex)
        {
            Debug.LogError("Ex: " + ex.Message);
            return null;
        }

        texture2D.Apply();

        return texture2D;
    }

    private static Texture2D CreateCompressedTexture(DTXModel model)
    {
        // DXT1 - Default
        TextureFormat textureFormat = GetTextureFormat(model.Header.BPPFormat);
        TextureFormat format = TextureFormat.DXT1;

        int scale = 8; // Extra bytes needed in the decoding process

        if (model.Header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT3)
        {
            // Not supported - no TextureFormat.DXT3 support.
            // If possible, code would:
            //      format = TextureFormat.DXT3;
            //      scale = 16;
            return null;
        }
        else if (model.Header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT5)
        {
            format = TextureFormat.DXT5;
            scale = 16;
        }

        var compressed_width = (int)((model.Header.BaseWidth + 3) / 4);
        var compressed_height = (int)((model.Header.BaseHeight + 3) / 4);

        var bytesToKeep = compressed_width * compressed_height * scale;
        var compressedData = model.Data.Take(bytesToKeep).ToArray();

        var texture2D = new Texture2D(model.Header.BaseWidth, model.Header.BaseHeight, format, false);
        try
        {
            texture2D.LoadRawTextureData(compressedData);
        }
        catch (Exception ex)
        {
            Debug.LogError("Ex: " + ex.Message);
            return null;
        }

        texture2D.Apply();

        return texture2D;
    }

    private static Texture2D Create8BitPaletteTexture(DTXModel model)
    {
        var palette = new List<Color32>();

        var expectedSize = 8 + (256 * 4) + (model.Header.BaseWidth * model.Header.BaseHeight);
        if (model.Data.Length < expectedSize)
        {
            if (ShowLogError)
            {
                Debug.LogError("Invalid DTX? Not enough data to support an 8 bit palette texture!");
            }

            return null;
        }

        // Two unknown ints
        // Used for the internal get palette function in LT1.
        var paletteHeader1 = BitConverter.ToInt32(model.Data, 0);
        var paletteHeader2 = BitConverter.ToInt32(model.Data, 4);

        // Read the colors from the palette.
        for (int i = 0; i < 256;i++)
        {
            var r = model.Data[8 + (i * 4) + 0];
            var g = model.Data[8 + (i * 4) + 1];
            var b = model.Data[8 + (i * 4) + 2];
            var a = model.Data[8 + (i * 4) + 3];

            var c = new Color32(r, g, b, a);
            palette.Add(c);
        }

        // I alpha is always 0 - convert to white so everything shows.
        if (palette.All(c => c.a == 0))
        {
            palette.ForEach(x => x.a = 255);
        }

        // Read the data - just indexes into the palette above.
        int bytesToSkip = 8 + (256 * 4);
        var data = model.Data.Skip(bytesToSkip).Take(model.Header.BaseWidth * model.Header.BaseHeight).ToArray();

        // # Apply the palette
        var colorData = new List<byte>();
        int dataIndex = 0;
        while (dataIndex < data.Length)
        {
            var color = palette[data[dataIndex]];
            colorData.Add(color.r);
            colorData.Add(color.g);
            colorData.Add(color.b);
            colorData.Add(color.a);

            dataIndex += 1;
        }

        var texture2D = new Texture2D(model.Header.BaseWidth, model.Header.BaseHeight, TextureFormat.RGBA32, false);
        try
        {
            texture2D.LoadRawTextureData(colorData.ToArray());
        }
        catch (Exception ex)
        {
            Debug.LogError("Ex: " + ex.Message);
            return null;
        }

        texture2D.Apply();

        return texture2D;
    }

    private static Texture2D Create32BitPaletteTexture(DTXModel model)
    {
        if (model.Header.SectionCount != 1)
        {
            // Should be 1 section for a 32bit palette texture.
            throw new Exception("Expected SectionCount of 1 for a 32bit palette texture");
        }

        var palette = new List<Color32>();

        int dataSize = model.Header.BaseWidth * model.Header.BaseHeight;
        var data = model.Data.Take(dataSize).ToArray();
        var colorData = new List<byte>();

        int skipCount = 0;
        int width = model.Header.BaseWidth;
        int height = model.Header.BaseHeight;
        // Should this be "i < MipMapCount - 1" or "i < MipMapCount" ?
        for (int i = 0; i < model.Header.MipMapCount - 1; i++)
        {
            width /= 2;
            height /= 2;
            var skip = width * height;
            skipCount += skip;
        }

        // Skip
        skipCount += 16; // Skip section type data
        skipCount += 10; // Skip 10 bytes
        skipCount += 2; // Skip 2 byte filler
        skipCount += 4; // Skip Section length (4x byte int)

        // Read the colors from the palette.
        for (int i = 0; i < 256; i++)
        {
            var a = model.Data[dataSize + skipCount + (i * 4) + 0];
            var r = model.Data[dataSize + skipCount + (i * 4) + 1];
            var g = model.Data[dataSize + skipCount + (i * 4) + 2];
            var b = model.Data[dataSize + skipCount + (i * 4) + 3];

            var c = new Color32(r, g, b, a);
            palette.Add(c);
        }

        // # Apply the palette
        int dataIndex = 0;
        while (dataIndex < data.Length)
        {
            var color = palette[data[dataIndex]];
            colorData.Add(color.r);
            colorData.Add(color.g);
            colorData.Add(color.b);
            colorData.Add(color.a);

            dataIndex += 1;
        }

        var texture2D = new Texture2D(model.Header.BaseWidth, model.Header.BaseHeight, TextureFormat.RGBA32, false);
        try
        {
            texture2D.LoadRawTextureData(colorData.ToArray());
        }
        catch (Exception ex)
        {
            Debug.LogError("Ex: " + ex.Message);
            return null;
        }

        texture2D.Apply();

        return texture2D;
    }

    public static UnityDTXModel ConvertDTX(DTXModel model)
    {
        var texture2d = ReadTextureData(model);
        if (texture2d == null)
        {
            return null;
        }

        texture2d.wrapMode = TextureWrapMode.Repeat;
        Texture2D convertedTexture2D = ConvertTextureToArgb32(texture2d);
        if (convertedTexture2D == null)
        {
            return null;
        }

        convertedTexture2D.wrapMode = TextureWrapMode.Repeat;

        var transparencyType = FlipTextureAndGetTransparencyType(convertedTexture2D);

        var unityDTX = new UnityDTXModel
        {
            DTXModel = model,
            Texture2D = convertedTexture2D,
            TransparencyType = transparencyType
        };

        return unityDTX;
    }
}