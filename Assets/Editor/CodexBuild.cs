#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CodexBuild
{
    private const string MainScene = "Assets/00_Scenes/SampleScene.unity";
    private const string DefaultBuildPath = "Builds/Windows/FINAL_GAME.exe";

    public static void BuildWindows()
    {
        string outputPath = GetArgumentValue("-codexBuildPath");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = DefaultBuildPath;
        }

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Console.Error.WriteLine("Build failed: " + report.summary.result);
            EditorApplication.Exit(1);
            return;
        }

        Console.WriteLine("Build succeeded: " + outputPath);
        EditorApplication.Exit(0);
    }

    private static string GetArgumentValue(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
#endif
