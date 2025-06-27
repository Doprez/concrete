using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Concrete;

public static class AssetDatabase
{
    private static Dictionary<Guid, string> GuidToPathMap = [];
    private static Dictionary<string, Guid> PathToGuidMap = [];

    private static string[] include = [".scene", ".glb"];

    // scans for missing guids and rebuilds them
    public static void Initialize(string root)
    {
        // empty database
        GuidToPathMap.Clear();
        PathToGuidMap.Clear();

        // find assets
        List<string> temp = [];
        foreach (string ext in include) temp.AddRange(Directory.GetFiles(root, $"*{ext}", SearchOption.AllDirectories));
        string[] assetfiles = temp.ToArray();
        
        // ensure every asset file has a guid file
        foreach (string assetpath in assetfiles)
        {
            string guidpath = assetpath + ".guid";

            // if guid file doesnt exist make a new one
            if (!File.Exists(guidpath)) File.WriteAllText(guidpath, Guid.NewGuid().ToString());
        }

        // ensure every guid file has an asset file
        string[] guidfiles = Directory.GetFiles(root, "*.guid", SearchOption.AllDirectories);
        foreach (string guidpath in guidfiles)
        {
            // remove .guid extension
            string assetpath = guidpath.Substring(0, guidpath.Length - 5);

            // deal with orphan guid files
            if (!File.Exists(assetpath))
            {
                File.Delete(guidpath);
                continue;
            }

            // populate database
            Guid decoded = Guid.Parse(File.ReadAllText(guidpath));
            string relative = Path.GetRelativePath(root, assetpath);
            GuidToPathMap[decoded] = relative;
            PathToGuidMap[relative] = decoded;
        }
    }

    public static string GetPath(Guid guid)
    {
        return GuidToPathMap.GetValueOrDefault(guid);
    }

    public static Guid GetGuid(string relativePath)
    {
        return PathToGuidMap.GetValueOrDefault(relativePath);
    }
}