using Concrete;
using System.Numerics;

public class Mover : Component
{
    public override void Update(float deltaTime)
    {
        gameObject.transform.localPosition += new Vector3(0, 0, deltaTime * -1);
    }
}