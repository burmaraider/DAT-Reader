using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DTX
{
    public static int identElement = 2;
    public struct DTXHeader
    {
        public UInt32 m_ResType;
        public Int32 m_Version;        // CURRENT_DTX_VERSION
        public UInt16 m_BaseWidth, m_BaseHeight;
        public UInt16 m_nMipmaps;
        public UInt16 m_nSections;
        public Int32 m_IFlags;     // Combination of DTX_ flags.
        public Int32 m_UserFlags;  // Flags that go on surfaces.

        // Extra data.  Here's how it's layed out:
        // m_Extra[0] = Texture group.
        // m_Extra[1] = Number of mipmaps to use (there are always 4 in the file, 
        //              but this says how many to use at runtime).
        // m_Extra[2] = BPPIdent telling what format the texture is in.
        // m_Extra[3] = Mipmap offset if the card doesn't support S3TC compression.
        // m_Extra[4] = Mipmap offset applied to texture coords (so a 512 could be 
        //				treated like a 256 or 128 texture in the editor).
        // m_Extra[5] = Texture priority (default 0).
        // m_Extra[6-9] = Detail texture scale (float value).
        // m_Extra[10-11] = Detail texture angle (integer degrees)
		public byte[] m_Extra;
    };

    public class DTXMaterial
    {
        public Dictionary<string, string> fileNameAndPath = new Dictionary<string, string>();
        public Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        
        public Dictionary<string, texSize> texSize = new Dictionary<string, texSize>();
    }
    public struct texSize
    {
        public int width;
        public int height;
        public int engineWidth;
        public int engineHeight;
    }
    
    public enum DTXReturn
    {
        ALREADYEXISTS,
        SUCCESS,
        FAILED
    };

    public static DTXReturn LoadDTX(string path, ref DTXMaterial dtxMaterial, string projectPath = "")
    {
        if (dtxMaterial.textures.ContainsKey(Path.GetFileName(path)))
            return DTXReturn.ALREADYEXISTS;

        Material mat = new Material(Shader.Find("Shader Graphs/Lithtech Vertex"));
        BinaryReader b;

        if (File.Exists(path))
        {
            string newPath = path;

            if (path.Contains(".spr") || path.Contains(".SPR"))
            {
                newPath = GetSpriteFilePath(path, projectPath);
            }

            b = new BinaryReader(File.Open(newPath, FileMode.Open));
        }
        else
        {
            string newPathCantFind = GetDefaultTexturePath(projectPath);

            //Bail out if we cant find the files
            if (String.IsNullOrEmpty(newPathCantFind))
                return DTXReturn.FAILED;

            b = new BinaryReader(File.Open(newPathCantFind, FileMode.Open));
        }

        DTX.DTXHeader header = ReadDTXHeader(b);

        b.BaseStream.Position = 164; // Jump to texture data, ignore command strings (always 128 bytes)
        byte[] texArray = b.ReadBytes((int)b.BaseStream.Length - 164);
        b.Close();

        TextureFormat textureFormat = GetTextureFormat(header.m_Extra[identElement]);
        Texture2D texture2D = CreateTexture(header, texArray, textureFormat);

        texSize texInfo = new texSize
        {
            width = header.m_BaseWidth,
            height = header.m_BaseHeight,
            engineWidth = header.m_BaseWidth,
            engineHeight = header.m_BaseHeight
        };

        ApplyMipMapOffset(header, ref texInfo);

        string filename = Path.GetFileName(path);
        AddTextureToMaterialDictionary(filename, texture2D, mat, dtxMaterial);
        AddMaterialToMaterialDictionary(filename, mat, dtxMaterial);
        AddTexSizeToDictionary(filename, texInfo, dtxMaterial);

        return DTXReturn.SUCCESS;
    }

    private static string GetSpriteFilePath(string path, string projectPath = "")
    {
        BinaryReader spriteReader = new BinaryReader(File.Open(path, FileMode.Open));
        spriteReader.BaseStream.Position = 20;

        int strLength = spriteReader.ReadUInt16();
        byte[] stringBytes = spriteReader.ReadBytes(strLength);
        string fileName = System.Text.Encoding.Default.GetString(stringBytes);

        return projectPath + "\\" + fileName;
    }

    private static string GetDefaultTexturePath(string projectPath = "")
    {

        //Check if WorldTextures\invisible.dtx exists, if not then check Tex\invisible.dtx
        //This should cover most lithtech games
        string newPath = projectPath + "\\WorldTextures\\invisible.dtx";
        if (File.Exists(newPath))
            return newPath;

        newPath = projectPath + "\\Tex\\invisible.dtx";
        if (File.Exists(newPath))
            return newPath;

        return String.Empty;
    }

    private static DTX.DTXHeader ReadDTXHeader(BinaryReader reader)
    {
        DTX.DTXHeader header;
        header.m_ResType = reader.ReadUInt32();
        header.m_Version = reader.ReadInt32();
        header.m_BaseWidth = reader.ReadUInt16();
        header.m_BaseHeight = reader.ReadUInt16();
        header.m_nMipmaps = reader.ReadUInt16();
        header.m_nSections = reader.ReadUInt16();
        header.m_IFlags = reader.ReadInt32();
        header.m_UserFlags = reader.ReadInt32();
        header.m_Extra = reader.ReadBytes(12);
        return header;
    }

    private static TextureFormat GetTextureFormat(byte identElement)
    {
        if (identElement == 6) return TextureFormat.DXT5;
        if (identElement == 5) return TextureFormat.DXT5Crunched; // we use crunched as dxt3
        if (identElement == 4) return TextureFormat.DXT1;
        if (identElement == 3) return TextureFormat.BGRA32;
        return TextureFormat.DXT5; // Default to DXT5
    }

    private static Texture2D CreateTexture(DTX.DTXHeader header, byte[] texArray, TextureFormat textureFormat)
    {
        Texture2D texture2D;

        //TODO: Add support for DXT3
        if (textureFormat == TextureFormat.DXT5Crunched)
        {
            texture2D = new Texture2D(header.m_BaseWidth, header.m_BaseHeight, TextureFormat.DXT5, false);
            texture2D.LoadRawTextureData(texArray);

            texture2D.Apply();
        }
        else
        {
            texture2D = new Texture2D(header.m_BaseWidth, header.m_BaseHeight, textureFormat, false);
            texture2D.LoadRawTextureData(texArray);
            texture2D.Apply();
        }

        return texture2D;
    }

    private static void ApplyMipMapOffset(DTX.DTXHeader header, ref texSize texInfo)
    {
        if (header.m_Extra[4] == 1)
        {
            texInfo.engineWidth /= 2;
            texInfo.engineHeight /= 2;
        }
        if (header.m_Extra[4] == 2)
        {
            texInfo.engineWidth /= 4;
            texInfo.engineHeight /= 4;
        }
        if (header.m_Extra[4] == 3)
        {
            texInfo.engineWidth /= 8;
            texInfo.engineHeight /= 8;
        }
    }

    private static void AddTextureToMaterialDictionary(string filename, Texture2D texture2D, Material mat, DTXMaterial dtxMaterial)
    {
        if (!dtxMaterial.textures.ContainsKey(filename))
            dtxMaterial.textures.Add(filename, texture2D);
    }

    private static void AddMaterialToMaterialDictionary(string filename, Material mat, DTXMaterial dtxMaterial)
    {
        if (!dtxMaterial.materials.ContainsKey(filename))
        {
            mat.name = filename;
            //mat.SetTexture("_MainTex", dtxMaterial.textures[filename]);
            mat.mainTexture = dtxMaterial.textures[filename];
            mat.SetFloat("_Metallic", 0.9f);
            mat.SetFloat("_Smoothness", 0.8f);

            dtxMaterial.materials.Add(filename, mat);
        }
    }

    private static void AddTexSizeToDictionary(string filename, texSize texInfo, DTXMaterial dtxMaterial)
    {
        if (!dtxMaterial.texSize.ContainsKey(filename))
        {
            dtxMaterial.texSize.Add(filename, texInfo);
        }
    }
}