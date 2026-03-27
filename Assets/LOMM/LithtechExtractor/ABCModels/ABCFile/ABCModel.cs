using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ABCModel
{
    public string Name { get; set; }
    public int Version { get; set; }
    public string CommandString { get; set; }
    public float InternalRadius { get; set; }

    public int LODCount { get; set; }
    public int NodeCount { get; set; }
    public int WeightSetCount { get; set; }

    public List<float> LODDistances { get; set; }

    public PiecesChunk PiecesChunk { get; set; }

    public Node RootNode { get; set; }

    public string RelativePathToABCFileLowercase { get; set; }

    private void FlattenNodesRecursive(Node node, List<Node> list)
    {
        if (node == null)
        {
            return;
        }

        list.Add(node);

        foreach (var child in node.Children)
        {
            FlattenNodesRecursive(child, list);
        }
    }

    public int GetMaterialCount()
    {
        if (PiecesChunk.Pieces == null || PiecesChunk.Pieces.Count == 0)
        {
            return 0;
        }

        // The number of materials is the number of unique/distinct MaterialIndexes.
        return PiecesChunk.Pieces.Select(x => x.MaterialIndex).Distinct().Count();
    }

    public ushort GetMaxMaterialIndex()
    {
        if (PiecesChunk.Pieces == null || PiecesChunk.Pieces.Count == 0)
        {
            return 0;
        }

        // The number of materials is the number of unique/distinct MaterialIndexes.
        return PiecesChunk.Pieces.Max(x => x.MaterialIndex);
    }

    public List<Node> GetFlattenedNodes()
    {
        var result = new List<Node>();
        FlattenNodesRecursive(RootNode, result);
        return result;
    }

    public List<Transform> GetFlattenedBoneTransforms()
    {
        var allNodes = GetFlattenedNodes();
        var flattenedBoneTransforms = allNodes.OrderBy(x => x.Id).Select(x => x.GameObject.transform).ToList();
        return flattenedBoneTransforms;
    }
}
