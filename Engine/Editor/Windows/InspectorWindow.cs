using System.Numerics;
using System.Drawing;
using System.Reflection;

using Hexa.NET.ImGui;
using System.Text;

namespace Concrete;

public static unsafe class InspectorWindow
{
    private static List<Component> removeComponentQue = [];

    private static Type[] allTypes = GetAllTypesInAllAssemblies();
    private static Type[] componentTypes = allTypes.Where(type => type.IsClass && !type.IsAbstract && type != typeof(Transform) && type.IsSubclassOf(typeof(Component))).ToArray();

    private static Type[] GetAllTypesInAllAssemblies()
    {
        List<Type> types = [];
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies) types.AddRange(assembly.GetTypes());
        return types.ToArray();
    }

    public static void RefreshComponentTypes()
    {
        allTypes = GetAllTypesInAllAssemblies();
        componentTypes = allTypes.Where(type => type.IsClass && !type.IsAbstract && type != typeof(Transform) && type.IsSubclassOf(typeof(Component))).ToArray();
    }
    
    public static void Draw(float deltaTime)
    {
        ImGui.Begin("Inspector");
        if (HierarchyWindow.selectedGameObject != null)
        {
            ImGui.PushID(HierarchyWindow.selectedGameObject.guid.ToString());

            // enabled and name
            ImGui.Checkbox("##first", ref HierarchyWindow.selectedGameObject.enabled);
            ImGui.SameLine();
            ImGui.InputText("##second", ref HierarchyWindow.selectedGameObject.name, 100);

            ImGui.Separator();

            // draw each component
            removeComponentQue.Clear();
            foreach (var component in HierarchyWindow.selectedGameObject.components) DrawComponent(component);
            foreach (var component in removeComponentQue) HierarchyWindow.selectedGameObject.RemoveComponent(component);

            ImGui.Separator();
            ImGui.Spacing();

            // add component button
            int width = (int)ImGui.GetContentRegionAvail().X;
            if (ImGui.Button("add component", new Vector2(width, 0))) ImGui.OpenPopup("ChooseComponent");

            // script drag and drop area
            ImGui.InvisibleButton("##", ImGui.GetContentRegionAvail());
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("file_path");
                if (!payload.IsNull)
                {
                    string file = Encoding.UTF8.GetString((byte*)payload.Data, payload.DataSize);
                    string relative = Path.GetRelativePath(ProjectManager.projectRoot, file);
                    string extension = Path.GetExtension(relative);

                    // if file is script
                    if (extension == ".cs")
                    {
                        var type = ScriptManager.GetClassTypeOfScript(file);
                        HierarchyWindow.selectedGameObject.AddComponentOfType(type);
                    }
                }
                ImGui.EndDragDropTarget();
            }

            // add component popup
            int selectedIndex = -1;
            if (ImGui.BeginPopup("ChooseComponent"))
            {
                RefreshComponentTypes();
                for (int i = 0; i < componentTypes.Length; i++)
                {
                    var type = componentTypes[i];
                    if (ImGui.Selectable(type.Name))
                    {
                        selectedIndex = i;
                        var selected = componentTypes[selectedIndex];
                        HierarchyWindow.selectedGameObject.AddComponentOfType(selected);
                    }
                }
                ImGui.EndPopup();
            }

            ImGui.PopID();
        }
        ImGui.End();
    }

    public static void DrawComponent(Component component)
    {
        var type = component.GetType();
        
        var flags = ImGuiTreeNodeFlags.None;
        if (type == typeof(Transform)) flags |= ImGuiTreeNodeFlags.DefaultOpen;

        void DrawVariables()
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields) DrawMember(field, component);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite);
            foreach (var property in properties) DrawMember(property, component);
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

    private static void DrawMember(MemberInfo member, Component component)
    {
        bool isfield = member is FieldInfo;
        var tryfield = member as FieldInfo;
        var tryproperty = member as PropertyInfo;

        bool show = false;
        string showAttributeName = null;
        var attributes = isfield ? tryfield.GetCustomAttributes() : tryproperty.GetCustomAttributes();
        foreach (var attribute in attributes) if (attribute is ShowAttribute showAttribute)
        {
            show = true;
            showAttributeName = showAttribute.name;
            break;
        }
        if (!show) return;

        var type = isfield ? tryfield.FieldType : tryproperty.PropertyType;
        var membername = isfield ? tryfield.Name : tryproperty.Name;
        var nametoshow = showAttributeName ?? membername;
        var curvalue = isfield ? tryfield.GetValue(component) : tryproperty.GetValue(component);

        if (type == typeof(int))
        {
            int value = (int)curvalue;
            if (ImGui.DragInt(nametoshow, ref value)) SetMemberValue(value);
        }
        else if (type == typeof(float))
        {
            float value = (float)curvalue;
            if (ImGui.DragFloat(nametoshow, ref value, 0.1f)) SetMemberValue(value);
        }
        else if (type == typeof(string))
        {
            string value = (string)curvalue;
            if (ImGui.InputText(nametoshow, ref value, 100)) SetMemberValue(value);
        }
        else if (type == typeof(bool))
        {
            bool value = (bool)curvalue;
            if (ImGui.Checkbox(nametoshow, ref value)) SetMemberValue(value);
        }
        else if (type == typeof(Vector3))
        {
            Vector3 value = (Vector3)curvalue;
            if (ImGui.DragFloat3(nametoshow, ref value, 0.1f)) SetMemberValue(value);
        }
        else if (type == typeof(Vector2))
        {
            Vector2 value = (Vector2)curvalue;
            if (ImGui.DragFloat2(nametoshow, ref value, 0.1f)) SetMemberValue(value);
        }
        else if (type == typeof(Color))
        {
            var flags = ImGuiColorEditFlags.NoInputs;
            var ColorToVector = (Color color) => new Vector3(color.R, color.G, color.B) / 255f;
            var VectorToColor = (Vector3 vector) => Color.FromArgb(255, (int)(vector.X * 255), (int)(vector.Y * 255), (int)(vector.Z * 255));
            var value = ColorToVector((Color)curvalue);
            if (ImGui.ColorPicker3(nametoshow, ref value, flags)) SetMemberValue(VectorToColor(value));
        }
        else if (type == typeof(ModelGuid))
        {
            ModelGuid model_guid = (ModelGuid)curvalue;

            string display = "...";

            // it has guid
            if (model_guid != null)
            {
                // get guid
                Guid asset_guid = model_guid.guid;

                // set name
                display = AssetDatabase.GetPath(asset_guid);
            }

            // readonly text box
            ImGui.InputText(nametoshow, ref display, 100, ImGuiInputTextFlags.ReadOnly);

            // drag and dropping
            if (ImGui.BeginDragDropTarget())
            {
                // if drag and drop is asset
                var payload = ImGui.AcceptDragDropPayload("file_path");
                if (!payload.IsNull)
                {
                    string file = Encoding.UTF8.GetString((byte*)payload.Data, payload.DataSize);
                    string relative = Path.GetRelativePath(ProjectManager.projectRoot, file);
                    string extension = Path.GetExtension(relative);

                    // if asset is model
                    if (extension == ".glb" || extension == ".gltf")
                    {
                        var new_model_ref = new ModelGuid();
                        new_model_ref.guid = AssetDatabase.GetGuid(relative);
                        SetMemberValue(new_model_ref);
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }
        else if (type == typeof(GameObjectGuid))
        {
            GameObjectGuid gameobject_guid = (GameObjectGuid)curvalue;

            string display = "...";

            // if has guid
            if (gameobject_guid != null)
            {
                // get guid
                Guid asset_guid = gameobject_guid.guid;

                // set name
                display = Scene.Current.FindGameObject(asset_guid).name;
            }

            // readonly text box
            ImGui.InputText(nametoshow, ref display, 100, ImGuiInputTextFlags.ReadOnly);

            // drag and dropping
            if (ImGui.BeginDragDropTarget())
            {
                // if drag and drop is gameobject guid
                var payload = ImGui.AcceptDragDropPayload("gameobject_guid");
                if (!payload.IsNull)
                {
                    var new_gobj_guid = *(Guid*)payload.Data;
                    var new_gobj = Scene.Current.FindGameObject(new_gobj_guid);

                    // if gameobject exists
                    if (new_gobj != null)
                    {
                        var new_gobj_ref = new GameObjectGuid();
                        new_gobj_ref.guid = new_gobj_guid;
                        SetMemberValue(new_gobj_ref);
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }

        void SetMemberValue(object value)
        {
            if (isfield) tryfield.SetValue(component, value);
            else tryproperty.SetValue(component, value);
        }
    }
}