using System;
using System.Numerics;

namespace Concrete;

public class Component
{
    [Include] public GameObject gameObject;

    public virtual void Start()
    {
        // can be overridden
    }

    public virtual void Update(float deltaTime)
    {
        // can be overridden
    }

    public virtual void Render(float deltaTime, Matrix4x4 view, Matrix4x4 proj)
    {
        // can be overridden
    }
}
