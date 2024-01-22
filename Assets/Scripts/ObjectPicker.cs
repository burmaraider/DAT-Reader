using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ObjectPicker : MonoBehaviour
{

    public GameObject selectedObject;
    public Text infoBox;

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject() == false)
        {
            if ( Input.GetMouseButtonDown (0))
            { 
                if(selectedObject != null)
                {
                    selectedObject.layer = 0;
                    infoBox.text = string.Format("");
                }

                RaycastHit hit; 
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
                if ( Physics.Raycast (ray,out hit,10000.0f)) 
                {
                    if(hit.transform.name != "PhysicsBSP")
                    {
                        infoBox.text = string.Format("Selected Object: {0}", hit.transform.name);

                        //change selected game object to layer 6 if selected, and 0 if deselected
                        if (selectedObject != null)
                        {
                            selectedObject.layer = 0;
                        }
                        selectedObject = hit.transform.gameObject;
                        selectedObject.layer = 6;
                    }
                }
            }
            if(Input.GetKeyDown(KeyCode.Delete))
            {
                if (selectedObject == null)
                {
                    infoBox.text = string.Format("No object selected");
                    return;
                }
                
                infoBox.text = string.Format("Deleted Object: {0}", selectedObject.name);
                Destroy(selectedObject);
                selectedObject = null;
            }
        }
    }

    public void ToggleBlockers(System.Boolean b)
    {
        var levelGameObject = GameObject.Find("Level");

        foreach(Transform t in levelGameObject.transform)
            if(t.tag == "Blocker")
                t.gameObject.SetActive(b);
    }

    public void ToggleVolumes(System.Boolean b)
    {
        var levelGameObject = GameObject.Find("Level");

        foreach(Transform t in levelGameObject.transform)
            if(t.tag == "Volumes")
                t.gameObject.SetActive(b);
    }

    public void ToggleBSP(System.Boolean b)
    {
        var levelGameObject = GameObject.Find("Level");

        foreach(Transform t in levelGameObject.transform)
            if(t.name == "PhysicsBSP")
                t.gameObject.SetActive(b);
    }

    public void ToggleShadows(bool b)
    {
        var levelGameObject = GameObject.Find("objects");

        foreach(Transform t in levelGameObject.transform)
        {
            var temp = t.GetComponent<Light>();

            if(temp)
            {
                if(b)
                    temp.shadows = LightShadows.Soft;
                else
                    temp.shadows = LightShadows.None;
            }
        }
    }

    public void ToggleObjects(System.Boolean b)
    {
        var levelGameObject = GameObject.Find("objects");

        foreach (Transform t in levelGameObject.transform)
        {
            foreach (MeshRenderer mr in t.GetComponentsInChildren<MeshRenderer>())
            {
                mr.enabled = b;
            }
        }
    }
}
