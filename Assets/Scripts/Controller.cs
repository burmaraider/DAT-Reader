using LithFAQ;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public List<UnityEngine.UI.Toggle> settingsToggleList;



    public void OnEnable()
    {
        UIActionManager.OnChangeLightColor += ChangeLightColor;
        UIActionManager.OnChangeLightRadius += ChangeLightRadius;
        
    }

    public void OnDisable()
    {
        UIActionManager.OnChangeLightColor -= ChangeLightColor;
        UIActionManager.OnChangeLightRadius -= ChangeLightRadius;
    }

    private void ChangeLightRadius(float arg1, string arg2)
    {
        Light light = null;

        var gameObjectToFind = GameObject.Find(arg2 + "_obj");

        if (!gameObjectToFind) return;

        gameObjectToFind.TryGetComponent<Light>(out light);

        if (light)
        {
            light.range = arg1 * 0.01f;
        }
    }

    private void ChangeLightColor(Color color, string arg2)
    {
        Light light = null;

        var gameObjectToFind = GameObject.Find(arg2 + "_obj");

        if (!gameObjectToFind) return;

        gameObjectToFind.TryGetComponent<Light>(out light);

        if (light)
        {
            light.color = color;
        }
    }

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
