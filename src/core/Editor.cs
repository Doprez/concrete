using System.Numerics;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

namespace Concrete;

public static unsafe class Editor
{
    private static int selectedGameObjectIdentifier;
    private static GameObject selectedGameObject
    {
        get => SceneManager.loadedScene.FindGameObject(selectedGameObjectIdentifier);
        set => selectedGameObjectIdentifier = value.id;
    }

    public static SceneCamera sceneCamera = new();
    public static Framebuffer scene_fb = new();
    public static Framebuffer game_fb = new();
    
    private static bool dockbuilderInitialized = false;
    private static bool sceneWindowFocussed = false;
    private static bool gameWindowFocussed = false;

    private static ImGuizmoOperation guizmoOperation = ImGuizmoOperation.Translate;
    private static ImGuizmoMode guizmoMode = ImGuizmoMode.Local;

    private static List<(GameObject, GameObject)> reparentque = [];

    private static List<Component> removeComponentQue = [];

    private static Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();
    private static Type[] componentTypes = allTypes.Where(type => type.IsClass && !type.IsAbstract && type != typeof(Transform) && type.IsSubclassOf(typeof(Component))).ToArray();

    public static void Update(float deltaTime)
    {
        if (sceneWindowFocussed) sceneCamera.ApplyMovement(deltaTime);
    }

    public static void Render(float deltaTime)
    {
        // deal with reparent que
        foreach (var tuple in reparentque)
        {
            var first = tuple.Item1;
            var second = tuple.Item2;
            if (second == null) first.transform.parent = null;
            else first.transform.parent = second.transform;
        }
        reparentque.Clear();

        // begin main menu bar
        if (ImGui.BeginMainMenuBar())
        {
            // scene menu
            if (ImGui.BeginMenu("Scene"))
            {
                if (ImGui.MenuItem("Save current scene")) SceneManager.SaveScene();
                if (ImGui.MenuItem("Load current scene")) SceneManager.LoadScene();
                ImGui.EndMenu();
            }

            // help menu
            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("Open engine repository")) OpenLink("https://github.com/sjoerdev/concrete");
                ImGui.EndMenu();
            }
            
