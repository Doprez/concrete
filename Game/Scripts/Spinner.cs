using System.Numerics;

namespace Concrete;

public class Spinner : Component
{
    public override void Update(float deltaTime)
    {
        gameObject.transform.localEulerAngles += new Vector3(0, deltaTime * 10, 0);
    }
}