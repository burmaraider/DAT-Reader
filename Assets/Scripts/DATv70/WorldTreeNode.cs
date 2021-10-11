using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using static LTTypes.LTTypes;


public class WorldTreeNode
    {
        public WorldTreeNode(List<WorldTreeNode> nodeList)
        {
            pNodeList = nodeList;
        }

        public WorldTreeNode()
        {
        }

        public LTVector vBBoxMin { get; set; }
        public LTVector vBBoxMax { get; set; }
        public LTFloat fCenterX { get; set; }
        public LTFloat fCenterZ { get; set; }
        public LTFloat fSmallestDim { get; set; }
        public WorldTreeNode pParent { get; set; }
        public int nChildren { get; set; }
        public List<WorldTreeNode> pNodeList { get; set; }

        public void SetBB(LTVector a, LTVector b)
        {
            vBBoxMin = a;
            vBBoxMax = b;

            fCenterX = (LTFloat)((LTFloat)(b.X + a.X) * 0.5f);
            fCenterZ = (LTFloat)((LTFloat)(b.Z + a.Z) * 0.5f);
            fSmallestDim = (LTFloat)System.Math.Min(b.X - a.X, b.Z - a.Z);
        }

        public WorldTreeNode GetChild(int nX, int nZ)
        {
            return pNodeList[nChildren + (nX * 2 + nZ)];
        }

        public WorldTreeNode GetChild(int nNode)
        {
            return pNodeList[nChildren + nNode];
        }

        public void Subdivide(List<WorldTreeNode> pNodeList, ref int nCurOffset)
        {
            LTVector vMin, vMax;

            nChildren = nCurOffset;
            nCurOffset = nCurOffset + 4;

            for(int i = 0; i < 4 ; i++)
            {
                GetChild(i).pParent = this;
            }

            vMin = new LTVector(vBBoxMin.X, vBBoxMin.Y, vBBoxMin.Z);
            vMax = new LTVector(fCenterX, vBBoxMax.Y, fCenterZ);
            GetChild(0, 0).SetBB(vMin, vMax);

            vMin = new LTVector(vBBoxMin.X, vBBoxMin.Y, fCenterZ);
            vMax = new LTVector(fCenterX, vBBoxMax.Y, vBBoxMax.Z);
            GetChild(0, 1).SetBB(vMin, vMax);

            vMin = new LTVector(fCenterX, vBBoxMin.Y, vBBoxMin.Z);
            vMax = new LTVector(vBBoxMax.X, vBBoxMax.Y, fCenterZ);
            GetChild(1, 0).SetBB(vMin, vMax);

            vMin = new LTVector(fCenterX, vBBoxMin.Y, fCenterZ);
            vMax = new LTVector(vBBoxMax.X, vBBoxMax.Y, vBBoxMax.Z);
            GetChild(1, 1).SetBB(vMin, vMax);


        }
       public void LoadLayout(ref BinaryReader b, ref byte nCurByte, ref byte nCurBit, List<WorldTreeNode> pNodeList, ref int nCurOffset)
       {
            bool bSubdivide;

            if(nCurBit == 8)
            {
                nCurByte = (byte)b.ReadByte();
                nCurBit = 0;
            }

            bSubdivide = (nCurByte & (1 << nCurBit)) > 0;
            nCurBit++;

            if (bSubdivide)
            {
                Subdivide(pNodeList, ref nCurOffset);

                for (int i = 0; i < 4; i++)
                {
                    GetChild(i).LoadLayout(ref b, ref nCurByte, ref nCurBit, pNodeList, ref nCurOffset);
                }
            }
        }
    }

    public class WorldTree
    {
        public int nNumNode { get; set; }
        public WorldTreeNode pRootNode { get; set; }
        public List<WorldTreeNode> pNodes { get; set; }

        public WorldTree()
        {
            pNodes = new List<WorldTreeNode>();
            pRootNode = new WorldTreeNode(pNodes);
        }

        public void ReadWorldTree(ref BinaryReader b)
        {
            int nDummyTerrainDepth, nCurOffset, i;
            LTVector vBoxMin, vBoxMax;
            byte nCurByte, nCurBit;
            WorldTreeNode pNewNode;

            nDummyTerrainDepth = 0;
            vBoxMin = DATReader70.ReadLTVector(ref b);
            vBoxMax = DATReader70.ReadLTVector(ref b);

            nNumNode = b.ReadInt32();
            nDummyTerrainDepth = b.ReadInt32();

            i = 0;

            if (nNumNode > 1)
            {
                while(i < nNumNode - 1)
                {
                    pNewNode = new WorldTreeNode(pNodes);
                    pNodes.Add(pNewNode);
                    i++;
                }
            }

            nCurByte = 0;
            nCurBit = 8;

            pRootNode.SetBB(vBoxMin, vBoxMax);

            nCurOffset = 0;

            pRootNode.LoadLayout(ref b, ref nCurByte, ref nCurBit, pNodes, ref nCurOffset);
        }

    }

