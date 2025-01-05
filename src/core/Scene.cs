using System.Numerics;

namespace Concrete;

public class Scene
{
    [Include] public List<GameObject> gameObjects = [];

    public Scene()
    {
        // do nothing
    }

    public List<Light> FindActiveLights()
    {
        List<Light> lights = [];
        foreach (var gameObject in gameObjects)
        {
            if (gameObject.enabled)
            {
                foreach (var component in gameObject.components)
                {
                    if (component is Light light)
                    {
                        lights.Add(light);
                    }
                }
            }
        }
        return lights;
    }

    public Camera FindAnyCamera()
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

    public GameObject FindGameObject(int id)
    {
        GameObject result = null;
        foreach (var gameObject in gameObjects) if (gameObject.id == id) result = gameObject;
        return result;
    }

    public void RemoveGameObject(GameObject gameObject)
    {
        gameObjects.Remove(gameObject);
        gameObject = null;
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