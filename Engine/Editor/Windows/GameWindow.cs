using System.Numerics;
using System.Drawing;

using Hexa.NET.ImGui;

namespace Concrete;

public static unsafe class GameWindow
{
    private static bool gameWindowFocussed = false;
    
    public static void Draw(float deltaTime)
    {
        ImGui.Begin("Game", ImGuiWindowFlags.NoScrollbar);
        gameWindowFocussed = ImGui.IsWindowFocused();

        // render to framebuffer
        GameRenderWindow.framebuffer.Resize(ImGui.GetContentRegionAvail());
        GameRenderWindow.framebuffer.Bind();
        GameRenderWindow.framebuffer.Clear(Color.DarkGray);
        var cam = Scene.Current.FindAnyCamera();
        SceneManager.RenderSceneObjects(deltaTime, cam.view, cam.proj);
        GameRenderWindow.framebuffer.Unbind();

        // record corner position
        var gamecornerpos = ImGui.GetCursorPos();

        // show framebuffer as image
        ImGui.Image((ImTextureID)GameRenderWindow.framebuffer.colorTexture, GameRenderWindow.framebuffer.size, Vector2.UnitY, Vector2.UnitX);

        {
            var buttonsize = new Vector2(0, 0);
            var padding = ImGui.GetStyle().WindowPadding;
            var stopped = SceneManager.playState == PlayState.stopped;
            var playing = SceneManager.playState == PlayState.playing;
            var paused = SceneManager.playState == PlayState.paused;

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.5f, 0.5f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.7f, 0.7f, 0.7f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.9f, 0.9f, 0.9f, 1));
            ImGui.SetCursorPos(gamecornerpos + padding);
            ImGui.BeginDisabled(playing || paused);
            if (ImGui.Button("play", buttonsize))
            {
                SceneManager.StartPlaying();
                ImGuiP.FocusWindow(ImGuiP.FindWindowByName("Game"), ImGuiFocusRequestFlags.None);
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(stopped);
            if (ImGui.Button(paused ? "continue" : "pause", buttonsize))
            {
                if (paused) SceneManager.ContinuePlaying();
                else SceneManager.PausePlaying();
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(stopped);
            if (ImGui.Button("stop", buttonsize)) SceneManager.StopPlaying();
            ImGui.EndDisabled();
            ImGui.PopStyleColor(3);
        }
        
        ImGui.End();
    }
}