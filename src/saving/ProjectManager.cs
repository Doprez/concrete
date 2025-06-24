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

        if (loadedProjectData.firstScene != "")
        {
            // load default scene
            string projectRoot = Path.GetDirectoryName(path);
            string scenePath = projectRoot + "/" + loadedProjectData.firstScene;
            SceneManager.LoadScene(scenePath);
        }
        else
        {
            // load empty scene
            SceneManager.CreateAndLoadNewScene();
        }
    }

    public static void CreateAndLoadNewProject(string path)
    {
        ProjectSerializer.NewProjectFile(path);
        LoadProject(path);
    }
}