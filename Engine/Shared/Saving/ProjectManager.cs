using System.Reflection;

namespace Concrete;

public static class ProjectManager
{
    public static string loadedProjectFilePath = null;
    public static ProjectData loadedProjectData = null;

    public static string projectRoot => Directory.GetParent(Path.GetFullPath(loadedProjectFilePath)).FullName;

    public static string memoryFilePath = Path.Combine(Path.GetTempPath(), "memory.txt");
    public static string tempProjectPath = Path.Combine(Path.GetTempPath(), "TempConcreteProject");

    public static void TryLoadLastProjectOrCreateTempProject()
    {
        if (File.Exists(memoryFilePath))
        {
            Console.WriteLine("Memory file found.");
            
            string memoryProjectPath = File.ReadAllText(memoryFilePath);
            
            if (File.Exists(memoryProjectPath))
            {
                Console.WriteLine("Memory file contained a valid project.");
                LoadProjectFile(memoryProjectPath);
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
        string projectfilepath = Path.Combine(tempProjectPath, "project.json");
        ProjectSerializer.NewProjectFile(projectfilepath);
        LoadProjectFile(projectfilepath, true);
        Directory.CreateDirectory(Path.Combine(tempProjectPath, "Scenes"));
        Directory.CreateDirectory(Path.Combine(tempProjectPath, "Scripts"));
    }

    // ----

    public static void SaveProjectFile(string path)
    {
        ProjectSerializer.SaveProjectFile(path, loadedProjectData);
    }

    public static void LoadProjectFile(string path, bool isTemp = false)
    {
        // load project
        loadedProjectFilePath = path;
        loadedProjectData = ProjectSerializer.LoadProjectFile(path);
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

        AfterProjectLoad(Path.GetDirectoryName(path));
    }

    // ----

    public static void NewProjectDir(string dir)
    {
        var filepath = Path.Combine(dir, "project.json");
        ProjectSerializer.NewProjectFile(filepath);
        LoadProjectFile(filepath);
        Directory.CreateDirectory("Scenes");
        Directory.CreateDirectory("Scripts");
    }

    public static void SaveProjectDir(string dir)
    {
        if (dir != projectRoot)
        {
            CopyDirectory(projectRoot, dir);
            LoadProjectDir(dir);
        }
        else
        {
            Console.WriteLine("Project is already up to date.");
        }
    }

    public static void LoadProjectDir(string dir, bool isTemp = false)
    {
        var filepath = Path.Combine(dir, "project.json");
        if (!File.Exists(filepath)) File.Create(filepath);

        // load project
        loadedProjectFilePath = filepath;
        loadedProjectData = ProjectSerializer.LoadProjectFile(filepath);
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
            File.WriteAllText(memoryFilePath, filepath);
            Console.WriteLine("Remembered the newly loaded project.");
        }

        // rebuild shared ref for scripts
        AfterProjectLoad(dir);

        // make sure gitignore exists

    }

    private static void CopyDirectory(string source, string dest)
    {
        // ensure existence
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException($"directory not found: {source}");

        // create destination
        Directory.CreateDirectory(dest);

        // copy files
        foreach (string file in Directory.GetFiles(source))
        {
            string destFile = Path.Combine(dest, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        // copy dirs
        foreach (string subdir in Directory.GetDirectories(source))
        {
            string dubdirdest = Path.Combine(dest, Path.GetFileName(subdir));
            CopyDirectory(subdir, dubdirdest);
        }
    }

    // ----

    static void AfterProjectLoad(string dir)
    {
        // rebuild shared ref for scripts
        string csproj = Path.Combine(dir, "project.csproj");
        if (File.Exists(csproj)) File.Delete(csproj);
        Dotnet.New(csproj);
        Dotnet.AddDll(csproj, Path.GetFullPath("Shared.dll"));

        // make sure gitignore exists
        string gitignore = Path.Combine(dir, ".gitignore");
        if (!File.Exists(gitignore)) File.WriteAllText(gitignore, "bin/\nobj/\nproject.csproj");
    }
}