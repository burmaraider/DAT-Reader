using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using static LTTypes.LTTypes;

public class WorldPoly
{
    public long m_nIndexAndNumVerts;
    public byte m_nLoVerts, m_nHiVerts;
    public LTVector m_vCenter;
    public LTFloat m_fRadius;
    public Int16 m_nLightmapWidth;
    public Int16 m_nLightmapHeight;
    public Int16 m_nUnknownNum;
    //public List<Int16> m_anUnknownList;
    public Int16[] m_anUnknownList;
    public LTVector m_O;
    public LTVector m_P;
    public LTVector m_Q;
    public int m_nPlane;
    public int m_nSurface;
    public List<VertexColor> m_aVertexColorList;
    public List<DiskRelVert> m_aRelDiskVerts;
    //: TLTRelDiskVertList;
    public int m_nLMFrameIndex; 

    public void ReadPoly70(ref BinaryReader b)
    {
        //Debug.Log("Position is: " + b.BaseStream.Position + "\n");

        float x,y,z;

        x = b.ReadSingle();
        y = b.ReadSingle();
        z = b.ReadSingle();

        m_vCenter = new LTVector((LTFloat)x,(LTFloat)y, (LTFloat)z);
        m_nLightmapWidth = b.ReadInt16();
        m_nLightmapHeight = b.ReadInt16();

        m_nUnknownNum = b.ReadInt16();

        if(m_nUnknownNum > 0)
        {
            Array.Resize(ref m_anUnknownList, m_nUnknownNum * 2);
            
            for(int t = 0; t < m_anUnknownList.Length; t++)
            {
                try
                {
                    m_anUnknownList[t] = b.ReadInt16();
                }
                catch(Exception e)
                {
                    Debug.Log("Failed at Pos: " + b.BaseStream.Position);
                }
            }
        }

        m_nSurface = b.ReadInt32();
        m_nPlane = b.ReadInt32();

        m_O = DATReader70.ReadLTVector(ref b);
        m_P = DATReader70.ReadLTVector(ref b);
        m_Q = DATReader70.ReadLTVector(ref b);

        int verts = GetNumVertices();
        m_aVertexColorList = new List<VertexColor>();
        for(int t = 0; t < verts; t++)
        {
            VertexColor _vertexColors = new VertexColor();
            _vertexColors.nVerts = b.ReadInt16();
            _vertexColors.red = b.ReadByte();
            _vertexColors.green = b.ReadByte();
            _vertexColors.blue = b.ReadByte();
            m_aVertexColorList.Add(_vertexColors);
        }
        FillRelVerts();  
    }

    public void ReadPoly66(ref BinaryReader b)
    {
        //Debug.Log("Position is: " + b.BaseStream.Position + "\n");

        float x,y,z;

        x = b.ReadSingle();
        y = b.ReadSingle();
        z = b.ReadSingle();

        m_vCenter = new LTVector((LTFloat)x,(LTFloat)y, (LTFloat)z);
        m_nLightmapWidth = b.ReadInt16();
        m_nLightmapHeight = b.ReadInt16();

        m_nUnknownNum = b.ReadInt16();

        if(m_nUnknownNum > 0)
        {
            Array.Resize(ref m_anUnknownList, m_nUnknownNum * 2);
            
            for(int t = 0; t < m_anUnknownList.Length; t++)
            {
                try
                {
                    m_anUnknownList[t] = b.ReadInt16();
                }
                catch(Exception e)
                {
                    Debug.Log("Failed at Pos: " + b.BaseStream.Position);
                    Debug.Log(e.Message);
                }
            }
        }

        m_nSurface = b.ReadInt16();
        m_nPlane = b.ReadInt16();

        //m_vUV1 = DATReader70.ReadLTVector(ref b);
        //m_vUV2 = DATReader70.ReadLTVector(ref b);
        //m_vUV3 = DATReader70.ReadLTVector(ref b);

        int verts = GetNumVertices();
        m_aVertexColorList = new List<VertexColor>();
        for(int t = 0; t < verts; t++)
        {
            VertexColor _vertexColors = new VertexColor();
            _vertexColors.nVerts = b.ReadInt16();
            _vertexColors.red = b.ReadByte();
            _vertexColors.green = b.ReadByte();
            _vertexColors.blue = b.ReadByte();
            m_aVertexColorList.Add(_vertexColors);
        }
        FillRelVerts();  
    }

    public int GetNumVertices()
    {
        return (int)m_nIndexAndNumVerts & 0xFF;
    }

    public void FillRelVerts()
    {
        m_aRelDiskVerts = new List<DiskRelVert>();
        for(int i = 0; i < m_nLoVerts; i++)
        {
            m_aRelDiskVerts.Add(new DiskRelVert());
            m_aRelDiskVerts[i].nRelVerts = (short)i;
        }
    }
}