using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RuntimeObjectType : MonoBehaviour
{

    public string objectType;
    public Transform cam;
    private float distanceFromCamera;
    private float scaleFactor;
    public float scale = 6.0f;
    public float distance = 2.0f;

    void Start()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        GetComponent<TextMeshPro>().fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field Overlay");
        GetComponent<TextMeshPro>().fontSize = 2f;
        GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
    }

    void FixedUpdate()
    {
        distanceFromCamera = Vector3.Distance(transform.position, cam.position);
        if(distanceFromCamera < 15f)
        {
            GetComponent<TextMeshPro>().text = objectType;
            scaleFactor = ((cam.position - transform.position).sqrMagnitude - distance) / scale;

            scaleFactor = (Mathf.Min(scaleFactor, 1.0f));
            GetComponent<TextMeshPro>().color = Color.white * (scaleFactor);
            
            transform.localScale = new Vector3(
                -Mathf.Clamp(scaleFactor, 0.0f, 1.0f), 
                Mathf.Clamp(scaleFactor, 0.0f, 1.0f), 
                Mathf.Clamp(scaleFactor, 0.0f, 1.0f));
            transform.LookAt(cam, Vector3.up);
        }
        else
            GetComponent<TextMeshPro>().color = Color.white * 0.0f;
    }
}
