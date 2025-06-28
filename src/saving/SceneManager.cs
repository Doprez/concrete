using System;
using System.Numerics;

namespace Concrete;

public static class SceneManager
{
    public static Scene loadedScene = null;
    public static PlayState playState = PlayState.stopped;
    public static string cachePath = "cache.scene";

    public static void StartPlaying()
    {
        // store cache
        if (File.Exists(cachePath)) File.Delete(cachePath);
        SaveScene(cachePath);

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
        
        // load cache
        LoadScene(cachePath);
        if (File.Exists(cachePath)) File.Delete(cachePath);
    }

    public static void CreateAndLoadNewScene()
    {
        var scene = new Scene();
        LoadScene(scene);

        var cameraObject = scene.AddGameObject();
        cameraObject.AddComponent<Camera>();
        cameraObject.name = "Main Camera";

        var lightObject = scene.AddGameObject();
        lightObject.AddComponent<DirectionalLight>();
        lightObject.transform.localEulerAngles = new Vector3(20, 135, 0);
        lightObject.name = "Directional Light";
    }

    public static void SaveScene(string path)
    {
        SceneSerializer.SaveScene(path, loadedScene);
    }

    public static void LoadScene(Scene scene)
    {
        loadedScene?.Dispose();
        loadedScene = scene;
    }

    public static void LoadScene(string path)
    {
        loadedScene?.Dispose();
        loadedScene = SceneSerializer.LoadScene(path);
    }

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