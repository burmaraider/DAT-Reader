using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static LTTypes;
using static LTUtils;

public static class BSPModelReader
{
    private static void ReadTextures(BinaryReader binaryReader, BSPModel model)
    {
        for (int i = 0; i < model.TextureCount; i++)
        {
            var bytes = new List<byte>();

            byte next;
            while ((next = binaryReader.ReadByte()) != 0)
            {
                bytes.Add(next);
            }

            string textureName = System.Text.Encoding.ASCII.GetString(bytes.ToArray());
            model.TextureNames.Add(textureName);
        }
    }

    private static List<WorldPolyModel> ReadPolies1(BinaryReader binaryReader, bool isTooBig, int polyCount)
    {
        List<WorldPolyModel> worldPolyList = new List<WorldPolyModel>();
        for (int i = 0; i < polyCount; i++)
        {
            WorldPolyModel poly = new WorldPolyModel();

            if (isTooBig)
            {
                Int32 nVertices = binaryReader.ReadInt16();
                byte hi = (byte)(nVertices >> 8);
                byte lo = (byte)(nVertices & 0xff);

                poly.IndexAndNumVerts = i;
                poly.LoVerts = lo;
                poly.HiVerts = hi;

                nVertices = (Int32)(poly.LoVerts + poly.HiVerts);

                poly.IndexAndNumVerts = unchecked((poly.IndexAndNumVerts & 0xFFFFFF00) | (nVertices & 0xFF));

                worldPolyList.Add(poly);
            }
            else
            {
                Int16 nVertices = binaryReader.ReadInt16();
                byte hi = (byte)(nVertices >> 8);
                byte lo = (byte)(nVertices & 0xff);

                poly.IndexAndNumVerts = i;
                poly.LoVerts = lo;
                poly.HiVerts = hi;

                nVertices = (short)(poly.LoVerts + poly.HiVerts);

                poly.IndexAndNumVerts = unchecked((poly.IndexAndNumVerts & 0xFFFFFF00) | (nVertices & 0xFF));

                worldPolyList.Add(poly);
            }
        }

        return worldPolyList;
    }

    private static List<LeafListModel> ReadLeafs(BinaryReader binaryReader, int leafCount, int version, Game gameType)
    {
        var models = new List<LeafListModel>();
        for (int i = 0; i < leafCount; i++)
        {
            LeafListModel leafList = new LeafListModel();

            leafList.LeafListCount = binaryReader.ReadInt16();

            if (leafList.LeafListCount == -1)
            {
                leafList.LeafListIndex = binaryReader.ReadInt16();
            }
            else if (leafList.LeafListCount > 0)
            {
                for (Int16 leafListIndex = 0; leafListIndex < leafList.LeafListCount; leafListIndex++)
                {
                    LeafModel leaf = new LeafModel();

                    leaf.PortalId = binaryReader.ReadInt16();
                    leaf.Size = binaryReader.ReadInt16();
                    leaf.Contents = binaryReader.ReadBytes(leaf.Size);

                    leafList.Leafs.Add(leaf);
                }
            }

            if (version == DATVersions.Version56)
            {
                leafList.PolyCount = binaryReader.ReadUInt16();
            }
            else
            {
                leafList.PolyCount = binaryReader.ReadInt32();
            }

            if (gameType == Game.LOMM)
            {
                Int16 unknown = binaryReader.ReadInt16();
                if (leafList.PolyCount > 0)
                {
                    leafList.Polies = new short[leafList.PolyCount * 2];

                    for (int y = 0; y < leafList.Polies.Length; y++)
                    {
                        leafList.Polies[y] = binaryReader.ReadInt16();
                    }
                }

                leafList.Cardinal = binaryReader.ReadInt16();
            }
            else
            {
                if (leafList.PolyCount > 0)
                {
                    leafList.Polies = new short[leafList.PolyCount * 4];

                    for (int y = 0; y < leafList.Polies.Length; y++)
                    {
                        if (version == DATVersions.Version56)
                        {
                            leafList.Polies[y] = binaryReader.ReadByte();
                        }
                        else
                        {
                            leafList.Polies[y] = binaryReader.ReadInt16();
                        }
                    }
                }

                leafList.Cardinal = binaryReader.ReadInt32();
            }

            models.Add(leafList);
        }

        return models;
    }

