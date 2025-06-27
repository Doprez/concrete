using System;
using System.IO;
using System.Text.Json;

namespace Concrete;

public static class ProjectManager
{
    public static string loadedProjectFilePath = null;
    public static ProjectData loadedProjectData = null;

    public static void SaveProject(string path)
    {
        ProjectSerializer.SaveProject(path, loadedProjectData);
    }

    public static void LoadProject(string path)
    {
        // load project
        loadedProjectFilePath = path;
        loadedProjectData = ProjectSerializer.LoadProject(path);
        Engine.window.Title = "Concrete Engine [" + Path.GetFullPath(loadedProjectFilePath) + "]";

        // initialize asset database
        string root = Directory.GetParent(Path.GetFullPath(loadedProjectFilePath)).FullName;
        AssetDatabase.Initialize(root);

        // try to load startup scene
        if (loadedProjectData.firstScene != "")
        {
            string sceneRelativePath = AssetDatabase.GetPath(Guid.Parse(loadedProjectData.firstScene));
            string sceneFullPath = Path.Combine(root, sceneRelativePath);

            Console.WriteLine("rel: " + sceneRelativePath);
            Console.WriteLine("full: " + sceneFullPath);

            SceneManager.LoadScene(sceneFullPath);
        }
        else
        {
            SceneManager.CreateAndLoadNewScene();
        }
    }

    public static void CreateAndLoadNewProject(string path)
    {
        ProjectSerializer.NewProjectFile(path);
        LoadProject(path);
    }
}