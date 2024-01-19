using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LithFAQ;
using static LithFAQ.LTTypes;
using static LithFAQ.LTUtils;
using System.IO;
using SFB;
using System;

public class Importer : MonoBehaviour
{
    [SerializeField]
    public DTX.DTXMaterial dtxMaterialList = new DTX.DTXMaterial();
    public Component DatReader;
    public string projectPath = String.Empty;
    public string fileName;
    public uint version;
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
        var extensions = new[] {
                new ExtensionFilter("Lithtech World DAT", "dat" )
            };
        // Open file
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);

        if (paths.Length > 0)
        {
            this.projectPath = Path.GetDirectoryName(paths[0]);
            fileName = Path.GetFileName(paths[0]);
            Debug.Log("Project Path: " + this.projectPath);
            Debug.Log("File Name: " + fileName);
            Debug.Log("File Path: " + paths[0]);

            Array.Resize(ref paths, 2);
            string[] projectPath = StandaloneFileBrowser.OpenFolderPanel("Open Project Path", paths[0], false);
            if (projectPath.Length > 0)
            {
                paths[1] = projectPath[0];
                this.projectPath = projectPath[0];
            }
            else
            {
                return;
            }

            BinaryReader binaryReader = new BinaryReader(File.Open(paths[0], FileMode.Open));
            
            if (binaryReader == null)
            {
                Debug.LogError("Could not open DAT file");
                return;
            }
            
            version = ReadDATVersion(ref binaryReader);

            DatReader = null;

            //Build the string to find the correct DAT reader class based on the version read from the DAT
            string szComponentName = "LithFAQ.DATReader" + version.ToString();
            DatReader = gameObject.AddComponent(Type.GetType(szComponentName));

            if (DatReader == null)
            {
                Debug.LogError("Could not find DAT reader for version " + version.ToString());
                return;
            }
            //load the DAT
            IDATReader reader = (IDATReader)DatReader;
            reader.Load(binaryReader);
        }
        return;
    }
    public void ClearLevel()
    {
        if (DatReader != null)
        {
            IDATReader reader = (IDATReader)DatReader;
            reader.ClearLevel();
            ResetAllProperties();
        }
        
    }
    
    private void ResetAllProperties()
    {
        projectPath = String.Empty;
        fileName = String.Empty;
        version = 0;
        Destroy(DatReader);
        DatReader = null;
        Resources.UnloadUnusedAssets();
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
            if(objectInfo.ContainsKey("HumanOnly"))
            {
                modelDefinition.bMoveToFloor = true;
            }
            
        }

        if (type == ModelType.Character)
        {
            modelDefinition.modelType = type;
            if (!configButes.ContainsKey(type))
            {
                ini.Open(projectPath + "\\Attributes\\CharacterButes.txt");
                configButes.Add(type, ini); //stuff this away
            }
            

            Dictionary<string, string> item = null;

            if (type == ModelType.BodyProp)
            {
                if(objectInfo.ContainsKey("CharacterType"))
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
                    modelDefinition.szModelTextureName.Add("Skins\\Characters\\" + key.Value.Replace("\"", ""));
                }
                if (key.Key == "DefaultSkin1")
                {
                    modelDefinition.szModelTextureName.Add("Skins\\Characters\\" + key.Value.Replace("\"", ""));
                }
                if (key.Key == "DefaultSkin2")
                {
                    modelDefinition.szModelTextureName.Add("Skins\\Characters\\" + key.Value.Replace("\"", ""));
                }
                if (key.Key == "DefaultSkin3")
                {
                    modelDefinition.szModelTextureName.Add("Skins\\Characters\\" + key.Value.Replace("\"", ""));
                }
            }

            modelDefinition.szModelFilePath = projectPath + "\\Models\\Characters\\" + modelDefinition.szModelFileName;
            modelDefinition.FitTextureList();

            return modelDefinition;
        }
        

        if (type == ModelType.Pickup)
        {
            modelDefinition.modelType = type;
            if (!configButes.ContainsKey(type))
            {
                ini.Open(projectPath + "\\Attributes\\PickupButes.txt");
                configButes.Add(type, ini); //stuff this away
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

                        if(!String.IsNullOrEmpty(modelName))
                        {
                            modelDefinition.szModelFileName = modelName.Replace("\"", "");
                            modelDefinition.szModelFilePath = projectPath + "\\" + modelName.Replace("\"", "");
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

            /*
            var item = ini.GetSectionsByName("Tamiko");

            foreach (var key in item)
            {

                if (key.Key == "DefaultModel")
                {
                    modelDefinition.szModelFileName = key.Value.Replace("\"", "");
                }
                if (key.Key == "DefaultSkin0")
                {
                    modelDefinition.szModelTextureName.Add(key.Value.Replace("\"", ""));
                }
                if (key.Key == "DefaultSkin1")
                {
                    modelDefinition.szModelTextureName.Add(key.Value.Replace("\"", ""));
                }
                if (key.Key == "DefaultSkin2")
                {
                    modelDefinition.szModelTextureName.Add(key.Value.Replace("\"", ""));
                }
                if (key.Key == "DefaultSkin3")
                {
                    modelDefinition.szModelTextureName.Add(key.Value.Replace("\"", ""));
                }
            }

            modelDefinition.szModelFilePath = projectPath + "\\Models\\Characters\\" + modelDefinition.szModelFileName + ".abc";
            modelDefinition.FitTextureList();
            */

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
            modelDefinition.szModelFilePath = projectPath + "\\" + modelDefinition.szModelFileName;

            if(objectInfo.ContainsKey("Chromakey"))
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