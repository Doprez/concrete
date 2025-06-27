using System.Numerics;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

namespace Concrete;

public static unsafe class ConsoleWindow
{
    private static string consoleSearchPrompt = "";

    public static void Draw(float deltaTime)
    {
        ImGui.Begin("Console", ImGuiWindowFlags.NoScrollbar);

        var consoleButtonWidth = 80;
        var consoleButtonWithSpacingWidth = consoleButtonWidth + ImGui.GetStyle().ItemSpacing.X;
        var searchBoxWidth = ImGui.GetContentRegionAvail().X - (consoleButtonWithSpacingWidth * 2f);

        ImGui.SetNextItemWidth(searchBoxWidth);
        ImGui.InputText("##search", ref consoleSearchPrompt, 100);
        ImGui.SameLine();
        if (ImGui.Button("clear", new(consoleButtonWidth, 0))) consoleSearchPrompt = "";
        ImGui.SameLine();
        if (ImGui.Button("reset", new(consoleButtonWidth, 0))) Debug.Clear();

        //if (ImGui.Button("test", new(consoleButtonWidth, 0))) Debug.Log("asdfasdfasdf_" + new Random().Next(88));

        ImGui.Separator();

        ImGui.BeginChild("ConsoleHistoryScrollRegion", ImGui.GetContentRegionAvail());
        var history = Debug.history;
        
        for (int index = 0; index < history.Count; index++)
        {
            string line = history[index];
            if (line.ToLower().Contains(consoleSearchPrompt.ToLower())) if (ImGui.Selectable(line)) ImGui.SetClipboardText(line);
        }

        // ImGui.SetScrollY(ImGui.GetScrollMaxY());
        ImGui.EndChild();

        ImGui.End();
    }
}