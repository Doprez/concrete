using System.Numerics;

using Hexa.NET.ImGui;

namespace Concrete;

public static unsafe class HierarchyWindow
{
    private static Guid selectedGameObjectIdentifier;
    public static GameObject selectedGameObject
    {
        get => Scene.Current.FindGameObject(selectedGameObjectIdentifier);
        set => selectedGameObjectIdentifier = value.guid;
    }

    private static List<(GameObject, GameObject)> reparentque = [];

    public static void Draw(float deltaTime)
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

        // render window
        ImGui.Begin("Hierarchy");

        var hbuttonsize = new Vector2(ImGui.GetContentRegionAvail().X / 2 - ImGui.GetStyle().ItemSpacing.X / 2, 0);

        if (ImGui.Button("Create", hbuttonsize)) Scene.Current.AddGameObject();
        ImGui.SameLine();
        ImGui.BeginDisabled(selectedGameObject == null);
        if (ImGui.Button("Delete", hbuttonsize)) Scene.Current.RemoveGameObject(selectedGameObject);
        ImGui.EndDisabled();

        ImGui.Separator();

        foreach (var gameObject in Scene.Current.gameObjects) if (gameObject.transform.parent == null) DrawHierarchyMember(gameObject);
        ImGui.InvisibleButton("##", ImGui.GetContentRegionAvail());
        Guid? guid = DragAndDrop.TargetGuid("gameobject_guid");
        if (guid != null)
        {
            var dragged = Scene.Current.FindGameObject(guid.Value);
            if (dragged != null) reparentque.Add((dragged, null));
        }

        ImGui.End();
    }

    private static void DrawHierarchyMember(GameObject gameObject)
    {
        Guid guid = gameObject.guid;
        ImGui.PushID(guid.ToString());

        var flags = ImGuiTreeNodeFlags.OpenOnArrow;
        if (gameObject.transform.children.Count == 0) flags |= ImGuiTreeNodeFlags.Leaf;
        if (selectedGameObject == gameObject) flags |= ImGuiTreeNodeFlags.Selected;
        bool open = ImGui.TreeNodeEx(gameObject.name, flags);
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) selectedGameObject = gameObject;

        DragAndDrop.SourceGuid("gameobject_guid", guid, gameObject.name);

        Guid? dragged_guid = DragAndDrop.TargetGuid("gameobject_guid");
        if (dragged_guid != null)
        {
            var dragged = Scene.Current.FindGameObject(dragged_guid.Value);
            if (dragged != null && !dragged.transform.children.Contains(gameObject.transform)) reparentque.Add((dragged, gameObject));
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
}