using UnityEngine;
using ImGuiNET;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Drawing;
using System;
using LithFAQ;
using System.Collections.Generic;
using System.Data;

public class ObjectList : MonoBehaviour
{
    // Some references to private controllers that manage health, movement, enemies etc.
    public Importer importer;

    public bool bShowObjectList = true;
    public List<string> szWorldObjectNames = new List<string>();
    public int iSelectedObject = 0;
    public int iPrevSelectedObject = 0;

    public void Start()
    {
        if (importer == null)
        {
            importer = FindAnyObjectByType<Importer>();
        }
    }

    void OnEnable()
    {
        ImGuiUn.Layout += OnLayout;
    }

    private void OnLayout()
    {
        if (!bShowObjectList)
            return;

        Vector2 screenSize = Camera.main.pixelRect.size;


        // Begin the window context
        ImGui.Begin("Object Viewer", ref bShowObjectList);

        if (szWorldObjectNames.Count <= 0)
        {
            if (importer.DatReader != null)
            {
                IDATReader reader = (IDATReader)importer.DatReader;

                var temp = reader.GetWorldObjects();

                foreach (var item in temp.obj)
                {
                    szWorldObjectNames.Add(item.options["Name"].ToString());
                }
            }
        }

        ImGui.PushItemWidth(-1);
        ImGui.ListBox("Objects", ref iSelectedObject, szWorldObjectNames.ToArray(), szWorldObjectNames.Count, szWorldObjectNames.Count);
            
       
        ImGui.End();

        if(iPrevSelectedObject != iSelectedObject)
        {
            iPrevSelectedObject = iSelectedObject;

            float x, y, z;

            if (importer.DatReader != null)
            {
                IDATReader reader = (IDATReader)importer.DatReader;

                var temp = reader.GetWorldObjects();

                LTTypes.LTVector test = (LTTypes.LTVector)temp.obj[iSelectedObject].options["Pos"];
                Vector3 newPos = test;

                Vector3 direction = Camera.main.transform.forward; // Get the direction the camera is facing
                Vector3 finalPos = newPos - direction * 192; // Calculate the final position




                Camera.main.transform.position = finalPos * 0.01f;
            }

           // Camera.main.transform.position = vNewPos;
        }

    }

    // Unsubscribe as well
    void OnDisable()
    {
        ImGuiUn.Layout -= OnLayout;
    }


}