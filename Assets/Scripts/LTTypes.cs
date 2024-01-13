using System;
using Unity;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LithFAQ
{
    public static class LTTypes
    {
        public enum PropType
        {
            PT_STRING = 0,
            PT_VECTOR = 1,
            PT_COLOR = 2,
            PT_REAL = 3,
            PT_FLAGS = 4,
            PT_BOOL = 5,
            PT_LONGINT = 6,
            PT_ROTATION = 7
        }

        public struct LTFloat : IEquatable<LTFloat>
        {
            public LTFloat(float i)
            {
                this.I = i;
            }
            public float I { get; set; }

            public bool Equals(LTFloat other) => I == other.I;
            public override bool Equals(object obj) => obj is LTFloat other && Equals(other);
            public override int GetHashCode() => I.GetHashCode();
            

            public override string ToString() => $"{I}";
            public static implicit operator float(LTFloat f) => f.I;
            public static bool operator ==(LTFloat left, LTFloat right) => left.Equals(right);
            public static bool operator !=(LTFloat left, LTFloat right) => !left.Equals(right);
            public static LTFloat operator +(LTFloat left, LTFloat right) => new LTFloat(left.I + right.I);
            public static LTFloat operator -(LTFloat left, LTFloat right) => new LTFloat(left.I - right.I);
            public static LTFloat operator *(LTFloat left, LTFloat right) => new LTFloat(left.I * right.I);
            public static LTFloat operator /(LTFloat left, LTFloat right) => new LTFloat(left.I / right.I);
            public static LTFloat operator %(LTFloat left, LTFloat right) => new LTFloat(left.I % right.I);

            public static implicit operator LTFloat(int i) => new LTFloat(i);
            public static implicit operator LTFloat(float i) => new LTFloat(i);


        }

        /// <summary>
        /// Return type of X, Y, Z floats
        /// </summary>
        public struct LTVector : IEquatable<LTVector>
        {
            public LTVector(LTFloat x, LTFloat y, LTFloat z)
            {
                X = x;
                Y = y;
                Z = z;
            }
            public LTFloat X { get; set; }
            public LTFloat Y { get; set; }
            public LTFloat Z { get; set; }

            public static implicit operator LTVector((LTFloat x, LTFloat y, LTFloat z) tuple) => new LTVector(tuple.x, tuple.y, tuple.z);

            public bool Equals(LTVector other) => (X.I, Y.I, Z.I) == (other.X.I, other.Y.I, other.Z.I);

            public override bool Equals(object obj) => (obj is LTVector vector) && Equals(vector);

            public static bool operator ==(LTVector left, LTVector right) => Equals(left, right);

            public static bool operator !=(LTVector left, LTVector right) => !Equals(left, right);

            public static LTVector operator +(LTVector left, LTVector right) => new LTVector(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

            public static LTVector operator -(LTVector left, LTVector right)
            {
                left.X -= right.X; 
                left.Y -= right.Y; 
                left.Z -= right.Z;
                return left;
            }


            public static LTVector operator *(LTVector left, LTVector right) => new LTVector(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

            public static LTVector operator /(LTVector left, LTVector right) => new LTVector(left.X / right.X, left.Y / right.Y, left.Z / right.Z);

            public static LTVector operator %(LTVector left, LTVector right) => new LTVector(left.X % right.X, left.Y % right.Y, left.Z % right.Z);

            public static implicit operator LTVector(Vector3 v) => new LTVector(v.x, v.y, v.z);

            public static implicit operator Vector3(LTVector v) => new Vector3(v.X, v.Y, v.Z);

            public override int GetHashCode() => (X, Y, Z).GetHashCode();

            public override string ToString() => $"X: {X} Y: {Y} Z: {Z}";
        }

        /// <summary>
        /// Return type of X Y Z W floats
        /// </summary>
        public struct LTRotation : IEquatable<LTRotation>
        {
            public LTRotation(LTFloat x, LTFloat y, LTFloat z, LTFloat w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }
            public LTFloat X { get; set; }
            public LTFloat Y { get; set; }
            public LTFloat Z { get; set; }
            public LTFloat W { get; set; }

            public bool Equals(LTRotation other) => (X.I, Y.I, Z.I, W.I) == (other.X.I, other.Y.I, other.Z.I, other.W.I);

            public override bool Equals(object obj) => (obj is LTRotation rot) && Equals(rot);

            public static bool operator ==(LTRotation left, LTRotation right) => Equals(left, right);

            public static bool operator !=(LTRotation left, LTRotation right) => !Equals(left, right);

            public override int GetHashCode() => (X, Y, Z, W).GetHashCode();

            public override string ToString() => $"X: {X} Y: {Y} Z: {Z} W: {W}";
        }

        public struct TUVPair
        {
            public TUVPair(LTFloat u, LTFloat v)
            {
                U = u;
                V = v;
            }
            public LTFloat U { get; set; }
            public LTFloat V { get; set; }
        }
    }
}