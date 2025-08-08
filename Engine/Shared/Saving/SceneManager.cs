using System.Numerics;

namespace Concrete;

public static class SceneManager
{
    public static PlayState playState = PlayState.stopped;

    private static Scene loadedScene = null;
    private static string snapshotPath = Path.Combine(Path.GetTempPath(), "snapshot.scene");

    public static void StartPlaying()
    {
        // store scene snapshot
        if (File.Exists(snapshotPath)) File.Delete(snapshotPath);
        SaveScene(snapshotPath);

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
        
        // load snapshot
        LoadScene(snapshotPath);
        if (File.Exists(snapshotPath)) File.Delete(snapshotPath);
    }

    public static Scene GetLoadedScene()
    {
        var loaded = loadedScene;
        return loaded;
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