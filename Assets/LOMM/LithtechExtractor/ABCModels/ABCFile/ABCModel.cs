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

    private List<Node> _flattenedNodesCache = null;

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

    public List<Node> GetFlattenedNodes()
    {
        if (_flattenedNodesCache is null)
        {
            _flattenedNodesCache = new List<Node>();
            FlattenNodesRecursive(RootNode, _flattenedNodesCache);
        }
        
        return _flattenedNodesCache;
    }


    private void CreateBindPosesRecursive(Node node, List<Matrix4x4> bindPoses)
    {
        bindPoses.Add(node.Matrix.ToMatrix4x4().inverse);

        foreach (var child in node.Children)
        {
            CreateBindPosesRecursive(child, bindPoses);
        }
    }

    public List<Matrix4x4> CreateBindPoses()
    {
        var bindPoses = new List<Matrix4x4>();
        CreateBindPosesRecursive(RootNode, bindPoses);
        return bindPoses;
    }
}
