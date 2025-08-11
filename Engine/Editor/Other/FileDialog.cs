using System.Numerics;

using Hexa.NET.ImGui;

namespace Concrete;

public static class FileDialog
{
    static string currentPath = ProjectManager.projectRoot;
    
    static string currentFile = "";
    static string currentFolder = "";

    static int currentFileIndex = -1;
    static int currentFolderIndex = -1;

    static string newFolderName = "";
    static string newFileName = "";

    static SortOrder fileNameSortOrder = SortOrder.None;
    static SortOrder sizeSortOrder = SortOrder.None;
    static SortOrder dateSortOrder = SortOrder.None;
    static SortOrder typeSortOrder = SortOrder.None;

    static void DeSelectFile()
    {
        currentFile = "";
        currentFileIndex = -1;
    }

    static void DeSelectFolder()
    {
        currentFolder = "";
        currentFolderIndex = -1;
    }

    static void DeSelectAll()
    {
        DeSelectFile();
        DeSelectFolder();
    }

    public static void Show(ref bool open, ref string resultPath, bool singleFile, Action OnChoose = null)
    {
        // return if it shouldnt be open
        if (!open) return;

        // setup the imgui window
        string title = singleFile ? "Select a file" : "Select a folder";
        ImGui.SetNextWindowSize(new Vector2(860, 500), ImGuiCond.FirstUseEver);
        if (ImGui.Begin(title, ImGuiWindowFlags.NoCollapse))
        {
            // read the directory
            var directory = new DirectoryInfo(currentPath);
            var files = directory.GetFiles().ToList();
            var folders = directory.GetDirectories().ToList();

            // display current path
            ImGui.Text(currentPath);

            var available = ImGui.GetContentRegionAvail();
            float folderPanelWidth = 200f;
            float buttonHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().ItemSpacing.Y * 2;
            float offsetHeight = buttonHeight * 2;
            float panelHeight = available.Y - offsetHeight;

            // folder panel
            ImGui.BeginChild("Folders", new Vector2(folderPanelWidth, panelHeight), ImGuiWindowFlags.NoScrollbar);

            // button to go up a directory
            if (ImGui.Selectable("..", false, ImGuiSelectableFlags.AllowDoubleClick))
            {
                if (ImGui.IsMouseDoubleClicked(0))
                {
                    var parent = Directory.GetParent(currentPath);
                    if (parent != null)
                    {
                        DeSelectAll();
                        currentPath = parent.FullName;
                    }
                }
            }

            // list folders as selectable buttons
            for (int i = 0; i < folders.Count; i++)
            {
                bool selected = i == currentFolderIndex;
                if (ImGui.Selectable(folders[i].Name, selected, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    currentFile = "";
                    if (ImGui.IsMouseDoubleClicked(0))
                    {
                        DeSelectAll();
                        currentPath = folders[i].FullName;
                    }
                    else
                    {
                        DeSelectFile();
                        currentFolderIndex = i;
                        currentFolder = folders[i].Name;
                    }
                }
            }

            ImGui.EndChild();
            ImGui.SameLine();

            // file panel
            float filePanelWidth = available.X - folderPanelWidth - ImGui.GetStyle().ItemSpacing.X;
            ImGui.BeginChild("Files", new Vector2(filePanelWidth, panelHeight), ImGuiWindowFlags.HorizontalScrollbar);

            // row of sorting buttons
            ImGui.Columns(4);
            if (ImGui.Selectable("File")) ToggleSort(ref fileNameSortOrder, ref sizeSortOrder, ref dateSortOrder, ref typeSortOrder);
            ImGui.NextColumn();
            if (ImGui.Selectable("Size")) ToggleSort(ref sizeSortOrder, ref fileNameSortOrder, ref dateSortOrder, ref typeSortOrder);
            ImGui.NextColumn();
            if (ImGui.Selectable("Type")) ToggleSort(ref typeSortOrder, ref fileNameSortOrder, ref sizeSortOrder, ref dateSortOrder);
            ImGui.NextColumn();
            if (ImGui.Selectable("Date")) ToggleSort(ref dateSortOrder, ref fileNameSortOrder, ref sizeSortOrder, ref typeSortOrder);
            ImGui.NextColumn();
            ImGui.Separator();
            SortFiles(ref files);

            // list of files as buttons
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                bool selected = i == currentFileIndex;
                if (ImGui.Selectable(file.Name, selected, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    DeSelectFolder();
                    currentFileIndex = i;
                    currentFile = file.Name;
                }

                ImGui.NextColumn();
                string str;
                long bytes = file.Length;
                if (bytes >= 1073741824) str = (bytes / 1073741824.0).ToString("0.##") + " GB"; // higher than gb
                else if (bytes >= 1048576) str = (bytes / 1048576.0).ToString("0.##") + " MB"; // higher than mb
                else if (bytes >= 1024) str = (bytes / 1024.0).ToString("0.##") + " KB"; // higher than kb
                else str = "1 KB"; // lower than kb
                float columnWidth = ImGui.GetColumnWidth();
                Vector2 textSize = ImGui.CalcTextSize(str + " ");
                float cursorStartX = ImGui.GetCursorPosX();
                float textPosX = cursorStartX + (columnWidth - textSize.X - ImGui.GetStyle().CellPadding.X * 2);
                ImGui.SetCursorPosX(textPosX > cursorStartX ? textPosX : cursorStartX);
                ImGui.Text(str);
                ImGui.NextColumn();
                ImGui.Text(file.Extension);
                ImGui.NextColumn();
                ImGui.Text(file.LastWriteTime.ToString("dd-MM-yyyy HH:mm"));
                ImGui.NextColumn();
            }

            ImGui.EndChild();

            // display selected path
            string selectedPath = Path.Combine(currentPath, currentFolder != "" ? currentFolder : currentFile);
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##selected", ref selectedPath, 512, ImGuiInputTextFlags.ReadOnly);
            ImGui.PopItemWidth();

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);

            // new folder button
            if (ImGui.Button("New folder")) ImGui.OpenPopup("Create Folder");

            ImGui.SameLine();

            // delete folder button
            bool canDeleteFolder = !string.IsNullOrEmpty(currentFolder);
            if (!canDeleteFolder) ImGui.BeginDisabled();
            if (ImGui.Button("Delete folder")) ImGui.OpenPopup("Delete Folder");
            if (!canDeleteFolder) ImGui.EndDisabled();

            ImGui.SameLine();

            // new file button
            if (ImGui.Button("New file")) ImGui.OpenPopup("Create File");

            ImGui.SameLine();

            // delete file button
            bool canDeleteFile = currentFile != "";
            if (!canDeleteFile) ImGui.BeginDisabled();
            if (ImGui.Button("Delete file")) ImGui.OpenPopup("Delete File");
            if (!canDeleteFile) ImGui.EndDisabled();

            // new folder popup
            if (ImGui.BeginPopupModal("Create Folder", ImGuiWindowFlags.NoResize))
            {
                ImGui.Text("Name:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##foldername", ref newFolderName, 100);

                // create button
                bool emptyName = string.IsNullOrWhiteSpace(newFolderName);
                ImGui.BeginDisabled(emptyName);
                if (ImGui.Button("Create"))
                {
                    var path = Path.Combine(currentPath, newFolderName);
                    Directory.CreateDirectory(path);
                    newFolderName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndDisabled();

                ImGui.SameLine();

                // cancel button
                if (ImGui.Button("Cancel"))
                {
                    newFolderName = "";
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            // new file popup
            if (ImGui.BeginPopupModal("Create File", ImGuiWindowFlags.NoResize))
            {
                ImGui.Text("Name:");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText("##filename", ref newFileName, 100);

                // create button
                bool emptyName = string.IsNullOrWhiteSpace(newFileName);
                ImGui.BeginDisabled(emptyName);
                if (ImGui.Button("Create"))
                {
                    var path = Path.Combine(currentPath, newFileName);
                    File.Create(path);
                    newFileName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndDisabled();

                ImGui.SameLine();

                // cancel button
                if (ImGui.Button("Cancel"))
                {
                    newFileName = "";
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            // delete folder popup
            if (ImGui.BeginPopupModal("Delete Folder", ImGuiWindowFlags.NoResize))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Are you sure you want to delete '{currentFolder}'?");
                ImGui.Spacing();

                var width = ImGui.GetContentRegionAvail().X / 2 - (ImGui.GetStyle().ItemSpacing.X / 2);
                if (ImGui.Button("Yes", new(width, 0)))
                {
                    Directory.Delete(Path.Combine(currentPath, currentFolder), true);
                    DeSelectFolder();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("No", new(width, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            // delete file popup
            if (ImGui.BeginPopupModal("Delete File", ImGuiWindowFlags.NoResize))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Are you sure you want to delete '{currentFile}'?");
                ImGui.Spacing();

                var width = ImGui.GetContentRegionAvail().X / 2 - (ImGui.GetStyle().ItemSpacing.X / 2);
                if (ImGui.Button("Yes", new(width, 0)))
                {
                    File.Delete(Path.Combine(currentPath, currentFile));
                    DeSelectFile();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("No", new(width, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.SameLine();

            // cancel dialog button
            ImGui.SetCursorPosX(available.X - 200);
            if (ImGui.Button("Cancel", new Vector2(100, 0)))
            {
                DeSelectAll();
                open = false;
            }

            ImGui.SameLine();

            bool disableChoose = false;
            if (singleFile && currentFile == "") disableChoose = true;
            if (!singleFile && currentFolder == "") disableChoose = true;

            // choose selected file or folder button
            ImGui.BeginDisabled(disableChoose);
            if (ImGui.Button("Choose", new Vector2(100, 0)))
            {
                resultPath = Path.Combine(currentPath, !string.IsNullOrEmpty(currentFolder) ? currentFolder : currentFile);
                DeSelectAll();
                OnChoose?.Invoke();
                open = false;
            }
            ImGui.EndDisabled();

            ImGui.End();
        }
    }

    static void ToggleSort(ref SortOrder target, ref SortOrder a, ref SortOrder b, ref SortOrder c)
    {
        a = b = c = SortOrder.None;
        target = target == SortOrder.Down ? SortOrder.Up : SortOrder.Down;
    }

    static void SortFiles(ref List<FileInfo> files)
    {
        if (fileNameSortOrder != SortOrder.None)
        {
            files = files.OrderBy(f => f.Name).ToList();
            if (fileNameSortOrder == SortOrder.Down) files.Reverse();
        }
        else if (sizeSortOrder != SortOrder.None)
        {
            files = files.OrderBy(f => f.Length).ToList();
            if (sizeSortOrder == SortOrder.Down) files.Reverse();
        }
        else if (typeSortOrder != SortOrder.None)
        {
            files = files.OrderBy(f => f.Extension).ToList();
            if (typeSortOrder == SortOrder.Down) files.Reverse();
        }
        else if (dateSortOrder != SortOrder.None)
        {
            files = files.OrderBy(f => f.LastWriteTime).ToList();
            if (dateSortOrder == SortOrder.Down) files.Reverse();
        }
    }

    public enum SortOrder
    {
        Up,
        Down,
        None
    }
}