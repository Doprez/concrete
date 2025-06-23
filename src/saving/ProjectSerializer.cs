using System;
using System.IO;
using System.Text.Json;

namespace Concrete;

public static class ProjectSerializer
{
    private static JsonSerializerOptions config = new()
    {
        IncludeFields = true,
        WriteIndented = true,
        IndentSize = 4,
    };

    public static void SaveProject(string path, ProjectData project)
    {
        if (File.Exists(path)) File.Delete(path);
        string json = JsonSerializer.Serialize(project, config);
        File.WriteAllText(path, json);
    }

    public static ProjectData LoadProject(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ProjectData>(json, config);
    }
    
    public static void NewProjectFile(string path)
    {
        // todo: implement correctly
        if (File.Exists(path)) File.Delete(path);
        var projectData = new ProjectData("project", "");
        string json = JsonSerializer.Serialize(projectData, config);
        File.WriteAllText(path, json);
    }
}