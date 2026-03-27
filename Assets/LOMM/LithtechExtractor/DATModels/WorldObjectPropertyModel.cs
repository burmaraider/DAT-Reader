using System;
using UnityEngine;
using static LTTypes;

[System.Serializable]
public class WorldObjectPropertyModel
{
    public string Name { get;set; }

    public PropType PropType { get;set; }
    
    public string StringValue { get; set; }

    public Vector3 VectorValue { get; set; }

    public float FloatValue { get; set; }

    public uint UIntValue { get; set; }

    public bool BoolValue { get; set; }

    public Quaternion QuatValue { get; set; }

    public string Value
    {
        get
        {
            switch (this.PropType)
            {
                case PropType.Float:
                    return FloatValue.ToString();
                case PropType.Flags:
                    return UIntValue.ToString();
                case PropType.Bool:
                    return BoolValue.ToString();
                case PropType.UInt:
                    return UIntValue.ToString();
                case PropType.Vector:
                case PropType.Rotation:
                    return $"{VectorValue.x},{VectorValue.y},{VectorValue.z}";
                case PropType.Color:
                    return $"{QuatValue.x},{QuatValue.y},{QuatValue.z},{QuatValue.w}";
            }

            return StringValue;
        }
    }

    public override string ToString()
    {
        var s = $"{Name} ({this.PropType}) = {Value}";
        if (this.PropType == PropType.UInt)
        {
            byte[] bytes = BitConverter.GetBytes(UIntValue);
            float floatValue = BitConverter.ToSingle(bytes, 0);

            s += $" (as float = {floatValue})";
        }
        return s;
    }
}
