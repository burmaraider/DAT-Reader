using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LithFAQ;
using static LithFAQ.LTTypes;
using static LithFAQ.LTUtils;
using static DTX;
using System.IO;
using SFB;
using System;

public class Importer : MonoBehaviour
{
    [SerializeField]
    public DTXMaterial dtxMaterialList = new DTXMaterial();
    public Component DatReader;
    public string szProjectPath = String.Empty;
    public string szFileName;
    public uint nVersion;
    public GameObject RuntimeGizmoPrefab;


    [SerializeField]
    public Material defaultMaterial;
    public Color defaultColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    public UnityEngine.UI.Text infoBox;
    public UnityEngine.UI.Text loadingUI;
    public GameObject prefab;

    public Dictionary<ModelType, INIParser> configButes = new Dictionary<ModelType, INIParser>();

    public void OpenDAT()
    {
        ExtensionFilter[] efExtensionFiler = new[] {
                new ExtensionFilter("Lithtech World DAT", "dat" )
            };
        // Open file
        string[] aFilePaths = StandaloneFileBrowser.OpenFilePanel("Open File", "", efExtensionFiler, false);

        if (aFilePaths.Length > 0)
        {
            this.szProjectPath = Path.GetDirectoryName(aFilePaths[0]);
            szFileName = Path.GetFileName(aFilePaths[0]);
            Debug.Log("Project Path: " + this.szProjectPath);
            Debug.Log("File Name: " + szFileName);
            Debug.Log("File Path: " + aFilePaths[0]);

            Array.Resize(ref aFilePaths, 2);
            string[] projectPath = StandaloneFileBrowser.OpenFolderPanel("Open Project Path", aFilePaths[0], false);
            if (projectPath.Length > 0)
            {
                aFilePaths[1] = projectPath[0];
                this.szProjectPath = projectPath[0];
            }
            else
            {
                return;
            }

            BinaryReader binaryReader = new BinaryReader(File.Open(aFilePaths[0], FileMode.Open));

            if (binaryReader == null)
            {
                Debug.LogError("Could not open DAT file");
                return;
            }

            nVersion = ReadDATVersion(ref binaryReader);

            DatReader = null;

            //Build the string to find the correct DAT reader class based on the version read from the DAT
            string szComponentName = "LithFAQ.DATReader" + nVersion.ToString();
            DatReader = gameObject.AddComponent(Type.GetType(szComponentName));

            if (DatReader == null)
            {
                Debug.LogError("Could not find DAT reader for version " + nVersion.ToString());
                return;
            }

            //load the DAT
            IDATReader reader = (IDATReader)DatReader;
            reader.Load(binaryReader);

            UIActionManager.OnPostLoadLevel?.Invoke();

        }
        return;
    }

    public void OnEnable()
    {
        UIActionManager.OnPreLoadLevel += OnPreLoadLevel;
        UIActionManager.OnPreClearLevel += ClearLevel;

    }

    public void OnDisable()
    {
        UIActionManager.OnPreLoadLevel -= OnPreLoadLevel;
        UIActionManager.OnPreClearLevel -= ClearLevel;
    }

    private void OnPreLoadLevel()
    {
        ClearLevel();
        OpenDAT();
    }

    public void ClearLevel()
    {
        ResetAllProperties();
    }

    private void ResetAllProperties()
    {
        szProjectPath = String.Empty;
        szFileName = String.Empty;
        nVersion = 0;
        Resources.UnloadUnusedAssets();

        UIActionManager.OnReset?.Invoke();
    }

    private uint ReadDATVersion(ref BinaryReader binaryReader)
    {
        uint version = binaryReader.ReadUInt32();
        binaryReader.BaseStream.Position = 0; //reset back to start of the file so that our DAT reader can read it
        return version;
    }


