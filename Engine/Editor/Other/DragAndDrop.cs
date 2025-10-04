using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;

namespace Concrete;

public static unsafe class DragAndDrop
{
    public static string TargetString(string type)
    {
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(type);
            if (!payload.IsNull)
            {
                string info = Encoding.UTF8.GetString((byte*)payload.Data, payload.DataSize);
                return info;
            }
            ImGui.EndDragDropTarget();
        }

        return null;
    }

    public static void SourceString(string type, string info, string display)
    {
        if (ImGui.BeginDragDropSource())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(info);

            fixed (byte* ptr = bytes) ImGui.SetDragDropPayload(type, ptr, (nuint)bytes.Length);

            ImGui.Text(display);
            
            ImGui.EndDragDropSource();
        }
    }

    public static Guid? TargetGuid(string type)
    {
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(type);
            if (!payload.IsNull)
            {
                Guid guid = *(Guid*)payload.Data;
                return guid;
            }
            ImGui.EndDragDropTarget();
        }

        return null;
    }

    public static void SourceGuid(string type, Guid guid, string display)
    {
        if (ImGui.BeginDragDropSource())
        {
            ImGui.SetDragDropPayload(type, &guid, (nuint)sizeof(Guid));
            ImGui.Text(display);
            ImGui.EndDragDropSource();
        }
    }
}