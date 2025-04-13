using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static DTX;

public static class DTX
{
    private static int fileNumber = 0;
    private static readonly int DTX_VERSION_LT1 = -2;
    private static readonly int DTX_VERSION_LT15 = -3;
    private static readonly int DTX_VERSION_LT2 = -5;

    public enum DTXBPP : byte
    {
        BPP_8P = 0,
        BPP_8 = 1,
        BPP_16 = 2,
        BPP_32 = 3,
        BPP_S3TC_DXT1 = 4,
        BPP_S3TC_DXT3 = 5,
        BPP_S3TC_DXT5 = 6,
        BPP_32P = 7
    }

    [Flags]
    public enum DTXFlags : uint
    {
        FULLBRIGHT = (1U << 0),     // 1        Use full bright.
        PREFER16BIT = (1U << 1),    // 2        Prefer 16-bit mode.
        UNK1 = (1U << 2),           // 4        Each TextureMipData has its texture data allocated.
        UNK2 = (1U << 3),           // 8        Set to indicate this has a "fixed" section count. Originally the sections count was wrong.
        UNK3 = (1U << 4),           // 16
        UNK4 = (1U << 5),           // 32
        NOSYSCACHE = (1U << 6),     // 64       Tells the engine  to not keep a system memory copy of the texture.
        PREFER4444 = (1U << 7),     // 128      If in 16-bit mode, use a 4444 texture for this.
        PREFER5551 = (1U << 8),     // 256      Use 5551 if 16-bit.
        _32BITSYSCOPY = (1 << 9),   // 512      If there is a sys copy - don't convert it to device specific format (keep it 32 bit).
        DTX_CUBEMAP = (1 << 10),    // 1024     Cube environment map.  +x is stored in the normal data area, -x,+y,-y,+z,-z are stored in their own sections
        DTX_BUMPMAP = (1 << 11),    // 2048     Bump mapped texture, this has 8 bit U and V components for the bump normal
	    DTX_LUMBUMPMAP = (1 << 12), // 4096     Bump mapped texture with luminance, this has 8 bits for luminance, U and V
    }
    public struct DTXHeader
    {
        public UInt32 ResourceType;
        public Int32 Version;        // CURRENT_DTX_VERSION
        public UInt16 BaseWidth;
        public UInt16 BaseHeight;
        public UInt16 MipMapCount;
        public UInt16 SectionCount;
        public Int32 Flags;     // Combination of DTX_ flags.
        public Int32 UserFlags;  // Flags that go on surfaces.
        public byte TextureGroup;
        public byte MipMapsToUse;
        public byte BPPFormat;
        public byte MipMapOffset;
        public byte MipMapTexCoordinateOffset;
        public byte TexturePriority;
        public float DetailTextureScale;
        public Int16 DetailTextureAngle;

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
    };

    public class UnitySPR
    {
        public string RelativePathToSprite { get; set; }
        public string[] DTXPaths { get; set; }
    }

    public class UnityDTX
    {
        public DTXHeader Header { get; set; }
        public string RelativePathToDTX { get; set; }
        public Material Material { get; set; }
        public Texture2D OriginalTexture2D { get; set; }
        public Texture2D  Texture2D { get; set; }
        public texSize TexSize { get; set; }
    }

    public class DTXMaterial
    {
        public Dictionary<string, string> fileNameAndPath { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, Material> materials { get; set; } = new Dictionary<string, Material>();
        public Dictionary<string, Texture2D> textures { get; set; } = new Dictionary<string, Texture2D>();
        public Dictionary<string, texSize> texSize { get; set; } = new Dictionary<string, texSize>();
    }

    public struct texSize
    {
        public int width;
        public int height;
        public int engineWidth;
        public int engineHeight;
    }

    public struct DTXColor
    {
        public byte a;
        public byte r;
        public byte g;
        public byte b;
    }
    
