﻿using UnityEngine;
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
    public List<string> szWorldObjectNameList = new List<string>();
    public int nSelectedObject = 0;
    public int nPrevSelectedObject = 0;

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
        UIActionManager.OnSelectObjectIn3D += OnSelectObjectIn3D;
    }


    void OnDisable()
    {
        ImGuiUn.Layout -= OnLayout;
        UIActionManager.OnSelectObjectIn3D -= OnSelectObjectIn3D;
    }

    private void OnSelectObjectIn3D(string szName)
    {
        //strip _obj off the end
        szName = szName.Replace("_obj", "");

        if(szWorldObjectNameList.Count  > 0)
        {
            int i = 0;
            foreach (var szWorldObject in szWorldObjectNameList)
            {
                if(szWorldObject.Equals(szName))
                {
                    nSelectedObject = i;
                }
                i++;
            }
        }
    }


    private void OnLayout()
    {
        if (!bShowObjectList)
            return;

        Vector2 screenSize = Camera.main.pixelRect.size;


        // Begin the window context
        ImGui.SetNextWindowSize(new Vector2(250, screenSize.y - 100), ImGuiCond.Once);
        ImGui.SetNextWindowPos(new Vector2(0, 20), ImGuiCond.Once);
        ImGui.Begin("Object Viewer", ref bShowObjectList);

        GetWorldObjects();

        ImGui.PushItemWidth(-1);
        ImGui.ListBox("Objects", ref nSelectedObject, szWorldObjectNameList.ToArray(), szWorldObjectNameList.Count, szWorldObjectNameList.Count);


        ImGui.End();

        if (nPrevSelectedObject != nSelectedObject)
        {
            nPrevSelectedObject = nSelectedObject;

            UIActionManager.OnSelectObject?.Invoke(nSelectedObject);

            if (importer.DatReader != null)
            {
                IDATReader reader = (IDATReader)importer.DatReader;

                var temp = reader.GetWorldObjects();

                LTTypes.LTVector test = (LTTypes.LTVector)temp.obj[nSelectedObject].options["Pos"];
                Vector3 newPos = test;

                Vector3 direction = Camera.main.transform.forward; // Get the direction the camera is facing
                Vector3 finalPos = newPos - direction * 192; // Calculate the final position

                Camera.main.transform.position = finalPos * 0.01f;
            }
        }

    }

    private void GetWorldObjects()
    {
        if (szWorldObjectNameList.Count <= 0)
        {
            if (importer.DatReader != null)
            {
                IDATReader reader = (IDATReader)importer.DatReader;

                var temp = reader.GetWorldObjects();

                foreach (var item in temp.obj)
                {
                    szWorldObjectNameList.Add(item.options["Name"].ToString());
                }
            }
        }
    }
}