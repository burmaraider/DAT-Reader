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
    public DTXMaterial dtxMaterialList { get; set; } = new DTXMaterial();
    public Component DatReader;
    public GameObject RuntimeGizmoPrefab;


    [SerializeField]
    public Material defaultMaterial;
    public Color defaultColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    public UnityEngine.UI.Text infoBox;
    public UnityEngine.UI.Text loadingUI;
    public GameObject prefab;

    [Header("GameSpecific")]
    public string szProjectPath = String.Empty;
    public string szFileName;
    public uint nVersion;
    public int nSelectedGame;
    public Game eGame { get { return (Game)nSelectedGame; }}

    public Dictionary<ModelType, INIParser> configButes = new Dictionary<ModelType, INIParser>();

    private void OpenDAT(string pathToDATFile, string projectPath)
    {
        this.szProjectPath = projectPath;
        this.szFileName = Path.GetFileName(pathToDATFile);

        Debug.Log("Project Path: " + this.szProjectPath);
        Debug.Log("File Name: " + this.szFileName);
        Debug.Log("File Path: " + pathToDATFile);

        BinaryReader binaryReader = new BinaryReader(File.Open(pathToDATFile, FileMode.Open));
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

    public void OpenDAT()
    {
        ExtensionFilter[] efExtensionFiler = new[] { new ExtensionFilter("Lithtech World DAT", "dat") };

        // Open file
        string[] pathToDATFile = StandaloneFileBrowser.OpenFilePanel("Open File", "", efExtensionFiler, false);
        if (pathToDATFile.Length == 0)
        {
            return;
        }

        string defaultProjectPath = Path.GetDirectoryName(pathToDATFile[0]);
        string[] projectPath = StandaloneFileBrowser.OpenFolderPanel("Open Project Path", defaultProjectPath, false);
        if (projectPath.Length == 0)
        {
            projectPath = new string[] { defaultProjectPath };
        }

        OpenDAT(pathToDATFile[0], projectPath[0]);
    }

    public void OpenDefaultDAT()
    {
        this.nSelectedGame = (int)Game.LOMM;
        OpenDAT("C:\\LoMM\\Data\\Worlds\\_RESCUEATTHERUINS.DAT", "C:\\LoMM\\Data");
    }

    public void OnEnable()
    {
        UIActionManager.OnPreLoadLevel += OnPreLoadLevel;
        UIActionManager.OnPreClearLevel += ClearLevel;
        UIActionManager.OnOpenDefaultLevel += OnOpenDefaultLevel;
    }

    public void OnDisable()
    {
        UIActionManager.OnPreLoadLevel -= OnPreLoadLevel;
        UIActionManager.OnPreClearLevel -= ClearLevel;
        UIActionManager.OnOpenDefaultLevel -= OnOpenDefaultLevel;
    }

    private void OnOpenDefaultLevel()
    {
        ClearLevel();
        OpenDefaultDAT();
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

        // TTTT Fix this:
        //if (szName != "Cot0" && szName != "Bookcase1")
        //{
        //    return null;
        //}

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
                String szCharButes = "\\Attributes\\CharacterButes.txt";
                //get game config selection
                if (nSelectedGame == (int)Game.DIEHARD)
                {
                    szCharButes = "\\Attributes\\Character.txt";
                }

                if (File.Exists(szProjectPath + szCharButes))
                {
                    ini.Open(szProjectPath + szCharButes);
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
        else if (type == ModelType.Pickup)
        {
            modelDefinition.modelType = type;
            if (!configButes.ContainsKey(type))
            {
                IDATReader reader = (IDATReader)DatReader;

                var nVersion = reader.GetVersion();

                string szButeFile = String.Empty;

                if (nVersion > 66)
                {
                    szButeFile = szProjectPath + "\\Attributes\\PickupButes.txt";
                }
                else
                {
                    szButeFile = szProjectPath + "\\Attributes\\Weapons.txt";
                }


                if (File.Exists(szButeFile))
                {
                    ini.Open(szButeFile);
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

                        string modelName = String.Empty;

                        if (nVersion > 66)
                            configButes[type].ReadValue(sections.Key, "Model", "1x1square.abc");
                        else
                            configButes[type].ReadValue(sections.Key, "HHModel", "1x1square.abc");



                        if (!String.IsNullOrEmpty(modelName))
                        {
                            modelDefinition.szModelFileName = modelName.Replace("\"", "");
                            modelDefinition.szModelFilePath = Path.Combine(szProjectPath, modelName.Replace("\"", ""));
                        }

                        //get skins, could be up to 4, but not always defined.. FUN

                        if (nVersion > 66)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                string szSkinString = String.Format("Skin{0}", i);

                                modelDefinition.szModelTextureName.Add(configButes[type].ReadValue(sections.Key, szSkinString, "").Replace("\"", String.Empty));

                            }
                            modelDefinition.FitTextureList();
                        }
                        else
                        {
                            modelDefinition.szModelTextureName.Add(configButes[type].ReadValue(sections.Key, "HHSkin", "").Replace("\"", String.Empty));
                        }

                    }
                    return modelDefinition;
                }

            }

        }
        else if (type == ModelType.WeaponItem)
        {
            modelDefinition.modelType = type;
            if (!configButes.ContainsKey(type))
            {
                IDATReader reader = (IDATReader)DatReader;

                var nVersion = reader.GetVersion();

                string szButeFile = String.Empty;

                if (nVersion > 66)
                {
                    szButeFile = szProjectPath + "\\Attributes\\PickupButes.txt";
                }
                else
                {
                    szButeFile = szProjectPath + "\\Attributes\\Weapons.txt";
                }


                if (File.Exists(szButeFile))
                {
                    ini.Open(szButeFile);
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

                        string modelName = String.Empty;

                        if (nVersion > 66)
                            configButes[type].ReadValue(sections.Key, "Model", "1x1square.abc");
                        else
                            configButes[type].ReadValue(sections.Key, "HHModel", "1x1square.abc");



                        if (!String.IsNullOrEmpty(modelName))
                        {
                            modelDefinition.szModelFileName = modelName.Replace("\"", "");
                            modelDefinition.szModelFilePath = Path.Combine(szProjectPath, modelName.Replace("\"", ""));
                        }

                        //get skins, could be up to 4, but not always defined.. FUN

                        if (nVersion > 66)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                string szSkinString = String.Format("Skin{0}", i);

                                modelDefinition.szModelTextureName.Add(configButes[type].ReadValue(sections.Key, szSkinString, "").Replace("\"", String.Empty));

                            }
                            modelDefinition.FitTextureList();
                        }
                        else
                        {
                            modelDefinition.szModelTextureName.Add(configButes[type].ReadValue(sections.Key, "HHSkin", "").Replace("\"", String.Empty));
                        }

                    }
                    return modelDefinition;
                }

            }

        }
        else if (type == ModelType.Prop)
        {
            modelDefinition.modelType = type;

            //find the key "Filename" in the dictionary
            string szFilename = (string)objectInfo["Filename"];

            
            if(!objectInfo.ContainsKey("Skin"))
            {
                Debug.LogError("No skin found for prop");
                return null;
            }


            string szSkins = (string)objectInfo["Skin"];
 

            string[] szSkinArray = szSkins.Split(';');

            foreach (var szSkin in szSkinArray)
            {
                modelDefinition.szModelTextureName.Add(szSkin);
            }

            modelDefinition.szModelFileName = szFilename;
            modelDefinition.szModelFilePath = Path.Combine(szProjectPath, modelDefinition.szModelFileName);

            if (objectInfo.ContainsKey("Chromakey"))
            {
                modelDefinition.bChromakey = (bool)objectInfo["Chromakey"];
            }

            return modelDefinition;

        }
        else if (type == ModelType.PropType)
        {
            modelDefinition.modelType = type;

            if (!configButes.ContainsKey(type))
            {
                String szPropToLoad = "\\Attributes\\PropTypes.txt";

                if(nSelectedGame == (int)Game.DIEHARD)
                {
                    szPropToLoad = "\\Attributes\\Prop.txt";
                }

                if (File.Exists(szProjectPath + szPropToLoad))
                {
                    ini.Open(szProjectPath + szPropToLoad);
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
                            modelDefinition.szModelFilePath = Path.Combine(szProjectPath, modelName.Replace("\"", ""));
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

                    modelDefinition.bChromakey = configButes[type].ReadValue(sections.Key, "Chromakey", false);

                    return modelDefinition;
                }

            }
        }
        else if (type == ModelType.Princess)
        {
            modelDefinition.modelType = type;

            modelDefinition.szModelFileName = "MODELS\\Princess.abc";
            modelDefinition.szModelTextureName.Add("SKINS\\PRINCESSPINK.DTX");
            modelDefinition.szModelFilePath = Path.Combine(szProjectPath, modelDefinition.szModelFileName);

            if (objectInfo.ContainsKey("Chromakey"))
            {
                modelDefinition.bChromakey = (bool)objectInfo["Chromakey"];
            }

            return modelDefinition;
        }
        else if (type == ModelType.Monster)
        {
            modelDefinition.modelType = type;

            string szFilename = (string)objectInfo["Filename"];
            modelDefinition.szModelFileName = szFilename;

            string skinName = Path.GetFileNameWithoutExtension(szFilename) + ".dtx";
            modelDefinition.szModelTextureName.Add("SKINS\\" + skinName);
            modelDefinition.szModelFilePath = Path.Combine(szProjectPath, modelDefinition.szModelFileName);

            if (objectInfo.ContainsKey("Chromakey"))
            {
                modelDefinition.bChromakey = (bool)objectInfo["Chromakey"];
            }

            return modelDefinition;
        }

        return null;
    }

    public void Quit()
    {
        Application.Quit();
    }
}