    public enum DTXReturn
    {
        ALREADYEXISTS,
        SUCCESS,
        FAILED
    };

    public static UnityDTX LoadDTX(string projectPath, string relativePathToDTX)
    {
        string fileNameAndPath = Path.Combine(projectPath, relativePathToDTX);
        if (!File.Exists(fileNameAndPath))
        {
            return null;
        }

        var b = new BinaryReader(File.Open(fileNameAndPath, FileMode.Open));

        DTX.DTXHeader header = ReadDTXHeader(b);

        b.BaseStream.Position = 164; // Jump to texture data, ignore command strings (always 128 bytes)
        byte[] texArray = b.ReadBytes((int)b.BaseStream.Length - 164);
        b.Close();

        Texture2D texture2D = ReadTextureData(header, texArray);
        if (texture2D == null)
        {
            return null;
        }
        
        texture2D.wrapMode = TextureWrapMode.Repeat;
        Texture2D convertedTexture2D = TextureConversion.ConvertTextureToArgb32(texture2D);
        convertedTexture2D.wrapMode = TextureWrapMode.Repeat;

        FlipTexture(convertedTexture2D);

        texSize texInfo = new texSize
        {
            width = header.BaseWidth,
            height = header.BaseHeight,
            engineWidth = header.BaseWidth,
            engineHeight = header.BaseHeight
        };

        if (header.Version == DTX_VERSION_LT2)
        {
            ApplyMipMapOffset(header, ref texInfo);
        }

        //do we need to apply fullbright?
        Material mat = new Material(Shader.Find("Shader Graphs/Lithtech Vertex"));
        if (header.UseFullBright)
        {
            mat.SetInt("_FullBright", 1);
        }

        var unityDTX = new UnityDTX
        {
            Header = header,
            RelativePathToDTX = relativePathToDTX,
            OriginalTexture2D = texture2D,
            Texture2D = convertedTexture2D,
            Material = mat,
            TexSize = texInfo
        };

        return unityDTX;
    }

    private static string GetPath(string relativePath, string projectPath)
    {
        string path = Path.Combine(projectPath, relativePath);
        if (File.Exists(path))
        {
            if (path.Contains(".spr") || path.Contains(".SPR"))
            {
                var unitySPR = GetSprite(path, projectPath);
                if (unitySPR.DTXPaths.Length == 0)
                {
                    return null;
                }

                // use the first path found in the sprite file.
                return Path.Combine(projectPath, unitySPR.DTXPaths[0]);
            }

            return path;
        }

        string defaultTexturePath = GetDefaultTexturePath(projectPath);

        //Bail out if we cant find the files
        if (String.IsNullOrEmpty(defaultTexturePath))
        {
            return null;
        }

        return defaultTexturePath;
    }

    public static DTXReturn LoadDTX(string relativePath, DTXMaterial dtxMaterial, string projectPath = "")
    {
        if (dtxMaterial.textures.ContainsKey(relativePath))
        {
            return DTXReturn.ALREADYEXISTS;
        }

        // Fix the path to be a valid path - OR return with Failed.
        string path = GetPath(relativePath, projectPath);
        if (File.Exists(path))
        {
            if (path.Contains(".spr") || path.Contains(".SPR"))
            {
                var unitySPR = GetSprite(path, projectPath);
                if (unitySPR.DTXPaths.Length == 0)
                {
                    return DTXReturn.FAILED;
                }

                // use the first path found in the sprite file.
                path = Path.Combine(projectPath, unitySPR.DTXPaths[0]);
            }
        }
        else
        {
            string defaultTexturePath = GetDefaultTexturePath(projectPath);

            //Bail out if we cant find the files
            if (String.IsNullOrEmpty(defaultTexturePath))
            {
                return DTXReturn.FAILED;
            }

            path = defaultTexturePath;
        }

        var unityDTX = LoadDTX(projectPath, path);
        if (unityDTX == null)
        {
            return DTXReturn.FAILED;
        }

        Material mat = new Material(Shader.Find("Shader Graphs/Lithtech Vertex"));
        if (unityDTX.Header.UseFullBright)
        {
            mat.SetInt("_FullBright", 1);
        }

        // Index off of relativePath. This will use the path to the DTX or the SPR.
        // Prevents issues with using filename which might be duplicated in subfolders.
        AddTextureToMaterialDictionary(relativePath, unityDTX.Texture2D, dtxMaterial);
        AddMaterialToMaterialDictionary(relativePath, mat, dtxMaterial);
        AddTexSizeToDictionary(relativePath, unityDTX.TexSize, dtxMaterial);

        return DTXReturn.SUCCESS;
    }

