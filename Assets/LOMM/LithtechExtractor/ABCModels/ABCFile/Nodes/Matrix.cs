using UnityEngine;

public class Matrix
{
    public Vector4 Unknown1 { get; set; }
    public Vector4 Unknown2 { get; set; }
    public Vector4 Unknown3 { get; set; }
    public Vector4 Unknown4 { get; set; }

    private bool positionSet = false;
    private Vector3 position;

    // Position is just the z value from Unknown4, Unknown1, Unknown2
    public Vector3 Position
    {
        get
        {
            if (!positionSet)
            {
                position = new Vector3(Unknown4.z, Unknown1.z, Unknown2.z);
            }

            return position;
        }

        set
        {
            this.position = value;
            positionSet = true;
        }
    }

    public string ToFormattedString(int indentLevel = 0)
    {
        var indent = new string('\t', indentLevel);
        return string.Empty
            + indent + $"Unk1: {Unknown1.w,-13:g}, {Unknown1.x,-13:g}, {Unknown1.y,-13:g}, {Unknown1.z,-13:g}\r\n"
            + indent + $"Unk2: {Unknown2.w,-13:g}, {Unknown2.x,-13:g}, {Unknown2.y,-13:g}, {Unknown2.z,-13:g}\r\n"
            + indent + $"Unk3: {Unknown3.w,-13:g}, {Unknown3.x,-13:g}, {Unknown3.y,-13:g}, {Unknown3.z,-13:g}\r\n"
            + indent + $"Unk4: {Unknown4.w,-13:g}, {Unknown4.x,-13:g}, {Unknown4.y,-13:g}, {Unknown4.z,-13:g}\r\n"
            + indent + $"Pos:  {Position.x,-13:g}, {Position.y,-13:g}, {Position.z,-13:g}\r\n"
            ;
    }
}