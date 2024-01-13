using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using static LithFAQ.LTTypes;

public class WorldData
{
    int m_nFlags;
    WorldBsp m_pOriginalBSP = new WorldBsp();
    int m_nNextPos;

    public int Flags {
        get{return m_nFlags;}
        set{m_nFlags = value;}
    }

    public WorldBsp OriginalBSP
    {
        get{ return m_pOriginalBSP;}
        set{ m_pOriginalBSP = value;}
    }

    public int NextPos
    {
        get{ return m_nNextPos;}
        set{ m_nNextPos = value;}
    }
}