    private static UnitySPR GetSprite(string path, string projectPath = "")
    {
        BinaryReader spriteReader = new BinaryReader(File.Open(path, FileMode.Open));
        
        var numDTX = spriteReader.ReadInt32();

        spriteReader.BaseStream.Position = 20; //jump past the header and read the first texture
        var unitySPR = new UnitySPR
        { 
            RelativePathToSprite = Path.GetRelativePath(projectPath, path),
            DTXPaths = new string[numDTX]
        };

        for (int i = 0; i < numDTX; i++)
        {
            int strLength = spriteReader.ReadUInt16();
            byte[] strData = spriteReader.ReadBytes(strLength);
            string dtxPath = System.Text.Encoding.UTF8.GetString(strData);
            unitySPR.DTXPaths[i] = dtxPath;
        }
        
        spriteReader.Close();

        return unitySPR;
    }

    private static string GetDefaultTexturePath(string projectPath)
    {
        //Check if WorldTextures\invisible.dtx exists, if not then check Tex\invisible.dtx
        //This should cover most lithtech games
        string newPath = projectPath + "\\WorldTextures\\invisible.dtx";
        if (File.Exists(newPath))
            return newPath;

        newPath = projectPath + "\\Tex\\invisible.dtx";
        if (File.Exists(newPath))
            return newPath;

        // Support for LoMM
        newPath = projectPath + "\\Textures\\LevelTextures\\Misc\\invisible.dtx";
        if (File.Exists(newPath))
            return newPath;

        return String.Empty;
    }

