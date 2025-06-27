using System.Numerics;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

namespace Concrete;

public static unsafe class InspectorWindow
{
    private static List<Component> removeComponentQue = [];
    private static Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();
    private static Type[] componentTypes = allTypes.Where(type => type.IsClass && !type.IsAbstract && type != typeof(Transform) && type.IsSubclassOf(typeof(Component))).ToArray();
    
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
        else if (type == typeof(Guid))
        {
            Guid value = (Guid)curvalue;
            string str = value.ToString();
            ImGui.InputText(name, ref str, 100, ImGuiInputTextFlags.ReadOnly);
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
        else if (type == typeof(Guid))
        {
            Guid value = (Guid)curvalue;
            string str = value.ToString();
            ImGui.InputText(name, ref str, 100, ImGuiInputTextFlags.ReadOnly);
        }
    }
}