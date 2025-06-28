using System.Numerics;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

namespace Concrete;

public static unsafe class SceneWindow
{
    public static SceneCamera sceneCamera = new();
    public static Framebuffer scene_fb = new();
    private static bool sceneWindowFocussed = false;
    private static ImGuizmoOperation guizmoOperation = ImGuizmoOperation.Translate;
    private static ImGuizmoMode guizmoMode = ImGuizmoMode.Local;
    
    public static void Draw(float deltaTime)
    {
        ImGui.Begin("Scene", ImGuiWindowFlags.NoScrollbar);
        sceneWindowFocussed = ImGui.IsWindowFocused();

        // update scene camera movement
        if (sceneWindowFocussed) sceneCamera.ApplyMovement(deltaTime);
        
        // render scene to framebuffer
        scene_fb.Resize(ImGui.GetContentRegionAvail());
        scene_fb.Bind();
        scene_fb.Clear(Color.DarkGray);
        SceneManager.RenderSceneObjects(deltaTime, sceneCamera.view, sceneCamera.proj);
        scene_fb.Unbind();

        // record corner position
        var scenecornerpos = ImGui.GetCursorPos();

        // show framebuffer as image
        ImGui.Image((nint)scene_fb.colorTexture, scene_fb.size, Vector2.UnitY, Vector2.UnitX);

        // imguizmo
        if (HierarchyWindow.selectedGameObject != null)
        {
            var position = ImGui.GetWindowPos();
            var size = ImGui.GetWindowSize();
            var rect = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
            Engine.opengl.Viewport(rect);

            ImGuizmo.SetDrawlist();
            ImGuizmo.SetRect(position.X, position.Y, size.X, size.Y);

            Matrix4x4 worldModelMatrix = HierarchyWindow.selectedGameObject.transform.GetWorldModelMatrix();

            Matrix4x4 sview, sproj;
            sview = sceneCamera.view;
            sproj = sceneCamera.proj;
            ImGuizmo.Manipulate(ref sview, ref sproj, guizmoOperation, guizmoMode, ref worldModelMatrix);
            if (ImGuizmo.IsUsing()) HierarchyWindow.selectedGameObject.transform.SetWorldModelMatrix(worldModelMatrix);

            Engine.opengl.Viewport(Engine.window.Size);
        }

        {
            var buttonsize = new Vector2(0, 0);
            var padding = ImGui.GetStyle().WindowPadding;
            var moving = guizmoOperation == ImGuizmoOperation.Translate;
            var rotating = guizmoOperation == ImGuizmoOperation.Rotate;
            var scaling = guizmoOperation == ImGuizmoOperation.Scale;
            var local = guizmoMode == ImGuizmoMode.Local;
            var world = guizmoMode == ImGuizmoMode.World;

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.5f, 0.5f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.7f, 0.7f, 0.7f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.9f, 0.9f, 0.9f, 1));
            ImGui.SetCursorPos(scenecornerpos + padding);
            ImGui.BeginDisabled(moving);
            if (ImGui.Button("move", buttonsize)) guizmoOperation = ImGuizmoOperation.Translate;
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(rotating);
            if (ImGui.Button("rotate", buttonsize)) guizmoOperation = ImGuizmoOperation.Rotate;
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(scaling);
            if (ImGui.Button("scale", buttonsize)) guizmoOperation = ImGuizmoOperation.Scale;
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.SeparatorEx(ImGuiSeparatorFlags.Vertical, 1);
            ImGui.SameLine();
            ImGui.BeginDisabled(local);
            if (ImGui.Button("local", buttonsize)) guizmoMode = ImGuizmoMode.Local;
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(world);
            if (ImGui.Button("world", buttonsize)) guizmoMode = ImGuizmoMode.World;
            ImGui.EndDisabled();
            ImGui.PopStyleColor(3);
        }

        ImGui.End();
    }
}