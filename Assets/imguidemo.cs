using UnityEngine;
using ImGuiNET;
using System;
using UnityEngine.UI;

public class MainGui : MonoBehaviour
{
    // Some references to private controllers that manage health, movement, enemies etc.
    public Importer importer;
    public Vector2 vHelpWindowSize = new Vector2(300, 300);
    public Vector2 vToggleOptionsSize = new Vector2(300, 300);

    public void Start()
    {
        if (importer == null)
        {
            importer = GameObject.FindAnyObjectByType<Importer>();
        }
    }

    public void Reset()
    {
        bToggleObjects = true;
        bToggleBlockers = true;
        bToggleVolumes = true;
        bToggleBSP = true;
        bToggleShadows = false;
    }

    // Subscribe to Layout events
    void OnEnable()
    {
        ImGuiUn.Layout += OnLayout;
    }
    // Unsubscribe as well
    void OnDisable()
    {
        ImGuiUn.Layout -= OnLayout;
    }

    // Some bools for controlling different windows
    private bool bLoadLevelClicked = false;
    private bool bClearLevelClicked = false;
    private bool bQuitClicked = false;
    private bool bShowHelp = true;
    private bool bShowToggleOptions = true;

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
            LoadLevelClicked();
        }
        if (bClearLevelClicked)
        {
            ClearLevelClicked();
        }
        if (bQuitClicked)
        {
            QuitClicked();
        }
        if(bShowHelp)
        {
            ShowHelpMenu();
        }
        if(bShowToggleOptions)
        {
            ShowToggleOptions();
        }

        Camera.main.GetComponent<ObjectPicker>().ToggleBlockers(bToggleBlockers);
        Camera.main.GetComponent<ObjectPicker>().ToggleBSP(bToggleBSP);
        Camera.main.GetComponent<ObjectPicker>().ToggleShadows(bToggleShadows);
        Camera.main.GetComponent<ObjectPicker>().ToggleVolumes(bToggleVolumes);
        Camera.main.GetComponent<ObjectPicker>().ToggleObjects(bToggleObjects);
        importer.gameObject.GetComponent<Controller>().ChangeAmbientLighting(fAmbientSlider);

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
        ImGui.EndMainMenuBar();
    }

    private void LoadLevelClicked()
    {
        bLoadLevelClicked = false;
        if (importer != null)
        {
            importer.OpenDAT();
        }
    }

    private void ClearLevelClicked()
    {
        bClearLevelClicked = false;
        if(importer != null)
        {
            importer.ClearLevel();
        }
    }

    private void QuitClicked()
    {
        bQuitClicked = false;
        if (importer != null)
        {
            Application.Quit();
        }
    }
}