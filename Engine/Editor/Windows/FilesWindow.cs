using System.Diagnostics;
using System.Numerics;
using System.Text;

using Hexa.NET.ImGui;

namespace Concrete;

public static unsafe class FilesWindow
{
    public static string selectedFileOrDir = null;
    public static string hoveredFileOrDir = null;

    static List<(string item, string dest)> movequeue = [];

    public static bool hovered = false;

    static string[] fileRenderExclusions = [".guid", ".csproj"];
    static string[] folderRenderExclusions = ["bin", "obj"];

    static string newScriptName = "";
    static bool openCreateScriptModalRequest = false;
    static string newScriptParentDirectory;

    public static void Draw(float deltaTime)
    {
        hoveredFileOrDir = null;

        foreach (var tuple in movequeue)
        {
            string item_path = tuple.item;
            string dest_path = tuple.dest;
            string item_path_moved = Path.Combine(dest_path, Path.GetFileName(item_path));

            if (item_path == item_path_moved) continue;

            // item is file
            if (File.Exists(item_path))
            {
                string extension = Path.GetExtension(item_path);

                // if file is asset
                if (extension != ".guid")
                {
                    string guid_path = AssetDatabase.GuidPathFromAssetPath(item_path);
                    string guid_path_moved = Path.Combine(dest_path, Path.GetFileName(guid_path));
                    File.Move(item_path, item_path_moved); // move asset file
                    if (File.Exists(guid_path)) File.Move(guid_path, guid_path_moved); // move guid file

                    AssetDatabase.Rebuild();
                }

                // if file is guid
                if (extension == ".guid")
                {
                    string asset_path = AssetDatabase.AssetPathFromGuidPath(item_path);
                    string asset_path_moved = Path.Combine(dest_path, Path.GetFileName(asset_path));

                    File.Move(item_path, item_path_moved); // move guid file
                    if (File.Exists(asset_path)) File.Move(asset_path, asset_path_moved); // move asset file

                    AssetDatabase.Rebuild();
                }
            }

            // item is directory
            if (Directory.Exists(item_path))
            {
                Directory.Move(item_path, item_path_moved);

                AssetDatabase.Rebuild();
            }
        }
        movequeue.Clear();

        ImGui.Begin("\uf07c Files");

        hovered = ImGui.IsWindowHovered();

        if (ProjectManager.loadedProjectFilePath != null)
        {
            string root = Path.GetDirectoryName(ProjectManager.loadedProjectFilePath);

            var fbuttonsize = new Vector2(ImGui.GetContentRegionAvail().X / 2 - ImGui.GetStyle().ItemSpacing.X / 2, 0);

            ImGui.BeginDisabled(File.Exists(selectedFileOrDir)); // cant make a folder inside a file
            if (ImGui.Button("New Folder", fbuttonsize))
            {
                string newfolderpath = MakeFolderUnique(root, "folder");
                Directory.CreateDirectory(newfolderpath);
                AssetDatabase.Rebuild();
            }
            ImGui.EndDisabled();

            ImGui.SameLine();

            ImGui.BeginDisabled(selectedFileOrDir == null);
            if (ImGui.Button("Delete", fbuttonsize))
            {
                if (Directory.Exists(selectedFileOrDir)) Directory.Delete(selectedFileOrDir, true);
                else File.Delete(selectedFileOrDir);
                selectedFileOrDir = null;
                AssetDatabase.Rebuild();
            }
            ImGui.EndDisabled();

            ImGui.Separator();

            RenderDirectoryInsides(root);

            ImGui.InvisibleButton("##", ImGui.GetContentRegionAvail());
            string info = DragAndDrop.TargetString("file_path");
            if (info != null) movequeue.Add((info, ProjectManager.projectRoot));

            if (ImGui.BeginPopupContextItem("EmptySpaceRightClickMenu"))
            {
                if (ImGui.MenuItem("New Folder"))
                {
                    string newfolderpath = MakeFolderUnique(root, "folder");
                    Directory.CreateDirectory(newfolderpath);
                    AssetDatabase.Rebuild();
                }

                if (ImGui.MenuItem("New Script"))
                {
                    openCreateScriptModalRequest = true;
                    newScriptParentDirectory = root;
                }

                ImGui.EndPopup();
            }

            if (openCreateScriptModalRequest)
            {
                ImGui.OpenPopup("Create Script");
                openCreateScriptModalRequest = false;
            }

            // new script popup
            ImGui.SetNextWindowSize(new Vector2(256, 0), ImGuiCond.FirstUseEver);
            if (ImGui.BeginPopupModal("Create Script", ImGuiWindowFlags.NoResize))
            {
                ImGui.Text("Name:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##scriptname", ref newScriptName, 100);

                // create button
                bool emptyName = string.IsNullOrWhiteSpace(newScriptName);
                ImGui.BeginDisabled(emptyName);
                if (ImGui.Button("Create"))
                {                    
                    string path = MakeFileUnique(newScriptParentDirectory, newScriptName, ".cs");
                    string template = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "NewScriptTemplate.cs"));
                    string templateWithName = template.Replace("InsertScriptName", newScriptName);
                    File.WriteAllText(path, templateWithName);
                    AssetDatabase.Rebuild();
                    
                    newScriptName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndDisabled();

                ImGui.SameLine();

                // cancel button
                if (ImGui.Button("Cancel"))
                {
                    newScriptName = "";
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            void RenderFile(string path)
            {
                var fileflags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                if (selectedFileOrDir == path) fileflags |= ImGuiTreeNodeFlags.Selected;

                string endname = Path.GetFileName(path);
                ImGui.PushID(path);
                ImGui.TreeNodeEx(endname, fileflags);

                if (ImGui.BeginPopupContextItem("FileRightClickMenu"))
                {
                    if (ImGui.MenuItem("Delete"))
                    {
                        if (File.Exists(path)) File.Delete(path);
                        if (selectedFileOrDir == path) selectedFileOrDir = null;
                        AssetDatabase.Rebuild();
                    }
                    ImGui.EndPopup();
                }

                if (ImGui.IsItemHovered()) hoveredFileOrDir = path;

                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0)) selectedFileOrDir = path;

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                {
                    if (Shell.IsCommandInPath("code")) Shell.Run("code", $"{ProjectManager.projectRoot} {path}");
                    else if (Shell.IsCommandInPath("notepad")) Shell.Run("notepad", path);
                }

                DragAndDrop.SourceString("file_path", path, endname);

                ImGui.PopID();
            }

            void RenderDirectoryAndInsides(string path)
            {
                ImGui.PushID(path);

                var dirflags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnArrow;
                if (selectedFileOrDir == path) dirflags |= ImGuiTreeNodeFlags.Selected;

                string relative = Path.GetRelativePath(root, path).Replace("\\", "/");
                string endname = Path.GetFileName(path);

                bool open = ImGui.TreeNodeEx(endname, dirflags);

                if (ImGui.BeginPopupContextItem("FolderRightClickMenu"))
                {
                    if (ImGui.MenuItem("Delete"))
                    {
                        if (Directory.Exists(path)) Directory.Delete(path, true);
                        if (selectedFileOrDir == path) selectedFileOrDir = null;
                        AssetDatabase.Rebuild();
                    }

                    if (ImGui.MenuItem("New Folder"))
                    {
                        string newfolderpath = MakeFolderUnique(path, "folder");
                        Directory.CreateDirectory(newfolderpath);
                        AssetDatabase.Rebuild();
                    }

                    if (ImGui.MenuItem("New Script"))
                    {
                        openCreateScriptModalRequest = true;
                        newScriptParentDirectory = path;
                    }

                    ImGui.EndPopup();
                }

                if (ImGui.IsItemHovered()) hoveredFileOrDir = path;

                if (ImGui.IsItemClicked()) selectedFileOrDir = path;

                DragAndDrop.SourceString("file_path", path, endname);

                string info = DragAndDrop.TargetString("file_path");
                if (info != null) movequeue.Add((info, path));

                if (open)
                {
                    if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0)) selectedFileOrDir = path;
                    RenderDirectoryInsides(path);
                    ImGui.TreePop();
                }

