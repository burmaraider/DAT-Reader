using LithFAQ;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public List<UnityEngine.UI.Toggle> settingsToggleList;
    // Start is called before the first frame update
    void Start()
    {
        settingsToggleList = new List<UnityEngine.UI.Toggle>();

        UnityEngine.UI.Toggle[] togglesInScene = FindObjectsOfType<UnityEngine.UI.Toggle>();

        foreach (UnityEngine.UI.Toggle toggle in togglesInScene)
        {
            //only add if not already in list
            if (!settingsToggleList.Contains(toggle))
            {
                settingsToggleList.Add(toggle);
            }
        }
    }

    public void ChangeAmbientLighting(float slider)
    {
        Color col = GameObject.Find("Level").GetComponent<Importer>().defaultColor;
        RenderSettings.ambientLight = col *= slider;
        RenderSettings.ambientIntensity = slider;
    }

}
