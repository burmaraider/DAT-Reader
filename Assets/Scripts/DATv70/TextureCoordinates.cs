using UnityEngine;
using LTTypes;
using System.Collections.Generic;

public static class TextureCoordinates
{
    public static int nTexWidth = 256;
    public static int nTexHeight = 256;
    public static int nScale = 1;
    public static List<Vector3> CalculateUVForPoly(WorldPoly tPoly)
    {

        float test = (float)tPoly.m_O.X;

        Vector3 O = new Vector3((float)tPoly.m_O.X, (float)tPoly.m_O.Z, (float)tPoly.m_O.Y);
        Vector3 P = new Vector3((float)tPoly.m_P.X, (float)tPoly.m_P.Z, (float)tPoly.m_P.Y);
        Vector3 Q = new Vector3((float)tPoly.m_Q.X, (float)tPoly.m_Q.Z, (float)tPoly.m_Q.Y);

        Vector3 center = new Vector3((float)tPoly.m_vCenter.X, (float)tPoly.m_vCenter.Z, (float)tPoly.m_vCenter.Y);

        O *= nScale;
        O -= center;
        P /= nScale;
        Q /= nScale;

        for(int i = 0; i < (tPoly.m_nLoVerts - 2); i++)
        {
            
            //tPoly.m_aDiskVerts

        }

        return new List<Vector3>();
    }
}