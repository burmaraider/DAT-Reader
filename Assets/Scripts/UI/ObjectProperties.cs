using UnityEngine;
using ImGuiNET;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Drawing;
using System;
using LithFAQ;
using System.Collections.Generic;
using System.Data;

public class ObjectProperties : MonoBehaviour
{
    public Importer importer;
    public WorldObjects worldObjects;
    public bool bShowObjectList = true;
    public int nSelectedItem;

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
        UIActionManager.OnSelectObject += OnSelectedItem;
        UIActionManager.OnReset += Reset;
    }


    public void OnDisable()
    {
        ImGuiUn.Layout -= OnLayout;
        UIActionManager.OnSelectObject -= OnSelectedItem;
        UIActionManager.OnReset -= Reset;
    }

    private void Reset()
    {
        nSelectedItem = 0;
        worldObjects = null;
    }

    private void OnSelectedItem(int nItem)
    {
        nSelectedItem = nItem;

        var reader = (IDATReader)importer.DatReader;
        worldObjects = reader.GetWorldObjects();
    }

    private void OnLayout()
    {

        if (!bShowObjectList)
            return;

        Vector2 screenSize = Camera.main.pixelRect.size;

        // Begin the window context
        ImGui.SetNextWindowSize(new Vector2(325, screenSize.y - 20), ImGuiCond.Once);
        ImGui.SetNextWindowPos(new Vector2(screenSize.x - 325, 20), ImGuiCond.Once);
        ImGui.Begin("Object Properties", ref bShowObjectList);

        //check iff null

        if (worldObjects == null)
        {
            ImGui.End();
            return;
        }


        ImGui.Text("Type: " + worldObjects.obj[nSelectedItem].objectName);


        for (int i = 0; i < worldObjects.obj[nSelectedItem].options.Count; i++)
        {
            var keys = new List<string>(worldObjects.obj[nSelectedItem].options.Keys);
            var values = new List<object>(worldObjects.obj[nSelectedItem].options.Values);

            if (keys[i].Contains("TrueBase", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (values[i].GetType() == typeof(string))
            {
                string str = (string)values[i];
                ImGui.InputText(keys[i], ref str, 100);

                if (str != (string)values[i])
                {
                    worldObjects.obj[nSelectedItem].options[keys[i]] = str;
                }
            }

            if (values[i].GetType() == typeof(int))
            {
                int n = (int)values[i];
                ImGui.InputInt(keys[i], ref n);

                if (n != (int)values[i])
                {
                    worldObjects.obj[nSelectedItem].options[keys[i]] = n;
                }
            }

            if (values[i].GetType() == typeof(float))
            {
                float f = (float)values[i];
                ImGui.DragFloat(keys[i], ref f);

                if (f != (float)values[i])
                {
                    worldObjects.obj[nSelectedItem].options[keys[i]] = f;

                    if (keys[i].Contains("lightradius", StringComparison.OrdinalIgnoreCase))
                    {
                        UIActionManager.OnChangeLightRadius?.Invoke(f, (string)worldObjects.obj[nSelectedItem].options["Name"]);
                    }
                }
            }

            if (values[i].GetType() == typeof(UInt32))
            {
                float f = (int)((UInt32)values[i]);
                ImGui.InputFloat(keys[i], ref f);
                if (f != (int)((UInt32)values[i]))
                {
                    worldObjects.obj[nSelectedItem].options[keys[i]] = (UInt32)f;
                }
            }

            if (values[i].GetType() == typeof(bool))
            {
                bool b = (bool)values[i];
                ImGui.Checkbox(keys[i], ref b);
                if (b != (bool)values[i])
                {
                    worldObjects.obj[nSelectedItem].options[keys[i]] = b;
                }
            }

            if (values[i].GetType() == typeof(LTTypes.LTVector))
            {
                //Are we trying to show a color? if so, lets make it editable with a color picker
                if (keys[i].Contains("color", StringComparison.OrdinalIgnoreCase))
                {
                    var color = (LTTypes.LTVector)values[i];
                    Vector3 c = new Vector3(color.X / 255, color.Y / 255, color.Z / 255);
                    ImGui.ColorEdit3(keys[i], ref c);
                    LTTypes.LTVector newColor = new LTTypes.LTVector(c.x * 255, c.y * 255, c.z * 255);
                    worldObjects.obj[nSelectedItem].options[keys[i]] = newColor;
                    var col = new UnityEngine.Color(c.x, c.y, c.z);


                    //dont worry about the outer color
                    if (!keys[i].Contains("outer", StringComparison.OrdinalIgnoreCase))
                    {
                        UIActionManager.OnChangeLightColor?.Invoke(col, (string)worldObjects.obj[nSelectedItem].options["Name"]);
                    }

                    continue;
                }

                Vector3 v = (LTTypes.LTVector)values[i];
                ImGui.InputFloat3(keys[i], ref v);
                worldObjects.obj[nSelectedItem].options[keys[i]] = (LTTypes.LTVector)v;
            }

            if (values[i].GetType() == typeof(LTTypes.LTRotation))
            {
                var radians = (LTTypes.LTRotation)values[i];

                //convert to Vector3 degrees
                Vector3 degrees = new Vector3(
                    (float)(radians.X * (180 / Math.PI)),
                    (float)(radians.Y * (180 / Math.PI)),
                    (float)(radians.Z * (180 / Math.PI))
                );

                ImGui.InputFloat3(keys[i], ref degrees);

                // Convert back to radians and update the dictionary
                LTTypes.LTRotation newRotation = new LTTypes.LTRotation(
                    (LTTypes.LTFloat)(degrees.x * (Math.PI / 180)),
                    (LTTypes.LTFloat)(degrees.y * (Math.PI / 180)),
                    (LTTypes.LTFloat)(degrees.z * (Math.PI / 180)),
                    radians.W
                );

                if (newRotation != radians)
                {
                    worldObjects.obj[nSelectedItem].options[keys[i]] = newRotation;
                }

            }

        }
        ImGui.End();
    }
}