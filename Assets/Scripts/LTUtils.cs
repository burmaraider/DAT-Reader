using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static LithFAQ.LTTypes;


namespace LithFAQ
{
    [Flags]
    public enum BitMask
    {
        SOLID = 0x01,
        NONEXISTENT = 0x02,
        INVISIBLE = 0x04,
        TRANSLUCENT = 0x08,
        SKY = 0x10,
        BRIGHT = 0x20,
        FLATSHADE = 0x40,
        LIGHTMAP = 0x80,
        NOSUBDIVIDE = 0x100,
        HULL = 0x200,
        DIRECTIONLIGHT = 0x400,
        GOURAUD = 0x800,
        PORTAL = 0x1000,
        PANNINGSKY = 0x1000,
    }
    
    public static class LTUtils
    {
        public static String ReadString(int dataLength, ref BinaryReader b)
        {
            byte[] tempArray = b.ReadBytes(dataLength);
            return System.Text.Encoding.ASCII.GetString(tempArray);
        }

        /// <summary>
        /// Get the object transform X, Y, Z of the Lithtech Object
        /// </summary>
        /// <param name="b"></param>
        /// <seealso cref="LTVector">See here</seealso>
        /// <returns></returns>
        public static LTVector ReadLTVector(ref BinaryReader b)
        {
            //Read data length 12 bytes
            //x - single
            //y - single
            //z - single
            float x, y, z;

            x = b.ReadSingle();
            y = b.ReadSingle();
            z = b.ReadSingle();

            return new LTVector((LTFloat)x, (LTFloat)y, (LTFloat)z);
        }

        /// <summary>
        /// Get the object Rotation X, Y, Z, W of the Lithtech Object
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static LTRotation ReadLTRotation(ref BinaryReader b)
        {
            //Read data length 12 bytes
            //x - single
            //y - single
            //z - single
            //w - single
            byte[] tempByte = b.ReadBytes(16);

            float x, y, z, w;

            x = BitConverter.ToSingle(tempByte, 0);
            y = BitConverter.ToSingle(tempByte, sizeof(Single));
            z = BitConverter.ToSingle(tempByte, sizeof(Single) + sizeof(Single));
            w = BitConverter.ToSingle(tempByte, sizeof(Single) + sizeof(Single) + sizeof(Single));

            return new LTRotation((LTFloat)x, (LTFloat)y, (LTFloat)z, (LTFloat)w);
        }

        /// <summary>
        /// Get the objects property type of the Lithtech Object
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static byte ReadPropertyType(ref BinaryReader b)
        {
            //Read the PropType
            return b.ReadByte();
        }

        /// <summary>
        /// Get the LongInt used in AllowedGameTypes of the Lithtech Object
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Int64 ReadLongInt(ref BinaryReader b)
        {
            //Read the Int64
            b.BaseStream.Position += 4;
            int temp = (int)b.ReadSingle();
            return temp;
        }

        /// <summary>
        /// Get the true or false flag from the property of the Lithtech Object
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool ReadBool(ref BinaryReader b)
        {
            //Read the string
            byte[] tempByte = new byte[1];
            b.Read(tempByte, 0, tempByte.Length);
            return BitConverter.ToBoolean(tempByte, 0);
        }
        /// <summary>
        /// Get the Real used in single float values of the Lithtech Object
        /// </summary>
        /// <param name="b"></param>
        /// <returns>description</returns>
        public static float ReadReal(ref BinaryReader b)
        {
            return b.ReadSingle();
        }
    }
}