using System.Collections.Generic;
using UnityEngine;

public class VertexModel
{
    public ushort SublodVertexIndex { get; set; }
    public List<WeightModel> Weights { get; set; }
    public Vector3 Location { get; set; }
    public Vector3 Normal { get; set; }

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
