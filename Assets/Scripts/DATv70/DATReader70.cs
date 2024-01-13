using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SFB;
using TMPro;
using static LithFAQ.LTTypes;
using static LithFAQ.LTUtils;
using UnityToolbag;
using static Utility.MaterialSafeMeshCombine;
using System.Linq;
using static DTX;
using System;

namespace LithFAQ
{
    public class DATReader70 : MonoBehaviour, IDATReader
    {
        [SerializeField]
        public DTX.DTXMaterial dtxMaterialList = new DTX.DTXMaterial();

        public WorldObjects LTGameObjects = new WorldObjects();
        WorldReader worldReader = new WorldReader();
        List<WorldBsp> bspListTest = new List<WorldBsp>();

        public float scale = 0.01f; //default scale to fit in Unity's world.

        public Importer importer;

        public void Start()
        {
            importer = GetComponent<Importer>();
            gameObject.AddComponent<Dispatcher>();
        }

        public void ClearLevel()
        {
            //reset loading text
            importer.loadingUI.text = "LOADING...";

            GameObject go = GameObject.Find("Level");

            //destroy all Meshes under the Level object
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                DestroyImmediate(meshFilter.sharedMesh);
            }

            foreach (Transform child in go.transform)
            {
                Destroy(child.gameObject);
            }

            go = GameObject.Find("objects");

            foreach (Transform child in go.transform)
            {
                Destroy(child.gameObject);
            }

            //find all objects named New Game Object
            GameObject[] newGameObjects = GameObject.FindObjectsOfType<GameObject>();

            //TODO: FIX THE ISSUE WHERE NEW GAMES OBJECTS ARE BEING CREATED IN ROOT OF SCENE.
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

            //reset UI
            Controller lightController = GetComponent<Controller>();

            foreach (var toggle in lightController.settingsToggleList)
            {
                toggle.isOn = true;

                if (toggle.name == "Shadows")
                    toggle.isOn = false;
            }
        }

        public void Load(BinaryReader b)
        {
            importer = gameObject.GetComponent<Importer>();

            ClearLevel();
            StartCoroutine("LoadLevel", b);
        }

        public IEnumerator LoadLevel(BinaryReader b)
        {
            Debug.Log("Loading Level...");
            //yield return new WaitForEndOfFrame();
            importer.loadingUI.enabled = true;
            yield return new WaitForEndOfFrame();

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

            for (int i = 0; i < WMList.nNumModels; i++)
            {
                //Debug.Log("Current Position: " + b.BaseStream.Position);
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
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }

                b.BaseStream.Position = nDummy;
            }

            b.BaseStream.Position = worldReader.WorldHeader.dwObjectDataPos;
            LoadObjects(ref b);

            importer.infoBox.text = string.Format("Loaded World: {0}", Path.GetFileName(importer.fileName));

            b.BaseStream.Close();

            importer.loadingUI.text = "Loading Objects";
            yield return new WaitForEndOfFrame();

