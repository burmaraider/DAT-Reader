using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityToolbag;
using static LithFAQ.LTTypes;
using static LithFAQ.LTUtils;
using static Utility.MaterialSafeMeshCombine;
using static DTX;
using TMPro;
using System.Xml.Linq;
using UnityEngine.Profiling;



namespace LithFAQ
{
    public class DATReader66 : MonoBehaviour, IDATReader
    {
        public WorldObjects LTGameObjects = new WorldObjects();
        WorldReader worldReader = new WorldReader();
        List<WorldBsp> bspListTest = new List<WorldBsp>();
        public List<WorldObject> WorldObjectList = new List<WorldObject>();

        public float UNITYSCALEFACTOR = 0.01f; //default scale to fit in Unity's world.
        public Importer importer;

        public ABCModelReader abc;


        public void OnEnable()
        {
            UIActionManager.OnPreClearLevel += ClearLevel;
        }

        public void OnDisable()
        {
            UIActionManager.OnPreClearLevel -= ClearLevel;
        }

        public void Start()
        {
            importer = GetComponent<Importer>();
            gameObject.AddComponent<Dispatcher>();

            abc = gameObject.AddComponent<ABCModelReader>();
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

            go = GameObject.Find("Models");

            foreach (Transform child in go.transform)
            {
                foreach (MeshFilter meshFilter in child.GetComponentsInChildren<MeshFilter>())
                {
                    DestroyImmediate(meshFilter.sharedMesh);
                }

                Destroy(child.gameObject);
            }


            worldReader = new WorldReader();
            bspListTest = new List<WorldBsp>();
            LTGameObjects = new WorldObjects();

            foreach (Texture2D tex in importer.dtxMaterialList.textures.Values)
            {
                DestroyImmediate(tex);
            }
            foreach (Material mat in importer.dtxMaterialList.materials.Values)
            {
                DestroyImmediate(mat);
            }

            importer.dtxMaterialList = null;
            importer.dtxMaterialList = new DTX.DTXMaterial();

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

            LoadLevel(b);

        }

