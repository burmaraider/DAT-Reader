﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public static class DTX
{
    public static int identElement = 2;

    public static int DTX1 = -2;
    public static int DTX15 = -3;
    public static int DTX2 = -5;
	

    [Flags]
    public enum DTXFlags
    {
        FULLBRIGHT = 0x01,
        PREFER16BIT = 0x02,
        ALPHA = 0X02, //DTX1
        UNK1 = 0x04,
        UNK2 = 0x08,
        UNK3 = 0x10,
        UNK4 = 0x20,
        UNK5 = 0x40,
        PREFER4444 = 0x80,
        PREFER5551 = 0x100,

    }
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

        byte[] texArray = { 0x00 };

        if (header.m_Version == DTX1)
        {
            byte[] pixelArray = new byte[header.m_BaseWidth * header.m_BaseHeight];
            b.BaseStream.Position += 8; // Jump to color section

            // Color section always has 256 entries
            DTXColor[] aColors = new DTXColor[256];

            int structSize = Marshal.SizeOf<DTXColor>();
            byte[] colorBytes = new byte[structSize * 256];
            b.Read(colorBytes, 0, colorBytes.Length);

            GCHandle handle = GCHandle.Alloc(aColors, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(colorBytes, 0, handle.AddrOfPinnedObject(), colorBytes.Length);
            }
            finally
            {
                handle.Free();
            }

            // Read the first mipmap header.m_BaseWidth x header.m_BaseHeight into pixelArray
            b.Read(pixelArray, 0, header.m_BaseWidth * header.m_BaseHeight);

            texArray = new byte[header.m_BaseWidth * header.m_BaseHeight * 4]; //argb -> bgra

            for (int y = 0; y < header.m_BaseHeight; y++)
            {
                for (int x = 0; x < header.m_BaseWidth; x++)
                {
                    int index = y * header.m_BaseWidth + x;
                    DTXColor color = aColors[pixelArray[index]];
                    int texIndex = index * 4;
                    texArray[texIndex] = color.b;
                    texArray[texIndex + 1] = color.g;
                    texArray[texIndex + 2] = color.r;
                    texArray[texIndex + 3] = color.a;
                }
            }

            if ((header.m_IFlags & (int)DTXFlags.ALPHA) != 0 )
            {
                //skip the remaining 3 mip maps
                b.BaseStream.Position += (header.m_BaseWidth * header.m_BaseHeight) / 4;
                b.BaseStream.Position += (header.m_BaseWidth * header.m_BaseHeight) / 16;
                b.BaseStream.Position += (header.m_BaseWidth * header.m_BaseHeight) / 64;

                for (int y = 0; y < header.m_BaseHeight; y++)
                {
                    for (int x = 0; x < header.m_BaseWidth; x += 2) // increment by 2 because 4 bpp alpha
                    {
                        byte alphaByte = b.ReadByte();

                        // Unpack the two 4-bit alpha values
                        byte alpha1 = (byte)((alphaByte & 0xF0) >> 4); // alpha for the first pixel
                        byte alpha2 = (byte)(alphaByte & 0x0F); // alpha for the second pixel

                        // Scale up the 4-bit alpha values to 8 bits
                        alpha1 = (byte)(alpha1 * 0x11);
                        alpha2 = (byte)(alpha2 * 0x11);

                        // Apply the alpha values to the corresponding pixels
                        texArray[(y * header.m_BaseWidth + x) * 4 + 3] = alpha1;
                        texArray[(y * header.m_BaseWidth + x + 1) * 4 + 3] = alpha2;
                    }
                }

            }

        }
        else
        {
            b.BaseStream.Position = 164; // Jump to texture data, ignore command strings (always 128 bytes)
            texArray = b.ReadBytes((int)b.BaseStream.Length - 164);
        }

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

        if (header.m_Version == DTX2)
        {
            ApplyMipMapOffset(header, ref texInfo);
        }

        //do we need to apply fullbright?
        if ((header.m_IFlags & (int)DTXFlags.FULLBRIGHT) != 0)
        {
            mat.SetInt("_FullBright", 1);
        }

        string filename = Path.GetFileName(path);
        AddTextureToMaterialDictionary(filename, texture2D, mat, dtxMaterial);
        AddMaterialToMaterialDictionary(filename, mat, dtxMaterial);
        AddTexSizeToDictionary(filename, texInfo, dtxMaterial);

        return DTXReturn.SUCCESS;
    }

    private static string GetSpriteFilePath(string path, string projectPath = "")
    {
        BinaryReader spriteReader = new BinaryReader(File.Open(path, FileMode.Open));
        spriteReader.BaseStream.Position = 20; //jump past the header and read the first texture

        int strLength = spriteReader.ReadUInt16();
        byte[] stringBytes = spriteReader.ReadBytes(strLength);
        string fileName = System.Text.Encoding.Default.GetString(stringBytes);
        spriteReader.Close();

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
        return TextureFormat.BGRA32; // Default to BGRA32
    }

    private static Texture2D CreateTexture(DTX.DTXHeader header, byte[] texArray, TextureFormat textureFormat)
    {
        Texture2D texture2D;

        //TODO: Add full support for DXT3. 4 bit alpha
        if (textureFormat == TextureFormat.DXT5Crunched)
            textureFormat = TextureFormat.DXT5;
 
        texture2D = new Texture2D(header.m_BaseWidth, header.m_BaseHeight, textureFormat, false);

        try
        {
            texture2D.LoadRawTextureData(texArray);
            texture2D.Apply();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return null;
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