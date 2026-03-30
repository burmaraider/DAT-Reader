using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ABCMeshLookupUtility : DataExtractor
{
    private static void ScaleVertices(List<Vector3> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = vertices[i] * UnityScaleFactor;
        }
    }

    public static Mesh CreateMesh(int lodIndex, PiecesChunk piecesChunk, List<Matrix4x4> bindPoses, string relativePathToABCFileLowercase)
    {
        List<Vector3> vertices = new();
        List<Vector2> uvs = new();
        List<Vector3> normals = new();
        List<BoneWeight> boneWeights = new();
        for (int textureIndex = 0; textureIndex < piecesChunk.TotalTextureCount; textureIndex++)
        {
            vertices.AddRange(piecesChunk.GetVertices(lodIndex, textureIndex));
            uvs.AddRange(piecesChunk.GetTextureCoordinates(lodIndex, textureIndex, true, relativePathToABCFileLowercase));
            normals.AddRange(piecesChunk.GetNormals(lodIndex, textureIndex));
            boneWeights.AddRange(piecesChunk.GetBoneWeights(lodIndex, textureIndex));
        }

        ScaleVertices(vertices);

        var mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        mesh.boneWeights = boneWeights.ToArray();
        mesh.bindposes = bindPoses.ToArray();
        mesh.subMeshCount = piecesChunk.TotalTextureCount;

        for (int textureIndex = 0; textureIndex < piecesChunk.TotalTextureCount; textureIndex++)
        {
            var indices = piecesChunk.GetIndices(lodIndex, textureIndex, false);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, textureIndex);
        }

        mesh.RecalculateBounds();
        //mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        
        return mesh;
    }

    private static void CreateABCMeshes(List<ABCModel> abcModels)
    {
        AssetDatabase.StartAssetEditing();

        int i = 0;
        foreach (var abcModel in abcModels)
        {
            i++;
            float progress = (float)i / abcModels.Count;
            EditorUtility.DisplayProgressBar("Creating ABC Meshes", $"Item {i} of {abcModels.Count}", progress);

            var bindPoses = abcModel.CreateBindPoses();

            string unityPathAndFilenameToMesh = Path.ChangeExtension(Path.Combine(ABCMeshPath, abcModel.RelativePathToABCFileLowercase), "asset").ConvertFolderSeperators();
            Directory.CreateDirectory(Path.GetDirectoryName(unityPathAndFilenameToMesh));

            try
            {
                if (abcModel.LODCount > 1)
                {
                    for (int lodIndex = 0; lodIndex < abcModel.LODCount; lodIndex++)
                    {
                        var mesh = CreateMesh(lodIndex, abcModel.PiecesChunk, bindPoses, abcModel.RelativePathToABCFileLowercase);

                        string dir = Path.GetDirectoryName(unityPathAndFilenameToMesh);
                        string name = Path.GetFileNameWithoutExtension(unityPathAndFilenameToMesh);
                        string unityPathAndFilenameToMeshLod = Path.Combine(dir, GetMeshWithLODName(name, lodIndex));
                        AssetDatabase.CreateAsset(mesh, unityPathAndFilenameToMeshLod);
                        UnityLookups.ABCMeshWithLODLookups.Add((abcModel.RelativePathToABCFileLowercase, lodIndex), unityPathAndFilenameToMeshLod);
                    }
                }
                else
                {
                    var mesh = CreateMesh(0, abcModel.PiecesChunk, bindPoses, abcModel.RelativePathToABCFileLowercase);
                    AssetDatabase.CreateAsset(mesh, unityPathAndFilenameToMesh);
                    UnityLookups.ABCMeshLookups.Add(abcModel.RelativePathToABCFileLowercase, unityPathAndFilenameToMesh);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        EditorUtility.ClearProgressBar();

        RefreshAssetDatabase();
    }

    public static string GetMeshWithLODName(string justNameWithoutExtension, int lodIndex)
    {
        return $"{justNameWithoutExtension}_LOD{lodIndex}.asset";
    }

    public static void SetLookups(List<ABCModel> abcModels)
    {
        UnityLookups.ABCMeshLookups.Clear();

        CreateABCMeshes(abcModels);
    }
}
