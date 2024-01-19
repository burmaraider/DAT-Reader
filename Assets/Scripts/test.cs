using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public Camera mainCam;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {

        this.transform.rotation = mainCam.transform.rotation;

        var gameObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (var gameObject in gameObjects)
        {
            if (gameObject.name.Contains("pointer", System.StringComparison.OrdinalIgnoreCase))
            {
                this.transform.position = gameObject.transform.position;
            }
        }
        

    }

    private void Awake()
    {

    }
}
