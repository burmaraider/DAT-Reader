using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ObjectPicker : MonoBehaviour
{

    private GameObject selectedObject;
    private GameObject pastSelectedObject;
    public Text infoBox;

    // Update is called once per frame
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
                    //StartCoroutine(ScaleMe(hit.transform));
                    Debug.Log("You selected the " + hit.transform.name); // ensure you picked right object

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
}
