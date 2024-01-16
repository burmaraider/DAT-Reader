using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;


public enum ModelType
{
    None,
    Pickup,
    Weapon,
    Character,
    Prop
}

public class ModelDefinition
{
    public string szModelFileName = string.Empty;
    public string szModelFilePath = string.Empty;
    public List<string> szModelTextureName = new List<string>();
    public ModelType modelType = ModelType.None;
    public Model model = new Model();
    public GameObject rootObject;

    public static string[] AVP2RandomCharacterGameStartPoint = { "BaseHuman", "Tamiko", "Eisenberg", "RailGunner", "Obrian", "Predalien", "BaseAlien", "BasePredator", "SmartGunner", "Grenadier", "LightPredator", "Merc2", "Merc3", "Rykov" };

    public void FitTextureList()
    {
        if (szModelTextureName.Count > 0)
        {
            //remove empty texture name
            for (int i = szModelTextureName.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(szModelTextureName[i]))
                {
                    szModelTextureName.RemoveAt(i);
                }
            }
        }
    }
}
