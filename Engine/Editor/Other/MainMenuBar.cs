using System.Diagnostics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;

namespace Concrete;

public static unsafe class MainMenuBar
{
    private static bool openScene = false;
    private static bool saveScene = false;
    private static bool openProject = false;
    private static bool saveProject = false;
    private static bool newProject = false;
    private static string fileDialogPath = "";
    
    public static void Draw(float deltaTime)
    {
        // file dialog
        if (openScene) FileDialog.Show(ref openScene, ref fileDialogPath, true, () => SceneManager.LoadScene(fileDialogPath));
        if (saveScene) FileDialog.Show(ref saveScene, ref fileDialogPath, true, () => SceneManager.SaveScene(fileDialogPath));
        if (openProject) FileDialog.Show(ref openProject, ref fileDialogPath, true, () => ProjectManager.LoadProject(fileDialogPath));
        if (saveProject) FileDialog.Show(ref saveProject, ref fileDialogPath, true, () => ProjectManager.SaveProject(fileDialogPath));
        if (newProject) FileDialog.Show(ref newProject, ref fileDialogPath, true, () => ProjectManager.CreateAndLoadNewProject(fileDialogPath));

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Project"))
            {
                if (ImGui.MenuItem("New")) newProject = true;
                ImGui.BeginDisabled(ProjectManager.loadedProjectData == null);
                if (ImGui.MenuItem("Save")) saveProject = true;
                ImGui.EndDisabled();
                if (ImGui.MenuItem("Open")) openProject = true;
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Scene"))
            {
                if (ImGui.MenuItem("New")) SceneManager.CreateAndLoadNewScene();
                ImGui.BeginDisabled(Scene.Current == null);
                if (ImGui.MenuItem("Save")) saveScene = true;
                ImGui.EndDisabled();
                if (ImGui.MenuItem("Open")) openScene = true;
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("Open engine repository")) OpenLink("https://github.com/sjoerdev/concrete");
                ImGui.EndMenu();

                void OpenLink(string url)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Process.Start(new ProcessStartInfo(url) { FileName = url, UseShellExecute = true, });
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) Process.Start("xdg-open", url);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) Process.Start("open", url);
                }
            }
            
            ImGui.EndMainMenuBar();
        }
    }
}