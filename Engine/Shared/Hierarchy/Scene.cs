using System.Numerics;

namespace Concrete;

public class Scene : IDisposable
{
    [Include] public List<GameObject> gameObjects = [];

    public static Scene Current => SceneManager.GetLoadedScene();

    public Scene()
    {
        // do nothing
    }

    public void Dispose()
    {
        foreach (var gameObject in gameObjects) gameObject.Dispose();
    }

    public Camera FindCamera()
    {
        foreach (var gameObject in gameObjects)
        {
            foreach (var component in gameObject.components)
            {
                if (component is Camera camera)
                {
                    return camera;
                }
            }
        }
        return null;
    }

    public GameObject FindGameObject(Guid guid)
    {
        GameObject result = null;
        foreach (var gameObject in gameObjects) if (gameObject.guid == guid) result = gameObject;
        return result;
    }

    public GameObject AddGameObject()
    {
        var gameObject = new GameObject();
        gameObject.transform = gameObject.AddComponent<Transform>();
        gameObject.guid = Guid.NewGuid();
        gameObject.name = $"GameObject ({gameObjects.Count})";
        gameObject.enabled = true;
        gameObjects.Add(gameObject);
        return gameObject;
    }

    public void RemoveGameObject(GameObject gameObject)
    {
        gameObjects.Remove(gameObject);
        gameObject.Dispose();
    }

    public void Start()
    {
        foreach (var gameObject in gameObjects) gameObject.Start();
    }

    public void Update(float deltaTime)
    {
        foreach (var gameObject in gameObjects) gameObject.Update(deltaTime);
    }

    public void Render(float deltaTime, Matrix4x4 view, Matrix4x4 proj)
    {
        foreach (var gameObject in gameObjects) gameObject.Render(deltaTime, view, proj);
    }
}