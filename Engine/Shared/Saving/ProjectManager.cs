using System.Reflection;

namespace Concrete;

public static class ProjectManager
{
    public static string loadedProjectFilePath = null;
    public static ProjectData loadedProjectData = null;

    public static string projectRoot => Directory.GetParent(Path.GetFullPath(loadedProjectFilePath)).FullName;

    public static string memoryFilePath = Path.Combine(Path.GetTempPath(), "memory.txt");
    public static string tempProjectPath = Path.Combine(Path.GetTempPath(), "TempConcreteProject");

    public static void SaveProject(string path)
    {
        ProjectSerializer.SaveProject(path, loadedProjectData);
    }

    public static void TryLoadLastProjectOrCreateTempProject()
    {
        if (File.Exists(memoryFilePath))
        {
            Console.WriteLine("Memory file found.");
            
            string memoryProjectPath = File.ReadAllText(memoryFilePath);
            
            if (File.Exists(memoryProjectPath))
            {
                Console.WriteLine("Memory file contained a valid project.");
                LoadProject(memoryProjectPath);
            }
            else
            {
                Console.WriteLine("Memory file did not contain a valid project.");
                CreateAndLoadTempProject();
            }
        }
        else
        {
            Console.WriteLine("No memory file found.");
            CreateAndLoadTempProject();
        }
    }

    public static void CreateAndLoadTempProject()
    {
        Console.WriteLine("Creating and loading a temporary project.");

        // make empty temp project directory
        if (Directory.Exists(tempProjectPath)) Directory.Delete(tempProjectPath, true);
        Directory.CreateDirectory(tempProjectPath);

        // create and load and remember temp project
        CreateAndLoadNewProject(Path.Combine(tempProjectPath, "project.json"));
    }

    public static void LoadProject(string path, bool isTemp = false)
    {
        // load project
        loadedProjectFilePath = path;
        loadedProjectData = ProjectSerializer.LoadProject(path);
        NativeWindow.window.Title = "Concrete Engine [" + Path.GetFullPath(loadedProjectFilePath) + "]";

        // initialize asset database
        AssetDatabase.Rebuild();

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
        
        if (!isTemp)
        {
            // remember project
            if (File.Exists(memoryFilePath)) File.Delete(memoryFilePath);
            File.WriteAllText(memoryFilePath, path);
            Console.WriteLine("Remembered the newly loaded project.");
        }
    }

    public static void CreateAndLoadNewProject(string path)
    {
        ProjectSerializer.NewProjectFile(path);
        LoadProject(path, true);
        Directory.CreateDirectory("Scenes");
        Directory.CreateDirectory("Scripts");
    }
}