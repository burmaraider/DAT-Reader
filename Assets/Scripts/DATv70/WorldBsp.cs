using System;
using System.Collections.Generic;
using System.IO;
using static LithFAQ.LTTypes;
using static LithFAQ.LTUtils;

public class WorldBsp
{
    public Int16 m_nWorldInfoFlags;
    public String m_szWorldName;
    public int       m_nPoints;    
    public int       m_nPlanes;      
    public int       m_nSurfaces;
    public int    m_nUserPortals;
    public int       m_nPolies;
    public int       m_nLeafs; 
    public int       m_nVerts;
    public int m_nTotalVisListSize;
    public int      m_nLeafLists;
    public int       m_nNodes;
    public int       m_nSections;
    public LTVector       m_vMinBox;
    public LTVector       m_vMaxBox;
    public LTVector m_vWorldTranslation;
    public int m_nNamesLen;
    public int m_nTextures;
    public List<String> m_aszTextureNames = new List<String>();                               
    public List<WorldPoly> m_pPolies = new List<WorldPoly>();
    public List<WorldPlane> m_pPlanes = new List<WorldPlane>();
    public List<WorldSurface> m_pSurfaces = new List<WorldSurface>();
    public List<WorldVertex> m_pPoints = new List<WorldVertex>();
    public List<Leafs> m_pLeafs = new List<Leafs>();
    public List<WorldTreeNode> m_pNodes;
    public List<object> m_pUserPortals = new List<object>();
    public List<object> m_pPBlockTable = new List<object>();

    public String WorldName
    {
        get{return m_szWorldName;}
    }

    public int datVersion;
    public int Load(ref BinaryReader b, bool doIt)
    {
        int dwWorldInfoFlags, dwUnknown, dwUnknown2, dwUnknown3 = 0;
        Int16 nNameLen = 0;

        m_nWorldInfoFlags = (short)b.ReadInt32();
        dwUnknown = b.ReadInt32();
        nNameLen = b.ReadInt16();
        m_szWorldName = ReadString(nNameLen, ref b);

        if(nNameLen == 0 || nNameLen > 255)
        {
            String errString = String.Format("Name Length was: {0}\n Name was: {1}", nNameLen, m_szWorldName);
            throw new Exception(errString);
        }

        m_nPoints = b.ReadInt32();
        m_nPlanes = b.ReadInt32();
        m_nSurfaces = b.ReadInt32();

        m_nUserPortals = b.ReadInt32();
        m_nPolies = b.ReadInt32();
        m_nLeafs = b.ReadInt32();
        m_nVerts = b.ReadInt32();
        m_nTotalVisListSize = b.ReadInt32();
        m_nLeafLists = b.ReadInt32();
        m_nNodes = b.ReadInt32();

        dwUnknown2 = b.ReadInt32();
        dwUnknown3 = b.ReadInt32();

        m_vMinBox = ReadLTVector(ref b);
        m_vMaxBox = ReadLTVector(ref b);
        m_vWorldTranslation = ReadLTVector(ref b);

        m_nNamesLen = b.ReadInt32();
        m_nTextures = b.ReadInt32(); //66 this is the number of OPQ textures, 70 this is the number of textures

        //if(datVersion == 66)
            //m_nSurfaces = m_nTextures; //66 this is the number of OPQ textures, 70 this is the number of textures


        //read textures
        ReadTextures(ref b);
        ReadPolies1(ref b);
        ReadLeafs(ref b);
        ReadPlanes(ref b);
        if(datVersion == 70)
            ReadSurfaces70(ref b);
        else if(datVersion == 66)
            ReadSurfaces66(ref b);
        if(datVersion == 70)
            ReadPoints(ref b);
        ReadPolies2(ref b);
        if(datVersion == 66)
        {
            b.BaseStream.Position += m_nNodes * 14;
            ReadPoints(ref b);
        }
        return 1;
    }

    public void ReadTextures(ref BinaryReader b)
    {
        for(int i = 0; i < m_nTextures; i++)
        {
            byte[] tempString = new byte[1];
            while(b.PeekChar() != 0)
            {
                tempString[tempString.Length-1] = b.ReadByte();
                Array.Resize(ref tempString, tempString.Length + 1);
            }
            Array.Resize(ref tempString, tempString.Length-1); // get rid of the 0x0
            m_aszTextureNames.Add(System.Text.Encoding.ASCII.GetString(tempString)); //add it to the list!
            b.BaseStream.Position++;
        }
        
    }

    public void ReadPolies1(ref BinaryReader b)
    {
        for(int i = 0; i < m_nPolies; i++)
        {
            WorldPoly pPoly = new WorldPoly();
            
            Int16 nVertices = b.ReadInt16();
            byte hi = (byte)(nVertices>>8);
            byte lo =  (byte)(nVertices&0xff);

            pPoly.m_nIndexAndNumVerts = i;
            pPoly.m_nLoVerts = lo;
            pPoly.m_nHiVerts = hi;

            nVertices = (short)(pPoly.m_nLoVerts + pPoly.m_nHiVerts);

            pPoly.m_nIndexAndNumVerts = unchecked((pPoly.m_nIndexAndNumVerts & 0xFFFFFF00) | (nVertices & 0xFF) );

            m_pPolies.Add(pPoly);
        }
    }