    private static DTX.DTXHeader ReadDTXHeader(BinaryReader reader)
    {
        DTX.DTXHeader header;
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

    private static TextureFormat GetTextureFormat(byte bytesPerPixel)
    {
        if (bytesPerPixel == (byte)DTXBPP.BPP_S3TC_DXT5) return TextureFormat.DXT5;
        if (bytesPerPixel == (byte)DTXBPP.BPP_S3TC_DXT3) return TextureFormat.DXT5Crunched; // we use crunched as dxt3
        if (bytesPerPixel == (byte)DTXBPP.BPP_S3TC_DXT1) return TextureFormat.DXT1;
        if (bytesPerPixel == (byte)DTXBPP.BPP_32) return TextureFormat.BGRA32;
        return TextureFormat.BGRA32; // Default to BGRA32
    }

    private static Texture2D ReadTextureData(DTX.DTXHeader header, byte[] texArray)
    {
        //Version check must come before everything else!!
		if (header.Version == DTX_VERSION_LT1
            || header.Version == DTX_VERSION_LT15
            || (header.Version != DTX_VERSION_LT2 && header.BPPFormat == (byte)DTXBPP.BPP_8P))
        {
            return Create8BitPaletteTexture(header, texArray);
        }
        else if (
            header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT1
            || header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT3
            || header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT5)
        {
            return CreateCompressedTexture(header, texArray);
        }
        else if (header.BPPFormat == (byte)DTXBPP.BPP_32)
        {
            return Create32bitTexture(header, texArray);
        }
        else if (header.BPPFormat == (byte)DTXBPP.BPP_32P)
        {
            return Create32BitPaletteTexture(header, texArray);
        }

        return Create32bitTexture(header, texArray);
    }

    private static void FlipTexture(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;

        Color[] pixels = texture.GetPixels();
        Color[] flipped = new Color[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            int srcRow = y * width;
            int dstRow = (height - 1 - y) * width;
            for (int x = 0; x < width; x++)
            {
                flipped[dstRow + x] = pixels[srcRow + x];
            }
        }

        texture.SetPixels(flipped);
        texture.Apply();
    }

    private static Texture2D Create32bitTexture(DTX.DTXHeader header, byte[] texArray)
    {
        // BGRA32 ?
        if (!header.Prefer4444)
        {
            // Force alpha to 1.0 for everything:
            for (int i = 0; i < texArray.Length; i += 4)
            {
                texArray[i + 3] = 255; // Set alpha (A) to 255
            }
        }

        var texture2D = new Texture2D(header.BaseWidth, header.BaseHeight, TextureFormat.BGRA32, false);
        try
        {
            texture2D.LoadRawTextureData(texArray);
        }
        catch(Exception ex)
        {
            Debug.LogError("Ex: " + ex.Message);
        }

        texture2D.Apply();

        return texture2D;
    }

    private static Texture2D CreateCompressedTexture(DTX.DTXHeader header, byte[] texArray)
    {
        // DXT1 - Default
        TextureFormat textureFormat = GetTextureFormat(header.BPPFormat);
        TextureFormat format = TextureFormat.DXT1;

        int scale = 8; // Extra bytes needed in the decoding process

        if (header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT3)
        {
            // Not supported - no TextureFormat.DXT3 support.
            // If possible, code would:
            //      format = TextureFormat.DXT3;
            //      scale = 16;
            return null;
        }
        else if (header.BPPFormat == (byte)DTXBPP.BPP_S3TC_DXT5)
        {
            format = TextureFormat.DXT5;
            scale = 16;
        }

        var compressed_width = (int)((header.BaseWidth + 3) / 4);
        var compressed_height = (int)((header.BaseHeight + 3) / 4);

        var bytesToKeep = compressed_width * compressed_height * scale;
        var compressedData = texArray.Take(bytesToKeep).ToArray();

        var texture2D = new Texture2D(header.BaseWidth, header.BaseHeight, format, false);
        try
        {
            texture2D.LoadRawTextureData(compressedData);
        }
        catch (Exception ex)
        {
            Debug.LogError("Ex: " + ex.Message);
        }

        texture2D.Apply();

        return texture2D;
    }

    private static Texture2D Create8BitPaletteTexture(DTX.DTXHeader header, byte[] texArray)
    {
        var palette = new List<Color32>();

        var expectedSize = 8 + (256 * 4) + (header.BaseWidth * header.BaseHeight);
        if (texArray.Length < expectedSize)
        {
            Debug.LogError("Invalid DTX? Not enough data to support an 8 bit palette texture!");
            return null;
        }

        // Two unknown ints
        // Used for the internal get palette function in LT1.
        var paletteHeader1 = BitConverter.ToInt32(texArray, 0);
        var paletteHeader2 = BitConverter.ToInt32(texArray, 4);

        // Read the colors from the palette.
        for (int i = 0; i < 256;i++)
        {
            var r = texArray[8 + (i * 4) + 0];
            var g = texArray[8 + (i * 4) + 1];
            var b = texArray[8 + (i * 4) + 2];
            var a = texArray[8 + (i * 4) + 3];

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
        var data = texArray.Skip(bytesToSkip).Take(header.BaseWidth * header.BaseHeight).ToArray();

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

        var texture2D = new Texture2D(header.BaseWidth, header.BaseHeight, TextureFormat.RGBA32, false);
        try
        {
            texture2D.LoadRawTextureData(colorData.ToArray());
        }
        catch (Exception ex)
        {
            Debug.LogError("Ex: " + ex.Message);
        }

        texture2D.Apply();

        return texture2D;
    }

    private static Texture2D Create32BitPaletteTexture(DTX.DTXHeader header, byte[] texArray)
    {
        if (header.SectionCount != 1)
        {
            // Should be 1 section for a 32bit palette texture.
            throw new Exception("Expected SectionCount of 1 for a 32bit palette texture");
        }

        var palette = new List<Color32>();

        int dataSize = header.BaseWidth * header.BaseHeight;
        var data = texArray.Take(dataSize).ToArray();
        var colorData = new List<byte>();

        int skipCount = 0;
        int width = header.BaseWidth;
        int height = header.BaseHeight;
        // Should this be "i < MipMapCount - 1" or "i < MipMapCount" ?
        for (int i = 0; i < header.MipMapCount - 1; i++)
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
            var a = texArray[dataSize + skipCount + (i * 4) + 0];
            var r = texArray[dataSize + skipCount + (i * 4) + 1];
            var g = texArray[dataSize + skipCount + (i * 4) + 2];
            var b = texArray[dataSize + skipCount + (i * 4) + 3];

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

        var texture2D = new Texture2D(header.BaseWidth, header.BaseHeight, TextureFormat.RGBA32, false);
        try
        {
            texture2D.LoadRawTextureData(colorData.ToArray());
        }
        catch (Exception ex)
        {
            Debug.LogError("Ex: " + ex.Message);
        }

        texture2D.Apply();

        return texture2D;
    }

    private static void ApplyMipMapOffset(DTX.DTXHeader header, ref texSize texInfo)
    {
        if (header.MipMapOffset == 1)
        {
            texInfo.engineWidth /= 2;
            texInfo.engineHeight /= 2;
        }
        if (header.MipMapOffset == 2)
        {
            texInfo.engineWidth /= 4;
            texInfo.engineHeight /= 4;
        }
        if (header.MipMapOffset == 3)
        {
            texInfo.engineWidth /= 8;
            texInfo.engineHeight /= 8;
        }
    }

    private static void AddTextureToMaterialDictionary(string filename, Texture2D texture2D, DTXMaterial dtxMaterial)
    {
        if (!dtxMaterial.textures.ContainsKey(filename))
        {
            dtxMaterial.textures.Add(filename, texture2D);
        }
    }

    public static void AddMaterialToMaterialDictionary(string filename, Material mat, DTXMaterial dtxMaterial)
    {
        if (!dtxMaterial.materials.ContainsKey(filename))
        {
            mat.name = filename;

            String[] splitName;
            if (mat.name.Contains("_Chromakey"))
            {
                splitName = mat.name.Split("_Chromakey");
                try
                {
                    mat.mainTexture = dtxMaterial.textures[splitName[0]];
                }
                catch (Exception)
                {

                    return;
                }
               
                mat.SetFloat("_Metallic", 0.9f);
                mat.SetFloat("_Smoothness", 0.8f);
                mat.SetColor("_Color", Color.white);
                dtxMaterial.materials.Add(filename, mat);
                return;
            }
            
            mat.mainTexture = dtxMaterial.textures[filename];
            mat.SetFloat("_Metallic", 0.9f);
            mat.SetFloat("_Smoothness", 0.8f);

            dtxMaterial.materials.Add(filename, mat);
        }
    }
    public static Material GetMaterialFromMaterialDictionary(string filename, DTXMaterial dtxMaterial)
    {
        if (dtxMaterial.materials.ContainsKey(filename))
        {
            return dtxMaterial.materials[filename];
        }
        return null;
    }

    private static void AddTexSizeToDictionary(string filename, texSize texInfo, DTXMaterial dtxMaterial)
    {
        if (!dtxMaterial.texSize.ContainsKey(filename))
        {
            dtxMaterial.texSize.Add(filename, texInfo);
        }
    }
}