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
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Concrete";
        window = Window.Create(options);
        window.Load += StartWindow;
        window.Update += UpdateWindow;
        window.Render += RenderWindow;
        window.FramebufferResize += ResizeWindow;
        window.Run();
        window.Dispose();
    }

    static void StartWindow()
    {
        opengl = GL.GetApi(window);
        input = window.CreateInput();
        igcontroller = new ImGuiController(opengl, window, input);

        var scene = new Scene();
        SceneManager.LoadScene(scene);

        var camera = GameObject.Create();
        camera.AddComponent<Camera>();
        camera.name = "Camera";

        var helmet = GameObject.Create();
        helmet.AddComponent<MeshRenderer>().modelPath = "res/models/helmet.glb";
        helmet.name = "Helmet";

        var robot = GameObject.Create();
        robot.AddComponent<MeshRenderer>().modelPath = "res/models/robot.glb";
        robot.transform.localPosition = new Vector3(2, 0, 0);
        robot.transform.localEulerAngles = new Vector3(-90, 0, 0);
        robot.name = "Robot";

        var light = GameObject.Create();
        light.AddComponent<DirectionalLight>();
        light.transform.localEulerAngles = new Vector3(20, 135, 0);
        light.name = "Light";
    }

    static void UpdateWindow(double deltaTime)
    {
        Metrics.Update((float)deltaTime);
        if (SceneManager.playState == PlayState.playing) SceneManager.UpdateScene((float)deltaTime);
        Editor.Update((float)deltaTime);
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
}