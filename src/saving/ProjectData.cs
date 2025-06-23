using System.Numerics;

namespace Concrete;

public class ProjectData
{
    public string projectName;
    public string firstScene;

    public ProjectData(string projectName, string firstScene)
    {
        this.projectName = projectName;
        this.firstScene = firstScene;
    }
}