using System.Numerics;

namespace Concrete;

public class GameObjectReference : Component
{
    [Include] public Guid guid;

    public GameObjectReference()
    {
        // do nothing
    }
}