    public void ReadLeafs(ref BinaryReader b)
    {
        if(m_nLeafs > 0)
        {
             for(int i = 0; i < m_nLeafs; i++)
             {
                Leafs pLeaf = new Leafs();

                pLeaf.m_nNumLeafLists = b.ReadInt16();

                if(pLeaf.m_nNumLeafLists == 0xFFFF)
                {
                    pLeaf.m_nLeafListIndex = b.ReadInt16();
                }

                else if(pLeaf.m_nNumLeafLists > 0)
                {
                    for(int t = 0; t < pLeaf.m_nNumLeafLists; t++)
                    {
                        LeafList pLeafList = new LeafList();

                        pLeafList.m_nPortalId = b.ReadInt16();
                        pLeafList.m_nSize = b.ReadInt16();
                        Array.Resize(ref pLeafList.m_pContents, pLeafList.m_nSize);
                        pLeafList.m_pContents = b.ReadBytes(pLeafList.m_nSize);

                        pLeaf.m_pLeafLists.Add(pLeafList);
                    }
                }

                pLeaf.m_nPoliesCount = b.ReadInt32();
                if(pLeaf.m_nPoliesCount > 0)
                {
                    Array.Resize(ref pLeaf.m_pPolies, pLeaf.m_nPoliesCount * 4);

                    for(int y = 0; y < pLeaf.m_pPolies.Length; y++)
                    {
                        pLeaf.m_pPolies[y] = b.ReadInt16();
                    }
                }

                pLeaf.m_nCardinal1 = b.ReadInt32();

                m_pLeafs.Add(pLeaf);
             }
        }
    }

    public void ReadPlanes(ref BinaryReader b)
    {
        if(m_nPlanes > 0)
        {
            for(int i =0; i< m_nPlanes; i++)
            {
                WorldPlane pPlane = new WorldPlane();
                pPlane.m_vNormal = ReadLTVector(ref b);
                pPlane.m_fDist = b.ReadSingle();
                m_pPlanes.Add(pPlane);
            }
        }
    }

    public void ReadSurfaces70(ref BinaryReader b)
    {
        if(m_nSurfaces > 0)
        {
            for(int i =0; i < m_nSurfaces; i++)
            {
                WorldSurface pSurface = new WorldSurface();
                pSurface.m_fUV1 = ReadLTVector(ref b);
                pSurface.m_fUV2 = ReadLTVector(ref b);
                pSurface.m_fUV3 = ReadLTVector(ref b);
                //extra stuff in .dat 66
                pSurface.m_nTexture = b.ReadInt16();
                pSurface.m_nFlags = b.ReadInt32();
                pSurface.m_nUnknown1 = b.ReadByte();
                pSurface.m_nUnknown2 = b.ReadByte();
                pSurface.m_nUnknown3 = b.ReadByte();
                pSurface.m_nUnknown4 = b.ReadByte();
                pSurface.m_nUseEffect = b.ReadByte();

                if(pSurface.m_nUseEffect > 0)
                {
                    Int16 nLen = b.ReadInt16();
                    if(nLen > 0)
                    {
                        pSurface.m_szEffect = ReadString(nLen, ref b);
                    }
                    nLen = b.ReadInt16();
                    if(nLen > 0)
                    {
                        pSurface.m_szEffectParam = ReadString(nLen, ref b);
                    }
                }

                pSurface.m_nTextureFlags = b.ReadInt16();
                
                m_pSurfaces.Add(pSurface);
            }
        }
    }

    public void ReadSurfaces66(ref BinaryReader b)
    {
        if(m_nSurfaces > 0)
        {
            for(int i =0; i < m_nSurfaces; i++)
            {
                WorldSurface pSurface = new WorldSurface();
                pSurface.m_fUV1 = ReadLTVector(ref b);
                pSurface.m_fUV2 = ReadLTVector(ref b);
                pSurface.m_fUV3 = ReadLTVector(ref b);
                //extra stuff in .dat 66
                pSurface.m_nTexture = b.ReadInt16();
                b.BaseStream.Position +=4;
                pSurface.m_nFlags = b.ReadInt32();
                pSurface.m_nUnknown1 = b.ReadByte();
                pSurface.m_nUnknown2 = b.ReadByte();
                pSurface.m_nUnknown3 = b.ReadByte();
                pSurface.m_nUnknown4 = b.ReadByte();
                pSurface.m_nUseEffect = b.ReadByte();

                if(pSurface.m_nUseEffect > 0)
                {
                    Int16 nLen = b.ReadInt16();
                    if(nLen > 0)
                    {
                        pSurface.m_szEffect = ReadString(nLen, ref b);
                    }
                    nLen = b.ReadInt16();
                    if(nLen > 0)
                    {
                        pSurface.m_szEffectParam = ReadString(nLen, ref b);
                    }
                }

                pSurface.m_nTextureFlags = b.ReadInt16();
                
                m_pSurfaces.Add(pSurface);
            }
        }
    }

    public void ReadPoints(ref BinaryReader b)
    {
        if(m_nPoints > 0)
        {
            for(int i = 0; i < m_nPoints; i++)
            {
                WorldVertex pVertex = new WorldVertex();
                pVertex.m_vData = ReadLTVector(ref b);
                if(datVersion == 66) // skip the normals, unity does a good enough job
                    b.BaseStream.Position += 12;
                m_pPoints.Add(pVertex);
            }
        }
    }

    public void ReadPolies2(ref BinaryReader b)
    {
        for(int i = 0; i < m_nPolies; i++)
        {
            if(datVersion == 70)
                m_pPolies[i].ReadPoly70(ref b);
            if(datVersion == 66)
                m_pPolies[i].ReadPoly66(ref b);
        }
    }

    public void ReadNodes(ref BinaryReader b)
    {

    }
}