                ImGui.PopID();
            }

            void RenderDirectoryInsides(string currentPath)
            {
                if (!Directory.Exists(currentPath)) return;

                string[] dirs = Directory.GetDirectories(currentPath);
                for (int i = 0; i < dirs.Length; i++)
                {
                    string name = Path.GetFileName(dirs[i]);
                    if (!folderRenderExclusions.Contains(name)) RenderDirectoryAndInsides(dirs[i]);
                }

                string[] files = Directory.GetFiles(currentPath);
                for (int i = 0; i < files.Length; i++) if (!fileRenderExclusions.Contains(Path.GetExtension(files[i]))) RenderFile(files[i]);
            }
        }

        ImGui.End();
    }

    static string MakeFileUnique(string parentDir, string fileName, string extension)
    {
        var path = Path.Combine(parentDir, fileName + extension);

        for (int i = 0; i < 100; i++)
        {
            if (File.Exists(path)) path = Path.Combine(parentDir, fileName + $"_{i}" + extension);
            else break;
        }

        return path;
    }

    static string MakeFolderUnique(string parentDir, string folderName)
    {
        var path = Path.Combine(parentDir, folderName);

        for (int i = 0; i < 100; i++)
        {
            if (Directory.Exists(path)) path = Path.Combine(parentDir, folderName + $"_{i}");
            else break;
        }

        return path;
    }
}