    public ModelDefinition CreateModelDefinition(string szName, ModelType type, Dictionary<string, object> objectInfo = null)
    {
        //Bail out!
        if (type == ModelType.None)
            return null;

        ModelDefinition modelDefinition = new ModelDefinition();
        INIParser ini = new INIParser();

        if (objectInfo != null)
        {
            if (objectInfo.ContainsKey("MoveToFloor"))
            {
                modelDefinition.bMoveToFloor = (bool)objectInfo["MoveToFloor"];
            }
            if (objectInfo.ContainsKey("ForceNoMoveToGround"))
            {
                modelDefinition.bMoveToFloor = !(bool)objectInfo["ForceNoMoveToGround"];
            }
            if (objectInfo.ContainsKey("HumanOnly"))
            {
                modelDefinition.bMoveToFloor = true;
            }

        }

        if (type == ModelType.Character)
        {
            modelDefinition.modelType = type;
            if (!configButes.ContainsKey(type))
            {
                if (File.Exists(szProjectPath + "\\Attributes\\CharacterButes.txt"))
                {
                    ini.Open(szProjectPath + "\\Attributes\\CharacterButes.txt");
                    configButes.Add(type, ini); //stuff this away
                }
                else
                {
                    Debug.LogError("Could not find CharacterButes.txt");
                    return null;
                }
            }


            Dictionary<string, string> item = null;

            if (type == ModelType.BodyProp)
            {
                if (objectInfo.ContainsKey("CharacterType"))
                {
                    szName = (string)objectInfo["CharacterType"];
                }
            }

            item = configButes[type].GetSectionsByName(szName);

            if (item == null)
                return null;

            foreach (var key in item)
            {

                if (key.Key == "DefaultModel")
                {
                    modelDefinition.szModelFileName = key.Value.Replace("\"", "");
                }
                if (key.Key == "DefaultSkin0")
                {
                    modelDefinition.szModelTextureName.Add("Skins\\Characters\\" + key.Value.Trim('"'));
                }
                if (key.Key == "DefaultSkin1")
                {
                    modelDefinition.szModelTextureName.Add("Skins\\Characters\\" + key.Value.Trim('"'));
                }
                if (key.Key == "DefaultSkin2")
                {
                    modelDefinition.szModelTextureName.Add("Skins\\Characters\\" + key.Value.Trim('"'));
                }
                if (key.Key == "DefaultSkin3")
                {
                    modelDefinition.szModelTextureName.Add("Skins\\Characters\\" + key.Value.Trim('"'));
                }
            }

            modelDefinition.szModelFilePath = szProjectPath + "\\Models\\Characters\\" + modelDefinition.szModelFileName;
            modelDefinition.FitTextureList();

            return modelDefinition;
        }


        if (type == ModelType.Pickup)
        {
            modelDefinition.modelType = type;
            if (!configButes.ContainsKey(type))
            {
                if (File.Exists(szProjectPath + "\\Attributes\\PickupButes.txt"))
                {
                    ini.Open(szProjectPath + "\\Attributes\\PickupButes.txt");
                    configButes.Add(type, ini); //stuff this away
                }
                else
                {
                    Debug.LogError("Could not find PickupButes.txt");
                    return null;
                }
            }

            foreach (var sections in configButes[type].GetSections)
            {

                var test = sections.Value;

                // check if keys has a name
                if (sections.Value.ContainsKey("Name"))
                {
                    if (sections.Value["Name"].Replace("\"", "") != szName)
                    {
                        continue;
                    }
                    else
                    {
                        string modelName = configButes[type].ReadValue(sections.Key, "Model", "1x1square.abc");

                        if (!String.IsNullOrEmpty(modelName))
                        {
                            modelDefinition.szModelFileName = modelName.Replace("\"", "");
                            modelDefinition.szModelFilePath = szProjectPath + "\\" + modelName.Replace("\"", "");
                        }

                        //get skins, could be up to 4, but not always defined.. FUN

                        for (int i = 0; i < 4; i++)
                        {
                            string szSkinString = String.Format("Skin{0}", i);

                            modelDefinition.szModelTextureName.Add(configButes[type].ReadValue(sections.Key, szSkinString, "").Replace("\"", String.Empty));

                        }
                        modelDefinition.FitTextureList();

                    }
                    return modelDefinition;
                }

            }

        }

        if (type == ModelType.Prop)
        {
            modelDefinition.modelType = type;

            //find the key "Filename" in the dictionary
            string szFilename = (string)objectInfo["Filename"];
            string szSkins = (string)objectInfo["Skin"];

            string[] szSkinArray = szSkins.Split(';');

            foreach (var szSkin in szSkinArray)
            {
                modelDefinition.szModelTextureName.Add(szSkin);
            }

            modelDefinition.szModelFileName = szFilename;
            modelDefinition.szModelFilePath = szProjectPath + "\\" + modelDefinition.szModelFileName;

            if (objectInfo.ContainsKey("Chromakey"))
            {
                modelDefinition.bChromakey = (bool)objectInfo["Chromakey"];
            }

            return modelDefinition;

        }

        if (type == ModelType.PropType)
        {
            modelDefinition.modelType = type;

            if (!configButes.ContainsKey(type))
            {
                if (File.Exists(szProjectPath + "\\Attributes\\PropTypes.txt"))
                {
                    ini.Open(szProjectPath + "\\Attributes\\PropTypes.txt");
                    configButes.Add(type, ini); //stuff this away
                }
                else
                {
                    Debug.LogError("Could not find PropTypes.txt");
                    return null;
                }
            }

            string szType = objectInfo["Type"].ToString();



            foreach (var sections in configButes[type].GetSections)
            {

                // check if keys has a name
                if (sections.Value.ContainsKey("Type"))
                {
                    if (sections.Value["Type"].Replace("\"", "") != szType)
                    {
                        continue;
                    }
                    else
                    {
                        string modelName = configButes[type].ReadValue(sections.Key, "Filename", "1x1square.abc");

                        if (!String.IsNullOrEmpty(modelName))
                        {
                            modelDefinition.szModelFileName = modelName.Replace("\"", "");
                            modelDefinition.szModelFilePath = szProjectPath + "\\" + modelName.Replace("\"", "");
                        }

                        //get skins, could be up to 4, but not always defined.. FUN
                        string szSkins = configButes[type].ReadValue(sections.Key, "Skin", "");

                        string[] szSkinArray = szSkins.Split(';');

                        foreach (string szSkin in szSkinArray)
                        {
                            modelDefinition.szModelTextureName.Add(szSkin.Replace("\"", ""));
                        }
                    }

                    string szMoveToFloorString = configButes[type].ReadValue(sections.Key, "MoveToFloor", "0");

                    if (szMoveToFloorString == "1")
                    {
                        modelDefinition.bMoveToFloor = true;
                    }
                    else
                    {
                        modelDefinition.bMoveToFloor = false;
                    }

                    return modelDefinition;
                }

            }

        }
        return null;
    }

    public void Quit()
    {
        Application.Quit();
    }
}