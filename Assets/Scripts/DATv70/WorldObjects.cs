using System;
using System.Collections.Generic;
using static LithFAQ.LTTypes;

public class WorldObjects
    {
        public List<WorldObject> obj;

        public int endingOffset;
    }
public class WorldObject
    {
        public Dictionary<string, object> options;
        public string objectName;
        public string objectType;
        public Int16 dataLength;
        public Int32 dataOffset;
        public Int32 objectEntries;

    }

public class VertexColor
{
    public Int16 nVerts;
    public byte red;
    public byte green;
    public byte blue;
}

public class DiskRelVert
{
    public Int16 nRelVerts;
}

public class Leafs
{
    public int m_nNumLeafLists;
    public Int16 m_nLeafListIndex;
    public List<LeafList> m_pLeafLists = new List<LeafList>();

    public int m_nPoliesCount;

    public Int16[] m_pPolies;

    public int m_nCardinal1;
}

public class LeafList
{
    public Int16 m_nPortalId;
    public Int16 m_nSize;
    public byte[] m_pContents;
}

public class WorldPlane
{
    public LTVector m_vNormal;
    public float m_fDist;
}

public class WorldSurface
{
    public LTVector m_fUV1;
    public LTVector m_fUV2;
    public LTVector m_fUV3;
    public Int16 m_nTexture;
    public int m_nFlags;
    public byte m_nUnknown1;
    public byte m_nUnknown2;
    public byte m_nUnknown3;
    public byte m_nUnknown4;
    public byte m_nUseEffect;
    public String m_szEffect;
    public String m_szEffectParam;
    public Int16 m_nTextureFlags;
}


public class WorldVertex
{
    public LTVector m_vData;
}

public class WorldModelList
{
    public int nNumModels;
    public List<WorldData> pModelList;
}