    private static List<WorldPlaneModel> ReadPlanes(BinaryReader binaryReader, int planeCount)
    {
        List<WorldPlaneModel> planes = new List<WorldPlaneModel>();
        if (planeCount > 0)
        {
            for (int i = 0; i < planeCount; i++)
            {
                WorldPlaneModel pPlane = new WorldPlaneModel();
                pPlane.Normal = ReadLTVector(ref binaryReader);
                pPlane.Distance = binaryReader.ReadSingle();
                planes.Add(pPlane);
            }
        }

        return planes;
    }

    private static List<WorldSurfaceModel> ReadSurfaces(BinaryReader binaryReader, int version, int surfaceCount)
    {
        if (version == DATVersions.Version66)
        {
            return ReadSurfaces66(binaryReader, surfaceCount);
        }

        return null;
    }

    private static List<WorldSurfaceModel> ReadSurfaces66(BinaryReader binaryReader, int surfaceCount)
    {
        var surfaces = new List<WorldSurfaceModel>();
        if (surfaceCount > 0)
        {
            for (int i = 0; i < surfaceCount; i++)
            {
                WorldSurfaceModel pSurface = new WorldSurfaceModel();
                pSurface.UV1 = ReadLTVector(ref binaryReader);
                pSurface.UV2 = ReadLTVector(ref binaryReader);
                pSurface.UV3 = ReadLTVector(ref binaryReader);
                pSurface.TextureIndex = binaryReader.ReadInt16();
                binaryReader.BaseStream.Position += 4;//extra stuff in .dat 66
                pSurface.Flags = binaryReader.ReadInt32();
                pSurface.Unknown1 = binaryReader.ReadByte();
                pSurface.Unknown2 = binaryReader.ReadByte();
                pSurface.Unknown3 = binaryReader.ReadByte();
                pSurface.Unknown4 = binaryReader.ReadByte();
                pSurface.UseEffect = binaryReader.ReadByte();

                if (pSurface.UseEffect > 0)
                {
                    Int16 nLen = binaryReader.ReadInt16();
                    if (nLen > 0)
                    {
                        pSurface.EffectName = ReadString(nLen, ref binaryReader);
                    }
                    nLen = binaryReader.ReadInt16();
                    if (nLen > 0)
                    {
                        pSurface.EffectParams = ReadString(nLen, ref binaryReader);
                    }
                }

                pSurface.TextureFlags = binaryReader.ReadInt16();

                surfaces.Add(pSurface);
            }
        }

        return surfaces;
    }

    private static void ReadAndUpdatePoly(BinaryReader binaryReader, WorldPolyModel poly)
    {
        float x, y, z;

        x = binaryReader.ReadSingle();
        y = binaryReader.ReadSingle();
        z = binaryReader.ReadSingle();

        poly.Center = new LTVector((LTFloat)x, (LTFloat)y, (LTFloat)z);
        poly.LightmapWidth = binaryReader.ReadInt16();
        poly.LightmapHeight = binaryReader.ReadInt16();

        poly.UnknownNum = binaryReader.ReadInt16();

        if (poly.UnknownNum > 0)
        {
            poly.UnknownList = new short[poly.UnknownNum * 2];

            for (int t = 0; t < poly.UnknownList.Length; t++)
            {
                try
                {
                    poly.UnknownList[t] = binaryReader.ReadInt16();
                }
                catch (Exception e)
                {
                    Debug.Log("Failed at Pos: " + binaryReader.BaseStream.Position);
                    Debug.Log(e.Message);
                }
            }
        }

        poly.SurfaceIndex = binaryReader.ReadInt16();
        poly.PlaneIndex = binaryReader.ReadInt16();

        int verts = (int)poly.IndexAndNumVerts & 0xFF;
        poly.VertexColorList = new List<VertexColorModel>();
        for (int t = 0; t < verts; t++)
        {
            VertexColorModel vertexColors = new VertexColorModel();
            vertexColors.VertexCount = binaryReader.ReadInt16();
            vertexColors.R = binaryReader.ReadByte();
            vertexColors.G = binaryReader.ReadByte();
            vertexColors.B = binaryReader.ReadByte();
            poly.VertexColorList.Add(vertexColors);
        }

        poly.O = ReadLTVector(ref binaryReader);
        poly.P = ReadLTVector(ref binaryReader);
        poly.Q = ReadLTVector(ref binaryReader);

        binaryReader.BaseStream.Position -= 36;
    }

