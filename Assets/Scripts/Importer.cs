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
    public Component DatReader;
    public string projectPath = String.Empty;
    public string fileName;
    public uint version;


    [SerializeField]
    public Material defaultMaterial;
    public Color defaultColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    public UnityEngine.UI.Text infoBox;
    public UnityEngine.UI.Text loadingUI;
    public GameObject prefab;

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

    public void Quit()
    {
        Application.Quit();
    }
}