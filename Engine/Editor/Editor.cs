using System.Drawing;

using Hexa.NET.ImGui;

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;

namespace Concrete;

public static unsafe class Editor
{
    public static ImGuiController igcontroller;

    static void Main()
    {
        var options = WindowOptions.Default;
        options.Size = new(1600, 900);
        options.Title = "Concrete Engine";
        NativeWindow.window = Window.Create(options);
        NativeWindow.window.Load += StartWindow;
        NativeWindow.window.Update += UpdateWindow;
        NativeWindow.window.Render += RenderWindow;
        NativeWindow.window.FramebufferResize += ResizeWindow;
        NativeWindow.window.FileDrop += FileDrop;
        NativeWindow.window.Run();
        NativeWindow.window.Dispose();
    }

    static void StartWindow()
    {
        NativeWindow.opengl = GL.GetApi(NativeWindow.window);
        NativeWindow.input = NativeWindow.window.CreateInput();
        
        // setup imgui controller and styling
        igcontroller = new ImGuiController(NativeWindow.opengl, NativeWindow.window, NativeWindow.input);
        EditorStyleChanger.ClearFonts();
        EditorStyleChanger.AddFont(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "cascadia.ttf"), 18);
        EditorStyleChanger.AddFont(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fontawesome_free_solid.otf"), 18, true);
        EditorStyleChanger.SetupCustomTheme();
        
        ProjectManager.TryLoadLastProjectOrCreateTempProject();
    }

    static void UpdateWindow(double deltaTime)
    {
        Metrics.Update((float)deltaTime);
        if (SceneManager.playState == PlayState.playing) SceneManager.UpdateSceneObjects((float)deltaTime);
        igcontroller.Update((float)deltaTime);
    }

    static void RenderWindow(double deltaTime)
    {
        NativeWindow.opengl.Enable(EnableCap.DepthTest);
        NativeWindow.opengl.Enable(EnableCap.Blend);
        NativeWindow.opengl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        NativeWindow.opengl.ClearColor(Color.Black);
        NativeWindow.opengl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Render((float)deltaTime);
        igcontroller.Render();
    }

    static void ResizeWindow(Vector2D<int> size)
    {
        NativeWindow.opengl.Viewport(size);
    }

    static void FileDrop(string[] paths)
    {
        if (!FilesWindow.hovered) return;
        foreach (var path in paths)
        {
            // calculate destination parent
            var parent = ProjectManager.projectRoot;
            var hovered = FilesWindow.hoveredFileOrDir;
            if (hovered != null)
            {
                if (Directory.Exists(hovered)) parent = Path.GetFullPath(hovered);
                if (File.Exists(hovered)) parent = Path.GetDirectoryName(Path.GetFullPath(hovered));
            }

            if (Directory.Exists(path))
            {
                // move dir to project dir
                var dirname = Path.GetFileName(path);
                var destination = Path.Combine(parent, dirname);
                Directory.Move(path, destination);
                
                // rebuild asset database
                AssetDatabase.Rebuild();
            }
            else if (File.Exists(path))
            {
                // move file to project dir
                var filename = Path.GetFileName(path);
                var destination = Path.Combine(parent, filename);
                Directory.Move(path, destination);
                
                // rebuild asset database
                AssetDatabase.Rebuild();
            }
        }
    }

    private static bool dockbuilderInitialized = false;

    public static void Render(float deltaTime)
    {
        SetupDockSpace();
        MainMenuBar.Draw(deltaTime);
        SceneWindow.Draw(deltaTime);
        GameWindow.Draw(deltaTime);
        HierarchyWindow.Draw(deltaTime);
        FilesWindow.Draw(deltaTime);
        ConsoleWindow.Draw(deltaTime);
        InspectorWindow.Draw(deltaTime);
        MetricsWindow.Draw(deltaTime);
        BuildWindow.Draw(deltaTime);
    }

    unsafe private static void SetupDockSpace()
    {
        uint dockspace = ImGui.DockSpaceOverViewport((ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.NoWindowMenuButton);
        if (!dockbuilderInitialized)
        {
            uint left, mid, right;
            uint topleft, lowleft;
            uint topmid, lowmid;

            ImGuiP.DockBuilderSplitNode(dockspace, ImGuiDir.Left, 0.25f, &left, &mid);
            ImGuiP.DockBuilderSplitNode(mid, ImGuiDir.Left, 0.66f, &mid, &right);
            ImGuiP.DockBuilderSplitNode(left, ImGuiDir.Up, 0.5f, &topleft, &lowleft);
            ImGuiP.DockBuilderSplitNode(mid, ImGuiDir.Down, 0.3f, &lowmid, &topmid);

            ImGuiP.DockBuilderDockWindow("Scene", topmid);
            ImGuiP.DockBuilderDockWindow("Game", topmid);
            ImGuiP.DockBuilderDockWindow("Build", topmid);
            ImGuiP.DockBuilderDockWindow("Metrics", topmid);
            ImGuiP.DockBuilderDockWindow("Hierarchy", topleft);
            ImGuiP.DockBuilderDockWindow("Files", lowleft);
            ImGuiP.DockBuilderDockWindow("Inspector", right);
            ImGuiP.DockBuilderDockWindow("Console", lowmid);

            ImGuiP.DockBuilderFinish(dockspace);
            dockbuilderInitialized = true;
        }
    }
}