    private static void ReadPolies2(BinaryReader binaryReader, int version, List<WorldPolyModel> polyModels)
    {
        if (version != DATVersions.Version66)
        {
            return;
        }
     
        for (int i = 0; i < polyModels.Count; i++)
        {
            ReadAndUpdatePoly(binaryReader, polyModels[i]);
        }
    }

    private static List<LTVector> ReadVertices(BinaryReader binaryReader, int version, int nodeCount, int pointCount)
    {
        // NOTE: This was ReadPoints. Renamed to ReadVertices.
        if (version != DATVersions.Version66)
        {
            return null;
        }

        var vertices = new List<LTVector>();
        binaryReader.BaseStream.Position += nodeCount * 14;
        if (pointCount > 0)
        {
            for (int i = 0; i < pointCount; i++)
            {
                var vertex = ReadLTVector(ref binaryReader);
                // skip the normals, unity does a good enough job
                binaryReader.BaseStream.Position += 12;
                vertices.Add(vertex);
            }
        }

        return vertices;
    }

    public static BSPModel ReadBSPModel(BinaryReader binaryReader, int version, Game gameType)
    {
        var model = new BSPModel();
        model.Version = version;
        model.GameType = gameType;
        model.WorldInfoFlags = (short)binaryReader.ReadInt32();

        var unknown = binaryReader.ReadInt32();
        var nameLen = binaryReader.ReadInt16();
        model.WorldName = ReadString(nameLen, ref binaryReader);

        if (nameLen == 0 || nameLen > 255)
        {
            throw new Exception($"Name Length was: {nameLen}\r\n Name was: {model.WorldName}");
        }

        model.PointCount = binaryReader.ReadInt32();
        model.PlaneCount = binaryReader.ReadInt32();
        model.SurfaceCount = binaryReader.ReadInt32();
        model.UserPortalCount = binaryReader.ReadInt32();
        model.PolyCount = binaryReader.ReadInt32();
        model.LeafCount = binaryReader.ReadInt32();
        model.VertCount = binaryReader.ReadInt32();
        model.TotalVisListSize = binaryReader.ReadInt32();
        model.LeafListCount = binaryReader.ReadInt32();
        model.NodeCount = binaryReader.ReadInt32();

        var unknown2 = binaryReader.ReadInt32();
        var unknown3 = binaryReader.ReadInt32();

        model.MinBox = ReadLTVector(ref binaryReader);
        model.MaxBox = ReadLTVector(ref binaryReader);
        model.WorldTranslation = ReadLTVector(ref binaryReader);
        
        model.NamesLen = binaryReader.ReadInt32();
        model.TextureCount = binaryReader.ReadInt32();

        ReadTextures(binaryReader, model);

        var isTooBig = model.VertCount > sizeof(Int16);
        model.Polies = ReadPolies1(binaryReader, isTooBig, model.PolyCount);

        //ReadPolies1(ref b);
        model.Leafs = ReadLeafs(binaryReader, model.LeafCount, model.Version, model.GameType);
        model.Planes = ReadPlanes(binaryReader, model.PlaneCount);
        model.Surfaces = ReadSurfaces(binaryReader, model.Version, model.SurfaceCount);
        // model.Points = ReadPoints... // Only version 70
        ReadPolies2(binaryReader, model.Version, model.Polies);
        model.Vertices = ReadVertices(binaryReader, model.Version, model.NodeCount, model.PointCount);
        return model;
    }
}