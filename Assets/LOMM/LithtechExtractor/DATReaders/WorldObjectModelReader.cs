using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static LTTypes;
using static LTUtils;

public static class WorldObjectModelReader
{
    private static bool NameMatches(string valueToCheck, params string[] names)
    {
        return names.Any(x => x.Equals(valueToCheck, StringComparison.OrdinalIgnoreCase));
    }

    private static bool NameMatchesMonster(string valueToCheck)
    {
        return NameMatches(valueToCheck,
            "Fish", "Bandit", "SkeletonWarrior", "Soldier", "Skeleton", "DragonFly", "Monk", "Troglodyte", "EvilEye", "Orc"
            , "Spider2", "Dagrell", "Lich", "Goblin", "EvilEyeTerror", "Dwarf", "Wight", "ArcherBot", "Gargoyle", "Basilisk", "Spider"
            , "Harpy", "Cow", "Goat", "LizardWarrior", "DruidBot", "Bird", "GolemStone", "Pig", "LichKing", "LizardMan", "Troll"
            , "PaladinBot", "Zombie", "DragonRed", "Titan", "Duck", "Mummy", "Hen", "Bat", "Gopher", "Rooster", "Nobleman"
            , "TitanGrand", "WarriorBot", "DwarfKing", "TownsFolkFemale", "TownsFolkGirl", "TownsFolkFemaleMid", "ElementalEarth", "Priest", "HereticBot");
    }

    public static WorldObjectTypes GetWorldObjectType(string objectType)
    {
        if (NameMatches(objectType, "StaticSunLight", "GlowingLight", "DirLight", "ObjectLight", "Light"))
        {
            return WorldObjectTypes.Light;
        }

        if (NameMatches(objectType, "AIBarrier"))
        {
            return WorldObjectTypes.AIBarrier;
        }

        if (NameMatches(objectType, "AIRail"))
        {
            return WorldObjectTypes.AIRail;
        }

        if (NameMatches(objectType, "Prop", "WallTorch", "BagGold", "Brazier", "TreasureChest", "Torch", "CandleWall", "DestructableProp", "Candle", "Candelabra", "Chandelier", "PropDamager"))
        {
            return WorldObjectTypes.Prop;
        }

        if (NameMatches(objectType, "StartPoint"))
        {
            return WorldObjectTypes.StartPoint;
        }

        if (NameMatches(objectType, "AmbientSound", "Sound", "SoundFX"))
        {
            return WorldObjectTypes.Sound;
        }

        if (NameMatches(objectType, "BuyZone"))
        {
            return WorldObjectTypes.BuyZone;
        }

        if (NameMatches(objectType, "RescueZone", "GoodKingRescueZone", "EvilKingRescueZone"))
        {
            return WorldObjectTypes.RescueZone;
        }

        if (NameMatches(objectType, "Teleporter", "PortalZone"))
        {
            return WorldObjectTypes.Teleporter;
        }

        if (NameMatches(objectType, "SwordInStone"))
        {
            return WorldObjectTypes.SwordInStone;
        }

        if (NameMatches(objectType, "WorldProperties"))
        {
            return WorldObjectTypes.WorldProperties;
        }

        if (NameMatches(objectType, "Princess"))
        {
            return WorldObjectTypes.Princess;
        }

        if (NameMatches(objectType, "SoftLandingZone"))
        {
            return WorldObjectTypes.SoftLandingZone;
        }

        if (NameMatches(objectType, "SpectatorStartPoint"))
        {
            return WorldObjectTypes.SpectatorStartPoint;
        }

        if (NameMatches(objectType, "EndlessFall"))
        {
            return WorldObjectTypes.EndlessFall;
        }

        if (NameMatches(objectType, "BlueWater", "ClearWater", "CorrosiveFluid", "DirtyWater", "Kato", "LiquidNitrogen", "PoisonGas", "Smoke"))
        {
            return WorldObjectTypes.VisibleVolume;
        }

        if (NameMatches(objectType, "BuyZone", "DamageBrush", "Electricity", "EndlessFall", "Ladder", "PortalZone", "SoftLandingZone", "TintScreen", "TotalRed", "Vacuum", "Weather", "Wind", "ZeroGravity", "InvisibleBrush"))
        {
            return WorldObjectTypes.InvisibleVolume;
        }

        if (NameMatches(objectType, "RotatingDoor", "Door"))
        {
            return WorldObjectTypes.Door;
        }

        if (NameMatches(objectType, "DestructableBrush", "DamageBrush", "WorldObject", "RotatingBrush", "Terrain", "Brush", "VolumeBrush", "WorldModelDebris", "GenericObject"))
        {
            return WorldObjectTypes.MiscGeometry;
        }

        if (NameMatches(objectType, "DemoSkyWorldModel", "SkyPointer"))
        {
            return WorldObjectTypes.Skybox;
        }

        if (NameMatchesMonster(objectType))
        {
            return WorldObjectTypes.Monster;
        }

        return WorldObjectTypes.Unknown;
    }

