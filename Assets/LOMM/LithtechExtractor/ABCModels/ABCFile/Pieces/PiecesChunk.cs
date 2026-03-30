using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class PiecesChunk
{
    public List<PieceModel> Pieces { get; set; } = new List<PieceModel>();
    public List<PieceModel> AllPieces { get; private set; } = new List<PieceModel>();

    private int? _totalTextureCount = null;

    public int TotalTextureCount
    {
        get
        {
            if (_totalTextureCount is null)
            {
                if (Pieces is null || Pieces.Count == 0)
                {
                    _totalTextureCount = 0;
                }
                else
                {
                    _totalTextureCount = Pieces.Max(x => x.MaterialIndex) + 1;
                }
            }

            return _totalTextureCount.Value;
        }
    }

    public int GetInitialOffset(int lodIndex, int materialIndex)
    {
        if (materialIndex == 0)
        {
            return 0;
        }

        return Pieces.Select(piece => piece.MaterialIndex < materialIndex ? piece.LODs[lodIndex].Vertices.Count : 0).Sum();
    }


    public List<int> GetIndices(int lodIndex, int materialIndex, bool flip)
    {
        List<int> indices = new();

        int offset = GetInitialOffset(lodIndex, materialIndex);
        foreach (var piece in Pieces)
        {
            if (piece.MaterialIndex == materialIndex)
            {
                foreach (var face in piece.LODs[lodIndex].Faces)
                {
                    var triangleIndices = face.FaceVertices.Select(face => face.VertexIndex + offset);
                    if (flip)
                    {
                        triangleIndices = triangleIndices.Reverse();
                    }

                    indices.AddRange(triangleIndices);
                }

                offset += piece.LODs[lodIndex].Vertices.Count;
            }
        }

        return indices;
    }

    public List<Vector3> GetVertices(int lodIndex, int materialIndex)
    {
        List<Vector3> vertices = new List<Vector3>();

        foreach (var piece in Pieces)
        {
            if (piece.MaterialIndex == materialIndex)
            {
                foreach (var vertex in piece.LODs[lodIndex].Vertices)
                {
                    vertices.Add(new Vector3(vertex.Position.x, vertex.Position.y, vertex.Position.z));
                }
            }
        }

        return vertices;
    }

    public List<BoneWeight> GetBoneWeights(int lodIndex, int materialIndex)
    {
        List<BoneWeight> boneWeights = new();

        foreach (var piece in Pieces)
        {
            if (piece.MaterialIndex == materialIndex)
            {
                foreach (var vertex in piece.LODs[lodIndex].Vertices)
                {
                    boneWeights.Add(vertex.GetBoneWeight());
                }
            }
        }

        return boneWeights;
    }

    public List<Vector3> GetNormals(int lodIndex, int materialIndex)
    {
        List<Vector3> normals = new();

        foreach (var piece in Pieces)
        {
            if (piece.MaterialIndex == materialIndex)
            {
                foreach (var vertex in piece.LODs[lodIndex].Vertices)
                {
                    normals.Add(new Vector3(vertex.Normal.x, vertex.Normal.y, vertex.Normal.z));
                }
            }
        }

        return normals;
    }

    public List<Vector2> GetTextureCoordinates(int lodIndex, int materialIndex, bool flipV, string relativePathToABCFileLowercase)
    {
        List<Vector2> textureCoordinates = new();

        foreach (var piece in Pieces)
        {
            if (piece.MaterialIndex == materialIndex)
            {
                for (int i = 0; i < piece.LODs[lodIndex].Vertices.Count; i++)
                {
                    var matchingFaceVertex = piece.LODs[lodIndex].Faces
                        .SelectMany(face => face.FaceVertices)
                        .FirstOrDefault(faceVertices => faceVertices.VertexIndex == i);

                    if (matchingFaceVertex != null)
                    {
                        textureCoordinates.Add(new Vector2(
                            matchingFaceVertex.Texcoord.x,
                            flipV ? 1f - matchingFaceVertex.Texcoord.y : matchingFaceVertex.Texcoord.y));
                    }
                    else
                    {
                        // In Lithtech, UV coordinates are on faces/triangles.
                        // In Unity, UV coordinates are on vertices. If a vertex isn't referenced by any face, we won't have any UV data for it so just fill out a dummy so the mesh won't break.
                        textureCoordinates.Add(new Vector2());
                    }
                }
            }
        }

        return textureCoordinates;
    }
    
    /// <summary>
    /// Resolves UV mismatches where the same vertex index is referenced by multiple faces
    /// with differing UV coordinates. Duplicate vertices are appended to the LOD's vertex
    /// list and the offending face indices are updated to point to the new entries.
    /// This is run across all pieces and all LODs before the Pieces list is populated.
    /// </summary>
    public void FixFacesForExtraUVCoordinates()
    {
        const float UVTolerance = 1e-6f;

        foreach (var piece in Pieces)
        {
            foreach (var lod in piece.LODs)
            {
                // Track the canonical UV assigned to each vertex index.
                // Key = original vertex index, Value = (tu, tv) already claimed by a prior face.
                var assignedUVs = new Dictionary<int, (float tu, float tv)>();

                foreach (var face in lod.Faces)
                {
                    for (int faceSlot = 0; faceSlot < face.FaceVertices.Count; faceSlot++)
                    {
                        var faceVertRef = face.FaceVertices[faceSlot];
                        int originalIndex = faceVertRef.VertexIndex;

                        if (!assignedUVs.TryGetValue(originalIndex, out var claimedUV))
                        {
                            // First face to use this vertex index — claim its UV.
                            assignedUVs[originalIndex] = (faceVertRef.Texcoord.x, faceVertRef.Texcoord.y);
                        }
                        else
                        {
                            // Another face already claimed this vertex with a different UV.
                            bool tuMismatch = Math.Abs(faceVertRef.Texcoord.x - claimedUV.tu) > UVTolerance;
                            bool tvMismatch = Math.Abs(faceVertRef.Texcoord.y - claimedUV.tv) > UVTolerance;

                            if (tuMismatch || tvMismatch)
                            {
                                // Clone the original vertex and append it.
                                var newVertex = new VertexModel(lod.Vertices[originalIndex]);
                                ushort newIndex = (ushort)lod.Vertices.Count;
                                lod.Vertices.Add(newVertex);

                                // Re-point this face slot to the new vertex.
                                faceVertRef.VertexIndex = newIndex;

                                // Claim the new index's UV so subsequent faces sharing this
                                // same mismatch UV can also reuse it instead of creating more duplicates.
                                assignedUVs[newIndex] = (faceVertRef.Texcoord.x, faceVertRef.Texcoord.y);
                            }
                        }
                    }
                }
            }
        }
    }
}