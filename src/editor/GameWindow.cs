using System.Numerics;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

namespace Concrete;

public static unsafe class GameWindow
{
    public static Framebuffer game_fb = new();
    private static bool gameWindowFocussed = false;
    
    public static void Draw(float deltaTime)
    {
        ImGui.Begin("Game", ImGuiWindowFlags.NoScrollbar);
        gameWindowFocussed = ImGui.IsWindowFocused();

        // render to framebuffer
        game_fb.Resize(ImGui.GetContentRegionAvail());
        game_fb.Bind();
        game_fb.Clear(Color.DarkGray);
        var cam = SceneManager.loadedScene.FindAnyCamera();
        SceneManager.RenderSceneObjects(deltaTime, cam.view, cam.proj);
        game_fb.Unbind();

        // record corner position
        var gamecornerpos = ImGui.GetCursorPos();

        // show framebuffer as image
        ImGui.Image((nint)game_fb.colorTexture, game_fb.size, Vector2.UnitY, Vector2.UnitX);

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
                ImGui.FocusWindow(ImGui.FindWindowByName("Game"), ImGuiFocusRequestFlags.None);
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
            ImGui.End();
            ImGui.PopStyleColor(3);
        }
        
        ImGui.End();
    }
}