    public static List<WorldObjectModel> ReadWorldObjectModels(BinaryReader binaryReader)
    {
        var worldObjects = new List<WorldObjectModel>();

        var objectCount = binaryReader.ReadInt32();

        for (int i = 0; i < objectCount; i++)
        {
            WorldObjectModel worldObject = new WorldObjectModel();

            worldObject.dataOffset = binaryReader.BaseStream.Position; // store our offset in our .dat
            worldObject.dataLength = binaryReader.ReadInt16(); // Read our object datalength
            var dataLength = binaryReader.ReadInt16(); //read out property length
            worldObject.ObjectType = ReadString(dataLength, ref binaryReader); // read our name
            worldObject.OptionsCount = binaryReader.ReadInt32();// read how many properties this object has

            for (int t = 0; t < worldObject.OptionsCount; t++)
            {
                var nObjectPropertyDataLength = binaryReader.ReadInt16();
                string szPropertyName = ReadString(nObjectPropertyDataLength, ref binaryReader);

                // Sometimes an object has the same key twice but different spelling - like RayHit or Rayhit.
                // They seem to have the same value so just remove the key if found and let it get replaced below.
                if (worldObject.options.ContainsKey(szPropertyName))
                {
                    worldObject.options.Remove(szPropertyName);
                    var prop = worldObject.Properties
                        .FirstOrDefault(x => x.Name.Equals(szPropertyName, System.StringComparison.OrdinalIgnoreCase));
                    if (prop != null)
                    {
                        worldObject.Properties.Remove(prop);
                    }
                }

                PropType propType = (PropType)binaryReader.ReadByte();

                worldObject.objectEntryFlag.Add(binaryReader.ReadInt32()); //read the flag

                switch (propType)
                {
                    case PropType.String:
                        worldObject.objectEntryStringDataLength.Add(binaryReader.ReadInt16()); //read the string length plus the data length
                        nObjectPropertyDataLength = binaryReader.ReadInt16();

                        //Read the string
                        string stringVal = ReadString(nObjectPropertyDataLength, ref binaryReader);
                        worldObject.options.Add(szPropertyName, stringVal);
                        worldObject.AddProperty(propType, szPropertyName, stringVal);
                        break;

                    case PropType.Vector:
                        nObjectPropertyDataLength = binaryReader.ReadInt16();

                        //Get our float data
                        LTVector tempVec = ReadLTVector(ref binaryReader);

                        //Add our object to the Dictionary
                        worldObject.options.Add(szPropertyName, tempVec);
                        worldObject.AddProperty(propType, szPropertyName, tempVec);
                        break;

                    case PropType.Rotation:

                        //Get our data length
                        nObjectPropertyDataLength = binaryReader.ReadInt16();

                        //Get our float data
                        LTRotation tempRot = ReadLTRotation(ref binaryReader);

                        //Add our object to the Dictionary
                        worldObject.options.Add(szPropertyName, tempRot);
                        worldObject.AddProperty(propType, szPropertyName, tempRot);
                        break;
                    case PropType.UInt:

                        // Read the "size" of what we should read.
                        // For UINT the nObjectPropertyDataLength should always be 4.
                        nObjectPropertyDataLength = binaryReader.ReadInt16();

                        //Add our object to the Dictionary
                        uint uIntVal = binaryReader.ReadUInt32();
                        worldObject.options.Add(szPropertyName, uIntVal);
                        worldObject.AddProperty(propType, szPropertyName, uIntVal);
                        break;
                    case PropType.Bool:
                        nObjectPropertyDataLength = binaryReader.ReadInt16();
                        bool boolVal = ReadBool(ref binaryReader);
                        worldObject.options.Add(szPropertyName, boolVal);
                        worldObject.AddProperty(propType, szPropertyName, boolVal);
                        break;
                    case PropType.Float:
                        nObjectPropertyDataLength = binaryReader.ReadInt16();

                        //Add our object to the Dictionary
                        var floatVal = ReadReal(ref binaryReader);
                        worldObject.options.Add(szPropertyName, floatVal);
                        worldObject.AddProperty(propType, szPropertyName, floatVal);
                        break;
                    case PropType.Color:
                        nObjectPropertyDataLength = binaryReader.ReadInt16();

                        //Get our float data
                        LTVector tempCol = ReadLTVector(ref binaryReader);

                        //Add our object to the Dictionary
                        worldObject.options.Add(szPropertyName, tempCol);
                        worldObject.AddProperty(propType, szPropertyName, tempCol);
                        break;
                    default:
                        Debug.LogError("Unknown prop type: " + propType);
                        break;
                }
            }

            worldObject.WorldObjectType = GetWorldObjectType(worldObject.ObjectType);
            worldObjects.Add(worldObject);
        }

        return worldObjects;
    }
}
