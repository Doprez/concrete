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
        StartScene();
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

    public static void SaveScene() => SceneSerializer.SaveScene("res/scenes/test.scene", loadedScene);

    public static void LoadScene(Scene scene) => loadedScene = scene;
    public static void LoadScene(string path) => loadedScene = SceneSerializer.LoadScene(path);
    public static void LoadScene() => SceneSerializer.LoadScene("res/scenes/test.scene");

    public static void StartScene() => loadedScene?.Start();
    public static void UpdateScene(float deltaTime) => loadedScene?.Update(deltaTime);
    public static void RenderScene(float deltaTime, Matrix4x4 view, Matrix4x4 proj) => loadedScene?.Render(deltaTime, view, proj);
}

public enum PlayState
{
    stopped,
    playing,
    paused
}