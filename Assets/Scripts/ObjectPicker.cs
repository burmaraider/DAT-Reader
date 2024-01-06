using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ObjectPicker : MonoBehaviour
{

    private GameObject selectedObject;
    private GameObject pastSelectedObject;
    public Text infoBox;

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject() == false)
        {
            if ( Input.GetMouseButtonDown (0))
            { 
                RaycastHit hit; 
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
                if ( Physics.Raycast (ray,out hit,10000.0f)) 
                {
                    if(hit.transform.name != "PhysicsBSP")
                    {
                        infoBox.text = string.Format("Selected Object: {0}\n", hit.transform.name);

                        if(pastSelectedObject == null)
                        {
                            selectedObject = hit.transform.gameObject;
                            selectedObject.AddComponent<cakeslice.Outline>();
                            pastSelectedObject = selectedObject;
                        }

                        else
                        {
                            selectedObject = hit.transform.gameObject;
                            selectedObject.AddComponent<cakeslice.Outline>();
                            Destroy(pastSelectedObject.GetComponent<cakeslice.Outline>());
                            pastSelectedObject = selectedObject;
                        }
                    }
                }
            }
            if(Input.GetKeyDown(KeyCode.Delete))
            {
                Destroy(selectedObject);
                pastSelectedObject = null;

                infoBox.text = string.Format("Deleted Object: {0}\n", selectedObject.name);
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

    public void ToggleShadows(System.Boolean b)
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
                mr.enabled = !mr.enabled;
            }
        }
    }
}