            int id = 0;
            foreach (WorldBsp tBSP in bspListTest)
            {
                if (tBSP.m_szWorldName.Contains("PhysicsBSP"))
                {
                    importer.loadingUI.text = "Loading BSP";
                    yield return new WaitForEndOfFrame();
                }

                if (tBSP.m_szWorldName != "VisBSP")
                {
                    GameObject mainObject = new GameObject(tBSP.WorldName);
                    mainObject.transform.parent = this.transform;
                    mainObject.AddComponent<MeshFilter>();
                    mainObject.AddComponent<MeshRenderer>().material = importer.defaultMaterial;

                    if (tBSP.m_aszTextureNames[0].Contains("Invisible.dtx") && tBSP.WorldName != "PhysicsBSP" ||
                       tBSP.m_aszTextureNames[0].Contains("Sky.dtx"))
                    {
                        mainObject.tag = "Blocker";
                    }

                    if (tBSP.m_aszTextureNames[0].Contains("AI.dtx") ||
                        tBSP.m_szWorldName.Contains("Volume") ||
                        tBSP.m_szWorldName.Contains("Water") ||
                        tBSP.m_szWorldName.Contains("Rain") ||
                        tBSP.m_szWorldName.Contains("rain") ||
                        tBSP.m_szWorldName.Contains("weather") ||
                        tBSP.m_szWorldName.Contains("Weather") ||
                        tBSP.m_szWorldName.Contains("Ladder"))
                    {
                        mainObject.tag = "Volumes";
                    }

                    LoadTexturesForBSP(tBSP);

                    foreach (WorldPoly tPoly in tBSP.m_pPolies)
                    {

                        float texWidth = 256f;
                        float texHeight = 256f;

                        string szTextureName = Path.GetFileName(tBSP.m_aszTextureNames[tBSP.m_pSurfaces[tPoly.m_nSurface].m_nTexture]);

                        if (szTextureName.Contains("sky", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        SetLithTechInternalTextureSize(ref texWidth, ref texHeight, szTextureName);

                        //Convert OPQ to UV magic
                        Vector3 center = tPoly.m_vCenter;

                        Vector3 o = tPoly.m_O;
                        Vector3 p = tPoly.m_P;
                        Vector3 q = tPoly.m_Q;

                        o *= scale;
                        o -= (Vector3)tPoly.m_vCenter;
                        p /= scale;
                        q /= scale;

                        Material matReference = importer.defaultMaterial;

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
                            List<Vector3> vertexList = new List<Vector3>();
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


                                Vector3 data = tVertex.m_vData;
                                data *= scale;
                                vertexList.Add(data);

                                Color color = new Color(tPoly.m_aVertexColorList[vCount].red / 255, tPoly.m_aVertexColorList[vCount].green / 255, tPoly.m_aVertexColorList[vCount].blue / 255, 1.0f);
                                _aVertexColorList.Add(color);

                                _aVertexNormalList.Add(tBSP.m_pPlanes[tPoly.m_nPlane].m_vNormal);

                                // Calculate UV coordinates based on the OPQ vectors
                                // Note that since the worlds are offset from 0,0,0 sometimes we need to subtract the center point
                                Vector3 curVert = vertexList[vCount];
                                float u = Vector3.Dot((curVert - center) - o, p);
                                float v = Vector3.Dot((curVert - center) - o, q);

                                //Scale back down into something more sane
                                u /= texWidth;
                                v /= texHeight;

                                _aUVList.Add(new Vector2(u, v));
                            }

                            m.SetVertices(vertexList);
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
                            mf.mesh = m;

                            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                        }
                        id++;
                    }
                }
                yield return null;
            }

            //find reflection probe and update it
            var reflectionProbe = GameObject.Find("Main Camera").GetComponent<ReflectionProbe>().RenderProbe();

            importer.loadingUI.text = "Combining Meshes";
            yield return new WaitForEndOfFrame();

            var g = GameObject.Find("PhysicsBSP");
            Mesh[] meshes = g.GetComponentsInChildren<MeshFilter>().Select(mf => mf.sharedMesh).ToArray();

            g.MeshCombine(true);

