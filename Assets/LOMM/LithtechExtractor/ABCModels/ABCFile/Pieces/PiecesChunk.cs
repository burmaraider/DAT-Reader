using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class PiecesChunk
{
    public List<PieceModel> Pieces { get; private set; } = new List<PieceModel>();

    public int GetTotalTextureCount()
    {
        int max = 0;
        foreach (PieceModel piece in Pieces)
        {
            if (piece.MaterialIndex > max) max = piece.MaterialIndex;
        }

        return max + 1;
    }

    public int GetTotalFaceCount(int lodIndex)
    {
        int count = Pieces
            .Sum(p => p.LODs[lodIndex].Faces.Count);

        return count;
    }

    public int GetFaceCount(int lodIndex, int materialIndex)
    {
        int count = Pieces
            .Where(p => p.MaterialIndex == materialIndex)
            .Sum(p => p.LODs[lodIndex].Faces.Count);

        return count;
    }

    public int GetVertexCount(int lodIndex, int materialIndex)
    {
        int faceCount = GetFaceCount(lodIndex, materialIndex);
        return faceCount * 3;
    }

    public int GetTotalVertexCount(int lodIndex)
    {
        int faceCount = GetTotalFaceCount(lodIndex);
        return faceCount * 3;
    }

    public int[] GetIndices(int lodIndex, int materialIndex, int offset, bool flip)
    {
        int faceCount = GetFaceCount(lodIndex, materialIndex);
        int[] indices = new int[faceCount * 3];

        for (int index = 0; index < faceCount; index++)
        {
            if (flip)
            {
                indices[index * 3 + 0] = index * 3 + 0 + offset;
                indices[index * 3 + 1] = index * 3 + 2 + offset;
                indices[index * 3 + 2] = index * 3 + 1 + offset;
            }
            else
            {
                indices[index * 3 + 0] = index * 3 + 0 + offset;
                indices[index * 3 + 1] = index * 3 + 1 + offset;
                indices[index * 3 + 2] = index * 3 + 2 + offset;
            }
        }

        return indices;
    }

    public Vector3[] GetVertices(int lodIndex, int materialIndex)
    {
        Vector3[] vertices = new Vector3[GetFaceCount(lodIndex, materialIndex) * 3];

        int vertexIndex = 0;
        foreach (PieceModel piece in Pieces)
        {
            if (piece.MaterialIndex == materialIndex)
            {
                foreach (var face in piece.LODs[0].Faces)
                {
                    foreach (var faceVertex in face.FaceVertices)
                    {
                        var vertex = piece.LODs[lodIndex].Vertices[faceVertex.VertexIndex];

                        vertices[vertexIndex] = new Vector3(vertex.Location.x, vertex.Location.y, vertex.Location.z);
                        vertexIndex++;
                    }
                }
            }
        }

        return vertices;
    }

    public Vector3[] GetNormals(int lodIndex, int materialIndex)
    {
        Vector3[] normals = new Vector3[GetFaceCount(lodIndex, materialIndex) * 3];

        int normalIndex = 0;
        foreach (PieceModel piece in Pieces)
        {
            if (piece.MaterialIndex == materialIndex)
            {
                foreach (var face in piece.LODs[0].Faces)
                {
                    foreach (var faceVertex in face.FaceVertices)
                    {
                        var vertex = piece.LODs[lodIndex].Vertices[faceVertex.VertexIndex];

                        normals[normalIndex] = new Vector3(vertex.Normal.x, vertex.Normal.y, vertex.Normal.z);
                        normalIndex++;
                    }
                }
            }
        }

        return normals;
    }

    public Vector2[] GetTextureCoordinates(int lodIndex, int materialIndex, bool flipV)
    {
        Vector2[] textureCoordinates = new Vector2[GetFaceCount(lodIndex, materialIndex) * 3];

        int textureCoordinateIndex = 0;
        foreach (PieceModel piece in Pieces)
        {
            if (piece.MaterialIndex == materialIndex)
            {
                foreach (var face in piece.LODs[0].Faces)
                {
                    foreach (var faceVertex in face.FaceVertices)
                    {
                        if (flipV)
                        {
                            textureCoordinates[textureCoordinateIndex] = new Vector2(faceVertex.Texcoord.x, 1f - faceVertex.Texcoord.y);
                        }
                        else
                        {
                            textureCoordinates[textureCoordinateIndex] = new Vector2(faceVertex.Texcoord.x, faceVertex.Texcoord.y);
                        }

                        textureCoordinateIndex++;
                    }
                }
            }
        }

        return textureCoordinates;
    }
}