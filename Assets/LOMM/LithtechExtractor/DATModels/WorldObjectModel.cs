using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static LTTypes;

public class WorldObjectModel
{
    public Dictionary<string, object> options { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    public List<WorldObjectPropertyModel> Properties { get; set; } = new List<WorldObjectPropertyModel>();
    public void AddProperty(PropType propType, string name, string val) { Properties.Add(new WorldObjectPropertyModel { StringValue = val, Name = name, PropType = propType }); }
    public void AddProperty(PropType propType, string name, Vector3 val) { Properties.Add(new WorldObjectPropertyModel { VectorValue = val, Name = name, PropType = propType }); }
    public void AddProperty(PropType propType, string name, float val) { Properties.Add(new WorldObjectPropertyModel { FloatValue = val, Name = name, PropType = propType }); }
    public void AddProperty(PropType propType, string name, uint val) { Properties.Add(new WorldObjectPropertyModel { UIntValue = val, Name = name, PropType = propType }); }
    public void AddProperty(PropType propType, string name, bool val) { Properties.Add(new WorldObjectPropertyModel { BoolValue = val, Name = name, PropType = propType }); }
    public void AddProperty(PropType propType, string name, LTRotation val) { Properties.Add(new WorldObjectPropertyModel { QuatValue = new Quaternion { w = val.W, x = val.X, y = val.Y, z = val.Z }, Name = name, PropType = propType }); }

    public string UniqueName { get; set; }
    public string ObjectType { get; set; }
    public WorldObjectTypes WorldObjectType { get; set; }
    public short dataLength { get; set; }
    public long dataOffset { get; set; }
    public int OptionsCount { get; set; }
    public bool IsBSP { get; set; }
    public List<int> objectEntryFlag { get; set; } = new List<int>();
    public List<short> objectEntryStringDataLength { get; set; } = new List<short>();

    // ******************************************************************************************
    // ** Properties below are set in FlattenProperties
    // ******************************************************************************************
    private bool flattened = false;
    private string name;
    private string skyObjectName;
    private LTTypes.LTVector originalPosition;
    private LTTypes.LTRotation originalRotation;
    private Vector3? position;
    private Quaternion? rotation;
    private Vector3? rotationInDegrees;
    private bool? hasGravity;
    private bool? moveToFloor;
    private string weaponType;
    private float? scale;
    private float? surfaceAlpha;
    private float? index;
    private string originalFilename;
    private string filename;
    private string filenameLowercase;
    private bool isABC;
    private string skin;
    private string allSkinsPathsLowercase;
    private List<string> skinsLowercase;
    private bool solid;
    private bool visible;
    private bool hidden;
    private bool rayhit;
    private bool shadow;
    private bool transparent;
    private bool showSurface;
    private bool useRotation;
    private string spriteSurfaceName;
    private Vector3? surfaceColor1;
    private Vector3? surfaceColor2;
    private float? viscosity;
    private float teamNumber;
    private float playerNumber;

    public string Name
    {
        get
        {
            if (name == null)
            {
                name = GetStringPropValue("Name");
            }

            return name;
        }
    }

    public string SkyObjectName { get { FlattenProperties(); return skyObjectName; } }
    public LTTypes.LTVector OriginalPosition { get { FlattenProperties(); return originalPosition; } }
    public LTTypes.LTRotation OriginalRotation { get { FlattenProperties(); return originalRotation; } }
    public Vector3? Position { get { FlattenProperties(); return position; } }
    public Quaternion? Rotation { get { FlattenProperties(); return rotation; } }
    public Vector3? RotationInDegrees { get { FlattenProperties(); return rotationInDegrees; } }
    public bool? HasGravity { get { FlattenProperties(); return hasGravity; } }
    public bool? MoveToFloor { get { FlattenProperties(); return moveToFloor; } }
    public string WeaponType { get { FlattenProperties(); return weaponType; } }
    public float? Scale { get { FlattenProperties(); return scale; } }
    public float? SurfaceAlpha { get { FlattenProperties(); return surfaceAlpha; } }
    public string OriginalFilename { get { FlattenProperties(); return originalFilename; } }
    public string Filename { get { FlattenProperties(); return filename; } }
    public string FilenameLowercase { get { FlattenProperties(); return filenameLowercase; } }
    public bool IsABC { get { FlattenProperties(); return isABC; } }
    public string Skin { get { FlattenProperties(); return skin; } }
    public string AllSkinsPathsLowercase { get { FlattenProperties(); return allSkinsPathsLowercase; } }
    public List<string> SkinsLowercase { get { FlattenProperties(); return skinsLowercase; } }
    public float? Index { get { FlattenProperties(); return index; } }
    public bool Solid { get { FlattenProperties(); return solid; } }
    public bool Visible { get { FlattenProperties(); return visible; } }
    public bool Hidden { get { FlattenProperties(); return hidden; } }
    public bool Rayhit { get { FlattenProperties(); return rayhit; } }
    public bool Shadow { get { FlattenProperties(); return shadow; } }
    public bool Transparent { get { FlattenProperties(); return transparent; } }
    public bool ShowSurface { get { FlattenProperties(); return showSurface; } }
    public bool UseRotation { get { FlattenProperties(); return useRotation; } }
    public string SpriteSurfaceName { get { FlattenProperties(); return spriteSurfaceName; } }
    public Vector3? SurfaceColor1 { get { FlattenProperties(); return surfaceColor1; } }
    public Vector3? SurfaceColor2 { get { FlattenProperties(); return surfaceColor2; } }
    public float? Viscosity { get { FlattenProperties(); return viscosity; } }
    public float PlayerNumber { get { FlattenProperties(); return playerNumber; } }
    public float TeamNumber { get { FlattenProperties(); return teamNumber; } }

    public bool? GetBoolPropValue(string propName)
    {
        return options.TryGetValue(propName, out var value) && value is bool boolVal
            ? boolVal
            : null;
    }

    public float? GetFloatPropValue(string propName)
    {
        return options.TryGetValue(propName, out var value) && value is float floatVal
            ? floatVal
            : null;
    }

    public uint? GetUIntPropValue(string propName)
    {
        return options.TryGetValue(propName, out var value) && value is uint uintVal
            ? uintVal
            : null;
    }

    public float? GetFloatValueFromUIntProp(string propName)
    {
        uint? tempVal = options.TryGetValue(propName, out var value) && value is uint uintVal
            ? uintVal
            : null;

        if (tempVal == null) return null;

        var bytes = BitConverter.GetBytes(tempVal.Value);
        return BitConverter.ToSingle(bytes, 0);
    }

    public string GetStringPropValue(string propName)
    {
        return options.TryGetValue(propName, out var value)
            ? value?.ToString()
            : null;
    }

    public LTTypes.LTVector GetVectorPropValue(string propName)
    {
        return options.TryGetValue(propName, out var value) && value is LTTypes.LTVector vec
            ? vec
            : default(LTTypes.LTVector);
    }

    public LTTypes.LTRotation GetRotationPropValue(string propName)
    {
        return options.TryGetValue(propName, out var value) && value is LTTypes.LTRotation rot
            ? rot
            : default(LTTypes.LTRotation);
    }

    public Vector3? GetVector3PropValue(string propName)
    {
        LTTypes.LTVector? tempVal = options.TryGetValue(propName, out var value) && value is LTTypes.LTVector vec
            ? vec
            : null;

        if (tempVal == null) return null;

        return tempVal.Value;
    }

    public Quaternion? GetQuaternionPropValue(string propName)
    {
        LTTypes.LTRotation? tempVal = options.TryGetValue(propName, out var value) && value is LTTypes.LTRotation rot
            ? rot
            : null;

        if (tempVal == null) return null;

        return new Quaternion(tempVal.Value.X, tempVal.Value.Y, tempVal.Value.Z, tempVal.Value.W);
    }

    public void FlattenProperties()
    {
        if (flattened) return;

        skyObjectName = GetStringPropValue("SkyObjectName");
        originalPosition = GetVectorPropValue("Pos");
        position = GetVector3PropValue("Pos");
        originalRotation = GetRotationPropValue("Rotation");
        rotation = GetQuaternionPropValue("Rotation");
        rotationInDegrees = rotation.HasValue ? new Vector3(rotation.Value.x * Mathf.Rad2Deg, rotation.Value.y * Mathf.Rad2Deg, rotation.Value.z * Mathf.Rad2Deg) : null;
        hasGravity = GetBoolPropValue("Gravity");
        solid = GetBoolPropValue("Solid") ?? false;
        visible = GetBoolPropValue("Visible") ?? false;
        hidden = GetBoolPropValue("Hidden") ?? false;
        rayhit = GetBoolPropValue("Rayhit") ?? false;
        shadow = GetBoolPropValue("Shadow") ?? false;
        transparent = GetBoolPropValue("Transparent") ?? false;
        showSurface = GetBoolPropValue("ShowSurface") ?? false;
        useRotation = GetBoolPropValue("UseRotation") ?? false;
        moveToFloor = GetBoolPropValue("MoveToFloor");
        weaponType = GetStringPropValue("WeaponType");
        scale = GetFloatPropValue("Scale");
        surfaceAlpha = GetFloatPropValue("SurfaceAlpha");

        spriteSurfaceName = GetStringPropValue("SpriteSurfaceName");
        surfaceColor1 = GetVector3PropValue("SurfaceColor1");
        surfaceColor1 = GetVector3PropValue("SurfaceColor1");
        viscosity = GetFloatPropValue("Viscosity");

        teamNumber = GetFloatValueFromUIntProp("TeamNbr") ?? 0f;
        playerNumber = GetFloatValueFromUIntProp("PlayerNbr") ?? 0f;

        originalFilename = GetStringPropValue("Filename");
        if (originalFilename != null)
        {
            filename = Path.Combine(originalFilename, string.Empty);
            filenameLowercase = filename.ConvertFolderSeperators().ToLower();
        }
        skin = GetStringPropValue("Skin");

        skinsLowercase = new List<string>();

        if (!string.IsNullOrWhiteSpace(skin))
        {
            allSkinsPathsLowercase = skin.ConvertFolderSeperators().ToLower();
            skinsLowercase = skin.SplitOnSemicolonAndLowercase();
        }

        if (!string.IsNullOrWhiteSpace(filename))
        {
            isABC = Path.GetExtension(filename).ToLower() == ".abc";
        }

        index = GetFloatValueFromUIntProp("Index");
    }
}
