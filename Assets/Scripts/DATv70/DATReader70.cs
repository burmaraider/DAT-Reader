using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SFB;
using TMPro;
using static LTTypes.LTTypes;
using System.Threading;
using UnityToolbag;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class DATReader70 : MonoBehaviour
{
    [SerializeField]
    public DTX.DTXMaterial dtxMaterialList = new DTX.DTXMaterial();
    [SerializeField]
    public Material defaultMaterial;

    public UnityEngine.UI.Text infoBox;
    public UnityEngine.UI.Text loadingUI;
    public GameObject prefab;
    public WorldObjects LTGameObjects = new WorldObjects();
    WorldReader worldReader = new WorldReader();
    List<WorldBsp> bspListTest = new List<WorldBsp>();

    public void Start()
    {
        gameObject.AddComponent<Dispatcher>();
    }

    public void ClearLevel()
    {
        var go = GameObject.Find("Level");
        go.transform.localScale = new Vector3(1, 1, 1);

        foreach (Transform child in go.transform)
        {
            Destroy(child.gameObject);
        }

        go = GameObject.Find("objects");
        
        foreach(Transform child in go.transform)
        {
            Destroy(child.gameObject);
        }

        //find all objects named New Game Object
        GameObject[] newGameObjects = GameObject.FindObjectsOfType<GameObject>();

        // List or use the found objects
        foreach (var obj in newGameObjects)
        {
            if (obj.name == "New Game Object")
            {
                // Do something with the object
                Destroy(obj);
            }
        }

        worldReader = new WorldReader();
        bspListTest = new List<WorldBsp>();
        LTGameObjects = new WorldObjects();
        foreach (Texture2D tex in dtxMaterialList.textures.Values)
        {
            DestroyImmediate(tex);
        }
        foreach (Material mat in dtxMaterialList.materials.Values)
        {
            DestroyImmediate(mat);
        }

        dtxMaterialList = null;
        dtxMaterialList = new DTX.DTXMaterial();

        Resources.UnloadUnusedAssets();

    }

    public void LoadLevelDialog()
    {
        //clear out everything
        ClearLevel();
        loadingUI.enabled = true;

        var extensions = new [] {
                new ExtensionFilter("Lithtech World DAT", "dat" )
            };
        // Open file
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);

        if (paths.Length > 0)
        {
            Array.Resize(ref paths, 2);
            var texPath = StandaloneFileBrowser.OpenFolderPanel("Open Texture Path", paths[0], false);
            if (paths.Length > 0)
            {
                paths[1] = texPath[0];
            }
        }

        if (paths.Length > 0)
            StartCoroutine("LoadLevel", paths);
            //LoadLevel(paths[0]);
    }
    
    private IEnumerator LoadLevel(String[] szFileName)
    {
        yield return new WaitForEndOfFrame();
        loadingUI.enabled = true;
        yield return new WaitForEndOfFrame();

        BinaryReader b = new BinaryReader(File.Open(szFileName[0], FileMode.Open));

        worldReader.ReadHeader(ref b);
        worldReader.ReadPropertiesAndExtents(ref b);

        WorldTree wTree = new WorldTree();

        wTree.ReadWorldTree(ref b);

        //read world models...
        byte[] anDummy = new byte[32];
        String szDump = String.Empty;
        int nDummy = 0;

        WorldData pWorldData = new WorldData();
        WorldBsp pWorldBSP = new WorldBsp();
        List<WorldBsp> pWorldBSP2 = new List<WorldBsp>();

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
            tBSP.datVersion = worldReader.WorldHeader.nVersion;
            
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

        var actualFileName = szFileName[0].Split('\\');
        infoBox.text = string.Format("Loaded World: {0}", actualFileName[actualFileName.Length-1]);

        b.BaseStream.Close();


        List<Vector3> vertexList = new List<Vector3>();
        List<Vector3> vertexList2 = new List<Vector3>();

        
        foreach(WorldBsp tBSP in bspListTest)
        {
            Debug.Log("Generating Mesh for: " + tBSP.m_szWorldName);
            if(tBSP.m_szWorldName != "VisBSP")
            {
                GameObject mainObject = new GameObject(tBSP.WorldName);
                mainObject.transform.parent = this.transform;
                mainObject.AddComponent<MeshFilter>();
                mainObject.AddComponent<MeshRenderer>().material = defaultMaterial;

                if(tBSP.m_aszTextureNames[0].Contains("Invisible.dtx") && tBSP.WorldName != "PhysicsBSP" ||
                   tBSP.m_aszTextureNames[0].Contains("Sky.dtx"))
                {
                    mainObject.tag = "Blocker";
                }

                if(tBSP.m_aszTextureNames[0].Contains("AI.dtx") || 
                    tBSP.m_szWorldName.Contains("Volume")||
                    tBSP.m_szWorldName.Contains("Water") ||
                    tBSP.m_szWorldName.Contains("Rain") ||
                    tBSP.m_szWorldName.Contains("rain") ||
                    tBSP.m_szWorldName.Contains("weather") ||
                    tBSP.m_szWorldName.Contains("Weather") ||
                    tBSP.m_szWorldName.Contains("Ladder"))
                {
                    mainObject.tag = "Volumes";
                }
                //Load texture
                foreach (var tex in tBSP.m_aszTextureNames)
                {
                    if (Path.GetFileName(tex).Contains("mtl106b"))
                    {
                        Debug.Log(Path.GetFileName(tex));
                    }
                    DTX.LoadDTX(szFileName[1] + "\\" + tex, ref dtxMaterialList);
                }

                int id = 0;
                foreach (WorldPoly tPoly in tBSP.m_pPolies)
                {
                    
                    //Set our defaults;
                    float texWidth = 256f;
                    float texHeight = 256f;

                    string szTextureName = Path.GetFileName(tBSP.m_aszTextureNames[tBSP.m_pSurfaces[tPoly.m_nSurface].m_nTexture]);

                    if(id == 1915)
                    {
                        Debug.Log("do the thing");
                    }

                    //Lookup the width and height the engine uses to calculate UV's
                    //UI Mipmap Offset changes this
                    foreach (var mats in dtxMaterialList.materials.Keys)
                    {
                       
                        if (mats.Contains(szTextureName))
                        {
                            texWidth = dtxMaterialList.texSize[szTextureName].engineWidth;
                            texHeight = dtxMaterialList.texSize[szTextureName].engineHeight;
                        }
                    }

                    //Convert OPQ to UV magic
                    Vector3 o = new Vector3();
                    Vector3 p = new Vector3();
                    Vector3 q = new Vector3();
                    Vector3 center;
                    float scale = 1.0f;

                    center = new Vector3(tPoly.m_vCenter.X, tPoly.m_vCenter.Y, tPoly.m_vCenter.Z);

                    o = new Vector3(tPoly.m_O.X, tPoly.m_O.Y, tPoly.m_O.Z);
                    p = new Vector3(tPoly.m_P.X, tPoly.m_P.Y, tPoly.m_P.Z);
                    q = new Vector3(tPoly.m_Q.X, tPoly.m_Q.Y, tPoly.m_Q.Z);

                    o *= scale;
                    o -= center;
                    p /= scale;
                    q /= scale;

                    Material matReference = defaultMaterial;


                        foreach (var mats in dtxMaterialList.materials.Keys)
                        {
                            if (mats.Contains(szTextureName))
                            {
                                matReference = dtxMaterialList.materials[szTextureName];
                            }
                        }


                    // CALCULATE EACH TRI INDIVIDUALLY.
                    for (int nTriIndex = 0; nTriIndex < tPoly.m_nLoVerts - 2; nTriIndex++)
                    {
                        List<Vector3> _aVertexList = new List<Vector3>();
                        List<Vector3> _aVertexNormalList = new List<Vector3>();
                        List<Color> _aVertexColorList = new List<Color>();
                        List<Vector2> _aUVList = new List<Vector2>();
                        List<int> _aTriangleIndices = new List<int>();

                        GameObject go = new GameObject(tBSP.WorldName + id);
                        go.transform.parent = mainObject.transform;
                        MeshRenderer mr = go.AddComponent<MeshRenderer>();
                        MeshFilter mf = go.AddComponent<MeshFilter>();

                        Mesh m = new Mesh();

                        // Do the thing
                        for (int vCount = 0; vCount < tPoly.m_nLoVerts; vCount++)
                        {
                            WorldVertex tVertex = tBSP.m_pPoints[tPoly.m_aVertexColorList[vCount].nVerts];


                            Vector3 data = new Vector3(tVertex.m_vData.X, tVertex.m_vData.Y, tVertex.m_vData.Z);
                            _aVertexList.Add(data);

                            Color color = new Color(tPoly.m_aVertexColorList[vCount].red / 255, tPoly.m_aVertexColorList[vCount].green / 255, tPoly.m_aVertexColorList[vCount].blue / 255, 1.0f);
                            _aVertexColorList.Add(color);

                            _aVertexNormalList.Add(new Vector3(
                                tBSP.m_pPlanes[tPoly.m_nPlane].m_vNormal.X,
                                tBSP.m_pPlanes[tPoly.m_nPlane].m_vNormal.Y,
                                tBSP.m_pPlanes[tPoly.m_nPlane].m_vNormal.Z
                            ));

                            // Calculate UV coordinates based on the OPQ vectors
                            // Note that since the worlds are offset from 0,0,0 sometimes we need to subtract the center point
                            Vector3 curVert = _aVertexList[vCount];
                            float u = Vector3.Dot((curVert - center) - o, p);
                            float v = Vector3.Dot((curVert - center) - o, q);

                            //Scale back down into something more sane
                            u /= texWidth;
                            v /= texHeight;

                            _aUVList.Add(new Vector2(u, v));
                        }

                        m.SetVertices(_aVertexList);
                        m.SetNormals(_aVertexNormalList);
                        m.SetUVs(0, _aUVList);
                        m.SetColors(_aVertexColorList);

                        //Hacky, whatever
                        _aTriangleIndices.Add(0);
                        _aTriangleIndices.Add(nTriIndex + 1);
                        _aTriangleIndices.Add((nTriIndex + 2) % tPoly.m_nLoVerts);

                        // Set triangles
                        m.SetTriangles(_aTriangleIndices, 0);
                        m.RecalculateTangents();

                        mr.material = matReference;
                        mf.mesh = m;

                        if(matReference.name.Contains("invisible") || matReference.name.Contains("Invisible") || matReference.name.Contains("Sky") || matReference.name.Contains("sky") || matReference.name.Contains("ai") || matReference.name.Contains("AI"))
                        {
                            mr.enabled = false;
                        }
                    }
                    id++;
                }

                //combine meshes

                
            }

        }
        yield return new WaitForEndOfFrame();
        loadingUI.enabled = false;
        yield return new WaitForEndOfFrame();
        GameObject.Find("Level").transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

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
                        light.color = new Color(col.x, col.y, col.z);
                    }

                    else if(subItem.Key == "BrightScale")
                        light.intensity = (float)subItem.Value * 0.5f;          
                }
                light.shadows = LightShadows.Soft;
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

                light.shadows = LightShadows.Soft;
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
            newGO.transform.SetParent(tempObject.transform);

            newGO.AddComponent<TextMeshPro>();
            var t = newGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            t.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            t.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
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
            var worldPropertiesArray = worldReader.WorldProperties.Split(';');

            foreach(var property in worldPropertiesArray)
            {
                if(property.Contains("AmbientLight"))
                {

                    string property2 = String.Empty;
                    int ambPos = property.IndexOf("AmbientLight");
                    int startPos = property.IndexOf(" ");
                    
                    int endPos = property.IndexOf(";");

                    if(endPos == -1)
                        endPos = property.Length;

                    property2 = property;
                    if(startPos == 0)
                    {   
                        property2 = property.Substring(startPos+1, endPos-1 - startPos);
                        startPos = property2.IndexOf(" ");
                        endPos = property2.Length;
                    }

                    var szTemp = property2.Substring(startPos+1, endPos-1 - startPos);

                    var splitStrings = szTemp.Split(' ');

                    Vector3 vAmbientRGB = Vector3.Normalize(new Vector3(
                        float.Parse(splitStrings[0]),
                        float.Parse(splitStrings[1]),
                        float.Parse(splitStrings[2])
                    ));

                    var color = new Color(vAmbientRGB.x, vAmbientRGB.y, vAmbientRGB.y, 255);
                    RenderSettings.ambientLight = color;
                    RenderSettings.ambientIntensity = 0.5f;
                }
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

        var totalObjectCount = b.ReadInt32();

        for(int i = 0; i < totalObjectCount; i++)
        {
            //Make a new object
            WorldObject tempObject = new WorldObject();

            //Make a dictionary to make things easier
            Dictionary<string, object> tempData = new Dictionary<string, object>();

            tempObject.dataLength = b.ReadInt16(); // Read our object datalength

            var dataLength = b.ReadInt16(); //read out property length

            tempObject.objectName = ReadString(dataLength, ref b); // read our name

            tempObject.objectEntries = b.ReadInt16();// read how many properties this object has

            b.BaseStream.Position += 2;

            for(int t = 0; t < tempObject.objectEntries; t++)
            {

                var tempDataLength = b.ReadInt16();
                string propertyName = ReadString(tempDataLength, ref b);

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