            ImGui.EndMainMenuBar();
        }

        int dockspace = ImGui.DockSpaceOverViewport((ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.NoWindowMenuButton);
        if (!dockbuilderInitialized)
        {
            int leftdock, rightdock = 0;
            int bottomleftdock, topleftdock = 0;
            ImGui.DockBuilderSplitNode(dockspace, ImGuiDir.Left, 0.25f, &leftdock, &rightdock);
            ImGui.DockBuilderSplitNode(leftdock, ImGuiDir.Up, 0.5f, &topleftdock, &bottomleftdock);

            ImGui.DockBuilderDockWindow("Scene", rightdock);
            ImGui.DockBuilderDockWindow("Game", rightdock);
            ImGui.DockBuilderDockWindow("Hierarchy", bottomleftdock);
            ImGui.DockBuilderDockWindow("Inspector", topleftdock);
            ImGui.DockBuilderDockWindow("Metrics", topleftdock);

            ImGui.DockBuilderFinish(dockspace);
            dockbuilderInitialized = true;
        }
        
        ImGui.Begin("Scene", ImGuiWindowFlags.NoScrollbar);
        sceneWindowFocussed = ImGui.IsWindowFocused();
        
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
        if (selectedGameObject != null)
        {
            var position = ImGui.GetWindowPos();
            var size = ImGui.GetWindowSize();
            var rect = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
            Engine.opengl.Viewport(rect);

            ImGuizmo.SetDrawlist();
            ImGuizmo.SetRect(position.X, position.Y, size.X, size.Y);

            Matrix4x4 worldModelMatrix = selectedGameObject.transform.GetWorldModelMatrix();

            Matrix4x4 sview, sproj;
            sview = sceneCamera.view;
            sproj = sceneCamera.proj;
            ImGuizmo.Manipulate(ref sview, ref sproj, guizmoOperation, guizmoMode, ref worldModelMatrix);
            if (ImGuizmo.IsUsing()) selectedGameObject.transform.SetWorldModelMatrix(worldModelMatrix);

            Engine.opengl.Viewport(Engine.window.Size);
        }

        {
            var buttonsize = new Vector2(50, 0);
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
            var buttonsize = new Vector2(64, 0);
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

        ImGui.Begin("Hierarchy");

        var hspace = ImGui.GetContentRegionAvail().X;
        var hwidth = hspace / 2 - ImGui.GetStyle().ItemSpacing.X / 2;
        var hsize = new Vector2(hwidth, 0);

        if (ImGui.Button("Create", hsize)) GameObject.Create();
        ImGui.SameLine();
        ImGui.BeginDisabled(selectedGameObject == null);
        if (ImGui.Button("Delete", hsize)) SceneManager.loadedScene.RemoveGameObject(selectedGameObject);
        ImGui.EndDisabled();

        ImGui.Separator();

        foreach (var gameObject in SceneManager.loadedScene.gameObjects) if (gameObject.transform.parent == null) DrawHierarchyMember(gameObject);
        ImGui.InvisibleButton("", ImGui.GetContentRegionAvail());
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(nameof(GameObject));
            if (!payload.IsNull)
            {
                var dragged = SceneManager.loadedScene.FindGameObject(*(int*)payload.Data);
                if (dragged != null) reparentque.Add((dragged, null));
            }
            ImGui.EndDragDropTarget();
        }
        ImGui.End();

        ImGui.Begin("Inspector");
        if (selectedGameObject != null)
        {
            ImGui.PushID(selectedGameObject.id);

            // enabled and name
            ImGui.Checkbox("##first", ref selectedGameObject.enabled);
            ImGui.SameLine();
            ImGui.InputText("##second", ref selectedGameObject.name, 100);

            ImGui.Separator();

            // draw each component
            removeComponentQue.Clear();
            foreach (var component in selectedGameObject.components) DrawComponent(component);
            foreach (var component in removeComponentQue) selectedGameObject.RemoveComponent(component);

            ImGui.Separator();
            ImGui.Spacing();

            // add component button
            int width = 128;
            int center = (int)ImGui.GetContentRegionAvail().X / 2;
            ImGui.SetCursorPosX(center - width / 2);
            if (ImGui.Button("add component", new Vector2(width, 0))) ImGui.OpenPopup("ChooseComponent");

            // add component popup
            int selectedIndex = -1;
            if (ImGui.BeginPopup("ChooseComponent"))
            {
                for (int i = 0; i < componentTypes.Length; i++)
                {
                    var type = componentTypes[i];
                    if (ImGui.Selectable(type.Name))
                    {
                        selectedIndex = i;
                        var selected = componentTypes[selectedIndex];
                        selectedGameObject.AddComponentOfType(selected);
                    }
                }
                ImGui.EndPopup();
            }

            ImGui.PopID();
        }
        ImGui.End();

        ImGui.Begin("Metrics", ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar);

        // frametime graph
        if (ImPlot.BeginPlot("frametime: " + (int)Metrics.averageFrameTime + "ms", new(-1, 128), ImPlotFlags.NoLegend | ImPlotFlags.NoMouseText | ImPlotFlags.NoInputs | ImPlotFlags.NoFrame))
        {
            // setup axis
            var xflags = ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoGridLines;
            var yflags = ImPlotAxisFlags.NoLabel;
            ImPlot.SetupAxes("frame", "time (ms)", xflags, yflags);
            ImPlot.SetupAxesLimits(0, Metrics.framesToCheck, 0, 40);
            
            // plot frames when ready
            if (Metrics.dataIsReady)
            {
                ImPlot.PlotLine("frametime", ref Metrics.lastFrameTimes[0], Metrics.framesToCheck, ImPlotLineFlags.Shaded);
            }

            ImPlot.EndPlot();
        }

        // framerate graph
        if (ImPlot.BeginPlot("framerate: " + (int)Metrics.averageFrameRate + "fps", new(-1, 128), ImPlotFlags.NoLegend | ImPlotFlags.NoMouseText | ImPlotFlags.NoInputs | ImPlotFlags.NoFrame))
        {
            // setup axis
            var xflags = ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoGridLines;
            var yflags = ImPlotAxisFlags.NoLabel;
            ImPlot.SetupAxes("frame", "rate (fps)", xflags, yflags);
            ImPlot.SetupAxesLimits(0, Metrics.framesToCheck, 0, 512);
            
            // plot frames when ready
            if (Metrics.dataIsReady)
            {
                ImPlot.PlotLine("framerate", ref Metrics.lastFrameRates[0], Metrics.framesToCheck, ImPlotLineFlags.Shaded);
            }

            ImPlot.EndPlot();
        }

        ImGui.End();
    }

    public static void DrawHierarchyMember(GameObject gameObject)
    {
        int id = gameObject.id;
        ImGui.PushID(id);

        var flags = ImGuiTreeNodeFlags.OpenOnArrow;
        if (gameObject.transform.children.Count == 0) flags |= ImGuiTreeNodeFlags.Leaf;
        if (selectedGameObject == gameObject) flags |= ImGuiTreeNodeFlags.Selected;
        bool open = ImGui.TreeNodeEx(gameObject.name, flags);
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) selectedGameObject = gameObject;

        if (ImGui.BeginDragDropSource())
        {
            ImGui.SetDragDropPayload(nameof(GameObject), &id, sizeof(int));
            ImGui.Text(gameObject.name);
            ImGui.EndDragDropSource();
        }
        
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(nameof(GameObject));
            if (!payload.IsNull)
            {
                var dragged = SceneManager.loadedScene.FindGameObject(*(int*)payload.Data);
                if (dragged != null && !dragged.transform.children.Contains(gameObject.transform)) reparentque.Add((dragged, gameObject));
            }
            ImGui.EndDragDropTarget();
        }

        if (open)
        {
            foreach (var child in gameObject.transform.children)
            {
                DrawHierarchyMember(child.gameObject);
            }
            ImGui.TreePop();
        }

        ImGui.PopID();
    }

    public static void DrawComponent(Component component)
    {
        var type = component.GetType();
        
        var flags = ImGuiTreeNodeFlags.None;
        if (type == typeof(Transform)) flags |= ImGuiTreeNodeFlags.DefaultOpen;

        void DrawVariables()
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields) DrawField(field, component);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite);
            foreach (var property in properties) DrawProperty(property, component);
        }

        bool visible = true;

        if (component is not Transform) if (ImGui.CollapsingHeader(type.Name, ref visible, flags)) DrawVariables();
        if (component is Transform) if (ImGui.CollapsingHeader(type.Name, flags)) DrawVariables();

        if (!visible)
        {
            visible = true;
            removeComponentQue.Add(component);
        }
    }

    private static void DrawField(FieldInfo field, Component component)
    {
        bool show = false;
        string showname = null;
        foreach (var attribute in field.GetCustomAttributes()) if (attribute is ShowAttribute showAttribute)
        {
            show = true;
            showname = showAttribute.name;
            break;
        }
        if (!show) return;

        var type = field.FieldType;
        var name = showname == null ? field.Name : showname;
        var curvalue = field.GetValue(component);

        if (type == typeof(int))
        {
            int value = (int)curvalue;
            if (ImGui.DragInt(name, ref value)) field.SetValue(component, value);
        }
        else if (type == typeof(float))
        {
            float value = (float)curvalue;
            if (ImGui.DragFloat(name, ref value, 0.1f)) field.SetValue(component, value);
        }
        else if (type == typeof(string))
        {
            string value = (string)curvalue;
            if (ImGui.InputText(name, ref value, 100)) field.SetValue(component, value);
        }
        else if (type == typeof(Vector3))
        {
            Vector3 value = (Vector3)curvalue;
            if (ImGui.DragFloat3(name, ref value, 0.1f)) field.SetValue(component, value);
        }
        else if (type == typeof(Vector2))
        {
            Vector2 value = (Vector2)curvalue;
            if (ImGui.DragFloat2(name, ref value, 0.1f)) field.SetValue(component, value);
        }
        else if (type == typeof(bool))
        {
            bool value = (bool)curvalue;
            if (ImGui.Checkbox(name, ref value)) field.SetValue(component, value);
        }
    }

    private static void DrawProperty(PropertyInfo property, Component component)
    {
        bool show = false;
        string showname = null;
        foreach (var attribute in property.GetCustomAttributes()) if (attribute is ShowAttribute showAttribute)
        {
            show = true;
            showname = showAttribute.name;
            break;
        }
        if (!show) return;

        var type = property.PropertyType;
        var name = showname == null ? property.Name : showname;
        var curvalue = property.GetValue(component);

        if (type == typeof(int))
        {
            int value = (int)curvalue;
            if (ImGui.DragInt(name, ref value)) property.SetValue(component, value);
        }
        else if (type == typeof(float))
        {
            float value = (float)curvalue;
            if (ImGui.DragFloat(name, ref value, 0.1f)) property.SetValue(component, value);
        }
        else if (type == typeof(string))
        {
            string value = (string)curvalue;
            if (ImGui.InputText(name, ref value, 100)) property.SetValue(component, value);
        }
        else if (type == typeof(Vector3))
        {
            Vector3 value = (Vector3)curvalue;
            if (ImGui.DragFloat3(name, ref value, 0.1f)) property.SetValue(component, value);
        }
        else if (type == typeof(Vector2))
        {
            Vector2 value = (Vector2)curvalue;
            if (ImGui.DragFloat2(name, ref value, 0.1f)) property.SetValue(component, value);
        }
        else if (type == typeof(bool))
        {
            bool value = (bool)curvalue;
            if (ImGui.Checkbox(name, ref value)) property.SetValue(component, value);
        }
    }

    public static void OpenLink(string url)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        
        if (isWindows)
        {
            var info = new ProcessStartInfo(url)
            {
                FileName = url,
                UseShellExecute = true,
            };

            Process.Start(info);
        }

        if (isLinux) Process.Start("xdg-open", url);

        if (isMac) Process.Start("open", url);
    }
}