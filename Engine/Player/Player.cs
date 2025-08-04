using System.Drawing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Concrete;

public static class Player
{
    static void Main()
    {
        var options = WindowOptions.Default;
        options.Size = new(1600, 900);
        options.Title = "Concrete Player";
        NativeWindow.window = Window.Create(options);
        NativeWindow.window.Load += StartWindow;
        NativeWindow.window.Update += UpdateWindow;
        NativeWindow.window.Render += RenderWindow;
        NativeWindow.window.FramebufferResize += ResizeWindow;
        NativeWindow.window.Run();
        NativeWindow.window.Dispose();
    }

    static void StartWindow()
    {
        NativeWindow.opengl = GL.GetApi(NativeWindow.window);
        NativeWindow.input = NativeWindow.window.CreateInput();
        ProjectManager.LoadProject("C:/Users/sjoer/Documents/GitHub/concrete/Example/project.json");
        SceneManager.StartPlaying();
    }

    static void UpdateWindow(double deltaTime)
    {
        Metrics.Update((float)deltaTime);
        if (SceneManager.playState == PlayState.playing) SceneManager.UpdateSceneObjects((float)deltaTime);
    }

    static void RenderWindow(double deltaTime)
    {
        NativeWindow.opengl.Enable(EnableCap.DepthTest);
        NativeWindow.opengl.ClearColor(Color.DarkGray);
        NativeWindow.opengl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        SceneManager.RenderSceneObjects((float)deltaTime, Scene.Current.FindAnyCamera().view, Scene.Current.FindAnyCamera().proj);
    }

    static void ResizeWindow(Vector2D<int> size)
    {
        NativeWindow.opengl.Viewport(size);
    }
}