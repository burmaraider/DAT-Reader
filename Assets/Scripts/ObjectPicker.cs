using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LithFAQ;

public class ObjectPicker : MonoBehaviour
{

    public GameObject selectedObject;
    public Text infoBox;
    public GameObject selectionBox;

    private void Start()
    {
        selectionBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        selectionBox.transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject() == false)
        {
            if ( Input.GetMouseButtonDown (0) && Input.GetKey(KeyCode.LeftShift))
            { 
                if(selectedObject != null)
                {
                    selectedObject.layer = 0;
                    infoBox.text = string.Format("");
                    selectionBox.transform.localScale = new Vector3(0, 0, 0);
                }

                //raycast all
                Ray curRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits;
                hits = Physics.RaycastAll(curRay, 100.0f);

                System.Array.Sort(hits, delegate (RaycastHit x, RaycastHit y) { return x.distance.CompareTo(y.distance); });

                foreach (var item in hits)
                {
                    Debug.Log(item.transform.name);
                }



                RaycastHit hit; 
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
                if ( Physics.Raycast(ray,out hit,10000.0f)) 
                {
                    if(hit.transform.name != "PhysicsBSP")
                    {
                        infoBox.text = string.Format("Selected Object: {0}", hit.transform.name);

                        //get the objects top root parent
                        Transform topParent = hit.transform.parent;

                        //inform the UI that we have selected an object
                        UIActionManager.OnSelectObjectIn3D?.Invoke(topParent.name);

                        //create a box that encapsulates the selected object
                        Renderer renderer = hit.transform.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            Bounds bounds = renderer.bounds;
                            selectionBox.transform.position = bounds.center;
                            selectionBox.transform.localScale = bounds.size;
                            selectionBox.layer = 6; // Set the layer of the box to 6
                            Material material = Resources.Load("Materials/SelectionBoxGlow") as Material;
                            selectionBox.GetComponent<Renderer>().material = material;

                            //check if this object is a trigger, if so, make the bounding box as big as the "dims"
                            var importer = GameObject.Find("Level").GetComponent<Importer>();
                            if (importer.DatReader != null)
                            {
                                IDATReader reader = (IDATReader)importer.DatReader;

                                var temp = reader.GetWorldObjects();

                                foreach (var item in temp.obj)
                                {
                                    if(hit.transform.parent.name.Replace("_obj", "") == (string)item.options["Name"])
                                    {
                                        //found matching object
                                        if(item.objectName.Contains("trigger", System.StringComparison.OrdinalIgnoreCase))
                                        {
                                            LTTypes.LTVector dims = (LTTypes.LTVector)item.options["Dims"];
                                            Vector3 newDims = dims;
                                            newDims *= 2f; //scale up first, since we based off center
                                            newDims *= 0.01f;

                                            //calculate the center of the trigger from hitlocation
                                            Vector3 center = hit.transform.position;

                                            //calculate bounds from dims and center
                                            Vector3 size = new Vector3(newDims.x, newDims.y, newDims.z);

                                            Bounds boundsDims = new Bounds(center, size);

                                            selectionBox.transform.position = boundsDims.center;
                                            selectionBox.transform.localScale = boundsDims.size;


                                        }
                                    }
                                }
                            }
                        }

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