            //after mesh combine, we need to recalculate the normals
            MeshFilter[] meshFilters = g.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in meshFilters)
            {
                
                mf.mesh.Optimize();
                mf.mesh.RecalculateNormals();
                mf.mesh.RecalculateTangents();
            }


            var twmToAdd = GameObject.Find("Level");
            foreach (var t in twmToAdd.gameObject.GetComponentsInChildren<MeshFilter>())
            {
                var mc = t.transform.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = t.mesh;
            }

            foreach (var t in twmToAdd.gameObject.GetComponentsInChildren<MeshRenderer>())
            {
                t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            }

            //Loop through all objects under Level and add MeshFilter to a list
            List<GameObject> toBatch = new List<GameObject>();

            foreach (Transform child in GameObject.Find("Level").transform)
            {
                if (child.gameObject.GetComponent<MeshFilter>() != null)
                {
                    toBatch.Add(child.gameObject);
                }
            }

            importer.loadingUI.enabled = false;
            yield return new WaitForEndOfFrame();

            //Batch all the objects
            StaticBatchingUtility.Combine(toBatch.ToArray(), GameObject.Find("Level"));

            yield return new WaitForEndOfFrame();
        }

        private void LoadTexturesForBSP(WorldBsp tBSP)
        {
            //Load texture
            foreach (var tex in tBSP.m_aszTextureNames)
            {
                DTX.LoadDTX(importer.projectPath + "\\" + tex, ref dtxMaterialList, importer.projectPath);
            }
        }

        private void SetLithTechInternalTextureSize(ref float texWidth, ref float texHeight, string szTextureName)
        {
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
        }

        public void LoadObjects(ref BinaryReader b)
        {

            LTGameObjects = ReadObjects(ref b);

            foreach (var obj in LTGameObjects.obj)
            {
                Vector3 objectPos = new Vector3();
                Quaternion objectRot = new Quaternion();
                Vector3 rot = new Vector3();
                String objectName = String.Empty;
                bool bInvisible = false;
                bool bChromakey = false;

                foreach (var subItem in obj.options)
                {
                    if (subItem.Key == "Name")
                        objectName = (String)subItem.Value;

                    else if (subItem.Key == "Pos")
                    {
                        LTVector temp = (LTVector)subItem.Value;
                        objectPos = new Vector3(temp.X, temp.Y, temp.Z) * 0.01f;
                    }

                    else if (subItem.Key == "Rotation")
                    {
                        LTRotation temp = (LTRotation)subItem.Value;
                        rot = new Vector3(temp.X * Mathf.Rad2Deg, temp.Y * Mathf.Rad2Deg, temp.Z * Mathf.Rad2Deg);
                    }

                }

                var tempObject = Instantiate(importer.prefab, objectPos, objectRot);
                tempObject.name = objectName + "_obj";
                tempObject.transform.eulerAngles = rot;

                if (obj.objectName == "GameStartPoint")
                    tempObject.AddComponent<GameStartPointEditor>();

                if (obj.objectName == "TranslucentWorldModel")
                {
                    string szObjectName = String.Empty;
                    foreach (var subItem in obj.options)
                    {
                        if (subItem.Key == "Visible")
                            bInvisible = (bool)subItem.Value;

                        else if (subItem.Key == "Chromakey")
                            bChromakey = (bool)subItem.Value;
                        else if (subItem.Key == "Name")
                            szObjectName = (String)subItem.Value;
                    }

                    var twm = tempObject.AddComponent<TranslucentWorldModel>();
                    twm.bChromakey = bChromakey;
                    twm.bVisible = bInvisible;
                    twm.szName = szObjectName;
                }


                if (obj.objectName == "Light")
                {
                    var light = tempObject.gameObject.AddComponent<Light>();

                    foreach (var subItem in obj.options)
                    {
                        if (subItem.Key == "LightRadius")
                            light.range = (float)subItem.Value * 0.20f;

                        else if (subItem.Key == "LightColor")
                        {
                            var vec = (LTVector)subItem.Value;
                            Vector3 col = Vector3.Normalize(new Vector3(vec.X, vec.Y, vec.Z));
                            light.color = new Color(col.x, col.y, col.z);
                        }

                        else if (subItem.Key == "BrightScale")
                            light.intensity = (float)subItem.Value * 0.75f;
                    }
                    light.shadows = LightShadows.Soft;

                    Controller lightController = transform.GetComponent<Controller>();

                    foreach (var toggle in lightController.settingsToggleList)
                    {
                        if (toggle.name == "Shadows")
                        {
                            if (toggle.isOn)
                                light.shadows = LightShadows.Soft;
                            else
                                light.shadows = LightShadows.None;
                        }
                    }
                }

                if (obj.objectName == "DirLight")
                {
                    var light = tempObject.gameObject.AddComponent<Light>();


                    foreach (var subItem in obj.options)
                    {
                        if (subItem.Key == "FOV")
                            light.spotAngle = (float)subItem.Value;

                        else if (subItem.Key == "LightRadius")
                            light.range = (float)subItem.Value * 0.025f;

                        else if (subItem.Key == "InnerColor")
                        {
                            var vec = (LTVector)subItem.Value;
                            Vector3 col = Vector3.Normalize(new Vector3(vec.X, vec.Y, vec.Z));
                            light.color = new Color(col.x, col.y, col.z);
                        }

                        else if (subItem.Key == "BrightScale")
                            light.intensity = (float)subItem.Value * 0.65f;
                    }

                    light.shadows = LightShadows.Soft;
                    light.type = LightType.Spot;

                    Controller lightController = GetComponent<Controller>();

                    foreach (var toggle in lightController.settingsToggleList)
                    {
                        if (toggle.name == "Shadows")
                        {
                            if (toggle.isOn)
                                light.shadows = LightShadows.Soft;
                            else
                                light.shadows = LightShadows.None;
                        }
                    }
                }

                if (obj.objectName == "StaticSunLight")
                {
                    var light = tempObject.gameObject.AddComponent<Light>();

                    foreach (var subItem in obj.options)
                    {
                        if (subItem.Key == "InnerColor")
                        {
                            var vec = (LTVector)subItem.Value;
                            Vector3 col = Vector3.Normalize(new Vector3(vec.X, vec.Y, vec.Z));
                            light.color = new Color(col.x, col.y, col.z);
                        }
                        else if (subItem.Key == "BrightScale")
                            light.intensity = (float)subItem.Value * 0.65f;
                    }

                    light.shadows = LightShadows.Soft;
                    light.type = LightType.Directional;

                    Controller lightController = GetComponent<Controller>();

                    foreach (var toggle in lightController.settingsToggleList)
                    {
                        if (toggle.name == "Shadows")
                        {
                            if (toggle.isOn)
                                light.shadows = LightShadows.Soft;
                            else
                                light.shadows = LightShadows.None;
                        }
                    }
                }

                var g = GameObject.Find("objects");
                tempObject.transform.SetParent(g.transform);
                g.transform.localScale = Vector3.one;


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
            if (worldReader.WorldProperties != null)
            {
                var worldPropertiesArray = worldReader.WorldProperties.Split(';');

                foreach (var property in worldPropertiesArray)
                {
                    if (property.Contains("AmbientLight"))
                    {

                        string property2 = String.Empty;
                        int ambPos = property.IndexOf("AmbientLight");
                        int startPos = property.IndexOf(" ");

                        int endPos = property.IndexOf(";");

                        if (endPos == -1)
                            endPos = property.Length;

                        property2 = property;
                        if (startPos == 0)
                        {
                            property2 = property.Substring(startPos + 1, endPos - 1 - startPos);
                            startPos = property2.IndexOf(" ");
                            endPos = property2.Length;
                        }

                        var szTemp = property2.Substring(startPos + 1, endPos - 1 - startPos);

                        var splitStrings = szTemp.Split(' ');

                        Vector3 vAmbientRGB = Vector3.Normalize(new Vector3(
                            float.Parse(splitStrings[0]),
                            float.Parse(splitStrings[1]),
                            float.Parse(splitStrings[2])
                        ));

                        var color = new Color(vAmbientRGB.x, vAmbientRGB.y, vAmbientRGB.y, 255);
                        RenderSettings.ambientLight = color;
                        importer.defaultColor = color;

                        //Check if color is 0,0,0 and boost a bit
                        if (color.r == 0 && color.g == 0 && color.b == 0)
                        {
                            RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.1f, 255);
                            importer.defaultColor = new Color(0.1f, 0.1f, 0.1f, 255);
                        }
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                        RenderSettings.ambientIntensity = 1.0f;
                    }
                    else
                    {
                        importer.defaultColor = new Color(0.1f, 0.1f, 0.1f, 255);
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                        RenderSettings.ambientIntensity = 1.0f;
                    }
                }
            }
        }
        public void Quit()
        {
            Application.Quit();
        }


        public static WorldObjects ReadObjects(ref BinaryReader b)
        {
            WorldObjects temp = new WorldObjects();
            temp.obj = new List<WorldObject>();

            var totalObjectCount = b.ReadInt32();

            for (int i = 0; i < totalObjectCount; i++)
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

                for (int t = 0; t < tempObject.objectEntries; t++)
                {

                    var tempDataLength = b.ReadInt16();
                    string propertyName = ReadString(tempDataLength, ref b);

                    byte propType = b.ReadByte();

                    switch (propType)
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
}