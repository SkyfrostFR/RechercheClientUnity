using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line builds of the twin client, e.g.
///   Unity -batchmode -quit -projectPath . -executeMethod TwinClientBuild.BuildLinux
/// Output: Builds/Linux/TiagoClient.x86_64 (Builds/ is git-ignored).
/// </summary>
public static class TwinClientBuild
{
    private const string Scene = "Assets/Scenes/TiagoClient.unity";

    public static void BuildLinux()
    {
        Build(BuildTarget.StandaloneLinux64, "Builds/Linux/TiagoClient.x86_64");
    }

    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/TiagoClient.exe");
    }

    private static void Build(BuildTarget target, string path)
    {
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = path,
            target = target,
            options = BuildOptions.Development
        });
        Debug.Log($"[TwinClientBuild] {target}: {report.summary.result}, " +
                  $"{report.summary.totalErrors} error(s) -> {path}");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