/*
public class WorldTreeNode
{
    LTVector m_vBBoxMin;
    LTVector m_vBBoxMax;

    float m_fCenterX;
    float m_fCenterZ;
    float m_fSmallestDim;
    WorldTreeNode m_pParent;
    int m_nChildren;
    List<WorldTreeNode> m_pNodeList = new List<WorldTreeNode>();

    public LTVector BoxMin 
    {
        get{ return m_vBBoxMin;} 
        set{ m_vBBoxMin = value;}
    }
    public LTVector BoxMax 
    {
        get{ return m_vBBoxMax;} 
        set{ m_vBBoxMax = value;}
    }

    public float CenterX
    {
        get{return m_fCenterX;}
        set{m_fCenterX = value;}
    }
    public float CenterZ
    {
        get{return m_fCenterZ;}
        set{m_fCenterZ = value;}
    }

    public float SmallestDim
    {
        get{return m_fSmallestDim;}
        set{m_fSmallestDim = value;}
    }

    public WorldTreeNode GetChild(int nX, int nZ)
    {
        return (WorldTreeNode)m_pNodeList[m_nChildren + (nX * 2 + nZ)];
    }

    public WorldTreeNode GetChild(int nNode)
    {
        return (WorldTreeNode)m_pNodeList[m_nChildren + nNode];
    }

    public void SetBBox(LTVector pMin, LTVector pMax)
    {
        m_vBBoxMin = pMin;
        m_vBBoxMax = pMax;

        m_fCenterX = (pMax.X + pMin.X) * 0.5f;
        m_fCenterZ = (pMax.Z + pMin.Z) * 0.5f;

        m_fSmallestDim = System.Math.Min(pMax.X - pMin.X, pMax.Z - pMin.Z);
    }

    public void Subdivide(List<WorldTreeNode> pNodeList, ref int nCurrentOffset)
    {
        LTVector vMin, vMax;
        m_nChildren = nCurrentOffset;
        nCurrentOffset = nCurrentOffset + 4;

        for(int i = 0; i < 4; i++)
        {
            GetChild(i).m_pParent = this;
        }

        vMin = new LTVector(m_vBBoxMin.X, m_vBBoxMin.Y, m_vBBoxMin.Z);
        vMax = new LTVector((LTFloat)m_fCenterX, m_vBBoxMax.Y, (LTFloat)m_fCenterZ);
        GetChild(0,0).SetBBox(vMin, vMax);

        vMin = new LTVector(m_vBBoxMin.X, m_vBBoxMin.Y, (LTFloat)m_fCenterZ);
        vMax = new LTVector((LTFloat)m_fCenterX, m_vBBoxMax.Y, m_vBBoxMax.Z);
        GetChild(0,1).SetBBox(vMin, vMax);

        vMin = new LTVector((LTFloat)m_fCenterX, m_vBBoxMin.Y, m_vBBoxMin.Z);
        vMax = new LTVector(m_vBBoxMax.X, m_vBBoxMax.Y, (LTFloat)m_fCenterZ);
        GetChild(1,0).SetBBox(vMin, vMax);
        
        vMin = new LTVector((LTFloat)m_fCenterX, m_vBBoxMin.Y, (LTFloat)m_fCenterZ);
        vMax = new LTVector(m_vBBoxMax.X, m_vBBoxMax.Y, m_vBBoxMax.Z);
        GetChild(1,1).SetBBox(vMin, vMax);
        
    }

    public void LoadLayout(ref BinaryReader b, ref byte nCurByte, ref byte nCurBit, List<WorldTreeNode> pNodeList, ref int nCurOffset)
    {
        bool bSubdivide;

        if(nCurBit == 8)
        {
            nCurByte = b.ReadByte();
            nCurBit = 0;
        }

        //bSubdivide = (nCurByte & (1 << nCurBit)) > 0;
        bSubdivide = (nCurByte & (1 << nCurBit)) > 0;

        nCurBit ++;

        if(bSubdivide)
        {
            Subdivide(pNodeList, ref nCurOffset);
            for(int i = 0; i < m_nChildren; i++)
            {
                GetChild(i).LoadLayout(ref b, ref nCurByte, ref nCurBit, pNodeList, ref nCurOffset);
            }
        }
    }
    public WorldTreeNode(List<WorldTreeNode> pNodeList)
    {
        m_pNodeList = pNodeList;
    }
    public WorldTreeNode()
    {
    }



}
*/