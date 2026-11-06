using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildGame : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;
    
    public void OnPostprocessBuild(BuildReport report)
    {
        Debug.Log("Build completed: " + report.summary.outputPath);
    }
    
    [MenuItem("WITCHBOUND/Build Linux")]
    public static void BuildLinux()
    {
        var playerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Game/MainGameScene.unity" },
            locationPathName = "/home/mikmik/Desktop/WITCHBOUND/Builds/WITCHBOUND",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        };
        
        var report = BuildPipeline.BuildPlayer(playerOptions);
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + report.summary.outputPath);
        }
        else
        {
            Debug.LogError("Build failed: " + report.summary.result);
        }
    }
}
