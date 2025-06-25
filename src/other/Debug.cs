using System;
using System.IO;
using System.Text.Json;

namespace Concrete;

public static class Debug
{
    public static List<string> history = [];

    public static void Log(string text)
    {
        history.Add(text);
    }

    public static void Clear()
    {
        history.Clear();
    }
}