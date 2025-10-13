using Hexa.NET.ImGui;

namespace Concrete;

public static unsafe class ConsoleWindow
{
    private static string consoleSearchPrompt = "";

    public static void Draw(float deltaTime)
    {
        ImGui.Begin("\uf120 Console", ImGuiWindowFlags.NoScrollbar);

        var consoleButtonWidth = 80;
        var searchBoxWidth = ImGui.GetContentRegionAvail().X - consoleButtonWidth - ImGui.GetStyle().ItemSpacing.X;

        ImGui.SetNextItemWidth(searchBoxWidth);
        ImGui.InputText("##search", ref consoleSearchPrompt, 100);
        ImGui.SameLine();
        if (ImGui.Button("clear", new(consoleButtonWidth, 0))) Debug.Clear();

        ImGui.Separator();

        ImGui.BeginChild("ConsoleHistoryScrollRegion", ImGui.GetContentRegionAvail());
        var history = Debug.history;
        
        for (int index = 0; index < history.Count; index++)
        {
            string line = history[index];
            if (line.ToLower().Contains(consoleSearchPrompt.ToLower()))
            {
                ImGui.PushID(index);
                if (ImGui.Selectable(line))
                {
                    ImGui.SetClipboardText(line);
                }
                ImGui.PopID();
            }
        }

        // ImGui.SetScrollY(ImGui.GetScrollMaxY());
        ImGui.EndChild();

        ImGui.End();
    }
}