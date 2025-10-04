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

        ImGui.Begin("Files");

        hovered = ImGui.IsWindowHovered();

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

            if (ImGui.BeginPopupContextItem("FolderRightClickMenu"))
            {
                if (ImGui.MenuItem("New Folder"))
                {
                    string newfolderpath = Path.Combine(ProjectManager.projectRoot, "folder");
                    for (int i = 0; i < 20; i++)
                    {
                        if (Directory.Exists(newfolderpath)) newfolderpath = newfolderpath + $" ({i})";
                        else break;
                    }
                    Directory.CreateDirectory(newfolderpath);
                    AssetDatabase.Rebuild();
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
                        string newfolderpath = path + "/folder";
                        for (int i = 0; i < 20; i++)
                        {
                            if (Directory.Exists(newfolderpath)) newfolderpath = newfolderpath + $" ({i})";
                            else break;
                        }
                        Directory.CreateDirectory(newfolderpath);
                        AssetDatabase.Rebuild();
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
}