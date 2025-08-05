namespace Concrete;

public static class AssetDatabase
{
    private static Dictionary<Guid, string> GuidToPathMap = [];
    private static Dictionary<string, Guid> PathToGuidMap = [];

    private static string[] includes = [".scene", ".glb", ".cs"];

    public static string GuidPathFromAssetPath(string asset_path)
    {
        string asset_basename = Path.GetFileNameWithoutExtension(asset_path);
        string guid_path = Path.Combine(Path.GetDirectoryName(asset_path), asset_basename + ".guid");
        return guid_path;
    }

    public static string AssetPathFromGuidPath(string guid_path)
    {
        string guid_basename = Path.GetFileNameWithoutExtension(guid_path);
        string asset_parent_path = Path.GetDirectoryName(guid_path);

        foreach (var include in includes)
        {
            string attempt = Path.Combine(asset_parent_path, guid_basename + include);
            if (File.Exists(attempt)) return attempt;
        }

        return null;
    }

    // scans for missing guids and rebuilds them
    public static void Rebuild()
    {
        // set root
        string root = ProjectManager.projectRoot;

        // empty database
        GuidToPathMap.Clear();
        PathToGuidMap.Clear();

        // find assets
        List<string> temp = [];
        foreach (string include in includes) temp.AddRange(Directory.GetFiles(root, $"*{include}", SearchOption.AllDirectories));
        string[] assetfiles = temp.ToArray();
        
        // ensure every asset file has a guid file
        foreach (string assetpath in assetfiles)
        {
            string guidpath = Path.ChangeExtension(assetpath, ".guid");

            // if guid file doesnt exist make a new one
            if (!File.Exists(guidpath)) File.WriteAllText(guidpath, Guid.NewGuid().ToString());
        }

        // ensure every guid file has an asset file
        string[] guidfiles = Directory.GetFiles(root, "*.guid", SearchOption.AllDirectories);
        foreach (string guidpath in guidfiles)
        {
            // find correct asset file
            string assetpath = null;
            foreach (var ext in includes)
            {
                var candidate = Path.ChangeExtension(guidpath, ext);
                if (File.Exists(candidate))
                {
                    assetpath = candidate;
                    break;
                }
            }

            // deal with orphan guid files
            if (assetpath == null)
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