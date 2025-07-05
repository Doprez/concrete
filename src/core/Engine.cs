using System.Numerics;
using System.Drawing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;

namespace Concrete;

public static class Engine
{
    public static GL opengl;
    public static IWindow window;
    public static IInputContext input;
    public static ImGuiController igcontroller;

    static void Main()
    {
        var options = WindowOptions.Default;
        options.Size = new(1600, 900);
        options.Title = "Concrete Engine";
        window = Window.Create(options);
        window.Load += StartWindow;
        window.Update += UpdateWindow;
        window.Render += RenderWindow;
        window.FramebufferResize += ResizeWindow;
        window.FileDrop += FileDrop;
        window.Run();
        window.Dispose();
    }

    static void StartWindow()
    {
        opengl = GL.GetApi(window);
        input = window.CreateInput();
        igcontroller = new ImGuiController(opengl, window, input, "res/fonts/cascadia.ttf", 18);
        ProjectManager.LoadProject("example/project.json");
    }

    static void UpdateWindow(double deltaTime)
    {
        Metrics.Update((float)deltaTime);
        if (SceneManager.playState == PlayState.playing) SceneManager.UpdateSceneObjects((float)deltaTime);
        igcontroller.Update((float)deltaTime);
    }

    static void RenderWindow(double deltaTime)
    {
        opengl.Enable(EnableCap.DepthTest);
        opengl.ClearColor(Color.Black);
        opengl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Editor.Render((float)deltaTime);
        igcontroller.Render();
    }

    static void ResizeWindow(Vector2D<int> size)
    {
        opengl.Viewport(size);
    }

    static void FileDrop(string[] paths)
    {
        if (!FilesWindow.hovered) return;
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                // move dir to project dir
                var dirname = Path.GetFileName(path);
                var destination = Path.Combine(ProjectManager.projectRoot, dirname);
                Directory.Move(path, destination);
                
                // rebuild asset database
                AssetDatabase.Rebuild();
            }
            else if (File.Exists(path))
            {
                // move file to project dir
                var filename = Path.GetFileName(path);
                var destination = Path.Combine(ProjectManager.projectRoot, filename);
                Directory.Move(path, destination);
                
                // rebuild asset database
                AssetDatabase.Rebuild();
            }
        }
    }
}