using UnityEngine;

namespace Utility
{
    public class WorldObjectComponent : MonoBehaviour
    {
        public string ObjectType;
        public WorldObjectTypes WorldObjectType;
        public Vector3 Position;
        public Vector3 RotationInDegrees;
        public bool MoveToFloor;
        public bool HasGravity;
        public bool Solid;
        public bool Visible;
        public bool Hidden;
        public bool Rayhit;
        public bool Shadow;
        public bool Transparent;
        public bool ShowSurface;
        public bool UseRotation;
        public float Scale;
        public bool IsABC;
        public string Filename;
        public string Skin;
        public string WeaponType;
        public bool HasSurfaceAlpha;
        public float SurfaceAlpha;
        public float Index;
        public string SkyObjectName;
        public string SpriteSurfaceName;
        public Vector3 SurfaceColor1;
        public Vector3 SurfaceColor2;
        public float Viscosity;
        public float TeamNumber;
        public float PlayerNumber;
    }
}
