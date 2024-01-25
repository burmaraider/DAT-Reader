using UnityEngine;
using ImGuiNET;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Drawing;
using System;

public class MainGui : MonoBehaviour
{
    // Some references to private controllers that manage health, movement, enemies etc.
    public Importer importer;
    public Vector2 vHelpWindowSize = new Vector2(300, 300);
    public Vector2 vToggleOptionsSize = new Vector2(300, 300);
    public Texture2D imguiTexture;

    public void Start()
    {
        if (importer == null)
        {
            importer = FindAnyObjectByType<Importer>();
        }
    }

    public void Reset()
    {
        bToggleObjects = true;
        bToggleBlockers = true;
        bToggleVolumes = true;
        bToggleBSP = true;
        bToggleShadows = false;
        bObjectViewer = false;

        var objectList = FindAnyObjectByType<ObjectList>();
        Destroy(objectList);
    }

    // Subscribe to Layout events
    void OnEnable()
    {
        ImGuiUn.Layout += OnLayout;
        UIActionManager.OnPostLoadLevel += OnLoadLevel;
        UIActionManager.OnReset += Reset;
        
    }

    // Unsubscribe as well
    void OnDisable()
    {
        ImGuiUn.Layout -= OnLayout;
        UIActionManager.OnPostLoadLevel -= OnLoadLevel;
        UIActionManager.OnReset -= Reset;
    }

    private void OnLoadLevel()
    {
        this.transform.gameObject.AddComponent<ObjectList>();
    }

    // Some bools for controlling different windows
    private bool bLoadLevelClicked = false;
    private bool bClearLevelClicked = false;
    private bool bQuitClicked = false;
    private bool bShowHelp = true;
    private bool bShowToggleOptions = true;
    private bool bShowAboutWindow = false;
    private bool bObjectViewer = false;

    //Options
    private float fAmbientSlider = 1.0f;
    private bool bToggleObjects = true;
    private bool bToggleBlockers = true;
    private bool bToggleVolumes = true;
    private bool bToggleBSP = true;
    private bool bToggleShadows = false;

    // Controll everything from the function that subscribes to Layout events
    void OnLayout()
    {

        ShowMainHeaderBar();

        // The IF checks is what controls whether the window is actually displayed
        if (bLoadLevelClicked)
        {
            bLoadLevelClicked = false;
            UIActionManager.OnPreLoadLevel?.Invoke();
        }
        if (bClearLevelClicked)
        {
            bClearLevelClicked = false;
            UIActionManager.OnPreClearLevel?.Invoke();
        }
        if (bQuitClicked)
        {
            Application.Quit();
        }
        if (bShowHelp)
        {
            ShowHelpMenu();
        }
        if (bShowToggleOptions)
        {
            ShowToggleOptions();
        }

        if (bShowAboutWindow)
        {
            ShowAboutWindow();
        }

        Camera.main.GetComponent<ObjectPicker>().ToggleBlockers(bToggleBlockers);
        Camera.main.GetComponent<ObjectPicker>().ToggleBSP(bToggleBSP);
        Camera.main.GetComponent<ObjectPicker>().ToggleShadows(bToggleShadows);
        Camera.main.GetComponent<ObjectPicker>().ToggleVolumes(bToggleVolumes);
        Camera.main.GetComponent<ObjectPicker>().ToggleObjects(bToggleObjects);
        importer.gameObject.GetComponent<Controller>().ChangeAmbientLighting(fAmbientSlider);

        ImGui.End();
    }

    private void ShowAboutWindow()
    {
        ImGui.Begin("About", ref bShowAboutWindow);

        ImGui.TextWrapped("DAT Viewer 0.2.2 \r\nThis tool will open .DAT LithTech engine world files and display them. Eventually the aim of this w");

        ImGui.End();
    }

    private void ShowToggleOptions()
    {
        Vector2 screenSize = Camera.main.pixelRect.size;

        ImGui.SetNextWindowPos(new Vector2(vHelpWindowSize.x + ImGui.GetStyle().WindowPadding.x, screenSize.y - vToggleOptionsSize.y));
        ImGui.SetNextWindowSize(vToggleOptionsSize);

        // Begin the window context
        ImGui.Begin("toggle Menu", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

        ImGui.Columns(2, "Toggle Columns");
        //Column 1
        ImGui.Checkbox("Show/Hide Objects", ref bToggleObjects);
        ImGui.Checkbox("Show/Hide Blockers", ref bToggleBlockers);
        ImGui.Checkbox("Show/Hide Volumes", ref bToggleVolumes);

        ImGui.NextColumn();
        ImGui.SliderFloat("Ambient", ref fAmbientSlider, 0.0f, 2.0f);
        ImGui.Checkbox("Show/Hide BSP", ref bToggleBSP);
        ImGui.Checkbox("Enable/Disable Shadows", ref bToggleShadows);

        ImGui.End();

    }

    private void ShowHelpMenu()
    {
        Vector2 screenSize = Camera.main.pixelRect.size;

        ImGui.SetNextWindowPos(new Vector2(0, screenSize.y - vHelpWindowSize.y));
        ImGui.SetNextWindowSize(vHelpWindowSize);

        // Begin the window context
        ImGui.Begin("Help Menu", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

        // Display some text in the window
        ImGui.TextWrapped("Right Click - Enable Freefly Camera\r\nWSAD - Move Camera\r\nQ & E - Lower and raise Camera\r\nShift - Move Faster\r\nDel - Delete Selected Object");

        ImGui.End();
    }

    // Top bar creation
    private void ShowMainHeaderBar()
    {
        ImGui.BeginMainMenuBar();

        if (ImGui.BeginMenu("File"))
        {
            ImGui.MenuItem("Load Level", null, ref bLoadLevelClicked);
            ImGui.MenuItem("Clear Level", null, ref bClearLevelClicked);
            ImGui.Separator();
            ImGui.MenuItem("Quit", null, ref bQuitClicked);
            ImGui.EndMenu();
        }
        ImGui.MenuItem("Help", null, ref bShowAboutWindow);
        ImGui.EndMainMenuBar();
    }
}