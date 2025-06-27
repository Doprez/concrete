using System.Numerics;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

namespace Concrete;

public static unsafe class FilesWindow
{
    private static string selectedFileOrDir = null;

    static List<(string item, string dest)> movequeue = [];

    public static void Draw(float deltaTime)
    {
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
                }

                // if file is guid
                if (extension == ".guid")
                {
                    string asset_path = AssetDatabase.AssetPathFromGuidPath(item_path);
                    string asset_path_moved = Path.Combine(dest_path, Path.GetFileName(asset_path));

                    File.Move(item_path, item_path_moved); // move guid file
                    if (File.Exists(asset_path)) File.Move(asset_path, asset_path_moved); // move asset file
                }
            }

            // item is directory
            if (Directory.Exists(item_path))
            {
                Directory.Move(item_path, item_path_moved);
            }
        }
        movequeue.Clear();

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

                string endname = Path.GetFileName(path);
                ImGui.PushID(path);
                ImGui.TreeNodeEx(endname, fileflags);
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0)) selectedFileOrDir = path;

                if (ImGui.BeginDragDropSource())
                {
                    byte[] payload = Encoding.UTF8.GetBytes(path);
                    fixed (byte* ptr = payload) ImGui.SetDragDropPayload("file_path", ptr, (nuint)payload.Length);
                    ImGui.Text(endname);
                    ImGui.EndDragDropSource();
                }

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
                if (ImGui.IsItemClicked()) selectedFileOrDir = path;

                if (ImGui.BeginDragDropSource())
                {
                    byte[] payload = Encoding.UTF8.GetBytes(path);
                    fixed (byte* ptr = payload) ImGui.SetDragDropPayload("file_path", ptr, (nuint)payload.Length);
                    ImGui.Text(endname);
                    ImGui.EndDragDropSource();
                }

                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("file_path");
                    if (!payload.IsNull)
                    {
                        string file = Encoding.UTF8.GetString((byte*)payload.Data, payload.DataSize);
                        movequeue.Add((file, path));
                    }
                    ImGui.EndDragDropTarget();
                }

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
                string[] dirs = Directory.GetDirectories(currentPath);
                for (int i = 0; i < dirs.Length; i++) RenderDirectoryAndInsides(dirs[i]);

                string[] files = Directory.GetFiles(currentPath);
                for (int i = 0; i < files.Length; i++) RenderFile(files[i]);
            }
        }

        ImGui.End();
    }
}