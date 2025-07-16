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
        uint dockspace = ImGui.DockSpaceOverViewport((ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.NoWindowMenuButton);
        if (!dockbuilderInitialized)
        {
            uint left, mid, right;
            uint topleft, lowleft;
            uint topmid, lowmid;

            ImGuiP.DockBuilderSplitNode(dockspace, ImGuiDir.Left, 0.25f, &left, &mid);
            ImGuiP.DockBuilderSplitNode(mid, ImGuiDir.Left, 0.66f, &mid, &right);
            ImGuiP.DockBuilderSplitNode(left, ImGuiDir.Up, 0.5f, &topleft, &lowleft);
            ImGuiP.DockBuilderSplitNode(mid, ImGuiDir.Down, 0.3f, &lowmid, &topmid);

            ImGuiP.DockBuilderDockWindow("Scene", topmid);
            ImGuiP.DockBuilderDockWindow("Game", topmid);
            ImGuiP.DockBuilderDockWindow("Metrics", topmid);
            ImGuiP.DockBuilderDockWindow("Hierarchy", topleft);
            ImGuiP.DockBuilderDockWindow("Files", lowleft);
            ImGuiP.DockBuilderDockWindow("Inspector", right);
            ImGuiP.DockBuilderDockWindow("Console", lowmid);

            ImGuiP.DockBuilderFinish(dockspace);
            dockbuilderInitialized = true;
        }
    }
}