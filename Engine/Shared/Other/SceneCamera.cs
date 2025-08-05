using System.Numerics;
using Silk.NET.Input;

namespace Concrete;

public class SceneCamera
{
    public Matrix4x4 view => Matrix4x4.CreateLookAt(position, position + forward, up);
    public Matrix4x4 proj => Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI * fov / 180f, (float)SceneRenderWindow.framebuffer.size.X / (float)SceneRenderWindow.framebuffer.size.Y, 0.1f, 1000f);

    public float fov = 90;

    public Vector3 position = new(-0.4f, 1.6f, 1.6f);
    public Vector3 rotation = new(10, 155, 0);

    public Vector3 forward => LocalDirection(Vector3.UnitZ);
    public Vector3 up => LocalDirection(Vector3.UnitY);
    public Vector3 right => LocalDirection(Vector3.UnitX);

    private Vector2 lastMousePos;

    public void ApplyMovement(float deltaTime)
    {
        var keyboard = NativeWindow.input.Keyboards[0];
        var mouse = NativeWindow.input.Mice[0];

        // position
        var movedir = new Vector3();
        if (keyboard.IsKeyPressed(Key.W)) movedir += forward;
        if (keyboard.IsKeyPressed(Key.A)) movedir += right;
        if (keyboard.IsKeyPressed(Key.S)) movedir -= forward;
        if (keyboard.IsKeyPressed(Key.D)) movedir -= right;
        if (keyboard.IsKeyPressed(Key.Space)) movedir += up;
        if (keyboard.IsKeyPressed(Key.ControlLeft)) movedir -= up;
        if (keyboard.IsKeyPressed(Key.ShiftLeft)) movedir *= 2;
        position += movedir * deltaTime;

        // rotation
        var lookSpeed = 0.12f;
        if (mouse.IsButtonPressed(MouseButton.Right))
        {
            var mouseDelta = lastMousePos - mouse.Position;
            rotation += new Vector3(-mouseDelta.Y, mouseDelta.X, 0) * lookSpeed;
        }
        lastMousePos = mouse.Position;
    }

    private Vector3 LocalDirection(Vector3 worldDirection)
    {
        var toRadians = MathF.PI / 180.0f;
        var quaternion = Quaternion.CreateFromYawPitchRoll(rotation.Y * toRadians, rotation.X * toRadians, rotation.Z * toRadians);
        var direction = Vector3.Transform(worldDirection, quaternion);
        return direction;
    }
}