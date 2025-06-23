using System;
using System.Numerics;

namespace Concrete;

public static class SceneManager
{
    public static Scene loadedScene = null;
    public static PlayState playState = PlayState.stopped;

    public static void StartPlaying()
    {
        SaveScene();
        StartSceneObjects();
        playState = PlayState.playing;
    }

    public static void PausePlaying()
    {
        playState = PlayState.paused;
    }

    public static void ContinuePlaying()
    {
        playState = PlayState.playing;
    }

    public static void StopPlaying()
    {
        playState = PlayState.stopped;
        LoadScene();
    }

    public static void CreateAndLoadNewScene()
    {
        LoadScene(new Scene());

        var cameraObject = GameObject.Create();
        cameraObject.AddComponent<Camera>();
        cameraObject.name = "Main Camera";

        var lightObject = GameObject.Create();
        lightObject.AddComponent<DirectionalLight>();
        lightObject.transform.localEulerAngles = new Vector3(20, 135, 0);
        lightObject.name = "Directional Light";
    }

    public static void LoadScene(Scene scene) => loadedScene = scene;
    public static void SaveScene() => SceneSerializer.SaveScene("res/scenes/test.scene", loadedScene);
    public static void LoadScene() => SceneSerializer.LoadScene("res/scenes/test.scene");
    public static void SaveScene(string path) => SceneSerializer.SaveScene(path, loadedScene);
    public static void LoadScene(string path) => loadedScene = SceneSerializer.LoadScene(path);

    public static void StartSceneObjects() => loadedScene?.Start();
    public static void UpdateSceneObjects(float deltaTime) => loadedScene?.Update(deltaTime);
    public static void RenderSceneObjects(float deltaTime, Matrix4x4 view, Matrix4x4 proj) => loadedScene?.Render(deltaTime, view, proj);
}

public enum PlayState
{
    stopped,
    playing,
    paused
}