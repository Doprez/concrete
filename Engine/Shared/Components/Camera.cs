using System.Numerics;

namespace Concrete;

public class Camera : Component
{
    [Include] [Show] public float fov = 90;

    public Matrix4x4 view => Matrix4x4.CreateLookAt(gameObject.transform.worldPosition, gameObject.transform.worldPosition + -gameObject.transform.forward, gameObject.transform.up);
    public Matrix4x4 proj => Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI * fov / 180f, (float)GameRenderWindow.framebuffer.size.X / (float)GameRenderWindow.framebuffer.size.Y, 0.1f, 1000f);
}