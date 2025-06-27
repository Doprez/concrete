using System.Numerics;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

namespace Concrete;

public static unsafe class Editor
{
    private static bool dockbuilderInitialized = false;

    public static void Render(float deltaTime)
    {
        SetupDockSpace();
        MainMenuBar.Draw(deltaTime);
        SceneWindow.Draw(deltaTime);
        GameWindow.Draw(deltaTime);
        HierarchyWindow.Draw(deltaTime);
        FilesWindow.Draw(deltaTime);
        ConsoleWindow.Draw(deltaTime);
        InspectorWindow.Draw(deltaTime);
        MetricsWindow.Draw(deltaTime);
    }

    private static void SetupDockSpace()
    {
        int dockspace = ImGui.DockSpaceOverViewport((ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.NoWindowMenuButton);
        if (!dockbuilderInitialized)
        {
            int left, mid, right;
            int topleft, lowleft;
            int topmid, lowmid;

            ImGui.DockBuilderSplitNode(dockspace, ImGuiDir.Left, 0.25f, &left, &mid);
            ImGui.DockBuilderSplitNode(mid, ImGuiDir.Left, 0.66f, &mid, &right);
            ImGui.DockBuilderSplitNode(left, ImGuiDir.Up, 0.5f, &topleft, &lowleft);
            ImGui.DockBuilderSplitNode(mid, ImGuiDir.Down, 0.3f, &lowmid, &topmid);

            ImGui.DockBuilderDockWindow("Scene", topmid);
            ImGui.DockBuilderDockWindow("Game", topmid);
            ImGui.DockBuilderDockWindow("Metrics", topmid);
            ImGui.DockBuilderDockWindow("Hierarchy", topleft);
            ImGui.DockBuilderDockWindow("Files", lowleft);
            ImGui.DockBuilderDockWindow("Inspector", right);
            ImGui.DockBuilderDockWindow("Console", lowmid);

            ImGui.DockBuilderFinish(dockspace);
            dockbuilderInitialized = true;
        }
    }
}