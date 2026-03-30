using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class VertexModel
{
    public ushort SublodVertexIndex { get; set; }
    public List<WeightModel> Weights { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Normal { get; set; }

    public VertexModel()
    {
        this.Weights = new List<WeightModel>();
    }

    public VertexModel(VertexModel source)
        : this()
    {
        SublodVertexIndex = source.SublodVertexIndex;
        Position = source.Position;
        Normal = source.Normal;
        foreach (var weight in source.Weights)
        {
            Weights.Add(new WeightModel
            {
                NodeIndex = weight.NodeIndex,
                Location = weight.Location,
                Bias = weight.Bias
            });
        }
    }

    public BoneWeight GetBoneWeight()
    {
        BoneWeight boneWeight = new BoneWeight();
        if (Weights.Count > 0) { boneWeight.boneIndex0 = Weights[0].NodeIndex; boneWeight.weight0 = Weights[0].Bias; }
        if (Weights.Count > 1) { boneWeight.boneIndex1 = Weights[1].NodeIndex; boneWeight.weight1 = Weights[1].Bias; }
        if (Weights.Count > 2) { boneWeight.boneIndex2 = Weights[2].NodeIndex; boneWeight.weight2 = Weights[2].Bias; }
        if (Weights.Count > 3) { boneWeight.boneIndex3 = Weights[3].NodeIndex; boneWeight.weight3 = Weights[3].Bias; }

        return boneWeight;
    }
}
