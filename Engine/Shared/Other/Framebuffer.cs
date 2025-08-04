using System.Numerics;
using System.Drawing;
using Silk.NET.OpenGL;

namespace Concrete;

public unsafe class Framebuffer
{
    private GL opengl;

    public Vector2 size;
    public uint handle;
    public uint colorTexture;
    public uint depthTexture;

    public Framebuffer()
    {
        opengl = NativeWindow.opengl;
        size = new Vector2(NativeWindow.window.Size.X, NativeWindow.window.Size.Y);
        handle = opengl.GenFramebuffer();
        opengl.BindFramebuffer(GLEnum.Framebuffer, handle);

        colorTexture = opengl.GenTexture();
        opengl.BindTexture(GLEnum.Texture2D, colorTexture);
        opengl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba, (uint)size.X, (uint)size.Y, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
        opengl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        opengl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
        opengl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, colorTexture, 0);

        depthTexture = opengl.GenTexture();
        opengl.BindTexture(GLEnum.Texture2D, depthTexture);
        opengl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.DepthComponent, (uint)size.X, (uint)size.Y, 0, GLEnum.DepthComponent, GLEnum.Float, null);
        opengl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        opengl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
        opengl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.DepthAttachment, GLEnum.Texture2D, depthTexture, 0);

        var status = opengl.CheckFramebufferStatus(GLEnum.Framebuffer);
        if (status != GLEnum.FramebufferComplete) throw new Exception("Framebuffer is not complete!");
        opengl.BindFramebuffer(GLEnum.Framebuffer, 0);
    }

    public void Clear(Color color)
    {
        opengl.ClearColor(color);
        opengl.Clear((uint)(GLEnum.ColorBufferBit | GLEnum.DepthBufferBit));
    }

    public void Bind()
    {
        opengl.BindFramebuffer(GLEnum.Framebuffer, handle);
        opengl.Viewport(new Size((int)size.X, (int)size.Y));
    }

    public void Unbind()
    {
        opengl.BindFramebuffer(GLEnum.Framebuffer, 0);
        opengl.Viewport(NativeWindow.window.Size);
    }

    public void Resize(Vector2 newSize)
    {
        size = newSize;
        Bind();
        opengl.BindTexture(GLEnum.Texture2D, colorTexture);
        opengl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba, (uint)size.X, (uint)size.Y, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
        opengl.BindTexture(GLEnum.Texture2D, depthTexture);
        opengl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.DepthComponent, (uint)size.X, (uint)size.Y, 0, GLEnum.DepthComponent, GLEnum.Float, null);
        Unbind();
    }
}