        public async void LoadLevel(BinaryReader b)
        {
            importer.loadingUI.enabled = true;
            await System.Threading.Tasks.Task.Yield();

            worldReader.ReadHeader(ref b);
            worldReader.ReadPropertiesAndExtents(ref b);

            WorldTree wTree = new WorldTree();

            wTree.ReadWorldTree(ref b);

            //read world models...
            byte[] anDummy = new byte[32];
            int nNextWMPosition = 0;

            WorldData pWorldData = new WorldData();

            WorldModelList WMList = new WorldModelList();
            WMList.pModelList = new List<WorldData>();
            WMList.nNumModels = b.ReadInt32();

            for (int i = 0; i < WMList.nNumModels; i++)
            {
                nNextWMPosition = b.ReadInt32();
                anDummy = b.ReadBytes(anDummy.Length);

                pWorldData.NextPos = nNextWMPosition;
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

                b.BaseStream.Position = nNextWMPosition;
            }

            b.BaseStream.Position = worldReader.WorldHeader.dwObjectDataPos;
            LoadObjects(ref b);

            importer.infoBox.text = string.Format("Loaded World: {0}", Path.GetFileName(importer.szFileName));

            b.BaseStream.Close();

            importer.loadingUI.text = "Loading Objects";
            await System.Threading.Tasks.Task.Yield();

            int id = 0;
            foreach (WorldBsp tBSP in bspListTest)
            {

                if (tBSP.m_szWorldName.Contains("PhysicsBSP"))
                {
                    importer.loadingUI.text = "Loading BSP";
                    await System.Threading.Tasks.Task.Yield();
                }

                if (tBSP.m_szWorldName != "VisBSP")
                {
                    bool isPartOfObject = !tBSP.m_szWorldName.Contains("PhysicsBSP");

                    GameObject mainObject = new GameObject(tBSP.WorldName);
                    mainObject.transform.parent = this.transform;
                    mainObject.AddComponent<MeshFilter>();
                    mainObject.AddComponent<MeshRenderer>().material = importer.defaultMaterial;

                    if (tBSP.m_aszTextureNames[0].Contains("AI.dtx", StringComparison.OrdinalIgnoreCase) ||
                        tBSP.m_aszTextureNames[0].Contains("sound.dtx", StringComparison.OrdinalIgnoreCase) ||
                        tBSP.m_szWorldName.Contains("volume", StringComparison.OrdinalIgnoreCase) ||
                        tBSP.m_szWorldName.Contains("Wwater") ||
                        tBSP.m_szWorldName.Contains("weather", StringComparison.OrdinalIgnoreCase) ||
                        tBSP.m_szWorldName.Contains("rain", StringComparison.OrdinalIgnoreCase) && !tBSP.m_szWorldName.Contains("terrain", StringComparison.OrdinalIgnoreCase) ||
                        tBSP.m_szWorldName.Contains("poison", StringComparison.OrdinalIgnoreCase) ||
                        tBSP.m_szWorldName.Contains("corrosive", StringComparison.OrdinalIgnoreCase) ||
                        tBSP.m_szWorldName.Contains("ladder", StringComparison.OrdinalIgnoreCase)
                        )
                    {
                        mainObject.tag = "Volumes";
                    }

                    LoadTexturesForBSP(tBSP);
                    

                    foreach (WorldPoly tPoly in tBSP.m_pPolies)
                    {

                        //remove all bsp invisible
                        if (tBSP.m_aszTextureNames[tPoly.GetSurface(tBSP).m_nTexture].Contains("Invisible.dtx", StringComparison.OrdinalIgnoreCase) ||
                            tBSP.m_aszTextureNames[tPoly.GetSurface(tBSP).m_nTexture].Contains("Sky.dtx", StringComparison.OrdinalIgnoreCase) ||
                            tBSP.m_aszTextureNames[tPoly.GetSurface(tBSP).m_nTexture].Contains("Rain.dtx", StringComparison.OrdinalIgnoreCase) ||
                            tBSP.m_aszTextureNames[tPoly.GetSurface(tBSP).m_nTexture].Contains("hull.dtx", StringComparison.OrdinalIgnoreCase) ||
                            tBSP.m_aszTextureNames[tPoly.GetSurface(tBSP).m_nTexture].Contains("occluder.dtx", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        float texWidth = 256f;
                        float texHeight = 256f;

                        string szTextureName = Path.GetFileName(tBSP.m_aszTextureNames[tBSP.m_pSurfaces[tPoly.m_nSurface].m_nTexture]);

                        //skip sky portals
                        if ((tPoly.GetSurface(tBSP).m_nFlags & (int)BitMask.SKY) == (int)BitMask.SKY)
                        {
                            continue;
                        }

                        SetLithTechInternalTextureSize(ref texWidth, ref texHeight, szTextureName);

                        //Convert OPQ to UV magic
                        Vector3 center = tPoly.m_vCenter;

                        //Vector3 O = 

                        Vector3 o = tPoly.GetSurface(tBSP).m_fUV1;
                        Vector3 p = tPoly.GetSurface(tBSP).m_fUV2;
                        Vector3 q = tPoly.GetSurface(tBSP).m_fUV3;

                        o *= UNITYSCALEFACTOR;
                        o -= (Vector3)tPoly.m_vCenter;
                        p /= UNITYSCALEFACTOR;
                        q /= UNITYSCALEFACTOR;

                        Material matReference = importer.defaultMaterial;

                        if (importer.dtxMaterialList.materials.TryGetValue(szTextureName, out var material))
                        {
                            matReference = material;
                        }

                        var possibleTWM = GameObject.Find(tBSP.WorldName + "_obj");

                        if (possibleTWM)
                        {

                            if (szTextureName.Contains("invisible", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            var twm = possibleTWM.GetComponent<TranslucentWorldModel>();
                            if (twm)
                            {
                                if (twm.bChromakey || (tPoly.GetSurface(tBSP).m_nFlags & (int)BitMask.TRANSLUCENT) == (int)BitMask.TRANSLUCENT)
                                {
                                    //try to find already existing material
                                    if (importer.dtxMaterialList.materials.ContainsKey(matReference.name + "_Chromakey"))
                                    {
                                        matReference = importer.dtxMaterialList.materials[matReference.name + "_Chromakey"];
                                    }
                                    else
                                    {
                                        //copy material from matReference to a new
                                        Material mat = new Material(Shader.Find("Shader Graphs/Lithtech Vertex Transparent"));
                                        mat.name = matReference.name + "_Chromakey";
                                        mat.mainTexture = matReference.mainTexture;
                                        mat.SetInt("_Chromakey", 1);
                                        matReference = mat;
                                        AddMaterialToMaterialDictionary(mat.name, mat, importer.dtxMaterialList);
                                    }
                                }

                                if ((tPoly.GetSurface(tBSP).m_nFlags & (int)BitMask.INVISIBLE) == (int)BitMask.INVISIBLE)
                                {
                                    mainObject.tag = "Blocker";
                                }
                                if (!twm.bVisible)
                                {
                                    mainObject.tag = "Blocker";
                                }

                            }
                        }

                        // CALCULATE EACH TRI INDIVIDUALLY.
                        // CALCULATE EACH TRI INDIVIDUALLY.
                        for (int nTriIndex = 0; nTriIndex < tPoly.m_nLoVerts - 2; nTriIndex++)
                        {
                            Vector3[] vertexList = new Vector3[tPoly.m_nLoVerts];
                            Vector3[] _aVertexNormalList = new Vector3[tPoly.m_nLoVerts];
                            Color[] _aVertexColorList = new Color[tPoly.m_nLoVerts];
                            Vector2[] _aUVList = new Vector2[tPoly.m_nLoVerts];
                            int[] _aTriangleIndices = new int[3];

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
                                data *= UNITYSCALEFACTOR;
                                vertexList[vCount] = data;

                                Color color = new Color(tPoly.m_aVertexColorList[vCount].red / 255, tPoly.m_aVertexColorList[vCount].green / 255, tPoly.m_aVertexColorList[vCount].blue / 255, 1.0f);
                                _aVertexColorList[vCount] = color;

                                _aVertexNormalList[vCount] = tBSP.m_pPlanes[tPoly.m_nPlane].m_vNormal;

                                // Calculate UV coordinates based on the OPQ vectors
                                // Note that since the worlds are offset from 0,0,0 sometimes we need to subtract the center point
                                Vector3 curVert = vertexList[vCount];
                                float u = Vector3.Dot((curVert - center) - o, p);
                                float v = Vector3.Dot((curVert - center) - o, q);

                                //Scale back down into something more sane
                                u /= texWidth;
                                v /= texHeight;

                                _aUVList[vCount] = new Vector2(u, v);
                            }

                            m.SetVertices(vertexList);
                            m.SetNormals(_aVertexNormalList);
                            m.SetUVs(0, _aUVList);
                            m.SetColors(_aVertexColorList);

                            //Hacky, whatever
                            _aTriangleIndices[0] = 0;
                            _aTriangleIndices[1] = nTriIndex + 1;
                            _aTriangleIndices[2] = (nTriIndex + 2) % tPoly.m_nLoVerts;

                            // Set triangles
                            m.SetTriangles(_aTriangleIndices, 0);
                            m.RecalculateTangents();

                            mr.material = matReference;
                            mf.mesh = m;

                            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                        }

                        id++;
                    }
                }
            }

            importer.loadingUI.text = "Combining Meshes";
            await System.Threading.Tasks.Task.Yield();

            //combine all meshes not named PhysicsBSP
            foreach (var t in GameObject.Find("Level").gameObject.GetComponentsInChildren<MeshFilter>())
            {
                if (t.transform.gameObject.name != "PhysicsBSP")
                {
                    t.gameObject.MeshCombine(true);
                }
            }


            var gPhysicsBSP = GameObject.Find("PhysicsBSP");
            gPhysicsBSP.MeshCombine(true);

            //after mesh combine, we need to recalculate the normals
            MeshFilter[] meshFilters = gPhysicsBSP.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in meshFilters)
            {
                //mf.mesh.Optimize();
                mf.mesh.RecalculateNormals();
                mf.mesh.RecalculateTangents();
            }

            //Assign the mesh collider to the combined meshes
            var gLevelRoot = GameObject.Find("Level");
            foreach (var t in gLevelRoot.gameObject.GetComponentsInChildren<MeshFilter>())
            {
                var mc = t.transform.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = t.mesh;
            }

            //Clip light from behind walls
            foreach (var t in gLevelRoot.gameObject.GetComponentsInChildren<MeshRenderer>())
            {
                t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            }

            //Loop through all objects under Level and add MeshFilter to a list so we can batch
            List<GameObject> toBatch = new List<GameObject>();

            foreach (Transform child in gLevelRoot.transform)
            {
                if (child.gameObject.GetComponent<MeshFilter>() != null)
                {
                    toBatch.Add(child.gameObject);
                }
            }

            importer.loadingUI.enabled = false;

            //Batch all the objects
           // StaticBatchingUtility.Combine(toBatch.ToArray(), gLevelRoot);
            await System.Threading.Tasks.Task.Yield();
            
            SetupSkyBoxMaterials();
        }

        /// <summary>
        /// Sets up the skybox materials, Lithtech engine games use SkyPointer's to set the index of the skybox objects <br />
        /// We can use Unity's Render Queue to set the order of the skybox objects
        /// </summary>
        private void SetupSkyBoxMaterials()
        {
            Shader shaderUnlitTransparent = Shader.Find("Unlit/Transparent");

            foreach (var item in LTGameObjects.obj)
            {
                if (!item.objectName.Contains("SkyPointer", StringComparison.OrdinalIgnoreCase))
                    continue;

                var twmName = (string)item.options["SkyObjectName"];
                var twmTranslucentWorldModel = GameObject.Find(twmName);

                if (!twmTranslucentWorldModel) continue;

                bool bHasIndex = item.options.ContainsKey("Index");
                float nIndex = 0;
                if (bHasIndex)
                {
                    var nValue = (UInt32)item.options["Index"];
                    var aBytes = BitConverter.GetBytes(nValue);
                    nIndex = BitConverter.ToSingle(aBytes, 0);
                }

                foreach (var mrMeshRenderer in twmTranslucentWorldModel.GetComponentsInChildren<MeshRenderer>())
                {
                    //set layer to 8 which is SkyBox so it doessssn't get rendered by the Main Camera.
                    mrMeshRenderer.gameObject.layer = 8;

                    //Since we combine meshes we need to set the material for each submesh
                    foreach (var mat in mrMeshRenderer.materials)
                    {
                        mat.shader = shaderUnlitTransparent;

                        //set the render queue to 3000 + the index value so the SkyPointer can control which element is drawn first.
                        if (bHasIndex)
                        {
                            mat.renderQueue = (int)nIndex + 3000;
                        }
                    }
                }
            }
        }

        private void LoadTexturesForBSP(WorldBsp tBSP)
        {
            //Load texture
            foreach (var tex in tBSP.m_aszTextureNames)
            {
                DTX.LoadDTX(importer.szProjectPath + "\\" + tex, ref importer.dtxMaterialList, importer.szProjectPath);
            }
        }

        private void SetLithTechInternalTextureSize(ref float texWidth, ref float texHeight, string szTextureName)
        {
            //Lookup the width and height the engine uses to calculate UV's
            //UI Mipmap Offset changes this
            foreach (var mats in importer.dtxMaterialList.materials.Keys)
            {
                if (mats.Contains(szTextureName))
                {
                    texWidth = importer.dtxMaterialList.texSize[szTextureName].engineWidth;
                    texHeight = importer.dtxMaterialList.texSize[szTextureName].engineHeight;
                }
            }
        }

        IEnumerator LoadAndPlay(string uri, AudioSource audioSource)
        {
            bool bIsNotWAV = false;
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error: " + www.error);
                }
                else
                {
                    if (www.downloadHandler.data[20] == 1)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        audioSource.clip = clip;
                        audioSource.Play();
                    }
                    else
                    {
                        bIsNotWAV = true;
                    }
                }
            }
            if (bIsNotWAV)
            {
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
                {
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Debug.LogError("Error: " + www.error);
                    }
                    else
                    {

                            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                            audioSource.clip = clip;
                            audioSource.Play();

                    }
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

                WorldObject thisObject = new WorldObject();

                foreach (var subItem in obj.options)
                {
                    if (subItem.Key == "Name")
                        objectName = (String)subItem.Value;

                    else if (subItem.Key == "Pos")
                    {
                        LTVector temp = (LTVector)subItem.Value;
                        objectPos = new Vector3(temp.X, temp.Y, temp.Z) * UNITYSCALEFACTOR;
                    }

                    else if (subItem.Key == "Rotation")
                    {
                        LTRotation temp = (LTRotation)subItem.Value;
                        rot = new Vector3(temp.X * Mathf.Rad2Deg, temp.Y * Mathf.Rad2Deg, temp.Z * Mathf.Rad2Deg);
                    }

                }

                var tempObject = Instantiate(importer.RuntimeGizmoPrefab, objectPos, objectRot);
                tempObject.name = objectName + "_obj";
                tempObject.transform.eulerAngles = rot;

                if (obj.objectName == "WorldProperties")
                {
                    //find child gameobject named Icon
                    var icon = tempObject.transform.Find("Icon");
                    icon.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Gizmos/worldproperties");
                    icon.gameObject.tag = "NoRayCast";
                    icon.gameObject.layer = 7;
                }

                if (obj.objectName == "SoundFX")
                {
                    //find child gameobject named Icon
                    var icon = tempObject.transform.Find("Icon");
                    icon.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Gizmos/sound");
                    icon.gameObject.tag = "NoRayCast";
                    icon.gameObject.layer = 7;

                    AudioSource temp = tempObject.AddComponent<AudioSource>();
                    var volumeControl = tempObject.AddComponent<Volume2D>();

                    string szFilePath = String.Empty;

                    foreach (var subItem in obj.options)
                    {

                        if (subItem.Key == "Sound")
                        {
                            szFilePath = importer.szProjectPath + "\\" + subItem.Value;
                        }

                        if (subItem.Key == "Loop")
                        {
                            temp.loop = (bool)subItem.Value;
                        }

                        if (subItem.Key == "Ambient")
                        {
                            if ((bool)subItem.Value)
                            {
                                temp.spatialize = false;
                            }
                            else
                            {
                                temp.spatialize = true;
                                temp.spatialBlend = 1.0f;

                            }
                        }

                        if (subItem.Key == "Volume")
                        {
                            float vol = (UInt32)subItem.Value;
                            temp.volume = vol / 100;
                        }
                        if (subItem.Key == "OuterRadius")
                        {
                            float vol = (float)subItem.Value;
                            temp.maxDistance = vol / 75;

                            volumeControl.audioSource = temp;
                            volumeControl.listenerTransform = Camera.main.transform;
                            volumeControl.maxDist = temp.maxDistance;
                        }

                    }
                    StartCoroutine(LoadAndPlay(szFilePath, temp));
                }

                if (obj.objectName == "TranslucentWorldModel" || obj.objectName == "Electricity" || obj.objectName == "Door")
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
                    //find child gameobject named Icon
                    var icon = tempObject.transform.Find("Icon");
                    icon.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Gizmos/light");
                    icon.gameObject.tag = "NoRayCast";
                    icon.gameObject.layer = 7;

                    var light = tempObject.gameObject.AddComponent<Light>();


                    foreach (var subItem in obj.options)
                    {
                        if (subItem.Key == "LightRadius")
                            light.range = (float)subItem.Value * 0.01f;

                        else if (subItem.Key == "LightColor")
                        {
                            var vec = (LTVector)subItem.Value;
                            Vector3 col = Vector3.Normalize(new Vector3(vec.X, vec.Y, vec.Z));
                            light.color = new Color(col.x, col.y, col.z);
                        }

                        else if (subItem.Key == "BrightScale")
                            light.intensity = (float)subItem.Value;
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
                    //find child gameobject named Icon
                    var icon = tempObject.transform.Find("Icon");
                    icon.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Gizmos/light");
                    icon.gameObject.tag = "NoRayCast";
                    icon.gameObject.layer = 7;
                    var light = tempObject.gameObject.AddComponent<Light>();


                    foreach (var subItem in obj.options)
                    {
                        if (subItem.Key == "FOV")
                        {
                            light.innerSpotAngle = (float)subItem.Value;
                            light.spotAngle = (float)subItem.Value;
                        }

                        else if (subItem.Key == "LightRadius")
                            light.range = (float)subItem.Value * 0.01f;

                        else if (subItem.Key == "InnerColor")
                        {
                            var vec = (LTVector)subItem.Value;
                            Vector3 col = Vector3.Normalize(new Vector3(vec.X, vec.Y, vec.Z));
                            light.color = new Color(col.x, col.y, col.z);
                        }

                        else if (subItem.Key == "BrightScale")
                            light.intensity = (float)subItem.Value * 15;
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
                    //find child gameobject named Icon
                    var icon = tempObject.transform.Find("Icon");
                    icon.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Gizmos/light");
                    icon.gameObject.tag = "NoRayCast";
                    icon.gameObject.layer = 7;
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
                            light.intensity = (float)subItem.Value;
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

                if (obj.objectName == "GameStartPoint")
                {

                    int nCount = ModelDefinition.AVP2RandomCharacterGameStartPoint.Length;

                    int nRandom = UnityEngine.Random.Range(0, nCount);
                    string szName = ModelDefinition.AVP2RandomCharacterGameStartPoint[nRandom];

                    var temp = importer.CreateModelDefinition(szName, ModelType.Character, obj.options);

                    var gos = abc.LoadABC(temp);

                    if (gos != null)
                    {
                        gos.transform.position = tempObject.transform.position;
                        gos.transform.eulerAngles = rot;
                        gos.transform.parent = tempObject.transform;
                        gos.tag = "NoRayCast";
                    }

                    //find child gameobject named Icon
                    var icon = tempObject.transform.Find("Icon");
                    icon.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Gizmos/gsp");
                    icon.gameObject.tag = "NoRayCast";
                    icon.gameObject.layer = 7;
                }

                if (obj.objectName == "WeaponItem")
                {
                    string szName = "";

                    if (obj.options.ContainsKey("WeaponType"))
                    {
                        szName = (string)obj.options["WeaponType"];
                    }

                    //abc.FromFile("Assets/Models/" + szName + ".abc", true);

                    var temp = importer.CreateModelDefinition(szName, ModelType.WeaponItem, obj.options);

                    var gos = abc.LoadABC(temp);

                    if (gos != null)
                    {
                        gos.transform.position = tempObject.transform.position;
                        gos.transform.eulerAngles = rot;
                        gos.transform.parent = tempObject.transform;
                        gos.tag = "NoRayCast";
                        gos.layer = 2;
                    }


                }

                if (obj.objectName == "PropType")
                {
                    string szName = "";

                    if (obj.options.ContainsKey("Name"))
                    {
                        szName = (string)obj.options["Name"];
                    }


                    var temp = importer.CreateModelDefinition(szName, ModelType.PropType, obj.options);

                    var gos = abc.LoadABC(temp);

                    if (gos != null)
                    {
                        gos.transform.position = tempObject.transform.position;
                        gos.transform.eulerAngles = rot;
                        gos.transform.parent = tempObject.transform;
                        gos.tag = "NoRayCast";
                    }
                }

                if (obj.objectName == "Prop" ||
                    obj.objectName == "AmmoBox" ||
                    obj.objectName == "Beetle" ||
                    //obj.objectName == "BodyProp" || // not implemented
                    obj.objectName == "Civilian" ||
                    obj.objectName == "Egg" ||
                    obj.objectName == "HackableLock" ||
                    obj.objectName == "Plant" ||
                    obj.objectName == "StoryObject" ||
                    obj.objectName == "MEMO" ||
                    obj.objectName == "PC" ||
                    obj.objectName == "PDA" ||
                    obj.objectName == "Striker" ||
                    obj.objectName == "TorchableLock" ||
                    obj.objectName == "Turret"
                    )
                {

                    string szName = "";

                    if (obj.options.ContainsKey("Name"))
                    {
                        szName = (string)obj.options["Name"];
                    }


                    var temp = importer.CreateModelDefinition(szName, ModelType.Prop, obj.options);

                    var gos = abc.LoadABC(temp);

                    if (gos != null)
                    {
                        gos.transform.position = tempObject.transform.position;
                        gos.transform.eulerAngles = rot;
                        gos.transform.parent = tempObject.transform;
                        gos.tag = "NoRayCast";
                    }
                }

                if (obj.objectName == "Trigger")
                {
                    //find child gameobject named Icon
                    var icon = tempObject.transform.Find("Icon");
                    icon.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Gizmos/trigger");
                    icon.gameObject.tag = "NoRayCast";
                    icon.gameObject.layer = 7;
                }


                    var g = GameObject.Find("objects");
                tempObject.transform.SetParent(g.transform);
                g.transform.localScale = Vector3.one;

            }


            //disable unity's nastyness
            //RenderSettings.ambientLight = Color.black;
            //RenderSettings.ambientIntensity = 0.0f;

            //Setup AmbientLight
            SetupAmbientLight();
        }
        public void SetupAmbientLight()
        {
            if (worldReader.WorldProperties == null)
                return;

            var worldPropertiesArray = worldReader.WorldProperties.Split(';');

            foreach (var property in worldPropertiesArray)
            {
                if (!property.Contains("AmbientLight"))
                {
                    SetDefaultAmbientLight();
                    continue;
                }

                var splitStrings = property.Split(' ');

                if (splitStrings.Length < 4)
                {
                    SetDefaultAmbientLight();
                    continue;
                }

                Vector3 vAmbientRGB = Vector3.Normalize(new Vector3(
                    float.Parse(splitStrings[1]),
                    float.Parse(splitStrings[2]),
                    float.Parse(splitStrings[3])
                ));

                var color = new Color(vAmbientRGB.x, vAmbientRGB.y, vAmbientRGB.z, 1);
                SetAmbientLight(color);
            }
        }

        private void SetDefaultAmbientLight()
        {
            importer.defaultColor = new Color(0.1f, 0.1f, 0.1f, 1);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.0f;
        }

        private void SetAmbientLight(Color color)
        {
            //Check if color is 0,0,0 and boost a bit
            if (color.r == 0 && color.g == 0 && color.b == 0)
            {
                color = new Color(0.1f, 0.1f, 0.1f, 1);
            }

            RenderSettings.ambientLight = color;
            importer.defaultColor = color;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.0f;
        }
        public void Quit()
        {
            Application.Quit();
        }

        public static WorldObjects ReadObjects(ref BinaryReader b)
        {
            WorldObjects woObject = new WorldObjects();
            woObject.obj = new List<WorldObject>();

            var nTotalObjectCount = b.ReadInt32();

            for (int i = 0; i < nTotalObjectCount; i++)
            {
                //Make a new object
                WorldObject theObject = new WorldObject();

                //Make a dictionary to make things easier
                Dictionary<string, object> tempData = new Dictionary<string, object>();

                theObject.dataOffset = b.BaseStream.Position; // store our offset in our .dat

                theObject.dataLength = b.ReadInt16(); // Read our object datalength

                var dataLength = b.ReadInt16(); //read out property length

                theObject.objectName = ReadString(dataLength, ref b); // read our name

                theObject.objectEntries = b.ReadInt32();// read how many properties this object has

                for (int t = 0; t < theObject.objectEntries; t++)
                {

                    var nObjectPropertyDataLength = b.ReadInt16();
                    string szPropertyName = ReadString(nObjectPropertyDataLength, ref b);

                    PropType propType = (PropType)b.ReadByte();

                    switch (propType)
                    {
                        case PropType.PT_STRING:
                            theObject.objectEntryFlag.Add(b.ReadInt32()); //read the flag
                            theObject.objectEntryStringDataLength.Add(b.ReadInt16()); //read the string length plus the data length
                            nObjectPropertyDataLength = b.ReadInt16();
                            //Read the string
                            tempData.Add(szPropertyName, ReadString(nObjectPropertyDataLength, ref b));
                            break;

                        case PropType.PT_VECTOR:

                            theObject.objectEntryFlag.Add(b.ReadInt32()); //read the flag
                            nObjectPropertyDataLength = b.ReadInt16();
                            //Get our float data
                            LTVector tempVec = ReadLTVector(ref b);
                            //Add our object to the Dictionary
                            tempData.Add(szPropertyName, tempVec);
                            break;

                        case PropType.PT_ROTATION:
                            theObject.objectEntryFlag.Add(b.ReadInt32()); //read the flag
                                                                          //Get our data length
                            nObjectPropertyDataLength = b.ReadInt16();
                            //Get our float data
                            LTRotation tempRot = ReadLTRotation(ref b);
                            //Add our object to the Dictionary
                            tempData.Add(szPropertyName, tempRot);
                            break;
                        case PropType.PT_UINT:
                            theObject.objectEntryFlag.Add(b.ReadInt32()); //read the flag
                            nObjectPropertyDataLength = b.ReadInt16();
                            //Add our object to the Dictionary
                            tempData.Add(szPropertyName, b.ReadUInt32());
                            break;
                        case PropType.PT_BOOL:
                            theObject.objectEntryFlag.Add(b.ReadInt32()); //read the flag
                            nObjectPropertyDataLength = b.ReadInt16();
                            tempData.Add(szPropertyName, ReadBool(ref b));
                            break;
                        case PropType.PT_REAL:
                            theObject.objectEntryFlag.Add(b.ReadInt32()); //read the flag
                            nObjectPropertyDataLength = b.ReadInt16();
                            //Add our object to the Dictionary
                            tempData.Add(szPropertyName, ReadReal(ref b));
                            break;
                        case PropType.PT_COLOR:
                            theObject.objectEntryFlag.Add(b.ReadInt32()); //read the flag
                            nObjectPropertyDataLength = b.ReadInt16();
                            //Get our float data
                            LTVector tempCol = ReadLTVector(ref b);
                            //Add our object to the Dictionary
                            tempData.Add(szPropertyName, tempCol);
                            break;
                    }
                }

                theObject.options = tempData;

                woObject.obj.Add(theObject);
            }
            return woObject;
        }

        public WorldObjects GetWorldObjects()
        {
            return LTGameObjects;
        }

        public uint GetVersion()
        {
            return (uint)worldReader.WorldHeader.nVersion;
        }
    }
}