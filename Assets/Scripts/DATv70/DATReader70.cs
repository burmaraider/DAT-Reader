using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SFB;
using TMPro;
using static LTTypes.LTTypes;


public class DATReader70 : MonoBehaviour
{
    public GameObject prefab;
    public WorldObjects LTGameObjects = new WorldObjects();
    WorldReader worldReader = new WorldReader();
    List<WorldBsp> bspListTest = new List<WorldBsp>();

    public void ClearLevel()
    {
        var go = GameObject.Find("Level");

        foreach(Transform child in go.transform)
        {
            Destroy(child.gameObject);
        }

        go = GameObject.Find("objects");
        
        foreach(Transform child in go.transform)
        {
            Destroy(child.gameObject);
        }

        worldReader = new WorldReader();
        bspListTest = new List<WorldBsp>();
        LTGameObjects = new WorldObjects();
    }

    public void LoadLevelDialog()
    {
        //clear out everything
        ClearLevel();

        var extensions = new [] {
                new ExtensionFilter("Lithtech World DAT", "dat" )
            };
        // Open file
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);

        if(paths.Length > 0)
            LoadLevel(paths[0]);
    }
    public void LoadLevel(String szFileName)
    {

        BinaryReader b = new BinaryReader(File.Open(szFileName, FileMode.Open));

        worldReader.ReadHeader(ref b);
        worldReader.ReadPropertiesAndExtents(ref b);

        WorldTree wTree = new WorldTree();

        wTree.ReadWorldTree(ref b);

        //read world models...
        //procedure TLTWorldReader.ReadWorldModels; 
        byte[] anDummy = new byte[32];
        String szDump = String.Empty;
        int nDummy = 0;

        WorldData pWorldData = new WorldData();
        WorldBsp pWorldBSP = new WorldBsp();
        List<WorldBsp> pWorldBSP2 = new List<WorldBsp>();
        int LoadBSPResult;

        WorldModelList WMList = new WorldModelList();
        WMList.pModelList = new List<WorldData>();
        WMList.nNumModels = b.ReadInt32();

        for(int i= 0; i< WMList.nNumModels; i++)
        {
            Debug.Log("Current Position: " + b.BaseStream.Position);
            nDummy = b.ReadInt32();
            anDummy = b.ReadBytes(anDummy.Length);

            pWorldData.NextPos = nDummy;
            WMList.pModelList.Add(pWorldData);

            WorldBsp tBSP = new WorldBsp();
            
        try
        {
            tBSP.Load(ref b, true);
            bspListTest.Add(tBSP);
        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
        }

            Debug.Log("Loaded: " + tBSP.m_szWorldName);
            b.BaseStream.Position = nDummy;
        }

        b.BaseStream.Position = worldReader.WorldHeader.dwObjectDataPos;
        LoadObjects(ref b);

        b.BaseStream.Close();


        List<Vector3> vertexList = new List<Vector3>();
        List<Vector3> vertexList2 = new List<Vector3>();

        int it = 0;
        foreach(WorldBsp tBSP in bspListTest)
        {
            Debug.Log("Generating Mesh for: " + tBSP.m_szWorldName);
            if(tBSP.m_szWorldName != "VisBSP")
            {
                GameObject mainObject = new GameObject(tBSP.WorldName);
                mainObject.transform.parent = this.transform;
                mainObject.AddComponent<MeshFilter>();
                mainObject.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));

                
                foreach(WorldPoly tPoly in tBSP.m_pPolies)
                {
                    GameObject go = new GameObject(tBSP.WorldName + it);
                    go.transform.parent = mainObject.transform;
                    go.transform.localScale = Vector3.one * 0.01f;
                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    MeshFilter mf = go.AddComponent<MeshFilter>();

                    Mesh m = new Mesh();

                    List<Vector3> tVec = new List<Vector3>();
                    List<Vector3> tNormals = new List<Vector3>();

                    for(int vCount = 0; vCount < tPoly.m_nLoVerts; vCount++)
                    {
                        WorldVertex tVertex = tBSP.m_pPoints[tPoly.m_aDiskVerts[vCount].nVerts];
                        tVec.Add(new Vector3(
                            tVertex.m_vData.X, tVertex.m_vData.Y, tVertex.m_vData.Z
                        ));

                        tNormals.Add(new Vector3(
                        tBSP.m_pPlanes[tPoly.m_nPlane].m_vNormal.X,
                        tBSP.m_pPlanes[tPoly.m_nPlane].m_vNormal.Y,
                        tBSP.m_pPlanes[tPoly.m_nPlane].m_vNormal.Z    
                        ));
                        
                    }
                    
                    m.vertices = tVec.ToArray();



                    /*
                    Vector3 uv1 = new Vector3(
                            tBSP.m_pPolies[it].m_vUV1.X,
                            tBSP.m_pPolies[it].m_vUV1.Y,
                            tBSP.m_pPolies[it].m_vUV1.Z
                    );
                    Vector3 uv2 = new Vector3(
                            tBSP.m_pPolies[it].m_vUV2.X,
                            tBSP.m_pPolies[it].m_vUV2.Y,
                            tBSP.m_pPolies[it].m_vUV2.Z
                    );
                    Vector3 uv3 = new Vector3(
                            tBSP.m_pPolies[it].m_vUV3.X,
                            tBSP.m_pPolies[it].m_vUV3.Y,
                            tBSP.m_pPolies[it].m_vUV3.Z
                    );


                    Barycentric tempB = new Barycentric(uv1, uv2, uv3, Vector3.up);

                    List<Vector2> uvList = new List<Vector2>();
                    Vector2 m_uv = tempB.Interpolate(uv1, uv2, uv3);
                   
                    m.uv = uvList.ToArray();
                    */
                    
                    //m.normals = tNormals.ToArray();
                    if(m.vertices.Length == 3)
                    {
                        int[] tempint = new int[3] {0,1,2};
                        m.SetTriangles(tempint, 0);
                    }
                    else
                    {
                        int[] tempint = new int[4] {0,1,2,3};
                        m.SetIndices(tempint, MeshTopology.Quads, 0);
                    }
                    mr.material = new Material(Shader.Find("Diffuse"));
                    m.RecalculateNormals();
                            mf.mesh = m;
                    it++;
                    tNormals.Clear();
                    tVec.Clear();
                }
                //combine meshes
                MeshFilter[] meshFilters = mainObject.GetComponentsInChildren<MeshFilter>();
                CombineInstance[] combine = new CombineInstance[meshFilters.Length];

                int i = 0;
                while (i < meshFilters.Length)
                {
                    combine[i].mesh = new Mesh();
                    combine[i].mesh = meshFilters[i].mesh;
                    combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                    meshFilters[i].gameObject.SetActive(false);

                    i++;
                }
                mainObject.transform.GetComponent<MeshFilter>().mesh = new Mesh();
                mainObject.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
                mainObject.transform.gameObject.SetActive(true);

                foreach(Transform tObj in mainObject.transform)
                {
                    Destroy(tObj.gameObject);
                }
            }
        }
    }
    public void LoadObjects(ref BinaryReader b)
    {

        LTGameObjects = ReadObjects(ref b);
        
        foreach(var obj in LTGameObjects.obj)
        {
            Vector3 objectPos = new Vector3();
            Quaternion objectRot = new Quaternion();
            Vector3 rot = new Vector3();
            String objectName = String.Empty;

            foreach(var subItem in obj.options)
            {
                if(subItem.Key == "Name")
                    objectName = (String)subItem.Value;
                
                else if(subItem.Key == "Pos")
                {
                    LTVector temp = (LTVector)subItem.Value;
                    objectPos = new Vector3(temp.X, temp.Y, temp.Z) * 0.01f;
                }

                else if(subItem.Key == "Rotation")
                {
                    LTRotation temp = (LTRotation)subItem.Value;
                    rot = new Vector3(temp.X * Mathf.Rad2Deg, temp.Y * Mathf.Rad2Deg, temp.Z * Mathf.Rad2Deg);
                }
            }

            var tempObject = Instantiate(prefab, objectPos, objectRot);
            tempObject.name = objectName;
            tempObject.transform.eulerAngles = rot;
            
            if(obj.objectName == "GameStartPoint")
                tempObject.AddComponent<GameStartPointEditor>();


            if(obj.objectName == "Light")
            {
                var light = tempObject.gameObject.AddComponent<Light>();
                
                foreach(var subItem in obj.options)
                {
                    if(subItem.Key == "LightRadius")
                        light.range = (float)subItem.Value * 0.020f;

                    else if(subItem.Key == "LightColor")
                    {
                        var vec = (LTVector)subItem.Value;
                        Vector3 col = Vector3.Normalize(new Vector3(vec.X, vec.Y, vec.Z));
                        //Vector3.Normalize()
                        light.color = new Color(col.x, col.y, col.z);
                    }

                    else if(subItem.Key == "BrightScale")
                        light.intensity = (float)subItem.Value * 0.5f;          
                }
            }

            if(obj.objectName == "DirLight")
            {
                var light = tempObject.gameObject.AddComponent<Light>();
                
                foreach(var subItem in obj.options)
                {
                    if(subItem.Key == "FOV")
                        light.spotAngle = (float)subItem.Value;

                    else if(subItem.Key == "LightRadius")
                        light.range = (float)subItem.Value * 0.015f;

                    else if(subItem.Key == "InnerColor")
                    {
                        var vec = (LTVector)subItem.Value;
                        Vector3 col = Vector3.Normalize(new Vector3(vec.X, vec.Y, vec.Z));
                        light.color = new Color(col.x, col.y, col.z);
                    }

                    else if(subItem.Key == "BrightScale")
                        light.intensity = (float)subItem.Value * 0.5f;
                }

                //light.shadows = LightShadows.Soft;
                light.type = LightType.Spot;
            }

            if(obj.objectName == "StaticSunLight")
            {
                var light = tempObject.gameObject.AddComponent<Light>();
                
                foreach(var subItem in obj.options)
                {
                    if(subItem.Key == "InnerColor")
                    {
                        var vec = (LTVector)subItem.Value;
                        Vector3 col = Vector3.Normalize(new Vector3(vec.X, vec.Y, vec.Z));
                        light.color = new Color(col.x, col.y, col.z);
                    }
                    else if(subItem.Key == "BrightScale")
                        light.intensity = (float)subItem.Value * 0.15f;
                }

                light.shadows = LightShadows.Soft;
                light.type = LightType.Directional;
            }

            var g = GameObject.Find("objects");
            tempObject.transform.SetParent(g.transform);
            g.transform.localScale = Vector3.one * 0.1f;


            var newGO = Instantiate(new GameObject(), tempObject.transform.position, Quaternion.identity);
            newGO.transform.parent = tempObject.transform;

            newGO.AddComponent<TextMeshPro>();
            var rtObj = newGO.AddComponent<RuntimeObjectType>();
            rtObj.cam = Camera.main.transform;
            rtObj.objectType = tempObject.name;
        }


        //disable unity's nastyness
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0.0f;

        //Setup AmbientLight
        SetupAmbientLight();
    }
    public void SetupAmbientLight()
    {
        if(worldReader.WorldProperties != null)
        {
            if(worldReader.WorldProperties.Contains("AmbientLight"))
            {
                int startPos = worldReader.WorldProperties.IndexOf(" ");
                int endPos = worldReader.WorldProperties.IndexOf(";");

                if(endPos == -1)
                    endPos = worldReader.WorldProperties.Length;

                var szTemp = worldReader.WorldProperties.Substring(startPos+1, endPos-1 - startPos);

                var splitStrings = szTemp.Split(' ');

                Vector3 tempVec = Vector3.Normalize(new Vector3(
                    float.Parse(splitStrings[0]),
                    float.Parse(splitStrings[1]),
                    float.Parse(splitStrings[2])
                ));

                var color = new Color(tempVec.x, tempVec.y, tempVec.y, 255);
                RenderSettings.ambientLight = color;
                RenderSettings.ambientIntensity = 0.5f;
            }
        }
    }
    public void Quit()
    {
        Application.Quit();
    }

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
        return b.ReadInt64();
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


    public static WorldObjects ReadObjects(ref BinaryReader b)
    {
        WorldObjects temp = new WorldObjects();
        temp.obj = new List<WorldObject>();
        

        //b.BaseStream.Position = offsetData;

        var totalObjectCount = b.ReadInt32();



        for(int i = 0; i < totalObjectCount; i++)
        {
            //Make a new object
            WorldObject tempObject = new WorldObject();

            //Make a dictionary to make things easier
            Dictionary<string, object> tempData = new Dictionary<string, object>();

            tempObject.dataLength = b.ReadInt16(); // Read our object datalength

            var dataLength = b.ReadInt16(); //read out property length

            //pos = (int)b.BaseStream.Position;
            tempObject.objectName = ReadString(dataLength, ref b); // read our name
           // b.BaseStream.Position = pos + dataLength;

            tempObject.objectEntries = b.ReadInt16();// read how many properties this object has

            b.BaseStream.Position += 2;

            for(int t = 0; t < tempObject.objectEntries; t++)
            {

                var tempDataLength = b.ReadInt16();

                //pos = (int)b.BaseStream.Position;
                string propertyName = ReadString(tempDataLength, ref b);
                //b.BaseStream.Position = pos + tempDataLength;

                byte propType = b.ReadByte();

                switch(propType)
                {
                    case (byte)PropType.PT_STRING:
                        b.BaseStream.Position += 6; // skip property flags;
                        //Get Data Length
                        tempDataLength = b.ReadInt16();
                        //Read the string
                        tempData.Add(propertyName, ReadString(tempDataLength, ref b));
                        break;

                    case (byte)PropType.PT_VECTOR:
                        b.BaseStream.Position += 4; // skip property flags;
                        //Get our data length
                        tempDataLength = b.ReadInt16();
                        //Get our float data
                        LTVector tempVec = ReadLTVector(ref b);
                        //Add our object to the Dictionary
                        tempData.Add(propertyName, tempVec);
                        break;

                    case (byte)PropType.PT_ROTATION:
                        b.BaseStream.Position += 4; // skip property flags;
                        //Get our data length
                        tempDataLength = b.ReadInt16();
                        //Get our float data
                        LTRotation tempRot = ReadLTRotation(ref b);
                        //Add our object to the Dictionary
                        tempData.Add(propertyName, tempRot);
                        break;
                    case (byte)PropType.PT_LONGINT:
                        b.BaseStream.Position += 2; // skip property flags;
                        //Get our data length
                        Int64 longInt = ReadLongInt(ref b);
                        //Add our object to the Dictionary
                        tempData.Add(propertyName, longInt);
                        break;
                    case (byte)PropType.PT_BOOL:
                        b.BaseStream.Position += 6; // skip property flags;
                        //Add our object to the Dictionary
                        tempData.Add(propertyName, ReadBool(ref b));
                        break;
                    case (byte)PropType.PT_REAL:
                        b.BaseStream.Position += 4; // skip property flags;
                        //Get our data length
                        tempDataLength = b.ReadInt16();
                        //Add our object to the Dictionary
                        tempData.Add(propertyName, ReadReal(ref b));
                        break;
                    case (byte)PropType.PT_COLOR:
                        b.BaseStream.Position += 4; // skip property flags;
                        //Get our data length
                        tempDataLength = b.ReadInt16();
                        //Get our float data
                        LTVector tempCol = ReadLTVector(ref b);
                        //Add our object to the Dictionary
                        tempData.Add(propertyName, tempCol);
                        break;
                }
            }

            tempObject.options = tempData;

            temp.obj.Add(tempObject);            
        }
        return temp;
    }

}


