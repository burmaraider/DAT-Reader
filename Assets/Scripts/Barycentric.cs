using UnityEngine;

public struct Barycentric
{
    public float u;
    public float v;
    public float w;
    public Barycentric(float aU, float aV, float aW)
    {
        u = aU;
        v = aV;
        w = aW;
    }
    public Barycentric(Vector2 aV1, Vector2 aV2, Vector2 aV3, Vector2 aP)
    {
        Vector2 a = aV2 - aV3, b = aV1 - aV3, c = aP - aV3;
        float aLen = a.x * a.x + a.y * a.y;
        float bLen = b.x * b.x + b.y * b.y;
        float ab = a.x * b.x + a.y * b.y;
        float ac = a.x * c.x + a.y * c.y;
        float bc = b.x * c.x + b.y * c.y;
        float d = aLen * bLen - ab * ab;
        u = (aLen * bc - ab * ac) / d;
        v = (bLen * ac - ab * bc) / d;
        w = 1.0f - u - v;
    }

    public Barycentric(Vector3 aV1, Vector3 aV2, Vector3 aV3, Vector3 aP)
    {
        Vector3 a = aV2 - aV3, b = aV1 - aV3, c = aP - aV3;
        float aLen = a.x * a.x + a.y * a.y + a.z * a.z;
        float bLen = b.x * b.x + b.y * b.y + b.z * b.z;
        float ab = a.x * b.x + a.y * b.y + a.z * b.z;
        float ac = a.x * c.x + a.y * c.y + a.z * c.z;
        float bc = b.x * c.x + b.y * c.y + b.z * c.z;
        float d = aLen * bLen - ab * ab;
        u = (aLen * bc - ab * ac) / d;
        v = (bLen * ac - ab * bc) / d;
        w = 1.0f - u - v;
    }
    public Barycentric(Vector4 aV1, Vector4 aV2, Vector4 aV3, Vector4 aP)
    {
        Vector4 a = aV2 - aV3, b = aV1 - aV3, c = aP - aV3;
        float aLen = a.x * a.x + a.y * a.y + a.z * a.z + a.w * a.w;
        float bLen = b.x * b.x + b.y * b.y + b.z * b.z + b.w * b.w;
        float ab = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
        float ac = a.x * c.x + a.y * c.y + a.z * c.z + a.w * c.w;
        float bc = b.x * c.x + b.y * c.y + b.z * c.z + b.w * c.w;
        float d = aLen * bLen - ab * ab;
        u = (aLen * bc - ab * ac) / d;
        v = (bLen * ac - ab * bc) / d;
        w = 1.0f - u - v;
    }
    public Barycentric(Color aV1, Color aV2, Color aV3, Color aP)
    {
        Color a = aV2 - aV3, b = aV1 - aV3, c = aP - aV3;
        float aLen = a.r * a.r + a.g * a.g + a.b * a.b;
        float bLen = b.r * b.r + b.g * b.g + b.b * b.b;
        float ab = a.r * b.r + a.g * b.g + a.b * b.b;
        float ac = a.r * c.r + a.g * c.g + a.b * c.b;
        float bc = b.r * c.r + b.g * c.g + b.b * c.b;
        float d = aLen * bLen - ab * ab;
        u = (aLen * bc - ab * ac) / d;
        v = (bLen * ac - ab * bc) / d;
        w = 1.0f - u - v;
    }

    public bool IsInside
    {
        get
        {
            return (u >= 0.0f) && (u <= 1.0f) && (v >= 0.0f) && (v <= 1.0f) && (w >= 0.0f); //(w <= 1.0f)
        }
    }
    public Vector2 Interpolate(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        return v1 * u + v2 * v + v3 * w;
    }
    public Vector3 Interpolate(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return v1 * u + v2 * v + v3 * w;
    }
    public Vector4 Interpolate(Vector4 v1, Vector4 v2, Vector4 v3)
    {
        return v1 * u + v2 * v + v3 * w;
    }
    public Color Interpolate(Color v1, Color v2, Color v3)
    {
        return v1 * u + v2 * v + v3 * w;
    }
}
