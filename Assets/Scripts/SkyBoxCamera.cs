using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyBoxCamera : MonoBehaviour
{
    public Camera mainCam;

    // Update is called once per frame
    void LateUpdate()
    {
        this.transform.rotation = mainCam.transform.rotation;
        
        var gameObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (var gameObject in gameObjects)
        {
            if (gameObject.name.Contains("pointer", System.StringComparison.OrdinalIgnoreCase) || gameObject.name.Contains("DemoSky", System.StringComparison.OrdinalIgnoreCase))
            {
                this.transform.position = gameObject.transform.position;
            }
        }
    }
}