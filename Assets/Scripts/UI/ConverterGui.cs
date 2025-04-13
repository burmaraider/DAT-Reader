using UnityEngine;
using ImGuiNET;

public class ConverterGui : MonoBehaviour
{
    // Some references to private controllers that manage health, movement, enemies etc.
    public Importer importer;
    public Texture2D imguiTexture;

    // Some bools for controlling different windows
    private bool bExportTextures = false;
    private bool bQuitClicked = false;

    public void Start()
    {
        if (importer == null)
        {
            importer = FindAnyObjectByType<Importer>();
        }
    }

    public void Reset()
    {
        bExportTextures = true;
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

    // Controll everything from the function that subscribes to Layout events
    void OnLayout()
    {
        ShowMainHeaderBar();

        // The IF checks is what controls whether the window is actually displayed
        if (bExportTextures)
        {
            bExportTextures = false;
            UIActionManager.OnExportTextures?.Invoke();
        }
        if (bQuitClicked)
        {
            Application.Quit();
        }

        ImGui.End();
    }

    // Top bar creation
    private void ShowMainHeaderBar()
    {
        ImGui.SetNextWindowBgAlpha(1.0f);
        ImGui.BeginMainMenuBar();
        if (ImGui.BeginMenu("File"))
        {
            ImGui.MenuItem("Export Textures", null, ref bExportTextures);
            ImGui.MenuItem("Quit", null, ref bQuitClicked);
            ImGui.EndMenu();
        }
        ImGui.EndMainMenuBar();
        ImGui.SetNextWindowBgAlpha(ImGui.GetStyle().Alpha);
    }
}