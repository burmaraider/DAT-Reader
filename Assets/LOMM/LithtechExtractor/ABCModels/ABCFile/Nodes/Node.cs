using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Node
{
    public string Name { get; set; } = string.Empty;
    public int Id { get; set; }
    public byte Flags { get; set; }
    public Matrix Matrix { get; set; }

    public List<Node> Children { get; set; } = new List<Node>();

    public bool IsHumanoid()
    {
        if (Name.Contains("Bip01", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Children is not null)
        {
            foreach (var child in Children)
            {
                if (child.IsHumanoid())
                {
                    return true;
                }
            }
        }

        return false;
    }

    public string ToFormattedString(bool showMatrix = false, int indentLevel = 0)
    {
        var childString = string.Empty;
        foreach (var child in Children)
        {
            childString += child.ToFormattedString(showMatrix, indentLevel + 1);
        }

        var indent = new string('\t', indentLevel);
        return
            // Indent level:
            indent
            // Id: Name - Chidren.Count
            + $"{Id}: {Name} - {Children.Count}\r\n"
            + (showMatrix ? Matrix.ToFormattedString(indentLevel + 1) : string.Empty)
            + childString;
    }

    public override string ToString()
    {
        string s = string.Empty;
        s += "\t--------------------\n";
        s += "\tName: " + Name + "\n";
        s += "\t--------------------\n";
        //for (int LODIndex = 0; LODIndex < detailLevels.Count; LODIndex++)
        //{
        //	s += "\t" + String.Format("LOD: {0} (Spec Power={1}, Spec Scale={2}, Texture Index={3}, unknown1={4}, unknown2={5})", LODIndex, specularPower, specularScale, textureIndex, unknown1, unknown2) + "\n";
        //	s += "\t\t" + "Number Triangles: " + detailLevels[LODIndex].Triangles.Count.ToString() + "\n";
        //	for (int i = 0; i < detailLevels[LODIndex].Triangles.Count; i++)
        //	{
        //		s += "\t\t\t" + "Triangle #" + i.ToString() + detailLevels[LODIndex].Triangles[i].ToString() + "\n";
        //	}

        //	s += "\t\t" + "Number Vertices: " + detailLevels[LODIndex].Vertices.Count.ToString() + "\n";
        //	for (int i = 0; i < detailLevels[LODIndex].Vertices.Count; i++)
        //	{
        //		s += String.Format("\t\t\tVertex #{0,-3}: {1}\n", i, detailLevels[LODIndex].Vertices[i].ToString2());
        //	}
        //}

        return s;
    }
}