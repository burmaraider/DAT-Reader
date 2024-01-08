using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        
    }

    private void Awake()
    {
        
    }

    public void ChangeAmbientLighting(UnityEngine.UI.Slider slider)
    {
        Color col = GameObject.Find("Level").GetComponent<DATReader70>().defaultColor;
        RenderSettings.ambientLight = col *= slider.value;
        RenderSettings.ambientIntensity = slider.value;
    }

}
