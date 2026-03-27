using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Xml.Linq;
using UnityEngine;

public static class ABCModelReader
{
    private static bool ShowError = false;

    private static Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private static Vector4 ReadVector4(BinaryReader reader)
    {
        float w = reader.ReadSingle();
        return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), w);
    }

    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadUInt16();
        byte[] stringBytes = reader.ReadBytes(length);
        return System.Text.Encoding.ASCII.GetString(stringBytes);
    }

    private static Matrix ReadMatrix(BinaryReader reader)
    {
        Matrix matrix = new Matrix();
        matrix.Unknown4 = ReadVector4(reader);
        matrix.Unknown1 = ReadVector4(reader);
        matrix.Unknown2 = ReadVector4(reader);
        matrix.Unknown3 = ReadVector4(reader);
        return matrix;
    }

    private static WeightModel ReadWeight(BinaryReader reader)
    {
        WeightModel weight = new WeightModel();
        weight.NodeIndex = reader.ReadInt32();
        weight.Location = ReadVector3(reader);
        weight.Bias = reader.ReadSingle();
        return weight;
    }

    private static VertexModel ReadVertex(BinaryReader reader)
    {
        VertexModel vertex = new VertexModel();
        int nWeightCount = reader.ReadUInt16();
        vertex.SublodVertexIndex = reader.ReadUInt16();
        vertex.Weights = new List<WeightModel>();
        for (int i = 0; i < nWeightCount; i++)
        {
            vertex.Weights.Add(ReadWeight(reader));
        }
        vertex.Location = ReadVector3(reader);
        vertex.Normal = ReadVector3(reader);
        return vertex;
    }

    private static FaceVertexModel ReadFaceVertex(BinaryReader reader)
    {
        FaceVertexModel faceVertex = new FaceVertexModel();
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        faceVertex.Texcoord = new Vector2(x, y); 
        faceVertex.VertexIndex = reader.ReadUInt16();
        return faceVertex;
    }

    private static FaceModel ReadFace(BinaryReader reader)
    {
        FaceModel face = new FaceModel();
        face.FaceVertices = new List<FaceVertexModel>();
        for (int i = 0; i < 3; i++)
        {
            face.FaceVertices.Add(ReadFaceVertex(reader));
        }
        return face;
    }

    private static LODModel ReadLOD(BinaryReader reader)
    {
        LODModel lod = new LODModel();
        int nFaceCount = reader.ReadInt32();
        lod.Faces = new List<FaceModel>();
        for (int i = 0; i < nFaceCount; i++)
        {
            lod.Faces.Add(ReadFace(reader));
        }
        int nVertexCount = reader.ReadInt32();
        lod.Vertices = new List<VertexModel>();
        for (int i = 0; i < nVertexCount; i++)
        {
            lod.Vertices.Add(ReadVertex(reader));
        }

        return lod;
    }

    private static PieceModel ReadPiece(int version, int lodCount, BinaryReader reader)
    {
        PieceModel piece = new PieceModel();

        if (reader.BaseStream.Position + sizeof(ushort) > reader.BaseStream.Length)
        {
            return null;
        }
        
        piece.MaterialIndex = reader.ReadUInt16();
        piece.SpecularPower = reader.ReadSingle();
        piece.SpecularScale = reader.ReadSingle();
        if (version > 9)
        {
            piece.LodWeight = reader.ReadSingle();
        }
        piece.Padding = reader.ReadUInt16();
        piece.Name = ReadString(reader);
        piece.LODs = new List<LODModel>();
        for (int i = 0; i < lodCount; i++)
        {
            piece.LODs.Add(ReadLOD(reader));
        }

        return piece;
    }

    private static Node ReadNode(BinaryReader reader)
    {
        Node node = new Node();

        node.Name = ReadString(reader);
        node.Id = reader.ReadUInt16();
        node.Flags = reader.ReadByte();
        node.Matrix = ReadMatrix(reader);
        var numChildren = reader.ReadInt32();

        for (int childIndex = 0; childIndex < numChildren; childIndex++)
        {
            var childNode = ReadNode(reader);
            node.Children.Add(childNode);
        }

        return node;
    }

    public static ABCModel ReadABCModel(string filename, string projectPath)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new Exception("No file selected");
        }

        if (!File.Exists(filename))
        {
            Debug.LogWarning($"ABCModelReader Error - File not found: {filename}");
            return null;
        }

        ABCModel model = new ABCModel();

        string fileExtension = Path.GetExtension(filename);
        model.Name = Path.GetFileNameWithoutExtension(filename);

        try
        {
            using BinaryReader reader = new BinaryReader(File.OpenRead(filename));
            int nextSectionOffset = 0;
            while (nextSectionOffset != -1)
            {
                reader.BaseStream.Seek(nextSectionOffset, SeekOrigin.Begin);

                if (fileExtension.Contains("ltb"))
                {
                    // Check if we have a pre-Jupiter LTB
                    string ltbHeader = ReadString(reader);

                    if (ltbHeader == "LTBHeader")
                    {
                        nextSectionOffset = reader.ReadInt32();
                        reader.BaseStream.Position = nextSectionOffset;
                    }
                }

                string sectionName = ReadString(reader);
                nextSectionOffset = reader.ReadInt32();
                if (sectionName == "Header")
                {
                    model.Version = reader.ReadInt32();
                    if (model.Version < 9 || model.Version > 13)
                    {
                        Debug.LogError($"Unsupported file version ({model.Version}).");
                        return null;
                    }

                    reader.BaseStream.Seek(8, SeekOrigin.Current);
                    model.NodeCount = reader.ReadInt32();
                    reader.BaseStream.Seek(20, SeekOrigin.Current);
                    model.LODCount = reader.ReadInt32();
                    reader.BaseStream.Seek(4, SeekOrigin.Current);
                    model.WeightSetCount = reader.ReadInt32();
                    reader.BaseStream.Seek(8, SeekOrigin.Current);

                    // Unknown new value
                    if (model.Version >= 13)
                    {
                        reader.BaseStream.Seek(4, SeekOrigin.Current);
                    }

                    model.CommandString = ReadString(reader);
                    model.InternalRadius = reader.ReadSingle();
                    reader.BaseStream.Seek(64, SeekOrigin.Current);
                    model.LODDistances = new List<float>();
                    for (int i = 0; i < model.LODCount; i++)
                    {
                        model.LODDistances.Add(reader.ReadSingle());
                    }
                }
                else if (sectionName == "Pieces")
                {
                    int nWeightCount = reader.ReadInt32();
                    int nPiecesCount = reader.ReadInt32();
                    model.PiecesChunk = new PiecesChunk();
                    for (int i = 0; i < nPiecesCount; i++)
                    {
                        var piece = ReadPiece(model.Version, model.LODCount, reader);
                        if (piece == null)
                        {
                            if (ShowError)
                            {
                                Debug.LogError($"Error while loading ABC file {filename}: Reached end of file too soon.");
                            }

                            return null;
                        }

                        model.PiecesChunk.Pieces.Add(piece);
                    }
                }
                else if (sectionName == "Nodes")
                {
                    model.RootNode = ReadNode(reader);
                }
            }

            reader.Close();
        }
        catch (Exception ex)
        {
            if (ShowError)
            {
                Debug.LogError($"Error while loading ABC file {filename}: {ex.Message}");
            }

            return null;
        }

        model.RelativePathToABCFileLowercase = Path.GetRelativePath(projectPath, filename).ConvertFolderSeperators().ToLower();
        return model;
    }
}
