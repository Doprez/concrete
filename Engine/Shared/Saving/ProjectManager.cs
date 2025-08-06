namespace Concrete;

public static class ProjectManager
{
    public static string loadedProjectFilePath = null;
    public static ProjectData loadedProjectData = null;

    public static string projectRoot => Directory.GetParent(Path.GetFullPath(loadedProjectFilePath)).FullName;

    public static void SaveProject(string path)
    {
        ProjectSerializer.SaveProject(path, loadedProjectData);
    }

    public static void LoadProject(string path, string preCompiledScriptAssembly = null)
    {
        // load project
        loadedProjectFilePath = path;
        loadedProjectData = ProjectSerializer.LoadProject(path);
        NativeWindow.window.Title = "Concrete Engine [" + Path.GetFullPath(loadedProjectFilePath) + "]";

        // initialize asset database
        AssetDatabase.Rebuild();

        if (preCompiledScriptAssembly == null)
        {
            // recompile script assembly
            ScriptManager.RecompileScripts();
        }
        else
        {
            // load script assembly dll from disk
            ScriptManager.LoadCompilesScriptsFromDisk(preCompiledScriptAssembly);
        }
        

        // try to load startup scene
        if (loadedProjectData.firstScene != "")
        {
            string sceneRelativePath = AssetDatabase.GetPath(Guid.Parse(loadedProjectData.firstScene));
            string sceneFullPath = Path.Combine(projectRoot, sceneRelativePath);
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