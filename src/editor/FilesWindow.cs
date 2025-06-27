using System.Numerics;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

namespace Concrete;

public static unsafe class FilesWindow
{
    private static string selectedFileOrDir = null;

    public static void Draw(float deltaTime)
    {
        ImGui.Begin("Files");

        if (ProjectManager.loadedProjectFilePath != null)
        {
            string root = Path.GetDirectoryName(ProjectManager.loadedProjectFilePath);

            var fbuttonsize = new Vector2(ImGui.GetContentRegionAvail().X / 2 - ImGui.GetStyle().ItemSpacing.X / 2, 0);

            ImGui.BeginDisabled(File.Exists(selectedFileOrDir)); // cant make a folder inside a file
            if (ImGui.Button("New Folder", fbuttonsize))
            {
                // make folder in root if no dir is selected
                string parentfolder = Directory.Exists(selectedFileOrDir) ? selectedFileOrDir : root;
                string newfolderpath = parentfolder + "/folder";

                // add number if folder name is already in use
                for (int i = 0; i < 20; i++)
                {
                    if (Directory.Exists(newfolderpath)) newfolderpath = parentfolder + "/folder (" + i.ToString() + ")";
                    else break;
                }
                
                // create the folder
                Directory.CreateDirectory(newfolderpath);
            }
            ImGui.EndDisabled();

            ImGui.SameLine();

            ImGui.BeginDisabled(selectedFileOrDir == null);
            if (ImGui.Button("Delete", fbuttonsize))
            {
                if (Directory.Exists(selectedFileOrDir)) Directory.Delete(selectedFileOrDir, true);
                else File.Delete(selectedFileOrDir);
                selectedFileOrDir = null;
            }
            ImGui.EndDisabled();

            ImGui.Separator();

            RenderDirectoryInsides(root);

            void RenderFile(string path)
            {
                var fileflags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                if (selectedFileOrDir == path) fileflags |= ImGuiTreeNodeFlags.Selected;

                string filename = Path.GetFileName(path);
                ImGui.TreeNodeEx(filename, fileflags);
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0)) selectedFileOrDir = path;
            }

            void RenderDirectoryAndInsides(string path)
            {
                ImGui.PushID(path);

                var dirflags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnArrow;
                if (selectedFileOrDir == path) dirflags |= ImGuiTreeNodeFlags.Selected;

                string relative = Path.GetRelativePath(root, path).Replace("\\", "/");

                if (ImGui.TreeNodeEx(relative + "/", dirflags))
                {
                    if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0)) selectedFileOrDir = path;
                    RenderDirectoryInsides(path);
                    ImGui.TreePop();
                }

                ImGui.PopID();
            }

            void RenderDirectoryInsides(string currentPath)
            {
                string[] dirs = Directory.GetDirectories(currentPath);
                for (int i = 0; i < dirs.Length; i++) RenderDirectoryAndInsides(dirs[i]);

                string[] files = Directory.GetFiles(currentPath);
                for (int i = 0; i < files.Length; i++) RenderFile(files[i]);
            }
        }

        ImGui.End();
    }
}