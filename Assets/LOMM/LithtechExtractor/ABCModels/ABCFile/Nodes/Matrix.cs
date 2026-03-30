using System;
using UnityEngine;

public class Matrix
{
    public Vector4 Row0 { get; set; }
    public Vector4 Row1 { get; set; }
    public Vector4 Row2 { get; set; }
    public Vector4 Row3 { get; set; }

    private bool positionSet = false;
    private Vector3 position;

    // Position is just the z value from Unknown4, Unknown1, Unknown2
    public Vector3 Position
    {
        get
        {
            if (!positionSet)
            {
                position = new Vector3(Row3.z, Row0.z, Row1.z);
            }

            return position;
        }

        set
        {
            this.position = value;
            positionSet = true;
        }
    }

    public Quaternion Rotation
    {
        get
        {
            float m00 = Row0.x;
            float m01 = Row0.y;
            float m02 = Row0.z;

            float m10 = Row1.x;
            float m11 = Row1.y;
            float m12 = Row1.z;

            float m20 = Row3.x;
            float m21 = Row3.y;
            float m22 = Row3.z;

            float trace = m00 + m11 + m22;
            float qw, qx, qy, qz;

            if (trace > 0f)
            {
                float s = Mathf.Sqrt(trace + 1f) * 2f;
                qw = 0.25f * s;
                qx = (m21 - m12) / s;
                qy = (m02 - m20) / s;
                qz = (m10 - m01) / s;
            }
            else if ((m00 > m11) && (m00 > m22))
            {
                float s = Mathf.Sqrt(1f + m00 - m11 - m22) * 2f;
                qw = (m21 - m12) / s;
                qx = 0.25f * s;
                qy = (m01 + m10) / s;
                qz = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                float s = Mathf.Sqrt(1f + m11 - m00 - m22) * 2f;
                qw = (m02 - m20) / s;
                qx = (m01 + m10) / s;
                qy = 0.25f * s;
                qz = (m12 + m21) / s;
            }
            else
            {
                float s = Mathf.Sqrt(1f + m22 - m00 - m11) * 2f;
                qw = (m10 - m01) / s;
                qx = (m02 + m20) / s;
                qy = (m12 + m21) / s;
                qz = 0.25f * s;
            }

            return new Quaternion(qx, qy, qz, qw);
        }
    }

    public string ToFormattedString(int indentLevel = 0)
    {
        var indent = new string('\t', indentLevel);
        return string.Empty
            + indent + $"Unk1: {Row0.w,-13:g}, {Row0.x,-13:g}, {Row0.y,-13:g}, {Row0.z,-13:g}\r\n"
            + indent + $"Unk2: {Row1.w,-13:g}, {Row1.x,-13:g}, {Row1.y,-13:g}, {Row1.z,-13:g}\r\n"
            + indent + $"Unk3: {Row2.w,-13:g}, {Row2.x,-13:g}, {Row2.y,-13:g}, {Row2.z,-13:g}\r\n"
            + indent + $"Unk4: {Row3.w,-13:g}, {Row3.x,-13:g}, {Row3.y,-13:g}, {Row3.z,-13:g}\r\n"
            + indent + $"Pos:  {Position.x,-13:g}, {Position.y,-13:g}, {Position.z,-13:g}\r\n"
            ;
    }

    internal Matrix4x4 ToMatrix4x4()
    {
        return Matrix4x4.TRS(Position, Quaternion.Normalize(Rotation), Vector3.one);
    }
}
